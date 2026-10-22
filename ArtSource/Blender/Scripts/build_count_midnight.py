"""
COUNT MIDNIGHT — "The Twelfth Hour" (level-2 boss, docs/LEVEL2_DESIGN.md §4). 6.9 m incl. top hat.
A gentleman-thief grandfather clock: tapered midnight tail-coat case with a glass window and a swinging pendulum,
a clock-dial head wearing a white domino mask with glowing magenta eye slits, a crown of twelve gold spikes and a
tall top hat, white gloves, a night-sky cape (star-lined), the HOUR hand (broad spade) and MINUTE hand (long arrow)
as swords. Original design (no IP reference copied).
PART CONTRACT (flat FBX, Unity re-parents by name; front = Blender -Y = Unity +Z):
  CM_Body (origin feet) · CM_Pendulum (pivot at its hinge) · CM_Head (pivot dial centre) · CM_HourHand / CM_MinuteHand
  (pivot dial centre, point to +Z = 12 o'clock) · CM_Arm_L / CM_Arm_R (pivot shoulder, hang down -Z) ·
  CM_Blade_Hour / CM_Blade_Minute (pivot in the glove grip, blade along Blender +Y = Unity -Z, like the graybox) ·
  CM_Cape (pivot between the shoulders).
run: python3 -I build_count_midnight.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'Characters', 'CountMidnight'), os.path.join('ArtSource', 'Blender', 'Characters'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere
K.reset()


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def set_origin(o, p):
    """move the object's origin to world point p without moving the geometry."""
    bpy.context.view_layer.update()
    mw = o.matrix_world.copy()
    o.data.transform(mw)
    o.matrix_world = Matrix.Identity(4)
    o.data.transform(Matrix.Translation(-Vector(p)))
    o.location = p


def part(name, objs, pivot):
    o = K.join(objs, name)
    set_origin(o, pivot)
    return o


