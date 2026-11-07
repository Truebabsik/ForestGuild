"""Оптимизация модели под Unity: децимация + масштаб + текстуры + экспорт без вшитых карт.

Запуск:
    blender.exe --background --factory-startup --python optimize_model.py -- \
        --src <input.fbx> --out <out_dir> [--height 1.3] [--tex 1024] [--max-tris 4000]

Результат в <out_dir>:
    <имя>.fbx                 — без вшитых текстур
    Textures/base_color.jpg   — из PNG, альфа не нужна
    Textures/normal.png
    Textures/metallic_smoothness.png   — R = metallic, A = 1 - roughness

Подпапка Textures выбрана намеренно: Unity ищет внешние текстуры FBX именно там.

Децимация нужна для пропов от 3D-генераторов: они приходят с 300k-500k треугольников,
что в сотни раз больше игровой нормы. Для hard-surface (наковальня, механизмы, архитектура)
сначала идёт planar dissolve — он схлопывает копланарные грани, сохраняя острые рёбра и
силуэт, и только если этого не хватило, добираем collapse.
"""
import bpy
import bmesh  # noqa: F401  (нужен для побочных эффектов импорта в некоторых сборках)
import math
import os
import shutil
import sys

# --- аргументы после "--" -----------------------------------------------------
argv = sys.argv
args = argv[argv.index("--") + 1:] if "--" in argv else []


def arg(name, default=None):
    if name in args:
        return args[args.index(name) + 1]
    return default


SRC = arg("--src")
OUT_DIR = arg("--out")
TARGET_H = float(arg("--height", "0") or 0)        # 0 = не менять масштаб
TEX_SIZE = int(arg("--tex", "1024"))
MAX_TRIS = int(arg("--max-tris", "0"))             # 0 = не децимировать
DISSOLVE_ANGLE = float(arg("--dissolve-angle", "5"))
UP_AXIS = (arg("--up-axis", "z") or "z").lower()   # какая ось Blender станет «вверх» в Unity
KEEP_NORMALS = (arg("--keep-normals", "0") or "0").lower() not in ("0", "false", "")

if not SRC or not OUT_DIR:
    raise SystemExit("usage: --src <input.fbx> --out <out_dir> [--height 1.3] [--tex 1024] [--max-tris 4000]")

TEX_DIR = os.path.join(OUT_DIR, "Textures")
FBX_OUT = os.path.join(OUT_DIR, os.path.splitext(os.path.basename(SRC))[0] + ".fbx")


def log(*a):
    print("[OPT]", *a, flush=True)


def save_image(img, path, fmt):
    """Сохранить картинку и ДОКАЗАТЬ, что файл появился на диске.

    WHY не `img.filepath_raw = p` + `img.save()`: в Blender 5.2 headless этот
    маршрут возвращает управление, НИЧЕГО не записав и НИЧЕГО не выбросив.
    Скрипт печатает «saved», а папка Textures остаётся пустой — молчаливый
    провал, который обнаруживается только в Unity по розовым моделям.
    Замерено зондом probe_save_api.py: пишет только явный аргумент.

        img.filepath_raw = p; img.save()   -> exists: False
        img.save(filepath=p)               -> exists: True
        img.filepath = p;     img.save()   -> exists: False
    """
    img.filepath_raw = path
    img.file_format = fmt
    img.save(filepath=path)
    if not os.path.exists(path) or os.path.getsize(path) == 0:
        raise SystemExit(
            f"FATAL: {path} was not written (Image.save silently no-opped)")
    log(f"saved {os.path.basename(path)} ({os.path.getsize(path)} bytes)")


