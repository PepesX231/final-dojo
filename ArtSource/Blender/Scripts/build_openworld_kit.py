"""
DREAMLAND open-world kit (docs/OPENWORLD_DESIGN.md) — Final Fantasy field spirit in our toon language:
round lush trees, tall stylised pines, glowing crystal clusters, boulders, floating rock islands, ancient ruins,
THE ALARM (nightmare alarm-clock monster), the Dream Key, speed ring.
Flat hierarchy, origin at the base (monster: feet), materials M_Kit_OW* remapped in Unity (ArtKit).
run: python3 -I build_openworld_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa
import bmesh  # noqa
import dr_bkit as K  # noqa
from mathutils import Vector, Matrix, noise  # noqa

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'OpenWorld', 'Models'), os.path.join('ArtSource', 'Blender', 'OpenWorld'))


def blob(name, r, center, material, seed, rough=0.22, subdiv=3, squash=(1, 1, 1)):
    """lumpy icosphere (foliage clumps, boulders)."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    rnd = random.Random(seed)
    off = Vector((rnd.random() * 10, rnd.random() * 10, rnd.random() * 10))
    for v in bm.verts:
        d = v.co.normalized()
        n = noise.noise(d * 1.6 + off)
        v.co = d * r * (1 + n * rough)
        v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
    o = K.from_bm(name, bm, material)
    o.location = center
    return o


def prism(name, r, h, center, material, tilt=(0, 0), sides=6, tip=0.35):
    """crystal: hex prism with a pointed tip, origin at its base."""
    bm = bmesh.new()
    base, top = [], []
    for k in range(sides):
        a = k / sides * 2 * math.pi
        base.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0)))
        top.append(bm.verts.new((math.cos(a) * r * 0.92, math.sin(a) * r * 0.92, h)))
    apex = bm.verts.new((0, 0, h + r * tip * 4))
    bm.faces.new(list(reversed(base)))
    for k in range(sides):
        j = (k + 1) % sides
        bm.faces.new((base[k], base[j], top[j], top[k]))
        bm.faces.new((top[k], top[j], apex))
    o = K.from_bm(name, bm, material)
    o.location = center
    o.rotation_euler = (math.radians(tilt[0]), math.radians(tilt[1]), 0)
    return o


def export(name):
    K.export(name)


# ===================================================== TREES (round canopy, 3 variants)
for vi, (h, cr, seed) in enumerate([(4.2, 2.6, 1), (5.6, 3.2, 2), (3.4, 2.1, 3)]):
    K.reset()
    rnd = random.Random(seed)
    pts = [(0, 0, 0), (0.15, 0.05, h * 0.35), (-0.1, 0.12, h * 0.7), (0.05, 0, h)]
    parts = []
    trunk = K.cyl('Trunk', 0.42, 0.22, h, (0, 0, h / 2), 'M_Kit_OWBark', segs=10, bevel=0.05)
    parts.append(trunk)
    for b in range(3):
        a = b * 2.1 + rnd.random()
        parts.append(K.cyl(f'Branch{b}', 0.12, 0.06, 1.4, (math.cos(a) * 0.5, math.sin(a) * 0.5, h * 0.75), 'M_Kit_OWBark', segs=6, bevel=0))
        parts[-1].rotation_euler = (math.cos(a) * 0.9, math.sin(a) * 0.9, 0)
    for k in range(7):
        a = k / 7 * 2 * math.pi + rnd.random()
        rr = cr * rnd.uniform(0.45, 0.7)
        c = (math.cos(a) * cr * 0.55, math.sin(a) * cr * 0.55, h + rnd.uniform(-0.3, 0.9))
        parts.append(blob(f'Leaf{k}', rr, c, 'M_Kit_OWLeaf' if k % 3 else 'M_Kit_OWLeafLight', seed * 10 + k, squash=(1, 1, 0.82)))
    parts.append(blob('LeafTop', cr * 0.75, (0, 0, h + cr * 0.55), 'M_Kit_OWLeafLight', seed * 99, squash=(1, 1, 0.85)))
    roots = []
    for k in range(4):
        a = k / 4 * 2 * math.pi + 0.4
        r = K.cyl(f'Root{k}', 0.2, 0.05, 0.9, (math.cos(a) * 0.45, math.sin(a) * 0.45, 0.2), 'M_Kit_OWBark', segs=6, bevel=0)
        r.rotation_euler = (math.sin(a) * -1.1, math.cos(a) * 1.1, 0); parts.append(r)
    K.join(parts, f'TREE_Round{vi}')
    export(f'TREE_Round{vi}')

# ===================================================== PINE (stack of cones)
K.reset()
parts = [K.cyl('Trunk', 0.3, 0.18, 2.2, (0, 0, 1.1), 'M_Kit_OWBark', segs=8, bevel=0)]
for k in range(5):
    r = 2.3 - k * 0.38
    parts.append(K.cyl(f'Tier{k}', r, 0.15, 2.0, (0, 0, 2.0 + k * 1.35), 'M_Kit_OWPine' if k % 2 == 0 else 'M_Kit_OWLeaf', segs=10, bevel=0.15))
