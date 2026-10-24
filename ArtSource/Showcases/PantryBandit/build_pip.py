"""Original pantry-bandit character, six baked skeletal actions, two hero props and a studio.
Run with the isolated Blender 4.5 background process. Never opens an existing project.
"""
import bpy, math, os, json, random, sys
from mathutils import Vector, Matrix, Quaternion, Euler
from pathlib import Path
P=Path(__file__).resolve().parent
ROOT=P.parents[2]
OUT=ROOT/'Assets/Showcases/PantryBandit'
PRE=ROOT/'DevBridge/pantry-showcase'
for p in [OUT/'Models',OUT/'Textures',PRE/'frames']:p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
sc.render.resolution_x=1200;sc.render.resolution_y=1200;sc.render.resolution_percentage=100
sc.render.fps=30;sc.world=bpy.data.worlds.new('World • midnight blue');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.16,.22,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.45
sc.view_settings.view_transform='AgX'
COLORS=['978DA4','F18D91','FFF0D3','229D99','E97050','253A50','D6B37A','C77989','F4BF48','D88D28','182333','66D2BB','F1DDAD','536780','426377','BCD8D5']
def rgb(h):return tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
im=bpy.data.images.new('Pip • painted palette',width=512,height=512,alpha=False)
pix=[]
for y in range(512):
 for x in range(512):
  idx=(y//128)*4+x//128;c=rgb(COLORS[idx]);u=(x%128)/127;v=(y%128)/127
  noise=((x*73+y*151)%37)/36-.5
  k=.94+.08*v+.025*noise
  pix.extend([min(1,t*k) for t in c]+[1])
im.pixels.foreach_set(pix);im.filepath_raw=str(OUT/'Textures/Pip_Palette.png');im.file_format='PNG';im.save();im.pack()
M={}
for name,rough,metal in [('Soft',.62,0),('Fabric',.82,0),('Boot',.42,0),('Eye',.18,0),('Metal',.3,.6)]:
 m=bpy.data.materials.new('MAT-Pip-'+name);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Roughness'].default_value=rough;b.inputs['Metallic'].default_value=metal
 tx=m.node_tree.nodes.new('ShaderNodeTexImage');tx.image=im;m.node_tree.links.new(tx.outputs['Color'],b.inputs['Base Color']);M[name]=m
geo=[]
def finish(o,name,color,bone=None,material='Soft'):
 o.name='GEO-'+name;bpy.context.view_layer.objects.active=o;o.select_set(True)
 if o.type=='CURVE':bpy.ops.object.convert(target='MESH');o=bpy.context.object
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.append(M[material])
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 uv=o.data.uv_layers.active.data
 for loop in o.data.loops:
  old=uv[loop.index].uv.copy();uv[loop.index].uv=((color%4+.16+old.x*.68)/4,(color//4+.16+old.y*.68)/4)
 for f in o.data.polygons:f.use_smooth=True
 if bone:
  vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(o.data.vertices))),1,'REPLACE');geo.append(o)
 bpy.ops.object.select_all(action='DESELECT');return o
def ell(name,p,size,c,b=None,mat='Soft',segments=24,rings=16):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=p);o=bpy.context.object;o.scale=size;return finish(o,name,c,b,mat)
def box(name,p,size,c,b=None,bevel=.04,mat='Boot'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 mod=o.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,c,b,mat)
def tube(name,points,r,c,b=None,mat='Soft'):
 cu=bpy.data.curves.new('Curve-'+name,'CURVE');cu.dimensions='3D';cu.resolution_u=12;cu.bevel_depth=r;cu.bevel_resolution=3
 sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for bp,p in zip(sp.bezier_points,points):bp.co=p;bp.handle_left_type='AUTO';bp.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,cu);sc.collection.objects.link(o);return finish(o,name,c,b,mat)
def link(name,a,b,r,c,bone=None,mat='Soft'):
 a,b=Vector(a),Vector(b);o=ell(name,(a+b)/2,(r,r,(b-a).length/2+r*.5),c,bone,mat,16,12);o.rotation_mode='QUATERNION';o.rotation_quaternion=(b-a).to_track_quat('Z','Y');return o

