"""
DREAM RUSH — CHR_AppleChild v2 ("Pip") built from scratch (user: "Model ก็กาก ทำใหม่").
Target read: Astro Bot / Mario chibi — big glossy apple head-body, huge expressive eyes, open grin,
blush, short noodle limbs with oversized white mitts and chunky lavender sneakers (the goofy run sells on them).

Contract kept with the game code (AppleProceduralAnimator / AppleFace / AppleCharacterSetup):
  CHR_AppleChild (empty) -> FLAT children Body, Leaf, Arm_L, Arm_R, Leg_L, Leg_R
  Body contains the face; eye slots use M_Apple_EyeWhite / M_Apple_Pupil / M_Apple_PupilShine (AppleFace swaps them)
  Height 1.2 m, feet at z=0, front = -Y (Unity +Z), Blender -X = character's RIGHT.
Outputs: Assets/DreamRush/Art/Characters/AppleChild/CHR_AppleChild.fbx + T_AppleChild_Skin.png,
         ArtSource/Blender/Characters/CHR_AppleChild_Game_v2.blend
run: python3 -I build_apple_v2.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa
import bmesh  # noqa
from mathutils import Vector, Matrix, Euler  # noqa
import numpy as np  # noqa

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Characters', 'AppleChild')
BLEND = os.path.join(ROOT, 'ArtSource', 'Blender', 'Characters')
os.makedirs(OUT, exist_ok=True); os.makedirs(BLEND, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

COL = {  # sRGB-ish preview colours (Unity owns the final look)
    'M_Apple_Skin': (0.85, 0.12, 0.16), 'M_Apple_Limb': (0.95, 0.78, 0.6), 'M_Apple_Glove': (0.97, 0.95, 0.9),
    'M_Apple_EyeWhite': (0.97, 0.96, 0.92), 'M_Apple_Pupil': (0.06, 0.05, 0.09), 'M_Apple_PupilShine': (1, 1, 1),
    'M_Apple_Ink': (0.3, 0.03, 0.07), 'M_Apple_Leaf': (0.4, 0.72, 0.35), 'M_Apple_Stem': (0.5, 0.3, 0.16),
    'M_Apple_Cheek': (1.0, 0.55, 0.6), 'M_Apple_Tongue': (1.0, 0.45, 0.5), 'M_Apple_Shoe': (0.62, 0.52, 0.95),
    'M_Apple_Sole': (0.98, 0.96, 0.92),
}


def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        c = COL.get(name, (0.5, 0.5, 0.5))
        m.diffuse_color = (c[0], c[1], c[2], 1)
    return m


def obj_from_bm(name, bm, material):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); scene.collection.objects.link(o)
    o.data.materials.append(mat(material))
    return o


def sphere(name, r, center, material, scale=(1, 1, 1), segs=24, rings=14, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=r, calc_uvs=True)
    o = obj_from_bm(name, bm, material)
    o.scale = scale; o.rotation_euler = rot; o.location = center
    return o


def tube(name, pts, radii, material, segs=12):
    """swept circle along a polyline (limbs, stem) with per-point radius, capped."""
    bm = bmesh.new(); rings = []
    for i, p in enumerate(pts):
        p = Vector(p)
        d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a).normalized()
        ring = [bm.verts.new(p + (a * math.cos(t) + b * math.sin(t)) * radii[i]) for t in [k / segs * 2 * math.pi for k in range(segs)]]
        rings.append(ring)
    for i in range(len(rings) - 1):
        for k in range(segs):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % segs], rings[i + 1][(k + 1) % segs], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return obj_from_bm(name, bm, material)


def apply_all(o):
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def join(objs, name):
    for o in objs: apply_all(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name; o.data.name = 'MSH_' + name
    return o


def smooth(o):
    for p in o.data.polygons: p.use_smooth = True


# ============================================================ BODY (apple)
BODY_C = Vector((0, 0, 0.64))
R = 0.43


def apple_radius(d):
    """direction -> radius: slightly squat, wider shoulders, top dimple, tapered bottom, two soft lobes."""
    x, y, z = d
    r = R * (1.0 + 0.06 * max(z, 0) - 0.08 * max(-z, 0) ** 2)
    phi = math.atan2(y, x)
    r *= 1.0 + 0.025 * math.cos(2 * phi) * max(z, 0)
    return r


bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=48, v_segments=32, radius=1.0, calc_uvs=True)
for v in bm.verts:
    d = v.co.normalized()
    r = apple_radius(d)
    p = d * r
    p.z *= 0.9
    rho2 = d.x * d.x + d.y * d.y
    if d.z > 0: p.z -= 0.13 * math.exp(-rho2 / 0.04)     # stem dimple
    else: p.z += 0.05 * math.exp(-rho2 / 0.03)           # blossom dimple
    v.co = p + BODY_C
body = obj_from_bm('BodySkin', bm, 'M_Apple_Skin')
smooth(body)


def surface(x, z_off, push=0.0):
    """point on the front (−Y) surface at lateral x and height offset from body centre."""
    d = Vector((x, -R, z_off / 0.9)).normalized()   # x/z are ~metres on the face
    # march along d until we hit the deformed surface (numerically, the deform is mild)
    r = apple_radius(d)
    p = d * r; p.z *= 0.9
    n = Vector((p.x, p.y, p.z / 0.81)).normalized()
    return p + BODY_C + n * push, n


face = []
# eyes: big tall ovals, slightly toed-in; pupils large and looking a touch up; 2 shine dots each
for side in (-1, 1):
    x = side * 0.132
    c, n = surface(x, 0.075, -0.02)
    yaw = math.atan2(n.x, -n.y)
    ew = sphere(f'EyeWhite{side}', 1.0, c, 'M_Apple_EyeWhite', scale=(0.097, 0.05, 0.13), rot=(math.radians(-8), 0, yaw))
    pc, _ = surface(x - side * 0.01, 0.065, 0.012)
    pu = sphere(f'Pupil{side}', 1.0, pc, 'M_Apple_Pupil', scale=(0.062, 0.03, 0.084), rot=(math.radians(-8), 0, yaw))
    s1, _ = surface(x + 0.02, 0.105, 0.036)
    s2, _ = surface(x - 0.02, 0.04, 0.034)
    sh1 = sphere(f'Shine{side}a', 1.0, s1, 'M_Apple_PupilShine', scale=(0.024, 0.012, 0.03), segs=12, rings=8, rot=(0, 0, yaw))
    sh2 = sphere(f'Shine{side}b', 1.0, s2, 'M_Apple_PupilShine', scale=(0.012, 0.01, 0.014), segs=10, rings=6, rot=(0, 0, yaw))
    # brow: a small thick arc, raised (eager, not angry)
    pts = []
    for k in range(6):
        t = k / 5
        bx = x + side * (-0.05 + 0.1 * t)
        bz = 0.215 + 0.02 * math.sin(t * math.pi) + 0.012 * t
        p, _ = surface(bx, bz, 0.006)
        pts.append(tuple(p))
    brow = tube(f'Brow{side}', pts, [0.008, 0.014, 0.016, 0.016, 0.014, 0.008], 'M_Apple_Ink', segs=8)
    # blush
    cc, cn = surface(side * 0.235, -0.06, -0.004)
    ck = sphere(f'Cheek{side}', 1.0, cc, 'M_Apple_Cheek', scale=(0.06, 0.02, 0.036), segs=16, rings=8, rot=(0, 0, math.atan2(cn.x, -cn.y)))
    face += [ew, pu, sh1, sh2, brow, ck]

# mouth: open "D" grin (half ellipse, flat top), dark ink with a tongue
bm = bmesh.new()
W, H, DEP = 0.1, 0.07, 0.05
outline = [(-W, 0)] + [(W * math.cos(math.pi + t * math.pi), H * math.sin(math.pi + t * math.pi)) for t in [k / 14 for k in range(1, 14)]] + [(W, 0)]
front = [bm.verts.new((x, -DEP / 2, z)) for x, z in outline]
back = [bm.verts.new((x, DEP / 2, z)) for x, z in outline]
bm.faces.new(front); bm.faces.new(list(reversed(back)))
for i in range(len(outline)):
    j = (i + 1) % len(outline)
    bm.faces.new((front[i], back[i], back[j], front[j]))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
mouth = obj_from_bm('Mouth', bm, 'M_Apple_Ink')
mc, mn = surface(0.0, -0.075, -0.008)
mouth.location = mc; mouth.rotation_euler = (math.radians(-8), 0, 0)
tc, _ = surface(0.0, -0.118, 0.008)
tongue = sphere('Tongue', 1.0, tc, 'M_Apple_Tongue', scale=(0.045, 0.02, 0.024), segs=16, rings=8)
face += [mouth, tongue]

body = join([body] + face, 'Body')
smooth(body)

# ============================================================ LEAF + STEM (pivot at stem base)
top_z = BODY_C.z + R * 1.06 * 0.9 - 0.13
stem = tube('Stem', [(0, 0, top_z - 0.03), (0.005, 0, top_z + 0.06), (0.03, 0, top_z + 0.13), (0.06, 0, top_z + 0.17)],
            [0.026, 0.024, 0.021, 0.018], 'M_Apple_Stem', segs=10)
# leaf: flattened, pointed, gently folded along its spine, sticking out to the character's left (+X) and up
bm = bmesh.new()
L, Wd = 0.2, 0.085
rows = []
for i in range(9):
    t = i / 8
    w = Wd * math.sin(t * math.pi) ** 0.8
    row = []
    for j in range(5):
        s = (j / 4) * 2 - 1
        fold = abs(s) * w * 0.35
        row.append(bm.verts.new((t * L, s * w, fold + 0.04 * math.sin(t * math.pi))))
    rows.append(row)
for i in range(8):
    for j in range(4):
        bm.faces.new((rows[i][j], rows[i + 1][j], rows[i + 1][j + 1], rows[i][j + 1]))
bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
leaf = obj_from_bm('LeafBlade', bm, 'M_Apple_Leaf')
sol = leaf.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.012; sol.offset = 0
bpy.context.view_layer.objects.active = leaf; bpy.ops.object.modifier_apply(modifier='sol')
leaf.location = (0.045, 0, top_z + 0.15); leaf.rotation_euler = (math.radians(10), math.radians(-28), math.radians(-15))
leafobj = join([stem, leaf], 'Leaf'); smooth(leafobj)

# ============================================================ ARMS (pivot at shoulder) — noodle + big mitt
arms = {}
for side, name in ((1, 'Arm_L'), (-1, 'Arm_R')):       # +X = character's LEFT
    sx = side * 0.4; sz = 0.6
    pts = [(sx - side * 0.03, 0, sz), (sx + side * 0.07, -0.01, sz - 0.08), (sx + side * 0.12, -0.02, sz - 0.19), (sx + side * 0.14, -0.03, sz - 0.27)]
    arm = tube(name + '_Tube', pts, [0.034, 0.032, 0.03, 0.03], 'M_Apple_Limb', segs=10)
    hc = Vector((sx + side * 0.155, -0.04, sz - 0.33))
    mitt = sphere(name + '_Mitt', 1.0, hc, 'M_Apple_Glove', scale=(0.085, 0.075, 0.095))
    thumb = sphere(name + '_Thumb', 1.0, hc + Vector((-side * 0.055, -0.045, 0.03)), 'M_Apple_Glove', scale=(0.035, 0.03, 0.045), segs=14, rings=8)
    cuff = tube(name + '_Cuff', [tuple(hc + Vector((-side * 0.01, 0.0, 0.085))), tuple(hc + Vector((-side * 0.012, 0.0, 0.055)))], [0.05, 0.052], 'M_Apple_Glove', segs=14)
    o = join([arm, mitt, thumb, cuff], name); smooth(o)
    arms[name] = (o, Vector((sx - side * 0.03, 0, sz)))

# ============================================================ LEGS (pivot at hip) — short noodle + chunky sneaker
legs = {}
for side, name in ((1, 'Leg_L'), (-1, 'Leg_R')):
    hx = side * 0.135; hz = 0.3
    leg = tube(name + '_Tube', [(hx, 0, hz), (hx + side * 0.01, 0, hz - 0.12), (hx + side * 0.015, 0, 0.11)], [0.036, 0.034, 0.034], 'M_Apple_Limb', segs=10)
    # sneaker: rounded toe box, slightly turned out
    shoe = sphere(name + '_Shoe', 1.0, (hx + side * 0.02, -0.05, 0.075), 'M_Apple_Shoe', scale=(0.11, 0.17, 0.085))
    sole = sphere(name + '_Sole', 1.0, (hx + side * 0.02, -0.05, 0.03), 'M_Apple_Sole', scale=(0.115, 0.178, 0.04))
    toe = sphere(name + '_Toe', 1.0, (hx + side * 0.02, -0.15, 0.06), 'M_Apple_Sole', scale=(0.07, 0.05, 0.05), segs=16, rings=10)
    collar = tube(name + '_Collar', [(hx + side * 0.015, 0, 0.15), (hx + side * 0.015, 0, 0.12)], [0.05, 0.055], 'M_Apple_Sole', segs=14)
    o = join([leg, shoe, sole, toe, collar], name); smooth(o)
    o.data.transform(Matrix.Translation((0, 0, 0)))
    legs[name] = (o, Vector((hx, 0, hz)))

# ============================================================ pivots, flat hierarchy, normalise height
parts = {'Body': (body, Vector((0, 0, 0.474))), 'Leaf': (leafobj, Vector((0.0, 0, top_z - 0.02)))}
parts.update(arms); parts.update(legs)
# measure height and scale to exactly 1.2 m (feet at 0)
zmax = max((o.matrix_world @ v.co).z for o, _ in parts.values() for v in o.data.vertices)
zmin = min((o.matrix_world @ v.co).z for o, _ in parts.values() for v in o.data.vertices)
S = 1.2 / (zmax - zmin)
for n, (o, piv) in parts.items():
    o.data.transform(Matrix.Translation((0, 0, -zmin)))
    o.data.transform(Matrix.Scale(S, 4))
    p = Vector((piv.x, piv.y, piv.z - zmin)) * S
    o.data.transform(Matrix.Translation(-p))
    o.location = p
root = bpy.data.objects.new('CHR_AppleChild', None); scene.collection.objects.link(root)
bpy.context.view_layer.update()
for n, (o, _) in parts.items():
    w = o.matrix_world.copy(); o.parent = root; o.matrix_world = w

# ============================================================ skin texture (sphere UVs on Body skin): red with streaks + top highlight
TEX = 1024
rng = np.random.default_rng(11)
u = np.linspace(0, 1, TEX)[None, :].repeat(TEX, 0)
v = np.linspace(0, 1, TEX)[:, None].repeat(TEX, 1)       # image row 0 = top of the PNG
base = np.array([0.86, 0.13, 0.17])
streaks = np.zeros((TEX, TEX))
for k in range(70):
    cu = rng.random(); w = rng.uniform(0.002, 0.008); amp = rng.uniform(0.04, 0.12)
    du = np.minimum(np.abs(u - cu), 1 - np.abs(u - cu))
    streaks += amp * np.exp(-(du / w) ** 2) * np.clip(1 - v * rng.uniform(0.8, 1.4), 0, 1)
yellow = np.clip((v - 0.0) * 0, 0, 1)
img = np.zeros((TEX, TEX, 3))
for c in range(3): img[..., c] = base[c]
img[..., 1] += streaks * 0.25; img[..., 0] += streaks * 0.1
# warm blush near the top (shoulders), deeper red at the bottom
top = np.clip(1 - v * 3.0, 0, 1)
img[..., 0] += top * 0.08; img[..., 1] += top * 0.1
bot = np.clip((v - 0.6) * 2.5, 0, 1)
img *= (1 - 0.18 * bot)[..., None]
speck = (rng.random((TEX, TEX)) > 0.9975) * 0.25
img[..., 1] += speck; img[..., 2] += speck * 0.6
img = np.clip(img, 0, 1)
from PIL import Image  # noqa
Image.fromarray((img * 255).astype(np.uint8)).save(os.path.join(OUT, 'T_AppleChild_Skin.png'))

# ============================================================ report + export
tot = 0
for n, (o, _) in parts.items():
    t = sum(len(p.vertices) - 2 for p in o.data.polygons); tot += t
    print(f"PART {n:6s} tris={t:5d} pivot={tuple(round(c, 3) for c in o.matrix_world.translation)} mats={[m.name for m in o.data.materials]}")
print('TOTAL_TRIS', tot, 'SCALE', round(S, 3))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND, 'CHR_AppleChild_Game_v2.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'CHR_AppleChild.fbx'), use_selection=True, object_types={'EMPTY', 'MESH'},
                         axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                         add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
print('EXPORTED')
