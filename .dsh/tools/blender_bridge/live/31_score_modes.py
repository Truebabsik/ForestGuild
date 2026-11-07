"""Score retarget modes by comparing arm/leg directions in torso space.

Runs inside live Blender via the bridge; reuses live/30_retarget_cmu.py.
"""
import importlib.util

import bpy
from mathutils import Vector

spec = importlib.util.spec_from_file_location(
    "cmu_live", r"F:\WORK\Unity_Dwarf\Dwarf\.dsh\tools\blender_bridge\live\30_retarget_cmu.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

SRC = r"F:\WORK\Unity_Dwarf\Dwarf\Resources\models\anims\Anims_Only_FBX_V1\FBX\62_07.fbx"
DST = r"F:\WORK\Unity_Dwarf\Dwarf\Assets\Models\Characters\DwarfHero\dwarf_hero.fbx"
FRAMES = (60, 100, 140)


def local_vec(arm, frame_bone, tip_bone):
    m = arm.matrix_world @ arm.pose.bones[frame_bone].matrix
    p = m.translation
    o = (arm.matrix_world @ arm.pose.bones[tip_bone].matrix).translation
    return (m.to_quaternion().inverted() @ (o - p)).normalized()


for mode in ("A", "B", "D", "F", "G"):
    dwarf, act, (f0, f1) = mod.build(SRC, DST, mode, "SCORE_" + mode)
    src = next(o for o in bpy.data.objects if o.name.startswith("CMU_"))
    tname = {b.name.split(":")[-1]: b.name for b in dwarf.data.bones}
    out = []
    for f in FRAMES:
        bpy.context.scene.frame_set(f)
        s_arm = local_vec(src, "chest", "rHand")
        t_arm = local_vec(dwarf, tname["Spine2"], tname["RightHand"])
        ang = s_arm.angle(t_arm, 0.0) * 57.2958
        s_leg = local_vec(src, "hip", "rFoot")
        t_leg = local_vec(dwarf, tname["Hips"], tname["RightFoot"])
        ang_leg = s_leg.angle(t_leg, 0.0) * 57.2958
        out.append("f%d arm %.0f leg %.0f" % (f, ang, ang_leg))
    print("[SCORE] mode %s: %s" % (mode, " | ".join(out)), flush=True)
