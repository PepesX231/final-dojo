"""MAMA'S COMING fruit kids, sculpted in clay (see clay.py) and rigged/animated (see rig.py).

Run:  blender -b -P ArtSource/Blender/Scripts/sculpt/kids.py -- [Orange Banana ...] [--preview-only]
Writes per kid:
  Assets/MamasComing/Art/Sculpted/<Name>/<Name>_Sculpt.fbx   skinned mesh + 8 animation takes
  ArtSource/Blender/Sculpted/<Name>_Sculpt.blend              editable source
  docs/evidence/sculpted-kids/<Name>_*.png                    turnaround renders
Style: bright, kid-friendly comic toon. Chunky readable silhouettes, big expressive faces,
saturated fruit colours, white cartoon gloves, colourful sneakers.
"""
import bpy, os, sys, math, json
import numpy as np
from mathutils import Vector, Euler, Matrix

sys.path.insert(0, os.path.dirname(__file__))
import clay as C
import rig as R

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../..'))
OUT = ROOT + '/Assets/MamasComing/Art/Sculpted'
SRC = ROOT + '/ArtSource/Blender/Sculpted'
EVID = ROOT + '/docs/evidence/sculpted-kids'
HEIGHT = 1.08

INK = '#1E1A2E'
GLOVE = '#FFF8EC'
EYE = '#FFFDF6'
MOUTH = '#5A1630'
TONGUE = '#FF7A8A'


