"""Quick workbench preview (material viewport colours, cavity+outline) of a .blend from several yaw angles.
usage: python3 -I preview_workbench.py <file.blend> <out_prefix> [yaw1,yaw2,...] [res]"""
import bpy, sys, math
from mathutils import Vector
blend, out = sys.argv[1], sys.argv[2]
yaws = [float(a) for a in (sys.argv[3] if len(sys.argv) > 3 else '0,35,90').split(',')]
res = int(sys.argv[4]) if len(sys.argv) > 4 else 600
bpy.ops.wm.open_mainfile(filepath=blend)
PAL = {'Bark':(0.35,0.2,0.12),'LeafLight':(0.55,0.8,0.3),'Leaf':(0.25,0.55,0.25),'Pine':(0.15,0.4,0.3),'CrystalPink':(1,0.5,0.8),'Crystal':(0.4,0.8,1),
       'Rock':(0.5,0.47,0.55),'GrassTop':(0.4,0.7,0.3),'Stone':(0.75,0.72,0.66),'ClockBody':(0.85,0.15,0.2),'ClockFace':(0.98,0.95,0.86),'EyeGlow':(1,0.9,0.2),
       'Brass':(0.85,0.65,0.3),'Ink':(0.1,0.08,0.12),'Gold':(1,0.8,0.3),
       'BR_WoodDark':(0.45,0.25,0.18),'BR_Wood':(0.85,0.6,0.4),'BR_Cream':(0.98,0.94,0.85),'BR_DuvetB':(1,0.85,0.4),'BR_Duvet':(0.45,0.55,0.95),'BR_Pillow':(1,1,1),'BR_Red':(0.9,0.2,0.25),
       'BR_Shade':(1,0.75,0.4),'BR_Book1':(0.3,0.5,0.9),'BR_Book2':(0.95,0.75,0.2),'BR_Book3':(0.4,0.75,0.5),'BR_Book4':(0.7,0.45,0.85),'BR_Moon':(1,0.95,0.7),'BR_Glass':(0.7,0.9,1),
       'BR_Memory':(1,0.85,0.3),'BR_Trim':(0.98,0.95,0.9),'BR_Curtain':(0.55,0.4,0.85),'BR_Rug1':(0.95,0.5,0.55),'BR_Rug2':(1,0.85,0.6),'BR_Rug3':(0.5,0.4,0.85),
       'CM_Coat':(0.12,0.12,0.32),'CM_Ink':(0.05,0.04,0.08),'CM_Gold':(1,0.78,0.3),'CM_Glass':(0.15,0.2,0.4),'CM_White':(0.97,0.95,0.92),'CM_Face':(1,0.96,0.85),'CM_Eye':(1,0.15,0.6),'CM_Red':(0.85,0.1,0.2),'CM_Glow':(0.5,0.95,1),'CM_BladeH':(0.95,0.75,0.3),'CM_BladeM':(0.8,0.85,0.95),'CM_Cape':(0.3,0.15,0.55),'MP_Stone':(0.4,0.38,0.55),'MP_Brick':(0.45,0.2,0.3),'MP_Dark':(0.08,0.07,0.12),'MP_Face':(1,0.95,0.82),'MP_Gold':(1,0.78,0.3),'MP_Roof':(0.2,0.25,0.5),'MP_Glow':(0.5,1,1),'MP_Window':(1,0.85,0.5),'MP_Wood':(0.5,0.3,0.2),'MP_Red':(0.9,0.15,0.25),'MP_Metal':(0.6,0.62,0.7),'MP_LampGlow':(1,0.9,0.6),'MP_Shard':(0.6,0.95,1),'MP_MinionBody':(0.85,0.15,0.3),'MP_Eye':(1,0.9,0.2),'MP_Mask':(0.97,0.97,0.97),'MP_WhaleBelly':(0.7,0.8,1),'MP_Whale':(0.15,0.2,0.5),'MP_Paper':(1,1,1),'MP_Tram':(1,0.85,0.1),'BR_Plush':(0.3,0.25,0.35),'BR_Yellow':(1,0.85,0.1),'BR_Cake1':(1,0.8,0.85),'BR_Cake2':(0.75,0.65,1),'BR_Cake3':(0.55,0.75,1),'BR_Cloud':(1,1,1),'BR_Paper':(1,1,0.95),'Apple_Skin':(0.85,0.12,0.16),'Apple_Limb':(0.95,0.78,0.6),'Apple_Glove':(0.97,0.95,0.9),'Apple_EyeWhite':(0.97,0.96,0.92),'Apple_PupilIris':(0.55,0.22,0.12),'Apple_PupilShine':(1,1,1),'Apple_Pupil':(0.06,0.05,0.09),'Apple_Ink':(0.3,0.03,0.07),'Apple_LeafVein':(0.75,0.9,0.5),'Apple_Leaf':(0.4,0.72,0.35),'Apple_Stem':(0.5,0.3,0.16),'Apple_Cheek':(1,0.55,0.6),'Apple_Tongue':(1,0.45,0.5),'Apple_Shoe':(0.62,0.52,0.95),'Apple_Sole':(0.98,0.96,0.92),'Apple_Accent':(1,0.82,0.2),'Apple_PackTrim':(0.98,0.96,0.92),'Apple_Pack':(0.45,0.6,0.98),'Apple_Belt':(0.3,0.22,0.5),'Apple_Sleeve':(0.62,0.55,0.98),'Apple_Sock':(0.98,0.97,0.95),'Chair_WoodDark':(0.6,0.36,0.21),'Chair_Wood':(0.8,0.53,0.33),'Chair_EyeWhite':(0.97,0.95,0.9),'Chair_Ink':(0.17,0.13,0.2),'Chair_Blush':(0.95,0.6,0.55),'Chair_Cushion':(0.95,0.85,0.82),'MP_Brute':(0.12,0.23,0.37),'MP_Brass':(0.78,0.57,0.23),'MP_EyeRed':(1,0.16,0.23),'MP_Cuckoo':(0.48,0.18,0.23),'MP_Feather':(0.42,0.25,0.82),'MP_Orange':(1,0.54,0.12),'Blade_Leaf':(0.35,0.78,0.35),'Blade_Vein':(0.8,0.95,0.5),'Blade_Gold':(1,0.8,0.25),'Blade_Grip':(0.5,0.3,0.18),'Blade_Twine':(0.9,0.8,0.55),'MP_Pyjama':(0.45,0.6,1),'MP_CarPaint':(0.9,0.3,0.5),'MP_CarGlass':(0.15,0.3,0.45),'MP_HeadLight':(1,1,0.9),'MP_TailLight':(1,0.1,0.2),'MP_Trunk':(0.55,0.42,0.3),'MP_Leaf':(0.3,0.65,0.3),'MP_Screen':(0.3,0.9,1),'MP_LightRed':(1,0.2,0.2),'MP_LightGreen':(0.3,1,0.6),'MP_Train':(0.1,0.7,0.65)}
