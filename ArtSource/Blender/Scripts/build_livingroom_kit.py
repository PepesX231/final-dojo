"""
Living-room prop kit for the Pop-up Storybook Living Room (docs/VISUAL_RECONSTRUCTION.md).
Shape language (§5.2): chunky, soft, slightly crooked toy furniture — never perfect boxes.
Each prop → its own FBX (flat hierarchy, origin at floor centre, Blender -Y = Unity +Z = prop FRONT faces the camera side).
Materials are named; Unity remaps them to DreamToon materials (LivingRoomKitSetup.cs).

run: python3 -I build_livingroom_kit.py
"""
import math
import os

import bpy
import bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Environment', 'Props')
BLEND_DIR = os.path.join(ROOT, 'ArtSource', 'Blender', 'Environment')
os.makedirs(OUT, exist_ok=True)
os.makedirs(BLEND_DIR, exist_ok=True)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    return m


def link(o):
    bpy.context.scene.collection.objects.link(o)
    return o


def from_bm(name, bm, material):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = link(bpy.data.objects.new(name, me))
    o.data.materials.append(mat(material))
    return o


def apply_mods(o):
    bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers):
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
        bpy.ops.object.modifier_apply(modifier=m.name)


def soft_box(name, size, center, material, bevel=0.08, segs=4, sag=0.0, puff=0.0):
    """rounded box; sag lowers the top centre, puff inflates the faces (cushion feel)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    for v in bm.verts:
        x, y, z = v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]
        if puff:
            k = (1 - (2 * v.co.x) ** 2) * (1 - (2 * v.co.y) ** 2)
            z += math.copysign(puff * k * size[2], v.co.z) if abs(v.co.z) > 0.49 else 0
        if sag and v.co.z > 0.49:
            z -= sag * (1 - (2 * v.co.x) ** 2) * (1 - (2 * v.co.y) ** 2)
        v.co = Vector((x, y, z))
    o = from_bm(name, bm, material)
    o.location = center
    b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = segs; b.limit_method = 'ANGLE'
    apply_mods(o)
    return o


def cyl(name, r1, r2, depth, center, material, segs=20, bevel=0.03):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r1, radius2=r2, depth=depth)
    o = from_bm(name, bm, material)
    o.location = center
    if bevel:
        b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = 3; b.limit_method = 'ANGLE'
        apply_mods(o)
    return o


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = name; objs[0].data.name = name
    return objs[0]


def uv_cube(o, size=1.0):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=size)
    bpy.ops.object.mode_set(mode='OBJECT')


def smooth(o, angle=50):
    for p in o.data.polygons: p.use_smooth = True
    if hasattr(o.data, 'set_sharp_from_angle'):
        o.data.set_sharp_from_angle(angle=math.radians(angle))


def crooked(o, amount=0.02, seed=1):
    """tiny hand-made irregularity: shear + scale so nothing is mechanically perfect."""
    import random
    r = random.Random(seed)
    sh = Matrix.Shear('XZ', 4, (r.uniform(-amount, amount), r.uniform(-amount, amount)))
    o.data.transform(sh)


def export(name):
    bpy.context.view_layer.update()
    for o in bpy.context.scene.objects:
        if o.type == 'MESH': smooth(o)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, name + '.blend'))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type == 'MESH')
    print(f"EXPORTED {name} tris={tris} objs={[o.name for o in bpy.context.scene.objects]}")


# ======================================================================== SOFA  (5.0 x 1.8 x 1.2)
reset()
W, D = 5.0, 1.8
parts = []
parts.append(soft_box('Base', (W - 0.3, D - 0.15, 0.32), (0, 0, 0.32), 'M_Kit_Fabric', bevel=0.12))
for i in range(3):                                   # seat cushions, each slightly different
    x = (i - 1) * 1.45
    parts.append(soft_box(f'Seat{i}', (1.4, 1.25, 0.24), (x, 0.18, 0.6 + 0.01 * i), 'M_Kit_FabricLight', bevel=0.1, sag=0.03, puff=0.15))
for i in range(3):                                   # back cushions, leaning back, uneven
    x = (i - 1) * 1.45
    b = soft_box(f'Back{i}', (1.38, 0.32, 0.62), (x, -0.62, 0.92), 'M_Kit_FabricLight', bevel=0.12, puff=0.2)
    b.rotation_euler = (math.radians(-12 + i * 2), 0, math.radians((i - 1) * 2)); parts.append(b)
parts.append(soft_box('BackFrame', (W - 0.4, 0.3, 0.75), (0, -0.78, 0.75), 'M_Kit_Fabric', bevel=0.13))
for s in (-1, 1):                                    # rolled arms
    a = soft_box(f'Arm{s}', (0.42, D - 0.1, 0.62), (s * (W / 2 - 0.25), 0, 0.62), 'M_Kit_Fabric', bevel=0.18, puff=0.1)
    parts.append(a)
    for yy in (-0.65, 0.65):                         # stubby wooden feet
        parts.append(cyl(f'Foot{s}{yy}', 0.09, 0.07, 0.16, (s * (W / 2 - 0.3), yy, 0.08), 'M_Kit_WoodDark', segs=12))
for o in parts: apply_mods(o)
sofa = join(parts, 'Sofa')
bpy.context.view_layer.update()
sofa.data.transform(Matrix.Translation(Vector((0, 0, 0))))
uv_cube(sofa, 1.2)
crooked(sofa, 0.015, 3)
export('PROP_Sofa')

# ======================================================================== COFFEE TABLE (2.6 x 1.6 x 0.6)
reset()
parts = []
top = cyl('Top', 1.0, 1.0, 0.13, (0, 0, 0.535), 'M_Kit_PaintedBlue', segs=40, bevel=0.05)
top.scale = (1.3, 0.8, 1); apply_mods(top); bpy.ops.object.select_all(action='DESELECT'); top.select_set(True)
bpy.context.view_layer.objects.active = top; bpy.ops.object.transform_apply(scale=True)
parts.append(top)
rim = cyl('Rim', 1.03, 1.03, 0.05, (0, 0, 0.47), 'M_Kit_WoodLight', segs=40, bevel=0.02)
rim.scale = (1.3, 0.8, 1); bpy.ops.object.select_all(action='DESELECT'); rim.select_set(True)
bpy.context.view_layer.objects.active = rim; bpy.ops.object.transform_apply(scale=True)
parts.append(rim)
for i, (x, y) in enumerate([(-0.9, -0.45), (0.9, -0.45), (-0.9, 0.45), (0.9, 0.45)]):
    leg = cyl(f'Leg{i}', 0.07, 0.11, 0.46, (x, y, 0.23), 'M_Kit_WoodLight', segs=14)
    leg.rotation_euler = (math.radians(-6 * math.copysign(1, y)), math.radians(6 * math.copysign(1, x)), 0)
    parts.append(leg)
    parts.append(cyl(f'Ball{i}', 0.12, 0.12, 0.08, (x * 1.04, y * 1.06, 0.04), 'M_Kit_PaintedBlue', segs=14))
for o in parts: apply_mods(o)
table = join(parts, 'CoffeeTable')
uv_cube(table, 1.0)
export('PROP_CoffeeTable')

# ======================================================================== FLOOR LAMP (3.0 tall) — leans, a little surreal
reset()
parts = []
parts.append(cyl('Base', 0.38, 0.42, 0.12, (0, 0, 0.06), 'M_Kit_WoodDark', segs=28))
parts.append(cyl('Knob', 0.12, 0.16, 0.12, (0, 0, 0.17), 'M_Kit_WoodDark', segs=16))
pole_len = 2.25
pole = cyl('Pole', 0.045, 0.055, pole_len, (0, 0, 0.2 + pole_len / 2), 'M_Kit_Brass', segs=12, bevel=0)
parts.append(pole)
# gentle bend: three segments
neck = cyl('Neck', 0.04, 0.045, 0.5, (0.12, 0, 2.62), 'M_Kit_Brass', segs=12, bevel=0)
neck.rotation_euler = (0, math.radians(28), 0); parts.append(neck)
shade = cyl('Shade', 0.42, 0.72, 0.62, (0.42, 0, 2.62), 'M_Kit_LampShade', segs=36, bevel=0.02)
shade.rotation_euler = (0, math.radians(14), 0); parts.append(shade)
bulb = cyl('Bulb', 0.18, 0.18, 0.2, (0.38, 0, 2.38), 'M_Kit_Bulb', segs=16, bevel=0.06)
parts.append(bulb)
for o in parts: apply_mods(o)
# shade: delete caps so it reads as a fabric drum open at the bottom
lamp_body = join([p for p in parts if p.name not in ('Shade', 'Bulb')], 'LampBody')
uv_cube(lamp_body, 1.0)
shade.name = 'LampShade'
bm = bmesh.new(); bm.from_mesh(shade.data)
bm.faces.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[f for f in bm.faces if abs(f.normal.z) > 0.9 and len(f.verts) > 8], context='FACES')
bm.to_mesh(shade.data); bm.free()
uv = shade.data.uv_layers.new(name='UVMap')
for poly in shade.data.polygons:                     # cylindrical UV for pleats
    for li in poly.loop_indices:
        co = shade.data.vertices[shade.data.loops[li].vertex_index].co
        uv.data[li].uv = ((math.atan2(co.y, co.x) / (2 * math.pi)) * 4 % 1.0, (co.z + 0.31) / 0.62)
bulb.name = 'LampBulb'
export('PROP_FloorLamp')

# ======================================================================== TOY BLOCK (1.2 cube), painted faces atlas
reset()
bm = bmesh.new()
bmesh.ops.create_cube(bm, size=1.2)
o = from_bm('ToyBlock', bm, 'M_Kit_ToyBlock')
o.location = (0, 0, 0.6)
b = o.modifiers.new('bev', 'BEVEL'); b.width = 0.09; b.segments = 4; b.limit_method = 'ANGLE'
apply_mods(o)
uvl = o.data.uv_layers.new(name='UVMap')
quad = {(0, 0, 1): 1, (0, 0, -1): 0, (1, 0, 0): 0, (-1, 0, 0): 2, (0, 1, 0): 3, (0, -1, 0): 2}
for poly in o.data.polygons:
    n = poly.normal
    ax = max(range(3), key=lambda i: abs(n[i]))
    key = tuple((1 if n[i] > 0 else -1) if i == ax else 0 for i in range(3))
    q = quad.get(key, 0)
    qx, qy = (q % 2) * 0.5, (1 - q // 2) * 0.5 - 0.5 + 0.5
    for li in poly.loop_indices:
        co = o.data.vertices[o.data.loops[li].vertex_index].co - Vector((0, 0, 0))
        others = [i for i in range(3) if i != ax]
        u = (co[others[0]] / 1.2 + 0.5); v = ((co[others[1]] - (0 if others[1] != 2 else 0)) / 1.2 + 0.5)
        if others[1] == 2: v = (co.z) / 1.2 + 0.5
        u = min(max(u, 0.02), 0.98); v = min(max(v, 0.02), 0.98)
        uvl.data[li].uv = (qx + u * 0.5, qy + v * 0.5)
o.data.transform(Matrix.Translation(Vector((0, 0, 0))))
export('PROP_ToyBlock')

# ======================================================================== PUFFY CUSHION (1 x 1 x 1 unit, scaled per use in Unity)
reset()
c = soft_box('Cushion', (0.98, 0.98, 0.84), (0, 0, 0.5), 'M_Kit_Cushion', bevel=0.16, segs=5, puff=0.12)
# tufted centre button dimple + corner tassels are skipped (too small to read at gameplay distance)
bm = bmesh.new(); bm.from_mesh(c.data)
for v in bm.verts:
    d = math.hypot(v.co.x, v.co.y)
    if v.co.z > 0 and d < 0.22:
        v.co.z -= 0.08 * (1 - d / 0.22)
bm.to_mesh(c.data); bm.free()
c.data.transform(Matrix.Translation(Vector((0, 0, 0))))
uv_cube(c, 1.0)
btn = cyl('Button', 0.06, 0.06, 0.04, (0, 0, 0.96), 'M_Kit_CushionButton', segs=12, bevel=0.015)
export('PROP_Cushion')

print('KIT DONE')