# ================================================================== shared kid anatomy
class Kid:
    def __init__(self, name, body_c, body_r, shoe, limb='#3B2F4A', persona=None):
        self.name = name
        self.bc = Vector(body_c)        # centre of the fruit body
        self.br = body_r                # nominal radius (influence + face placement)
        self.shoe = shoe                # (upper, sole, lace)
        self.limb = limb
        self.persona = persona or {}
        self.field = C.Field(name, res=0.010)
        self.extras = []                # rigid meshes riding on Body
        self.slots = {}

    # --- standard noodle limbs, gloves and sneakers, blended into the same clay
    def limbs(self, shoulder_z, shoulder_x, hip_x=.085, arm_out=.11, arm_len=.25, leg_top=None):
        bc, F = self.bc, self.field
        sh = Vector((shoulder_x, 0.0, shoulder_z))
        el = sh + Vector((arm_out * .55, -.01, -arm_len * .5))
        wr = sh + Vector((arm_out, -.015, -arm_len))
        hand = wr + (wr - el).normalized() * .07
        leg_top = leg_top if leg_top is not None else bc.z - self.br * .55
        hip = Vector((hip_x, 0, leg_top))
        ank = Vector((hip_x + .01, -.005, .075))
        kn = (hip + ank) / 2 + Vector((0, -.012, 0))
        toe = ank + Vector((0.006, -.11, -.035))
        self.J = R.Joints(pelvis=(0, 0, leg_top + .02), chest=(0, 0, bc.z + self.br * .6), face=(0, -self.br, bc.z + self.br * .1),
                          shoulder=sh, elbow=el, wrist=wr, hand=hand, hip=hip, knee=kn, ankle=ank, toe=toe)
        for s in 'LR':
            j = self.J.j[s]
            F.chain([j['shoulder'], j['elbow'], j['wrist']], .026, .022)
            F.chain([j['hip'] + Vector((0, 0, .04)), j['knee'], j['ankle']], .03, .027)
            # cartoon glove: puffy fist + thumb + rolled cuff
            d = (j['hand'] - j['wrist']).normalized()
            side = Vector((1 if s == 'L' else -1, 0, 0))
            F.blob(j['wrist'] + d * .06, .056, (1.0, 1.05, .9))
            F.ball(j['wrist'] + d * .045 + Vector((0, -.04, .02)) - side * .01, .024)
            F.blob(j['wrist'] + d * .01, .04, (1.15, 1.15, .55), rot=d.to_track_quat('Z', 'Y'))
            # chunky sneaker: toe bulb, heel, ankle collar
            a = j['ankle']
            F.blob(a + Vector((0, -.05, -.035)), .07, (.85, 1.35, .62))
            F.blob(a + Vector((0, .02, -.03)), .06, (.9, .9, .7))
            F.blob(a + Vector((0, -.005, .0)), .045, (1, 1, .55))
        self.hand_c = {s: self.J.j[s]['wrist'] + (self.J.j[s]['hand'] - self.J.j[s]['wrist']).normalized() * .05 for s in 'LR'}

    def face(self, S, eye_sep=.11, eye_z=.08, eye_r=.062, look=(0, 0), lid=0.25, lid_tilt=0.0, brow=.02,
             smile=(.1, .035, -.07), grin_depth=.03, cheeks=.03, teeth=True, tongue=True, squint=(0, 0)):
        """Sculpt sockets, cheeks and a mouth into the clay, then seat cartoon eyes in the sockets."""
        bc = self.bc
        front = lambda x, z: S.surface(bc + Vector((x, -1.0, z)), (0, 1, 0))
        self.eyes = []
        for k in (-1, 1):
            p = front(k * eye_sep, eye_z)
            S.crease(p + Vector((0, .01, 0)), eye_r * 1.25, eye_r * .55, pinch=.05)          # socket
            S.inflate(p + Vector((0, 0, eye_r * 1.05)), eye_r * 1.1, brow, scale=(1.3, 1, .6))   # brow ridge
            S.inflate(front(k * (eye_sep + .03), smile[2] + .02), .07, cheeks)              # apple cheeks
            self.eyes.append((k, p))
        # mouth: a crescent 'D' grin scooped out of the clay; the lip rim is pushed up around it
        self.smile = smile; self.open = grin_depth * 2.2
        code, depth = self.mouth_region(S.V)
        N = S._fast_normals()
        rim = (code == 0) & (self.mouth_region(S.V, pad=.018)[0] > 0)
        S.V -= (depth * grin_depth * 1.2)[:, None] * N
        S.V[rim] += N[rim] * grin_depth * .18
        S.smooth_region(front(0, smile[2]), smile[0] * 1.5, 1)
        self.eye_cfg = dict(r=eye_r, look=look, lid=lid, lid_tilt=lid_tilt, teeth=teeth, tongue=tongue, squint=squint)

    def mouth_region(self, P, pad=0.0):
        """Per point: 0 none, 1 mouth, 2 tongue, 3 teeth; plus scoop depth 0..1 (front-facing crescent)."""
        w, curve, mz = self.smile
        w = w + pad
        x = P[:, 0] - self.bc.x; z = P[:, 2] - self.bc.z
        u = np.clip(x / w, -1, 1)
        zt = mz + curve * u * u + pad
        zb = mz + curve * u * u - self.open * (1 - u * u) - pad
        t = (z - zb) / np.maximum(1e-4, zt - zb)
        inside = (np.abs(x) < w) & (t > 0) & (t < 1) & (P[:, 1] < self.bc.y - self.br * .35)
        code = np.where(inside, 1, 0)
        code[inside & (t > .74) & (np.abs(u) < .78)] = 3
        code[inside & (t < .42) & (np.abs(u) < .5)] = 2
        m = np.minimum(np.minimum(t, 1 - t) * 2, 1 - np.abs(u))
        return code, np.where(inside, C.smoothstep(m * 5), 0.0)

    def build_eyes(self, S):
        cfg = self.eye_cfg; r = cfg['r']
        N = S._fast_normals()
        mats = self.mats
        for k, p in self.eyes:
            # seat the eyeball in the socket, bulging out of the face like a toy's
            i = np.argmin(np.linalg.norm(S.V - np.asarray(p), axis=1)); n = Vector(N[i])
            c = Vector(p) + n * (r * .25)
            eye = C.uv_sphere(f'{self.name}_Eye_{k}', r, c, (1.0, .72, 1.18 - cfg['squint'][0 if k < 0 else 1]), 24, 14)
            eye.rotation_euler = n.to_track_quat('-Y', 'Z').to_euler()
            eye.data.materials.append(mats['eye'])
            C.apply_xf(eye)
            look = Vector((cfg['look'][0], 0, cfg['look'][1]))
            pc = c + n * (r * .62) + look * r * .35 + Vector((-k * .004, 0, 0))
            pupil = C.uv_sphere(f'{self.name}_Pupil_{k}', r * .48, pc, (1, .45, 1.25), 16, 10)
            pupil.rotation_euler = n.to_track_quat('-Y', 'Z').to_euler()
            pupil.data.materials.append(mats['ink']); C.apply_xf(pupil)
            hl = C.uv_sphere(f'{self.name}_Glint_{k}', r * .15, pc + n * r * .08 + Vector((k * .004, -r * .12, r * .14)), (1, .5, 1), 10, 6)
            hl.data.materials.append(mats['eye']); C.apply_xf(hl)
            self.extras += [eye, pupil, hl]
            # heavy cartoon eyelid: a peel-coloured cap slanted for attitude
            if cfg['lid'] > 0:
                lid = C.uv_sphere(f'{self.name}_Lid_{k}', r * 1.08, c + n * .004, (1.06, .8, 1.25), 20, 12)
                tilt = cfg['lid_tilt'] * k
                me = lid.data
                cut = r * 1.25 * (1 - 2 * cfg['lid'])
                import bmesh
                bm = bmesh.new(); bm.from_mesh(me)
                kill = [v for v in bm.verts if v.co.z < cut]
                bmesh.ops.delete(bm, geom=kill, context='VERTS')
                bm.to_mesh(me); bm.free()
                lid.rotation_euler = (n.to_track_quat('-Y', 'Z') @ Euler((0, math.radians(tilt), 0)).to_quaternion()).to_euler()
                lid.data.materials.append(mats['lid']); C.apply_xf(lid)
                sol = lid.modifiers.new('thick', 'SOLIDIFY'); sol.thickness = .006
                with bpy.context.temp_override(object=lid, active_object=lid): bpy.ops.object.modifier_apply(modifier='thick')
                self.extras.append(lid)

    def finish(self, S, paint_fn, tris=10000):
        """Body mesh + extras, all painted; returns the single body object."""
        ob = S.to_object(self.name + '_Body')
        mats = self.mats
        order = list(mats.values())
        slot = {k: i for i, k in enumerate(mats)}

        def chooser(Cc, Nn):
            res = paint_fn(Cc, Nn, slot)
            code = self.mouth_region(Cc)[0]
            res[code == 1] = slot['mouth']; res[code == 2] = slot['tongue']; res[code == 3] = slot['eye']
            return res
        C.paint(ob, order, chooser)
        C.decimate(ob, tris)
        return ob