K.join(parts, 'TREE_Pine'); export('TREE_Pine')

# ===================================================== CRYSTALS (2 clusters + a giant spire)
for vi, seed in enumerate([5, 8]):
    K.reset()
    rnd = random.Random(seed)
    parts = [blob('Base', 1.4, (0, 0, 0.1), 'M_Kit_OWRock', seed, squash=(1.2, 1.2, 0.45))]
    for k in range(7):
        a = rnd.random() * 6.28; d = rnd.uniform(0, 0.9)
        h = rnd.uniform(1.2, 3.6) if k else 4.2
        parts.append(prism(f'C{k}', rnd.uniform(0.22, 0.42) if k else 0.55, h, (math.cos(a) * d, math.sin(a) * d, 0.2),
                           'M_Kit_OWCrystal' if k % 3 else 'M_Kit_OWCrystalPink', tilt=(rnd.uniform(-25, 25), rnd.uniform(-25, 25))))
    K.join(parts, f'CRYSTAL_Cluster{vi}'); export(f'CRYSTAL_Cluster{vi}')
K.reset()
parts = [blob('Base', 3.2, (0, 0, 0.3), 'M_Kit_OWRock', 77, squash=(1.2, 1.2, 0.5))]
parts.append(prism('Spire', 1.3, 14, (0, 0, 0.5), 'M_Kit_OWCrystal', tilt=(4, -3), tip=0.5))
for k in range(5):
    a = k * 1.3
    parts.append(prism(f'S{k}', 0.6, 5 + k, (math.cos(a) * 1.6, math.sin(a) * 1.6, 0.3), 'M_Kit_OWCrystalPink' if k % 2 else 'M_Kit_OWCrystal', tilt=(math.cos(a) * 22, math.sin(a) * 22)))
K.join(parts, 'CRYSTAL_Spire'); export('CRYSTAL_Spire')

# ===================================================== BOULDERS
for vi, seed in enumerate([11, 12]):
    K.reset()
    o = blob(f'ROCK_Big{vi}', 2.0, (0, 0, 1.0), 'M_Kit_OWRock', seed, rough=0.32, squash=(1.3, 1.0, 0.8))
    moss = blob('Moss', 1.7, (0, 0, 1.75), 'M_Kit_OWLeaf', seed + 50, rough=0.15, squash=(1.25, 0.95, 0.25))
    K.join([o, moss], f'ROCK_Big{vi}'); export(f'ROCK_Big{vi}')

# ===================================================== FLOATING ISLAND (top at z=0)
for vi, (r, seed) in enumerate([(6, 21), (10, 22)]):
    K.reset()
    parts = []
    top = K.cyl('Top', r, r * 0.92, 1.2, (0, 0, -0.6), 'M_Kit_OWGrassTop', segs=24, bevel=0.3)
    parts.append(top)
    under = blob('Under', r * 0.95, (0, 0, -r * 0.55), 'M_Kit_OWRock', seed, rough=0.28, squash=(1, 1, 1.25))
    # cut the top half of the under-rock by flattening verts above z=-1
    for v in under.data.vertices:
        if v.co.z > r * 0.55 - 0.7: v.co.z = r * 0.55 - 0.7
    under.data.update()
    parts.append(under)
    rnd = random.Random(seed)
    for k in range(8):
        a = rnd.random() * 6.28
        ln = rnd.uniform(2, 5)
        parts.append(K.cyl(f'Vine{k}', 0.12, 0.04, ln, (math.cos(a) * r * 0.9, math.sin(a) * r * 0.9, -1.0 - ln / 2), 'M_Kit_OWLeaf', segs=6, bevel=0))
    K.join(parts, f'ISLAND_Float{vi}'); export(f'ISLAND_Float{vi}')

# ===================================================== RUINS
K.reset()
parts = []
for s in (-1, 1):
    parts.append(K.soft_box(f'Leg{s}', (1.4, 1.4, 7), (s * 3.2, 0, 3.5), 'M_Kit_OWStone', bevel=0.12))
    parts.append(K.soft_box(f'Cap{s}', (1.8, 1.8, 0.5), (s * 3.2, 0, 7.2), 'M_Kit_OWStone', bevel=0.08))
for k in range(9):           # arch voussoirs
    a = math.pi * (k + 0.5) / 9
    x, z = math.cos(a) * 3.6, math.sin(a) * 2.4 + 7.5
    b = K.soft_box(f'V{k}', (1.15, 1.4, 0.9), (x, 0, z), 'M_Kit_OWStone', bevel=0.08)
    b.rotation_euler = (0, -(a - math.pi / 2), 0); parts.append(b)
