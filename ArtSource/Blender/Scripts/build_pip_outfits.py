"""
Pip's per-level outfits (user brief 2026-10-20: the character's style changes with each level).
  PIP_Outfit_Phantom : Midnight Palace — a phantom-thief domino mask hugging the apple face around the big eyes
                       (black, white piping, a red tip), plus a tiny top hat pinned beside the stem.
  PIP_Outfit_Densha  : World 1 (Densha-Attack energy) — chunky yellow over-ear headphones with a black band.
Authored in Pip's BODY-local space (origin = Body pivot, metres, front = -Y), measured from CHR_AppleChild_Game_v2.blend,
so StylePack can parent them straight under the Body transform. Materials M_Outfit_* → DreamToon in Unity (OutfitSetup).
run: python3 -I build_pip_outfits.py
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402
import dr_bkit as K  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'ArtSource', 'Blender', 'Characters', 'CHR_AppleChild_Game_v4.blend')
K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'Characters', 'Outfits'), os.path.join('ArtSource', 'Blender', 'Characters'))

# ---------------------------------------------------------------- measure Pip's body
bpy.ops.wm.open_mainfile(filepath=SRC)
body = bpy.data.objects['Body']
mw = body.matrix_world.copy()
pivot = mw.translation.copy()
vs = [mw @ v.co for v in body.data.vertices]
core = [v for v in vs if v.y < pivot.y + 0.45]   # v3: ignore the pillow backpack when measuring the apple
mn = Vector((min(v.x for v in core), min(v.y for v in core), min(v.z for v in core)))
mx = Vector((max(v.x for v in core), max(v.y for v in core), max(v.z for v in core)))
R = (mx.x - mn.x) / 2 * 0.985          # skin radius (the face sits on the front, not the sides)
C = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2 + 0.0, (mn.z + mx.z) / 2 - 0.01))
S = R / 0.43                           # scale of the v2 build (R was 0.43 before normalisation)
print('BODY pivot', tuple(round(a, 3) for a in pivot), 'centre', tuple(round(a, 3) for a in C), 'R', round(R, 3), 'S', round(S, 3))
from mathutils.bvhtree import BVHTree
skin_idx = [i for i, m in enumerate(body.data.materials) if m and m.name.startswith('M_Apple_Skin')]
bvh = BVHTree.FromPolygons([tuple(v) for v in vs], [tuple(p.vertices) for p in body.data.polygons if not skin_idx or p.material_index in skin_idx])   # skin only: lashes/eyes must not dent the mask

def surf(x, z, push):
    """front surface point at lateral x / height z (metres from the body centre), pushed out by push."""
    o = Vector((C.x + x, C.y - 2.0, C.z + z))
    hit, n, i, d = bvh.ray_cast(o, Vector((0, 1, 0)))
    if hit is None:
        d3 = Vector((x, -math.sqrt(max(R * R - x * x - z * z, 0.001)), z)).normalized()
        return C + d3 * (R + push)
    nn = (hit - C).normalized()
    return hit + nn * push

# eyes (from build_apple_v2: x ±0.132, z +0.075 from the 0.43-radius centre, ovals 0.097 × 0.13) → scaled
EX, EZ, ERX, ERZ = 0.132 * S, 0.075 * S, 0.112 * S, 0.15 * S

K.reset()

def to_body_local(o):
    """move world-authored geometry into Body-local space (origin at the Body pivot)."""
    o.data.transform(Matrix.Translation(-pivot))
    o.location = (0, 0, 0)


# ================================================================ PIP_Outfit_Phantom
# the domino silhouette in (x, z) around the eye line; eye holes 8 % larger than Pip's eyes
OUT = [(0.0, 0.17), (0.13, 0.215), (0.25, 0.235), (0.34, 0.34), (0.38, 0.17), (0.35, -0.01), (0.27, -0.09), (0.16, -0.1), (0.07, -0.06), (0.0, -0.03)]   # v3: lifted off the grin
outline = [(x * S, z * S + EZ) for x, z in OUT] + [(-x * S, z * S + EZ) for x, z in reversed(OUT[1:-1])]

def smooth_loop(pts, n=6):
    """Catmull-Rom resample of a closed loop."""
    out = []
    L = len(pts)
    for i in range(L):
        p0, p1, p2, p3 = [Vector((*pts[(i + k) % L], 0)) for k in (-1, 0, 1, 2)]
        for t in [k / n for k in range(n)]:
            t2, t3 = t * t, t * t * t
            q = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out.append((q.x, q.y))
    return out

curve = bpy.data.curves.new('MaskShape', 'CURVE'); curve.dimensions = '2D'; curve.fill_mode = 'BOTH'
def add_loop(pts):
    sp = curve.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for k, (x, z) in enumerate(pts): sp.points[k].co = (x, z, 0, 1)
    sp.use_cyclic_u = True
add_loop(smooth_loop(outline, 5))
for side in (-1, 1):
    add_loop([(side * EX + math.cos(a) * ERX * 1.14, EZ + math.sin(a) * ERZ * 1.12) for a in [k / 48 * 2 * math.pi for k in range(48)]])
co = K.link(bpy.data.objects.new('MaskShape', curve))
bpy.ops.object.select_all(action='DESELECT'); co.select_set(True); bpy.context.view_layer.objects.active = co
bpy.ops.object.convert(target='MESH'); mask = bpy.context.view_layer.objects.active
bm = bmesh.new(); bm.from_mesh(mask.data)
bmesh.ops.triangulate(bm, faces=bm.faces[:])
bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
for v in bm.verts:                       # wrap the flat (x, z) shape onto the apple's face
    v.co = surf(v.co.x, v.co.y, 0.026)
bm.to_mesh(mask.data); bm.free()
mask.data.materials.clear(); mask.data.materials.append(K.mat('M_Outfit_Ink'))
sol = mask.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = 0.012; sol.offset = -1; K.apply_mods(mask)
# white rim piping along the outer edge (P5-style trim, our own shape)
rim_pts = [surf(x, z, 0.03) for x, z in smooth_loop(outline, 5)]
curve2 = bpy.data.curves.new('Rim', 'CURVE'); curve2.dimensions = '3D'; curve2.bevel_depth = 0.007 * S; curve2.bevel_resolution = 2
sp = curve2.splines.new('POLY'); sp.points.add(len(rim_pts) - 1)
for k, p in enumerate(rim_pts): sp.points[k].co = (p.x, p.y, p.z, 1)
sp.use_cyclic_u = True
ro = K.link(bpy.data.objects.new('Rim', curve2))
bpy.ops.object.select_all(action='DESELECT'); ro.select_set(True); bpy.context.view_layer.objects.active = ro
bpy.ops.object.convert(target='MESH'); ro = bpy.context.view_layer.objects.active
ro.data.materials.clear(); ro.data.materials.append(K.mat('M_Outfit_White'))
parts = [mask, ro]
# tiny top hat pinned at a jaunty angle beside the stem
hat_base = C + Vector((-0.17 * S, 0.02 * S, R * 0.93))
brim = K.cyl('Brim', 0.11 * S, 0.11 * S, 0.012 * S, hat_base, 'M_Outfit_Ink', segs=24, bevel=0.003)
crown = K.cyl('Crown', 0.065 * S, 0.072 * S, 0.12 * S, hat_base + Vector((0, 0, 0.066 * S)), 'M_Outfit_Ink', segs=24, bevel=0.004)
band = K.cyl('Band', 0.0735 * S, 0.0735 * S, 0.022 * S, hat_base + Vector((0, 0, 0.02 * S)), 'M_Outfit_Red', segs=24, bevel=0)
hat = K.join([brim, crown, band], 'Hat')
hat.data.transform(Matrix.Translation(-hat_base)); hat.data.transform(Matrix.Rotation(math.radians(-24), 4, 'Y')); hat.data.transform(Matrix.Translation(hat_base))
parts.append(hat)
o = K.join(parts, 'PIP_Outfit_Phantom'); to_body_local(o)
K.export('PIP_Outfit_Phantom')

# ================================================================ PIP_Outfit_Densha (headphones)
K.reset()
parts = []
for side in (-1, 1):
    c = C + Vector((side * R * 0.98, 0.0, 0.08 * S))
    cup = K.cyl(f'Cup{side}', 0.1 * S, 0.11 * S, 0.075 * S, c + Vector((side * 0.03 * S, 0, 0)), 'M_Outfit_Yellow', segs=28, bevel=0.012 * S)
    cup.data.transform(Matrix.Rotation(math.radians(90), 4, 'Y'))
    pad = K.cyl(f'Pad{side}', 0.085 * S, 0.085 * S, 0.03 * S, c - Vector((side * 0.01 * S, 0, 0)), 'M_Outfit_Ink', segs=28, bevel=0.008 * S)
    pad.data.transform(Matrix.Rotation(math.radians(90), 4, 'Y'))
    dot = K.uv_sphere(f'Logo{side}', 1.0, c + Vector((side * 0.07 * S, 0, 0)), 'M_Outfit_Ink', segs=12, rings=8, squash=(0.008 * S, 0.035 * S, 0.035 * S))
    parts += [cup, pad, dot]
# band: arc over the head (slightly behind the stem)
pts = []
for k in range(25):
    a = math.pi * k / 24
    pts.append(C + Vector((math.cos(a) * R * 1.04, 0.07 * S, 0.08 * S + math.sin(a) * R * 1.12)))
curve = bpy.data.curves.new('Band', 'CURVE'); curve.dimensions = '3D'; curve.bevel_depth = 0.022 * S; curve.bevel_resolution = 3
sp = curve.splines.new('POLY'); sp.points.add(len(pts) - 1)
for k, p in enumerate(pts): sp.points[k].co = (p.x, p.y, p.z, 1)
bo = K.link(bpy.data.objects.new('Band', curve))
bpy.ops.object.select_all(action='DESELECT'); bo.select_set(True); bpy.context.view_layer.objects.active = bo
bpy.ops.object.convert(target='MESH'); bo = bpy.context.view_layer.objects.active
bo.data.materials.clear(); bo.data.materials.append(K.mat('M_Outfit_Ink'))
parts.append(bo)
o = K.join(parts, 'PIP_Outfit_Densha'); to_body_local(o)
K.export('PIP_Outfit_Densha')
print('OUTFITS DONE')
