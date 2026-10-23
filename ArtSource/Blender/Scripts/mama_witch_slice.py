"""Original Witch Kitchen test kit. Metres, feet/base pivots, reproducible Blender 4.5 source."""
import bpy, math, os, json
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
OUT=os.path.join(ROOT,'Assets/MamasComing/WitchTest/Models')
SOURCE=os.path.join(ROOT,'ArtSource/Blender/MamaWitch')
os.makedirs(OUT,exist_ok=True); os.makedirs(SOURCE,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
MAT={}
def material(name,color,rough=.55,metal=0,glow=0,tile=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    if glow: p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=glow
    if tile is not None:
        n=m.node_tree.nodes.new('ShaderNodeTexImage'); n.image=bpy.data.images.load(os.path.join(ROOT,'Assets/MamasComing/WitchTest/Textures/WitchSurfaces.png'),check_existing=True)
        uv=m.node_tree.nodes.new('ShaderNodeTexCoord'); mp=m.node_tree.nodes.new('ShaderNodeMapping')
        mp.inputs['Scale'].default_value=(.5,.5,1); mp.inputs['Location'].default_value=(*tile,0)
        m.node_tree.links.new(uv.outputs['UV'],mp.inputs['Vector']); m.node_tree.links.new(mp.outputs['Vector'],n.inputs['Vector']); m.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
    MAT[name]=m; return m
material('SURF_Wood',(.50,.29,.14),.55,tile=(0,.5))
material('SURF_Sage',(.37,.46,.32),.62,tile=(.5,.5))
material('SURF_Cream',(.87,.79,.61),.42,tile=(0,0))
material('SURF_Plum',(.19,.09,.17),.65,tile=(.5,0))
material('MAT_Brass',(.67,.43,.15),.24,.72)
material('MAT_Peach',(.95,.49,.27),.25,0,.3)
material('MAT_Leaf',(.25,.43,.23),.65)
material('MAT_Ink',(.055,.045,.08),.5)
material('MAT_Glass',(.36,.65,.58),.18,.25)
material('MAT_Rose',(.66,.25,.29),.6)
material('MAT_Skin',(.73,.49,.34),.68)
material('MAT_Hair',(.30,.15,.10),.7)
material('MAT_Eye',(.95,.91,.79),.26)
material('MAT_Cork',(.42,.27,.14),.8)
material('MAT_Glow',(.95,.64,.33),.35,0,2.2)
parts=[]
def use(o,name,mat):
    o.name=name; o.data.materials.append(MAT[mat]); parts.append(o); return o
def finish(o,bevel=0):
    bpy.context.view_layer.objects.active=o
    if bevel:
        b=o.modifiers.new('crafted rounded edge','BEVEL'); b.width=bevel; b.segments=3
        bpy.ops.object.modifier_apply(modifier=b.name)
    for p in o.data.polygons: p.use_smooth=True
    w=o.modifiers.new('weighted surface normals','WEIGHTED_NORMAL'); w.keep_sharp=True
    bpy.ops.object.modifier_apply(modifier=w.name)
    return o
def box(n,p,s,m,bevel=.015,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=bpy.context.object; o.dimensions=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if rot:o.rotation_euler=rot
    use(o,n,m); return finish(o,min(bevel,min(s)*.24))
def sphere(n,p,s,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=p);o=bpy.context.object; o.scale=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);use(o,n,m)
    for q in o.data.polygons:q.use_smooth=True
    return o
def lathe(n,profile,m,segments=48,p=(0,0,0)):
    v=[];f=[]
    for r,z in profile:
        for i in range(segments):
            t=2*math.pi*i/segments;v.append((r*math.cos(t),r*math.sin(t),z))
    for j in range(len(profile)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;f.append((a,b,b+segments,a+segments))
    mesh=bpy.data.meshes.new(n);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);o.location=p;use(o,n,m)
    for q in mesh.polygons:q.use_smooth=True
    return o
def tube(n,points,r,m,cyclic=False):
    c=bpy.data.curves.new(n,'CURVE'); c.dimensions='3D';c.resolution_u=3;c.bevel_depth=r;c.bevel_resolution=1
    s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for q,p in zip(s.bezier_points,points):q.co=p;q.handle_left_type='AUTO';q.handle_right_type='AUTO'
    s.use_cyclic_u=cyclic;o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;use(o,n,m);o.select_set(False);return o
def ring(n,p,r,th,m):
    return tube(n,[(p[0]+r*math.cos(i*math.pi/8),p[1]+r*math.sin(i*math.pi/8),p[2]) for i in range(16)],th,m,True)
def leaf(n,p,length,width,m='MAT_Leaf',angle=0):
    # Curved, folded leaf, with a central ridge rather than a flat oval.
    vs=[];fs=[]
    for i in range(9):
        t=i/8; w=math.sin(math.pi*t)*width
        for side in [-1,0,1]:
            x=side*w;y=t*length;z=math.sin(math.pi*t)*length*.18+(1-abs(side))*.004
            vs.append((p[0]+x*math.cos(angle)-y*math.sin(angle),p[1]+x*math.sin(angle)+y*math.cos(angle),p[2]+z))
    for i in range(8):
        for j in range(2):a=i*3+j;fs.append((a,a+1,a+4,a+3))
    mesh=bpy.data.meshes.new(n);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);use(o,n,m)
    sol=o.modifiers.new('leaf thickness','SOLIDIFY');sol.thickness=.001;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=sol.name)
    return o
