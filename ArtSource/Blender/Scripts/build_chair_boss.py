"""
BOSS_Chair — procedural build (GDD §12.2/§12.3, Applebook Surrealism §5).

Handcrafted stylized wooden chair: rounded, slightly uneven, four expressive legs, curved backrest,
soft bevels, warm wood, expressive eyes on the upper backrest, small mouth, visible seat.
Same footprint as the graybox (seat top 0.70 m, 1.1 x 1.05, backrest top ~1.78 m) so gameplay is unchanged.

Run:  python3 -I build_chair_boss.py            (bpy module)
Out:  Assets/DreamRush/Art/Characters/Chair/BOSS_Chair.fbx + T_Chair_Wood.png
      ArtSource/Blender/Characters/BOSS_Chair_v1.blend, ArtSource/Renders/BOSS_Chair_*.png

Axis rule (blender-asset-pipeline skill): character front = Blender -Y  (Unity +Z),
Blender -X = character's right (Unity +X). Flat hierarchy, pivots on each object's origin.
"""
import math
import os
import random

import bpy
import bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Characters', 'Chair')
BLEND = os.path.join(ROOT, 'ArtSource', 'Blender', 'Characters', 'BOSS_Chair_v1.blend')
RENDERS = os.path.join(ROOT, 'ArtSource', 'Renders')
os.makedirs(OUT, exist_ok=True)
os.makedirs(os.path.dirname(BLEND), exist_ok=True)
os.makedirs(RENDERS, exist_ok=True)
TEX = os.path.join(OUT, 'T_Chair_Wood.png')

random.seed(7)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


# ---------------------------------------------------------------- wood texture (painted, low-contrast grain)
def make_wood_texture(path, size=512):
    img = bpy.data.images.new('T_Chair_Wood', size, size, alpha=False)
    base = (0.84, 0.55, 0.32)       # warm honey wood, sRGB-ish
    dark = (0.66, 0.38, 0.22)
    px = [0.0] * (size * size * 4)
    rnd = random.Random(3)
    knots = [(rnd.random() * size, rnd.random() * size, 18 + rnd.random() * 20) for _ in range(5)]
    for y in range(size):
        for x in range(size):
            # grain runs along U; wobble with low-frequency sines (hand-painted feel, not photo wood)
            w = math.sin(y * 0.19 + math.sin(x * 0.013) * 3.0 + math.sin(x * 0.051) * 0.6)
            g = 0.5 + 0.5 * w
            g = g ** 9                                  # thin darker grain lines
            for kx, ky, kr in knots:
                d = math.hypot(x - kx, (y - ky) * 1.8)
                if d < kr:
                    g = max(g, (1 - d / kr) ** 2 * 0.8)
            n = (rnd.random() - 0.5) * 0.025            # paper tooth
            t = min(1.0, g * 0.45)
            r = base[0] * (1 - t) + dark[0] * t + n
            gg = base[1] * (1 - t) + dark[1] * t + n
            b = base[2] * (1 - t) + dark[2] * t + n
            i = (y * size + x) * 4
            px[i] = max(0, r); px[i + 1] = max(0, gg); px[i + 2] = max(0, b); px[i + 3] = 1.0  # byte image: display-space values
    img.pixels = px
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    return img


wood_img = make_wood_texture(TEX)


def material(name, color, rough=0.6, image=None):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = rough
    if image is not None:
        tn = m.node_tree.nodes.new('ShaderNodeTexImage')
        tn.image = image
        m.node_tree.links.new(tn.outputs['Color'], bsdf.inputs['Base Color'])
    return m


def srgb(h):
    return tuple(((h >> s) & 255) / 255.0 for s in (16, 8, 0))


def lin(c):
    return tuple(v ** 2.2 for v in c)


M_WOOD = material('M_Chair_Wood', lin(srgb(0xCC8754)), 0.7, wood_img)
M_WOOD_DARK = material('M_Chair_WoodDark', lin(srgb(0x9A5C36)), 0.7)
M_EYE = material('M_Chair_EyeWhite', lin(srgb(0xF7F1E4)), 0.4)
M_INK = material('M_Chair_Ink', lin(srgb(0x2B2233)), 0.5)
M_BLUSH = material('M_Chair_Blush', lin(srgb(0xF29C8C)), 0.8)
M_CUSHION = material('M_Chair_Cushion', lin(srgb(0xF3D9D2)), 0.9)


