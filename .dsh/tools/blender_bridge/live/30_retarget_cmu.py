"""Live-Blender retarget preview: CMU clip -> dwarf_hero, three ways to compose rotations.

Run inside the user's Blender through the bridge:

    SRC = r"...\\62_07.fbx"; DST = r"...\\dwarf_hero.fbx"; MODE = "D"; FRAME = 100
    exec(open(r"F:\\WORK\\Unity_Dwarf\\Dwarf\\.dsh\\tools\\blender_bridge\\live\\30_retarget_cmu.py").read())

MODE:
  A — parent-relative delta conjugated by the target's WORLD rest orientation
  B — parent-relative delta conjugated by the target's PARENT-RELATIVE rest
  D — world delta applied as an absolute orientation (positions come from the parent chain)
"""
import bpy
from mathutils import Vector

PAIRS = [
    ("hip", "Hips"), ("abdomen", "Spine1"), ("chest", "Spine2"), ("neck", "Neck"),
    ("head", "Head"), ("rCollar", "RightShoulder"), ("rShldr", "RightArm"),
    ("rForeArm", "RightForeArm"), ("rHand", "RightHand"), ("lCollar", "LeftShoulder"),
    ("lShldr", "LeftArm"), ("lForeArm", "LeftForeArm"), ("lHand", "LeftHand"),
    ("rThigh", "RightUpLeg"), ("rShin", "RightLeg"), ("rFoot", "RightFoot"),
    ("lThigh", "LeftUpLeg"), ("lShin", "LeftLeg"), ("lFoot", "LeftFoot"),
]


def log(*a):
    print("[LIVE]", *a, flush=True)


def clear(prefixes=("CMU_", "DWARF_")):
    for o in list(bpy.data.objects):
        if o.name.startswith(prefixes):
            bpy.data.objects.remove(o, do_unlink=True)


def import_fbx(path, prefix):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in new if o.type == "ARMATURE")
    arm.name = prefix + "_ARM"
    for o in new:
        if o is not arm and o.type == "MESH":
            o.name = prefix + "_MESH"
    return arm, new


def build(src_path, dst_path, mode, action_name):
    clear()
    src, _ = import_fbx(src_path, "CMU")
    dst, dst_objs = import_fbx(dst_path, "DWARF")
    bpy.context.view_layer.update()

    name = {}
    for b in dst.data.bones:
        name.setdefault(b.name.split(":")[-1], b.name)
    pairs = [(s, name[t]) for s, t in PAIRS if s in src.data.bones and t in name]
    if len(pairs) < 10:
        raise RuntimeError("too few mapped bones: %d" % len(pairs))

    s_rest = {s: src.matrix_world @ src.data.bones[s].matrix_local for s, _ in pairs}
    t_rest = {t: dst.matrix_world @ dst.data.bones[t].matrix_local for _, t in pairs}
    s_rel, t_rel = {}, {}
    for s, t in pairs:
        sb, tb = src.data.bones[s], dst.data.bones[t]
        s_rel[s] = sb.parent.matrix_local.inverted() @ sb.matrix_local if sb.parent else sb.matrix_local
        t_rel[t] = tb.parent.matrix_local.inverted() @ tb.matrix_local if tb.parent else tb.matrix_local

    for pb in dst.pose.bones:
        pb.rotation_mode = "QUATERNION"

    depth = {}
    for b in dst.data.bones:
        d, p = 0, b.parent
        while p is not None:
            d += 1
            p = p.parent
        depth[b.name] = d
    levels = {}
    for t in t_rest:
        levels.setdefault(depth[t], []).append(t)

    f0, f1 = (int(round(v)) for v in src.animation_data.action.frame_range)
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start, scene.frame_end = f0, f1

    dst.animation_data_create()
    act = bpy.data.actions.new(action_name)
    dst.animation_data.action = act

    src_of = {t: s for s, t in pairs}

    # G needs a reference pose: CMU clips open on a T-pose, and taking it as the neutral
    # removes the constant difference between the two rest poses (T-pose vs A-pose, bone roll)
    ref = {}
    if mode == "G":
        scene.frame_set(f0)
        for t in t_rest:
            s = src_of[t]
            sb = src.data.bones[s]
            s_pose = src.pose.bones[s].matrix
            s_pose_rel = (src.pose.bones[sb.parent.name].matrix.inverted() @ s_pose
                          if sb.parent else s_pose)
            delta = s_pose_rel.to_quaternion() @ s_rel[s].to_quaternion().inverted()
            rest_q = t_rest[t].to_quaternion()
            ref[t] = rest_q.inverted() @ delta @ rest_q

    for f in range(f0, f1 + 1):
        scene.frame_set(f)
        if mode in ("D", "F"):
            for level in sorted(levels):
                for t in levels[level]:
                    s = src_of[t]
                    s_world = src.matrix_world @ src.pose.bones[s].matrix
                    if mode == "F":
                        # aim the target bone straight at the source bone's direction:
                        # shortest arc from its own rest direction, so limbs cannot end up
                        # mirrored or rotated onto the wrong side
                        s_dir = (s_world.to_3x3() @ Vector((0, 1, 0))).normalized()
                        t_dir = (t_rest[t].to_3x3() @ Vector((0, 1, 0))).normalized()
                        q = t_dir.rotation_difference(s_dir) @ t_rest[t].to_quaternion()
                    else:
                        delta = s_world.to_quaternion() @ s_rest[s].to_quaternion().inverted()
                        q = delta @ t_rest[t].to_quaternion()
                    pb = dst.pose.bones[t]
                    m = q.to_matrix().to_4x4()
                    m.translation = (dst.matrix_world @ pb.matrix).translation
                    pb.matrix = dst.matrix_world.inverted() @ m
                bpy.context.view_layer.update()
            for t in t_rest:
                dst.pose.bones[t].keyframe_insert("rotation_quaternion", frame=f, group=t)
            continue

        for t in t_rest:
            s = src_of[t]
            sb = src.data.bones[s]
            s_pose = src.pose.bones[s].matrix
            s_pose_rel = (src.pose.bones[sb.parent.name].matrix.inverted() @ s_pose
                          if sb.parent else s_pose)
            delta = s_pose_rel.to_quaternion() @ s_rel[s].to_quaternion().inverted()
            rest_q = (t_rel[t] if mode == "B" else t_rest[t]).to_quaternion()
            basis = rest_q.inverted() @ delta @ rest_q
            if mode == "G":
                basis = basis @ ref[t].inverted()
            pb = dst.pose.bones[t]
            pb.rotation_quaternion = basis
            pb.location = (0.0, 0.0, 0.0)
            pb.keyframe_insert("rotation_quaternion", frame=f, group=t)

    act.name = action_name
    log("mode %s: %d bones, %d..%d frames, action %r" % (mode, len(pairs), f0, f1, act.name))
    return dst, act, (f0, f1)


MODE = globals().get("MODE", "D")
FRAME = globals().get("FRAME", 100)
SRC = globals().get("SRC", r"F:\WORK\Unity_Dwarf\Dwarf\Resources\models\anims\Anims_Only_FBX_V1\FBX\62_07.fbx")
DST = globals().get("DST", r"F:\WORK\Unity_Dwarf\Dwarf\Assets\Models\Characters\DwarfHero\dwarf_hero.fbx")

dwarf, action, (first, last) = build(SRC, DST, MODE, "CMU_62_07_" + MODE)
bpy.context.scene.frame_set(min(max(FRAME, first), last))
log("done at frame", bpy.context.scene.frame_current)