def count_tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def world_dims(obj):
    """Габариты в МИРОВЫХ осях.

    WHY не obj.dimensions: это размер локального bbox, он НЕ учитывает поворот объекта.
    FBX-импортёр Blender часто компенсирует конвертацию осей поворотом на 90 градусов,
    и тогда локальный Z — это вовсе не высота. Мировой bbox считается через matrix_world
    и всегда даёт истинную ориентацию в сцене, где Z — вверх.
    """
    from mathutils import Vector

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    xs = [c.x for c in corners]
    ys = [c.y for c in corners]
    zs = [c.z for c in corners]
    return (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


# --- чистим прошлый прогон целиком, чтобы не осталось дублей и папки .fbm ----
if os.path.isdir(OUT_DIR):
    shutil.rmtree(OUT_DIR)
    log("cleaned previous output:", OUT_DIR)
os.makedirs(TEX_DIR, exist_ok=True)

log("blender:", bpy.app.version_string)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
log("source:", SRC, os.path.getsize(SRC), "bytes")

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
if not meshes:
    raise SystemExit("no mesh found in " + SRC)
mesh_obj = meshes[0]
arm_obj = arms[0] if arms else None
log("mesh:", mesh_obj.name, "armature:", arm_obj.name if arm_obj else "NONE")

# --- 1. децимация -------------------------------------------------------------
# WHY порядок именно такой: децимация ДО масштаба, потому что она не зависит от размера,
# а лишние вершины на 500k-сетке замедляют каждую последующую операцию.
if MAX_TRIS > 0:
    t0 = count_tris(mesh_obj)
    log("tris before   :", t0)

    if t0 > MAX_TRIS:
        activate(mesh_obj)

        # Кастомные нормали от генератора мешают dissolve и дают артефакты после — убираем.
        # НО: у моделей с корректно запечённой картой нормалей они согласованы с ней.
        # Стирание ломает это согласие, и затенение «разъезжается» тёмными пятнами.
        # Проверено на горне: с затиранием — брак, без — чисто. См. --keep-normals.
        if not KEEP_NORMALS:
            try:
                bpy.ops.mesh.customdata_custom_splitnormals_clear()
                log("cleared custom split normals")
            except Exception as e:  # noqa: BLE001
                log("split normals clear skipped:", repr(e))
        else:
            log("custom split normals KEPT (--keep-normals)")

        mod = mesh_obj.modifiers.new("PlanarDissolve", "DECIMATE")
        mod.decimate_type = "DISSOLVE"
        mod.angle_limit = math.radians(DISSOLVE_ANGLE)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        t1 = count_tris(mesh_obj)
        log(f"after dissolve: {t1}  (angle={DISSOLVE_ANGLE} deg)")

        if t1 > MAX_TRIS:
            mod = mesh_obj.modifiers.new("Collapse", "DECIMATE")
            mod.decimate_type = "COLLAPSE"
            mod.ratio = MAX_TRIS / float(t1)
            bpy.ops.object.modifier_apply(modifier=mod.name)
            log("after collapse:", count_tris(mesh_obj))

        tri = mesh_obj.modifiers.new("Triangulate", "TRIANGULATE")
        bpy.ops.object.modifier_apply(modifier=tri.name)

        # Сглаживание по углу: плоские грани остаются плоскими, скругление рога — гладким.
        # При --keep-normals шейдинг не трогаем: он уже согласован с картой нормалей.
        if not KEEP_NORMALS:
            try:
                bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
                log("shading: auto smooth 35 deg")
            except Exception as e:  # noqa: BLE001
                log("auto smooth unavailable, flat/smooth fallback:", repr(e))
                try:
                    bpy.ops.object.shade_smooth()
                    log("shading: plain smooth")
                except Exception:  # noqa: BLE001
                    pass
        else:
            log("shading: left untouched (--keep-normals)")

    log("tris final    :", count_tris(mesh_obj))
else:
    log("tris          :", count_tris(mesh_obj), "(decimation off)")

# --- 2. масштаб ---------------------------------------------------------------
# WHY мировой bbox, а не dimensions: см. комментарий world_dims(). Локальный Z врёт,
# когда импортёр повесил на объект поворот при конвертации осей.
if TARGET_H > 0:
    lw = mesh_obj.dimensions
    ww = world_dims(mesh_obj)
    log(f"local dims    : X={lw.x:.4f} Y={lw.y:.4f} Z={lw.z:.4f}")
    log(f"world dims    : X={ww[0]:.4f} Y={ww[1]:.4f} Z={ww[2]:.4f}  (Z = вверх)")
    h0 = ww[2] if UP_AXIS == "z" else ww[1]
    factor = TARGET_H / h0
    log(f"height target : {TARGET_H} m  -> factor {factor:.6f}")
    bpy.ops.object.select_all(action="SELECT")
    if arm_obj:
        bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.transform.resize(value=(factor, factor, factor), orient_type="GLOBAL")
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.context.view_layer.update()
    ww2 = world_dims(mesh_obj)
    log(f"size after    : X={ww2[0]:.4f} Y={ww2[1]:.4f} Z={ww2[2]:.4f} m")
log("bones        :", len(arm_obj.data.bones) if arm_obj else "n/a")

# --- 3. текстуры --------------------------------------------------------------
imgs = [i for i in bpy.data.images if i.size[0] > 0]
log("textures in  :", [(i.name, f"{i.size[0]}x{i.size[1]}") for i in imgs])
for i in imgs:
    if i.size[0] != TEX_SIZE or i.size[1] != TEX_SIZE:
        i.scale(TEX_SIZE, TEX_SIZE)
log("textures out :", [(i.name, f"{i.size[0]}x{i.size[1]}") for i in imgs])


def find(*keys):
    for i in imgs:
        n = i.name.lower()
        if all(k in n for k in keys):
            return i
    return None


def find_base():
    for i in imgs:
        n = i.name.lower()
        if not any(k in n for k in ("normal", "roughness", "metallic")):
            return i
    return None


base, nrm = find_base(), find("normal")
rgh, met = find("roughness"), find("metallic")
log("identified   : base=%s normal=%s rough=%s metal=%s",
    base and base.name, nrm and nrm.name, rgh and rgh.name, met and met.name)

if base:
    save_image(base, os.path.join(TEX_DIR, "base_color.jpg"), "JPEG")

if nrm:
    save_image(nrm, os.path.join(TEX_DIR, "normal.png"), "PNG")

# --- 4. metallic + roughness -> одна текстура --------------------------------
# URP Lit ждёт metallic в R и smoothness в A — потому именно такая раскладка.
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
        save_image(packed, os.path.join(TEX_DIR, "metallic_smoothness.png"), "PNG")
        log("  packed as R=metallic, A=smoothness")
    except Exception as e:  # noqa: BLE001 - сообщаем и продолжаем
        log("PACK FAILED:", repr(e))
else:
    log("PACK SKIPPED: metallic or roughness not found")

# --- 5. экспорт без вшитых текстур -------------------------------------------
# RELATIVE, а не COPY: COPY создаёт дублирующую папку <имя>.fbm.
bpy.ops.object.select_all(action="SELECT")
try:
    bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True,
                             embed_textures=False, path_mode="RELATIVE")
    log("exported (embed_textures=False, path_mode=RELATIVE)")
except TypeError:
    bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True, embed_textures=False)
    log("exported (embed_textures=False)")

# --- 6. итог ------------------------------------------------------------------
log("--- result sizes ---")
total = 0
for root, _dirs, files in os.walk(OUT_DIR):
    for f in sorted(files):
        p = os.path.join(root, f)
        total += os.path.getsize(p)
        log(f"  {os.path.relpath(p, OUT_DIR):<42s} {os.path.getsize(p):>10d} bytes")
src_size = os.path.getsize(SRC)
log(f"TOTAL: {total} bytes ({total/1048576.0:.2f} MiB) vs source {src_size/1048576.0:.2f} MiB"
    f"  -> x{src_size/max(total,1):.1f} smaller")
log("OPTIMIZE COMPLETE")