# ---------------------------------------------------------------- helpers
def obj_from_bm(name, bm, mat):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    o.data.materials.append(mat)
    return o


def rounded_box(name, size, center, mat, bevel=0.04, segs=3):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    o = obj_from_bm(name, bm, mat)
    o.location = center
    mod = o.modifiers.new('bevel', 'BEVEL')
    mod.width = bevel; mod.segments = segs; mod.limit_method = 'NONE'
    return o


TURN = [(0.0, 1.0), (0.06, 1.0), (0.08, 1.18), (0.12, 1.18), (0.15, 0.82), (0.22, 0.9), (0.6, 0.78), (0.7, 0.86),
        (0.74, 1.08), (0.78, 1.08), (0.82, 0.8), (0.9, 0.86), (1.0, 0.92)]   # lathe profile: (t from top, radius factor)


def tapered_leg(name, top, length, r_top, r_bot, mat, splay=(0, 0)):
    """Lathe-turned spindle (rings, beads, a long taper) — origin at the TOP of the leg (pivot for gait swings)."""
    bm = bmesh.new(); segs = 18; rings = []
    for t, f in TURN:
        r = (r_top + (r_bot - r_top) * t) * f
        rings.append([bm.verts.new((math.cos(a) * r, math.sin(a) * r, -t * length)) for a in [k / segs * 2 * math.pi for k in range(segs)]])
    for i in range(len(rings) - 1):
        for k in range(segs):
            bm.faces.new((rings[i][k], rings[i + 1][k], rings[i + 1][(k + 1) % segs], rings[i][(k + 1) % segs]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = obj_from_bm(name, bm, mat)
    o.location = top
    o.rotation_euler = (splay[0], splay[1], 0)
    mod = o.modifiers.new('bevel', 'BEVEL')
    mod.width = 0.006; mod.segments = 2; mod.limit_method = 'ANGLE'
    return o


def ball(name, r, loc, mat, squash=(1, 1, 1), segs=16):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=r)
    for v in bm.verts: v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
    o = obj_from_bm(name, bm, mat); o.location = loc
    return o


def apply_all(o):
    bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers):
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True)
        bpy.ops.object.modifier_apply(modifier=m.name)


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    for o in objs:
        apply_all(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = name
    objs[0].data.name = name
    return objs[0]


def set_origin(o, point):
    bpy.context.view_layer.update()
    mw = o.matrix_world.copy()
    local = mw.inverted() @ Vector(point)
    o.data.transform(Matrix.Translation(-local))
    o.location = mw @ local


def box_uv(o, scale=1.6):
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=scale)
    bpy.ops.object.mode_set(mode='OBJECT')


# ---------------------------------------------------------------- body
SEAT_TOP = 0.70
SEAT_T = 0.15
seat = rounded_box('Seat', (1.10, 1.05, SEAT_T), (0, 0, SEAT_TOP - SEAT_T / 2), M_WOOD, bevel=0.06, segs=4)
# soft cushion dip on the seat (hand-made, "sit here" read)
apply_all(seat)
for v in seat.data.vertices:
    if v.co.z > SEAT_T / 2 - 0.01:
        d = min(1.0, (v.co.x ** 2 / 0.3 + v.co.y ** 2 / 0.28))
        v.co.z -= 0.025 * (1 - d)
# front apron (a little lip under the seat — readable "chair" silhouette from the 3/4 camera)
apron = rounded_box('Apron', (0.92, 0.06, 0.10), (0, -0.47, SEAT_TOP - SEAT_T - 0.04), M_WOOD_DARK, bevel=0.025)
apply_all(apron)
for v in apron.data.vertices:   # scalloped lower edge (three soft arches)
    if v.co.z < -0.02:
        v.co.z += 0.035 * abs(math.sin(v.co.x / 0.46 * math.pi * 1.5))

