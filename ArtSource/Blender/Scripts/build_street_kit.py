"""
M21 street dressing kit (user: "the roads are empty"). Same illustrated palette as the interior kit / v3 facades.
PLANTER_Box, HYDRANT, BUS_Stop, NEWS_Box, BOLLARD, BIKE_Rack, VENDOR_Cart, FLOWER_Bed. Fronts face Blender -Y.
run: python3 -I build_street_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
import dr_bkit as K  # noqa: E402
K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere
R = random.Random(2122)
W, WD, CR, TL, RO, LI, MU, INK, LF, GL = ('M_Kit_MP_IntWood', 'M_Kit_MP_IntWoodDark', 'M_Kit_MP_IntCream', 'M_Kit_MP_IntTeal', 'M_Kit_MP_IntRose',
                                          'M_Kit_MP_IntLilac', 'M_Kit_MP_IntMustard', 'M_Kit_MP_IntInk', 'M_Kit_MP_IntLeaf', 'M_Kit_MP_IntGlass')
GLOW, BRASS = 'M_Kit_MP_LampGlow', 'M_Kit_MP_Brass'


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def pbox(n, size, c, m, bev=0.02):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    o = K.from_bm(n, bm, m); o.location = c
    if bev > 0.0005:
        b = o.modifiers.new('bev', 'BEVEL'); b.width = min(bev, min(size) * 0.45); b.segments = 2; b.limit_method = 'ANGLE'; K.apply_mods(o)
    return o


def bush(p, c, r, m=LF):
    for k in range(5):
        a = k / 5 * 2 * math.pi
        p.append(SP('Bush', r * R.uniform(0.55, 0.75), (c[0] + math.cos(a) * r * 0.45, c[1] + math.sin(a) * r * 0.45, c[2] + R.uniform(0, r * 0.3)), m, segs=10, rings=7))
    p.append(SP('BushTop', r * 0.7, (c[0], c[1], c[2] + r * 0.45), m, segs=10, rings=7))


def finish(p, n): K.join(p, n); K.export(n)


# planter box with a bush and flowers (1.6 x 0.8)
K.reset(); p = []
p.append(pbox('Box', (1.6, 0.8, 0.55), (0, 0, 0.275), W, 0.04))
p.append(pbox('Rim', (1.7, 0.9, 0.08), (0, 0, 0.58), CR, 0.03))
p.append(pbox('Soil', (1.5, 0.7, 0.04), (0, 0, 0.6), WD, 0.01))
bush(p, (-0.3, 0, 0.75), 0.4); bush(p, (0.4, 0.05, 0.7), 0.32)
for k in range(6): p.append(SP('Flower', 0.07, (R.uniform(-0.7, 0.7), R.uniform(-0.3, 0.3), R.uniform(0.95, 1.15)), R.choice([RO, MU, LI, CR]), segs=8, rings=5))
finish(p, 'ST_Planter')
# hydrant
K.reset(); p = []
p.append(CY('Foot', 0.16, 0.17, 0.06, (0, 0, 0.03), INK, segs=14, bevel=0.01))
p.append(CY('Body', 0.12, 0.12, 0.5, (0, 0, 0.31), TL, segs=16, bevel=0.02))
p.append(SP('Cap', 0.13, (0, 0, 0.56), TL, segs=14, rings=8, squash=(1, 1, 0.7)))
p.append(CY('Nut', 0.035, 0.035, 0.05, (0, 0, 0.66), BRASS, segs=6, bevel=0.005))
for s in (-1, 1): p.append(CY('Arm', 0.045, 0.045, 0.1, (s * 0.15, 0, 0.36), BRASS, segs=10, bevel=0.01)); rot(p[-1], y=90)
p.append(CY('Front', 0.06, 0.06, 0.08, (0, -0.15, 0.4), BRASS, segs=10, bevel=0.01)); rot(p[-1], x=90)
finish(p, 'ST_Hydrant')
# bus stop shelter (3.6 x 1.4 x 2.6) with bench + poster frame (blank illustrated panel) + roof light
K.reset(); p = []
for x in (-1.7, 1.7):
    for y in (-0.55, 0.55): p.append(CY('Post', 0.05, 0.05, 2.5, (x, y, 1.25), INK, segs=10, bevel=0.005))
p.append(pbox('Roof', (3.8, 1.6, 0.12), (0, 0, 2.56), TL, 0.04))
p.append(pbox('RoofTrim', (3.84, 1.64, 0.04), (0, 0, 2.48), CR, 0.01))
p.append(pbox('Light', (3.2, 0.2, 0.03), (0, -0.4, 2.48), GLOW, 0.005))
p.append(pbox('BackGlass', (3.4, 0.04, 1.8), (0, 0.55, 1.25), GL, 0.01))
p.append(pbox('Panel', (1.0, 0.06, 1.6), (1.15, 0.52, 1.25), MU, 0.02))
p.append(pbox('PanelArt', (0.8, 0.07, 1.2), (1.15, 0.5, 1.3), CR, 0.01))
p.append(SP('PanelMoon', 0.22, (1.15, 0.45, 1.5), RO, segs=12, rings=8, squash=(1, 0.2, 1)))
p.append(pbox('Seat', (2.0, 0.42, 0.06), (-0.5, 0.25, 0.48), W, 0.02))
for x in (-1.3, 0.3): p.append(pbox('SeatLeg', (0.06, 0.36, 0.45), (x, 0.25, 0.23), INK, 0.01))
p.append(CY('SignPole', 0.04, 0.04, 2.9, (-2.1, -0.6, 1.45), INK, segs=8, bevel=0))
p.append(CY('Sign', 0.28, 0.28, 0.05, (-2.1, -0.6, 2.75), MU, segs=20, bevel=0.01)); rot(p[-1], x=90)
p.append(SP('SignMoon', 0.12, (-2.1, -0.64, 2.75), INK, segs=10, rings=6, squash=(1, 0.3, 1)))
finish(p, 'ST_BusStop')
# news / snack vending box
K.reset(); p = []
p.append(pbox('Body', (0.6, 0.5, 1.1), (0, 0, 0.6), RO, 0.05))
p.append(pbox('Window', (0.44, 0.04, 0.4), (0, -0.25, 0.85), GL, 0.02))
p.append(pbox('Slot', (0.3, 0.04, 0.06), (0, -0.25, 0.45), INK, 0.01))
p.append(pbox('Top', (0.66, 0.56, 0.06), (0, 0, 1.18), CR, 0.02))
for x in (-0.24, 0.24): p.append(pbox('Leg', (0.06, 0.4, 0.06), (x, 0, 0.03), INK, 0.01))
finish(p, 'ST_NewsBox')
# bollard
K.reset(); p = []
p.append(CY('Body', 0.11, 0.12, 0.85, (0, 0, 0.425), INK, segs=14, bevel=0.02))
p.append(SP('Top', 0.11, (0, 0, 0.85), INK, segs=14, rings=8, squash=(1, 1, 0.6)))
p.append(K.torus('Band', 0.12, 0.02, (0, 0, 0.7), MU, segs=16, rsegs=6))
finish(p, 'ST_Bollard')
# bike rack with 2 toy bikes
K.reset(); p = []
for x in (-0.6, 0, 0.6):
    p.append(K.torus('Loop', 0.35, 0.025, (x, 0, 0.35), INK, segs=24, rsegs=6)); rot(p[-1], x=90)
for k, x in enumerate((-0.3, 0.3)):
    col = (RO, TL)[k]
    for y in (-0.5, 0.5): p.append(K.torus('Wheel', 0.28, 0.03, (x, y, 0.3), INK, segs=24, rsegs=6)); rot(p[-1], z=90, x=90)
    p.append(pbox('Frame', (0.04, 1.0, 0.05), (x, 0, 0.45), col, 0.01)); rot(p[-1], x=8)
    p.append(pbox('Seat', (0.1, 0.22, 0.05), (x, 0.3, 0.72), INK, 0.02))
    p.append(CY('Bar', 0.02, 0.02, 0.5, (x, -0.42, 0.75), col, segs=6, bevel=0)); rot(p[-1], y=90)
finish(p, 'ST_BikeRack')
# vendor cart (sweets) with striped awning
K.reset(); p = []
p.append(pbox('Cart', (1.6, 0.8, 0.9), (0, 0, 0.75), CR, 0.05))
for i in range(6): p.append(pbox('Stripe', (0.24, 0.02, 0.8), (-0.65 + i * 0.26, -0.41, 0.75), TL if i % 2 == 0 else CR, 0.005))
p.append(pbox('Top', (1.7, 0.9, 0.06), (0, 0, 1.22), WD, 0.02))
for s in (-1, 1): p.append(K.torus('Wheel', 0.28, 0.05, (s * 0.85, 0, 0.3), INK, segs=20, rsegs=6)); rot(p[-1], y=90)
for x in (-0.78, 0.78): p.append(CY('Pole', 0.025, 0.025, 1.2, (x, 0.3, 1.8), INK, segs=8, bevel=0))
for i in range(8):
    a = pbox('Awning', (0.24, 1.2, 0.04), (-0.84 + i * 0.24, 0.0, 2.42), RO if i % 2 == 0 else CR, 0.01); rot(a, x=-12); p.append(a)
for k in range(4): p.append(SP('Candy', 0.08, (-0.5 + k * 0.32, -0.1, 1.33), (RO, MU, LI, TL)[k], segs=10, rings=6))
p.append(SP('Lamp', 0.09, (0, -0.4, 2.2), GLOW, segs=10, rings=6))
finish(p, 'ST_VendorCart')
# flower bed island (for the grand avenues): 4 x 1.6 m
K.reset(); p = []
p.append(pbox('Curb', (4.0, 1.6, 0.3), (0, 0, 0.15), CR, 0.05))
p.append(pbox('Soil', (3.8, 1.4, 0.06), (0, 0, 0.31), WD, 0.02))
for x in (-1.3, 0, 1.3): bush(p, (x, 0, 0.5), 0.42)
for k in range(14): p.append(SP('Flower', 0.08, (R.uniform(-1.8, 1.8), R.uniform(-0.6, 0.6), R.uniform(0.4, 0.75)), R.choice([RO, MU, LI, CR]), segs=8, rings=5))
finish(p, 'ST_FlowerBed')
print('street kit done')
