"""Concept test: give the dwarf the CMU skeleton instead of retargeting each clip.

Pipeline (all inside the live Blender, on COPIES — the working rig is untouched):
  1. a fresh copy of the mesh is re-posed into the CMU rest pose (T-pose) with the Mixamo rig;
  2. the deformation is baked into the copy (we are not re-skinning, weights just get renamed);
  3. a CMU-named armature is built on the dwarf's joint positions, with CMU bone rolls;
  4. vertex groups are renamed/merged (Mixamo -> CMU), the copy is bound to the new armature;
  5. a clip's rotations are copied 1:1 (same rest orientation, so no maths), locations dropped.

Objects it creates: CMUDW_MESH, CMUDW_RIG (+ a fresh CMUSRC armature imported for the clip).
"""
import math
import os
import time

import bpy
from mathutils import Matrix, Vector

CMU_FBX = r"F:\WORK\Unity_Dwarf\Dwarf\Resources\models\anims\Anims_Only_FBX_V1\FBX\62_07.fbx"
DWARF_ARM = "DWARF_ARM"
DWARF_MESH = "DWARF_MESH"

# CMU bone -> Mixamo bone (used both for joint positions and for vertex-group renames)
MAP = {
    "hip": "Hips", "abdomen": "Spine1", "chest": "Spine2", "neck": "Neck", "head": "Head",
    "rCollar": "RightShoulder", "rShldr": "RightArm", "rForeArm": "RightForeArm", "rHand": "RightHand",
    "lCollar": "LeftShoulder", "lShldr": "LeftArm", "lForeArm": "LeftForeArm", "lHand": "LeftHand",
    "rThigh": "RightUpLeg", "rShin": "RightLeg", "rFoot": "RightFoot",
    "lThigh": "LeftUpLeg", "lShin": "LeftLeg", "lFoot": "LeftFoot",
}

PARENT = {
    "hip": None, "abdomen": "hip", "chest": "abdomen", "neck": "chest", "head": "neck",
    "rCollar": "chest", "rShldr": "rCollar", "rForeArm": "rShldr", "rHand": "rForeArm",
    "lCollar": "chest", "lShldr": "lCollar", "lForeArm": "lShldr", "lHand": "lForeArm",
    "rThigh": "hip", "rShin": "rThigh", "rFoot": "rShin",
    "lThigh": "hip", "lShin": "lThigh", "lFoot": "lShin",
}

# mixer groups that have no CMU counterpart: fingers/toes/spine split
MERGE = {
    "Hips": "hip", "Spine": "abdomen", "Spine1": "abdomen", "Spine2": "chest",
    "Neck": "neck", "Head": "head", "HeadTop_End": "head",
    "LeftShoulder": "lCollar", "LeftArm": "lShldr", "LeftForeArm": "lForeArm",
    "LeftHand": "lHand", "LeftUpLeg": "lThigh", "LeftLeg": "lShin", "LeftFoot": "lFoot",
    "LeftToeBase": "lFoot", "LeftToe_End": "lFoot",
    "RightShoulder": "rCollar", "RightArm": "rShldr", "RightForeArm": "rForeArm",
    "RightHand": "rHand", "RightUpLeg": "rThigh", "RightLeg": "rShin", "RightFoot": "rFoot",
    "RightToeBase": "rFoot", "RightToe_End": "rFoot",
}

# anything left over falls back by prefix: fingers -> hand, toes -> foot, eyes -> head
FALLBACK = (("LeftHand", "lHand"), ("RightHand", "rHand"),
            ("LeftToe", "lFoot"), ("RightToe", "rFoot"),
            ("LeftEye", "head"), ("RightEye", "head"), ("Eye", "head"))


def log(*a):
    print("[CONCEPT]", *a, flush=True)


def short(name):
    return name.split(":")[-1]


def drop_objects(prefixes):
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
        if o.type == "MESH":
            o.name = prefix + "_MESH"
    return arm, new


drop_objects(("CMUDW_", "CMUSRC_"))

src, _ = import_fbx(CMU_FBX, "CMUSRC")
dst = bpy.data.objects[DWARF_ARM]
mix = {short(b.name): b.name for b in dst.data.bones}
mesh = bpy.data.objects[DWARF_MESH]

# --- 0. clean pose of the dwarf rig
for pb in dst.pose.bones:
    pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()
if dst.animation_data:
    dst.animation_data.action = None

# --- 1. copy the mesh, re-pose it into the CMU rest pose with the Mixamo rig
tpose_mesh = mesh.copy()
tpose_mesh.data = mesh.data.copy()
tpose_mesh.name = "CMUDW_MESH"
tpose_mesh.parent = None
bpy.context.scene.collection.objects.link(tpose_mesh)
bpy.context.view_layer.objects.active = dst

order = []


def walk(bone):
    order.append(bone.name)
    for c in bone.children:
        walk(c)


for b in dst.data.bones:
    if b.parent is None:
        walk(b)


def world_rest(src_arm, bone_name):
    b = src_arm.data.bones[bone_name]
    return src_arm.matrix_world @ b.matrix_local


def apply_world_rotation(pb, arm, target_matrix):
    m = target_matrix.to_quaternion().to_matrix().to_4x4()
    m.translation = (arm.matrix_world @ pb.matrix).translation
    pb.matrix = arm.matrix_world.inverted() @ m