# Primary readable forms: a pointed muzzle, round upright ears and long trailing tail.
ell('pear shaped jacket',(0,.015,.72),(.30,.215,.33),3,'chest','Fabric')
ell('cream belly',(0,-.175,.70),(.205,.085,.245),2,'chest','Fabric')
ell('mouse head',(0,-.01,1.27),(.355,.285,.31),0,'head')
ell('tapered muzzle',(0,-.255,1.15),(.215,.225,.145),2,'head')
ell('black button nose',(0,-.475,1.20),(.075,.055,.05),10,'head','Eye')
for side in [-1,1]:
 b='ear.L' if side<0 else 'ear.R';x=side*.35
 ell('ear outer '+b,(x,.015,1.53),(.255,.105,.29),0,b)
 ell('ear warm lining '+b,(x,-.080,1.535),(.194,.035,.225),1,b)
 ell('ear inner bounce '+b,(x,-.108,1.52),(.11,.012,.145),7,b)
 ell('eye porcelain '+str(side),(side*.163,-.257,1.335),(.118,.075,.142),2,'head','Eye')
 ell('eye pupil '+str(side),(side*.163,-.324,1.335),(.076,.027,.093),10,'head','Eye')
 ell('eye glint '+str(side),(side*.144,-.350,1.372),(.025,.009,.029),2,'head','Eye',16,10)
 ell('cheek '+str(side),(side*.23,-.258,1.15),(.055,.025,.035),1,'head')
 box('tooth '+str(side),(side*.025,-.397,1.04),(.044,.035,.063),2,'head',.009)
 for j in range(3):
  tube('whisker '+str(side)+' '+str(j),[(side*.145,-.39,1.15),(side*.28,-.42,1.14+(j-1)*.025),(side*(.39+j*.012),-.38,1.13+(j-1)*.05)],.004,5,'head')
 for z in [.65,.78]:ell('jacket brass snap',(side*.235,-.16,z),(.018,.015,.018),6,'chest','Metal',12,8)
tube('mischievous smile',[(-.10,-.400,1.097),(0,-.425,1.073),(.10,-.40,1.097)],.008,5,'head')
for i in range(3):
 o=ell('forehead tuft '+str(i),(-.06+i*.05,.02,1.55),(.035,.055,.115-i*.02),0,'head');o.rotation_euler.y=-.25+i*.2

# Scarf has a thick collar and articulated tails rather than a paper-thin plane.
tube('scarf collar',[(-.21,-.10,1.015),(0,-.21,1.015),(.21,-.10,1.015),(.20,.10,1.015),(0,.17,1.015),(-.21,.10,1.015),(-.21,-.10,1.015)],.047,4,'chest','Fabric')
ell('scarf knot',(-.205,-.11,1.01),(.075,.06,.07),4,'chest','Fabric')
box('scarf tail first',(-.25,.04,.93),(.13,.25,.035),4,'scarf.01',.025,'Fabric')
box('scarf tail second',(-.28,.24,.82),(.14,.24,.035),4,'scarf.02',.025,'Fabric')
for i in range(3):box('scarf cream stitch '+str(i),(-.28,.20+i*.045,.84),(.145,.015,.04),2,'scarf.02',.007,'Fabric')
box('canvas pocket',(.255,.08,.63),(.16,.22,.24),6,'chest',.06,'Fabric')
box('pocket fold',(.29,.072,.74),(.12,.23,.06),12,'chest',.02,'Fabric')
box('pocket brass clasp',(.345,-.003,.67),(.025,.045,.055),6,'chest',.01,'Metal')
for side in [-1,1]:
 s='L' if side<0 else 'R';sh=(side*.265,0,.95);el=(side*.385,-.015,.73);wr=(side*.43,-.06,.53)
 link('sleeve '+s,sh,el,.072,3,'arm.'+s,'Fabric');link('forearm '+s,el,wr,.045,0,'forearm.'+s)
 ell('mitten '+s,(side*.44,-.065,.48),(.08,.065,.085),2,'hand.'+s,'Fabric')
 ell('mitten thumb '+s,(side*.382,-.095,.49),(.034,.04,.05),2,'hand.'+s,'Fabric',16,10)
 for j in range(3):ell('mitten fingers '+s+str(j),(side*.44+(j-1)*.035,-.067,.423),(.023,.055,.036),2,'hand.'+s,'Fabric',12,8)
 link('leg upper '+s,(side*.155,0,.52),(side*.175,0,.31),.066,0,'thigh.'+s)
 link('leg lower '+s,(side*.175,0,.31),(side*.18,-.02,.13),.05,0,'shin.'+s)
 box('boot sole '+s,(side*.18,-.105,.055),(.245,.37,.085),12,'foot.'+s,.035)
 ell('soft boot '+s,(side*.18,-.095,.14),(.116,.17,.103),5,'foot.'+s,'Boot')
 box('boot toe guard '+s,(side*.18,-.235,.104),(.2,.07,.07),3,'foot.'+s,.025)
 for j in range(2):box('lace '+s+str(j),(side*.18,-.10+j*.055,.235),(.12,.015,.015),2,'foot.'+s,.006)