# legs: front pair splays forward/out, back pair slightly back — uneven on purpose (§12.2)
LEG_TOP_Z = SEAT_TOP - SEAT_T + 0.01
legs = {
    # name: (x, y, r_top, r_bot, splay_x, splay_y, length)
    'Leg_FL': (+0.42, -0.40, 0.075, 0.058, math.radians(-5), math.radians(-4), 0.515),
    'Leg_FR': (-0.42, -0.40, 0.078, 0.060, math.radians(-4), math.radians(5), 0.51),
    'Leg_BL': (+0.42, +0.40, 0.074, 0.056, math.radians(5), math.radians(-3), 0.52),
    'Leg_BR': (-0.43, +0.41, 0.076, 0.058, math.radians(4), math.radians(4), 0.515),
}
leg_objs = []
for n, (x, y, rt, rb, sx, sy, L) in legs.items():
    leg = tapered_leg(n, (x, y, LEG_TOP_Z), L, rt, rb, M_WOOD, splay=(sx, sy))
    # little rounded foot ("shoe") — reads as feet when it runs
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=rb * 1.25)
    for v in bm.verts:
        v.co.z *= 0.7
        v.co.z -= L - 0.01
        v.co.y -= 0.015
    foot = obj_from_bm(n + '_foot', bm, M_WOOD_DARK)
    foot.location = leg.location
    foot.rotation_euler = leg.rotation_euler
    leg = join([leg, foot], n)
    leg_objs.append(leg)

# backrest: two posts leaning back, a curved top rail (the "face" board) and one slat
BACK_Y = 0.43
LEAN = math.radians(9)
posts = []
for side, x in (('L', 0.45), ('R', -0.45)):
    p = tapered_leg('Post_' + side, (x, BACK_Y + math.sin(LEAN) * 1.0, 1.62), 1.0, 0.055, 0.065, M_WOOD)
    p.rotation_euler = (-LEAN, 0, 0)
    posts.append(p)


