"""
M21 Midnight Palace interior kit (user: "the rooms are dumb low-poly boxes; outside and inside don't match").
Stylized but detailed: chunky bevels, soft cushions, real silhouettes (crown mouldings, turned legs, book rows,
curtained beds), the same flat palette as the illustrated v3 facades (cream / wood / teal / rose / lilac / mustard / ink).
Fronts face Blender -Y (= Unity +Z, the yaw the room code already uses). Origin at the floor, centred.
run: python3 -I build_interior_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere
R = random.Random(2121)
W, WD, CR, TL, RO, LI, MU, INK, LF, GL = ('M_Kit_MP_IntWood', 'M_Kit_MP_IntWoodDark', 'M_Kit_MP_IntCream', 'M_Kit_MP_IntTeal', 'M_Kit_MP_IntRose',
                                          'M_Kit_MP_IntLilac', 'M_Kit_MP_IntMustard', 'M_Kit_MP_IntInk', 'M_Kit_MP_IntLeaf', 'M_Kit_MP_IntGlass')
BRASS, FACE, GLOW, CAKE, TAT, COP = 'M_Kit_MP_Brass', 'M_Kit_MP_Face', 'M_Kit_MP_LampGlow', 'M_Kit_MP_IntCake', 'M_Kit_MP_IntTatami', 'M_Kit_MP_IntCopper'
BOOKS = [TL, RO, LI, MU, CR, WD]


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def pbox(n, size, c, m, bev):
    """cheap box (8 verts + one bevel segment) for the many small parts — soft_box subdivides and would cost 50k tris a bookcase."""
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    o = K.from_bm(n, bm, m); o.location = c
    if bev > 0.0005:
        b = o.modifiers.new('bev', 'BEVEL'); b.width = min(bev, min(size) * 0.45); b.segments = 1; b.limit_method = 'ANGLE'
        K.apply_mods(o)
    return o


def box(p, n, size, c, m, bev=0.02, segs=2, **kw):
    if segs <= 1 and not kw: p.append(pbox(n, size, c, m, bev))
    else: p.append(SB(n, size, c, m, bevel=max(bev, 0.002), segs=segs, **kw))
    return p[-1]


def leg(p, n, x, y, h, r=0.035, m=WD, turned=True):
    p.append(CY(n, r, r * 0.7, h, (x, y, h / 2), m, segs=10, bevel=0.01))
    if turned: p.append(SP(n + 'B', r * 1.5, (x, y, h * 0.55), m, segs=10, rings=6))


def finish(p, name):
    K.join(p, name); K.export(name)


def books_row(p, x0, x1, y, z, depth=0.26, hmax=0.36, lean=True):
    x = x0
    while x < x1 - 0.04:
        if R.random() < 0.12 and x < x1 - 0.3:   # a lying stack
            for k in range(R.randint(2, 4)):
                box(p, 'Stack', (0.26, depth * 0.85, 0.045), (x + 0.13, y, z + 0.024 + k * 0.047), R.choice(BOOKS), bev=0.008, segs=1)
            x += 0.3; continue
        w = R.uniform(0.035, 0.07); h = R.uniform(0.62, 1.0) * hmax
        b = box(p, 'Book', (w, depth, h), (x + w / 2, y, z + h / 2), R.choice(BOOKS), bev=0.006, segs=1)
        if lean and R.random() < 0.06 and x + w + 0.12 < x1: rot(b, y=14); x += 0.06
        x += w + 0.004


# ------------------------------------------------------------------ BOOKCASE_Tall (2.0 x 0.42 x 2.6)
K.reset(); p = []
box(p, 'Back', (2.0, 0.04, 2.5), (0, 0.19, 1.25), WD)
for s in (-1, 1): box(p, 'Side', (0.07, 0.42, 2.5), (s * 0.965, 0, 1.25), W, bev=0.015)
box(p, 'Plinth', (2.06, 0.46, 0.14), (0, -0.01, 0.07), WD)
box(p, 'Top', (2.12, 0.48, 0.08), (0, -0.01, 2.54), W, bev=0.02)
box(p, 'Crown', (2.2, 0.52, 0.06), (0, -0.02, 2.6), CR, bev=0.02)
for k, z in enumerate((0.14, 0.62, 1.1, 1.58, 2.06)):
    box(p, 'Shelf', (1.9, 0.4, 0.04), (0, 0, z), W, bev=0.01, segs=1)
    box(p, 'Lip', (1.9, 0.02, 0.05), (0, -0.2, z + 0.01), CR, bev=0.005, segs=1)
    if k < 4: books_row(p, -0.92, 0.92, 0.02, z + 0.02)
p.append(SP('Globe', 0.14, (0.55, 0.0, 2.73), TL, segs=16, rings=10))
p.append(CY('GlobeStand', 0.08, 0.05, 0.06, (0.55, 0, 2.62), BRASS, segs=12, bevel=0.01))
box(p, 'TopStack', (0.3, 0.22, 0.12), (-0.5, 0, 2.66), RO, bev=0.01, segs=1)
finish(p, 'INT_Bookcase')

# ------------------------------------------------------------------ ARMCHAIR (0.95 x 0.9 x 0.95)
def chair(name, width, fabric, accent):
    K.reset(); p = []
    box(p, 'Base', (width, 0.82, 0.32), (0, 0, 0.3), fabric, bev=0.1, segs=3)
    box(p, 'Seat', (width - 0.3, 0.66, 0.16), (0, -0.04, 0.52), fabric, bev=0.07, segs=3, puff=0.25)
    box(p, 'Back', (width - 0.12, 0.2, 0.62), (0, 0.32, 0.78), fabric, bev=0.09, segs=3, puff=0.2)
    for s in (-1, 1):
        box(p, 'Arm', (0.16, 0.8, 0.3), (s * (width / 2 - 0.08), 0, 0.6), fabric, bev=0.07, segs=3)
        p.append(CY('Roll', 0.1, 0.1, 0.8, (s * (width / 2 - 0.08), 0, 0.74), fabric, segs=14, bevel=0.02)); rot(p[-1], x=90)
        for t in (-1, 1): leg(p, 'Leg', s * (width / 2 - 0.12), t * 0.32, 0.16, 0.03, WD, turned=False)
    box(p, 'Pillow', (0.36, 0.12, 0.3), (width * 0.18, 0.18, 0.78), accent, bev=0.05, segs=3, puff=0.35)
    rot(p[-1], z=-12, x=-10)
    finish(p, name)
chair('INT_Armchair', 0.95, RO, MU)
chair('INT_Sofa', 2.1, TL, CR)

# ------------------------------------------------------------------ FLOOR_Lamp (1.75 m)
K.reset(); p = []
p.append(CY('Foot', 0.2, 0.22, 0.05, (0, 0, 0.025), INK, segs=20, bevel=0.015))
p.append(CY('Pole', 0.018, 0.018, 1.5, (0, 0, 0.8), BRASS, segs=8, bevel=0))
p.append(CY('Shade', 0.16, 0.27, 0.3, (0, 0, 1.6), CR, segs=20, bevel=0.01))
p.append(SP('Bulb', 0.07, (0, 0, 1.5), GLOW, segs=10, rings=6))
finish(p, 'INT_FloorLamp')

# ------------------------------------------------------------------ SIDE_Table (round, books + cup)
K.reset(); p = []
p.append(CY('Top', 0.32, 0.32, 0.04, (0, 0, 0.58), W, segs=24, bevel=0.012))
p.append(CY('Stem', 0.035, 0.05, 0.54, (0, 0, 0.29), WD, segs=10, bevel=0))
p.append(CY('Foot', 0.2, 0.22, 0.04, (0, 0, 0.02), WD, segs=18, bevel=0.01))
for k in range(3): box(p, 'Book', (0.24 - k * 0.02, 0.17, 0.04), (-0.05, 0.02, 0.62 + k * 0.042), BOOKS[k], bev=0.006, segs=1)
p.append(CY('Cup', 0.045, 0.04, 0.08, (0.17, -0.06, 0.64), CR, segs=12, bevel=0.005))
finish(p, 'INT_SideTable')

# ------------------------------------------------------------------ CAFE_Counter (3.2 x 0.8 x 1.05) with espresso machine, cake dome, register
K.reset(); p = []
box(p, 'Body', (3.2, 0.72, 0.98), (0, 0.04, 0.49), CR, bev=0.03)
for i in range(16): box(p, 'Slat', (0.16, 0.03, 0.8), (-1.5 + i * 0.2, -0.33, 0.48), TL if i % 2 == 0 else CR, bev=0.01, segs=1)
box(p, 'Kick', (3.22, 0.05, 0.08), (0, -0.33, 0.04), INK, bev=0.01, segs=1)
box(p, 'Top', (3.36, 0.86, 0.06), (0, 0.0, 1.01), WD, bev=0.02)
# espresso machine
box(p, 'Esp', (0.62, 0.42, 0.42), (-0.9, 0.12, 1.25), BRASS, bev=0.06, segs=3)
box(p, 'EspTop', (0.56, 0.38, 0.05), (-0.9, 0.12, 1.49), INK, bev=0.015, segs=1)
for s in (-1, 1):
    p.append(CY('Group', 0.05, 0.04, 0.08, (-0.9 + s * 0.15, -0.12, 1.16), INK, segs=10, bevel=0.005))
    p.append(CY('Cup', 0.04, 0.035, 0.06, (-0.9 + s * 0.15, -0.14, 1.07), CR, segs=10, bevel=0.005))
p.append(CY('Gauge', 0.05, 0.05, 0.02, (-0.9, -0.1, 1.38), FACE, segs=12, bevel=0.005)); rot(p[-1], x=90)
# cake dome
p.append(CY('Plate', 0.25, 0.27, 0.03, (0.4, -0.05, 1.06), CR, segs=24, bevel=0.01))
for k, (dx, dy) in enumerate(((-0.08, 0), (0.09, 0.05), (0.02, -0.09))):
    p.append(CY('Cake', 0.08, 0.08, 0.1, (0.4 + dx, -0.05 + dy, 1.13), CAKE if k != 1 else MU, segs=14, bevel=0.015))
    p.append(SP('Cherry', 0.022, (0.4 + dx, -0.05 + dy, 1.2), RO, segs=8, rings=5))
p.append(SP('Dome', 0.26, (0.4, -0.05, 1.07), GL, segs=20, rings=10, squash=(1, 1, 1.05)))
p.append(SP('Knob', 0.03, (0.4, -0.05, 1.35), BRASS, segs=8, rings=5))
# register + cookie jar + cups stack
box(p, 'Register', (0.36, 0.3, 0.2), (1.25, 0.1, 1.14), TL, bev=0.03, segs=2)
box(p, 'RegScreen', (0.24, 0.04, 0.12), (1.25, 0.02, 1.3), INK, bev=0.01, segs=1); rot(p[-1], x=-20)
p.append(CY('Jar', 0.1, 0.1, 0.2, (0.95, -0.15, 1.14), GL, segs=14, bevel=0.02))
p.append(CY('JarLid', 0.105, 0.105, 0.03, (0.95, -0.15, 1.26), RO, segs=14, bevel=0.01))
for k in range(4): p.append(CY('Stack', 0.045, 0.04, 0.05, (-0.35, 0.2, 1.065 + k * 0.05), CR if k % 2 else TL, segs=12, bevel=0.005))
finish(p, 'INT_CafeCounter')

# ------------------------------------------------------------------ BISTRO table + chair
K.reset(); p = []
p.append(CY('Top', 0.42, 0.42, 0.04, (0, 0, 0.74), W, segs=28, bevel=0.015))
p.append(CY('Rim', 0.43, 0.43, 0.015, (0, 0, 0.715), INK, segs=28, bevel=0))
p.append(CY('Stem', 0.03, 0.03, 0.7, (0, 0, 0.37), INK, segs=10, bevel=0))
for a in range(3):
    t = a / 3 * 2 * math.pi
    box(p, 'Foot', (0.38, 0.05, 0.03), (math.cos(t) * 0.17, math.sin(t) * 0.17, 0.02), INK, bev=0.01, segs=1); rot(p[-1], z=math.degrees(t))
p.append(CY('Cup', 0.045, 0.04, 0.07, (0.12, -0.08, 0.79), CR, segs=12, bevel=0.005))
p.append(CY('Saucer', 0.08, 0.08, 0.01, (0.12, -0.08, 0.765), CR, segs=14, bevel=0))
p.append(CY('Vase', 0.04, 0.03, 0.12, (-0.14, 0.1, 0.82), TL, segs=10, bevel=0.005))
p.append(SP('Flower', 0.05, (-0.14, 0.1, 0.92), RO, segs=10, rings=6))
finish(p, 'INT_BistroTable')
K.reset(); p = []
p.append(CY('Seat', 0.22, 0.22, 0.05, (0, 0, 0.47), W, segs=20, bevel=0.015))
p.append(CY('Cushion', 0.2, 0.19, 0.04, (0, 0, 0.51), RO, segs=20, bevel=0.015))
for a in range(4):
    t = a / 4 * 2 * math.pi + math.pi / 4
    p.append(CY('Leg', 0.018, 0.014, 0.46, (math.cos(t) * 0.15, math.sin(t) * 0.15, 0.23), INK, segs=8, bevel=0))
p.append(K.torus('BackLoop', 0.2, 0.018, (0, 0.2, 0.72), INK, segs=24, rsegs=6)); rot(p[-1], x=90)
for s in (-1, 1): p.append(CY('Post', 0.016, 0.016, 0.26, (s * 0.17, 0.19, 0.6), INK, segs=8, bevel=0))
finish(p, 'INT_BistroChair')

# ------------------------------------------------------------------ BED_Cozy (1.7 x 2.2)
K.reset(); p = []
box(p, 'Frame', (1.7, 2.15, 0.3), (0, 0, 0.25), W, bev=0.04)
box(p, 'Headboard', (1.8, 0.12, 0.9), (0, 1.08, 0.7), WD, bev=0.05, segs=3)
p.append(CY('HeadArc', 0.9, 0.9, 0.12, (0, 1.08, 1.0), WD, segs=32, bevel=0.03)); rot(p[-1], x=90)
box(p, 'Foot', (1.8, 0.1, 0.5), (0, -1.08, 0.35), WD, bev=0.04)
box(p, 'Mattress', (1.6, 2.05, 0.22), (0, 0, 0.5), CR, bev=0.08, segs=3)
box(p, 'Quilt', (1.68, 1.4, 0.12), (0, -0.33, 0.62), LI, bev=0.06, segs=3, puff=0.4)
box(p, 'Fold', (1.68, 0.3, 0.1), (0, 0.38, 0.66), CR, bev=0.05, segs=3, puff=0.3)
for s in (-1, 1): box(p, 'Pillow', (0.62, 0.38, 0.16), (s * 0.38, 0.78, 0.7), CR, bev=0.07, segs=3, puff=0.45)
box(p, 'Throw', (0.5, 0.5, 0.05), (0.45, -0.6, 0.7), MU, bev=0.03, segs=2); rot(p[-1], z=20)
p.append(SP('Moon', 0.1, (0, 1.12, 1.68), MU, segs=12, rings=8, squash=(1, 0.4, 1)))
finish(p, 'INT_Bed')

# ------------------------------------------------------------------ NIGHTSTAND + lamp
K.reset(); p = []
box(p, 'Body', (0.55, 0.42, 0.55), (0, 0, 0.35), W, bev=0.03)
for k in range(2):
    box(p, 'Drawer', (0.47, 0.03, 0.2), (0, -0.215, 0.24 + k * 0.23), CR, bev=0.015, segs=1)
    p.append(SP('Knob', 0.022, (0, -0.24, 0.24 + k * 0.23), BRASS, segs=8, rings=5))
for x in (-0.22, 0.22):
    for y in (-0.16, 0.16): leg(p, 'Leg', x, y, 0.08, 0.025, WD, turned=False)
p.append(CY('LampBase', 0.07, 0.09, 0.18, (0.1, 0.02, 0.72), TL, segs=14, bevel=0.02))
p.append(CY('LampShade', 0.09, 0.15, 0.16, (0.1, 0.02, 0.9), CR, segs=16, bevel=0.01))
p.append(SP('LampBulb', 0.04, (0.1, 0.02, 0.84), GLOW, segs=8, rings=5))
box(p, 'Book', (0.18, 0.14, 0.04), (-0.12, -0.02, 0.65), RO, bev=0.006, segs=1)
finish(p, 'INT_Nightstand')

# ------------------------------------------------------------------ WARDROBE (1.4 x 0.6 x 2.2)
K.reset(); p = []
box(p, 'Body', (1.4, 0.6, 2.05), (0, 0, 1.15), W, bev=0.03)
box(p, 'Crown', (1.5, 0.68, 0.1), (0, -0.01, 2.22), CR, bev=0.03)
for s in (-1, 1):
    box(p, 'Door', (0.66, 0.03, 1.9), (s * 0.345, -0.305, 1.15), W, bev=0.015, segs=1)
    box(p, 'Inset', (0.5, 0.02, 0.75), (s * 0.345, -0.325, 1.6), CR, bev=0.01, segs=1)
    box(p, 'Inset2', (0.5, 0.02, 0.75), (s * 0.345, -0.325, 0.7), CR, bev=0.01, segs=1)
    p.append(CY('Handle', 0.015, 0.015, 0.22, (s * 0.06, -0.34, 1.15), BRASS, segs=8, bevel=0))
    for t in (-1, 1): leg(p, 'Leg', s * 0.6, t * 0.22, 0.12, 0.035, WD, turned=False)
box(p, 'HatBox', (0.5, 0.4, 0.25), (-0.3, 0, 2.4), RO, bev=0.04, segs=2)
finish(p, 'INT_Wardrobe')

# ------------------------------------------------------------------ DESK_Study (1.4 x 0.7) with chair, lamp, papers
K.reset(); p = []
box(p, 'Top', (1.4, 0.7, 0.05), (0, 0, 0.76), W, bev=0.015)
box(p, 'Drawers', (0.42, 0.62, 0.66), (0.46, 0, 0.4), CR, bev=0.02)
for k in range(3): box(p, 'Drawer', (0.36, 0.02, 0.18), (0.46, -0.32, 0.18 + k * 0.21), TL, bev=0.008, segs=1); p.append(SP('Knob', 0.018, (0.46, -0.34, 0.18 + k * 0.21), BRASS, segs=8, rings=5))
leg(p, 'Leg', -0.64, -0.28, 0.74, 0.03); leg(p, 'Leg', -0.64, 0.28, 0.74, 0.03)
p.append(CY('LampArmBase', 0.08, 0.09, 0.03, (-0.5, 0.2, 0.795), INK, segs=12, bevel=0.01))
p.append(CY('LampArm', 0.012, 0.012, 0.45, (-0.45, 0.13, 1.0), INK, segs=6, bevel=0)); rot(p[-1], x=25, y=15)
p.append(CY('LampHead', 0.05, 0.11, 0.12, (-0.38, 0.02, 1.2), MU, segs=14, bevel=0.01)); rot(p[-1], x=40)
for k in range(3): box(p, 'Paper', (0.21, 0.29, 0.004), (-0.05 + k * 0.03, -0.05, 0.79 + k * 0.004), CR, bev=0, segs=1); rot(p[-1], z=k * 9 - 9)
p.append(CY('PencilCup', 0.045, 0.045, 0.11, (0.25, 0.22, 0.84), RO, segs=12, bevel=0.005))
for k in range(3): p.append(CY('Pencil', 0.006, 0.006, 0.16, (0.25 + (k - 1) * 0.015, 0.22, 0.92), MU, segs=6, bevel=0))
# chair
p.append(CY('ChairSeat', 0.22, 0.22, 0.05, (-0.1, -0.55, 0.46), W, segs=18, bevel=0.012))
box(p, 'ChairBack', (0.42, 0.05, 0.35), (-0.1, -0.76, 0.75), W, bev=0.02); rot(p[-1], x=-8)
for a in range(4):
    t = a / 4 * 2 * math.pi + math.pi / 4
    p.append(CY('ChairLeg', 0.016, 0.014, 0.44, (-0.1 + math.cos(t) * 0.15, -0.55 + math.sin(t) * 0.15, 0.22), WD, segs=8, bevel=0))
finish(p, 'INT_Desk')

# ------------------------------------------------------------------ WATCHMAKER bench (2.0 x 0.8) with pegboard, magnifier lamp, gears, little clocks
K.reset(); p = []
box(p, 'Top', (2.0, 0.8, 0.07), (0, 0, 0.92), WD, bev=0.02)
for x in (-0.92, 0.92):
    for y in (-0.33, 0.33): leg(p, 'Leg', x, y, 0.9, 0.04, WD, turned=False)
box(p, 'Shelf', (1.9, 0.7, 0.03), (0, 0, 0.25), W, bev=0.01, segs=1)
box(p, 'Board', (2.0, 0.04, 1.0), (0, 0.42, 1.5), CR, bev=0.02)
for i in range(9):
    for j in range(4): p.append(CY('Hole', 0.012, 0.012, 0.01, (-0.85 + i * 0.21, 0.4, 1.12 + j * 0.24), INK, segs=6, bevel=0)); rot(p[-1], x=90)
for k, x in enumerate((-0.7, -0.45, -0.2, 0.3, 0.55)):
    box(p, 'Tool', (0.03, 0.02, R.uniform(0.18, 0.3)), (x, 0.38, 1.6), BRASS if k % 2 else INK, bev=0.005, segs=1)
for k, x in enumerate((-0.6, 0.0, 0.6)):
    p.append(CY('WallClock', 0.12, 0.12, 0.04, (x, 0.39, 1.82), FACE if k != 1 else MU, segs=20, bevel=0.012)); rot(p[-1], x=90)
    p.append(K.torus('Rim', 0.12, 0.015, (x, 0.37, 1.82), BRASS, segs=20, rsegs=6)); rot(p[-1], x=90)
p.append(CY('LampArm', 0.015, 0.015, 0.6, (0.7, 0.15, 1.2), INK, segs=8, bevel=0)); rot(p[-1], x=-30)
p.append(K.torus('Loupe', 0.11, 0.02, (0.7, -0.08, 1.42), BRASS, segs=24, rsegs=6)); rot(p[-1], x=70)
p.append(CY('Lens', 0.1, 0.1, 0.01, (0.7, -0.08, 1.42), GL, segs=20, bevel=0)); rot(p[-1], x=70)
for k in range(5):
    g = K.star('Gear', R.uniform(0.06, 0.12), R.uniform(0.04, 0.09), 0.02, (R.uniform(-0.8, 0.4), R.uniform(-0.25, 0.1), 0.97), BRASS, points=R.randint(6, 10), bevel=0.004)
    rot(g, x=90); p.append(g)
box(p, 'Mat', (0.6, 0.4, 0.01), (-0.2, -0.1, 0.96), TL, bev=0.004, segs=1)
finish(p, 'INT_Workbench')

# ------------------------------------------------------------------ GRANDFATHER clock (2.4 m)
K.reset(); p = []
box(p, 'Base', (0.7, 0.45, 0.5), (0, 0, 0.25), WD, bev=0.03)
box(p, 'Waist', (0.52, 0.38, 1.2), (0, 0, 1.1), W, bev=0.03)
box(p, 'Window', (0.32, 0.02, 0.8), (0, -0.19, 1.1), GL, bev=0.01, segs=1)
p.append(CY('PendRod', 0.008, 0.008, 0.6, (0, -0.16, 1.2), BRASS, segs=6, bevel=0))
p.append(CY('Bob', 0.08, 0.08, 0.02, (0, -0.16, 0.9), BRASS, segs=16, bevel=0.005)); rot(p[-1], x=90)
box(p, 'Hood', (0.7, 0.45, 0.62), (0, 0, 2.0), WD, bev=0.04)
p.append(CY('Dial', 0.24, 0.24, 0.03, (0, -0.23, 2.0), FACE, segs=28, bevel=0.01)); rot(p[-1], x=90)
p.append(K.torus('DialRim', 0.24, 0.025, (0, -0.24, 2.0), BRASS, segs=28, rsegs=6)); rot(p[-1], x=90)
for k, (L, a) in enumerate(((0.14, 300), (0.2, 60))):
    h = box(p, 'Hand', (0.02, 0.01, L), (math.sin(math.radians(a)) * L / 2, -0.26, 2.0 + math.cos(math.radians(a)) * L / 2), INK, bev=0.003, segs=1); rot(h, y=a)
p.append(CY('Arch', 0.36, 0.36, 0.44, (0, 0, 2.3), WD, segs=24, bevel=0.03)); rot(p[-1], x=90)
p.append(SP('Finial', 0.05, (0, 0, 2.72), BRASS, segs=10, rings=6))
finish(p, 'INT_GrandfatherClock')

# ------------------------------------------------------------------ DISPLAY case (1.4 x 0.6 x 1.0) with watches
K.reset(); p = []
box(p, 'Body', (1.4, 0.6, 0.75), (0, 0, 0.375), W, bev=0.03)
box(p, 'Panel', (1.3, 0.02, 0.55), (0, -0.305, 0.38), CR, bev=0.01, segs=1)
box(p, 'Velvet', (1.3, 0.5, 0.02), (0, 0, 0.77), LI, bev=0.005, segs=1)
for k in range(6):
    x = -0.5 + k * 0.2
    p.append(CY('Watch', 0.055, 0.055, 0.02, (x, -0.05, 0.795), FACE, segs=16, bevel=0.005))
    p.append(K.torus('Case', 0.055, 0.01, (x, -0.05, 0.8), BRASS if k % 2 else CR, segs=16, rsegs=5))
    box(p, 'Strap', (0.04, 0.22, 0.008), (x, 0.08, 0.785), RO if k % 3 == 0 else TL, bev=0.003, segs=1)
# open-top showcase: brass corner posts + rail and a slanted glass front lip (no opaque glass box hiding the watches)
for x in (-0.68, 0.68):
    for y in (-0.28, 0.28): p.append(CY('Post', 0.02, 0.02, 0.3, (x, y, 0.92), BRASS, segs=8, bevel=0))
for y in (-0.28, 0.28): box(p, 'Rail', (1.38, 0.03, 0.03), (0, y, 1.07), BRASS, bev=0.005, segs=1)
for x in (-0.68, 0.68): box(p, 'RailS', (0.03, 0.58, 0.03), (x, 0, 1.07), BRASS, bev=0.005, segs=1)
box(p, 'Sign', (0.3, 0.02, 0.12), (0.45, -0.2, 0.86), CR, bev=0.005, segs=1); rot(p[-1], x=-25)
finish(p, 'INT_DisplayCase')

# ------------------------------------------------------------------ DOJO: tatami, training dummy, glove rack, stone lantern
K.reset(); p = []
box(p, 'Mat', (1.9, 0.95, 0.06), (0, 0, 0.03), TAT, bev=0.015, segs=1)
for s in (-1, 1): box(p, 'Border', (1.9, 0.06, 0.065), (0, s * 0.45, 0.033), INK, bev=0.005, segs=1)
finish(p, 'INT_Tatami')
K.reset(); p = []
p.append(CY('Base', 0.3, 0.34, 0.12, (0, 0, 0.06), WD, segs=16, bevel=0.02))
p.append(CY('Post', 0.07, 0.07, 1.0, (0, 0, 0.6), W, segs=12, bevel=0.01))
p.append(CY('Body', 0.22, 0.22, 0.7, (0, 0, 1.25), W, segs=18, bevel=0.04))
for z in (1.0, 1.5): p.append(K.torus('Band', 0.225, 0.02, (0, 0, z), WD, segs=20, rsegs=6))
p.append(SP('Head', 0.17, (0, 0, 1.8), W, segs=16, rings=10))
for k, (z, a) in enumerate(((1.4, -35), (1.4, 35), (1.15, 0))):
    arm = CY('Arm', 0.035, 0.03, 0.4, (math.sin(math.radians(a)) * 0.3, -0.3 * math.cos(math.radians(a)), z), WD, segs=8, bevel=0.005)
    rot(arm, x=90, z=a); p.append(arm)
p.append(SB('Tag', (0.18, 0.02, 0.12), (0, -0.215, 1.35), RO, bevel=0.01, segs=1))
finish(p, 'INT_TrainingDummy')
K.reset(); p = []
for s in (-1, 1): box(p, 'Side', (0.08, 0.3, 1.6), (s * 0.7, 0, 0.8), WD, bev=0.02)
for z in (0.5, 1.1, 1.55): box(p, 'Bar', (1.4, 0.08, 0.06), (0, 0, z), W, bev=0.015, segs=1)
for k in range(4):
    for row, z in enumerate((0.3, 0.9)):
        x = -0.5 + k * 0.33; col = (RO, TL, MU, LI)[(k + row) % 4]
        p.append(SP('Glove', 0.11, (x, -0.08, z), col, segs=12, rings=8, squash=(0.85, 0.75, 1.1)))
        p.append(CY('Cuff', 0.07, 0.07, 0.08, (x, -0.08, z + 0.12), CR, segs=10, bevel=0.01))
finish(p, 'INT_GloveRack')
K.reset(); p = []
box(p, 'Foot', (0.5, 0.5, 0.12), (0, 0, 0.06), CR, bev=0.03)
p.append(CY('Pillar', 0.1, 0.12, 0.5, (0, 0, 0.37), CR, segs=8, bevel=0.02))
box(p, 'Box', (0.42, 0.42, 0.34), (0, 0, 0.8), CR, bev=0.03)
box(p, 'Light', (0.24, 0.44, 0.18), (0, 0, 0.8), GLOW, bev=0.01, segs=1)
p.append(CY('Roof', 0.42, 0.06, 0.24, (0, 0, 1.08), INK, segs=4, bevel=0.03)); rot(p[-1], z=45)
p.append(SP('Knob', 0.05, (0, 0, 1.22), CR, segs=8, rings=5))
finish(p, 'INT_StoneLantern')

# ------------------------------------------------------------------ VAULT: round safe door (wall), gold pile
K.reset(); p = []
p.append(CY('Frame', 1.25, 1.25, 0.2, (0, 0, 1.4), INK, segs=40, bevel=0.04)); rot(p[-1], x=90)
p.append(CY('Door', 1.05, 1.05, 0.25, (0, -0.06, 1.4), COP, segs=40, bevel=0.05)); rot(p[-1], x=90)
p.append(K.torus('Ring', 0.85, 0.04, (0, -0.2, 1.4), BRASS, segs=36, rsegs=6)); rot(p[-1], x=90)
for k in range(12):
    a = k / 12 * 2 * math.pi
    p.append(SP('Bolt', 0.06, (math.cos(a) * 1.15, -0.12, 1.4 + math.sin(a) * 1.15), BRASS, segs=8, rings=5))
p.append(CY('Hub', 0.16, 0.16, 0.12, (0, -0.24, 1.4), BRASS, segs=18, bevel=0.02)); rot(p[-1], x=90)
for k in range(3):
    sp = box(p, 'Spoke', (0.05, 0.05, 0.9), (0, -0.28, 1.4), BRASS, bev=0.01, segs=1); rot(sp, y=k * 60)
finish(p, 'INT_SafeDoor')
K.reset(); p = []
for k in range(26):
    a = R.uniform(0, 2 * math.pi); rr = R.uniform(0, 0.5); z = max(0.02, 0.35 - rr * 0.6) * R.uniform(0.3, 1.0)
    c = CY('Coin', 0.08, 0.08, 0.02, (math.cos(a) * rr, math.sin(a) * rr, z), BRASS, segs=14, bevel=0.004); rot(c, x=R.uniform(-30, 30), y=R.uniform(-30, 30)); p.append(c)
for k in range(3): box(p, 'Bar', (0.26, 0.12, 0.08), (-0.25 + k * 0.08, 0.3, 0.04 + k * 0.08), MU, bev=0.015, segs=1)
p.append(SP('Mound', 0.45, (0, 0, -0.1), BRASS, segs=18, rings=10, squash=(1, 1, 0.45)))
finish(p, 'INT_GoldPile')

# ------------------------------------------------------------------ PLANT_Monstera
K.reset(); p = []
p.append(CY('Pot', 0.22, 0.17, 0.4, (0, 0, 0.2), TL, segs=18, bevel=0.03))
p.append(CY('Rim', 0.24, 0.24, 0.06, (0, 0, 0.41), CR, segs=18, bevel=0.015))
p.append(CY('Soil', 0.2, 0.2, 0.02, (0, 0, 0.42), WD, segs=16, bevel=0))
for k in range(7):
    a = k / 7 * 2 * math.pi + R.uniform(-0.2, 0.2); L = R.uniform(0.45, 0.75); h = R.uniform(0.6, 1.1)
    stem = CY('Stem', 0.012, 0.01, h, (math.cos(a) * 0.08, math.sin(a) * 0.08, 0.42 + h / 2), LF, segs=6, bevel=0); rot(stem, x=-math.sin(a) * 18, y=math.cos(a) * 18); p.append(stem)
    leaf = SP('Leaf', 0.24, (math.cos(a) * (0.12 + L * 0.4), math.sin(a) * (0.12 + L * 0.4), 0.42 + h), LF, segs=14, rings=8, squash=(1.0, 0.75, 0.12))
    leaf.rotation_euler = (math.radians(-25 * math.sin(a)), math.radians(25 * math.cos(a)), a); p.append(leaf)
finish(p, 'INT_Monstera')

# ------------------------------------------------------------------ RUG_Oval (3.2 x 2.2, with border + pattern dots)
K.reset(); p = []
p.append(CY('Border', 1.6, 1.6, 0.012, (0, 0, 0.006), INK, segs=40, bevel=0)); p[-1].scale = (1, 0.69, 1)
p.append(CY('Field', 1.5, 1.5, 0.016, (0, 0, 0.008), RO, segs=40, bevel=0)); p[-1].scale = (1, 0.69, 1)
p.append(CY('Inner', 1.05, 1.05, 0.02, (0, 0, 0.01), CR, segs=36, bevel=0)); p[-1].scale = (1, 0.69, 1)
for k in range(12):
    a = k / 12 * 2 * math.pi
    p.append(CY('Dot', 0.07, 0.07, 0.022, (math.cos(a) * 1.28, math.sin(a) * 0.88, 0.011), MU, segs=10, bevel=0))
finish(p, 'INT_Rug')

# ------------------------------------------------------------------ PENDANT lamp (hangs from ceiling: origin at the ceiling mount, body below)
K.reset(); p = []
p.append(CY('Mount', 0.08, 0.06, 0.04, (0, 0, -0.02), INK, segs=12, bevel=0.01))
p.append(CY('Cord', 0.008, 0.008, 0.8, (0, 0, -0.42), INK, segs=6, bevel=0))
p.append(CY('Shade', 0.08, 0.26, 0.24, (0, 0, -0.92), MU, segs=20, bevel=0.01))
p.append(SP('Bulb', 0.07, (0, 0, -1.04), GLOW, segs=10, rings=6))
finish(p, 'INT_Pendant')
print('interior kit done')