tail_pts=[(0,.15,.52),(.08,.36,.38),(.28,.48,.22),(.47,.52,.13),(.65,.45,.11),(.77,.30,.14),(.76,.15,.20)]
tail=tube('long curling mouse tail',tail_pts,.034,7,None)
# Smooth blended bone weights on a continuous tail mesh.
geo.append(tail)
for i in range(6):tail.vertex_groups.new(name='tail.%02d'%i)
for v in tail.data.vertices:
 world=tail.matrix_world@v.co;dist=[(world-Vector((Vector(tail_pts[i])+Vector(tail_pts[i+1]))/2)).length for i in range(6)];ids=sorted(range(6),key=lambda i:dist[i])[:2]
 a,b=ids;wa=dist[b]/max(.001,dist[a]+dist[b]);tail.vertex_groups[a].add([v.index],wa,'REPLACE');tail.vertex_groups[b].add([v.index],1-wa,'REPLACE')

# True armature, all actions baked to transform keys. No Python handlers required by exported animation.
arm_data=bpy.data.armatures.new('Pip skeleton');arm=bpy.data.objects.new('ARM_Pip',arm_data);sc.collection.objects.link(arm);bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
rest={}
def bone(name,a,b,parent=None):
 e=arm_data.edit_bones.new(name);e.head=a;e.tail=b
 if parent:e.parent=arm_data.edit_bones[parent]
 rest[name]=(Vector(a),Vector(b));return e
bone('root',(0,0,0),(0,0,.15))
bone('hips',(0,0,.5),(0,0,.73),'root');bone('chest',(0,0,.73),(0,0,1.02),'hips');bone('head',(0,0,1.05),(0,0,1.4),'chest')
bone('ear.L',(-.25,.01,1.48),(-.44,.01,1.7),'head');bone('ear.R',(.25,.01,1.48),(.44,.01,1.7),'head')
bone('scarf.01',(-.21,-.08,1.0),(-.25,.14,.91),'chest');bone('scarf.02',(-.25,.14,.91),(-.28,.35,.80),'scarf.01')
for side in [-1,1]:
 s='L' if side<0 else 'R'
 bone('arm.'+s,(side*.265,0,.95),(side*.385,-.015,.73),'chest');bone('forearm.'+s,(side*.385,-.015,.73),(side*.43,-.06,.53),'arm.'+s);bone('hand.'+s,(side*.43,-.06,.53),(side*.44,-.065,.44),'forearm.'+s)
 bone('thigh.'+s,(side*.155,0,.52),(side*.175,0,.31),'hips');bone('shin.'+s,(side*.175,0,.31),(side*.18,-.02,.13),'thigh.'+s);bone('foot.'+s,(side*.18,-.02,.13),(side*.18,-.24,.13),'shin.'+s)
