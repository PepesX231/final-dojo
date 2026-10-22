"""
Bedroom lobby kit — "THE NIGHT ROOM" diorama (docs/LOBBY_DESIGN.md).
Pip's bedroom is the game's lobby: a cut-away toy diorama floating in the dream sky. Shape language: chunky,
soft, slightly crooked toy furniture (rounded bevels, puffy cushions), apple motifs everywhere.
Conventions (same as every DREAM RUSH kit): one FBX per prop, flat hierarchy, origin at the floor centre of the prop,
FRONT faces Blender -Y (= Unity +Z). Materials M_Kit_BR_* are remapped to DreamToon in Unity (BedroomHubBuilder).
run: python3 -I build_bedroom_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
import dr_bkit as K  # noqa: E402
from mathutils import Vector  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'Bedroom', 'Models'), os.path.join('ArtSource', 'Blender', 'Bedroom'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))
    return o


def leaf(name, center, size, yaw=0, tilt=30, material='M_Kit_BR_Leaf'):
    o = SP(name, 1.0, (0, 0, 0), material, segs=16, rings=8, squash=(size * 0.5, size, size * 0.12))
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:   # pointed tip
        if v.co.y > 0: v.co.x *= max(0.15, 1 - v.co.y / size * 0.8)
    bm.to_mesh(o.data); bm.free()
    o.location = Vector((0, size * 0.9, 0))
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.rotation_euler = (math.radians(tilt), 0, math.radians(yaw))
    o.location = center
    return o


def apple_shape(name, r, center, material, squash=(1, 1, 1)):
    """apple: sphere with a dimple at top and bottom (r = radius)."""
    o = SP(name, r, (0, 0, 0), material, segs=32, rings=18)
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        n = v.co.normalized()
        dimple = max(0.0, n.z) ** 6 * 0.28 + max(0.0, -n.z) ** 8 * 0.12
        v.co = v.co * (1 - dimple * (1 - abs(n.z) * 0.2))
        v.co.x *= squash[0]; v.co.y *= squash[1]; v.co.z *= squash[2] * (1 + 0.06 * (1 - n.z))
    bm.to_mesh(o.data); bm.free()
    o.location = center
    return o


def text_mesh(name, s, size, depth, center, material, font_rot=(90, 0, 0)):
    cu = bpy.data.curves.new(name, 'FONT'); cu.body = s; cu.size = size; cu.extrude = depth; cu.bevel_depth = depth * 0.35
    cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    o = K.link(bpy.data.objects.new(name, cu))
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.convert(target='MESH')
    o = bpy.context.view_layer.objects.active
    o.data.materials.clear(); o.data.materials.append(K.mat(material))
    rot(o, *font_rot); o.location = center
    return o


def finish(name, parts):
    o = K.join(parts, name)
    K.export(name)


# ===================================================================== BED_Apple (2.0 × 2.75, headboard at back +Y)
K.reset()
p = []
p.append(SB('Frame', (1.95, 2.6, 0.34), (0, 0, 0.29), 'M_Kit_BR_Wood', bevel=0.07))
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    p.append(CY(f'Foot{i}', 0.1, 0.08, 0.14, (dx * 0.85, dy * 1.18, 0.07), 'M_Kit_BR_WoodDark', segs=14, bevel=0.02))
p.append(SB('Mattress', (1.84, 2.48, 0.26), (0, 0, 0.58), 'M_Kit_BR_Cream', bevel=0.1, puff=0.12))
p.append(SB('Duvet', (2.02, 1.62, 0.16), (0, -0.44, 0.76), 'M_Kit_BR_Duvet', bevel=0.08, puff=0.35))
p.append(SB('DuvetFold', (2.04, 0.3, 0.17), (0, 0.4, 0.82), 'M_Kit_BR_DuvetB', bevel=0.07, puff=0.25))
for i, x in enumerate((-0.44, 0.44)):
    p.append(rot(SB(f'Pillow{i}', (0.78, 0.5, 0.22), (x, 0.86, 0.86), 'M_Kit_BR_Pillow', bevel=0.1, puff=0.5), x=-14, z=(-6 if i else 5)))
# apple headboard + leaf + stem + seed inlays
p.append(rot(apple_shape('Headboard', 1.0, (0, 1.36, 1.15), 'M_Kit_BR_Red', squash=(1.08, 0.13, 0.86)), z=0))
p.append(CY('Stem', 0.06, 0.05, 0.42, (0.06, 1.36, 2.1), 'M_Kit_BR_WoodDark', segs=10, bevel=0.015))
p.append(leaf('Leaf', (0.12, 1.36, 2.22), 0.36, yaw=-60, tilt=-20))
for i, (x, z) in enumerate([(-0.32, 1.25), (0.32, 1.25)]):
    p.append(rot(SP(f'Seed{i}', 1.0, (x, 1.21, z), 'M_Kit_BR_WoodDark', segs=14, rings=8, squash=(0.08, 0.03, 0.13)), y=(14 if i else -14)))
finish('BED_Apple', p)

# ===================================================================== NIGHTSTAND + LAMP_Mushroom
K.reset()
p = [SB('Body', (0.72, 0.6, 0.6), (0, 0, 0.34), 'M_Kit_BR_Wood', bevel=0.06)]
p.append(SB('Drawer', (0.58, 0.05, 0.22), (0, -0.31, 0.4), 'M_Kit_BR_Cream', bevel=0.02))
p.append(SP('Knob', 0.045, (0, -0.35, 0.4), 'M_Kit_BR_Gold', segs=12, rings=8))
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    p.append(CY(f'Leg{i}', 0.04, 0.03, 0.06, (dx * 0.3, dy * 0.24, 0.03), 'M_Kit_BR_WoodDark', segs=10, bevel=0))
finish('NIGHTSTAND', p)

K.reset()
p = [CY('Base', 0.17, 0.19, 0.05, (0, 0, 0.025), 'M_Kit_BR_WoodDark', segs=20)]
p.append(CY('Stem', 0.06, 0.08, 0.38, (0, 0, 0.23), 'M_Kit_BR_Cream', segs=16, bevel=0.01))
cap = SP('Cap', 0.33, (0, 0, 0.44), 'M_Kit_BR_Shade', segs=28, rings=14, squash=(1, 1, 0.62))
bm = bmesh.new(); bm.from_mesh(cap.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.02], context='VERTS'); bm.to_mesh(cap.data); bm.free()
sol = cap.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.03; K.apply_mods(cap)
p.append(cap)
r = random.Random(3)
for i in range(7):
    a = r.uniform(0, 6.28); e = r.uniform(0.35, 1.2)
    n = Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e) * 0.62))
    p.append(SP(f'Dot{i}', 0.045, (n.x * 0.33, n.y * 0.33, 0.44 + n.z * 0.33 * 1.0), 'M_Kit_BR_Cream', segs=10, rings=6, squash=(1, 1, 0.5)))
finish('LAMP_Mushroom', p)

# ===================================================================== SHELF_Books (2.0 wide, 2.5 tall, front -Y)
K.reset()
p = [SB('Back', (1.9, 0.06, 2.4), (0, 0.24, 1.2), 'M_Kit_BR_WoodDark', bevel=0.02)]
for s in (-1, 1): p.append(SB(f'Side{s}', (0.1, 0.52, 2.46), (s * 0.95, 0, 1.23), 'M_Kit_BR_Wood', bevel=0.03))
for i, z in enumerate((0.06, 0.66, 1.26, 1.86)):
    p.append(SB(f'Board{i}', (1.86, 0.5, 0.06), (0, 0, z), 'M_Kit_BR_Wood', bevel=0.02))
p.append(SB('Crown', (2.1, 0.6, 0.12), (0, 0, 2.48), 'M_Kit_BR_WoodDark', bevel=0.05))
r = random.Random(11)
book_mats = ['M_Kit_BR_Book1', 'M_Kit_BR_Book2', 'M_Kit_BR_Book3', 'M_Kit_BR_Book4', 'M_Kit_BR_Red']
for shelf, z in enumerate((0.09, 0.69, 1.29, 1.89)):
    x = -0.86
    while x < 0.8:
        w = r.uniform(0.07, 0.15); h = r.uniform(0.34, 0.5) if shelf < 3 else r.uniform(0.25, 0.4)
        if r.random() < 0.12 and x > -0.6:   # a leaning book
            b = SB(f'Book{shelf}_{x:.2f}', (w, 0.36, h), (x + 0.1, -0.02, z + h / 2 - 0.02), r.choice(book_mats), bevel=0.012, segs=1)
            p.append(rot(b, y=18)); x += w + 0.16; continue
        if r.random() < 0.1:   # a gap with a little stack lying down
            for k in range(3):
                p.append(SB(f'Stack{shelf}_{x:.2f}_{k}', (0.22, 0.32, 0.05), (x + 0.12, -0.02, z + 0.025 + k * 0.05), r.choice(book_mats), bevel=0.01, segs=1))
            x += 0.28; continue
        p.append(SB(f'Book{shelf}_{x:.2f}', (w, 0.36, h), (x + w / 2, -0.02 + r.uniform(-0.03, 0.03), z + h / 2), r.choice(book_mats), bevel=0.012, segs=1))
        x += w + 0.008
# moon globe on top
p.append(SP('Globe', 0.2, (-0.55, 0, 2.78), 'M_Kit_BR_Moon', segs=20, rings=12))
p.append(CY('GlobeStand', 0.1, 0.13, 0.06, (-0.55, 0, 2.57), 'M_Kit_BR_Gold', segs=14))
finish('SHELF_Books', p)

# ===================================================================== DESK (1.9 × 0.9, front -Y) with lamp, pencils, drawing
K.reset()
p = [SB('Top', (1.9, 0.9, 0.08), (0, 0, 0.8), 'M_Kit_BR_Wood', bevel=0.03)]
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    p.append(CY(f'Leg{i}', 0.05, 0.04, 0.78, (dx * 0.86, dy * 0.38, 0.39), 'M_Kit_BR_WoodDark', segs=12, bevel=0))
p.append(SB('Drawers', (0.5, 0.8, 0.55), (0.62, 0, 0.47), 'M_Kit_BR_Wood', bevel=0.03))
for k in range(2):
    p.append(SB(f'DrawerFace{k}', (0.42, 0.04, 0.2), (0.62, -0.41, 0.36 + k * 0.25), 'M_Kit_BR_Cream', bevel=0.015))
    p.append(SP(f'DKnob{k}', 0.03, (0.62, -0.44, 0.36 + k * 0.25), 'M_Kit_BR_Gold', segs=10, rings=6))
p.append(rot(SB('Paper', (0.5, 0.36, 0.006), (-0.2, -0.05, 0.845), 'M_Kit_BR_Paper', bevel=0.002, segs=1), z=8))
p.append(CY('Cup', 0.08, 0.07, 0.18, (0.45, 0.2, 0.93), 'M_Kit_BR_Red', segs=16, bevel=0.01))
for k, (m, dx) in enumerate([('M_Kit_BR_Book1', -0.02), ('M_Kit_BR_Book3', 0.03), ('M_Kit_BR_Gold', 0.0)]):
    p.append(rot(CY(f'Pencil{k}', 0.012, 0.012, 0.28, (0.45 + dx, 0.2 + dx, 1.05), m, segs=6, bevel=0), x=(k - 1) * 8, y=dx * 200))
# arm lamp
p.append(CY('LampBase', 0.12, 0.13, 0.04, (-0.7, 0.25, 0.86), 'M_Kit_BR_Gold', segs=16))
a1 = rot(CY('Arm1', 0.02, 0.02, 0.5, (-0.7, 0.25 - 0.08, 1.08), 'M_Kit_BR_Gold', segs=8, bevel=0), x=-20); p.append(a1)
a2 = rot(CY('Arm2', 0.02, 0.02, 0.45, (-0.7, 0.0, 1.38), 'M_Kit_BR_Gold', segs=8, bevel=0), x=60); p.append(a2)
head = rot(CY('Head', 0.05, 0.14, 0.2, (-0.7, -0.2, 1.4), 'M_Kit_BR_Shade', segs=18, bevel=0.01), x=-130); p.append(head)
finish('DESK', p)

# ===================================================================== WARDROBE (1.3 × 0.7 × 2.4)
K.reset()
p = [SB('Body', (1.3, 0.7, 2.25), (0, 0, 1.2), 'M_Kit_BR_Wood', bevel=0.05)]
for s in (-1, 1):
    p.append(SB(f'Door{s}', (0.58, 0.05, 1.95), (s * 0.31, -0.36, 1.2), 'M_Kit_BR_Cream', bevel=0.03))
    p.append(apple_shape(f'Handle{s}', 0.06, (s * 0.07, -0.42, 1.25), 'M_Kit_BR_Red'))
p.append(SB('Crown', (1.42, 0.78, 0.14), (0, 0, 2.38), 'M_Kit_BR_WoodDark', bevel=0.05))
p.append(SP('CrownApple', 0.17, (0, -0.1, 2.6), 'M_Kit_BR_Red', segs=20, rings=12))
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    p.append(SP(f'Foot{i}', 0.08, (dx * 0.55, dy * 0.27, 0.06), 'M_Kit_BR_WoodDark', segs=12, rings=8))
finish('WARDROBE', p)

# ===================================================================== JAR_Dream (memories)
K.reset()
p = [CY('Glass', 0.2, 0.2, 0.46, (0, 0, 0.25), 'M_Kit_BR_Glass', segs=24, bevel=0.03)]
p.append(CY('Lid', 0.22, 0.22, 0.07, (0, 0, 0.51), 'M_Kit_BR_WoodDark', segs=24, bevel=0.02))
p.append(SB('Ribbon', (0.46, 0.46, 0.04), (0, 0, 0.43), 'M_Kit_BR_Red', bevel=0.015, segs=1))
r = random.Random(5)
for i in range(6):
    p.append(rot(K.star(f'Mem{i}', 0.07, 0.03, 0.03, (r.uniform(-0.09, 0.09), r.uniform(-0.09, 0.09), 0.1 + i * 0.055), 'M_Kit_BR_Memory', bevel=0.01),
                 x=r.uniform(0, 90), z=r.uniform(0, 90)))
finish('JAR_Dream', p)

# ===================================================================== WINDOW_Round (frame on the back wall, radius 1.3)
K.reset()
p = [rot(K.torus('Frame', 1.3, 0.1, (0, 0, 0), 'M_Kit_BR_Trim', segs=56, rsegs=12), x=90)]
p.append(SB('BarH', (2.6, 0.08, 0.07), (0, 0, 0), 'M_Kit_BR_Trim', bevel=0.02))
p.append(SB('BarV', (0.07, 0.08, 2.6), (0, 0, 0), 'M_Kit_BR_Trim', bevel=0.02))
p.append(SB('Sill', (2.4, 0.42, 0.1), (0, -0.16, -1.36), 'M_Kit_BR_Wood', bevel=0.03))
for s in (-1, 1):   # tied-back curtains
    for k in range(5):
        c = SB(f'Curt{s}_{k}', (0.16, 0.08, 2.4 - abs(k - 2) * 0.08), (s * (1.3 + k * 0.1), -0.14 - (k % 2) * 0.03, 0.15), 'M_Kit_BR_Curtain', bevel=0.05, puff=0.2)
        p.append(rot(c, y=s * (4 + k * 2)))
    p.append(rot(SB(f'Tie{s}', (0.6, 0.12, 0.1), (s * 1.48, -0.2, -0.25), 'M_Kit_BR_Gold', bevel=0.03), y=s * 10))
p.append(SB('Rod', (3.9, 0.05, 0.05), (0, -0.16, 1.48), 'M_Kit_BR_Gold', bevel=0.02))
finish('WINDOW_Round', p)

# ===================================================================== DOOR_Arch (1.2 × 2.3, front -Y)
K.reset()
p = [SB('Panel', (1.1, 0.08, 1.7), (0, 0, 0.85), 'M_Kit_BR_Wood', bevel=0.02)]
top = rot(CY('Top', 0.55, 0.55, 0.08, (0, 0, 1.7), 'M_Kit_BR_Wood', segs=32, bevel=0.02), x=90); p.append(top)
p.append(rot(K.torus('Frame', 0.62, 0.06, (0, -0.02, 1.7), 'M_Kit_BR_Trim', segs=40, rsegs=8), x=90))
for s in (-1, 1): p.append(SB(f'Jamb{s}', (0.12, 0.14, 1.7), (s * 0.62, -0.02, 0.85), 'M_Kit_BR_Trim', bevel=0.02))
p.append(rot(K.torus('Porthole', 0.18, 0.035, (0, -0.06, 1.62), 'M_Kit_BR_Gold', segs=24, rsegs=8), x=90))
p.append(SP('Knob', 0.06, (0.4, -0.08, 0.95), 'M_Kit_BR_Gold', segs=14, rings=8))
p.append(SB('Sign', (0.5, 0.03, 0.22), (0, -0.06, 1.25), 'M_Kit_BR_Paper', bevel=0.01, segs=1))
finish('DOOR_Arch', p)

# ===================================================================== RUG_Round (concentric, 4.6 m)
K.reset()
p = [CY('R0', 2.3, 2.3, 0.03, (0, 0, 0.015), 'M_Kit_BR_Rug1', segs=64, bevel=0.012)]
p.append(CY('R1', 1.85, 1.85, 0.03, (0, 0, 0.02), 'M_Kit_BR_Rug2', segs=64, bevel=0.01))
p.append(CY('R2', 1.45, 1.45, 0.03, (0, 0, 0.025), 'M_Kit_BR_Rug1', segs=64, bevel=0.01))
p.append(CY('R3', 0.9, 0.9, 0.03, (0, 0, 0.03), 'M_Kit_BR_Rug3', segs=48, bevel=0.01))
p.append(rot(K.star('Star', 0.55, 0.24, 0.02, (0, 0, 0.05), 'M_Kit_BR_Gold', bevel=0.005), x=90))
finish('RUG_Round', p)

# ===================================================================== toys
K.reset()   # TOY_Rocket (0.9 tall)
p = [CY('Body', 0.16, 0.16, 0.5, (0, 0, 0.42), 'M_Kit_BR_Cream', segs=20, bevel=0.03)]
p.append(CY('Nose', 0.16, 0.0, 0.3, (0, 0, 0.82), 'M_Kit_BR_Red', segs=20, bevel=0.0))
p.append(rot(K.torus('Window', 0.07, 0.02, (0, -0.16, 0.5), 'M_Kit_BR_Gold', segs=20, rsegs=8), x=90))
p.append(SP('Glass', 0.065, (0, -0.15, 0.5), 'M_Kit_BR_Moon', segs=14, rings=8, squash=(1, 0.4, 1)))
for i in range(3):
    a = i * 120
    f = SB(f'Fin{i}', (0.05, 0.22, 0.26), (0, 0, 0), 'M_Kit_BR_Red', bevel=0.02)
    f.location = (math.sin(math.radians(a)) * 0.2, -math.cos(math.radians(a)) * 0.2, 0.2); rot(f, z=-a); p.append(f)
p.append(CY('Flame', 0.1, 0.03, 0.14, (0, 0, 0.1), 'M_Kit_BR_Memory', segs=12, bevel=0))
finish('TOY_Rocket', p)

K.reset()   # TOY_Sheep (plush, 0.6)
p = []
r = random.Random(8)
for i in range(14):
    p.append(SP(f'Wool{i}', r.uniform(0.11, 0.15), (r.uniform(-0.2, 0.2), r.uniform(-0.12, 0.14), 0.3 + r.uniform(-0.08, 0.1)), 'M_Kit_BR_Cream', segs=12, rings=8))
p.append(SP('Head', 0.12, (0, -0.24, 0.34), 'M_Kit_BR_Plush', segs=16, rings=10, squash=(0.9, 1, 1.05)))
for s in (-1, 1):
    p.append(rot(SP(f'Ear{s}', 0.06, (s * 0.12, -0.22, 0.38), 'M_Kit_BR_Plush', segs=10, rings=6, squash=(1.6, 0.5, 0.7)), y=s * 25))
    p.append(SP(f'Eye{s}', 0.018, (s * 0.045, -0.35, 0.37), 'M_Kit_BR_Ink', segs=8, rings=6))
for i, (dx, dy) in enumerate([(-1, -1), (1, -1), (-1, 1), (1, 1)]):
    p.append(CY(f'Leg{i}', 0.04, 0.04, 0.16, (dx * 0.12, dy * 0.08, 0.08), 'M_Kit_BR_Plush', segs=10, bevel=0.015))
finish('TOY_Sheep', p)

K.reset()   # TOY_Train (a little yellow engine)
p = [SB('Cab', (0.34, 0.6, 0.3), (0, 0, 0.27), 'M_Kit_BR_Yellow', bevel=0.05)]
p.append(SB('Roof', (0.4, 0.66, 0.06), (0, 0, 0.45), 'M_Kit_BR_Ink', bevel=0.02))
for s in (-1, 1):
    for k in range(2): p.append(rot(CY(f'Wheel{s}{k}', 0.07, 0.07, 0.05, (s * 0.18, -0.18 + k * 0.36, 0.08), 'M_Kit_BR_Ink', segs=14, bevel=0.01), y=90))
    p.append(SB(f'Win{s}', (0.02, 0.4, 0.12), (s * 0.17, 0, 0.32), 'M_Kit_BR_Moon', bevel=0.005, segs=1))
for s in (-1, 1): p.append(SP(f'Light{s}', 0.03, (s * 0.1, -0.31, 0.2), 'M_Kit_BR_Memory', segs=8, rings=6))
finish('TOY_Train', p)

K.reset()   # TOY_Ball
p = [SP('Ball', 0.22, (0, 0, 0.22), 'M_Kit_BR_Book2', segs=24, rings=14)]
p.append(rot(K.torus('Band', 0.215, 0.04, (0, 0, 0.22), 'M_Kit_BR_Cream', segs=32, rsegs=8), x=90))
p.append(K.torus('Band2', 0.215, 0.04, (0, 0, 0.22), 'M_Kit_BR_Red', segs=32, rsegs=8))
finish('TOY_Ball', p)

K.reset()   # PLANT_Pot
p = [CY('Pot', 0.22, 0.17, 0.36, (0, 0, 0.18), 'M_Kit_BR_Red', segs=20, bevel=0.03)]
p.append(CY('Rim', 0.25, 0.25, 0.06, (0, 0, 0.37), 'M_Kit_BR_Red', segs=20, bevel=0.02))
p.append(CY('Soil', 0.21, 0.21, 0.02, (0, 0, 0.39), 'M_Kit_BR_WoodDark', segs=20, bevel=0))
for i in range(7):
    p.append(leaf(f'L{i}', (0, 0, 0.42), 0.32 + (i % 3) * 0.06, yaw=i * 51, tilt=40 + (i % 2) * 18))
finish('PLANT_Pot', p)

# ===================================================================== diorama base (underside of the floating room, 11 × 9)
K.reset()
p = []
p.append(SB('Layer1', (11.2, 9.2, 0.5), (0, 0, -0.25), 'M_Kit_BR_Cake1', bevel=0.22, segs=4))
p.append(SB('Layer2', (10.9, 8.9, 0.42), (0, 0, -0.66), 'M_Kit_BR_Cake2', bevel=0.2, segs=4, puff=0.02))
p.append(SB('Layer3', (10.4, 8.4, 0.36), (0, 0, -1.02), 'M_Kit_BR_Cake3', bevel=0.18, segs=4))
r = random.Random(21)
for i in range(26):   # cloud puffs underneath
    x, y = r.uniform(-4.6, 4.6), r.uniform(-3.6, 3.6)
    p.append(SP(f'Puff{i}', r.uniform(0.7, 1.4), (x, y, -1.25 - r.uniform(0.1, 0.6)), 'M_Kit_BR_Cloud', segs=16, rings=10, squash=(1.3, 1.1, 0.6)))
finish('DIORAMA_Base', p)

# ===================================================================== sky props
K.reset()   # CLOUD_Puff
r = random.Random(2); p = []
for i in range(6):
    p.append(SP(f'C{i}', r.uniform(0.5, 0.85), ((i - 2.5) * 0.6, r.uniform(-0.2, 0.2), r.uniform(-0.1, 0.3) - abs(i - 2.5) * 0.08), 'M_Kit_BR_Cloud', segs=16, rings=10))
finish('CLOUD_Puff', p)

K.reset()   # MOON_Crescent (boolean)
m = SP('Moon', 1.0, (0, 0, 0), 'M_Kit_BR_Moon', segs=40, rings=20, squash=(1, 0.45, 1))
cut = SP('Cut', 0.92, (0.42, 0, 0.22), 'M_Kit_BR_Moon', segs=40, rings=20, squash=(1, 0.8, 1))
b = m.modifiers.new('bool', 'BOOLEAN'); b.operation = 'DIFFERENCE'; b.object = cut; b.solver = 'EXACT'
K.apply_mods(m); bpy.data.objects.remove(cut)
finish('MOON_Crescent', [m])

K.reset()   # ZZZ letters
p = [text_mesh('Z1', 'Z', 0.9, 0.12, (0, 0, 0), 'M_Kit_BR_Gold'),
     text_mesh('Z2', 'Z', 0.65, 0.1, (0.62, 0, 0.55), 'M_Kit_BR_Gold'),
     text_mesh('Z3', 'Z', 0.45, 0.08, (1.05, 0, 0.98), 'M_Kit_BR_Gold')]
finish('PROP_ZZZ', p)

K.reset()   # FENCE_Toy segment (1 m, low picket fence for the open edges)
p = [SB('Rail', (1.0, 0.05, 0.06), (0, 0, 0.32), 'M_Kit_BR_Trim', bevel=0.02)]
for i in range(4):
    x = -0.375 + i * 0.25
    p.append(SB(f'Picket{i}', (0.09, 0.05, 0.42), (x, 0, 0.21), 'M_Kit_BR_Trim', bevel=0.02))
    p.append(rot(SB(f'Tip{i}', (0.064, 0.05, 0.064), (x, 0, 0.42), 'M_Kit_BR_Trim', bevel=0.01, segs=1), y=45))
finish('FENCE_Toy', p)
K.reset()   # WALLPLATE_Round: 3.0 × 3.0 wall panel with a round window hole (r 1.32), wallpaper material (world-UV in Unity)
plate = SB('Plate', (3.0, 0.25, 3.0), (0, 0, 1.5), 'M_Kit_BR_Wallpaper', bevel=0.005, segs=1)
hole = CY('Hole', 1.32, 1.32, 1.0, (0, 0, 1.5), 'M_Kit_BR_Wallpaper', segs=64, bevel=0)
rot(hole, x=90)
b = plate.modifiers.new('bool', 'BOOLEAN'); b.operation = 'DIFFERENCE'; b.object = hole; b.solver = 'EXACT'
K.apply_mods(plate); bpy.data.objects.remove(hole)
finish('WALLPLATE_Round', [plate])
print('BEDROOM KIT DONE')
