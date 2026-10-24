"""MAMA'S COMING whimsical house props, sculpted in clay (clay.py): ordinary home objects with a little fantasy.

Run:  blender -b -P ArtSource/Blender/Scripts/sculpt/props.py
Writes Assets/MamasComing/Art/Sculpted/Props/PROP_*.fbx, ArtSource/Blender/Sculpted/Props.blend and
docs/evidence/sculpted-kids/PROP_*.png. Pivots at the base centre, 1 unit = 1 m, front = -Y.
"""
import bpy, os, sys, math, json
import numpy as np
from mathutils import Vector, Euler, Matrix

sys.path.insert(0, os.path.dirname(__file__))
import clay as C

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../..'))
OUT = ROOT + '/Assets/MamasComing/Art/Sculpted/Props'
SRC = ROOT + '/ArtSource/Blender/Sculpted'
EVID = ROOT + '/docs/evidence/sculpted-kids'


def painted(S, name, mats, chooser, tris):
    ob = S.to_object(name)
    C.paint(ob, mats, chooser)
    C.decimate(ob, tris)
    return ob


def teapot():
    """Plump teapot whose spout curls like an elephant trunk and whose lid wears a knitted bobble."""
    F = C.Field('Teapot', .009)
    F.blob((0, 0, .17), .17, (1.1, 1.0, .9))
    F.blob((0, 0, .06), .13, (1.15, 1.1, .45))
    F.chain([(0, -.15, .15), (0, -.25, .2), (0, -.31, .28), (0, -.29, .34), (0, -.25, .33)], .045, .022)   # curly spout
    F.chain([(0, .14, .25), (0, .24, .22), (0, .25, .12), (0, .15, .09)], .022)                               # handle
    F.blob((0, 0, .32), .09, (1, 1, .45))
    F.ball((0, 0, .39), .045)
    S = F.solid(.008); S.smooth(3)
    S.crease(Vector((0, -.25, .335)), .02, .015)                                   # spout opening
    for z in (.06, .29): S.stroke([Vector((math.cos(a) * .19 * (1 if z < .1 else .5), math.sin(a) * .19 * (1 if z < .1 else .5), z)) for a in np.linspace(0, 6.3, 25)], .02, S.crease, .02)
    S.thumbprints(10, .03, .003, seed=31); S.smooth(1, .3, -.31)
    m = [C.material('Prop_TeaMint', '#7FE0C4', .35), C.material('Prop_TeaPink', '#FF8FB1', .45), C.material('Prop_Ink', '#1E1A2E', .4)]
    return painted(S, 'PROP_CurlyTeapot', m, lambda c, n: np.where(c[:, 2] > .3, 1, np.where((np.abs(c[:, 2] - .2) < .025), 1, 0)), 4000)


def moon_lamp():
    """Crescent-moon floor lamp on a twisting stalk, bulb glowing in the moon's cup."""
    F = C.Field('MoonLamp', .01)
    F.blob((0, 0, .03), .18, (1, 1, .22))
    pts = [Vector((math.sin(t * 3) * .05, math.cos(t * 3) * .05 - .05, .05 + t * 1.25)) for t in np.linspace(0, 1, 10)]
    F.chain(pts, .035, .025)
    S = F.solid(.01); S.smooth(3)
    M = C.Field('Moon', .008)
    for a in np.linspace(-1.2, 1.2, 11):
        r = .07 * math.cos(a * .9) + .02
        M.ball((math.sin(a) * .26, -.05, 1.35 + math.cos(a) * .26 - .26), r)
    MS = M.solid(.008); MS.smooth(3)
    MS.crease(Vector((-.09, -.12, 1.22)), .04, .015); MS.crease(Vector((.09, -.12, 1.22)), .04, .015)   # sleepy eyes
    mats = [C.material('Prop_LampLilac', '#A98BFF', .45), C.material('Prop_MoonButter', '#FFE27A', .3), C.material('Prop_Bulb', '#FFF8D6', .2)]
    a = painted(S, 'PROP_MoonLamp', mats, lambda c, n: np.zeros(len(c), int), 2500)
    b = painted(MS, 'PROP_MoonLamp_Moon', mats, lambda c, n: np.ones(len(c), int), 2500)
    bulb = C.uv_sphere('PROP_MoonLamp_Bulb', .07, (0, -.05, 1.2), (1, 1, 1.2), 16, 10); bulb.data.materials.append(mats[2])
    return join([a, b, bulb], 'PROP_MoonLamp')