for i in range(6):bone('tail.%02d'%i,tail_pts[i],tail_pts[i+1],'hips' if i==0 else 'tail.%02d'%(i-1))
bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT')
for o in geo:o.select_set(True)
bpy.context.view_layer.objects.active=geo[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name='GEO-Pip_skinned';mesh.parent=arm
mod=mesh.modifiers.new('Pip weighted skeleton','ARMATURE');mod.object=arm
arm.show_in_front=True
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'
bind={b.name:b.matrix_local.copy() for b in arm.data.bones}
posed={}
def worldbone(name,a,b):
 a,b=Vector(a),Vector(b);rv=rest[name][1]-rest[name][0];q=rv.rotation_difference(b-a)@bind[name].to_quaternion()
 desired=Matrix.Translation(a)@q.to_matrix().to_4x4();pb=arm.pose.bones[name];posed[name]=desired
 # Solve local basis explicitly, rather than reading a depsgraph parent from the previous frame.
 if pb.parent:pb.matrix_basis=bind[name].inverted()@bind[pb.parent.name]@posed[pb.parent.name].inverted()@desired
 else:pb.matrix_basis=bind[name].inverted()@desired
def ik(a,b,l1,l2,forward):
 d=b-a;length=min(d.length,l1+l2-.002);u=d.normalized();v=Vector(forward);v=(v-u*v.dot(u)).normalized();t=(l1*l1-l2*l2+length*length)/(2*length)
 return a+u*t+v*math.sqrt(max(.00001,l1*l1-t*t))
actions={}
def sample(kind,t):
 posed.clear();posed['root']=bind['root']
 for p in arm.pose.bones:p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
 phase=t*math.tau;walking=kind in ['Walk','Run'];run=kind=='Run';stride=.24 if run else .20
 bob=(abs(math.sin(phase))* (.045 if run else .018)) if walking else math.sin(phase)*.009
 hop=0;crouch=0;lean=0
 if kind=='Hop':
  if t<.18:crouch=.14*math.sin(t/.18*math.pi*.5)
  elif t<.78:hop=.48*math.sin((t-.18)/.6*math.pi);lean=-.15*math.sin((t-.18)/.6*math.pi)
  else:crouch=.12*math.sin((t-.78)/.22*math.pi)
 if kind=='Snatch':crouch=.16*math.sin(t*math.pi);lean=.3*math.sin(t*math.pi)
 pos=Vector((math.sin(phase)*.018 if walking else 0,0,hop+bob-crouch))
 bodyq=Euler((lean+(-.15 if run else .03 if walking else 0),math.sin(phase)*(.06 if walking else .025),math.sin(phase)*(.07 if walking else .02)),'XYZ').to_quaternion()
 hip=Vector((0,0,.50))+pos;chest=hip+bodyq@Vector((0,0,.23));neck=chest+bodyq@Vector((0,0,.32))
 worldbone('hips',hip,chest);worldbone('chest',chest,neck);worldbone('head',neck,neck+bodyq@Vector((0,0,.35)))
 for side in [-1,1]:
  s='L' if side<0 else 'R';a=(t+(0 if side<0 else .5))%1;stance=.4 if run else .6
  fy=0;lift=0
  if walking:
   if a<stance:fy=-stride+2*stride*a/stance
   else:u=(a-stance)/(1-stance);fy=stride-2*stride*(u*u*(3-2*u));lift=(.17 if run else .12)*math.sin(math.pi*u)
  ankle=Vector((side*.18,fy-.02,.13+lift+hop));hipside=hip+bodyq@Vector((side*.155,0,.02))
  knee=ik(hipside,ankle,.211,.183,(0,-1,0));worldbone('thigh.'+s,hipside,knee);worldbone('shin.'+s,knee,ankle);worldbone('foot.'+s,ankle,ankle+Vector((0,-.22,.025*math.sin(phase) if walking else 0)))
  shoulder=chest+bodyq@Vector((side*.265,0,.22));swing=math.sin(phase+(0 if side<0 else math.pi))*(.20 if run else .12) if walking else 0
  wrist=shoulder+Vector((side*.17,swing,-.40))
  if kind=='Snatch':wrist=shoulder+Vector((side*.03,-.42*math.sin(t*math.pi),-.28+.06*math.sin(t*math.pi)))
  if kind=='Cheer':wrist=shoulder+Vector((side*(.2+.08*math.sin(phase*2)),-.09,.23+.04*math.cos(phase*2)))
  if kind=='Hop':wrist=shoulder+Vector((side*.18,-.12,.1*math.sin(t*math.pi)-.1))
  elbow=ik(shoulder,wrist,.25,.22,(side*.3,.1,-1));worldbone('arm.'+s,shoulder,elbow);worldbone('forearm.'+s,elbow,wrist);worldbone('hand.'+s,wrist,wrist+Vector((0,-.015,-.09)))
 for side in ['L','R']:
  pb=arm.pose.bones['ear.'+side];pb.rotation_quaternion=Euler((math.sin(phase-.7)*(.12 if walking else .045),0,math.sin(phase+(0 if side=='L' else .9))*.08)).to_quaternion()
 for i in range(6):arm.pose.bones['tail.%02d'%i].rotation_quaternion=Euler((math.sin(phase-i*.4)*.05,math.cos(phase-i*.4)*.06,math.sin(phase-i*.45)*(.18 if walking else .08))).to_quaternion()
 for i in [1,2]:arm.pose.bones['scarf.%02d'%i].rotation_quaternion=Euler((math.sin(phase-i*.8)*(.18 if walking else .06),math.cos(phase-i*.5)*.10,0)).to_quaternion()
 for pb in arm.pose.bones:
  pb.keyframe_insert('location');pb.keyframe_insert('rotation_quaternion')
for kind,count in [('Idle',61),('Walk',31),('Run',23),('Hop',37),('Snatch',49),('Cheer',61)]:
 act=bpy.data.actions.new('AN_Pip_'+kind);arm.animation_data_create();arm.animation_data.action=act
 for f in range(1,count+1):sc.frame_set(f);sample(kind,(f-1)/(count-1))
 if hasattr(act,'fcurves'):
  for fc in act.fcurves:
   for k in fc.keyframe_points:k.interpolation='LINEAR'
 actions[kind]=act;act.use_fake_user=True
arm.animation_data.action=actions['Idle'];sc.frame_set(1)
# Export skeleton and mesh only, without studio or decorative context.
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/Pip.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')

# Hero props: solid wedge with geometric dimples; folded carton with ribbed cap and printed badge.
props=[]
verts=[(-.18,-.13,0),(.18,-.13,0),(-.18,.13,0),(-.18,-.13,.22),(.18,-.13,.08),(-.18,.13,.22)]
me=bpy.data.meshes.new('Cheese wedge');me.from_pydata(verts,[],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]);me.update();ob=bpy.data.objects.new('Cheese',me);sc.collection.objects.link(ob);ob=finish(ob,'golden cheese wedge',8,None,'Boot');props.append(ob)
for x,y,z,r in [(-.11,-.135,.11,.043),(.02,-.135,.08,.035),(-.12,.01,.215,.041),(-.03,-.06,.17,.035)]:
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=12,radius=r,location=(x,y,z));cutter=bpy.context.object
 bpy.context.view_layer.objects.active=ob;bo=ob.modifiers.new('Real cheese cavity','BOOLEAN');bo.operation='DIFFERENCE';bo.solver='EXACT';bo.object=cutter;bpy.ops.object.modifier_apply(modifier=bo.name);bpy.data.objects.remove(cutter,do_unlink=True)
