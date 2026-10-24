"""Fruit-kid rig + procedural keyframed animation.

Bone layout matches the game's other fruit rigs (see build_banana_rig.py and KidActionAnimator):

  Root - Body - Face (marker)
       - Arm_L, Arm_L_Lower - Hand_L          upper/lower limb bones are siblings under Root
       - Arm_R, Arm_R_Lower - Hand_R
       - Leg_L, Leg_L_Lower, Foot_L
       - Leg_R, Leg_R_Lower, Foot_R

The character faces Blender -Y; its right side is -X. Animation is authored as intent
(body offset/tilt/squash, hand and foot targets) and solved with 2-bone IK every frame,
then written as plain FK keys so Unity plays it as an ordinary Generic clip.
"""
import bpy
import math
import numpy as np
from mathutils import Vector, Matrix, Euler

ORDER = ['Root', 'Body', 'Face', 'Arm_L', 'Arm_L_Lower', 'Hand_L', 'Arm_R', 'Arm_R_Lower', 'Hand_R',
         'Leg_L', 'Leg_L_Lower', 'Foot_L', 'Leg_R', 'Leg_R_Lower', 'Foot_R']
FWD = Vector((0, -1, 0)); UP = Vector((0, 0, 1))
SIDE = {'L': Vector((1, 0, 0)), 'R': Vector((-1, 0, 0))}


def parent_of(b):
    if b == 'Root': return None
    if b == 'Face': return 'Body'
    if b.startswith('Hand_'): return 'Arm_' + b[-1] + '_Lower'
    return 'Root'


def frame3(y, x_hint):
    y = y.normalized()
    x = x_hint - y * x_hint.dot(y)
    if x.length < 1e-5: x = UP.cross(y)
    x.normalize()
    z = x.cross(y)
    return Matrix((x, y, z)).transposed()


def mat(r3, head):
    m = r3.to_4x4(); m.translation = head; return m


class Joints:
    """Rest-pose joint positions (metres, Blender space) for one kid."""

    def __init__(self, pelvis, chest, face, shoulder, elbow, wrist, hand, hip, knee, ankle, toe):
        self.pelvis, self.chest, self.face = Vector(pelvis), Vector(chest), Vector(face)
        self.j = {}
        for s, k in (('L', 1), ('R', -1)):
            mir = lambda p: Vector((p[0] * k, p[1], p[2]))
            self.j[s] = dict(shoulder=mir(shoulder), elbow=mir(elbow), wrist=mir(wrist), hand=mir(hand),
                             hip=mir(hip), knee=mir(knee), ankle=mir(ankle), toe=mir(toe))

    def rest(self):
        R = {'Root': Matrix.Identity(4)}
        R['Body'] = mat(frame3(self.chest - self.pelvis, Vector((1, 0, 0))), self.pelvis)
        R['Face'] = mat(frame3(FWD, Vector((1, 0, 0))), self.face)
        L = {'Root': .1, 'Body': (self.chest - self.pelvis).length, 'Face': .06}
        for s in 'LR':
            j = self.j[s]
            R['Arm_' + s] = mat(frame3(j['elbow'] - j['shoulder'], SIDE[s]), j['shoulder'])
            R['Arm_' + s + '_Lower'] = mat(frame3(j['wrist'] - j['elbow'], SIDE[s]), j['elbow'])
            R['Hand_' + s] = mat(frame3(j['hand'] - j['wrist'], SIDE[s]), j['wrist'])
            R['Leg_' + s] = mat(frame3(j['knee'] - j['hip'], FWD), j['hip'])
            R['Leg_' + s + '_Lower'] = mat(frame3(j['ankle'] - j['knee'], FWD), j['knee'])
            R['Foot_' + s] = mat(frame3(j['toe'] - j['ankle'], UP), j['ankle'])
            L['Arm_' + s] = (j['elbow'] - j['shoulder']).length
            L['Arm_' + s + '_Lower'] = (j['wrist'] - j['elbow']).length
            L['Hand_' + s] = (j['hand'] - j['wrist']).length
            L['Leg_' + s] = (j['knee'] - j['hip']).length
            L['Leg_' + s + '_Lower'] = (j['ankle'] - j['knee']).length
            L['Foot_' + s] = (j['toe'] - j['ankle']).length
        return R, L

    def capsules(self, body_r):
        """Influence capsules (a, b, radius) used for automatic skin weights."""
        c = {'Body': (self.pelvis, self.chest, body_r)}
        for s in 'LR':
            j = self.j[s]
            c['Arm_' + s] = (j['shoulder'], j['elbow'], .02)
            c['Arm_' + s + '_Lower'] = (j['elbow'], j['wrist'], .02)
            c['Hand_' + s] = (j['wrist'], j['hand'] + (j['hand'] - j['wrist']) * .4, .05)
            c['Leg_' + s] = (j['hip'], j['knee'], .02)
            c['Leg_' + s + '_Lower'] = (j['knee'], j['ankle'], .02)
            c['Foot_' + s] = (j['ankle'], j['toe'], .045)
        return c


