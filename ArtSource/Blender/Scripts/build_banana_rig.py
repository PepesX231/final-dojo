"""Build a skinned, bone-animated Unity copy of the supplied Banana sibling.

The supplied Blender 5.2 file animates its noodle arms/legs with 40 absolute shape keys
and moves gloves/sneakers/body with empties. Unity cannot drive the hands of that setup,
so this script derives an armature from the authored motion:

  Root ── Body ── Face (marker)          rigid peel, face, stem, shoulder puffs
       ├─ Arm_L / Arm_L_Lower ── Hand_L   noodle tube skinned upper/lower, glove on Hand
       ├─ Arm_R / Arm_R_Lower ── Hand_R
       ├─ Leg_L / Leg_L_Lower, Foot_L     noodle tube skinned upper/lower, sneaker on Foot
       └─ Leg_R / Leg_R_Lower, Foot_R

Upper and lower limb bones are siblings under Root, matching the apple kid's transform
layout that KidActionAnimator drives. Elbow/knee positions come from the authored tube's
centreline every frame. Bind pose is frame 11 (both gloves level, arms hanging).

Never overwrites the supplied source. Run:
  blender -b -P ArtSource/Blender/Scripts/build_banana_rig.py
or with the `bpy` 5.2 Python module:  python build_banana_rig.py
"""
import bpy, os, sys, json, math
from mathutils import Vector, Matrix, Quaternion

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../..'))
SRC = ROOT + '/ArtSource/Blender/ProvidedFamily/Banana_Sibling_v5_GoofyWalk.blend'
OUT_DIR = ROOT + '/Assets/MamasComing/Art/Family/Banana'
OUT_FBX = OUT_DIR + '/Banana_Rigged.fbx'
OUT_BLEND = ROOT + '/ArtSource/Blender/MamaRecast/Banana_Rigged.blend'
REPORT = ROOT + '/ArtSource/Blender/MamaRecast/banana-rig.json'
HEIGHT = 1.08          # metres, same as MamaFruitPrefabs.Height
BIND = 11              # bind/rest frame
FIRST, LAST = 1, 41    # 41 repeats 1 for a seamless loop
RINGS, RING = 25, 14   # noodle tube topology: 25 rings of 14 verts + 2 caps
ELBOW_RING = 12

bpy.ops.wm.open_mainfile(filepath=SRC, load_ui=False, use_scripts=False)
scene = bpy.context.scene
O = bpy.data.objects
root = next(o for o in O if o.name.startswith('BANANA'))


def belongs(o):
    q = o
    while q:
        if q == root: return True
        q = q.parent
    return False


members = [o for o in O if belongs(o)]
# The source names sides as seen from the front camera: its "L" parts sit at -X while the banana
# faces -Y, which is the character's own right. Bones use the character's sides, like the apple rig.
CTRL = {
    'Body': O['CTRL • body waddle'],
    'Foot_R': O['CTRL • duck foot L'], 'Foot_L': O['CTRL • duck foot R'],
    'Hand_R': O['CTRL • swinging glove L'], 'Hand_L': O['CTRL • swinging glove R'],
}
TUBE = {
    'Arm_R': O['Arm L • flexible cartoon'], 'Arm_L': O['Arm R • flexible cartoon'],
    'Leg_R': O['Leg L • flexible cartoon'], 'Leg_L': O['Leg R • flexible cartoon'],
}


def owner_bone(o):
    """Rigid parts follow the controller empty they are parented under."""
    for t, tube in TUBE.items():
        if o == tube: return None
    q = o.parent
    while q and q != root:
        for b, c in CTRL.items():
            if q == c: return b
        q = q.parent
    return 'Body'


def depsgraph(frame):
    scene.frame_set(frame)
    return bpy.context.evaluated_depsgraph_get()


def ring_centres(o, dg):
    e = o.evaluated_get(dg)
    m = e.to_mesh()
    w = e.matrix_world
    out = []
    for r in range(RINGS):
        s = Vector()
        for i in range(r * RING, (r + 1) * RING): s += w @ m.vertices[i].co
        out.append(s / RING)
    e.to_mesh_clear()
    return out


# ---------------------------------------------------------------- normalisation
dg = depsgraph(BIND)
lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
for o in members:
    if o.type != 'MESH': continue
    e = o.evaluated_get(dg); m = e.to_mesh()
    for v in m.vertices:
        p = e.matrix_world @ v.co
        lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    e.to_mesh_clear()
