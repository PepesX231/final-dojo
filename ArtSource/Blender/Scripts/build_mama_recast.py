"""Original game-ready domestic comedy props. Blender 4.5, metres, -Y front.
Palette UV atlas keeps a single shared material per model. No downloaded models.
"""
import bpy, math, os, json, random
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
OUT=os.path.join(ROOT,'Assets/MamasComing/Art/Recast')
SOURCE=os.path.join(ROOT,'ArtSource/Blender/MamaRecast')
for folder in [OUT+'/Models',OUT+'/Textures',SOURCE]:os.makedirs(folder,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
random.seed(71)
palette=[('Ink','222336'),('Cream','FFF1CE'),('Honey','F7AC38'),('Teddy','B66532'),
('LightFur','EDBD7D'),('Coral','ED544C'),('Teal','26B8AD'),('Plum','57376C'),
('White','FFF9EE'),('Pink','F396AC'),('Blue','4678D6'),('Steel','879DA5'),
('Wood','A65C30'),('DarkWood','633A2D'),('Gold','EDC35D'),('Leaf','5C9841'),
('Bag','344253'),('Copper','C7773E'),('Orange','F17A29'),('PaleBlue','93D5DA'),
('Sponge','EEC25C'),('Crust','BF7332'),('Sauce','BD372E'),('Paper','DFB984'),
('Purple','8854C6'),('Lime','B9CD38'),('Red','D83440'),('Sand','DBC89A'),
('Fabric','F4CB29'),('Soot','4A4356'),('DeepTeal','176C74'),('Lilac','AD8CD6')]
lookup={name:i for i,(name,col) in enumerate(palette)}
def rgb(value):return tuple(int(value[i:i+2],16)/255 for i in (0,2,4))
materials={}
for name,col in palette:
    m=bpy.data.materials.new(name);m.diffuse_color=(*rgb(col),1);materials[name]=m
atlas=bpy.data.materials.new('MAMA_PaintedAtlas');atlas.diffuse_color=(1,1,1,1)
size=512; image=bpy.data.images.new('MAMA_PaintedAtlas',width=size,height=size,alpha=False)
px=[]
def linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
for y in range(size):
    for x in range(size):
        col=(x//64)+(y//128)*8;r,g,b=rgb(palette[col][1]);u=(x%64)/64;v=(y%128)/128
        variation=.96+.025*math.sin(x*.7+y*.13)+.02*math.sin(y*.27)+.02*v
        px.extend((linear(r*variation),linear(g*variation),linear(b*variation),1))
image.pixels=px;image.filepath_raw=OUT+'/Textures/MamaPaintedAtlas.png';image.file_format='PNG';image.save()
image.pack()
atlas.use_nodes=True
atlas_shader=atlas.node_tree.nodes.get('Principled BSDF');atlas_shader.inputs['Roughness'].default_value=.64
atlas_tex=atlas.node_tree.nodes.new('ShaderNodeTexImage');atlas_tex.image=image
atlas.node_tree.links.new(atlas_tex.outputs['Color'],atlas_shader.inputs['Base Color'])
parts=[];manifest=[]
def use(o,name,mat):
    o.name=name;o.data.materials.clear();o.data.materials.append(materials[mat]);parts.append(o);return o
def smooth(o):
    for f in o.data.polygons:f.use_smooth=True
def sphere(name,p,s,mat,segments=20,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=p)
    o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);smooth(o);return use(o,name,mat)
def box(name,p,s,mat,bevel=.02,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if rot:o.rotation_euler=rot
    if bevel:
        b=o.modifiers.new('Crafted edge','BEVEL');b.width=min(bevel,min(s)*.4);b.segments=3;bpy.ops.object.modifier_apply(modifier=b.name)
    smooth(o);return use(o,name,mat)
def cyl(name,p,r,depth,mat,axis=(0,0,1),verts=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=p)
    o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    bevel=o.modifiers.new('Lip bevel','BEVEL');bevel.width=min(.008,depth*.12);bevel.segments=2;bpy.ops.object.modifier_apply(modifier=bevel.name)
    smooth(o);return use(o,name,mat)
def tube(name,points,r,mat,res=8):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=2;curve.bevel_depth=r;curve.bevel_resolution=1
    sp=curve.splines.new('POLY');sp.points.add(len(points)-1)
    for pt,co in zip(sp.points,points):pt.co=(*co,1)
    o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.convert(target='MESH');o=bpy.context.object;smooth(o);return use(o,name,mat)
def torus(name,p,major,minor,mat,axis=(0,0,1),segments=32):
    bpy.ops.mesh.primitive_torus_add(major_segments=segments,minor_segments=8,location=p,major_radius=major,minor_radius=minor)
    o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();smooth(o);return use(o,name,mat)
def lathe(name,profile,mat,n=32):
    v=[];f=[]
    for r,z in profile:
        for i in range(n):a=i*2*math.pi/n;v.append((r*math.cos(a),r*math.sin(a),z))
    for j in range(len(profile)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;f.append((a,b,b+n,a+n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);smooth(o);return use(o,name,mat)
def finish(name):
    global parts
    # UV each part into its colour cell, with enough inset for mipmaps.
    for o in parts:
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        slot=lookup[o.data.materials[0].name];cx=slot%8;cy=slot//8
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
        for uv in o.data.uv_layers.active.data:uv.uv=((cx+.06+uv.uv.x*.88)/8,(cy+.04+uv.uv.y*.92)/4)
        o.data.materials.clear();o.data.materials.append(atlas)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.data.calc_loop_triangles();tris=len(o.data.loop_triangles)
    bpy.ops.export_scene.fbx(filepath=OUT+'/Models/'+name+'.fbx',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
    manifest.append({'name':name,'triangles':tris,'vertices':len(o.data.vertices),'dimensions_blender':list(o.dimensions),'material_slots':len(o.data.materials)})
    o.hide_set(True);parts=[]

# Teddy: seated plush silhouette, muzzle, sewn limbs, contrasting paw pads and a bow.
sphere('Pear belly',(0,0,.2),(.14,.1,.17),'Teddy')
sphere('Round head',(0,-.008,.42),(.145,.11,.135),'Teddy')
for side in [-1,1]:
    sphere('Ear',(side*.115,.0,.525),(.061,.032,.064),'Teddy')
    sphere('Ear inset',(side*.115,-.028,.53),(.039,.012,.041),'LightFur')
    sphere('Floppy arm',(side*.17,-.008,.23),(.055,.06,.12),'Teddy')
    sphere('Seated foot',(side*.105,-.09,.072),(.075,.105,.065),'Teddy')
    sphere('Paw pad',(side*.105,-.181,.075),(.044,.013,.04),'LightFur')
    sphere('Button eye',(side*.056,-.105,.444),(.014,.009,.018),'Ink',16,8)
    sphere('Eye glint',(side*.058,-.113,.451),(.004,.003,.005),'White',12,8)
sphere('Muzzle',(0,-.105,.392),(.075,.036,.05),'LightFur')
sphere('Nose',(0,-.141,.411),(.024,.013,.016),'Ink',16,8)
tube('Smile',[(-.025,-.137,.382),(0,-.143,.372),(.026,-.137,.382)],.003,'DarkWood')
tube('Belly seam',[(.006,-.103,z) for z in [.1,.14,.18,.22,.26,.3]],.002,'DarkWood')
for z in [.13,.17,.21,.25,.29]:tube('Stitch',[(-.008,-.106,z),(.018,-.106,z+.003)],.0018,'Cream')
sphere('Bow left',(-.035,-.098,.31),(.037,.018,.024),'Teal');sphere('Bow right',(.035,-.098,.31),(.037,.018,.024),'Teal');sphere('Bow knot',(0,-.114,.31),(.018,.014,.019),'Gold')
finish('PROP_PlushTeddy')

# Hamster: broad cheeks, short muzzle, circular ears, tiny feet; deliberately not a rat.
sphere('Pear-shaped body',(0,.035,.13),(.15,.19,.13),'Honey')
sphere('Face',(0,-.115,.17),(.14,.1,.12),'Honey')
sphere('Cream chest',(0,-.13,.09),(.11,.075,.07),'Cream')
for side in [-1,1]:
    sphere('Round ears',(side*.102,-.08,.265),(.055,.025,.06),'Honey')
    sphere('Ear fold',(side*.102,-.104,.27),(.034,.009,.037),'Pink')
    sphere('Full cheek',(side*.072,-.191,.132),(.067,.041,.047),'Cream')
    sphere('Eye',(side*.068,-.194,.204),(.021,.012,.025),'Ink')
    sphere('Eye shine',(side*.072,-.205,.212),(.006,.004,.007),'White',12,8)
    for y in [-.11,.15]:sphere('Tiny paw',(side*.105,y,.027),(.041,.051,.026),'Pink',16,8)
    for k in [-1,0,1]:tube('Whisker',[(side*.085,-.215,.145+k*.008),(side*.19,-.225,.145+k*.017)],.0018,'DarkWood')
sphere('Nose',(0,-.232,.151),(.02,.012,.014),'Pink',16,8)
box('Front teeth',(0,-.219,.118),(.025,.015,.021),'White',.004)
finish('PET_Hamster')

# Goldfish: continuous oval body, forked tail, dorsal fin, expressive eyes and gill curves.
sphere('Fish body',(0,0,0),(.065,.13,.08),'Orange')
sphere('Muzzle',(0,-.112,-.006),(.045,.045,.047),'Honey')
for side in [-1,1]:
    sphere('Eye',(side*.052,-.077,.027),(.018,.025,.023),'Cream')
    sphere('Pupil',(side*.066,-.09,.03),(.008,.012,.013),'Ink',12,8)
    tube('Gill',[(side*.058,-.035,.043),(side*.065,-.024,0),(side*.05,-.025,-.034)],.003,'Sauce')
    fin=sphere('Tail lobe',(side*.043,.16,.006),(.06,.06,.012),'Coral',16,8);fin.rotation_euler[1]=side*.5
    sphere('Pectoral',(side*.065,.0,-.025),(.045,.045,.008),'Honey',16,8)
sphere('Dorsal',(0,.015,.078),(.008,.08,.04),'Coral',16,8)
torus('Mouth',(0,-.151,-.012),.014,.004,'Coral',axis=(0,1,0),segments=16)
finish('PET_Goldfish')

# Crinkled trash bag: sculpted radial pleats, gathered mouth, tied handles.
o=lathe('Gathered bag',[(0,0),(.15,.015),(.25,.08),(.28,.24),(.245,.44),(.17,.55),(.045,.62),(.04,.66)],'Bag',32)
for v in o.data.vertices:
    angle=math.atan2(v.co.y,v.co.x);z=v.co.z;factor=1+.055*math.sin(angle*7+z*19)+.03*math.sin(angle*11-z*9)
    v.co.x*=factor;v.co.y*=factor
torus('Tied neck',(0,0,.625),.044,.014,'Ink',segments=20)
for side in [-1,1]:tube('Tied handle',[(0,0,.64),(side*.075,.01,.71),(side*.09,.01,.755),(side*.035,0,.75),(0,0,.65)],.018,'Bag')
finish('PROP_TrashBag')

# Open bin, outside lip, metal pedal and rear wheels. Lid exported separately for a hinge.
lathe('Hollow wheelie bin',[(0,0),(.24,.02),(.29,.1),(.31,.72),(.33,.76),(.33,.79),(.29,.79),(.27,.12),(0,.12)],'DeepTeal',32)
torus('Rim',(0,0,.775),.31,.018,'Teal')
box('Pedal',(0,-.3,.04),(.24,.12,.035),'Steel',.01)
for side in [-1,1]:cyl('Wheel',(side*.25,.18,.07),.072,.05,'Ink',axis=(1,0,0),verts=16)
box('Front badge',(0,-.308,.48),(.22,.018,.21),'Cream',.025)
for k in range(3):
    a=k*2*math.pi/3
    tube('Recycling arrows',[(.07*math.cos(a),-.322,.48+.07*math.sin(a)),(.07*math.cos(a+1.4),-.322,.48+.07*math.sin(a+1.4))],.009,'Teal')
finish('PROP_OpenBin')
box('Lid slab',(0,-.31,0),(.69,.66,.045),'Teal',.02)
tube('Handle',[(-.11,-.5,.025),(-.11,-.5,.075),(.11,-.5,.075),(.11,-.5,.025)],.017,'DeepTeal')
finish('PROP_BinLid')

# Upholstered seat cushion: soft edge piping, indented centre and fabric tufts.
o=box('Stuffed cushion',(0,0,.065),(.64,.58,.13),'Fabric',.06)
for v in o.data.vertices:
    if v.co.z>0:v.co.z-=.022*math.exp(-((v.co.x/.17)**2+(v.co.y/.17)**2))
points=[]
for i in range(65):
    a=i*2*math.pi/64;x=.3*math.copysign(abs(math.cos(a))**.4,math.cos(a));y=.27*math.copysign(abs(math.sin(a))**.4,math.sin(a));points.append((x,y,.067))
tube('Piped seam',points,.005,'Gold')
for x in [-.12,.12]:
    for y in [-.1,.1]:sphere('Tuft button',(x,y,.122),(.014,.014,.006),'Honey',12,8)
finish('PROP_SeatCushion')

# Cake smear reads as cake: sponge layers, frosting lobes and berries rather than a pink sphere.
box('Sponge',(0,0,.055),(.24,.19,.09),'Sponge',.025)
box('Jam layer',(0,0,.074),(.242,.192,.017),'Sauce',.008)
box('Cream top',(0,0,.108),(.26,.21,.035),'Cream',.015)
for x,y,s in [(-.12,-.03,.05),(.12,.04,.04),(.01,-.1,.04)]:sphere('Frosting drip',(x,y,.07),(s,s*.5,.06),'Pink',16,8)
sphere('Cherry',(.04,0,.149),(.031,.031,.031),'Coral',16,8)
tube('Cherry stalk',[(.04,0,.17),(.04,0,.21),(.065,0,.22)],.003,'Leaf')
finish('PROP_CakeSlice')

# Pizza carton with actual folded side walls, a pizza graphic and grease marks.
box('Box bottom',(0,0,.014),(.43,.43,.028),'Paper',.008)
for y in [-.204,.204]:box('Folded wall',(0,y,.043),(.43,.018,.06),'Paper',.004)
for x in [-.204,.204]:box('Folded wall',(x,0,.043),(.018,.41,.06),'Paper',.004)
box('Lid',(0,0,.08),(.44,.44,.02),'Cream',.006)
cyl('Pizza emblem',(0,0,.094),.125,.004,'Sauce',verts=32)
cyl('Cheese',(0,0,.097),.102,.003,'Honey',verts=24)
for a in range(5):cyl('Pepperoni',(.065*math.cos(a*1.256),.065*math.sin(a*1.256),.1),.018,.004,'Coral',verts=12)
for side in [-1,1]:box('Print stripe',(side*.17,0,.093),(.025,.32,.004),'Coral',.001)
finish('PROP_PizzaBox')

# Bicycle: diamond frame, two real wheel rings/spokes, fork, saddle, bars and cranks.
front=Vector((.56,0,.35));rear=Vector((-.56,0,.35));crank=Vector((-.08,0,.32));seat=Vector((-.18,0,.83));neck=Vector((.34,0,.84))
for c in [front,rear]:
    torus('Tyre',c,.31,.032,'Ink',axis=(0,1,0),segments=40)
    torus('Rim',c,.278,.008,'Steel',axis=(0,1,0),segments=32)
    cyl('Hub',c,.045,.14,'Steel',axis=(0,1,0))
    for i in range(12):
        a=i*math.pi/6;tube('Spoke',[c,c+Vector((math.cos(a)*.275,0,math.sin(a)*.275))],.003,'Steel')
for a,b in [(rear,seat),(seat,crank),(crank,rear),(seat,neck),(crank,neck),(neck,front)]:tube('Frame',[a,b],.024,'Teal')
tube('Seat post',[seat,seat+Vector((0,0,.09))],.018,'Steel')
box('Saddle',(-.18,0,.94),(.24,.18,.06),'Ink',.035)
tube('Handlebar',[(.34,0,.84),(.38,0,1.05),(.38,-.22,1.05)],.02,'Steel')
tube('Handlebar return',[(.38,0,1.05),(.38,.22,1.05)],.02,'Steel')
for side in [-1,1]:tube('Grips',[(.38,side*.14,1.05),(.38,side*.24,1.05)],.027,'Ink')
torus('Chainring',crank,.1,.01,'Steel',axis=(0,1,0),segments=24)
for side in [-1,1]:
    tube('Crank',[crank,crank+Vector((side*.09,side*.1,-side*.07))],.012,'Steel')
    box('Pedal',crank+Vector((side*.09,side*.14,-side*.07)),(.11,.09,.03),'Ink',.008)
finish('PROP_Bicycle')

# Water pump: identifiable electric motor, ribbed housing, feet, bolts and pipe couplings.
cyl('Motor',(0,0,.22),.15,.36,'Teal',axis=(0,1,0),verts=32)
for y in [-.13,-.08,-.03,.02,.07,.12]:torus('Cooling fin',(0,y,.22),.15,.009,'DeepTeal',axis=(0,1,0),segments=24)
cyl('Pump head',(0,-.23,.22),.17,.12,'Steel',axis=(0,1,0),verts=24)
box('Foot',(0,0,.035),(.42,.42,.07),'DeepTeal',.02)
cyl('Intake',(0,-.35,.22),.05,.13,'Copper',axis=(0,1,0))
cyl('Outlet',(0,-.22,.43),.05,.18,'Copper')
box('Power cap',(.13,.02,.36),(.14,.18,.1),'Teal',.018)
for x in [-.17,.17]:
    for y in [-.16,.16]:cyl('Bolt',(x,y,.077),.018,.017,'Steel',verts=6)
finish('PROP_WaterPump')

# Flanged water pipe and elbow pieces, authored as shapes rather than crossed cylinders.
for name,pts in [('PROP_PipeElbow',[(0,0,0),(0,0,.25)]+[(0,-.12+.12*math.cos(a),.25+.12*math.sin(a)) for a in [i*math.pi/16 for i in range(1,9)]]+[(0,-.35,.37)]),('PROP_PipeStraight',[(0,0,0),(0,0,.65)])]:
    tube('Copper pipe',pts,.035,'Copper')
    for p in [pts[0],pts[-1]]:sphere('Coupling',p,(.052,.052,.052),'Steel',16,8)
    finish(name)

# Router and electric breaker: distinct real-world silhouettes, no invented text.
box('Router shell',(0,0,.05),(.32,.2,.1),'Cream',.025)
for side in [-1,1]:tube('Antenna',[(side*.12,.07,.08),(side*.15,.07,.32)],.009,'Ink')
for x in [-.1,-.06,-.02,.02]:sphere('Status LED',(x,-.102,.045),(.007,.005,.007),'Teal',12,8)
for i in range(5):box('Cooling slots',(.13,-.06+i*.03,.102),(.02,.012,.003),'Ink',.001)
finish('PROP_Router')
box('Breaker case',(0,0,0),(.47,.11,.58),'Steel',.035)
box('Inset',(0,-.061,0),(.39,.015,.48),'Ink',.018)
for row in range(2):
    for col in range(3):
        x=-.12+col*.12;z=-.1+row*.17
        box('Fuse',(x,-.083,z),(.09,.03,.13),'Cream',.006)
        box('Switch',(x,-.112,z+.015),(.05,.035,.06),'Coral' if row==0 else 'Teal',.006)
box('Warning badge',(0,-.083,.21),(.18,.018,.05),'Honey',.004)
finish('PROP_BreakerPanel')

# Secondary props, all recognisable from the gameplay camera.
lathe('Vase',[(0,0),(.085,.012),(.095,.08),(.105,.17),(.065,.25),(.045,.31),(.055,.33),(.04,.33),(.033,.30),(.05,.25),(.08,.17),(.07,.04),(0,.04)],'Teal')
finish('PROP_CeramicVase')
box('Toolbox',(0,0,.14),(.46,.27,.28),'Coral',.035)
box('Toolbox lid',(0,0,.3),(.47,.29,.055),'Red',.018)
tube('Handle',[(-.09,0,.33),(-.09,0,.41),(.09,0,.41),(.09,0,.33)],.018,'Ink')
for x in [-.14,.14]:box('Latch',(x,-.145,.28),(.035,.025,.08),'Steel',.007)
finish('PROP_Toolbox')
lathe('Bucket',[(0,0),(.12,.008),(.15,.25),(.157,.27),(.145,.28),(.134,.25),(.11,.02),(0,.02)],'Blue')
torus('Bucket rim',(0,0,.27),.15,.012,'PaleBlue')
tube('Swinging handle',[(.15*math.cos(a),0,.27+.18*math.sin(a)) for a in [i*math.pi/24 for i in range(25)]],.008,'Steel')
finish('PROP_Bucket')
lathe('Extinguisher',[(0,0),(.075,.008),(.09,.035),(.09,.39),(.065,.44),(.027,.46)],'Red')
box('White label',(0,-.09,.25),(.11,.007,.18),'Cream',.008)
box('Lever',(0,0,.48),(.045,.19,.035),'Ink',.008)
tube('Hose',[(.025,.015,.47),(.13,.02,.42),(.14,.02,.18),(.11,-.02,.15)],.012,'Ink')
torus('Gauge',(-.04,-.03,.46),.024,.008,'Steel',axis=(0,1,0),segments=16)
finish('PROP_Extinguisher')
# Fabric towel, volume and large folds; no costly runtime cloth solver.
verts=[];faces=[];nx=12;ny=16
for y in range(ny+1):
    for x in range(nx+1):
        u=x/nx;v=y/ny;verts.append(((u-.5)*.55,(v-.5)*.7,.018+.015*math.sin(u*math.pi*5)*( .3+.7*v)+.013*math.sin(v*math.pi*3)))
for y in range(ny):
    for x in range(nx):a=y*(nx+1)+x;faces.append((a,a+1,a+nx+2,a+nx+1))
mesh=bpy.data.meshes.new('Woven towel');mesh.from_pydata(verts,[],faces);o=bpy.data.objects.new('Woven towel',mesh);bpy.context.collection.objects.link(o);use(o,'Woven towel','Teal');smooth(o)
bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Hem thickness','SOLIDIFY');mod.thickness=.006;bpy.ops.object.modifier_apply(modifier=mod.name)
for y in [-.31,.31]:tube('Woven border',[((i/24-.5)*.55,y,.028+.015*math.sin(i/24*math.pi*5)) for i in range(25)],.004,'Cream')
finish('PROP_FoldedTowel')

bpy.data.objects['PROP_PlushTeddy'].hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=SOURCE+'/MamaRecast_PropLibrary.blend')
with open(SOURCE+'/manifest.json','w') as f:json.dump({'units':'metres','front':'-Y','models':manifest},f,indent=2)
print('RECAST_COMPLETE '+json.dumps(manifest))