def mk_mats(kid, peel, peel2, lid=None, extra=None):
    m = {
        'peel': C.material(kid.name + '_Peel', peel, .45),
        'peel2': C.material(kid.name + '_PeelAccent', peel2, .5),
        'ink': C.material('Kid_Ink', INK, .4),
        'eye': C.material('Kid_EyeWhite', EYE, .25),
        'glove': C.material('Kid_Glove', GLOVE, .6),
        'limb': C.material(kid.name + '_Limb', kid.limb, .6),
        'shoe': C.material(kid.name + '_Shoe', kid.shoe[0], .5),
        'sole': C.material(kid.name + '_Sole', kid.shoe[1], .6),
        'mouth': C.material('Kid_Mouth', MOUTH, .5),
        'lid': C.material(kid.name + '_Lid', lid or peel, .45),
        'tongue': C.material('Kid_Tongue', TONGUE, .45),
    }
    for k, v in (extra or {}).items(): m[k] = C.material(kid.name + '_' + k.capitalize(), v[0], v[1])
    kid.mats = m
    return m


def limb_paint(kid, Cc, Nn, slot):
    """Shared: gloves, sneakers (with a toe-cap and sole), noodle limbs. Returns slot or -1."""
    out = np.full(len(Cc), -1)
    J = kid.J.j
    for s in 'LR':
        hc = kid.hand_c[s]
        g = np.linalg.norm(Cc - np.asarray(hc), axis=1) < .085
        out[g] = slot['glove']
        a = np.asarray(J[s]['ankle'])
        shoe = (np.linalg.norm((Cc - (a + (0, -.02, -.02))) / (1.0, 1.4, 1.0), axis=1) < .1) & (Cc[:, 2] < .12)
        out[shoe] = slot['shoe']
        sole = shoe & (Cc[:, 2] < .022)
        out[sole] = slot['sole']
        toe = shoe & (Cc[:, 1] < a[1] - .085) & (Cc[:, 2] < .06)
        out[toe] = slot['sole']
        for seg in (('shoulder', 'elbow'), ('elbow', 'wrist'), ('hip', 'knee'), ('knee', 'ankle')):
            a0, b0 = np.asarray(J[s][seg[0]]), np.asarray(J[s][seg[1]])
            if seg[0] in ('shoulder', 'hip'): a0 = a0 + (b0 - a0) * .45   # the limb starts where it leaves the fruit
            d = R.seg_dist(Cc, a0, b0)
            out[(d < .05) & (out < 0)] = slot['limb']
    return out