def loft(name, rings, material, segs=24, cap=True):
    """rings: list of (z, rx, ry, cx, cy) ellipses → tube (tapered case, tails, boots)."""
    bm = bmesh.new(); prev = None; loops = []
    for (z, rx, ry, cx, cy) in rings:
        loop = [bm.verts.new((cx + math.cos(a) * rx, cy + math.sin(a) * ry, z)) for a in [k / segs * 2 * math.pi for k in range(segs)]]
        if prev:
            for k in range(segs):
                bm.faces.new((prev[k], prev[(k + 1) % segs], loop[(k + 1) % segs], loop[k]))
        prev = loop; loops.append(loop)
    if cap:
        bm.faces.new(list(reversed(loops[0]))); bm.faces.new(loops[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = K.from_bm(name, bm, material)
    return o


def flat_shape(name, pts, depth, material, y, smooth_n=4, bulge=0.0):
    """closed 2D outline in the XZ plane (Catmull-Rom smoothed) → filled slab `depth` thick facing -Y at y; bulge pushes the middle forward."""
    out = []
    L = len(pts)
    for i in range(L):
        p0, p1, p2, p3 = [Vector((*pts[(i + k) % L], 0)) for k in (-1, 0, 1, 2)]
        for t in [k / smooth_n for k in range(smooth_n)]:
            t2, t3 = t * t, t * t * t
            q = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out.append((q.x, q.y))
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '2D'; cu.fill_mode = 'BOTH'
    sp = cu.splines.new('POLY'); sp.points.add(len(out) - 1)
    for k, (x, z) in enumerate(out): sp.points[k].co = (x, z, 0, 1)
    sp.use_cyclic_u = True
    o = K.link(bpy.data.objects.new(name, cu))
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.convert(target='MESH'); o = bpy.context.view_layer.objects.active
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=1, use_grid_fill=True)
    xs = [v.co.x for v in bm.verts]; w = max(max(xs), -min(xs), 1e-3)
    for v in bm.verts:   # XY plane -> XZ plane at y, middle bulging forward
        x, z = v.co.x, v.co.y
        v.co = Vector((x, y - bulge * (1 - (x / w) ** 2), z))
    bm.to_mesh(o.data); bm.free()
    o.data.materials.clear(); o.data.materials.append(K.mat(material))
    sol = o.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = depth; sol.offset = 1; K.apply_mods(o)
    return o


# ===================================================================== BODY
body = []
# legs + pointed boots
for s in (-1, 1):
    body.append(loft(f'Leg{s}', [(0.25, 0.17, 0.2, s * 0.42, 0), (0.75, 0.15, 0.17, s * 0.43, 0), (1.15, 0.2, 0.22, s * 0.42, 0), (1.5, 0.25, 0.26, s * 0.4, 0)], 'M_CM_Coat', segs=18))
    body.append(loft(f'Stripe{s}', [(0.5, 0.03, 0.03, s * 0.42 + s * 0.16, 0), (1.45, 0.03, 0.03, s * 0.4 + s * 0.24, 0)], 'M_CM_Gold', segs=6))
    boot = loft(f'Boot{s}', [(0.0, 0.2, 0.36, s * 0.42, -0.08), (0.18, 0.21, 0.36, s * 0.42, -0.08), (0.42, 0.18, 0.22, s * 0.42, 0), (0.7, 0.2, 0.22, s * 0.42, 0), (0.78, 0.25, 0.27, s * 0.42, 0.02)], 'M_CM_Ink', segs=18)
    body.append(boot)
    body.append(rot(SP(f'Toe{s}', 1.0, (s * 0.42, -0.5, 0.1), 'M_CM_Ink', segs=16, rings=8, squash=(0.12, 0.24, 0.09)), x=-12))
    body.append(K.torus(f'Cuff{s}', 0.22, 0.045, (s * 0.42, 0, 0.6), 'M_CM_Gold', segs=24, rsegs=8))
    body.append(rot(K.star(f'Spur{s}', 0.12, 0.05, 0.03, (s * 0.42, 0.3, 0.12), 'M_CM_Gold', bevel=0.005), z=90))
# the clock-case torso: tapered waist → broad shoulders, slightly deeper at the chest
case = loft('Case', [(1.35, 0.62, 0.48, 0, 0), (1.6, 0.6, 0.48, 0, 0), (2.4, 0.66, 0.5, 0, 0), (3.2, 0.95, 0.56, 0, 0), (3.85, 1.2, 0.6, 0, 0), (4.05, 1.1, 0.56, 0, 0)], 'M_CM_Coat', segs=32)
body.append(case)
# gold trims: waist belt, chest bands, lapel lines
body.append(loft('Belt', [(1.5, 0.63, 0.5, 0, 0), (1.66, 0.64, 0.51, 0, 0)], 'M_CM_Gold', segs=32))
body.append(loft('ChestBand', [(3.62, 1.14, 0.6, 0, 0), (3.74, 1.17, 0.61, 0, 0)], 'M_CM_Gold', segs=32))
# window on the front (pendulum chamber): gold frame + dark glass
win = SB('WindowFrame', (0.82, 0.08, 1.45), (0, -0.5, 2.55), 'M_CM_Gold', bevel=0.04)
glass = SB('WindowGlass', (0.66, 0.06, 1.28), (0, -0.53, 2.55), 'M_CM_Glass', bevel=0.02)
body += [win, glass]
# coat tails (two long flaps behind, flaring)
for s in (-1, 1):
    tail = loft(f'Tail{s}', [(1.65, 0.34, 0.12, s * 0.32, 0.4), (1.0, 0.38, 0.12, s * 0.42, 0.48), (0.35, 0.36, 0.1, s * 0.55, 0.55)], 'M_CM_Coat', segs=12)
    body.append(tail)
# epaulettes: flared gold shields with a star stud and a dangling fringe (sharper silhouette than v1's buns)
for s in (-1, 1):
    ep = SP(f'Pad{s}', 1.0, (s * 1.22, 0, 4.0), 'M_CM_Gold', segs=28, rings=12, squash=(0.5, 0.44, 0.16))
    bm = bmesh.new(); bm.from_mesh(ep.data)
    for v in bm.verts:   # flare the outer edge upward into a point
        t = max(0.0, v.co.x * s) / 0.5
        v.co.z += 0.22 * t ** 3
    bm.to_mesh(ep.data); bm.free(); body.append(ep)
    body.append(rot(K.star(f'PadStar{s}', 0.14, 0.06, 0.05, (s * 1.22, -0.05, 4.18), 'M_CM_Red', bevel=0.01), x=-70))
    for k in range(9):
        a = math.pi * (0.1 + 0.8 * k / 8)
        body.append(CY(f'Fringe{s}_{k}', 0.035, 0.025, 0.34, (s * (1.22 + 0.42 * math.sin(a) * 0.0) + s * 0.44 * math.cos(math.pi / 2 * 0), -math.cos(a) * 0.4, 3.78), 'M_CM_Gold', segs=6, bevel=0))
# collar ruff (frilled ring)
for k in range(16):
    a = k / 16 * 2 * math.pi
    body.append(rot(SP(f'Ruff{k}', 1.0, (math.cos(a) * 0.45, math.sin(a) * 0.36, 4.15), 'M_CM_White', segs=12, rings=6, squash=(0.16, 0.06, 0.13)), z=math.degrees(a) + 90))
body.append(CY('Neck', 0.26, 0.22, 0.34, (0, 0.28, 4.22), 'M_CM_Ink', segs=20, bevel=0.03))   # hidden behind the dial (v1's neck read as a nose)
# white shirt-front V between sharp lapels, a red cravat bow + ruby pin
body.append(flat_shape('ShirtV', [(-0.42, 4.08), (0.42, 4.08), (0.08, 3.25), (0.0, 3.15), (-0.08, 3.25)], 0.04, 'M_CM_White', -0.6, bulge=0.06))
for s in (-1, 1):
    body.append(flat_shape(f'Lapel{s}', [(s * 0.42, 4.1), (s * 0.95, 4.02), (s * 0.7, 3.62), (s * 0.12, 3.12), (s * 0.1, 3.3)], 0.05, 'M_CM_Ink', -0.64, bulge=0.0))
    body.append(flat_shape(f'Bow{s}', [(s * 0.02, 3.98), (s * 0.3, 4.1), (s * 0.34, 3.86), (s * 0.02, 3.92)], 0.08, 'M_CM_Red', -0.7))
body.append(SP('BowKnot', 1.0, (0, -0.74, 3.95), 'M_CM_Red', segs=14, rings=8, squash=(0.08, 0.06, 0.08)))
body.append(SP('Ruby', 1.0, (0, -0.68, 3.6), 'M_CM_Eye', segs=14, rings=8, squash=(0.06, 0.04, 0.08)))
# gold buttons down the front + a pocket-watch chain
for k in range(3):
    body.append(SP(f'Button{k}', 0.07, (0, -0.56, 3.45 - k * 0.0) if k == 0 else (0.0, -0.53, 1.85 - (k - 1) * 0.0), 'M_CM_Gold', segs=12, rings=8))
pts = [(-0.6 + 0.6 * t, -0.53 - 0.06 * math.sin(t * math.pi), 3.3 - 0.35 * math.sin(t * math.pi)) for t in [k / 12 for k in range(13)]]
curve = bpy.data.curves.new('Chain', 'CURVE'); curve.dimensions = '3D'; curve.bevel_depth = 0.025; curve.bevel_resolution = 2
sp = curve.splines.new('POLY'); sp.points.add(len(pts) - 1)
for k, p in enumerate(pts): sp.points[k].co = (*p, 1)
co = K.link(bpy.data.objects.new('Chain', curve)); bpy.ops.object.select_all(action='DESELECT'); co.select_set(True); bpy.context.view_layer.objects.active = co
bpy.ops.object.convert(target='MESH'); co = bpy.context.view_layer.objects.active; co.data.materials.clear(); co.data.materials.append(K.mat('M_CM_Gold'))
body.append(co)
CM_Body = part('CM_Body', body, (0, 0, 0))

# ===================================================================== PENDULUM (hinge at the top of the window)
pend = [CY('Rod', 0.03, 0.03, 0.95, (0, -0.47, 2.72), 'M_CM_Gold', segs=8, bevel=0)]
bob = rot(CY('Bob', 0.24, 0.24, 0.06, (0, -0.47, 2.2), 'M_CM_Gold', segs=28, bevel=0.015), x=90)
pend += [bob, rot(K.star('BobStar', 0.12, 0.05, 0.03, (0, -0.51, 2.2), 'M_CM_Glow', bevel=0.005), x=0)]
CM_Pendulum = part('CM_Pendulum', pend, (0, -0.47, 3.18))

# ===================================================================== HEAD (dial, mask, crown, top hat)
HZ = 5.1   # dial centre
head = []
dial = rot(CY('Dial', 0.98, 0.98, 0.26, (0, 0, HZ), 'M_CM_Face', segs=48, bevel=0.03), x=90); head.append(dial)
head.append(rot(K.torus('Rim', 1.0, 0.09, (0, -0.06, HZ), 'M_CM_Gold', segs=56, rsegs=10), x=90))
head.append(rot(K.torus('RimBack', 0.98, 0.07, (0, 0.12, HZ), 'M_CM_Ink', segs=48, rsegs=8), x=90))
for k in range(12):   # numerals as bold ticks (XII, III, VI, IX longer)
    a = k / 12 * 2 * math.pi
    big = k % 3 == 0
    t = SB(f'Tick{k}', (0.06 if not big else 0.1, 0.03, 0.2 if big else 0.12), (math.sin(a) * 0.8, -0.14, HZ + math.cos(a) * 0.8), 'M_CM_Ink', bevel=0.01, segs=1)
    head.append(rot(t, y=-math.degrees(a)))
# crown of twelve spikes behind the rim
for k in range(12):
    a = k / 12 * 2 * math.pi
    sp_ = CY(f'Spike{k}', 0.09, 0.0, 0.42, (0, 0, 0), 'M_CM_Gold', segs=8, bevel=0)
    sp_.location = (math.sin(a) * 1.18, 0.08, HZ + math.cos(a) * 1.18)
    rot(sp_, y=math.degrees(a))
    head.append(sp_)
# domino mask: a phantom-thief wing — widow's peak in the middle, blades sweeping up past the rim, white with ink trim
MZ = HZ + 0.18
wing = [(0.0, 0.5), (0.16, 0.3), (0.55, 0.36), (0.95, 0.62), (1.32, 0.98), (1.12, 0.46), (0.86, 0.06), (0.5, -0.16), (0.2, -0.08), (0.0, -0.2)]
outline = [(x, z + MZ) for x, z in wing] + [(-x, z + MZ) for x, z in reversed(wing[1:-1])]
head.append(flat_shape('Mask', outline, 0.09, 'M_CM_White', -0.24, smooth_n=5, bulge=0.12))
trim = [(x * 1.06, (z - 0.17) * 1.08 + 0.17 + MZ) for x, z in wing] + [(-x * 1.06, (z - 0.17) * 1.08 + 0.17 + MZ) for x, z in reversed(wing[1:-1])]
head.append(flat_shape('MaskTrim', trim, 0.06, 'M_CM_Ink', -0.17, smooth_n=5, bulge=0.12))
for s in (-1, 1):   # glowing eye slits: pointed, slanted down toward the nose (menace)
    slit = [(s * 0.14, MZ + 0.04), (s * 0.4, MZ + 0.18), (s * 0.7, MZ + 0.26), (s * 0.62, MZ + 0.1), (s * 0.36, MZ - 0.02)]
    if s < 0: slit = list(reversed(slit))
    head.append(flat_shape(f'Eye{s}', slit, 0.05, 'M_CM_Eye', -0.39, smooth_n=3, bulge=0.0))
# wicked grin across the lower dial: ink crescent with white tooth notches
grin = [(-0.62, HZ - 0.28), (-0.3, HZ - 0.5), (0.0, HZ - 0.56), (0.3, HZ - 0.5), (0.62, HZ - 0.28), (0.3, HZ - 0.38), (0.0, HZ - 0.42), (-0.3, HZ - 0.38)]
head.append(flat_shape('Grin', grin, 0.04, 'M_CM_Ink', -0.17, smooth_n=4))
for k in range(-3, 4):
    x = k * 0.14; zt = HZ - 0.38 - 0.04 * (1 - (x / 0.62) ** 2) - 0.005
    head.append(flat_shape(f'Tooth{k}', [(x - 0.05, zt + 0.005), (x + 0.05, zt + 0.005), (x, zt - 0.09)], 0.02, 'M_CM_White', -0.2, smooth_n=1))
# top hat (tilted), gold band, a minute-hand feather
hat_c = Vector((0.12, 0.05, HZ + 1.0))
brim = CY('HatBrim', 0.72, 0.72, 0.07, hat_c, 'M_CM_Ink', segs=40, bevel=0.03)
crown = CY('HatCrown', 0.46, 0.5, 0.95, hat_c + Vector((0, 0, 0.5)), 'M_CM_Ink', segs=40, bevel=0.04)
band = CY('HatBand', 0.505, 0.505, 0.14, hat_c + Vector((0, 0, 0.12)), 'M_CM_Red', segs=40, bevel=0)
feather = SB('HatFeather', (0.06, 0.04, 0.7), hat_c + Vector((0.42, 0.0, 0.42)), 'M_CM_Gold', bevel=0.02, segs=2)
rot(feather, y=24)
hat = K.join([brim, crown, band, feather], 'Hat')
hat.data.transform(Matrix.Translation(-hat_c)); hat.data.transform(Matrix.Rotation(math.radians(-14), 4, 'Y')); hat.data.transform(Matrix.Translation(hat_c))
head.append(hat)
CM_Head = part('CM_Head', head, (0, 0, HZ))

# clock hands on the dial (pivot = dial centre, pointing to 12 = +Z)
hh = SB('HourHandMesh', (0.1, 0.03, 0.5), (0, -0.2, HZ + 0.22), 'M_CM_Ink', bevel=0.012, segs=1)
hh2 = rot(SB('HourTip', (0.18, 0.03, 0.18), (0, -0.2, HZ + 0.46), 'M_CM_Ink', bevel=0.01, segs=1), y=45)
CM_HourHand = part('CM_HourHand', [hh, hh2], (0, -0.2, HZ))
mh = SB('MinuteHandMesh', (0.06, 0.03, 0.78), (0, -0.22, HZ + 0.36), 'M_CM_Ink', bevel=0.01, segs=1)
cap = rot(CY('Hub', 0.08, 0.08, 0.05, (0, -0.24, HZ), 'M_CM_Gold', segs=16, bevel=0.01), x=90)
CM_MinuteHand = part('CM_MinuteHand', [mh, cap], (0, -0.22, HZ))

# ===================================================================== ARMS (shoulder pivots, hanging down)
def arm(side):
    """bent sleeve (puffed shoulder, elbow out), gold-banded flared cuff, long-fingered glove curled round the grip at (sx, 0, 1.92)."""
    sx = side * 1.42
    el = Vector((sx + side * 0.18, 0.1, 2.95)); wr = Vector((sx, -0.02, 2.2))
    parts = [SP(f'Puff{side}', 1.0, (sx, 0, 3.85), 'M_CM_Coat', segs=20, rings=12, squash=(0.33, 0.33, 0.36))]
    up = loft(f'Upper{side}', [(0, 0.28, 0.28, 0, 0), (1, 0.2, 0.2, 0, 0)], 'M_CM_Coat', segs=16)
    bm = bmesh.new(); bm.from_mesh(up.data)
    for v in bm.verts:   # map z 0..1 onto shoulder -> elbow
        t = v.co.z; c = Vector((sx, 0, 3.85)).lerp(el, t); v.co = Vector((c.x + v.co.x, c.y + v.co.y, c.z))
    bm.to_mesh(up.data); bm.free(); parts.append(up)
    parts.append(SP(f'Elbow{side}', 0.21, tuple(el), 'M_CM_Coat', segs=16, rings=10))
    fo = loft(f'Fore{side}', [(0, 0.2, 0.2, 0, 0), (1, 0.17, 0.17, 0, 0)], 'M_CM_Coat', segs=16)
    bm = bmesh.new(); bm.from_mesh(fo.data)
    for v in bm.verts:
        t = v.co.z; c = el.lerp(wr, t); v.co = Vector((c.x + v.co.x, c.y + v.co.y, c.z))
    bm.to_mesh(fo.data); bm.free(); parts.append(fo)
    parts.append(loft(f'Cuff{side}', [(2.42, 0.2, 0.2, sx, -0.02), (2.16, 0.32, 0.32, sx, -0.02), (2.1, 0.3, 0.3, sx, -0.02)], 'M_CM_White', segs=20))
    parts.append(K.torus(f'CuffBand{side}', 0.24, 0.035, (sx, -0.02, 2.36), 'M_CM_Gold', segs=24, rsegs=6))
    parts.append(SP(f'Glove{side}', 1.0, (sx, 0, 1.95), 'M_CM_White', segs=20, rings=12, squash=(0.26, 0.22, 0.26)))
    for k in range(4):   # long fingers wrapped forward around the grip, pointed tips
        x = sx + (k - 1.5) * 0.11
        pts = [(x, -0.08, 1.98), (x, -0.26, 1.92), (x, -0.24, 1.76), (x, -0.08, 1.72)]
        parts.append(loft(f'Finger{side}{k}', [(0, 0.055, 0.055, 0, 0), (1, 0.035, 0.035, 0, 0)], 'M_CM_White', segs=8))
        f = parts[-1]; bm = bmesh.new(); bm.from_mesh(f.data)
        for v in bm.verts:
            t = v.co.z; i = min(int(t * 3), 2); u = t * 3 - i
            c = Vector(pts[i]).lerp(Vector(pts[i + 1]), u); v.co = Vector((c.x + v.co.x, c.y + v.co.y * 0.6, c.z + v.co.y * 0.8))
        bm.to_mesh(f.data); bm.free()
    parts.append(SP(f'Thumb{side}', 1.0, (sx - side * 0.2, -0.16, 2.02), 'M_CM_White', segs=12, rings=8, squash=(0.07, 0.1, 0.07)))
    return part(f'CM_Arm_{"L" if side < 0 else "R"}', parts, (sx, 0, 3.95))

CM_Arm_L = arm(-1); CM_Arm_R = arm(1)

# ===================================================================== BLADES (grip at the glove, blade toward +Y)
def blade_hour():
    gx = 1.42; g = Vector((gx, 0, 1.92))
    parts = [CY('GripH', 0.07, 0.07, 0.6, (0, 0, 0), 'M_CM_Ink', segs=12, bevel=0.01)]
    rot(parts[0], x=90); parts[0].location = g + Vector((0, -0.05, 0))
    parts.append(rot(K.torus('Guard', 0.22, 0.05, g + Vector((0, 0.3, 0)), 'M_CM_Gold', segs=24, rsegs=8), x=90))
    # broad clock-hand blade: long tapered slab + a hollow spade near the tip
    bl = SB('BladeH', (0.36, 3.3, 0.1), g + Vector((0, 2.05, 0)), 'M_CM_BladeH', bevel=0.03, segs=2)
    bm = bmesh.new(); bm.from_mesh(bl.data)
    for v in bm.verts:
        t = (v.co.y + 1.65) / 3.3; v.co.x *= 1.0 - 0.35 * t
    bm.to_mesh(bl.data); bm.free(); parts.append(bl)
    spade = SP('Spade', 1.0, g + Vector((0, 3.9, 0)), 'M_CM_BladeH', segs=24, rings=12, squash=(0.62, 0.62, 0.09))
    hole = SP('SpadeHole', 1.0, g + Vector((0, 3.85, 0)), 'M_CM_BladeH', segs=24, rings=12, squash=(0.32, 0.32, 0.5))
    b = spade.modifiers.new('bool', 'BOOLEAN'); b.operation = 'DIFFERENCE'; b.object = hole; b.solver = 'EXACT'; K.apply_mods(spade); bpy.data.objects.remove(hole)
    parts.append(spade)
    tip = SB('TipH', (0.4, 0.4, 0.1), g + Vector((0, 4.55, 0)), 'M_CM_BladeH', bevel=0.02, segs=1); rot(tip, z=45); parts.append(tip)
    parts.append(SB('FullerH', (0.08, 2.9, 0.12), g + Vector((0, 1.95, 0)), 'M_CM_Glow', bevel=0.02, segs=1))
    return part('CM_Blade_Hour', parts, g)

def blade_minute():
    gx = -1.42; g = Vector((gx, 0, 1.92))
    parts = [rot(CY('GripM', 0.06, 0.06, 0.55, g + Vector((0, -0.05, 0)), 'M_CM_Ink', segs=12, bevel=0.01), x=90)]
    parts.append(rot(K.torus('GuardM', 0.17, 0.04, g + Vector((0, 0.28, 0)), 'M_CM_Gold', segs=20, rsegs=8), x=90))
    bl = SB('BladeM', (0.16, 5.0, 0.07), g + Vector((0, 2.85, 0)), 'M_CM_BladeM', bevel=0.02, segs=2); parts.append(bl)
    tip = SB('TipM', (0.36, 0.36, 0.07), g + Vector((0, 5.42, 0)), 'M_CM_BladeM', bevel=0.02, segs=1); rot(tip, z=45); parts.append(tip)
    parts.append(SB('FullerM', (0.05, 4.6, 0.09), g + Vector((0, 2.75, 0)), 'M_CM_Glow', bevel=0.015, segs=1))
    return part('CM_Blade_Minute', parts, g)

CM_Blade_Hour = blade_hour(); CM_Blade_Minute = blade_minute()

# ===================================================================== CAPE (night sky outside, star lining)
cape = []
outer = loft('CapeOuter', [(4.25, 1.15, 0.3, 0, 0.62), (3.0, 1.45, 0.34, 0, 0.82), (1.6, 1.7, 0.4, 0, 1.0), (0.55, 1.85, 0.42, 0, 1.12)], 'M_CM_Cape', segs=28, cap=False)
bm = bmesh.new(); bm.from_mesh(outer.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < 0.55], context='FACES')   # open at the front: a half shell behind him
for v in bm.verts:
    v.co.z += 0.12 * math.sin(v.co.x * 3.0)    # wavy hem
