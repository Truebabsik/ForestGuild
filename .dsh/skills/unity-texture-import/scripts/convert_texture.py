"""Convert one material from the Resources library into Unity-ready textures.

The library stores 4096x4096 source maps in a generic metallic/roughness layout.
URP Lit wants something different, so this script repacks:

    basecolor        -> <name>_color.jpg    (sRGB, full size)
    normalOGL        -> <name>_normal.png   (linear, NormalMap)
    metallic
    ambientocclusion -> <name>_masks.png    (R = metallic, G = AO, A = smoothness)
    roughness

Height maps are ignored: URP Lit does not read them.

Map file names are matched case-insensitively on purpose -- the packs disagree
with each other ("normalogl" vs "normalOGL").

Usage:
    python convert_texture.py --material stone_wood_01/Stone_01 --name stone01

    --material  path below Resources/textures, or just the material folder name
    --name      output base name in Assets/Textures
    --size      size of color and normal (default 1024)
    --mask-size size of the packed mask map (default 512)
"""

import argparse
import pathlib
import sys

from PIL import Image

# WHY the search instead of a fixed depth: the script assumes skills sit directly under the repo
# (<repo>/skills/.../scripts), and in this project they live one level down in .dsh - so parents[3]
# pointed at .dsh and every run died with "material not found below .dsh\Resources\textures". Finding
# the library by its own existence works for both layouts and for any future move.
_HERE = pathlib.Path(__file__).resolve()
REPO = next(
    (p for p in _HERE.parents if (p / "Resources" / "textures").is_dir()),
    _HERE.parents[3],
)
LIBRARY = REPO / "Resources" / "textures"
OUT_DIR = REPO / "Assets" / "Textures"

# map suffix -> canonical key, matched on the file name after the last underscore.
#
# WHY the extra aliases: packs do not agree on a vocabulary. The stone/wood library ships
# `basecolor`/`normalOGL`, while the Lava2 pack ships `COLOR`/`NRM`/`OCC`/`DISP`. Without an
# alias those files are skipped silently and the material comes out untextured with no error.
#
# `normal`/`nrm` are DirectX normals (green pointing the other way) and are flipped on import:
# using them as-is gives inverted bumps -- the exact trap AGENTS.md warns about.
#
# `spec` is specular INTENSITY, not glossiness. Lava2's SPEC is flat mid-grey (~130) wherever the
# surface is a lava lake and correlates with OCC at 0.88, so it says "how bright the highlight is",
# not "how sharp". URP has no specular-intensity slot, so it is folded into smoothness -- a
# compromise, and the reason `build_masks` blends it instead of taking it raw.
KEY_ALIASES = {
    "basecolor": "basecolor",
    "color": "basecolor",
    "normalogl": "normal",
    "normal": "normal_dx",
    "nrm": "normal_dx",
    "roughness": "roughness",
    "metallic": "metallic",
    "ambientocclusion": "ao",
    "occ": "ao",
    "height": "height",
    "disp": "height",
    "emissive": "emissive",
    "spec": "specular",
}


def find_material(selector: str) -> pathlib.Path:
    """Resolve a material folder from a selector such as `Wood_01` or `stone_wood_01/Wood_01`.

    The library nests materials as `<Pack>/JPEG/<Material>`, and material folders
    carry a `Stylized_` prefix. So the last selector fragment must equal the
    folder name or be its `_`-delimited suffix (`HandpaintedStones_02` matches
    `Stylized_HandpaintedStones_02`). Earlier fragments only have to appear
    somewhere in the path.

    Matching everything as a loose substring would be wrong: `Wood_01` is a
    substring of the pack name `Materials_Stylized_stone_wood_01`.
    """
    wanted = [p for p in selector.replace("\\", "/").split("/") if p]
    material_name = wanted[-1].lower()
    ancestors = [w.lower() for w in wanted[:-1]]

    candidates = []
    for folder in LIBRARY.rglob("*"):
        if not folder.is_dir() or not has_basecolor(folder):
            continue
        name = folder.name.lower()
        if name != material_name and not name.endswith("_" + material_name):
            continue
        parts = [p.lower() for p in folder.relative_to(LIBRARY).parts]
        if all(any(a in part for part in parts) for a in ancestors):
            candidates.append(folder)

    if not candidates:
        raise SystemExit(f"material not found below {LIBRARY}: {selector}")
    if len(candidates) > 1:
        listing = "\n  ".join(str(c.relative_to(LIBRARY)) for c in candidates)
        raise SystemExit(f"ambiguous selector {selector!r}, {len(candidates)} matches:\n  {listing}")
    return candidates[0]