def outside_body(kid, Cc):
    return np.linalg.norm((Cc - np.asarray(kid.bc)) / np.asarray(kid.body_scale), axis=1) > kid.br * 1.08


def mouth_paint(kid, S, ob, slot):
    """Faces whose verts were pressed deep enough (S.carve) become mouth."""
    return None


# ================================================================== the five fruits
def orange():
    k = Kid('Orange', (0, 0, .5), .3, ('#3FA9F5', '#FFFFFF', '#FFD23F'), limb='#4A3A5C',
            persona=dict(swagger=.8, bounce=1.1, arm=1.1))
    k.body_scale = (1.0, .97, .93)
    F = k.field
    F.blob(k.bc, .3, k.body_scale)
    F.blob(k.bc + Vector((0, -.04, -.12)), .2, (1.1, 1, .8))          # belly mass
    k.limbs(shoulder_z=.46, shoulder_x=.25, hip_x=.09, arm_out=.17, arm_len=.24)
    S = F.solid(.0095)
    S.smooth(3)
    S.crease(Vector((0, 0, .79)), .05, .03)                            # navel at the top where the stem sits
    S.lumps(14, .07, .004, seed=11, region=S.V[:, 2] > .3)
    k.face(S, eye_sep=.1, eye_z=.08, eye_r=.064, look=(.15, .1), lid=.28, lid_tilt=-12, brow=.025,
           smile=(.11, .05, -.06), grin_depth=.032, cheeks=.03)
    peel = (np.linalg.norm(S.V - np.asarray(k.bc), axis=1) > .2) & (S.V[:, 2] > .3)
    S.noise(.0035, 95, seed=2, mask=peel.astype(float), waves=14)      # pitted orange peel
    S.thumbprints(10, .04, .004, seed=7, region=peel)
    S.smooth(1, lam=.3, mu=-.31)
    mats = mk_mats(k, '#FF9A1F', '#FFC233', lid='#FF8A1A', extra={'leaf': ('#3DCB54', .5), 'stem': ('#7A4E2D', .6)})
    k.build_eyes(S)

    def p(Cc, Nn, slot):
        out = limb_paint(k, Cc, Nn, slot)
        body = out < 0
        res = np.where(body, slot['peel'], out)
        # sunny highlight band on the upper half, like a cel-painted shine
        shine = body & (Nn[:, 2] > .8) & (Nn[:, 1] < .2) & (Cc[:, 2] > .66)
        res[shine] = slot['peel2']
        return res
    body = k.finish(S, p)
    # stem + leaf, rolled from clay
    st = C.Field('OrangeStem', .006); st.chain([(0, 0, .77), (.005, 0, .83), (.02, .005, .86)], .018, .013)
    stem = st.solid(.006).to_object('Orange_Stem'); stem.data.materials.append(mats['stem'])
    lf = C.Field('OrangeLeaf', .006)
    lf.blob((.09, .01, .87), .07, (1.2, .55, .16), rot=Euler((0, math.radians(-18), math.radians(12))))
    leaf = lf.solid(.0065)
    leaf.crease(Vector((.09, .01, .885)), .07, .006, pinch=.05, scale=(1.4, .25, 1))   # vein
    leaf.smooth(2)
    lo = leaf.to_object('Orange_Leaf'); lo.data.materials.append(mats['leaf'])
    k.extras += [stem, lo]
    return k, body