centre_xy = Vector()
for f in range(FIRST, LAST):
    scene.frame_set(f); centre_xy += CTRL['Body'].matrix_world.translation
centre_xy /= (LAST - FIRST)
SCALE = HEIGHT / (hi.z - lo.z)
T = Matrix.Scale(SCALE, 4) @ Matrix.Translation((-centre_xy.x, -centre_xy.y, -lo.z))
T_inv = T.inverted()


# ---------------------------------------------------------------- per-frame samples
def sample(frame):
    d = depsgraph(frame)
    s = {'ctrl': {b: T @ c.matrix_world @ T_inv for b, c in CTRL.items()}, 'verts': {}, 'centre': {}}
    for t, o in TUBE.items(): s[t] = [T @ p for p in ring_centres(o, d)]
    for o in members:
        if o.type != 'MESH': continue
        e = o.evaluated_get(d); m = e.to_mesh(); w = T @ e.matrix_world
        vs = [w @ v.co for v in m.vertices]
        if o in TUBE.values(): s['verts'][o.name] = vs
        s['centre'][o.name] = sum(vs, Vector()) / len(vs)
        e.to_mesh_clear()
    return s


samples = {f: sample(f) for f in range(FIRST, LAST + 1)}
rest = samples[BIND]


def frame3(y, x_hint):
    y = y.normalized()
    x = (x_hint - y * x_hint.dot(y))
    if x.length < 1e-5: x = Vector((0, 0, 1)).cross(y)
    x.normalize()
    z = x.cross(y)
    return Matrix((x, y, z)).transposed()


def mat(rot3, head):
    m = rot3.to_4x4(); m.translation = head; return m


def ctrl_frame(b, s):
    """Bone along the controller's local Z (up), X along its X; scale removed."""
    c = s['ctrl'][b]
    r = c.to_3x3().normalized()
    return mat(frame3(r.col[2], r.col[0]), c.translation)


SIDE = {'R': Vector((-1, 0, 0)), 'L': Vector((1, 0, 0))}  # character's side, facing -Y
LEN = {}
REST = {'Root': Matrix.Identity(4)}
REST['Body'] = ctrl_frame('Body', rest)
for side in 'LR':
    for limb in ('Arm', 'Leg'):
        rings = rest[limb + '_' + side]
        a, k, b = rings[0], rings[ELBOW_RING], rings[-1]
        fwd = Vector((0, -1, 0))  # character faces Blender -Y
        REST[limb + '_' + side] = mat(frame3(k - a, SIDE[side] if limb == 'Arm' else fwd), a)
        REST[limb + '_' + side + '_Lower'] = mat(frame3(b - k, SIDE[side] if limb == 'Arm' else fwd), k)
        LEN[limb + '_' + side] = (k - a).length
        LEN[limb + '_' + side + '_Lower'] = (b - k).length
    lower = REST['Arm_' + side + '_Lower']
    wrist = lower.translation + lower.col[1].xyz * LEN['Arm_' + side + '_Lower']
    REST['Hand_' + side] = mat(frame3(lower.col[1].xyz, SIDE[side]), wrist)
    REST['Foot_' + side] = ctrl_frame('Foot_' + side, rest)
# face marker: in front of the eyes, child of Body
eyes = Vector()
dgb = depsgraph(BIND)
for n in ('Eye L • rounded ivory eyeball', 'Eye R • rounded ivory eyeball'):
    e = O[n].evaluated_get(dgb); m = e.to_mesh()
    eyes += T @ (sum((e.matrix_world @ v.co for v in m.vertices), Vector()) / len(m.vertices)); e.to_mesh_clear()
eyes /= 2
REST['Face'] = mat(frame3(Vector((0, -1, 0)), Vector((1, 0, 0))), eyes)
PARENT = {'Root': None, 'Face': 'Body'}
for b in REST:
    if b.startswith('Hand_'): PARENT[b] = 'Arm_' + b[-1] + '_Lower'
    elif b not in PARENT: PARENT[b] = 'Root'
BONE_LEN = {b: max(.04, LEN.get(b, .08)) for b in REST}
BONE_LEN['Root'] = .1; BONE_LEN['Body'] = .25


