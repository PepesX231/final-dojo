"""
DREAM RUSH — CHR_AppleChild game build (blender-asset-pipeline skill)
Turns the user's Cheeky Apple Chibi v3 sculpt into a game-ready rigid-part character:
  * curves -> meshes, decimated to the P0 triangle budget
  * split into Body / Leaf / Arm_L / Arm_R / Leg_L / Leg_R with pivots at stem base / shoulders / hips
  * scaled to 1.2 m, feet at Z=0, front = -Y (Unity +Z after export)
  * apple skin colour baked to a texture (procedural nodes are not portable)
  * FBX + game .blend written next to the source

usage: python3 -I build_apple_game.py <source.blend> <out_dir>
Side naming: Blender -X = character's RIGHT (verified in Unity 2026-10-16: front +Z, Leg_R at +X).
Hierarchy is flat (all parts under CHR_AppleChild) — see note at the parenting step.
"""
import bpy, bmesh, sys, os, math
from mathutils import Matrix, Vector

SRC, OUT = sys.argv[-2], sys.argv[-1]
os.makedirs(OUT, exist_ok=True)
TARGET_HEIGHT = 1.2
SOURCE_HEIGHT = 3.29          # measured: feet z=0 .. leaf tip z=3.29
S = TARGET_HEIGHT / SOURCE_HEIGHT

bpy.ops.wm.open_mainfile(filepath=SRC)
scene = bpy.context.scene

# ---------------------------------------------------------------- clean studio objects
for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT') or 'floor' in o.name.lower():
        bpy.data.objects.remove(o, do_unlink=True)

# ---------------------------------------------------------------- curves -> meshes (lower resolution first)
for o in bpy.data.objects:
    if o.type == 'CURVE':
        o.data.resolution_u = min(o.data.resolution_u, 10)
        o.data.bevel_resolution = min(o.data.bevel_resolution, 3)

def select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]

curves = [o for o in bpy.data.objects if o.type == 'CURVE']
select_only(curves)
bpy.ops.object.convert(target='MESH')
# apply leaf modifiers
leaf = bpy.data.objects['Tiny leaf']
select_only([leaf])
bpy.ops.object.convert(target='MESH')

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def decimate(o, target):
    t = tris(o)
    if t <= target:
        return
    m = o.modifiers.new('dec', 'DECIMATE')
    m.ratio = max(0.05, target / t)
    select_only([o])
    bpy.ops.object.modifier_apply(modifier=m.name)

# ---------------------------------------------------------------- groups
N = lambda n: bpy.data.objects[n]
face = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith(
    ('Reference', 'Muted', 'Left upper', 'Right upper', 'Subtle'))]
groups = {
    'Body': [N('APPLE • oversized chibi body'), N('Stem • jaunty tilt')] + face,
    'Leaf': [N('Tiny leaf')],
    'Arm_R': [N('Short bent arm -1'), N('Glove cuff -1'), N('Clasped glove front'), N('Top curled thumb')],
    'Arm_L': [N('Short bent arm 1'), N('Glove cuff 1'), N('Clasped glove rear'),
              N('Folded glove finger 0'), N('Folded glove finger 1'), N('Folded glove finger 2')],
    'Leg_R': [N('Stubby leg -1'), N('Flat sole foot -1')],
    'Leg_L': [N('Stubby leg 1'), N('Flat sole foot 1')],
}
budget = {  # per source object, before join
    'APPLE • oversized chibi body': 4800, 'Stem • jaunty tilt': 240,
}
for o in face:
    budget[o.name] = 100 if 'reflection' in o.name.lower() else 300
for g in ('Arm_R', 'Arm_L'):
    for o in groups[g]:
        budget[o.name] = 260 if 'finger' in o.name.lower() else 420
for g in ('Leg_R', 'Leg_L'):
    for o in groups[g]:
        budget[o.name] = 700

def fix_normals(o):
    # Source body/eyes/pupils have inward-facing normals (Cycles hides it, Unity back-face culls them).
    select_only([o])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')

for objs in groups.values():
    for o in objs:
        select_only([o])
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        fix_normals(o)
        if o.name in budget:
            decimate(o, budget[o.name])

# ---------------------------------------------------------------- bake apple skin before joining
body = N('APPLE • oversized chibi body')
select_only([body])
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
bpy.ops.object.mode_set(mode='OBJECT')
skin = body.material_slots[0].material
img = bpy.data.images.new('T_AppleChild_Skin', 1024, 1024)
nt = skin.node_tree
tex_node = nt.nodes.new('ShaderNodeTexImage')
tex_node.image = img
nt.nodes.active = tex_node
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 8
scene.render.bake.use_pass_direct = False
scene.render.bake.use_pass_indirect = False
scene.render.bake.use_pass_color = True
scene.render.bake.margin = 8
bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
img.filepath_raw = os.path.join(OUT, 'T_AppleChild_Skin.png')
img.file_format = 'PNG'
img.save()