def apple():
    k = Kid('Apple', (0, 0, .52), .3, ('#FF4F7B', '#FFFFFF', '#FFE45C'), limb='#3D2E52',
            persona=dict(swagger=.55, bounce=1.0, arm=1.0))
    k.body_scale = (1.04, 1.0, .9)
    F = k.field
    F.blob(k.bc, .3, k.body_scale)
    for x in (-.11, .11): F.ball(k.bc + Vector((x, 0, .1)), .2)           # the two shoulders of an apple
    F.blob(k.bc + Vector((0, 0, -.15)), .2, (1.15, 1.05, .7))
    k.limbs(shoulder_z=.47, shoulder_x=.27, arm_out=.11, arm_len=.24)
    S = F.solid(.0095)
    S.smooth(3)
    S.crease(Vector((0, 0, .8)), .09, .06, pinch=.2)                    # deep top dimple
    S.crease(Vector((0, 0, .26)), .06, .02)                              # little bottom dimple
    S.lumps(10, .08, .007, seed=4, region=S.V[:, 2] > .3)
    k.face(S, eye_sep=.105, eye_z=.07, eye_r=.066, look=(0, .05), lid=.12, lid_tilt=8, brow=.02,
           smile=(.12, .06, -.06), grin_depth=.034, cheeks=.035)
    S.thumbprints(12, .045, .004, seed=9, region=S.V[:, 2] > .3)
    S.smooth(1, lam=.3, mu=-.31)
    mats = mk_mats(k, '#FF3B3B', '#FFD54A', lid='#F02E3C', extra={'leaf': ('#46D160', .5), 'stem': ('#7A4E2D', .6)})
    k.build_eyes(S)

    def p(Cc, Nn, slot):
        out = limb_paint(k, Cc, Nn, slot)
        body = out < 0
        res = np.where(body, slot['peel'], out)
        # sun-kissed yellow streaks: wavy vertical bands
        ang = np.arctan2(Cc[:, 0], Cc[:, 1])
        streak = body & (np.sin(ang * 7 + np.sin(Cc[:, 2] * 18) * 1.2) > .82) & (Cc[:, 2] > .4) & (Cc[:, 1] > -.05)
        res[streak] = slot['peel2']
        return res
    body = k.finish(S, p)
    st = C.Field('AppleStem', .006); st.chain([(0, 0, .76), (.0, .0, .86), (.03, .0, .93)], .02, .014)
    stem = st.solid(.006).to_object('Apple_Stem'); stem.data.materials.append(mats['stem'])
    lf = C.Field('AppleLeaf', .006)
    lf.blob((.1, 0, .9), .085, (1.25, .55, .15), rot=Euler((0, math.radians(-25), math.radians(-8))))
    leaf = lf.solid(.0065); leaf.crease(Vector((.1, 0, .915)), .08, .006, pinch=.05, scale=(1.4, .25, 1)); leaf.smooth(2)
    lo = leaf.to_object('Apple_Leaf'); lo.data.materials.append(mats['leaf'])
    k.extras += [stem, lo]
    return k, body


def banana():
    k = Kid('Banana', (0, 0, .58), .2, ('#8E5CFF', '#FFFFFF', '#5CF2C7'), limb='#3A2F55',
            persona=dict(swagger=1.0, bounce=1.25, arm=1.3))
    k.body_scale = (1, 1, 2.2)
    F = k.field
    # the crescent: a rolled snake of clay bent round a circle behind the kid, fat belly, tapering ends
    cen, RAD = Vector((0, .62, .6)), .62
    th = np.linspace(-.64, .64, 13)
    arc = [cen + Vector((0, -math.cos(a), math.sin(a))) * RAD for a in th]
    rad = [.045 + .115 * (1 - (a / .64) ** 2) ** .6 for a in th]
    for i in range(len(arc) - 1):
        F.rope(arc[i], arc[i + 1], (rad[i] + rad[i + 1]) / 2)
    k.limbs(shoulder_z=.5, shoulder_x=.13, hip_x=.07, arm_out=.15, arm_len=.25, leg_top=.27)
    S = F.solid(.009)
    S.smooth(3)
    # five flattened peel faces with ridges between them, pressed by a palette knife along the curve
    for a, P, r in zip(th[1:-1], arc[1:-1], rad[1:-1]):
        T = Vector((0, math.sin(a), math.cos(a)))
        X = Vector((1, 0, 0)); B = T.cross(X)
        for i in range(5):
            ang = math.radians(i * 72)
            n = X * math.sin(ang) + B * math.cos(ang)
            S.flatten(P + n * r * .82, r * 1.1, n, .5, scale=(1, 1, 1))
    S.smooth(2)
    k.face(S, eye_sep=.07, eye_z=.1, eye_r=.056, look=(-.2, .15), lid=.0, brow=.03,
           smile=(.085, .05, -.03), grin_depth=.034, cheeks=.02)
    S.thumbprints(10, .03, .003, seed=13, region=S.V[:, 2] > .3)
    S.smooth(1, lam=.3, mu=-.31)
    mats = mk_mats(k, '#FFE03D', '#FFF27A', extra={'tip': ('#5B3A1E', .6), 'spot': ('#C98B2E', .55)})
    k.build_eyes(S)
    top, bot = arc[-1], arc[0]

    def p(Cc, Nn, slot):
        out = limb_paint(k, Cc, Nn, slot)
        body = out < 0
        res = np.where(body, slot['peel'], out)
        res[body & (Nn[:, 2] > .4) & (Cc[:, 2] > .55)] = slot['peel2']
        rng = np.sin(Cc[:, 0] * 61) * np.sin(Cc[:, 2] * 47 + 1) * np.sin(Cc[:, 1] * 53)
        res[body & (rng > .55) & (Cc[:, 1] > .05)] = slot['spot']           # freckles on the back only
        res[body & (np.linalg.norm(Cc - np.asarray(bot), axis=1) < .06)] = slot['tip']
        return res
    body = k.finish(S, p)
    tan = Vector((0, math.sin(.64), math.cos(.64)))
    st = C.Field('BananaStem', .006); st.chain([top - tan * .02, top + tan * .06, top + tan * .1 + Vector((0, .03, 0))], .032, .024)
    stem = st.solid(.006)
    so = stem.to_object('Banana_Stem'); so.data.materials.append(mats['tip'])
    k.extras.append(so)
    return k, body


