"""
Dream Galaxy kit (level select after sleeping — Astro Bot galaxy reference, docs/LEVEL1_DESIGN.md).
Each world is a tiny storybook planet you can read at a glance:
  PLANET_Book     World 1 "The Dream Book": open book on a paper moon, a tower of books with THE CHAIR on top
  PLANET_Toy      World 2 (locked): rounded toy-block planet with blocks + a spinning top
  PLANET_Bath     World 3 (locked): bath-water planet with a rubber duck and bubbles
  PLANET_Kitchen  World 4 (locked): teacup planet on a saucer
  PROP_PillowShip Pip's flying pillow
  PROP_GalStar    chunky 5-point star (collectible / decoration)
  PROP_GalRing    planetary ring
Flat hierarchy, origin at the planet centre (ship/star: centre), materials M_Kit_Gal* remapped in Unity (ArtKit).
run: python3 -I build_galaxy_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402  (must come before bmesh)
import dr_bkit as K  # noqa: E402
from mathutils import Vector  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'Environment', 'Props'))


def book(name, w, d, t, center, cover, yaw=0.0, page='M_Kit_GalPaper'):
    """closed book: cover slab + inset page block (reads as a book from any side)."""
    c = K.soft_box(name + '_Cover', (w, d, t), center, cover, bevel=0.03, segs=2)
    p = K.soft_box(name + '_Pages', (w * 0.94, d * 0.96, t * 0.78), (center[0] + w * 0.03, center[1], center[2]), page, bevel=0.015, segs=1)
    o = K.join([c, p], name)
    o.rotation_euler = (0, 0, math.radians(yaw))
    return o


def tiny_chair(name, center, s=1.0):
    x, y, z = center
    parts = [K.soft_box(name + '_Seat', (0.9 * s, 0.9 * s, 0.14 * s), (x, y, z + 0.5 * s), 'M_Kit_GalWood', bevel=0.04)]
    parts.append(K.soft_box(name + '_Cush', (0.8 * s, 0.8 * s, 0.12 * s), (x, y, z + 0.62 * s), 'M_Kit_GalRed', bevel=0.05, puff=0.2))
    for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
        parts.append(K.cyl(f'{name}_Leg{i}', 0.06 * s, 0.05 * s, 0.5 * s, (x + dx * 0.36 * s, y + dy * 0.36 * s, z + 0.25 * s), 'M_Kit_GalWood', segs=10, bevel=0))
    parts.append(K.soft_box(name + '_Back', (0.9 * s, 0.12 * s, 0.85 * s), (x, y + 0.42 * s, z + 1.0 * s), 'M_Kit_GalWood', bevel=0.05))
    return K.join(parts, name)


def cloud(name, center, s, seed):
    r = random.Random(seed); parts = []
    for i in range(5):
        parts.append(K.uv_sphere(f'{name}_{i}', s * r.uniform(0.45, 0.7), (center[0] + (i - 2) * s * 0.55, center[1] + r.uniform(-0.2, 0.2) * s, center[2] + r.uniform(-0.1, 0.25) * s), 'M_Kit_GalCloud', segs=16, rings=10))
    return K.join(parts, name)


# =========================================================== PLANET_Book (≈ 9 m across)
K.reset()
parts = []
parts.append(K.uv_sphere('Moon', 3.2, (0, 0, 0), 'M_Kit_GalPaper', squash=(1, 1, 0.92)))
# open book resting on top: two thick page blocks tilted up into a shallow V + cover underneath
for side in (-1, 1):
    pg = K.soft_box(f'Page{side}', (3.4, 4.6, 0.55), (side * 1.75, 0, 3.15), 'M_Kit_GalPaper', bevel=0.12, puff=0.06)
    pg.rotation_euler = (0, math.radians(-side * 7), 0); parts.append(pg)
cov = K.soft_box('Cover', (7.4, 5.0, 0.22), (0, 0, 2.78), 'M_Kit_GalRed', bevel=0.06); parts.append(cov)
# book tower (the Throne Tower landmark) with THE CHAIR on top
z = 3.45
r = random.Random(4)
cols = ['M_Kit_GalRed', 'M_Kit_GalLav', 'M_Kit_GalBlue', 'M_Kit_GalYellow', 'M_Kit_GalRed', 'M_Kit_GalLav']
for i in range(6):
    t = 0.42 - i * 0.02
    w = 1.9 - i * 0.12
    parts.append(book(f'Tower{i}', w, w * 0.72, t, (1.3 + r.uniform(-0.08, 0.08), 0.4, z + t / 2), cols[i], yaw=r.uniform(-25, 25)))
    z += t
parts.append(tiny_chair('Throne', (1.3, 0.4, z), 0.8))
# pop-up houses on the left page (paper cut-out triangles + boxes)
for i, (x, y) in enumerate([(-2.4, -1.2), (-1.5, 0.9), (-2.7, 1.4)]):
    h = K.soft_box(f'House{i}', (0.55, 0.5, 0.6), (x, y, 3.75), 'M_Kit_GalYellow' if i % 2 else 'M_Kit_GalBlue', bevel=0.03)
    roof = K.cyl(f'Roof{i}', 0.48, 0.0, 0.5, (x, y, 4.3), 'M_Kit_GalRed', segs=4, bevel=0.02); roof.rotation_euler = (0, 0, math.radians(45))
    parts += [h, roof]
# little trees
for i, (x, y) in enumerate([(-0.9, -1.6), (-3.0, 0.1), (0.2, -1.8)]):
    parts.append(K.cyl(f'Trunk{i}', 0.07, 0.06, 0.4, (x, y, 3.65), 'M_Kit_GalWood', segs=8, bevel=0))
    parts.append(K.uv_sphere(f'Leaf{i}', 0.32, (x, y, 4.05), 'M_Kit_GalGreen', segs=14, rings=8))
parts.append(cloud('Cloud0', (-3.4, 2.4, 1.0), 0.9, 1))
parts.append(cloud('Cloud1', (3.3, -2.2, -0.6), 0.8, 2))
planet = K.join(parts, 'PLANET_Book')
K.export('PLANET_Book')

# =========================================================== PLANET_Toy
K.reset()
parts = [K.soft_box('Core', (5.6, 5.6, 5.6), (0, 0, 0), 'M_Kit_GalYellow', bevel=1.4, segs=6)]
r = random.Random(9)
cols = ['M_Kit_GalRed', 'M_Kit_GalBlue', 'M_Kit_GalGreen', 'M_Kit_GalLav']
for i in range(7):
    a = r.uniform(0, 6.28); rad = r.uniform(0.3, 1.9)
    b = K.soft_box(f'Block{i}', (0.8, 0.8, 0.8), (math.cos(a) * rad, math.sin(a) * rad, 3.2 + (i % 3) * 0.75), cols[i % 4], bevel=0.12)
    b.rotation_euler = (0, 0, r.uniform(0, 1.5)); parts.append(b)
parts.append(K.cyl('Top', 0.7, 0.05, 1.0, (1.6, -1.4, 3.5), 'M_Kit_GalRed', segs=16))
parts.append(K.cyl('TopStem', 0.07, 0.07, 0.6, (1.6, -1.4, 4.2), 'M_Kit_GalWood', segs=8, bevel=0))
K.join(parts, 'PLANET_Toy'); K.export('PLANET_Toy')

# =========================================================== PLANET_Bath
K.reset()
parts = [K.uv_sphere('Water', 3.3, (0, 0, 0), 'M_Kit_GalBlue')]
parts.append(K.uv_sphere('DuckBody', 1.1, (0.4, 0, 3.4), 'M_Kit_GalYellow', squash=(1.25, 0.95, 0.75)))
parts.append(K.uv_sphere('DuckHead', 0.62, (-0.6, 0, 4.25), 'M_Kit_GalYellow'))
beak = K.cyl('Beak', 0.26, 0.04, 0.5, (-1.25, 0, 4.15), 'M_Kit_GalOrange', segs=12, bevel=0); beak.rotation_euler = (0, math.radians(-90), 0); parts.append(beak)
r = random.Random(3)
for i in range(9):
    a = r.uniform(0, 6.28); e = r.uniform(-0.5, 1.2)
    p = Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * (3.5 + r.uniform(0, 1.2))
    parts.append(K.uv_sphere(f'Bubble{i}', r.uniform(0.2, 0.5), tuple(p), 'M_Kit_GalCloud', segs=14, rings=8))
K.join(parts, 'PLANET_Bath'); K.export('PLANET_Bath')

# =========================================================== PLANET_Kitchen (teacup)
K.reset()
parts = [K.cyl('Cup', 2.6, 1.8, 3.4, (0, 0, 0.6), 'M_Kit_GalCloud', segs=32, bevel=0.2)]
parts.append(K.cyl('Tea', 2.4, 2.4, 0.1, (0, 0, 2.25), 'M_Kit_GalWood', segs=32, bevel=0))
parts.append(K.cyl('Saucer', 3.8, 3.2, 0.35, (0, 0, -1.25), 'M_Kit_GalLav', segs=32, bevel=0.1))
handle = K.torus('Handle', 0.85, 0.22, (2.7, 0, 0.8), 'M_Kit_GalCloud'); handle.rotation_euler = (math.radians(90), 0, 0); parts.append(handle)
spoon = K.soft_box('Spoon', (0.25, 0.25, 3.6), (0.8, 0.6, 3.0), 'M_Kit_GalYellow', bevel=0.1); spoon.rotation_euler = (math.radians(20), math.radians(15), 0); parts.append(spoon)
K.join(parts, 'PLANET_Kitchen'); K.export('PLANET_Kitchen')

# =========================================================== PROP_PillowShip (≈1.6 m)
K.reset()
parts = [K.soft_box('Pillow', (1.6, 1.1, 0.42), (0, 0, 0), 'M_Kit_GalCloud', bevel=0.18, segs=5, puff=0.45)]
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    parts.append(K.uv_sphere(f'Tassel{i}', 0.1, (dx * 0.8, dy * 0.55, 0), 'M_Kit_GalYellow', segs=10, rings=6))
parts.append(K.cyl('Mast', 0.03, 0.03, 0.9, (0.55, 0.35, 0.55), 'M_Kit_GalWood', segs=6, bevel=0))
flag = K.soft_box('Flag', (0.4, 0.03, 0.26), (0.77, 0.35, 0.86), 'M_Kit_GalRed', bevel=0.01); parts.append(flag)
K.join(parts, 'PROP_PillowShip'); K.export('PROP_PillowShip')

# =========================================================== PROP_GalStar (≈1 m)
K.reset()
K.star('PROP_GalStar', 0.5, 0.24, 0.26, (0, 0, 0), 'M_Kit_GalYellow')
K.export('PROP_GalStar')

# =========================================================== PROP_GalRing (R = 1)
K.reset()
K.torus('PROP_GalRing', 1.0, 0.05, (0, 0, 0), 'M_Kit_GalLav', segs=64, rsegs=8)
K.export('PROP_GalRing')

# =========================================================== PLANET_Dreamland (open world: meadow globe, crystals, trees, lake)
K.reset()
parts = [K.uv_sphere('Globe', 3.4, (0, 0, 0), 'M_Kit_GalGreen', segs=40, rings=24)]
parts.append(K.uv_sphere('Lake', 1.6, (1.6, -1.2, 2.55), 'M_Kit_GalBlue', squash=(1, 1, 0.25)))
r = random.Random(12)
for i in range(9):
    a = r.uniform(0, 6.28); e = r.uniform(0.35, 1.2)
    d = Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e)))
    base = d * 3.3
    if i % 3 == 0:
        c = K.cyl(f'Crystal{i}', 0.32, 0.0, 1.8, tuple(base + d * 0.8), 'M_Kit_GalLav', segs=6, bevel=0.02)
    else:
        parts.append(K.cyl(f'Trunk{i}', 0.08, 0.06, 0.6, tuple(base + d * 0.3), 'M_Kit_GalWood', segs=6, bevel=0))
        c = K.uv_sphere(f'Canopy{i}', 0.5, tuple(base + d * 0.85), 'M_Kit_GalGreen', segs=14, rings=8)
    c.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    parts.append(c)
parts.append(K.cyl('Spire', 0.55, 0.0, 3.4, (-0.6, 0.4, 4.6), 'M_Kit_GalLav', segs=6, bevel=0.03))
K.join(parts, 'PLANET_Dreamland'); K.export('PLANET_Dreamland')
