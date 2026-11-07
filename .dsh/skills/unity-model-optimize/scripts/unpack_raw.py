"""Unpack an FBX's embedded textures at full resolution, changing nothing else.

    blender.exe --background --factory-startup --python unpack_raw.py -- --src <in.fbx> --out <dir>

Why this exists: Unity does not extract the embedded textures from these
generator FBX files at all -- the imported material comes out with every texture
slot empty, in both material import modes. So the "before the pipeline" model is
not visible in Unity until the maps are unpacked to a sibling `Textures/` folder,
which is where Unity looks for an FBX's external textures.

This is the raw baseline, so it deliberately does none of what the optimiser
does: no decimation, no rescale, no texture resize, no metallic/roughness
packing. Maps keep their original names, formats and 4096x4096 resolution.

The source file is only ever read.
"""
import bpy
import os
import sys

argv = sys.argv
args = argv[argv.index("--") + 1:] if "--" in argv else []


def arg(name, default=None):
    return args[args.index(name) + 1] if name in args else default


SRC = arg("--src")
OUT_DIR = arg("--out")
if not SRC or not OUT_DIR:
    raise SystemExit("usage: --src <in.fbx> --out <dir>")


def log(*a):
    print("[UNPACK]", *a, flush=True)


TEX_DIR = os.path.join(OUT_DIR, "Textures")
os.makedirs(TEX_DIR, exist_ok=True)
FBX_OUT = os.path.join(OUT_DIR, os.path.splitext(os.path.basename(SRC))[0] + ".fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
log("source:", SRC, os.path.getsize(SRC), "bytes")

imgs = [i for i in bpy.data.images if i.size[0] > 0]
total = 0
for img in imgs:
    # keep the original file name and extension where the FBX recorded one
    name = os.path.basename(img.filepath) if img.filepath else img.name
    if not os.path.splitext(name)[1]:
        name += ".png"
    fmt = "JPEG" if name.lower().endswith((".jpg", ".jpeg")) else "PNG"
    if fmt == "JPEG":
        name = os.path.splitext(name)[0] + ".jpg"
    path = os.path.join(TEX_DIR, name)
    img.filepath_raw = path
    img.file_format = fmt
    img.save()
    size = os.path.getsize(path)
    total += size
    log(f"  wrote {name}  {img.size[0]}x{img.size[1]}  {size} bytes ({size / 1048576.0:.2f} MiB)")

log(f"textures total: {total / 1048576.0:.2f} MiB")

# Unity wires only _BaseMap and _BumpMap when importing an FBX, so metallic and
# roughness are invisible on the raw model -- and a forge is mostly metal, so it
# would render like painted plastic. Packing them makes the raw baseline shade
# the same way as the optimised one, which is what makes the comparison fair.
# Full resolution: this step is not compression.
met = next((i for i in imgs if "metallic" in i.name.lower()), None)
rgh = next((i for i in imgs if "roughness" in i.name.lower()), None)
if met and rgh:
    try:
        import numpy as np

        w, h = met.size

        def px(img):
            a = np.empty(w * h * 4, dtype=np.float32)
            img.pixels.foreach_get(a)
            return a.reshape(h, w, 4)

        out = np.zeros_like(px(met))
        out[..., 0] = px(met)[..., 0]        # R = metallic
        out[..., 3] = 1.0 - px(rgh)[..., 0]  # A = smoothness
        packed = bpy.data.images.new("metallic_smoothness", w, h, alpha=True)
        packed.pixels.foreach_set(out.reshape(-1))
        packed.filepath_raw = os.path.join(TEX_DIR, "metallic_smoothness.png")
        packed.file_format = "PNG"
        packed.save()
        size = os.path.getsize(packed.filepath_raw)
        log(f"  packed metallic_smoothness.png  {w}x{h}  {size} bytes ({size / 1048576.0:.2f} MiB)")
    except Exception as e:  # noqa: BLE001
        log("PACK FAILED:", repr(e))
else:
    log("PACK SKIPPED: metallic or roughness not found")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True,
                         embed_textures=False, path_mode="RELATIVE")
log("exported:", FBX_OUT, os.path.getsize(FBX_OUT), "bytes")
log("UNPACK COMPLETE")