def watermelon():
    k = Kid('Watermelon', (0, 0, .5), .36, ('#FF6B35', '#FFFFFF', '#FFE45C'), limb='#2E3550',
            persona=dict(swagger=.35, bounce=.7, heavy=.8, arm=.8))
    k.body_scale = (1.12, 1.0, .86)
    F = k.field
    F.blob(k.bc, .36, k.body_scale)
    F.blob(k.bc + Vector((0, -.05, -.1)), .25, (1.2, 1, .8))
    k.limbs(shoulder_z=.44, shoulder_x=.36, hip_x=.12, arm_out=.12, arm_len=.25)
    S = F.solid(.0105)
    S.smooth(3)
    # sliced top: press it flat, then sculpt a fleshy rim
    S.flatten(Vector((0, 0, .74)), .5, (0, 0, 1), 1.0)
    S.smooth_region(Vector((0, 0, .74)), .3, 3)
    S.lumps(10, .1, .008, seed=21, region=S.V[:, 2] > .3)
    k.face(S, eye_sep=.12, eye_z=.05, eye_r=.058, look=(0, 0), lid=.32, lid_tilt=6, brow=.03,
           smile=(.15, .045, -.09), grin_depth=.036, cheeks=.035)
    S.thumbprints(12, .05, .004, seed=17, region=S.V[:, 2] > .3)
    S.smooth(1, lam=.3, mu=-.31)
    mats = mk_mats(k, '#3DBE4E', '#1E8A3A', lid='#36B048', extra={'flesh': ('#FF4D6A', .4), 'rind': ('#F4FFC9', .5)})
    k.build_eyes(S)

    def p(Cc, Nn, slot):
        out = limb_paint(k, Cc, Nn, slot)
        body = out < 0
        res = np.where(body, slot['peel'], out)
        ang = np.arctan2(Cc[:, 0], Cc[:, 1])
        stripe = np.sin(ang * 8 + np.sin(Cc[:, 2] * 24) * .7) > .35
        res[body & stripe & (Cc[:, 2] > .14)] = slot['peel2']
        top = body & (Cc[:, 2] > .72)
        res[top] = slot['rind']
        res[top & (Nn[:, 2] > .9)] = slot['flesh']
        return res
    body = k.finish(S, p, tris=10000)
    # seeds on the flesh: little teardrops
    rng = np.random.default_rng(3)
    for i in range(9):
        a = rng.uniform(0, 6.283); r = rng.uniform(.04, .2)
        sd = C.uv_sphere(f'Watermelon_Seed{i}', .018, (math.cos(a) * r * 1.1, math.sin(a) * r, .742), (1, .6, .35), 10, 6)
        sd.rotation_euler = (0, 0, a); sd.data.materials.append(mats['ink']); C.apply_xf(sd)
        k.extras.append(sd)
    return k, body