be=ob.modifiers.new('Cheese edge softness','BEVEL');be.width=.005;be.segments=2;bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=be.name)
for loop in ob.data.uv_layers.active.data:loop.uv=((8%4+.5)/4,(8//4+.5)/4)
bpy.ops.object.select_all(action='DESELECT')
for o in props:o.select_set(True)
bpy.context.view_layer.objects.active=props[0];bpy.ops.object.join();cheese=bpy.context.object;cheese.name='GEO-Cheese';bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/Cheese.fbx'),use_selection=True,bake_anim=False,axis_forward='-Z',axis_up='Y')
cheese.location=(-.95,-.05,0)
cart=[]
cart.append(box('milk carton',(0,0,.22),(.29,.24,.44),2,None,.018))
cart.append(box('carton blue sleeve',(0,-.124,.23),(.25,.012,.28),3,None,.008))
cart.append(box('folded gable',(0,0,.48),(.29,.24,.12),2,None,.025))
cart.append(box('sealed seam',(0,0,.55),(.29,.04,.05),3,None,.012))
cart.append(ell('carton milk badge',(0,-.137,.25),(.065,.012,.09),2))
cart.append(ell('carton leaf badge',(0,-.15,.25),(.025,.006,.055),3))
bpy.ops.object.select_all(action='DESELECT')
for o in cart:o.select_set(True)
bpy.context.view_layer.objects.active=cart[0];bpy.ops.object.join();milk=bpy.context.object;milk.name='GEO-MilkCarton'
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/MilkCarton.fbx'),use_selection=True,bake_anim=False,axis_forward='-Z',axis_up='Y');milk.location=(1.05,.20,.22)
bpy.context.view_layer.update()
for prop in [cheese,milk]:prop.location.z-=min((prop.matrix_world@v.co).z for v in prop.data.vertices)

# Studio: sculpted plinth and restrained graphic wayfinding.
def simplemat(name,h,rough=.6):
 m=bpy.data.materials.new(name);m.diffuse_color=(*rgb(h),1);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*rgb(h),1);b.inputs['Roughness'].default_value=rough;return m
navy=simplemat('MAT-Studio navy','102E3D');gold=simplemat('MAT-Studio warm brass','CBA46D',.3);paper=simplemat('MAT-Lettering cream','F3E5CA')
def cyl(name,p,r,depth,mat):
 bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=r,depth=depth,location=p);o=bpy.context.object;o.name=name;o.data.materials.append(mat);mo=o.modifiers.new('Machined edge','BEVEL');mo.width=.035;mo.segments=3
 return o
