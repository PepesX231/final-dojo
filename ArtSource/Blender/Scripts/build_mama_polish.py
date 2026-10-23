"""MAMA original cookware and furniture surface kit, Blender 4.5, metres."""
import bpy, math, os, json
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../..'))
OUT = os.path.join(ROOT, 'Assets/MamasComing/Art/Polish/Models')
SOURCE = os.path.join(ROOT, 'ArtSource/Blender/MamaPolish')
os.makedirs(OUT, exist_ok=True)
os.makedirs(SOURCE, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
report = []

def finish_export(obj, name):
    obj.name = name
    bpy.context.view_layer.objects.active = obj
    for poly in obj.data.polygons:
        poly.use_smooth = True
    normals = obj.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
    normals.keep_sharp = True
    bpy.ops.object.modifier_apply(modifier=normals.name)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name+'.fbx'), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, add_leaf_bones=False, bake_anim=False)
    obj.data.calc_loop_triangles()
    report.append({'name':name, 'triangles':len(obj.data.loop_triangles)})

def lathe(name, profile, segments=64):
    vertices, faces = [], []
    for radius, height in profile:
        for i in range(segments):
            theta = 2*math.pi*i/segments
            vertices.append((radius*math.cos(theta), radius*math.sin(theta), height))
    for row in range(len(profile)-1):
        for i in range(segments):
            a=row*segments+i; b=row*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces); mesh.update()
    obj=bpy.data.objects.new(name, mesh); bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(island_margin=.03)
    bpy.ops.object.mode_set(mode='OBJECT')
    finish_export(obj,name)

for name, width in [('ENV_CraftedEdge', .045), ('ENV_SoftCushion', .16)]:
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj=bpy.context.object
    bevel=obj.modifiers.new('Soft manufactured edge', 'BEVEL'); bevel.width=width; bevel.segments=4
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    finish_export(obj,name)

# Hollow wall, rolled lip, weighted base: not a solid cylinder with a painted top.
lathe('PROP_CraftedPotBody', [(0,0),(.145,0),(.176,.008),(.194,.028),(.2,.07),
    (.2,.235),(.207,.248),(.213,.253),(.213,.262),(.207,.268),(.195,.268),
    (.19,.258),(.186,.237),(.186,.067),(.17,.025),(0,.025)])
# Domed lid and turned rolled edge, with the existing game knob retained separately.
lathe('PROP_CraftedLid', [(0,.058),(.055,.058),(.12,.047),(.18,.025),(.205,.014),
    (.219,.012),(.225,.007),(.224,.001),(.216,-.003),(.195,.001),(.175,.012),
    (.11,.034),(.05,.045),(0,.045)])
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE,'MamaCraftedKit.blend'))
with open(os.path.join(SOURCE,'manifest.json'),'w',encoding='utf8') as f:
    json.dump({'units':'metres','models':report},f,indent=2)
print('MAMA_POLISH_EXPORT '+json.dumps(report))
