"""Shared Blender helpers for DREAM RUSH kit scripts (bpy 5.x as a module). OUT/BLEND_DIR are set by caller via set_out()."""
import math
import os

import bpy
import bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'DreamRush', 'Art', 'Environment', 'Props')
BLEND_DIR = os.path.join(ROOT, 'ArtSource', 'Blender', 'Environment')
os.makedirs(OUT, exist_ok=True)
os.makedirs(BLEND_DIR, exist_ok=True)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    return m


def link(o):
    bpy.context.scene.collection.objects.link(o)
    return o


def from_bm(name, bm, material):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = link(bpy.data.objects.new(name, me))
    o.data.materials.append(mat(material))
    return o


def apply_mods(o):
    bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers):
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
        bpy.ops.object.modifier_apply(modifier=m.name)


def soft_box(name, size, center, material, bevel=0.08, segs=4, sag=0.0, puff=0.0):
    """rounded box; sag lowers the top centre, puff inflates the faces (cushion feel)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    for v in bm.verts:
        x, y, z = v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]
        if puff:
            k = (1 - (2 * v.co.x) ** 2) * (1 - (2 * v.co.y) ** 2)
            z += math.copysign(puff * k * size[2], v.co.z) if abs(v.co.z) > 0.49 else 0
        if sag and v.co.z > 0.49:
            z -= sag * (1 - (2 * v.co.x) ** 2) * (1 - (2 * v.co.y) ** 2)
        v.co = Vector((x, y, z))
    o = from_bm(name, bm, material)
    o.location = center
    b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = segs; b.limit_method = 'ANGLE'
    apply_mods(o)
    return o


def cyl(name, r1, r2, depth, center, material, segs=20, bevel=0.03):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r1, radius2=r2, depth=depth)
    o = from_bm(name, bm, material)
    o.location = center
    if bevel:
        b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = 3; b.limit_method = 'ANGLE'
        apply_mods(o)
    return o


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = name; objs[0].data.name = name
    # bake the first part's transform into the mesh: origin back at the world origin (the asset's base),
    # identity rotation — otherwise Unity code that sets rotation/position overrides a hidden offset/tilt
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return objs[0]


def uv_cube(o, size=1.0):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=size)
    bpy.ops.object.mode_set(mode='OBJECT')


def smooth(o, angle=50):
    for p in o.data.polygons: p.use_smooth = True
    if hasattr(o.data, 'set_sharp_from_angle'):
        o.data.set_sharp_from_angle(angle=math.radians(angle))


def crooked(o, amount=0.02, seed=1):
    """tiny hand-made irregularity: shear + scale so nothing is mechanically perfect."""
    import random
    r = random.Random(seed)
    sh = Matrix.Shear('XZ', 4, (r.uniform(-amount, amount), r.uniform(-amount, amount)))
    o.data.transform(sh)


def export(name):
    bpy.context.view_layer.update()
    for o in bpy.context.scene.objects:
        if o.type == 'MESH': smooth(o)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, name + '.blend'))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.context.scene.objects if o.type == 'MESH')
    print(f"EXPORTED {name} tris={tris} objs={[o.name for o in bpy.context.scene.objects]}")




def set_out(out_rel, blend_rel=os.path.join('ArtSource', 'Blender', 'Environment')):
    global OUT, BLEND_DIR
    OUT = os.path.join(ROOT, out_rel); BLEND_DIR = os.path.join(ROOT, blend_rel)
    os.makedirs(OUT, exist_ok=True); os.makedirs(BLEND_DIR, exist_ok=True)


def uv_sphere(name, r, center, material, segs=32, rings=16, squash=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=r)
    for v in bm.verts: v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
    o = from_bm(name, bm, material)
    o.location = center
    return o


def torus(name, R, r, center, material, segs=48, rsegs=12):
    bm = bmesh.new()
    verts = []
    for i in range(segs):
        a = i / segs * 2 * math.pi
        ring = []
        for j in range(rsegs):
            b = j / rsegs * 2 * math.pi
            x = (R + r * math.cos(b)) * math.cos(a); y = (R + r * math.cos(b)) * math.sin(a); z = r * math.sin(b)
            ring.append(bm.verts.new((x, y, z)))
        verts.append(ring)
    for i in range(segs):
        for j in range(rsegs):
            a, b2 = verts[i][j], verts[(i + 1) % segs][j]
            c, d = verts[(i + 1) % segs][(j + 1) % rsegs], verts[i][(j + 1) % rsegs]
            bm.faces.new((a, b2, c, d))
    o = from_bm(name, bm, material)
    o.location = center
    return o


def star(name, r_out, r_in, depth, center, material, points=5, bevel=0.06):
    bm = bmesh.new()
    top, bot = [], []
    for k in range(points * 2):
        a = k / (points * 2) * 2 * math.pi + math.pi / 2
        rr = r_out if k % 2 == 0 else r_in
        x, z = math.cos(a) * rr, math.sin(a) * rr
        top.append(bm.verts.new((x, -depth / 2, z))); bot.append(bm.verts.new((x, depth / 2, z)))
    ct = bm.verts.new((0, -depth / 2 - depth * 0.35, 0)); cb = bm.verts.new((0, depth / 2 + depth * 0.35, 0))
    n = len(top)
    for k in range(n):
        bm.faces.new((ct, top[k], top[(k + 1) % n]))
        bm.faces.new((cb, bot[(k + 1) % n], bot[k]))
        bm.faces.new((top[k], bot[k], bot[(k + 1) % n], top[(k + 1) % n]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = from_bm(name, bm, material)
    o.location = center
    if bevel:
        b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = 2; b.limit_method = 'ANGLE'
        apply_mods(o)
    return o