def curved_board(name, width, height, thick, center_z, curve, mat, segs=24):
    """Board curved around Z (concave toward the sitter / camera), slightly wider at the top (uneven)."""
    bm = bmesh.new()
    verts = []
    for j in range(2):
        z = -height / 2 + j * height
        w = width * (1.0 + 0.06 * j)
        row = []
        for i in range(segs + 1):
            u = i / segs - 0.5
            x = u * w
            y = curve * (u * 2) ** 2      # ends bend back
            row.append((x, y, z))
        verts.append(row)
    vs = {}
    for j in range(2):
        for i in range(segs + 1):
            for k, off in enumerate((-thick / 2, thick / 2)):
                x, y, z = verts[j][i]
                vs[(j, i, k)] = bm.verts.new((x, y + off, z))
    for i in range(segs):
        for k in range(2):
            bm.faces.new([vs[(0, i, k)], vs[(0, i + 1, k)], vs[(1, i + 1, k)], vs[(1, i, k)]])
        for j in range(2):
            bm.faces.new([vs[(j, i, 0)], vs[(j, i + 1, 0)], vs[(j, i + 1, 1)], vs[(j, i, 1)]])
    for i in (0, segs):
        bm.faces.new([vs[(0, i, 0)], vs[(0, i, 1)], vs[(1, i, 1)], vs[(1, i, 0)]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = obj_from_bm(name, bm, mat)
    o.location = (0, BACK_Y + math.tan(LEAN) * (center_z - SEAT_TOP), center_z)
    o.rotation_euler = (-LEAN, 0, 0)
    mod = o.modifiers.new('bevel', 'BEVEL')
    mod.width = 0.03; mod.segments = 3; mod.limit_method = 'NONE'
    # gentle top arch
    return o


top_rail = curved_board('TopRail', 1.0, 0.54, 0.11, 1.53, 0.07, M_WOOD)
apply_all(top_rail)
for v in top_rail.data.vertices:          # arch the top edge like a cartoon headboard
    if v.co.z > 0.1:
        v.co.z += 0.07 * math.cos(v.co.x * math.pi / 1.1)
slat = curved_board('Slat', 0.86, 0.11, 0.07, 1.05, 0.05, M_WOOD_DARK)
# turned finials on the posts + three spindles between the slat and the seat (Windsor read from the 3/4 camera)
extra = []
for side, x in (('L', 0.45), ('R', -0.45)):
    tz = 1.62 + 0.02; ty = BACK_Y + math.sin(LEAN) * 1.0 + 0.0
    extra.append(ball('Finial_' + side, 0.085, (x, ty, tz + 0.07), M_WOOD_DARK, squash=(1, 1, 1.15)))
    extra.append(ball('FinialTip_' + side, 0.04, (x, ty, tz + 0.18), M_WOOD_DARK))
for k, x in enumerate((-0.22, 0.0, 0.22)):
    zt = 0.98; zb = SEAT_TOP + 0.01
    sp = tapered_leg(f'Spindle{k}', (x, BACK_Y + math.tan(LEAN) * (zt - SEAT_TOP), zt), zt - zb, 0.03, 0.034, M_WOOD)
    sp.rotation_euler = (-LEAN, 0, 0); extra.append(sp)
backrest = join([top_rail, slat] + posts + extra, 'Backrest')

body_parts = [seat, apron, backrest]
for o in body_parts:
    apply_all(o)
body = join(body_parts, 'Body')
set_origin(body, (0, 0, 0))
for o in [body] + leg_objs:
    box_uv(o)

# ---------------------------------------------------------------- seat cushion (gingham) — THE thing you sit on (focal + gameplay read)
bm = bmesh.new()
bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=5, use_grid_fill=True)
for v in bm.verts:
    x, y, z = v.co.x * 0.98, v.co.y * 0.93, v.co.z * 0.1
    if v.co.z > 0.49:
        z += 0.05 * (1 - (2 * v.co.x) ** 2) * (1 - (2 * v.co.y) ** 2)   # puffy top
    v.co = Vector((x, y, z))
cushion = obj_from_bm('SeatCushion', bm, M_CUSHION)
cushion.location = (0, -0.03, SEAT_TOP + 0.045)
mod = cushion.modifiers.new('bevel', 'BEVEL'); mod.width = 0.04; mod.segments = 3; mod.limit_method = 'ANGLE'
apply_all(cushion)
for side in (-1, 1):                                       # two little ties at the back posts
    t = rounded_box('Tie_' + ('L' if side > 0 else 'R'), (0.05, 0.04, 0.14), (side * 0.42, BACK_Y - 0.02, SEAT_TOP + 0.02), M_CUSHION, bevel=0.015)
    t.rotation_euler = (0.4, side * 0.3, 0)
    apply_all(t)
    cushion = join([cushion, t], 'SeatCushion')
tufts = []
for bx in (-0.22, 0.22):
    for by in (-0.2, 0.2):
        tufts.append(ball(f'Tuft{bx}{by}', 0.03, (bx, -0.03 + by, SEAT_TOP + 0.045 + 0.075), M_WOOD_DARK, squash=(1, 1, 0.5), segs=10))
pip = bpy.data.curves.new('Piping', 'CURVE'); pip.dimensions = '3D'; pip.bevel_depth = 0.018; pip.bevel_resolution = 2
spl = pip.splines.new('POLY'); pts = []
for k in range(64):
    a = k / 64 * 2 * math.pi; c, s_ = math.cos(a), math.sin(a)
    px = 0.49 * math.copysign(abs(c) ** 0.25, c); py = 0.465 * math.copysign(abs(s_) ** 0.25, s_)
    pts.append((px, py - 0.03, SEAT_TOP + 0.045 + 0.045))
spl.points.add(len(pts) - 1)
for k, p in enumerate(pts): spl.points[k].co = (*p, 1)
spl.use_cyclic_u = True
po = bpy.data.objects.new('Piping', pip); scene.collection.objects.link(po)
bpy.ops.object.select_all(action='DESELECT'); po.select_set(True); bpy.context.view_layer.objects.active = po
bpy.ops.object.convert(target='MESH'); po = bpy.context.view_layer.objects.active
po.data.materials.clear(); po.data.materials.append(M_WOOD_DARK)
cushion = join([cushion] + tufts + [po], 'SeatCushion')
box_uv(cushion, 0.5)

# ---------------------------------------------------------------- face (on the front of the top rail)
bpy.context.view_layer.update()
FACE_Z = 1.55
face_y = BACK_Y + math.tan(LEAN) * (FACE_Z - SEAT_TOP) - 0.06   # front surface of the rail (toward -Y)


def oval(name, size, loc, mat, segs=20, depth_scale=1.0, tilt=True):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1] * depth_scale, v.co.z * size[2]))
    o = obj_from_bm(name, bm, mat)
    o.location = loc
    if tilt:
        o.rotation_euler = (-LEAN, 0, 0)
    return o