def has_basecolor(folder: pathlib.Path) -> bool:
    """True when the folder holds a colour map under any of the known names."""
    return "basecolor" in collect_maps(folder)


def collect_maps(folder: pathlib.Path) -> dict:
    """Map canonical key -> file path, ignoring case in the map suffix."""
    found = {}
    for f in sorted(folder.glob("*.jpg")) + sorted(folder.glob("*.png")):
        stem = f.stem
        if "_" not in stem:
            continue
        key = KEY_ALIASES.get(stem.rsplit("_", 1)[1].lower())
        if key and key not in found:
            found[key] = f
    return found


def to_opengl(img: Image.Image) -> Image.Image:
    """Flip the green channel of a DirectX normal map.

    A normal map is a vector field: `G` stores the Y component. DirectX points Y down the
    texture, OpenGL points it up, so the same surface lit by the same light shows raised
    detail with one and pressed-in detail with the other. `255 - G` converts between them.

    Verified numerically on Lava2 rather than assumed: reconstructing the normal from
    `DISP` correlates G with `-dh/dv` at +0.66 (DirectX) and with `+dh/dv` at -0.66.
    """
    r, g, b = img.convert("RGB").split()
    return Image.merge("RGB", (r, g.point(lambda v: 255 - v), b))


def resize(img: Image.Image, size: int, mode: str) -> Image.Image:
    img = img.convert(mode)
    if img.size == (size, size):
        return img
    return img.resize((size, size), Image.Resampling.LANCZOS)


def flatness(img: Image.Image) -> str:
    """Report a map that carries no information.

    Several packs ship placeholder maps: `metallic` all zero, `ambientocclusion`
    and `roughness` all white. That is silent -- an all-white roughness map means
    smoothness 0, so the surface ends up fully matte with no warning at all.
    """
    lo, hi = img.convert("L").getextrema()
    if lo == hi:
        return f"FLAT {lo}"
    return ""


TRANSPOSE = {
    90: Image.Transpose.ROTATE_90,    # PIL rotates counter-clockwise
    180: Image.Transpose.ROTATE_180,
    270: Image.Transpose.ROTATE_270,
}


def apply_rotation(img: Image.Image, quad: int, is_normal: bool) -> Image.Image:
    """Rotate a map by a multiple of 90 degrees.

    Needed because a map's grain direction is baked in, while a Unity cube always
    puts U along each face's first axis. A beam is 6 m long and 0.2 m tall, so its
    long faces carry U along the length -- a texture whose grain runs along V ends
    up crosswise. Rotating the source fixes that.

    A normal map is a vector field, not a picture: rotating the pixels is not
    enough, the tangent-space XY has to be rotated as well. For a 90 degree
    counter-clockwise image rotation the height field becomes `h'(u,v) = h(v, 1-u)`,
    so the normal goes to `(-n_y, n_x, n_z)`, which in stored channels is
    `R' = 255 - G`, `G' = R`. The 270 case is that inverse, and 180 flips both.
    """
    if quad == 0:
        return img
    img = img.transpose(TRANSPOSE[quad])
    if not is_normal:
        return img
    r, g, b = img.split()
    if quad == 180:
        return Image.merge("RGB", (r.point(lambda v: 255 - v), g.point(lambda v: 255 - v), b))
    if quad == 90:
        return Image.merge("RGB", (g.point(lambda v: 255 - v), r, b))
    return Image.merge("RGB", (g, r.point(lambda v: 255 - v), b))