def build_armature(name, joints):
    R, L = joints.rest()
    data = bpy.data.armatures.new(name + '_Rig')
    rig = bpy.data.objects.new(name + '_Rig', data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for b in ORDER:
        eb = data.edit_bones.new(b)
        eb.head = (0, 0, 0); eb.tail = (0, max(.04, L[b]), 0)
        eb.matrix = R[b]
        eb.length = max(.04, L[b])
    for b in ORDER:
        if parent_of(b): data.edit_bones[b].parent = data.edit_bones[parent_of(b)]
    bpy.ops.object.mode_set(mode='OBJECT')
    for pb in rig.pose.bones: pb.rotation_mode = 'QUATERNION'
    rig['rest_lengths'] = {k: float(v) for k, v in L.items()}
    return rig, R, L


def seg_dist(P, a, b):
    a = np.asarray(a); b = np.asarray(b); ab = b - a
    t = np.clip(((P - a) @ ab) / max(1e-9, ab @ ab), 0, 1)
    return np.linalg.norm(P - (a + np.outer(t, ab)), axis=1)


def skin(ob, rig, joints, body_r, rigid=None):
    """Smooth automatic weights from influence capsules (top-2 blend), or rigid to one bone."""
    for b in ORDER: ob.vertex_groups.new(name=b)
    n = len(ob.data.vertices)
    if rigid:
        ob.vertex_groups[rigid].add(list(range(n)), 1.0, 'REPLACE')
    else:
        P = np.empty(n * 3); ob.data.vertices.foreach_get('co', P); P = P.reshape(-1, 3)
        caps = joints.capsules(body_r)
        names = list(caps)
        E = np.stack([np.maximum(0, seg_dist(P, a, b) - r) for a, b, r in caps.values()], 1)
        W = 1.0 / (E + 0.012) ** 4
        top = np.argsort(-W, axis=1)[:, :2]
        for i in range(n):
            w0, w1 = W[i, top[i, 0]], W[i, top[i, 1]]
            s = w0 + w1
            ob.vertex_groups[names[top[i, 0]]].add([i], w0 / s, 'REPLACE')
            if w1 / s > 0.02: ob.vertex_groups[names[top[i, 1]]].add([i], w1 / s, 'REPLACE')
    m = ob.modifiers.new('Armature', 'ARMATURE'); m.object = rig
    ob.parent = rig


# ------------------------------------------------------------------ posing
def ik2(root, target, l1, l2, pole):
    d = target - root
    dist = min(max(d.length, 1e-4), (l1 + l2) * 0.999)
    dn = d.normalized()
    a = (l1 * l1 + dist * dist - l2 * l2) / (2 * l1 * dist)
    a = max(-1, min(1, a))
    h = math.sqrt(max(0, 1 - a * a)) * l1
    p = pole - dn * pole.dot(dn)
    p = p.normalized() if p.length > 1e-5 else UP.cross(dn).normalized()
    mid = root + dn * (a * l1) + p * h
    end = root + dn * dist
    return mid, end


class Pose:
    """Intent for one frame. Targets are in the kid's body space (moved with the body)."""

    def __init__(self, **kw):
        self.off = Vector(kw.get('off', (0, 0, 0)))       # body offset (m)
        self.tilt = Vector(kw.get('tilt', (0, 0, 0)))     # body rotation (deg) about pelvis: x=lean fwd(-), y=side, z=twist
        self.sq = kw.get('sq', 0.0)                       # +squash / -stretch
        self.hands = kw.get('hands', {})                  # side -> offset from rest wrist (body space)
        self.feet = kw.get('feet', {})                    # side -> offset from rest ankle (world, planted)
        self.foot_pitch = kw.get('foot_pitch', {})        # side -> deg
        self.hand_turn = kw.get('hand_turn', {})          # side -> deg extra wrist roll


def solve(joints, R, L, pose):
    W = {'Root': Matrix.Identity(4)}
    rot = Euler([math.radians(a) for a in pose.tilt], 'XYZ').to_matrix().to_4x4()
    piv = joints.pelvis
    sz = 1 + pose.sq; sxy = 1 / math.sqrt(max(.2, sz))
    S = Matrix.Diagonal((sxy, sxy, sz, 1))
    body = Matrix.Translation(piv + pose.off) @ rot @ S @ Matrix.Translation(-piv)
    rigid = Matrix.Translation(piv + pose.off) @ rot @ Matrix.Translation(-piv)
    W['Body'] = body @ R['Body']
    W['Face'] = body @ R['Face']
    for s in 'LR':
        j = joints.j[s]
        # arms ride on the squashing body
        sh = body @ j['shoulder']
        wrist = rigid @ (j['wrist'] + Vector(pose.hands.get(s, (0, 0, 0))))
        pole = rigid.to_3x3() @ (SIDE[s] * .3 + FWD * -.6 + UP * -.4)
        el, wr = ik2(sh, wrist, L['Arm_' + s], L['Arm_' + s + '_Lower'], pole)
        W['Arm_' + s] = seg(R['Arm_' + s], j['shoulder'], j['elbow'], sh, el)
        W['Arm_' + s + '_Lower'] = seg(R['Arm_' + s + '_Lower'], j['elbow'], j['wrist'], el, wr)
        lower_sw = W['Arm_' + s + '_Lower'].to_3x3() @ R['Arm_' + s + '_Lower'].to_3x3().inverted()
        turn = Matrix.Rotation(math.radians(pose.hand_turn.get(s, 0)), 3, (wr - el).normalized())
        W['Hand_' + s] = mat(turn @ lower_sw @ R['Hand_' + s].to_3x3(), wr)
        # legs: hips ride with the body, feet stay where they are planted
        hip = rigid @ j['hip']
        ank = j['ankle'] + Vector(pose.feet.get(s, (0, 0, 0)))
        kn, an = ik2(hip, ank, L['Leg_' + s], L['Leg_' + s + '_Lower'], FWD + UP * .1)
        W['Leg_' + s] = seg(R['Leg_' + s], j['hip'], j['knee'], hip, kn)
        W['Leg_' + s + '_Lower'] = seg(R['Leg_' + s + '_Lower'], j['knee'], j['ankle'], kn, an)
        pitch = Matrix.Rotation(math.radians(pose.foot_pitch.get(s, 0)), 3, SIDE['L'])
        W['Foot_' + s] = mat(pitch @ R['Foot_' + s].to_3x3(), an)
    return W


def seg(rest, h0, t0, h1, t1):
    sw = (t0 - h0).normalized().rotation_difference((t1 - h1).normalized()).to_matrix()
    return mat(sw @ rest.to_3x3(), h1)


def key_pose(rig, W, f):
    for b in ORDER:
        bone = rig.data.bones[b]
        p = parent_of(b)
        if p:
            prest = rig.data.bones[p].matrix_local
            basis = (prest.inverted() @ bone.matrix_local).inverted() @ W[p].inverted() @ W[b]
        else:
            basis = bone.matrix_local.inverted() @ W[b]
        pb = rig.pose.bones[b]
        pb.matrix_basis = basis
        for path in ('location', 'rotation_quaternion', 'scale'): pb.keyframe_insert(path, frame=f)


def make_action(rig, joints, R, L, name, frames, pose_fn, loop=True):
    """pose_fn(t in [0,1)) -> Pose. Last key repeats the first for seamless loops."""
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data_create(); rig.animation_data.action = act
    last = frames if loop else frames - 1
    for f in range(0, last + 1):
        t = (f % frames) / frames if loop else f / (frames - 1)
        key_pose(rig, solve(joints, R, L, pose_fn(t)), f + 1)
    act.frame_range = (1, last + 1)
    # keep it in an NLA track so the FBX exporter treats every action as its own take
    tr = rig.animation_data.nla_tracks.new(); tr.name = name
    tr.strips.new(name, 1, act); tr.mute = True
    rig.animation_data.action = None
    return act


# ------------------------------------------------------------------ the move set
def ease(t): return t * t * (3 - 2 * t)
def back(t, s=1.7): t -= 1; return t * t * ((s + 1) * t + s) + 1
def pulse(t, a, b): return 0.0 if t < a or t > b else math.sin((t - a) / (b - a) * math.pi)
def lerp(a, b, t): return a + (b - a) * t
def keys(t, pts):
    """Piecewise eased curve through (time, value) points."""
    for (t0, v0), (t1, v1) in zip(pts[:-1], pts[1:]):
        if t <= t1:
            u = 0 if t1 == t0 else (t - t0) / (t1 - t0)
            if isinstance(v0, tuple): return tuple(lerp(a, b, ease(u)) for a, b in zip(v0, v1))
            return lerp(v0, v1, ease(u))
    return pts[-1][1]


def moves(persona):
    """Persona tweaks the shared move set so each fruit moves like itself.
    persona: swagger (0..1 hip sway), bounce (vertical energy), heavy (slow, low), arm (arm swing)."""
    sw, bo, hv, am = persona.get('swagger', .5), persona.get('bounce', 1.), persona.get('heavy', 0.), persona.get('arm', 1.)
    T2 = 2 * math.pi

    def idle(t):
        a = T2 * t
        breath = math.sin(a) * .035 * (1 - hv * .3)
        return Pose(off=(math.sin(a) * .006 * sw, 0, -abs(math.sin(a)) * .006),
                    tilt=(-2 + math.sin(a) * 1.5, math.sin(a) * 2.5 * sw, math.sin(a * .5 + 1) * 4 * sw),
                    sq=breath,
                    hands={'L': (math.sin(a + 1) * .012, -.01, .015 + math.sin(a) * .012),
                           'R': (-math.sin(a + 1) * .012, -.01, .015 + math.sin(a + .6) * .012)})

    def walk(t):
        a = T2 * t
        step = abs(math.sin(a))
        return Pose(off=(math.sin(a) * .018 * sw, 0, step * .03 * bo - .01),
                    tilt=(-7, math.sin(a) * 5 * sw, math.sin(a) * 10 * sw),
                    sq=(.5 - step) * .08 * bo,
                    hands={'L': (0, math.sin(a) * .09 * am, .03 + max(0, -math.sin(a)) * .03),
                           'R': (0, -math.sin(a) * .09 * am, .03 + max(0, math.sin(a)) * .03)},
                    feet={'L': (0, -math.cos(a) * .07, max(0, math.sin(a)) * .05),
                          'R': (0, math.cos(a) * .07, max(0, -math.sin(a)) * .05)},
                    foot_pitch={'L': max(0, math.sin(a)) * 25, 'R': max(0, -math.sin(a)) * 25})

    def run(t):
        a = T2 * t
        step = abs(math.sin(a))
        return Pose(off=(math.sin(a) * .02 * sw, -.02, step * .06 * bo - .02),
                    tilt=(-17, math.sin(a) * 6 * sw, math.sin(a) * 16),
                    sq=(.5 - step) * .16 * bo,
                    hands={'L': (-.03, math.sin(a) * .16 * am, .09 + math.sin(a) * .05),
                           'R': (.03, -math.sin(a) * .16 * am, .09 - math.sin(a) * .05)},
                    feet={'L': (0, -math.cos(a) * .12, max(0, math.sin(a)) * .1),
                          'R': (0, math.cos(a) * .12, max(0, -math.sin(a)) * .1)},
                    foot_pitch={'L': math.sin(a) * 30, 'R': -math.sin(a) * 30})

    def jump(t):
        # anticipation -> launch stretch -> tuck -> fall -> land squash -> settle
        z = keys(t, [(0, 0), (.2, -.07), (.32, .16), (.55, .3), (.78, .1), (.84, -.06), (1, 0)])
        sq = keys(t, [(0, 0), (.2, .32), (.3, -.28), (.5, .05), (.78, -.1), (.84, .35), (1, 0)])
        arms = keys(t, [(0, 0), (.2, -.06), (.32, .3), (.55, .18), (.8, .22), (.86, -.02), (1, 0)])
        tuck = keys(t, [(0, 0), (.32, 0), (.5, .08), (.75, .02), (1, 0)])
        return Pose(off=(0, 0, z), tilt=(keys(t, [(0, 0), (.2, -12), (.35, 8), (.84, -10), (1, 0)]), 0, 0), sq=sq,
                    hands={'L': (.04, .02, arms), 'R': (-.04, .02, arms)},
                    feet={'L': (0, 0, max(0, z) + tuck), 'R': (0, 0, max(0, z) + tuck)},
                    foot_pitch={'L': -tuck * 300, 'R': -tuck * 300})

    def punch(t):
        # wind-up twist, snap the right glove forward, overshoot, recover
        twist = keys(t, [(0, 0), (.25, 22), (.4, -30), (.55, -26), (1, 0)])
        reach = keys(t, [(0, 0), (.25, -.04), (.38, .36), (.55, .34), (1, 0)])
        sq = keys(t, [(0, 0), (.25, .12), (.38, -.14), (.6, .02), (1, 0)])
        return Pose(off=(0, keys(t, [(0, 0), (.25, .03), (.4, -.06), (1, 0)]), 0), tilt=(keys(t, [(0, 0), (.25, 4), (.4, -14), (1, 0)]), 0, twist), sq=sq,
                    hands={'R': (.08 * min(1, reach * 4), -reach, .14 * min(1, abs(reach) * 4)),
                           'L': (-.02, .05, .12 * min(1, abs(reach) * 3))},
                    feet={'R': (0, keys(t, [(0, 0), (.3, .02), (.45, -.06), (1, 0)]), 0)},
                    hand_turn={'R': twist * .8})

    def taunt(t):
        # cheeky booty-wiggle + "come at me" hand waves: made for pranking friends
        a = T2 * t * 2
        return Pose(off=(math.sin(a) * .03, 0, abs(math.sin(a)) * .02),
                    tilt=(4, math.sin(a) * 9, math.sin(a) * 14 + 30 * math.sin(T2 * t)),
                    sq=math.sin(a * 2) * .06,
                    hands={'L': (.03, -.06, .22 + math.sin(a * 2) * .05), 'R': (-.03, -.06, .22 - math.sin(a * 2) * .05)},
                    hand_turn={'L': math.sin(a * 2) * 50, 'R': -math.sin(a * 2) * 50})

    def hit(t):
        # bonked: recoil stretch, wobble like jelly, settle
        wob = math.sin(t * 26) * (1 - t) ** 2
        return Pose(off=(0, keys(t, [(0, 0), (.12, .06), (1, 0)]), 0), tilt=(keys(t, [(0, 0), (.12, 18), (1, 0)]) + wob * 6, wob * 8, 0),
                    sq=keys(t, [(0, 0), (.08, -.25), (.2, .25), (1, 0)]) + wob * .12,
                    hands={'L': (.05, .06, .25 * (1 - t)), 'R': (-.05, .06, .25 * (1 - t))})

    def cheer(t):
        a = T2 * t * 2
        hop = abs(math.sin(a)) * .08 * bo
        return Pose(off=(0, 0, hop), sq=(.04 - hop) * 1.4, tilt=(0, math.sin(a / 2) * 6, 0),
                    hands={'L': (.02, 0, .38 + math.sin(a) * .03), 'R': (-.02, 0, .38 - math.sin(a) * .03)},
                    feet={'L': (0, 0, hop * .7), 'R': (0, 0, hop * .7)})

    return [('Idle', 48, idle, True), ('Walk', 40, walk, True), ('Run', 24, run, True), ('Jump', 30, jump, False),
            ('Punch', 18, punch, False), ('Taunt', 48, taunt, True), ('Hit', 30, hit, False), ('Cheer', 32, cheer, True)]