def waxapple():
    k = Kid('WaxApple', (0, 0, .52), .27, ('#FFD23F', '#FFFFFF', '#FF5FA2'), limb='#4B2C4F',
            persona=dict(swagger=.9, bounce=1.0, arm=1.0))
    k.body_scale = (1, 1, 1.1)
    F = k.field
    # bell: narrow shoulders, wide skirt flaring into four lobes
    F.blob(k.bc + Vector((0, 0, .14)), .19, (1, 1, 1.05))
    F.blob(k.bc + Vector((0, 0, -.04)), .27, (1, 1, .85))
    for i in range(4):
        a = math.radians(45 + i * 90)
        F.ball(k.bc + Vector((math.sin(a) * .2, math.cos(a) * .2, -.17)), .12)
    k.limbs(shoulder_z=.5, shoulder_x=.22, hip_x=.085, arm_out=.12, arm_len=.24, leg_top=.25)
    S = F.solid(.0095)
    S.smooth(3)
    for i in range(4):                                                    # grooves between the lobes
        a = math.radians(i * 90)
        n = Vector((math.sin(a), math.cos(a), 0))
        S.stroke([k.bc + n * .3 + Vector((0, 0, z)) for z in (-.2, -.08, .05)], .04, S.crease, .02, pinch=.15)
    S.crease(Vector((0, 0, .79)), .05, .03)
    k.face(S, eye_sep=.095, eye_z=.1, eye_r=.06, look=(.25, .05), lid=.3, lid_tilt=-15, brow=.02,
           smile=(.085, .02, -.03), grin_depth=.028, cheeks=.03, squint=(.0, .25))
    S.thumbprints(10, .04, .004, seed=19, region=S.V[:, 2] > .3)
    S.smooth(1, lam=.3, mu=-.31)
    mats = mk_mats(k, '#FF4F8B', '#FFA3C4', lid='#F23E7E', extra={'crown': ('#7FD957', .5)})
    k.build_eyes(S)

    def p(Cc, Nn, slot):
        out = limb_paint(k, Cc, Nn, slot)
        body = out < 0
        res = np.where(body, slot['peel'], out)
        res[body & (Nn[:, 2] > .45) & (Cc[:, 2] > .62)] = slot['peel2']    # glossy waxy shine
        return res
    body = k.finish(S, p)
    # little sepal crown on top
    cf = C.Field('WaxCrown', .006)
    for i in range(4):
        a = math.radians(i * 90 + 20)
        cf.rope((0, 0, .78), (math.sin(a) * .06, math.cos(a) * .06, .83), .016)
    cr = cf.solid(.006).to_object('WaxApple_Crown'); cr.data.materials.append(mats['crown'])
    k.extras.append(cr)
    return k, body


