"""
NPC_Clockfolk — the Midnight Palace's sleeping residents (M15). A chubby dreamer in pyjamas whose head is a pocket watch:
cream dial face with sleepy half-lidded eyes and a small smile, gold rim + crown winder, a floppy nightcap with a pompom,
stubby arms, fluffy slippers. The pyjama material is tinted per resident in Unity (ClockfolkLook). Origin = feet, 1.35 m.
run: python3 -I build_clockfolk.py
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere
K.reset()


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def tube(name, pts, radii, mat, segs=10):
    bm = bmesh.new(); rings = []
    for i, q in enumerate(pts):
        q = Vector(q); d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a).normalized()
        rings.append([bm.verts.new(q + (a * math.cos(t) + b * math.sin(t)) * radii[i]) for t in [k / segs * 2 * math.pi for k in range(segs)]])
    for i in range(len(rings) - 1):
        for k in range(segs): bm.faces.new((rings[i][k], rings[i][(k + 1) % segs], rings[i + 1][(k + 1) % segs], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return K.from_bm(name, bm, mat)


p = []
# body: pear-shaped pyjama with a lighter collar and three buttons
p.append(SP('Body', 1.0, (0, 0, 0.42), 'M_Kit_MP_Pyjama', segs=32, rings=16, squash=(0.34, 0.3, 0.36)))
p.append(K.torus('Collar', 0.2, 0.05, (0, 0, 0.74), 'M_Kit_MP_Mask', segs=28, rsegs=8))
for k in range(3): p.append(SP(f'Btn{k}', 0.035, (0, -0.3 + k * 0.012, 0.58 - k * 0.12), 'M_Kit_MP_Gold', segs=10, rings=6))
p.append(SB('Pocket', (0.16, 0.03, 0.12), (0.15, -0.29, 0.36), 'M_Kit_MP_Mask', bevel=0.02, segs=2))
# head: a pocket watch on its edge facing front
HZ = 1.0
head = CY('Watch', 0.3, 0.3, 0.2, (0, 0, HZ), 'M_Kit_MP_Gold', segs=40, bevel=0.05); p.append(rot(head, x=90))
p.append(rot(CY('Face', 0.26, 0.26, 0.02, (0, -0.1, HZ), 'M_Kit_MP_Face', segs=40, bevel=0.005), x=90))
for k in range(12):
    a = k / 12 * 2 * math.pi
    t = SB(f'Tick{k}', (0.015, 0.01, 0.04 if k % 3 == 0 else 0.025), (math.sin(a) * 0.22, -0.115, HZ + math.cos(a) * 0.22), 'M_Kit_MP_Dark', bevel=0.004, segs=1)
    p.append(rot(t, y=-math.degrees(a)))
p.append(CY('Crown', 0.05, 0.05, 0.08, (0, 0, HZ + 0.34), 'M_Kit_MP_Gold', segs=12, bevel=0.01))
p.append(rot(K.torus('Bow', 0.06, 0.015, (0, 0, HZ + 0.42), 'M_Kit_MP_Gold', segs=16, rsegs=6), x=90))
# sleepy face: half-lidded eyes (dark arcs + lids), rosy cheeks, small smile
for s in (-1, 1):
    p.append(SP(f'Eye{s}', 1.0, (s * 0.09, -0.115, HZ + 0.03), 'M_Kit_MP_Dark', segs=14, rings=8, squash=(0.05, 0.02, 0.035)))
    p.append(SP(f'Lid{s}', 1.0, (s * 0.09, -0.12, HZ + 0.05), 'M_Kit_MP_Face', segs=14, rings=8, squash=(0.058, 0.022, 0.03)))
    p.append(SP(f'Cheek{s}', 1.0, (s * 0.16, -0.112, HZ - 0.06), 'M_Kit_MP_Red', segs=12, rings=6, squash=(0.04, 0.012, 0.025)))
p.append(tube('Smile', [(-0.05, -0.118, HZ - 0.07), (0, -0.12, HZ - 0.095), (0.05, -0.118, HZ - 0.07)], [0.01, 0.012, 0.01], 'M_Kit_MP_Dark', segs=6))
# floppy nightcap with pompom (pyjama colour)
cap = tube('Cap', [(0.0, 0.05, HZ + 0.25), (0.06, 0.08, HZ + 0.45), (0.2, 0.12, HZ + 0.55), (0.32, 0.12, HZ + 0.45)], [0.2, 0.13, 0.07, 0.03], 'M_Kit_MP_Pyjama', segs=16)
p.append(cap)
p.append(K.torus('CapBand', 0.21, 0.04, (0.0, 0.05, HZ + 0.25), 'M_Kit_MP_Mask', segs=28, rsegs=8))
p.append(SP('Pompom', 0.07, (0.33, 0.12, HZ + 0.41), 'M_Kit_MP_Mask', segs=14, rings=8))
# arms (stubby, sleeves + mitts) and slippers
for s in (-1, 1):
    p.append(tube(f'Arm{s}', [(s * 0.3, 0, 0.6), (s * 0.42, -0.03, 0.45), (s * 0.44, -0.08, 0.32)], [0.08, 0.07, 0.065], 'M_Kit_MP_Pyjama', segs=12))
    p.append(SP(f'Mitt{s}', 0.07, (s * 0.45, -0.1, 0.27), 'M_Kit_MP_Mask', segs=14, rings=8))
    p.append(SP(f'Slipper{s}', 1.0, (s * 0.15, -0.05, 0.06), 'M_Kit_MP_Feather', segs=18, rings=10, squash=(0.11, 0.17, 0.07)))
    p.append(SP(f'Fluff{s}', 0.055, (s * 0.15, -0.18, 0.1), 'M_Kit_MP_Mask', segs=12, rings=8))
K.join(p, 'NPC_Clockfolk'); K.export('NPC_Clockfolk')
