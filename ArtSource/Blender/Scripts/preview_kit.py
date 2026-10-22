"""Shape/colour preview of the living-room kit (import FBX, flat palette colours, one 3/4 render). usage: python3 -I preview_kit.py <out.png>"""
import bpy, os, sys, math
from mathutils import Vector
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
PROPS = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Environment', 'Props')
TEX = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Environment', 'Textures')
out = sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
cols = {'M_Kit_Fabric': ('T_FabricRose.png', None), 'M_Kit_FabricLight': ('T_FabricRose.png', 1.15), 'M_Kit_WoodDark': (None, (0.42, 0.25, 0.16)),
        'M_Kit_PaintedBlue': (None, (0.62, 0.76, 0.86)), 'M_Kit_WoodLight': (None, (0.86, 0.66, 0.45)), 'M_Kit_Brass': (None, (0.8, 0.62, 0.35)),
        'M_Kit_LampShade': ('T_LampShade.png', None), 'M_Kit_Bulb': (None, (1, 0.9, 0.6)), 'M_Kit_ToyBlock': ('T_ToyBlock.png', None),
        'M_Kit_Cushion': (None, (0.56, 0.83, 0.93)), 'M_Kit_CushionButton': (None, (0.97, 0.93, 0.87))}
x = -6.5
layout = [('PROP_Sofa', 0, 0), ('PROP_CoffeeTable', 3.4, 0.6), ('PROP_FloorLamp', 5.8, -0.6), ('PROP_ToyBlock', 7.6, 0.8), ('PROP_Cushion', 9.4, 0.5)]
for name, px, py in layout:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(PROPS, name + '.fbx'))
    for o in set(bpy.data.objects) - before:
        o.location.x += px; o.location.y += py
        if name == 'PROP_Cushion': o.scale = (2.4 * o.scale.x, 2.4 * o.scale.y, 0.6 * o.scale.z)
for m in bpy.data.materials:
    key = m.name.split('.')[0]
    if key not in cols: continue
    tex, c = cols[key]
    m.use_nodes = True
    b = m.node_tree.nodes.get('Principled BSDF')
    if b is None: continue
    b.inputs['Roughness'].default_value = 0.8
    if tex:
        t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(os.path.join(TEX, tex))
        m.node_tree.links.new(t.outputs['Color'], b.inputs['Base Color'])
    elif c:
        b.inputs['Base Color'].default_value = (*[v ** 2.2 for v in c], 1)
# floor
bpy.ops.mesh.primitive_plane_add(size=40, location=(2, 0, 0))
fl = bpy.context.object; fm = bpy.data.materials.new('floor'); fm.use_nodes = True
fm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.85, 0.78, 0.68, 1); fl.data.materials.append(fm)
sc.world = bpy.data.worlds.new('W'); sc.world.use_nodes = True
sc.world.node_tree.nodes['Background'].inputs[0].default_value = (0.5, 0.45, 0.65, 1)
ld = bpy.data.lights.new('sun', 'SUN'); ld.energy = 3.5; lo = bpy.data.objects.new('sun', ld); lo.rotation_euler = (math.radians(50), 0, math.radians(-30)); sc.collection.objects.link(lo)
cd = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam); sc.camera = cam
cd.lens = 32
cam.location = (2.2, -13.5, 7.5)
cam.rotation_euler = (Vector((2.2, 0, 0.6)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
sc.render.resolution_x, sc.render.resolution_y = 1280, 640
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print('preview ->', out)