def mushroom_stool():
    """Chunky toadstool stool: puffy cap with raised spots, squat stem with a skirt."""
    F = C.Field('Mushroom', .01)
    F.blob((0, 0, .18), .12, (1, 1, 1.5))
    F.blob((0, 0, .32), .16, (1.1, 1.1, .35))
    F.blob((0, 0, .44), .27, (1, 1, .5))
    S = F.solid(.01); S.smooth(3)
    rng = np.random.default_rng(4)
    spots = []
    for i in range(9):
        a = rng.uniform(0, 6.28); r = rng.uniform(.05, .22)
        p = S.surface((math.cos(a) * r, math.sin(a) * r, 1.0), (0, 0, -1))
        S.inflate(p, .05, .012); spots.append(p)
    S.flatten(Vector((0, 0, .58)), .2, (0, 0, 1), .6)                                 # sit-able top
    S.thumbprints(10, .04, .003, seed=5); S.smooth(1, .3, -.31)
    sp = np.asarray(spots)
    m = [C.material('Prop_StemCream', '#FFF1D6', .55), C.material('Prop_CapRed', '#FF5A5F', .4), C.material('Prop_Spot', '#FFFDF6', .5)]

    def ch(c, n):
        out = np.where(c[:, 2] > .37, 1, 0)
        d = np.min(np.linalg.norm(c[:, None, :] - sp[None], axis=2), axis=1)
        out[(d < .045) & (c[:, 2] > .37)] = 2
        return out
    return painted(S, 'PROP_MushroomStool', m, ch, 3000)


def jelly_beanbag():
    """Jelly-bean beanbag: a squishy bean that slumps, with a stitched seam."""
    F = C.Field('Bean', .012)
    F.blob((-.12, 0, .2), .26, (1, .9, .75))
    F.blob((.14, 0, .16), .24, (1, .95, .7))
    F.blob((0, .12, .32), .18, (1.2, .7, .8))
    S = F.solid(.012); S.smooth(4)
    S.clamp_floor(0.0, .05)
    S.stroke([Vector((x, -.24, .16 + .05 * math.sin(x * 8))) for x in np.linspace(-.35, .35, 9)], .025, S.crease, .02)
    S.lumps(8, .1, .01, seed=8)
    S.smooth(1, .3, -.31)
    m = [C.material('Prop_BeanTeal', '#38D4C8', .35), C.material('Prop_BeanSeam', '#FFFDF6', .5)]
    return painted(S, 'PROP_JellyBeanbag', m, lambda c, n: np.where(np.abs(c[:, 1] + .24) < .025, 1, 0), 3000)