# ---------------------------------------------------------------- simplify materials (portable)
def flat_material(mat, color, rough=0.6, image=None):
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = rough
    if image is not None:
        t = nt.nodes.new('ShaderNodeTexImage')
        t.image = image
        nt.links.new(t.outputs['Color'], bsdf.inputs['Base Color'])
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])

rename = {
    'Apple • candy crimson': 'M_Apple_Skin',
    'Expression • deep burgundy': 'M_Apple_Ink',
    'Reference • recessed mouth': 'M_Apple_Ink',
    'Eye ivory': 'M_Apple_EyeWhite',
    'Reference • warm eye whites': 'M_Apple_EyeWhite',
    'Ivory cotton gloves': 'M_Apple_Glove',
    'Leaf jade': 'M_Apple_Leaf',
    'Reference • muted pupil reflection': 'M_Apple_PupilShine',
    'Reference • near black pupils': 'M_Apple_Pupil',
    'Stem cinnamon': 'M_Apple_Stem',
    'Warm biscuit limbs': 'M_Apple_Limb',
}
canonical = {}
for old, new in rename.items():
    m = bpy.data.materials.get(old)
    if m is None:
        continue
    if new in canonical:
        # merge duplicate (e.g. two ink materials) into the first
        for o in bpy.data.objects:
            for s in o.material_slots:
                if s.material == m:
                    s.material = canonical[new]
        continue
    col = tuple(m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) \
        if 'Principled BSDF' in m.node_tree.nodes else (0.5, 0.5, 0.5, 1)
    m.name = new
    canonical[new] = m
    flat_material(m, col, image=img if new == 'M_Apple_Skin' else None)

# ---------------------------------------------------------------- join groups, scale, pivots
pivots = {
    'Body': Vector((0.0, 0.0, 1.30)),
    'Leaf': Vector((-0.04, 0.0, 3.12)),
    'Arm_R': Vector((-0.91, 0.0, 2.06)),
    'Arm_L': Vector((0.91, 0.0, 2.06)),
    'Leg_R': Vector((-0.36, 0.02, 1.40)),
    'Leg_L': Vector((0.36, 0.02, 1.40)),
}
final = {}
for name, objs in groups.items():
    select_only(objs)
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = 'MSH_' + name
    o.data.transform(Matrix.Scale(S, 4))          # world-space verts scaled about origin (feet)
    p = pivots[name] * S
    o.data.transform(Matrix.Translation(-p))
    o.location = p
    for poly in o.data.polygons:
        poly.use_smooth = True
    final[name] = o

bpy.context.view_layer.update()  # matrix_world must be current before re-parenting
root = bpy.data.objects.new('CHR_AppleChild', None)
scene.collection.objects.link(root)
root.empty_display_size = 0.3

def parent(child, par):
    w = child.matrix_world.copy()
    child.parent = par
    child.matrix_world = w

# FLAT hierarchy on purpose: FBX 'Apply Transform' (bake_space_transform) only fixes direct children of
# the root. Arms/Leaf follow the body via AppleProceduralAnimator's virtual parenting instead.
for n in final:
    parent(final[n], root)

# remove anything that is not part of the character
keep = set(final.values()) | {root}
for o in list(bpy.data.objects):
    if o not in keep:
        bpy.data.objects.remove(o, do_unlink=True)

# ---------------------------------------------------------------- report
total = 0
for n, o in final.items():
    t = tris(o)
    total += t
    print(f"PART {n:6s} tris={t:5d} pivot={tuple(round(c, 3) for c in o.matrix_world.translation)}")
bb_min = Vector((1e9,) * 3); bb_max = Vector((-1e9,) * 3)
for o in final.values():
    for v in o.data.vertices:
        w = o.matrix_world @ v.co
        bb_min = Vector(map(min, bb_min, w)); bb_max = Vector(map(max, bb_max, w))
print("TOTAL_TRIS", total)
print("BOUNDS", tuple(round(c, 3) for c in bb_min), tuple(round(c, 3) for c in bb_max))

# ---------------------------------------------------------------- save + export
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'CHR_AppleChild_Game_v1.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in keep:
    o.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, 'CHR_AppleChild.fbx'),
    use_selection=True, object_types={'EMPTY', 'MESH'},
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
    add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
print("EXPORTED")