def pose_world(b, s, poses):
    if b == 'Root': return Matrix.Identity(4)
    if b == 'Face': return poses['Body'] @ REST['Body'].inverted() @ REST['Face']
    if b in ('Body', 'Foot_L', 'Foot_R'):
        delta = s['ctrl'][b] @ rest['ctrl'][b].inverted()
        m = delta @ REST[b]
        r = m.to_3x3().normalized(); return mat(r, m.translation)
    if b.startswith('Hand_'):
        lower = poses['Arm_' + b[-1] + '_Lower']
        wrist = lower.translation + lower.col[1].xyz * LEN['Arm_' + b[-1] + '_Lower']
        delta = (s['ctrl'][b] @ rest['ctrl'][b].inverted()).to_3x3().normalized()
        return mat(delta @ REST[b].to_3x3(), wrist)
    limb, side = b[:3], b[4]
    rings = s[limb + '_' + side]
    head, tail = (rings[0], rings[ELBOW_RING]) if not b.endswith('Lower') else (rings[ELBOW_RING], rings[-1])
    r0 = REST[b].to_3x3()
    swing = r0.col[1].rotation_difference((tail - head).normalized()).to_matrix()
    return mat(swing @ r0, head)


ORDER = ['Root', 'Body', 'Face', 'Arm_L', 'Arm_L_Lower', 'Hand_L', 'Arm_R', 'Arm_R_Lower', 'Hand_R',
         'Leg_L', 'Leg_L_Lower', 'Foot_L', 'Leg_R', 'Leg_R_Lower', 'Foot_R']

# ---------------------------------------------------------------- bind meshes
def budget(name):
    """Triangle cap per supplied part (the source is ~324k triangles, built for stills)."""
    if 'crescent peel' in name: return 5000
    if name.startswith(('Peel ridge', 'Smile • sculpted', 'Smile • fine', 'Sneaker')) and ('piping' in name or 'ridge' in name or 'lip' in name): return 900
    if 'stitch' in name or 'lace' in name or 'highlight' in name or 'pupil' in name or 'outline' in name: return 160
    if name.startswith(('Eye', 'Smile')): return 500
    if name.startswith('Glove'): return 360
    return 700