def snail_clock():
    """Wall clock grown as a snail shell: spiral shell is the dial, the snail peeks out underneath."""
    F = C.Field('Snail', .008)
    for i, t in enumerate(np.linspace(0, 1, 26)):
        a = t * 4.2 * math.pi
        r = .03 + .2 * t
        F.ball((math.cos(a) * r * (1 - t * .25), 0, .3 + math.sin(a) * r * (1 - t * .25)), .03 + .07 * t)
    F.chain([(-.2, -.02, .1), (0, -.04, .08), (.22, -.05, .12), (.3, -.06, .2)], .05, .035)
    F.rope((.3, -.06, .2), (.33, -.06, .3), .012); F.rope((.27, -.06, .2), (.27, -.06, .31), .012)
    S = F.solid(.008); S.smooth(3)
    S.flatten(Vector((0, -.08, .3)), .3, (0, -1, 0), .7)                               # dial face
    S.thumbprints(8, .03, .003, seed=12); S.smooth(1, .3, -.31)
    m = [C.material('Prop_ShellPeach', '#FFB37A', .4), C.material('Prop_SnailLime', '#B6F06A', .45), C.material('Prop_Ink', '#1E1A2E', .4)]
    ob = painted(S, 'PROP_SnailClock', m, lambda c, n: np.where(c[:, 2] < .16, 1, 0), 3500)
    hands = []
    for L, ang, nm in ((.12, 30, 'H'), (.17, 140, 'M')):
        h = C.Field('Hand' + nm, .006)
        h.rope((0, -.09, .3), (math.sin(math.radians(ang)) * L, -.09, .3 + math.cos(math.radians(ang)) * L), .012)
        ho = h.solid(.006).to_object('PROP_SnailClock_' + nm); ho.data.materials.append(m[2]); hands.append(ho)
    return join([ob] + hands, 'PROP_SnailClock')


def soap_bottle():
    """Pump soap bottle with a bubble crown floating off the nozzle."""
    F = C.Field('Soap', .008)
    F.blob((0, 0, .11), .085, (1, .8, 1.35))
    F.blob((0, 0, .23), .03, (1, 1, .8))
    F.chain([(0, 0, .25), (0, 0, .29), (0, -.06, .29)], .015)
    S = F.solid(.007); S.smooth(3); S.thumbprints(6, .02, .002, seed=2); S.smooth(1, .3, -.31)
    m = [C.material('Prop_SoapBerry', '#FF6FAE', .2), C.material('Prop_PumpWhite', '#FFFDF6', .5), C.material('Prop_Bubble', '#BDEBFF', .05)]
    ob = painted(S, 'PROP_BubbleSoap', m, lambda c, n: np.where(c[:, 2] > .215, 1, 0), 2000)
    parts = [ob]
    for i, (p, r) in enumerate((((0, -.09, .33), .03), ((.03, -.11, .39), .022), ((-.02, -.1, .44), .015))):
        b = C.uv_sphere(f'PROP_BubbleSoap_B{i}', r, p, (1, 1, 1), 14, 8); b.data.materials.append(m[2]); parts.append(b)
    return join(parts, 'PROP_BubbleSoap')


def join(objs, name):
    for o in objs: C.apply_xf(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active; o.name = name; o.data.name = name
    return o


def render(ob):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.cavity_type = 'BOTH'
    sh.show_object_outline = True; sh.object_outline_color = (.07, .06, .12)
    sc.render.resolution_x = sc.render.resolution_y = 512
    cam = bpy.data.objects.get('Cam') or bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    if cam.name not in sc.collection.objects: sc.collection.objects.link(cam)
    sc.camera = cam
    dims = ob.dimensions; h = max(dims) * 1.6 + .3
    cam.location = (-h * .55, -h * 1.0, dims.z * .55 + h * .25)
    d = Vector((0, 0, dims.z * .45)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    for o in sc.objects:
        if o.type == 'MESH': o.hide_render = o != ob
    sc.render.filepath = f'{EVID}/{ob.name}.png'
    bpy.ops.render.render(write_still=True)


C.reset()
os.makedirs(OUT, exist_ok=True); os.makedirs(EVID, exist_ok=True); os.makedirs(SRC, exist_ok=True)
report = {}
for fn in (teapot, moon_lamp, mushroom_stool, jelly_beanbag, snail_clock, soap_bottle):
    ob = fn()
    for p in ob.data.polygons: p.use_smooth = True
    render(ob)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=f'{OUT}/{ob.name}.fbx', use_selection=True, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='FACE', bake_anim=False)
    report[ob.name] = {'triangles': C.tri_count(ob), 'size_m': [round(x, 3) for x in ob.dimensions]}
    print('PROP_DONE', ob.name, report[ob.name])
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=f'{SRC}/Props_Sculpt.blend', copy=True)
json.dump(report, open(f'{SRC}/props-report.json', 'w'), indent=2)