parts.append(blob('MossTop', 1.4, (0, 0, 10.1), 'M_Kit_OWLeaf', 3, squash=(1.6, 0.8, 0.35)))
K.join(parts, 'RUIN_Arch'); export('RUIN_Arch')
K.reset()
parts = [K.soft_box('Base', (2.2, 2.2, 0.6), (0, 0, 0.3), 'M_Kit_OWStone', bevel=0.1)]
parts.append(K.cyl('Shaft', 0.8, 0.75, 5.5, (0, 0, 3.3), 'M_Kit_OWStone', segs=12, bevel=0.05))
parts.append(blob('Break', 0.85, (0, 0, 6.1), 'M_Kit_OWStone', 4, rough=0.4, squash=(1, 1, 0.5)))
K.join(parts, 'RUIN_Pillar'); export('RUIN_Pillar')

# ===================================================== THE ALARM (nightmare alarm clock, ≈5 m tall)
K.reset()
parts = []
body = K.cyl('Body', 2.0, 2.0, 1.6, (0, 0, 3.0), 'M_Kit_OWClockBody', segs=40, bevel=0.35)
body.rotation_euler = (math.radians(90), 0, 0); parts.append(body)
face = K.cyl('Face', 1.7, 1.7, 0.2, (0, -0.82, 3.0), 'M_Kit_OWClockFace', segs=40, bevel=0.05)
face.rotation_euler = (math.radians(90), 0, 0); parts.append(face)
for k in range(12):
    a = k / 12 * 2 * math.pi
    t = K.soft_box(f'Tick{k}', (0.1, 0.05, 0.3 if k % 3 == 0 else 0.16), (math.sin(a) * 1.45, -0.94, 3.0 + math.cos(a) * 1.45), 'M_Kit_OWInk', bevel=0.01)
    t.rotation_euler = (0, -a, 0); parts.append(t)
for s in (-1, 1):            # angry eyes (on the face) + clock hands as brows
    parts.append(K.uv_sphere(f'Eye{s}', 0.42, (s * 0.62, -0.95, 3.35), 'M_Kit_OWEyeGlow', squash=(1, 0.35, 0.75)))
    brow = K.soft_box(f'Brow{s}', (0.9, 0.08, 0.14), (s * 0.62, -0.98, 3.95), 'M_Kit_OWInk', bevel=0.02)
    brow.rotation_euler = (0, math.radians(s * 18), 0); parts.append(brow)
    bell = K.uv_sphere(f'Bell{s}', 0.85, (s * 1.25, 0, 5.05), 'M_Kit_OWBrass', squash=(1, 1, 0.75))
    bell.rotation_euler = (0, math.radians(s * 25), 0); parts.append(bell)
    leg = K.cyl(f'Leg{s}', 0.22, 0.12, 1.6, (s * 1.2, 0, 1.0), 'M_Kit_OWBrass', segs=10, bevel=0)
    leg.rotation_euler = (0, math.radians(-s * 18), 0); parts.append(leg)
    parts.append(K.uv_sphere(f'Foot{s}', 0.4, (s * 1.45, -0.1, 0.25), 'M_Kit_OWBrass', squash=(1, 1.4, 0.6)))
mouth = K.soft_box('Mouth', (1.2, 0.1, 0.3), (0, -0.98, 2.35), 'M_Kit_OWInk', bevel=0.05); parts.append(mouth)
for k in range(5):
    parts.append(K.soft_box(f'Tooth{k}', (0.16, 0.12, 0.2), (-0.48 + k * 0.24, -1.02, 2.42), 'M_Kit_OWClockFace', bevel=0.02))
parts.append(K.cyl('Hammer', 0.08, 0.08, 1.0, (0, 0, 5.4), 'M_Kit_OWBrass', segs=8, bevel=0))
parts.append(K.uv_sphere('HammerHead', 0.22, (0, 0, 5.95), 'M_Kit_OWBrass'))
parts.append(K.soft_box('Key', (0.3, 0.9, 0.5), (0, 1.15, 3.0), 'M_Kit_OWBrass', bevel=0.08))
K.join(parts, 'MONSTER_Alarm'); export('MONSTER_Alarm')

# ===================================================== DREAM KEY (≈1.2 m)
K.reset()
parts = [K.torus('Bow', 0.32, 0.08, (0, 0, 0.9), 'M_Kit_OWGold')]
parts[0].rotation_euler = (math.radians(90), 0, 0)
parts.append(K.star('Gem', 0.18, 0.09, 0.1, (0, 0, 0.9), 'M_Kit_OWCrystalPink', bevel=0.02))
parts.append(K.cyl('Shaft', 0.07, 0.07, 1.0, (0, 0, 0.1), 'M_Kit_OWGold', segs=10, bevel=0))
for k in range(2):
    parts.append(K.soft_box(f'Bit{k}', (0.22, 0.06, 0.1), (0.13, 0, -0.25 + k * 0.18), 'M_Kit_OWGold', bevel=0.01))
K.join(parts, 'PROP_DreamKey'); export('PROP_DreamKey')

# ===================================================== SPEED RING (R = 1.6)
K.reset()
K.torus('PROP_SpeedRing', 1.6, 0.14, (0, 0, 0), 'M_Kit_OWGold', segs=48, rsegs=10)
export('PROP_SpeedRing')