def export(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    # Bake all mesh transforms, keeping a flat hierarchy and a natural root pivot.
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    root=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(root)
    for o in parts:o.parent=root
    root.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,path_mode='AUTO')
    tri=0
    for o in parts:o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
    return {'name':name,'objects':len(parts),'triangles':tri}
report=[]
# -------------------------------------------------------------------- WAND, 10 cm
parts=[]
tube('Wand_Applewood',[(0,0,0),(.001,0,.025),(-.002,0,.057),(.001,0,.082)],.0029,'SURF_Wood')
for i in range(3):
    z=.012+i*.008;ring('Wand_GripBinding',(0,0,z),.0031,.00055,'MAT_Brass')
for phase in [0,math.pi]:
    tube('Wand_TwiningVine',[(.004*math.cos(t*math.pi*2+phase),.004*math.sin(t*math.pi*2+phase),.046+t*.047) for t in [i/18 for i in range(19)]],.0007,'MAT_Brass')
sphere('Wand_AppleBud',(.001,0,.089),(.008,.0065,.008),'MAT_Peach')
leaf('Wand_Leaf',(.001,0,.095),.014,.004,angle=-.7)
tube('Wand_Stem',[(.001,0,.094),(.004,0,.101)],.0008,'SURF_Wood')
report.append(export('PROP_AppleWand'))
# -------------------------------------------------------------------- CUP, 11 cm
parts=[]
lathe('Cup_Ceramic',[(0,0),(.025,0),(.026,.007),(.032,.012),(.039,.03),(.045,.082),(.046,.098),(.043,.102),(.04,.097),(.039,.082),(.034,.03),(.024,.015),(0,.015)],'SURF_Cream',64)
ring('Cup_GoldLip',(0,0,.100),.0437,.0013,'MAT_Brass');ring('Cup_GoldFoot',(0,0,.008),.026,.001,'MAT_Brass')
tube('Cup_CurvedHandle',[(.041,0,.079),(.066,0,.084),(.073,0,.058),(.057,0,.03),(.037,0,.033)],.005,'SURF_Cream')
ring('Cup_Band',(0,0,.027),.037,.0008,'MAT_Brass')
leaf('Cup_LeafMedallion',(-.007,-.038,.05),.022,.008,'MAT_Leaf',angle=0)
report.append(export('PROP_MoonCup'))
# -------------------------------------------------------------------- POTION, 20 cm
parts=[]
lathe('Bottle_GreenCeramic',[(0,0),(.037,0),(.043,.008),(.047,.055),(.044,.10),(.029,.14),(.018,.155),(.018,.18),(.02,.182),(.021,.19),(.013,.192),(.012,.175),(.012,.16),(.024,.145),(.034,.10),(.036,.02),(0,.018)],'MAT_Glass',56)
lathe('Bottle_Cork',[(0,.18),(.014,.18),(.015,.205),(0,.205)],'MAT_Cork',32)
ring('Bottle_NeckBinding',(0,0,.167),.019,.002,'MAT_Brass')
box('Bottle_Label',(0,-.044,.072),(.054,.003,.052),'SURF_Cream',.008)
leaf('Bottle_LabelLeaf',(-.01,-.047,.061),.029,.009,'MAT_Leaf')
report.append(export('PROP_MoonPotion'))
# -------------------------------------------------------------------- BOOK
parts=[]
box('Book_PageBlock',(0,0,.025),(.235,.285,.045),'SURF_Cream',.006)
for z in [.003,.051]:box('Book_LeatherCover',(0,0,z),(.255,.307,.009),'SURF_Plum',.005)
box('Book_RoundedSpine',(-.126,0,.027),(.023,.308,.057),'SURF_Plum',.01)
for y in [-.12,-.07,.07,.12]:box('Book_SpineBand',(-.13,y,.028),(.026,.013,.061),'MAT_Brass',.004)
for x in [-.107,.107]:
    for y in [-.13,.13]:box('Book_CornerGuard',(x,y,.057),(.03,.032,.003),'MAT_Brass',.004)