bm.to_mesh(outer.data); bm.free()
sol = outer.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.06; K.apply_mods(outer)
cape.append(outer)
collar = loft('HighCollar', [(4.05, 0.75, 0.42, 0, 0.3), (4.95, 0.95, 0.5, 0, 0.42)], 'M_CM_Cape', segs=24, cap=False)
bm = bmesh.new(); bm.from_mesh(collar.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < 0.15], context='FACES'); bm.to_mesh(collar.data); bm.free()
sol = collar.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.05; K.apply_mods(collar)
cape.append(collar)
r = random.Random(12)
RINGS = [(4.25, 1.15, 0.3, 0.62), (3.0, 1.45, 0.34, 0.82), (1.6, 1.7, 0.4, 1.0), (0.55, 1.85, 0.42, 1.12)]
def ring_at(z):
    for (z0, rx0, ry0, cy0), (z1, rx1, ry1, cy1) in zip(RINGS, RINGS[1:]):
        if z1 <= z <= z0:
            t = (z0 - z) / (z0 - z1); return rx0 + (rx1 - rx0) * t, ry0 + (ry1 - ry0) * t, cy0 + (cy1 - cy0) * t
    return RINGS[-1][1:]
for k in range(30):   # stars on the outside of the night-sky cape
    a = r.uniform(0.25, math.pi - 0.25); z = r.uniform(0.9, 3.9)
    rx, ry, cy = ring_at(z)
    p = (math.cos(a) * rx * 1.02, cy + math.sin(a) * ry + 0.05, z)
    st = K.star(f'Star{k}', r.uniform(0.05, 0.11), 0.025, 0.02, p, 'M_CM_Glow', bevel=0.004)
    cape.append(rot(st, z=math.degrees(-a) + 90 + r.uniform(-15, 15)))
CM_Cape = part('CM_Cape', cape, (0, 0.5, 4.05))

# export everything in one FBX (flat; Unity CountMidnightSetup re-parents by name)
K.export('CM_CountMidnight')
print('COUNT MIDNIGHT DONE')
