"""Derive a Unity FBX copy. Never overwrite the supplied Blender 5.2 originals."""
import bpy,os,json,math
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
path=ROOT+'/ArtSource/Blender/ProvidedFamily/Banana_Sibling_v5_GoofyWalk.blend'
bpy.ops.wm.open_mainfile(filepath=path,load_ui=False,use_scripts=False)
root=next(o for o in bpy.data.objects if o.name.startswith('BANANA'))
def belongs(o):
    q=o
    while q:
        if q==root:return True
        q=q.parent
    return False
members=[o for o in bpy.data.objects if belongs(o)]
bpy.context.scene.frame_set(1)
meshes=[o for o in members if o.type=='MESH']
# Freeze evaluated geometry to make an engine copy, retaining transform animation.
for o in meshes:
    evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
    o.modifiers.clear();o.data=mesh
    if len(mesh.polygons)>300:
        bpy.context.view_layer.objects.active=o
        dec=o.modifiers.new('Runtime silhouette reduction','DECIMATE');dec.ratio=.28 if len(mesh.polygons)>2000 else .5
        bpy.ops.object.modifier_apply(modifier=dec.name)
bpy.ops.object.select_all(action='DESELECT')
for o in members:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.nla.bake(frame_start=1,frame_end=41,step=1,only_selected=True,visual_keying=True,clear_constraints=True,clear_parents=False,bake_types={'OBJECT'})
out=ROOT+'/Assets/MamasComing/Art/Family/Banana';os.makedirs(out,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=out+'/Banana_GoofyWalk.fbx',use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
triangles=0
for o in meshes:o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
with open(ROOT+'/DevBridge/mama-rebuild/banana-export.json','w') as f:json.dump({'triangles':triangles,'meshes':len(meshes),'frames':[1,41],'fps':bpy.context.scene.render.fps,'note':'Rigid transform walk baked; Blender-only vertex deformation/modifier animation frozen at frame 1. Source 5.2 files preserved.'},f,indent=2)
print('BANANA_EXPORTED',triangles)