# CMU bones to mimic: the mapped ones; others are left alone
for tname in order:
    sname = None
    for c, m in MAP.items():
        if mix[m] == tname:
            sname = c
            break
    if sname is None:
        continue
    apply_world_rotation(dst.pose.bones[tname], dst, world_rest(src, sname))
    bpy.context.view_layer.update()
log("dwarf posed into the CMU rest pose")

# --- 2. bake the deformation into the copy
bpy.ops.object.select_all(action='DESELECT')
tpose_mesh.select_set(True)
bpy.context.view_layer.objects.active = tpose_mesh
for mod in list(tpose_mesh.modifiers):
    if mod.type == "ARMATURE":
        bpy.ops.object.modifier_apply(modifier=mod.name)
log("T-pose baked into CMUDW_MESH: %d verts" % len(tpose_mesh.data.vertices))

# --- 3. CMU armature on the dwarf's joints, with CMU bone rolls
joints = {}
for cname, mname in MAP.items():
    pb = dst.pose.bones[mix[mname]]
    head = (dst.matrix_world @ pb.matrix).translation.copy()
    tail = head + (dst.matrix_world @ pb.matrix).to_3x3() @ Vector((0, pb.bone.length, 0))
    joints[cname] = (head, tail)

arm_data = bpy.data.armatures.new("CMUDW_RIG")
new_arm = bpy.data.objects.new("CMUDW_RIG", arm_data)
bpy.context.scene.collection.objects.link(new_arm)
bpy.context.view_layer.objects.active = new_arm
bpy.ops.object.mode_set(mode='EDIT')

for cname in MAP:
    eb = arm_data.edit_bones.new(cname)
    head, tail = joints[cname]
    eb.head = head
    eb.tail = tail
for cname, parent in PARENT.items():
    if parent and cname in arm_data.edit_bones and parent in arm_data.edit_bones:
        arm_data.edit_bones[cname].parent = arm_data.edit_bones[parent]
        arm_data.edit_bones[cname].use_connect = False
# rolls: take the CMU rest Z axis so clip rotations land in the right plane
for cname in MAP:
    zb = (src.matrix_world @ src.data.bones[cname].matrix_local).to_3x3().col[2]
    arm_data.edit_bones[cname].align_roll(Vector(zb))
bpy.ops.object.mode_set(mode='OBJECT')
log("CMUDW_RIG: %d bones" % len(arm_data.bones))

# restore the dwarf rig
for pb in dst.pose.bones:
    pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()

# --- 4. vertex groups: Mixamo -> CMU (merge the leftovers, then normalise)
groups = {g.index: short(g.name) for g in tpose_mesh.vertex_groups}
weights = {}
for v in tpose_mesh.data.vertices:
    acc = {}
    for g in v.groups:
        name = groups.get(g.group)
        if not name:
            continue
        target = MERGE.get(name)
        if target is None and name in MAP:
            target = name
        if target is None:
            for prefix, fallback in FALLBACK:
                if name.startswith(prefix):
                    target = fallback
                    break
        if target is None:
            continue
        acc[target] = acc.get(target, 0.0) + g.weight
    total = sum(acc.values())
    if total > 0:
        weights[v.index] = {k: w / total for k, w in acc.items()}

# anything still without weight (eyes, teeth) would stay behind while the body moves
orphans = [v.index for v in tpose_mesh.data.vertices if v.index not in weights]
for vidx in orphans:
    z = tpose_mesh.data.vertices[vidx].co.z
    weights[vidx] = {"head": 1.0} if z > 1.0 else {"hip": 1.0}
log("orphan verts parked: %d" % len(orphans))

for g in list(tpose_mesh.vertex_groups):
    tpose_mesh.vertex_groups.remove(g)
new_groups = {c: tpose_mesh.vertex_groups.new(name=c) for c in MAP}
for vidx, ws in weights.items():
    for cname, w in ws.items():
        new_groups[cname].add([vidx], w, 'REPLACE')
log("vertex groups rebuilt: %d verts weighted, %d groups" % (len(weights), len(new_groups)))

tpose_mesh.parent = new_arm
tpose_mesh.matrix_parent_inverse = new_arm.matrix_world.inverted()
mod = tpose_mesh.modifiers.new("Armature", 'ARMATURE')
mod.object = new_arm

# --- 5. clip: copy rotations 1:1 (rest orientations match), drop locations
act_src = src.animation_data.action
f0, f1 = (int(round(v)) for v in act_src.frame_range)
scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start, scene.frame_end = f0, f1

new_arm.animation_data_create()
act = bpy.data.actions.new("CMU_62_07_on_cmu_rig")
new_arm.animation_data.action = act
for pb in new_arm.pose.bones:
    pb.rotation_mode = "QUATERNION"

t0 = time.time()
for f in range(f0, f1 + 1):
    scene.frame_set(f)
    for cname in MAP:
        spb = src.pose.bones.get(cname)
        npb = new_arm.pose.bones.get(cname)
        if spb is None or npb is None:
            continue
        npb.rotation_quaternion = spb.rotation_quaternion.copy()
    for cname in MAP:
        npb = new_arm.pose.bones.get(cname)
        if npb:
            npb.keyframe_insert("rotation_quaternion", frame=f, group=cname)
log("clip copied in %.1fs: %s frames %d..%d" % (time.time() - t0, act.name, f0, f1))

bpy.context.scene.frame_set(100)
print("[CONCEPT] done; objects:", [o.name for o in bpy.data.objects], flush=True)
