"""
DREAM RUSH — CHR_AppleChild v3 ("Pip") — quality pass (user 2026-10-20: "ตัวโมเดล เปลี่ยนให้หมด ขอดีกว่านี้").
v3 over v2: 5-lobed apple base + hand-made asymmetry, iris + lash line + tooth row, veined double leaf, 3-finger
mitts with rolled cuffs, real sneakers (toe cap, tread band, laces, star badge, heel tab), and a back that reads —
a dream-pillow backpack on a star-buckle belt (the camera sees Pip's back 90% of the time).
Body size/pivots identical to v2 so outfits + colliders keep fitting.
Target read: Astro Bot / Mario chibi — big glossy apple head-body, huge expressive eyes, open grin,
blush, short noodle limbs with oversized white mitts and chunky lavender sneakers (the goofy run sells on them).

Contract kept with the game code (AppleProceduralAnimator / AppleFace / AppleCharacterSetup):
  CHR_AppleChild (empty) -> FLAT children Body, Leaf, Arm_L, Arm_R, Leg_L, Leg_R
  Body contains the face; eye slots use M_Apple_EyeWhite / M_Apple_Pupil / M_Apple_PupilShine (AppleFace swaps them)
  Height 1.2 m, feet at z=0, front = -Y (Unity +Z), Blender -X = character's RIGHT.
Outputs: Assets/DreamRush/Art/Characters/AppleChild/CHR_AppleChild.fbx + T_AppleChild_Skin.png,
         ArtSource/Blender/Characters/CHR_AppleChild_Game_v3.blend
run: python3 -I build_apple_v3.py
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
    'M_Apple_Sole': (0.98, 0.96, 0.92), 'M_Apple_PupilIris': (0.55, 0.22, 0.12), 'M_Apple_LeafVein': (0.75, 0.9, 0.5),
    'M_Apple_Accent': (1.0, 0.82, 0.2), 'M_Apple_Pack': (0.45, 0.6, 0.98), 'M_Apple_PackTrim': (0.98, 0.96, 0.92), 'M_Apple_Belt': (0.3, 0.22, 0.5),
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


def ring(name, center, axis, R0, r0, material, segs=24, rsegs=8):
    """torus around `axis` at `center` (rolled cuffs, collars, belt loops)."""
    bm = bmesh.new(); axis = Vector(axis).normalized()
    u = axis.orthogonal().normalized(); w = axis.cross(u).normalized(); rows = []
    for i in range(segs):
        a = i / segs * 2 * math.pi
        radial = u * math.cos(a) + w * math.sin(a)
        rows.append([bm.verts.new(Vector(center) + radial * (R0 + r0 * math.cos(b)) + axis * (r0 * math.sin(b)))
                     for b in [j / rsegs * 2 * math.pi for j in range(rsegs)]])
    for i in range(segs):
        for j in range(rsegs):
            bm.faces.new((rows[i][j], rows[(i + 1) % segs][j], rows[(i + 1) % segs][(j + 1) % rsegs], rows[i][(j + 1) % rsegs]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return obj_from_bm(name, bm, material)


# ============================================================ BODY (apple)
BODY_C = Vector((0, 0, 0.64))
R = 0.43


def apple_radius(d):
    """direction -> radius: slightly squat, wider shoulders, top dimple, tapered bottom, two soft lobes."""
    x, y, z = d
    r = R * (1.0 + 0.06 * max(z, 0) - 0.08 * max(-z, 0) ** 2)
    phi = math.atan2(y, x)
    r *= 1.0 + 0.025 * math.cos(2 * phi) * max(z, 0)
    r *= 1.0 + 0.04 * math.cos(5 * phi + 0.3) * max(-z, 0) ** 1.6     # the 5 soft lobes of a real apple's base
    r *= 1.0 + 0.012 * math.sin(phi * 3 + 1.1) * (1 - abs(z))          # hand-made, never perfectly round
    return r


bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=64, v_segments=40, radius=1.0, calc_uvs=True)
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
    ir = sphere(f'Iris{side}', 1.0, pc, 'M_Apple_PupilIris', scale=(0.066, 0.03, 0.088), rot=(math.radians(-8), 0, yaw))
    pc2, _ = surface(x - side * 0.012, 0.06, 0.02)
    pu = sphere(f'Pupil{side}', 1.0, pc2, 'M_Apple_Pupil', scale=(0.044, 0.026, 0.062), rot=(math.radians(-8), 0, yaw))
    # lash line: a thick ink arc hugging the top of the eye, flicking up at the outer corner (Astro-Bot polish)
    lp = []
    for k in range(9):
        a = math.radians(160 - k * 17.5) if side > 0 else math.radians(20 + k * 17.5)   # inner -> outer
        lp.append(tuple(surface(x + math.cos(a) * 0.104, 0.075 + math.sin(a) * 0.138, 0.008)[0]))
    ox = x + side * 0.118
    lp.append(tuple(surface(ox, 0.155, 0.006)[0])); lp.append(tuple(surface(ox + side * 0.022, 0.19, 0.004)[0]))
    lash = tube(f'Lash{side}', lp, [0.006, 0.01, 0.013, 0.014, 0.015, 0.015, 0.015, 0.014, 0.013, 0.01, 0.005], 'M_Apple_Ink', segs=8)
    face += [ir, lash]
    s1, _ = surface(x + 0.02, 0.105, 0.04)
    s2, _ = surface(x - 0.025, 0.035, 0.038)
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
teeth, _ = surface(0.0, -0.084, 0.0)
tooth = sphere('Teeth', 1.0, teeth, 'M_Apple_Sole', scale=(0.068, 0.016, 0.016), segs=20, rings=8, rot=(math.radians(-8), 0, 0))
face += [mouth, tongue, tooth]

# ---------------------------------------------------------------- the back: star-buckle belt + dream-pillow backpack
import dr_bkit as K  # noqa: E402


def skin_point(phi, dz, push):
    """point on the deformed skin at azimuth phi (0 = +X, -90° = front) and height dz from BODY_C (bisection on polar angle)."""
    lo, hi = -math.pi / 2 + 0.05, math.pi / 2 - 0.05
    for _ in range(40):
        th = (lo + hi) / 2
        d = Vector((math.cos(th) * math.cos(phi), math.cos(th) * math.sin(phi), math.sin(th)))
        p = d * apple_radius(d); p.z *= 0.9
        if p.z < dz: lo = th
        else: hi = th
    n = Vector((p.x, p.y, p.z / 0.81)).normalized()
    return p + BODY_C + n * push


back = skin_point(math.radians(90), -0.04, 0.0)
pack = K.soft_box('Pack', (0.36, 0.27, 0.13), (0, 0, 0), 'M_Apple_Pack', bevel=0.075, segs=5, puff=0.9)
pack.rotation_euler = (math.radians(90 + 14), math.radians(8), 0); pack.location = tuple(back + Vector((0, 0.075, 0.01)))
packparts = [pack]
for sx_ in (-1, 1):
    for sz_ in (-1, 1):   # pillow tufts at the 4 corners
        packparts.append(sphere(f'Tuft{sx_}{sz_}', 1.0, back + Vector((sx_ * 0.175, 0.07 + sz_ * -0.02, sz_ * 0.13 + sx_ * 0.012)), 'M_Apple_PackTrim', scale=(0.038, 0.034, 0.038), segs=12, rings=8))
moon = K.star('PackStar', 0.055, 0.025, 0.02, (0, 0, 0), 'M_Apple_Accent', bevel=0.005)
moon.rotation_euler = (math.radians(14), 0, math.radians(180)); moon.location = tuple(back + Vector((0.035, 0.262, -0.02)))
moon.scale = (1.3, 1.3, 1.3)
packparts.append(moon)
for sx_ in (-1, 1):   # stitched seam line around the pillow face
    pass
face += packparts

body = join([body] + face, 'Body')
smooth(body)

# ============================================================ LEAF + STEM (pivot at stem base)
top_z = BODY_C.z + R * 1.06 * 0.9 - 0.13
stem = tube('Stem', [(0, 0, top_z - 0.03), (0.004, 0.0, top_z + 0.05), (0.022, 0.004, top_z + 0.12), (0.05, 0.006, top_z + 0.165), (0.07, 0.004, top_z + 0.175)],
            [0.028, 0.025, 0.022, 0.019, 0.017], 'M_Apple_Stem', segs=12)


def leaf_blade(name, L, Wd, curl):
    bm = bmesh.new(); rows = []
    for i in range(13):
        t = i / 12
        w = Wd * (math.sin(t * math.pi) ** 0.75) * (1 - 0.25 * t)
        row = []
        for j in range(7):
            sgn = (j / 6) * 2 - 1
            fold = abs(sgn) * w * 0.4                                    # V-fold along the spine
            row.append(bm.verts.new((t * L, sgn * w, fold + curl * math.sin(t * math.pi * 0.9) - 0.02 * t * t)))
        rows.append(row)
    for i in range(12):
        for j in range(6):
            bm.faces.new((rows[i][j], rows[i + 1][j], rows[i + 1][j + 1], rows[i][j + 1]))
    o = obj_from_bm(name, bm, 'M_Apple_Leaf')
    sol = o.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.012; sol.offset = 0
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='sol')
    # midrib: a lighter vein riding the fold
    vein = tube(name + 'Vein', [(t * L * 0.92, 0, 0.008 + curl * math.sin(t * math.pi * 0.9) - 0.02 * t * t) for t in [k / 6 for k in range(7)]],
                [0.007, 0.0065, 0.006, 0.005, 0.004, 0.003, 0.002], 'M_Apple_LeafVein', segs=6)
    return join([o, vein], name)


leaf = leaf_blade('LeafBlade', 0.27, 0.105, 0.05)
leaf.location = (0.055, 0, top_z + 0.15); leaf.rotation_euler = (math.radians(12), math.radians(-30), math.radians(-12))
leaf2 = leaf_blade('LeafSmall', 0.15, 0.06, 0.03)
leaf2.location = (0.03, 0.01, top_z + 0.13); leaf2.rotation_euler = (math.radians(-15), math.radians(-40), math.radians(150))
leafobj = join([stem, leaf, leaf2], 'Leaf'); smooth(leafobj)


# ============================================================ ARMS (pivot at shoulder) — tapered noodle + 3-finger mitt
arms = {}
for side, name in ((1, 'Arm_L'), (-1, 'Arm_R')):       # +X = character's LEFT
    sx = side * 0.4; sz = 0.6
    pts = [(sx - side * 0.03, 0, sz), (sx + side * 0.05, -0.008, sz - 0.05), (sx + side * 0.1, -0.016, sz - 0.13),
           (sx + side * 0.125, -0.024, sz - 0.21), (sx + side * 0.14, -0.03, sz - 0.27)]
    arm = tube(name + '_Tube', pts, [0.038, 0.035, 0.032, 0.03, 0.03], 'M_Apple_Limb', segs=12)
    hc = Vector((sx + side * 0.158, -0.04, sz - 0.335))
    palm = sphere(name + '_Mitt', 1.0, hc, 'M_Apple_Glove', scale=(0.082, 0.07, 0.09))
    parts_ = [arm, palm]
    for k, (fx, fz) in enumerate(((0.035, -0.06), (0.0, -0.072), (-0.035, -0.062))):   # 3 chubby fingers curling forward
        parts_.append(sphere(f'{name}_F{k}', 1.0, hc + Vector((fx * side * 0.6 + side * 0.012, -0.035, fz)), 'M_Apple_Glove', scale=(0.032, 0.036, 0.04), segs=14, rings=8))
    parts_.append(sphere(name + '_Thumb', 1.0, hc + Vector((-side * 0.06, -0.045, 0.02)), 'M_Apple_Glove', scale=(0.034, 0.03, 0.046), segs=14, rings=8, rot=(0, math.radians(side * 25), 0)))
    parts_.append(ring(name + '_Cuff', hc + Vector((-side * 0.01, 0.0, 0.078)), (side * 0.15, 0.1, 1), 0.044, 0.018, 'M_Apple_Glove'))
    o = join(parts_, name); smooth(o)
    arms[name] = (o, Vector((sx - side * 0.03, 0, sz)))

# ============================================================ LEGS (pivot at hip) — noodle + real sneaker
legs = {}
for side, name in ((1, 'Leg_L'), (-1, 'Leg_R')):
    hx = side * 0.135; hz = 0.3
    leg = tube(name + '_Tube', [(hx, 0, hz), (hx + side * 0.008, 0, hz - 0.08), (hx + side * 0.014, 0, hz - 0.15), (hx + side * 0.016, 0, 0.11)],
               [0.038, 0.036, 0.034, 0.034], 'M_Apple_Limb', segs=12)
    cx = hx + side * 0.02
    upper = sphere(name + '_Shoe', 1.0, (cx, -0.045, 0.085), 'M_Apple_Shoe', scale=(0.112, 0.175, 0.088))
    sole = sphere(name + '_Sole', 1.0, (cx, -0.05, 0.032), 'M_Apple_Sole', scale=(0.122, 0.188, 0.042))
    band = sphere(name + '_Band', 1.0, (cx, -0.05, 0.05), 'M_Apple_Accent', scale=(0.118, 0.183, 0.014), segs=32, rings=6)
    toe = sphere(name + '_Toe', 1.0, (cx, -0.158, 0.064), 'M_Apple_Sole', scale=(0.078, 0.06, 0.054), segs=18, rings=10)
    collar = ring(name + '_Collar', (hx + side * 0.015, 0.0, 0.15), (0, 0, 1), 0.044, 0.016, 'M_Apple_Sole')
    tongue_ = sphere(name + '_Tongue', 1.0, (cx, -0.035, 0.16), 'M_Apple_Shoe', scale=(0.05, 0.03, 0.04), segs=14, rings=8, rot=(math.radians(-30), 0, 0))
    heel = sphere(name + '_Heel', 1.0, (cx, 0.115, 0.12), 'M_Apple_Accent', scale=(0.03, 0.02, 0.04), segs=12, rings=8)
    laces = []
    for k in range(3):   # laces: white bars over the instep
        y = -0.075 - k * 0.032; z = 0.155 - k * 0.018
        laces.append(tube(f'{name}_Lace{k}', [(cx - 0.05, y, z - 0.012), (cx, y - 0.004, z + 0.004), (cx + 0.05, y, z - 0.012)], [0.009, 0.01, 0.009], 'M_Apple_Sole', segs=6))
    # star badge on the outer side of the shoe
    import dr_bkit as K  # noqa: E402
    badge = K.star(name + '_Star', 0.04, 0.018, 0.016, (0, 0, 0), 'M_Apple_Accent', bevel=0.004)
    badge.rotation_euler = (0, 0, math.radians(side * 90)); badge.location = (cx + side * 0.11, -0.03, 0.09)
    o = join([leg, upper, sole, band, toe, collar, tongue_, heel, badge] + laces, name); smooth(o)
    legs[name] = (o, Vector((hx, 0, hz)))

# ============================================================ pivots, flat hierarchy, normalise height
parts = {'Body': (body, Vector((0, 0, 0.474))), 'Leaf': (leafobj, Vector((0.0, 0, top_z - 0.02)))}
parts.update(arms); parts.update(legs)
# measure height and scale to exactly 1.2 m (feet at 0)
zmax = max((o.matrix_world @ v.co).z for o, _ in parts.values() for v in o.data.vertices)
zmin = min((o.matrix_world @ v.co).z for o, _ in parts.values() for v in o.data.vertices)
zbody = max((body.matrix_world @ v.co).z for v in body.data.vertices if (v.co - BODY_C).length < 0.7)
S = 1.024 / (zbody - zmin)    # same body size as v2 (outfits + colliders measured on it); a bigger leaf may poke above 1.2
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
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND, 'CHR_AppleChild_Game_v3.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'CHR_AppleChild.fbx'), use_selection=True, object_types={'EMPTY', 'MESH'},
                         axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                         add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
print('EXPORTED')