ring('Book_AppleSeal',(0,0,.058),.041,.002,'MAT_Brass')
leaf('Book_LeafSeal',(-.01,0,.061),.058,.022,'MAT_Leaf',angle=.2)
box('Book_Bookmark',(.065,-.163,.021),(.018,.05,.002),'MAT_Rose',.001)
for z in [.014,.023,.032,.041]:tube('Book_PageLines',[(-.10,-.144,z),(.1,-.144,z)],.0005,'MAT_Cork')
report.append(export('PROP_Grimoire'))
# -------------------------------------------------------------------- RECEPTACLES
parts=[]
lathe('Saucer',[(0,0),(.065,0),(.083,.007),(.09,.014),(.087,.018),(.064,.01),(.04,.008),(0,.008)],'SURF_Cream',64)
ring('Saucer_GoldRim',(0,0,.016),.087,.0013,'MAT_Brass');ring('Saucer_CupSeat',(0,0,.01),.028,.0009,'MAT_Brass')
report.append(export('PROP_CupSaucer'))
parts=[]
box('Rack_Base',(0,0,.012),(.24,.13,.024),'SURF_Wood',.008)
for x in [-.11,.11]:box('Rack_Post',(x,0,.062),(.018,.12,.10),'SURF_Wood',.008)
for y in [-.054,.054]:box('Rack_Rail',(0,y,.082),(.22,.015,.022),'SURF_Wood',.006)
ring('Rack_BottleNest',(0,0,.03),.049,.003,'MAT_Brass')
report.append(export('PROP_PotionRack'))
parts=[]
for i in range(2):
    box('Stack_Cover',(0,0,i*.032+.004),(.275,.32,.008),'SURF_Plum' if i==0 else 'SURF_Sage',.004)
    box('Stack_Pages',(0,0,i*.032+.018),(.26,.30,.02),'SURF_Cream',.004)
    box('Stack_Cover',(0,0,i*.032+.031),(.275,.32,.006),'SURF_Plum' if i==0 else 'SURF_Sage',.003)
report.append(export('PROP_BookStack'))
# -------------------------------------------------------------------- OPEN SPELL BOOK
parts=[]
box('OpenBook_Leather',(0,0,.012),(.40,.25,.024),'SURF_Plum',.009)
for x in [-.10,.10]:box('OpenBook_Pages',(x,0,.032),(.193,.235,.024),'SURF_Cream',.005)
page=bpy.data.materials.new('MAT_SpellPages');page.use_nodes=True;p=page.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.8
t=page.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(os.path.join(ROOT,'Assets/MamasComing/WitchTest/Textures/SpellPages.png'));page.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color']);MAT['MAT_SpellPages']=page
mesh=bpy.data.meshes.new('SpellPage');mesh.from_pydata([(-.192,-.114,.045),(.192,-.114,.045),(.192,.114,.045),(-.192,.114,.045)],[],[(0,1,2,3)]);mesh.update()
uv=mesh.uv_layers.new();coords=[(0,0),(1,0),(1,1),(0,1)]
for loop in mesh.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
o=bpy.data.objects.new('OpenBook_IllustratedSpread',mesh);bpy.context.collection.objects.link(o);use(o,o.name,'MAT_SpellPages')
report.append(export('PROP_OpenSpellbook'))
# -------------------------------------------------------------------- HUMAN-SCALE KITCHEN
parts=[]
for row in range(12):
    for col in range(4):
        x=-1.8+(col+.5)*.9;y=-1.65+(row+.5)*.28
        box('Floor_OakPlank',(x,y,-.026),(.896,.275,.052),'SURF_Wood',.006)
