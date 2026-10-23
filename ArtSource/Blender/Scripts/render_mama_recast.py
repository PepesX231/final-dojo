import bpy,os,math
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
bpy.ops.wm.open_mainfile(filepath=ROOT+'/ArtSource/Blender/MamaRecast/MamaRecast_PropLibrary.blend',load_ui=False)
OUT=ROOT+'/DevBridge/mama-rebuild/renders';os.makedirs(OUT,exist_ok=True)
mat=bpy.data.materials['MAMA_PaintedAtlas'];mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.64
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(ROOT+'/Assets/MamasComing/Art/Recast/Textures/MamaPaintedAtlas.png');mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
models=[o for o in bpy.data.objects if o.type=='MESH']
for o in models:o.hide_render=True
bpy.ops.object.camera_add(location=(2,-3,2));camera=bpy.context.object;bpy.context.scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.6
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.16,.2,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
for pos,power,sz in [((2,-3,5),550,4),((-3,-1,2),320,3),((0,3,4),450,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=sz;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.resolution_x=480;scene.render.resolution_y=480;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for name in ['PROP_PlushTeddy','PET_Hamster','PET_Goldfish','PROP_TrashBag','PROP_OpenBin','PROP_SeatCushion','PROP_CakeSlice','PROP_PizzaBox','PROP_Bicycle','PROP_WaterPump','PROP_BreakerPanel','PROP_FoldedTowel']:
    obj=bpy.data.objects[name];obj.hide_set(False);obj.hide_render=False
    pts=[obj.matrix_world@Vector(c) for c in obj.bound_box];center=sum(pts,Vector())/8;extent=max(obj.dimensions)
    camera.location=center+Vector((1.2,-2.4,1.5))*extent;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=extent*1.35
    scene.render.filepath=OUT+'/'+name+'.png';bpy.ops.render.render(write_still=True)
    obj.hide_render=True
print('RECAST_RENDER_COMPLETE')