def mouth_from_carve(k, S, body):
    """Paint the pressed smile dark, with a tongue in its lower middle."""
    me = body.data
    slots = [m.name for m in me.materials]
    mi = slots.index(k.mats['mouth'].name)
    if k.mats.get('tongue') is None:
        k.mats['tongue'] = C.material('Kid_Tongue', TONGUE, .45)
        me.materials.append(k.mats['tongue'])
    ti = [m.name for m in me.materials].index(k.mats['tongue'].name)
    pts = np.asarray([np.asarray(p) for p in k.mouth_pts])
    n = len(me.polygons)
    Cc = np.empty(n * 3); me.polygons.foreach_get('center', Cc); Cc = Cc.reshape(-1, 3)
    # distance to the smile polyline
    d = np.full(n, 9.0)
    for a, b in zip(pts[:-1], pts[1:]): d = np.minimum(d, R.seg_dist(Cc, a, b))
    w = np.linalg.norm(pts[-1] - pts[0])
    mid = (pts[len(pts) // 2] + pts[0] * 0)
    near_mid = np.abs(Cc[:, 0]) < w * .32
    idx = np.empty(n, int); me.polygons.foreach_get('material_index', idx)
    mouth = (d < .022 + .012 * near_mid) & (Cc[:, 1] < k.bc.y - k.br * .4)
    idx[mouth] = mi
    tongue = mouth & near_mid & (Cc[:, 2] < mid[2] - .004)
    idx[tongue] = ti
    me.polygons.foreach_set('material_index', idx)


BUILDERS = {'Apple': apple, 'Banana': banana, 'Orange': orange, 'Watermelon': watermelon, 'WaxApple': waxapple}


# ================================================================== rig, animate, export, render
def assemble(k, body):
    # normalise height to the game's kid height (feet on the floor)
    objs = [body] + k.extras
    zmax = max(max((o.matrix_world @ v.co).z for v in o.data.vertices) for o in objs)
    s = HEIGHT / zmax
    for o in objs:
        o.data.transform(Matrix.Scale(s, 4))
    J = k.J
    J.pelvis *= s; J.chest *= s; J.face *= s
    for side in J.j.values():
        for key in side: side[key] = side[key] * s
    k.scale = s
    rig, Rm, L = R.build_armature(k.name, J)
    R.skin(body, rig, J, k.br * s * .9)
    for e in k.extras:
        R.skin(e, rig, J, 0, rigid='Body')
    # one mesh for the game: body + face parts
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    body.name = k.name + '_Mesh'; body.data.name = k.name + '_Mesh'
    acts = []
    for name, frames, fn, loop in R.moves(k.persona):
        acts.append(R.make_action(rig, J, Rm, L, f'{k.name}_{name}', frames, fn, loop))
    for pb in rig.pose.bones: pb.matrix_basis = Matrix.Identity(4)   # back to the rest pose
    return rig, body, acts


def export(k, rig, body):
    d = f'{OUT}/{k.name}'
    os.makedirs(d, exist_ok=True); os.makedirs(SRC, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path = f'{d}/{k.name}_Sculpt.fbx'
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'},
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True,
        add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
        use_armature_deform_only=False, mesh_smooth_type='FACE',
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0, bake_anim_step=1)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=f'{SRC}/{k.name}_Sculpt.blend', copy=True)
    return path


def render(k, rig, tag='turn', frames=None, action=None):
    os.makedirs(EVID, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'
    sh.show_cavity = True; sh.cavity_type = 'BOTH'; sh.curvature_ridge_factor = 1.2; sh.curvature_valley_factor = 1.4
    sh.show_object_outline = True; sh.object_outline_color = (0.07, 0.06, 0.12)
    sh.show_shadows = True
    sc.display.render_aa = '8'
    sc.render.resolution_x = 760; sc.render.resolution_y = 760
    sc.render.film_transparent = False
    w = bpy.data.worlds.new('w'); sc.world = w; w.color = (1.0, .93, .78)
    cam = bpy.data.objects.get('PreviewCam')
    if not cam:
        cam = bpy.data.objects.new('PreviewCam', bpy.data.cameras.new('PreviewCam')); sc.collection.objects.link(cam)
        cam.data.lens = 60
    sc.camera = cam
    if action:
        rig.animation_data.action = bpy.data.actions[action]
    shots = []
    for i, yaw in enumerate((-25, 0, 90, 180)):
        a = math.radians(yaw)
        cam.location = (math.sin(a) * 3.0, -math.cos(a) * 3.0, 1.0)
        cam.rotation_euler = (math.radians(84), 0, a)
        sc.frame_set(frames[0] if frames else 1)
        p = f'{EVID}/{k.name}_{tag}_{i}.png'
        sc.render.filepath = p; bpy.ops.render.render(write_still=True)
        shots.append(p)
    if action: rig.animation_data.action = None
    return shots


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    names = [a for a in argv if not a.startswith('--')] or list(BUILDERS)
    report = {}
    for n in names:
        C.reset()
        k, body = BUILDERS[n]()
        rig, mesh, acts = assemble(k, body)
        tris = C.tri_count(mesh)
        render(k, rig)
        if '--preview-only' not in argv:
            path = export(k, rig, mesh)
            for a in ('Punch', 'Jump', 'Taunt'):
                act = f'{k.name}_{a}'
                fr = {'Punch': 8, 'Jump': 13, 'Taunt': 12}[a]
                sc = bpy.context.scene
                rig.animation_data.action = bpy.data.actions[act]; sc.frame_set(fr)
                cam = bpy.data.objects['PreviewCam']; cam.location = (-1.3, -2.7, 1.0); cam.rotation_euler = (math.radians(84), 0, math.radians(-26))
                sc.render.filepath = f'{EVID}/{k.name}_pose_{a}.png'; bpy.ops.render.render(write_still=True)
                rig.animation_data.action = None
            report[n] = {'fbx': os.path.relpath(path, ROOT), 'triangles': tris,
                         'materials': [m.name for m in mesh.data.materials],
                         'actions': [a.name for a in acts], 'scale': k.scale}
        print('KID_DONE', n, tris)
    if report:
        os.makedirs(SRC, exist_ok=True)
        old = {}
        rp = f'{SRC}/kids-report.json'
        if os.path.exists(rp): old = json.load(open(rp))
        old.update(report)
        json.dump(old, open(rp, 'w'), indent=2)


main()