# Back wall with actual window opening, room front kept open for camera.
box('Wall_BackLower',(0,1.72,.53),(3.7,.14,1.06),'SURF_Cream',.012)
box('Wall_BackUpper',(0,1.72,2.48),(3.7,.14,.56),'SURF_Cream',.012)
box('Wall_BackLeft',(-1.25,1.72,1.62),(1.2,.14,1.16),'SURF_Cream',.012)
box('Wall_BackRight',(1.55,1.72,1.62),(.6,.14,1.16),'SURF_Cream',.012)
box('Wall_Left',(-1.87,.35,1.38),(.14,2.9,2.76),'SURF_Cream',.015)
box('Wall_Right',(1.87,.35,1.38),(.14,2.9,2.76),'SURF_Cream',.015)
box('Wall_Front',(0,-1.16,1.38),(3.7,.14,2.76),'SURF_Cream',.015)
for z in [.15,2.55]:box('Wall_LeftTrim',(-1.78,.35,z),(.04,2.9,.05),'MAT_Brass',.008)
for x in [-.65,1.25]:box('Window_Jamb',(x,1.61,1.63),(.065,.12,1.19),'SURF_Wood',.012)
for z in [1.05,2.22]:box('Window_Header',(.30,1.61,z),(1.95,.13,.07),'SURF_Wood',.012)
box('Window_Center',(.3,1.63,1.63),(.045,.08,1.1),'SURF_Wood',.008)
box('Window_Crossbar',(.3,1.63,1.62),(1.8,.08,.035),'SURF_Wood',.007)
box('Window_Sill',(.3,1.50,1.04),(2.02,.3,.07),'SURF_Wood',.012)
# Cabinets and continuous stone working surface, real height .94 m.
box('Cabinet_Carcass',(0,1.25,.455),(3.2,.60,.87),'SURF_Sage',.025)
box('Countertop_Stone',(0,1.23,.915),(3.29,.69,.05),'SURF_Cream',.016)
box('Cabinet_Kickboard',(0,.937,.085),(3.16,.04,.12),'SURF_Plum',.008)
for i in range(6):
    x=-1.59+(i+.5)*.53
    box('Cabinet_Door',(x,.935,.49),(.505,.035,.69),'SURF_Sage',.015)
    for sx in [-1,1]:box('Cabinet_DoorMoulding',(x+sx*.214,.908,.49),(.015,.012,.59),'SURF_Wood',.004)
    for z in [.205,.775]:box('Cabinet_DoorMoulding',(x,.908,z),(.44,.012,.014),'SURF_Wood',.004)
    sphere('Cabinet_BrassKnob',(x+.165,.886,.71),(.016,.016,.016),'MAT_Brass')
for i in range(14):
    x=-1.55+i*.235
    box('Backsplash_Tile',(x,1.625,1.075),(.229,.025,.24),'SURF_Sage' if i%3 else 'SURF_Cream',.005)
# Wall shelving, architectural silhouette and human scale cues.
for x in [-1.23,1.48]:
    box('Shelf_CurvedLip',(x,1.45,1.83),(.52,.29,.045),'SURF_Wood',.014)
    for xx in [x-.19,x+.19]:tube('Shelf_Bracket',[(xx,1.61,1.61),(xx,1.46,1.68),(xx,1.33,1.81)],.014,'MAT_Brass')
    for j in range(3):
        xx=x-.15+j*.15
        lathe('Shelf_SpiceJar',[(0,0),(.045,0),(.05,.10),(.04,.16),(.03,.17),(0,.17)],'MAT_Glass' if j%2 else 'SURF_Cream',32,p=(xx,1.45,1.85))
        lathe('Shelf_Cork',[(0,0),(.033,0),(.033,.026),(0,.026)],'MAT_Cork',24,p=(xx,1.45,2.015))
# Giant stool, lower floor only, helps communicate scale.
lathe('Stool_Seat',[(0,0),(.22,0),(.23,.025),(.22,.05),(0,.05)],'SURF_Wood',48,p=(.9,.08,.48))
for x in [.74,1.06]:
    for y in [-.08,.24]:tube('Stool_Leg',[(x+(.02 if x>.9 else -.02),y,.03),(x,y,.5)],.022,'SURF_Wood')