EYE_X = 0.23
eye_l = oval('Eye_L', (0.125, 0.035, 0.15), (+EYE_X, face_y - 0.005, FACE_Z), M_EYE)
eye_r = oval('Eye_R', (0.125, 0.035, 0.15), (-EYE_X, face_y - 0.005, FACE_Z), M_EYE)
pup_l = oval('Pupil_L', (0.055, 0.02, 0.07), (+EYE_X - 0.015, face_y - 0.035, FACE_Z - 0.02), M_INK)
pup_r = oval('Pupil_R', (0.055, 0.02, 0.07), (-EYE_X + 0.015, face_y - 0.035, FACE_Z - 0.02), M_INK)
for nm, px in (('L', +EYE_X - 0.015), ('R', -EYE_X + 0.015)):   # catch-lights fused into the pupils (move with them)
    sh = oval('Shine' + nm, (0.022, 0.012, 0.026), (px + 0.022, face_y - 0.05, FACE_Z + 0.01), M_EYE, segs=10)
    sh2 = oval('Shine2' + nm, (0.01, 0.01, 0.012), (px - 0.02, face_y - 0.048, FACE_Z - 0.045), M_EYE, segs=8)
    pp = pup_l if nm == 'L' else pup_r
    pp = join([pp, sh, sh2], 'Pupil_' + nm)
    if nm == 'L': pup_l = pp
    else: pup_r = pp
# worried brows (pivot at their centre so the animator can tilt them)
brows = []
for side, x, ang in (('L', +EYE_X, 8), ('R', -EYE_X, -8)):   # rest: slightly worried (inner ends up) — nervous, never evil
    b = rounded_box('Brow_' + side, (0.17, 0.03, 0.035), (x, face_y - 0.02, FACE_Z + 0.175), M_INK, bevel=0.012)
    b.rotation_euler = (-LEAN, math.radians(ang), 0)
    brows.append(b)
# small wobbly mouth
mc = bpy.data.curves.new('MouthC', 'CURVE'); mc.dimensions = '3D'; mc.bevel_depth = 0.016; mc.bevel_resolution = 3
spl = mc.splines.new('POLY'); mp = [(-0.1 + 0.2 * t, 0, 0.018 * math.sin(t * math.pi * 3) - 0.01 * t) for t in [k / 16 for k in range(17)]]   # nervous wobble
spl.points.add(len(mp) - 1)
for k, p in enumerate(mp): spl.points[k].co = (*p, 1)
mouth = bpy.data.objects.new('Mouth', mc); scene.collection.objects.link(mouth)
bpy.ops.object.select_all(action='DESELECT'); mouth.select_set(True); bpy.context.view_layer.objects.active = mouth
bpy.ops.object.convert(target='MESH'); mouth = bpy.context.view_layer.objects.active
mouth.data.materials.clear(); mouth.data.materials.append(M_INK)
mouth.location = (0, face_y - 0.02, FACE_Z - 0.19); mouth.rotation_euler = (-LEAN, math.radians(-4), 0)
blush_l = oval('Blush_L', (0.06, 0.012, 0.03), (+0.37, face_y - 0.005, FACE_Z - 0.13), M_BLUSH, segs=12)
blush_r = oval('Blush_R', (0.06, 0.012, 0.03), (-0.37, face_y - 0.005, FACE_Z - 0.13), M_BLUSH, segs=12)
face_static = join([mouth, blush_l, blush_r], 'Face')
for o in [eye_l, eye_r, pup_l, pup_r] + brows:
    apply_all(o)

# ---------------------------------------------------------------- shading + stats
for o in scene.objects:
    if o.type == 'MESH':
        for p in o.data.polygons:
            p.use_smooth = True
        if hasattr(o.data, 'set_sharp_from_angle'):
            o.data.set_sharp_from_angle(angle=math.radians(40))

tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in scene.objects if o.type == 'MESH')
bb = [o.matrix_world @ Vector(c) for o in scene.objects if o.type == 'MESH' for c in o.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
print(f"CHAIR tris={tris} bounds min={tuple(round(v, 3) for v in mn)} max={tuple(round(v, 3) for v in mx)}")
print("OBJECTS", sorted(o.name for o in scene.objects))

bpy.ops.wm.save_as_mainfile(filepath=BLEND)

# ---------------------------------------------------------------- export (flat hierarchy)
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
    o.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, 'BOSS_Chair.fbx'),
    use_selection=True, object_types={'EMPTY', 'MESH'},
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
    add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
print("EXPORTED")
