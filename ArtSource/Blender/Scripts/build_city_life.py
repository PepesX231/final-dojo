"""
M16 city-life kit for the Midnight Palace (user: "the city looks dead"). Low-poly, readable at distance, made to be
instanced by the hundreds: hover car, pedestrian (sleepy dreamer), palm, food kiosk, traffic light, monorail car,
bench, bin. Fronts face Blender −Y (= Unity +Z). Tinted per instance in Unity via material names (CarPaint, Pyjama).
run: python3 -I build_city_life.py
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def tube(name, pts, radii, mat, segs=8):
    bm = bmesh.new(); rings = []
    for i, q in enumerate(pts):
        q = Vector(q); d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a).normalized()
        rings.append([bm.verts.new(q + (a * math.cos(t) + b * math.sin(t)) * radii[i]) for t in [k / segs * 2 * math.pi for k in range(segs)]])
    for i in range(len(rings) - 1):
        for k in range(segs): bm.faces.new((rings[i][k], rings[i][(k + 1) % segs], rings[i + 1][(k + 1) % segs], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return K.from_bm(name, bm, mat)


def frond(name, base, ang, length, mat):
    """a flat drooping palm leaf: a strip of quads bending down, wide in the middle."""
    bm = bmesh.new(); n = 7; prev = None
    dx, dy = math.cos(ang), math.sin(ang)
    for i in range(n + 1):
        t = i / n; w = math.sin(t * math.pi) * 0.42 + 0.03
        p = Vector(base) + Vector((dx * length * t, dy * length * t, 0.6 * math.sin(t * 1.6) - 1.6 * t * t))
        side = Vector((-dy, dx, 0)) * w
        a = bm.verts.new(p - side + Vector((0, 0, w * 0.25))); b = bm.verts.new(p + side + Vector((0, 0, w * 0.25)))
        if prev: bm.faces.new((prev[0], prev[1], b, a))
        prev = (a, b)
    return K.from_bm(name, bm, mat)


# ------------------------------------------------------------------ hover car (4.4 m)
K.reset(); p = []
p.append(SB('Body', (2.0, 4.4, 0.62), (0, 0, 0.62), 'M_Kit_MP_CarPaint', bevel=0.22, segs=3))
p.append(SB('Nose', (1.9, 1.3, 0.4), (0, -1.9, 0.5), 'M_Kit_MP_CarPaint', bevel=0.18, segs=3))
p.append(SB('Cabin', (1.55, 2.0, 0.55), (0, 0.25, 1.15), 'M_Kit_MP_CarGlass', bevel=0.25, segs=3))
p.append(SB('Spoiler', (1.9, 0.35, 0.08), (0, 2.25, 1.15), 'M_Kit_MP_Dark', bevel=0.03, segs=1))
for s in (-1, 1):
    p.append(SB(f'Fin{s}', (0.08, 0.3, 0.3), (s * 0.8, 2.25, 1.0), 'M_Kit_MP_Dark', bevel=0.02, segs=1))
    p.append(SB(f'Head{s}', (0.55, 0.06, 0.12), (s * 0.55, -2.52, 0.62), 'M_Kit_MP_HeadLight', bevel=0.03, segs=1))
    p.append(SB(f'Pod{s}', (0.5, 1.1, 0.32), (s * 1.05, -1.2, 0.32), 'M_Kit_MP_Dark', bevel=0.12, segs=2))
    p.append(SB(f'PodR{s}', (0.5, 1.1, 0.32), (s * 1.05, 1.5, 0.32), 'M_Kit_MP_Dark', bevel=0.12, segs=2))
p.append(SB('Tail', (1.85, 0.06, 0.1), (0, 2.22, 0.78), 'M_Kit_MP_TailLight', bevel=0.02, segs=1))
p.append(SB('Under', (1.6, 3.6, 0.06), (0, 0, 0.2), 'M_Kit_MP_Glow', bevel=0.02, segs=1))
K.join(p, 'CAR_Hover'); K.export('CAR_Hover')

# ------------------------------------------------------------------ pedestrian: a sleepy grown-up dreamer (1.6 m, low poly)
K.reset(); p = []
p.append(tube('Legs', [(0, 0, 0.05), (0, 0, 0.5)], [0.17, 0.2], 'M_Kit_MP_Dark', segs=8))
p.append(SP('Body', 1.0, (0, 0, 0.82), 'M_Kit_MP_Pyjama', segs=12, rings=8, squash=(0.3, 0.24, 0.42)))
p.append(CY('Head', 0.24, 0.24, 0.14, (0, 0, 1.42), 'M_Kit_MP_Gold', segs=14, bevel=0.03)); rot(p[-1], x=90)
p.append(CY('Face', 0.2, 0.2, 0.02, (0, -0.08, 1.42), 'M_Kit_MP_Face', segs=14, bevel=0)); rot(p[-1], x=90)
p.append(SB('Hand1', (0.03, 0.01, 0.15), (0, -0.095, 1.47), 'M_Kit_MP_Dark', bevel=0.005, segs=1))
p.append(tube('Cap', [(0, 0.03, 1.6), (0.08, 0.06, 1.78), (0.2, 0.08, 1.74)], [0.17, 0.08, 0.03], 'M_Kit_MP_Pyjama', segs=8))
for s in (-1, 1):
    p.append(tube(f'Arm{s}', [(s * 0.27, 0, 1.05), (s * 0.34, -0.02, 0.78), (s * 0.33, -0.05, 0.62)], [0.07, 0.06, 0.055], 'M_Kit_MP_Pyjama', segs=6))
    p.append(SB(f'Shoe{s}', (0.13, 0.26, 0.09), (s * 0.1, -0.05, 0.045), 'M_Kit_MP_Feather', bevel=0.04, segs=1))
K.join(p, 'PED_Dreamer'); K.export('PED_Dreamer')

# ------------------------------------------------------------------ palm (7.5 m)
K.reset(); p = []
pts = [(0, 0, 0), (0.15, 0, 2.5), (0.45, 0.05, 5.0), (0.85, 0.1, 7.2)]
p.append(tube('Trunk', pts, [0.28, 0.22, 0.18, 0.15], 'M_Kit_MP_Trunk', segs=8))
for k in range(4): p.append(K.torus(f'Ring{k}', 0.24 - k * 0.025, 0.04, (0.06 + k * 0.2, 0.0, 1.0 + k * 1.6), 'M_Kit_MP_Trunk', segs=10, rsegs=4))
top = (0.85, 0.1, 7.2)
for k in range(9): p.append(frond(f'Frond{k}', top, k * 2 * math.pi / 9 + 0.2, 3.2 + (k % 3) * 0.4, 'M_Kit_MP_Leaf'))
p.append(SP('Nuts', 0.25, (0.85, 0.1, 7.0), 'M_Kit_MP_Trunk', segs=8, rings=6))
K.join(p, 'PALM_Tree'); K.export('PALM_Tree')

# ------------------------------------------------------------------ food kiosk (2.4 × 1.8 × 2.9)
K.reset(); p = []
p.append(SB('Booth', (2.4, 1.6, 2.2), (0, 0, 1.1), 'M_Kit_MP_CarPaint', bevel=0.08, segs=2))
p.append(SB('Window', (2.0, 0.06, 0.9), (0, -0.81, 1.45), 'M_Kit_MP_Window', bevel=0.02, segs=1))
p.append(SB('Counter', (2.3, 0.45, 0.08), (0, -0.98, 1.0), 'M_Kit_MP_Metal', bevel=0.02, segs=1))
p.append(SB('Menu', (1.8, 0.06, 0.3), (0, -0.84, 2.02), 'M_Kit_MP_Screen', bevel=0.02, segs=1))
aw = SB('Awning', (2.7, 1.0, 0.08), (0, -1.25, 2.25), 'M_Kit_MP_Red', bevel=0.03, segs=1); rot(aw, x=-14); p.append(aw)
p.append(SB('Roof', (2.6, 1.8, 0.2), (0, 0, 2.3), 'M_Kit_MP_Dark', bevel=0.05, segs=1))
p.append(SB('Sign', (1.6, 0.4, 0.5), (0, 0, 2.65), 'M_Kit_MP_Screen', bevel=0.05, segs=1))
for s in (-1, 1): p.append(CY(f'Stool{s}', 0.2, 0.2, 0.7, (s * 0.7, -1.6, 0.35), 'M_Kit_MP_Metal', segs=10, bevel=0.02))
K.join(p, 'KIOSK_Food'); K.export('KIOSK_Food')

# ------------------------------------------------------------------ traffic light (arm over the road, 6 m)
K.reset(); p = []
p.append(CY('Pole', 0.12, 0.1, 6.0, (0, 0, 3.0), 'M_Kit_MP_Dark', segs=10, bevel=0.0))
p.append(SB('Arm', (0.15, 4.0, 0.15), (0, -2.0, 5.8), 'M_Kit_MP_Dark', bevel=0.03, segs=1))
for j, (y, m) in enumerate(((-2.2, 'M_Kit_MP_LightRed'), (-3.6, 'M_Kit_MP_LightGreen'))):
    p.append(SB(f'Box{j}', (0.45, 0.4, 1.2), (0, y, 5.25), 'M_Kit_MP_Dark', bevel=0.05, segs=1))
    p.append(SP(f'L{j}', 0.13, (0, y - 0.21, 5.6), m, segs=10, rings=6))
    p.append(SP(f'Y{j}', 0.13, (0, y - 0.21, 5.25), 'M_Kit_MP_Dark', segs=10, rings=6))
    p.append(SP(f'G{j}', 0.13, (0, y - 0.21, 4.9), 'M_Kit_MP_Dark', segs=10, rings=6))
p.append(SB('Walk', (0.35, 0.3, 0.45), (0, -0.22, 2.6), 'M_Kit_MP_LightGreen', bevel=0.04, segs=1))
p.append(SB('Street', (1.4, 0.06, 0.35), (0, -1.2, 6.2), 'M_Kit_MP_Screen', bevel=0.03, segs=1))
K.join(p, 'TRAFFIC_Light'); K.export('TRAFFIC_Light')

# ------------------------------------------------------------------ monorail car (12 m, pivot at the rail top)
K.reset(); p = []
p.append(SB('Shell', (2.6, 12.0, 2.6), (0, 0, 1.6), 'M_Kit_MP_Mask', bevel=0.8, segs=4))
p.append(SB('Stripe', (2.66, 11.2, 0.35), (0, 0, 1.25), 'M_Kit_MP_Train', bevel=0.12, segs=2))
p.append(SB('Windows', (2.68, 10.0, 0.6), (0, 0, 2.0), 'M_Kit_MP_Window', bevel=0.12, segs=2))
p.append(SB('Bogie', (1.2, 9.0, 0.5), (0, 0, 0.2), 'M_Kit_MP_Dark', bevel=0.1, segs=1))
p.append(SB('Head', (2.0, 0.08, 0.3), (0, -6.0, 1.5), 'M_Kit_MP_HeadLight', bevel=0.04, segs=1))
K.join(p, 'MONO_Car'); K.export('MONO_Car')

# ------------------------------------------------------------------ bench + bin
K.reset(); p = []
p.append(SB('Seat', (1.8, 0.5, 0.08), (0, 0, 0.45), 'M_Kit_MP_Wood', bevel=0.03, segs=1))
p.append(SB('Back', (1.8, 0.08, 0.4), (0, 0.24, 0.75), 'M_Kit_MP_Wood', bevel=0.03, segs=1))
for s in (-1, 1): p.append(SB(f'Leg{s}', (0.08, 0.5, 0.45), (s * 0.8, 0, 0.22), 'M_Kit_MP_Dark', bevel=0.02, segs=1))
p.append(CY('Bin', 0.28, 0.24, 0.85, (1.4, 0, 0.42), 'M_Kit_MP_Train', segs=12, bevel=0.03))
p.append(CY('BinLid', 0.3, 0.3, 0.06, (1.4, 0, 0.88), 'M_Kit_MP_Dark', segs=12, bevel=0.02))
K.join(p, 'BENCH_Bin'); K.export('BENCH_Bin')