ring('Stool_Footrest',(.9,.08,.2),.19,.012,'MAT_Brass')
# Hanging lamp, cropped ceiling remains open.
tube('Lamp_Cord',[(-.55,.3,2.75),(-.55,.3,2.3)],.008,'MAT_Ink')
lathe('Lamp_Shade',[(.05,0),(.18,-.16),(.175,-.17),(.04,-.015)],'SURF_Plum',48,p=(-.55,.3,2.3))
sphere('Lamp_Bulb',(-.55,.3,2.17),(.04,.04,.04),'MAT_Glow')
# Apple leaf botanical relief, repeated with restraint.
for i in range(4):leaf('Window_PlantLeaf',(.9+i*.035,1.45,1.09),.14,.045,'MAT_Leaf',angle=i*.8)
lathe('Window_PlantPot',[(0,0),(.06,0),(.08,.1),(.075,.11),(.065,.10),(.045,.018),(0,.018)],'SURF_Plum',40,p=(.96,1.47,1.075))
report.append(export('ENV_WitchKitchen'))
# -------------------------------------------------------------------- MOTHER, actual human dimensions
parts=[]
for x in [-.095,.095]:sphere('Mother_Shoe',(x,-.065,.055),(.075,.14,.055),'MAT_Ink')
lathe('Mother_TailoredDress',[(.21,.11),(.29,.19),(.31,.33),(.27,.62),(.21,.87),(.19,1.02),(.25,1.12),(.21,1.22),(.08,1.29)],'SURF_Plum',64)
ring('Mother_Hem',(0,0,.18),.285,.009,'MAT_Brass');ring('Mother_Belt',(0,0,.88),.213,.015,'MAT_Brass')
box('Mother_BeltBuckle',(0,-.219,.88),(.08,.012,.065),'MAT_Brass',.015)
for side in [-1,1]:
    tube('Mother_Sleeve',[(side*.21,0,1.16),(side*.31,-.025,.99),(side*.29,-.14,.88)],.055,'SURF_Plum')
    sphere('Mother_Hand',(side*.285,-.15,.86),(.043,.038,.065),'MAT_Skin')
lathe('Mother_Neck',[(.055,1.22),(.055,1.34)],'MAT_Skin',32)
sphere('Mother_Head',(0,-.005,1.43),(.105,.095,.14),'MAT_Skin')
sphere('Mother_Nose',(0,-.1,1.42),(.027,.034,.035),'MAT_Skin')
for side in [-1,1]:
    sphere('Mother_EyeWhite',(side*.04,-.087,1.46),(.025,.014,.022),'MAT_Eye')
    sphere('Mother_Eye',(side*.04,-.100,1.46),(.010,.005,.014),'MAT_Ink')
    tube('Mother_Brow',[(side*.019,-.1,1.495),(side*.048,-.101,1.503),(side*.064,-.092,1.498)],.005,'MAT_Hair')
    sphere('Mother_HairCurl',(side*.093,.0,1.47),(.045,.085,.1),'MAT_Hair')
tube('Mother_Mouth',[(-.028,-.091,1.37),(0,-.101,1.363),(.026,-.091,1.37)],.004,'MAT_Ink')
sphere('Mother_HairBun',(0,.082,1.49),(.10,.07,.10),'MAT_Hair')
lathe('Mother_HatBrim',[(0,1.55),(.23,1.55),(.25,1.565),(.23,1.58),(0,1.58)],'SURF_Plum',64)
lathe('Mother_HatCrown',[(.115,1.57),(.105,1.66),(.08,1.77),(.046,1.86),(.016,1.92),(0,1.94)],'SURF_Plum',48)
ring('Mother_HatRibbon',(0,0,1.62),.11,.012,'MAT_Rose')
leaf('Mother_HatLeaf',(-.025,-.108,1.64),.08,.025,'MAT_Brass')
report.append(export('CHR_MotherWitch'))
# Keep full reproducible scene source. A preview is separate from Unity verification.
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE,'WitchKitchen_Kit_v1.blend'))
with open(os.path.join(SOURCE,'model-manifest.json'),'w') as f:json.dump({'units':'metres','models':report},f,indent=2)
print('WITCH_EXPORT_COMPLETE '+json.dumps(report))