cyl('STUDIO • top platform',(0,0,-.08),1.65,.16,navy);cyl('STUDIO • brass reveal',(0,0,-.145),1.68,.045,gold);cyl('STUDIO • base',(0,0,-.20),1.76,.10,navy)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.26));bpy.context.object.data.materials.append(navy)
def label(text,p,size,mat):
 cu=bpy.data.curves.new('Typography','FONT');cu.body=text;cu.size=size;cu.extrude=.001;cu.align_x='CENTER';o=bpy.data.objects.new('TYPE-'+text,cu);sc.collection.objects.link(o);o.location=p;o.rotation_euler=(math.radians(90),0,0);cu.materials.append(mat);return o
label('P I P',(-.95,.24,1.50),.18,paper);label('THE PANTRY BANDIT',(-.95,.24,1.37),.035,paper)
label('01 / MOTION STUDY',(.91,.24,.92),.045,paper);label('ORIGINAL CHARACTER',(.91,.24,.83),.028,paper)
def area(name,p,power,size,color):
 ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.shape='DISK';ld.size=size;ld.color=rgb(color);o=bpy.data.objects.new(name,ld);sc.collection.objects.link(o);o.location=p;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
area('LIGHT • warm key',(-3,-4,5),550,4,'FFE2BA');area('LIGHT • cool rim',(3,1.6,4),750,3,'9DDCD9');area('LIGHT • face fill',(1,-4,2),160,2,'FFF1D4')
camd=bpy.data.cameras.new('CAM • hero');cam=bpy.data.objects.new('CAM • hero',camd);sc.collection.objects.link(cam);sc.camera=cam;cam.location=(3.2,-6,2.7);target=Vector((0,0,.8));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=3.9;camd.lens=65
sc.frame_start=1;sc.frame_end=61;arm.animation_data.action=actions['Idle'];sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Pip_PantryBandit.blend'))
tri=sum(len(o.data.polygons) for o in [mesh,cheese,milk]);manifest={'blender':bpy.app.version_string,'character_vertices':len(mesh.data.vertices),'character_triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),'bones':len(arm.data.bones),'actions':{k:[1,int(v.frame_range.y)] for k,v in actions.items()},'materials':len(mesh.data.materials),'skin':'baked skeletal transforms; no handlers','source':'Original geometry generated for this isolated showcase','poly_faces_with_props':tri}
(P/'manifest.json').write_text(json.dumps(manifest,indent=2));print('SHOWCASE_EXPORTED',json.dumps(manifest),flush=True)
if '--no-render' in sys.argv:sys.exit(0)
# Hero still first so visual inspection can happen before the motion renders complete.
sc.render.filepath=str(PRE/'hero.png');bpy.ops.render.render(write_still=True)
sc.render.resolution_x=700;sc.render.resolution_y=700;sc.cycles.samples=12
for kind in ['Walk','Run','Hop','Snatch','Cheer']:
 arm.animation_data.action=actions[kind];last=int(actions[kind].frame_range.y)
 for i in range(12):
  sc.frame_set(1+round((last-1)*i/12));sc.render.filepath=str(PRE/'frames'/('%s_%02d.png'%(kind,i)));bpy.ops.render.render(write_still=True)
 print('RENDERED_ACTION',kind,flush=True)
print('SHOWCASE_COMPLETE',flush=True)