dgb = depsgraph(BIND)
parts = []
tri_before = 0
for o in members:
    if o.type != 'MESH': continue
    e = o.evaluated_get(dgb)
    me = bpy.data.meshes.new_from_object(e, preserve_all_data_layers=True, depsgraph=dgb)
    tube = next((t for t, x in TUBE.items() if x == o), None)
    me.transform(T @ o.matrix_world)
    me.calc_loop_triangles(); tri_before += len(me.loop_triangles)
    p = bpy.data.objects.new('BANANA_' + o.name.split(' •')[0].replace(' ', '_'), me)
    p['source'] = o.name
    scene.collection.objects.link(p)
    # runtime triangle budget per part; deforming tubes keep their ring topology
    tris = len(me.loop_triangles)
    cap = budget(o.name)
    if not tube and tris > cap:
        dec = p.modifiers.new('Runtime triangle budget', 'DECIMATE'); dec.ratio = cap / tris
        with bpy.context.temp_override(object=p, active_object=p):
            bpy.ops.object.modifier_apply(modifier=dec.name)
    for b in ORDER: p.vertex_groups.new(name=b)
    if tube:
        upper, lower, end = tube, tube + '_Lower', ('Hand_' if tube.startswith('Arm') else 'Foot_') + tube[-1]

        def ss(a, b, x):
            t = min(1, max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
        for v in p.data.vertices:
            r = min(RINGS - 1, v.index // RING) if v.index < RINGS * RING else RINGS - 1
            t = r / (RINGS - 1)
            wl = ss(.32, .64, t); we = ss(.86, 1.0, t)
            p.vertex_groups[upper].add([v.index], 1 - wl, 'REPLACE')
            p.vertex_groups[lower].add([v.index], wl * (1 - we), 'REPLACE')
            p.vertex_groups[end].add([v.index], wl * we, 'REPLACE')
    else:
        p.vertex_groups[owner_bone(o)].add(list(range(len(p.data.vertices))), 1.0, 'REPLACE')
    parts.append(p)

# remove the supplied scene objects from this derived file (source file itself is untouched)
for o in list(O):
    if o not in parts: bpy.data.objects.remove(o, do_unlink=True)

# ---------------------------------------------------------------- armature
arm_data = bpy.data.armatures.new('Banana_Rig')
rig = bpy.data.objects.new('Banana_Rig', arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
for b in ORDER:
    eb = arm_data.edit_bones.new(b)
    eb.head = (0, 0, 0); eb.tail = (0, BONE_LEN[b], 0)
    eb.matrix = REST[b]
    eb.length = BONE_LEN[b]
for b in ORDER:
    if PARENT[b]: arm_data.edit_bones[b].parent = arm_data.edit_bones[PARENT[b]]
bpy.ops.object.mode_set(mode='OBJECT')
for p in parts:
    mod = p.modifiers.new('Armature', 'ARMATURE'); mod.object = rig
    p.parent = rig

c_bind = {p['source']: sum((v.co for v in p.data.vertices), Vector()) / len(p.data.vertices) for p in parts}

# ---------------------------------------------------------------- bake bone animation
for pb in rig.pose.bones: pb.rotation_mode = 'QUATERNION'
for f in range(FIRST, LAST + 1):
    s = samples[f]; poses = {}
    for b in ORDER:
        poses[b] = pose_world(b, s, poses)
        bone = rig.data.bones[b]
        if PARENT[b]:
            prest = rig.data.bones[PARENT[b]].matrix_local
            basis = (prest.inverted() @ bone.matrix_local).inverted() @ poses[PARENT[b]].inverted() @ poses[b]
        else:
            basis = bone.matrix_local.inverted() @ poses[b]
        pb = rig.pose.bones[b]
        pb.matrix_basis = basis
        pb.keyframe_insert('location', frame=f); pb.keyframe_insert('rotation_quaternion', frame=f); pb.keyframe_insert('scale', frame=f)
rig.animation_data.action.name = 'Banana_Walk'
scene.frame_start, scene.frame_end = FIRST, LAST

# ---------------------------------------------------------------- fidelity: skinned vs authored
tube_err = {}; part_err = {}
for f in range(FIRST, LAST + 1, 5):
    scene.frame_set(f); d = bpy.context.evaluated_depsgraph_get()
    for p in parts:
        e = p.evaluated_get(d); m = e.to_mesh(); vs = [e.matrix_world @ v.co for v in m.vertices]
        name = p['source']
        if name in samples[f]['verts']:
            ref = samples[f]['verts'][name]
            err = [(a - b).length for a, b in zip(vs, ref)]
            tube_err[name] = max(tube_err.get(name, 0), sum(err) / len(err))
        else:
            c = sum(vs, Vector()) / len(vs)
            # compare displacement from the bind pose, so decimation offsets cancel out
            part_err[name] = max(part_err.get(name, 0), ((c - c_bind[name]) - (samples[f]['centre'][name] - samples[BIND]['centre'][name])).length)
        e.to_mesh_clear()
scene.frame_set(BIND)

bpy.ops.object.select_all(action='DESELECT')
for p in parts: p.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
body = bpy.context.view_layer.objects.active
body.name = 'Banana_Mesh'; body.data.name = 'Banana_Mesh'
body.data.calc_loop_triangles(); tri_after = len(body.data.loop_triangles)

os.makedirs(OUT_DIR, exist_ok=True); os.makedirs(os.path.dirname(OUT_BLEND), exist_ok=True)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); body.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX, use_selection=True, object_types={'ARMATURE', 'MESH'},
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True,
    add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
    use_armature_deform_only=False, mesh_smooth_type='FACE',
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0, bake_anim_step=1)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND, copy=True)
report = {
    'source': os.path.relpath(SRC, ROOT), 'fbx': os.path.relpath(OUT_FBX, ROOT),
    'height_m': HEIGHT, 'scale': SCALE, 'bind_frame': BIND, 'frames': [FIRST, LAST], 'fps': scene.render.fps,
    'bones': {b: {'parent': PARENT[b], 'length_m': round(BONE_LEN[b], 4)} for b in ORDER},
    'limb_lengths_m': {k: round(v, 4) for k, v in LEN.items()},
    'triangles_source': tri_before, 'triangles_runtime': tri_after,
    'materials': [s.material.name for s in body.material_slots],
    'fidelity_note': 'max over sampled frames vs the authored shape-key/empty animation, metres at runtime scale',
    'tube_mean_vertex_error_m': {k: round(v, 4) for k, v in tube_err.items()},
    'rigid_part_max_offset_m': {k: round(v, 4) for k, v in sorted(part_err.items(), key=lambda kv: -kv[1])[:12]},
}
with open(REPORT, 'w') as fh: json.dump(report, fh, indent=2)
print('BANANA_RIG_EXPORTED', json.dumps({k: report[k] for k in ('triangles_source', 'triangles_runtime', 'scale', 'tube_mean_vertex_error_m', 'rigid_part_max_offset_m', 'limb_lengths_m')}, indent=1))