def build_masks(maps: dict, size: int) -> Image.Image:
    """URP Lit reads R = metallic and A = smoothness; _OcclusionMap reads G."""
    if "metallic" in maps:
        metallic = resize(Image.open(maps["metallic"]), size, "L")
    else:
        # stone, wood and dirt are dielectrics; the packs simply omit the map
        metallic = Image.new("L", (size, size), 0)

    if "ao" in maps:
        ao = resize(Image.open(maps["ao"]), size, "L")
    else:
        ao = Image.new("L", (size, size), 255)

    if "roughness" in maps:
        roughness = resize(Image.open(maps["roughness"]), size, "L")
        smoothness = roughness.point(lambda v: 255 - v)
    elif "specular" in maps:
        # URP drops the specular map entirely on import, so its information is not lost silently:
        # it is remapped into smoothness. Full multiply would make lava lakes mirror-like, which is
        # wrong for a stylised read -- the map is mellowed towards a neutral 0.35 gloss.
        spec = resize(Image.open(maps["specular"]), size, "L")
        smoothness = spec.point(lambda v: int(90 + v * 0.45))
    else:
        smoothness = Image.new("L", (size, size), 128)

    masks = Image.merge("RGBA", (metallic, ao, Image.new("L", (size, size), 0), smoothness))
    return masks


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--material", required=True)
    ap.add_argument("--name", required=True)
    ap.add_argument("--size", type=int, default=1024)
    ap.add_argument("--mask-size", type=int, default=512)
    ap.add_argument("--rotate", type=int, default=0, choices=(0, 90, 180, 270),
                    help="rotate every map so directional grain lands on the axis the mesh exposes as U")
    ap.add_argument("--emissive-from-color", action="store_true",
                    help="reuse the colour map as the emission map (self-lit materials such as lava)")
    args = ap.parse_args()

    folder = find_material(args.material)
    maps = collect_maps(folder)
    print(f"material : {folder.relative_to(REPO)}")
    print(f"maps     : {', '.join(sorted(maps)) or 'NONE'}")

    # a placeholder map is not an error, but it changes how the surface shades
    notes = []
    for key in ("metallic", "ao", "roughness", "specular"):
        if key in maps:
            verdict = flatness(Image.open(maps[key]))
            if verdict:
                notes.append(f"{key} is a placeholder ({verdict})")
    for n in notes:
        print(f"NOTE     : {n}")
    if any(n.startswith("roughness") for n in notes):
        print("NOTE     : flat roughness 255 -> smoothness 0, surface will be fully matte")

    if "basecolor" not in maps:
        raise SystemExit("no basecolor map -- nothing to build")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    written = []
    if args.rotate:
        print(f"NOTE     : rotating every map by {args.rotate} degrees")

    color = resize(Image.open(maps["basecolor"]), args.size, "RGB")
    color = apply_rotation(color, args.rotate, False)
    color_path = OUT_DIR / f"{args.name}_color.jpg"
    color.save(color_path, "JPEG", quality=92, subsampling=0)
    written.append(color_path)

    if "normal" in maps:
        normal = resize(Image.open(maps["normal"]), args.size, "RGB")
        normal = apply_rotation(normal, args.rotate, True)
    elif "normal_dx" in maps:
        normal = resize(Image.open(maps["normal_dx"]), args.size, "RGB")
        normal = to_opengl(normal)
        normal = apply_rotation(normal, args.rotate, True)
        print("NOTE     : DirectX normal flipped to OpenGL (green inverted)")
    else:
        normal = None
        print("WARNING: no *_normalOGL map found -- normal slot will be empty")

    if normal is not None:
        normal_path = OUT_DIR / f"{args.name}_normal.png"
        normal.save(normal_path, "PNG", optimize=True)
        written.append(normal_path)

    # Emission. Most packs ship a dedicated `emissive` map; self-lit materials such as lava often
    # do not, because their colour map already carries the heat -- `--emissive-from-color` reuses
    # it so the bright veins glow and the dark crust stays dark.
    if "emissive" in maps:
        emit = resize(Image.open(maps["emissive"]), args.size, "RGB")
        emit = apply_rotation(emit, args.rotate, False)
        emit_path = OUT_DIR / f"{args.name}_emissive.jpg"
        emit.save(emit_path, "JPEG", quality=92, subsampling=0)
        written.append(emit_path)
    elif args.emissive_from_color:
        emit_path = OUT_DIR / f"{args.name}_emissive.jpg"
        color.save(emit_path, "JPEG", quality=92, subsampling=0)
        written.append(emit_path)
        print("NOTE     : no emissive map -- colour map reused as emission")

    masks_path = OUT_DIR / f"{args.name}_masks.png"
    masks = apply_rotation(build_masks(maps, args.mask_size), args.rotate, False)
    masks.save(masks_path, "PNG", optimize=True)
    written.append(masks_path)

    print()
    total = 0
    for p in written:
        kib = p.stat().st_size / 1024
        total += kib
        print(f"  {p.name:<28} {kib:8.1f} KiB")
    print(f"  {'TOTAL':<28} {total:8.1f} KiB")
    return 0


if __name__ == "__main__":
    sys.exit(main())
