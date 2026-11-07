"""Measure where the bytes are in an FBX, before optimising anything.

    blender.exe --background --factory-startup --python measure_model.py -- --src <file.fbx>

Prints file size, geometry counts, world dimensions, and per-texture resolution
and packed size. Skill rule #1: never cut blind. On generated props the textures
usually hold most of the weight, but that has to be confirmed per file rather
than assumed.
"""
import bpy
import os
import sys

from mathutils import Vector

argv = sys.argv
args = argv[argv.index("--") + 1:] if "--" in argv else []


def arg(name, default=None):
    return args[args.index(name) + 1] if name in args else default


SRC = arg("--src")
if not SRC:
    raise SystemExit("usage: --src <file.fbx>")


def log(*a):
    print("[MEASURE]", *a, flush=True)


def count_tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def world_dims(obj):
    """True world-space extents; obj.dimensions ignores rotation (see the skill)."""
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return tuple(max(c[i] for c in corners) - min(c[i] for c in corners) for i in range(3))


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)

src_size = os.path.getsize(SRC)
log("file        :", SRC)
log("file size   :", src_size, f"bytes ({src_size / 1048576.0:.2f} MiB)")

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
log("objects     :", len(bpy.data.objects), "| meshes", len(meshes), "| armatures", len(arms))

total_tris = 0
total_verts = 0
for m in meshes:
    t = count_tris(m)
    total_tris += t
    total_verts += len(m.data.vertices)
    d = world_dims(m)
    log(f"  mesh {m.name!r}: tris={t} verts={len(m.data.vertices)} "
        f"world dims X={d[0]:.4f} Y={d[1]:.4f} Z={d[2]:.4f} (Z up)")
    log(f"    materials: {[s.material.name if s.material else None for s in m.material_slots]}")
log("tris total  :", total_tris)
log("verts total :", total_verts)

log("materials   :", [m.name for m in bpy.data.materials])

packed_total = 0
log("textures:")
for img in bpy.data.images:
    if img.size[0] == 0:
        continue
    psize = img.packed_file.size if img.packed_file else 0
    packed_total += psize
    log(f"  {img.name!r} {img.size[0]}x{img.size[1]} packed={psize} bytes "
        f"({psize / 1048576.0:.2f} MiB) source={img.filepath or '(generated)'}")

log(f"packed total: {packed_total} bytes ({packed_total / 1048576.0:.2f} MiB)")
if src_size:
    log(f"textures are {100.0 * packed_total / src_size:.1f}% of the file")
    log(f"geometry+rest is {100.0 * (src_size - packed_total) / src_size:.1f}%")
log("MEASURE COMPLETE")
