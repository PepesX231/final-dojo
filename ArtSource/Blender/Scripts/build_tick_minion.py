"""
MINION_Tick v2 — the Tick-Tock shadows of the Midnight Palace (model quality pass 2026-10-20).
A 1.5 m twin-bell alarm clock gone rogue: gold-rimmed midnight case, cream dial with ticks and crooked hands,
a mini phantom mask with yellow eye slits, a jagged grin, bells + hammer, stubby arms with white mitts, pointy shoes,
and a tattered shadow scarf. Single mesh (TickMinion animates the whole transform). Front = -Y.
run: python3 -I build_tick_minion.py   (overrides the MINION_Tick from build_city_kit.py)
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere
K.reset()


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def flat_shape(name, pts, depth, material, y, smooth_n=4, bulge=0.0):
    """closed 2D outline in the XZ plane (Catmull-Rom smoothed) → filled slab `depth` thick facing -Y at y; bulge pushes the middle forward."""
    out = []
    L = len(pts)
    for i in range(L):
        p0, p1, p2, p3 = [Vector((*pts[(i + k) % L], 0)) for k in (-1, 0, 1, 2)]
        for t in [k / smooth_n for k in range(smooth_n)]:
            t2, t3 = t * t, t * t * t
            q = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out.append((q.x, q.y))
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '2D'; cu.fill_mode = 'BOTH'
    sp = cu.splines.new('POLY'); sp.points.add(len(out) - 1)
    for k, (x, z) in enumerate(out): sp.points[k].co = (x, z, 0, 1)
    sp.use_cyclic_u = True
    o = K.link(bpy.data.objects.new(name, cu))
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.convert(target='MESH'); o = bpy.context.view_layer.objects.active
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=1, use_grid_fill=True)
    xs = [v.co.x for v in bm.verts]; w = max(max(xs), -min(xs), 1e-3)
    for v in bm.verts:   # XY plane -> XZ plane at y, middle bulging forward
        x, z = v.co.x, v.co.y
        v.co = Vector((x, y - bulge * (1 - (x / w) ** 2), z))
    bm.to_mesh(o.data); bm.free()
    o.data.materials.clear(); o.data.materials.append(K.mat(material))
    sol = o.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = depth; sol.offset = 1; K.apply_mods(o)
    return o



BZ = 0.98   # body centre
p = []
p.append(rot(CY('Case', 0.56, 0.56, 0.44, (0, 0, BZ), 'M_Kit_MP_MinionBody', segs=40, bevel=0.1), x=90))
p.append(rot(K.torus('Rim', 0.52, 0.06, (0, -0.22, BZ), 'M_Kit_MP_Gold', segs=48, rsegs=10), x=90))
p.append(rot(CY('Dial', 0.47, 0.47, 0.04, (0, -0.21, BZ), 'M_Kit_MP_Face', segs=40, bevel=0.01), x=90))
for k in range(12):
    a = k / 12 * 2 * math.pi; big = k % 3 == 0
    t = SB(f'Tick{k}', (0.03 if not big else 0.05, 0.02, 0.09 if big else 0.06), (math.sin(a) * 0.39, -0.24, BZ + math.cos(a) * 0.39), 'M_Kit_MP_Dark', bevel=0.008, segs=1)
    p.append(rot(t, y=-math.degrees(a)))
# mini phantom mask (white wings + dark trim) with angry yellow slits
MZ = BZ + 0.1
wing = [(0.0, 0.2), (0.08, 0.13), (0.24, 0.15), (0.42, 0.26), (0.56, 0.42), (0.47, 0.16), (0.36, 0.0), (0.2, -0.07), (0.08, -0.04), (0.0, -0.09)]
outline = [(x, z + MZ) for x, z in wing] + [(-x, z + MZ) for x, z in reversed(wing[1:-1])]
p.append(flat_shape('Mask', outline, 0.05, 'M_Kit_MP_Mask', -0.3, smooth_n=5, bulge=0.05))
for s in (-1, 1):
    slit = [(s * 0.06, MZ + 0.03), (s * 0.18, MZ + 0.09), (s * 0.31, MZ + 0.12), (s * 0.27, MZ + 0.04), (s * 0.15, MZ - 0.01)]
    if s < 0: slit = list(reversed(slit))
    p.append(flat_shape(f'Eye{s}', slit, 0.03, 'M_Kit_MP_Eye', -0.37, smooth_n=3))
# jagged grin
grin = [(-0.26, BZ - 0.12), (-0.12, BZ - 0.24), (0.0, BZ - 0.26), (0.12, BZ - 0.24), (0.26, BZ - 0.12), (0.12, BZ - 0.17), (0.0, BZ - 0.19), (-0.12, BZ - 0.17)]
p.append(flat_shape('Grin', grin, 0.02, 'M_Kit_MP_Dark', -0.26, smooth_n=4))
for k in range(-2, 3):
    x = k * 0.075; zt = BZ - 0.165 - 0.02 * (1 - (x / 0.26) ** 2)
    p.append(flat_shape(f'Tooth{k}', [(x - 0.03, zt + 0.004), (x + 0.03, zt + 0.004), (x, zt - 0.05)], 0.012, 'M_Kit_MP_Mask', -0.29, smooth_n=1))
# crooked hands peeking out under the mask
p.append(rot(SB('HandH', (0.035, 0.02, 0.2), (0.05, -0.27, BZ - 0.06), 'M_Kit_MP_Dark', bevel=0.008, segs=1), y=-130))
# twin bells, hammer, carry handle, little feet-legs
for s in (-1, 1):
    bell = SP(f'Bell{s}', 0.24, (s * 0.34, 0.02, BZ + 0.6), 'M_Kit_MP_Gold', segs=24, rings=12, squash=(1, 1, 0.78))
    p.append(rot(bell, y=s * 28))
    p.append(SP(f'BellKnob{s}', 0.05, (s * 0.44, 0.02, BZ + 0.8), 'M_Kit_MP_Gold', segs=10, rings=6))
    p.append(rot(CY(f'BellPost{s}', 0.03, 0.03, 0.2, (s * 0.24, 0.02, BZ + 0.48), 'M_Kit_MP_Gold', segs=8, bevel=0), y=s * 28))
p.append(CY('HammerRod', 0.02, 0.02, 0.32, (0, 0.05, BZ + 0.66), 'M_Kit_MP_Dark', segs=8, bevel=0))
p.append(SP('Hammer', 0.06, (0, 0.05, BZ + 0.83), 'M_Kit_MP_Dark', segs=10, rings=6, squash=(1.6, 1, 1)))
for s in (-1, 1):
    p.append(rot(CY(f'Leg{s}', 0.055, 0.045, 0.42, (s * 0.24, 0.02, 0.36), 'M_Kit_MP_Dark', segs=10, bevel=0), y=s * 8))
    shoe = SP(f'Shoe{s}', 0.13, (s * 0.27, -0.08, 0.08), 'M_Kit_MP_Dark', segs=16, rings=8, squash=(0.9, 1.7, 0.6))
    p.append(shoe)
    p.append(SP(f'ShoeCurl{s}', 0.055, (s * 0.27, -0.3, 0.15), 'M_Kit_MP_Dark', segs=10, rings=6))   # curled jester toe
    p.append(SP(f'ShoeBell{s}', 0.03, (s * 0.27, -0.31, 0.22), 'M_Kit_MP_Gold', segs=8, rings=6))
    # stubby arms + white mitts (raised, ready to pounce)
    arm = CY(f'Arm{s}', 0.05, 0.04, 0.34, (s * 0.66, -0.04, BZ + 0.08), 'M_Kit_MP_Dark', segs=8, bevel=0)
    p.append(rot(arm, y=s * 55))
    p.append(SP(f'Mitt{s}', 0.1, (s * 0.8, -0.08, BZ + 0.2), 'M_Kit_MP_Mask', segs=14, rings=8, squash=(1, 0.9, 1.1)))
# tattered shadow scarf trailing behind
hem = [(-0.5, BZ + 0.25), (0.5, BZ + 0.25), (0.62, BZ - 0.35), (0.42, BZ - 0.22), (0.3, BZ - 0.5), (0.12, BZ - 0.3), (0.0, BZ - 0.58), (-0.12, BZ - 0.3), (-0.3, BZ - 0.5), (-0.42, BZ - 0.22), (-0.62, BZ - 0.35)]
cape = flat_shape('ShadowCape', hem, 0.04, 'M_Kit_MP_Dark', 0.26, smooth_n=2, bulge=-0.12)
sc = [cape]
p += sc
p.append(rot(K.torus('Collar', 0.5, 0.06, (0, 0.12, BZ - 0.08), 'M_Kit_MP_Dark', segs=32, rsegs=6), x=90))
K.join(p, 'MINION_Tick'); K.export('MINION_Tick')
