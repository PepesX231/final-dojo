"""Render front/side/back/top silhouettes of a .blend character (Cycles CPU, headless).
usage: python3 -I render_views.py <file.blend> <out_dir> [res]"""
import bpy, sys, os, math
from mathutils import Vector
blend, out = sys.argv[-2], sys.argv[-1]
res = 512
bpy.ops.wm.open_mainfile(filepath=blend)
sc = bpy.context.scene
sc.render.engine = 'CYCLES'
sc.cycles.samples = 24
sc.cycles.device = 'CPU'
sc.render.resolution_x = sc.render.resolution_y = res
sc.render.film_transparent = False
os.makedirs(out, exist_ok=True)
if not any(o.type == 'LIGHT' for o in bpy.data.objects):   # bare asset files: add a neutral studio rig
    if sc.world is None: sc.world = bpy.data.worlds.new('W')
    sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.55, 0.52, 0.62, 1); bg.inputs[1].default_value = 0.9
    for n, rot, e in (('key', (math.radians(50), 0, math.radians(-35)), 3.5), ('rim', (math.radians(60), 0, math.radians(150)), 1.5)):
        ld = bpy.data.lights.new(n, 'SUN'); ld.energy = e; lo = bpy.data.objects.new(n, ld); lo.rotation_euler = rot; sc.collection.objects.link(lo)
# bounds of character collection
objs = [o for o in bpy.data.objects if o.type in ('MESH','CURVE') and 'floor' not in o.name.lower()]
mn = Vector((1e9,)*3); mx = Vector((-1e9,)*3)
dg = bpy.context.evaluated_depsgraph_get()
for o in objs:
    ev = o.evaluated_get(dg)
    for c in ev.bound_box:
        w = o.matrix_world @ Vector(c)
        mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
print("BOUNDS", tuple(round(v,3) for v in mn), tuple(round(v,3) for v in mx))
ctr = (mn+mx)/2; size = max(mx-mn)
cam = bpy.data.objects.get('Camera • portrait')
if cam is None:
    cd = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam)
sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = size*1.15
views = {'front':(0,-1,0),'side':(1,0,0),'back':(0,1,0),'top':(0,0,1),'threequarter':(0.7,-0.7,0.35)}
for name, d in views.items():
    d = Vector(d).normalized()
    cam.location = ctr + d*20
    cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z','Y' if name!='top' else 'Y').to_euler()
    sc.render.filepath = os.path.join(out, f"{name}.png")
    bpy.ops.render.render(write_still=True)
print("done")