PAL.update({'MP_IntWoodDark':(0.48,0.32,0.22),'MP_IntWood':(0.73,0.57,0.41),'MP_IntCream':(0.95,0.9,0.81),'MP_IntTeal':(0.37,0.6,0.58),'MP_IntRose':(0.85,0.55,0.55),'MP_IntLilac':(0.66,0.58,0.82),'MP_IntMustard':(0.89,0.71,0.35),'MP_IntInk':(0.22,0.22,0.3),'MP_IntLeaf':(0.44,0.64,0.45),'MP_IntGlass':(0.75,0.89,0.92),'MP_IntCake':(0.96,0.78,0.81),'MP_IntTatami':(0.8,0.75,0.52),'MP_IntCopper':(0.75,0.48,0.31)})
for m in bpy.data.materials:
    for k,c in sorted(PAL.items(), key=lambda kv:-len(kv[0])):
        if k in m.name: m.diffuse_color=(c[0],c[1],c[2],1); break
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.show_object_outline = True
sh.background_type = 'VIEWPORT'
sc.render.resolution_x = sc.render.resolution_y = res
objs = [o for o in sc.objects if o.type == 'MESH']
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in objs:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c); mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
ctr = (mn + mx) / 2; size = max(mx - mn)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 60
for yaw in yaws:
    a = math.radians(yaw)
    d = size * 2.6
    cam.location = ctr + Vector((math.sin(a) * d, -math.cos(a) * d, size * 0.35))
    cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = f"{out}_{int(yaw)}.png"
    bpy.ops.render.render(write_still=True)
