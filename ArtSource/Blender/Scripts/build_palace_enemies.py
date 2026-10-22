"""
Midnight Palace enemy roster (M14, user 2026-10-20: "the monsters all look the same and I fight the same one").
Each one reads differently by SILHOUETTE first, colour second:
  BRUTE_Bell     2.6 m  wide + heavy: a navy alarm-clock tank with a brass shield-dial face, angry brows, twin bells
                        and hammer fists. (origin: feet)
  ROLLER_Gear    1.7 m  round + spiky: a brass gear on its edge with a cyclops eye in the hub. (origin: GEAR CENTRE —
                        the game spins it around local X; the builder lifts it by its radius)
  CUCKOO_Sniper  1.4 m  small + winged: a cuckoo-clock house with a bird leaning out, beak, purple wings, pine-cone weights.
                        (origin: body centre — it hovers)
Front = Blender -Y (Unity +Z). Materials M_Kit_MP_* → Unity (MidnightPalaceBuilder remaps them).
run: python3 -I build_palace_enemies.py
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def tube(name, pts, radii, material, segs=10):
    bm = bmesh.new(); rings = []
    for i, p in enumerate(pts):
        p = Vector(p); d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a).normalized()
        rings.append([bm.verts.new(p + (a * math.cos(t) + b * math.sin(t)) * radii[i]) for t in [k / segs * 2 * math.pi for k in range(segs)]])
    for i in range(len(rings) - 1):
        for k in range(segs):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % segs], rings[i + 1][(k + 1) % segs], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return K.from_bm(name, bm, material)


def gear(name, r, thick, teeth, center, mat, hole=0.3):
    bm = bmesh.new(); n = teeth * 4; top, bot = [], []
    for k in range(n):
        a = k / n * 2 * math.pi; rr = r * (1.0 if (k % 4) in (1, 2) else 0.84)
        top.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, thick / 2))); bot.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, -thick / 2)))
    bm.faces.new(top); bm.faces.new(list(reversed(bot)))
    for k in range(n): bm.faces.new((bot[k], bot[(k + 1) % n], top[(k + 1) % n], top[k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = K.from_bm(name, bm, mat); o.location = center
    b = o.modifiers.new('bev', 'BEVEL'); b.width = 0.02; b.segments = 2; b.limit_method = 'ANGLE'; K.apply_mods(o)
    return o


# ===================================================================== BELL BRUTE
K.reset()
p = []
BZ = 1.45
body = CY('Case', 1.05, 1.05, 1.0, (0, 0, BZ), 'M_Kit_MP_Brute', segs=48, bevel=0.16); rot(body, x=90); p.append(body)
p.append(rot(K.torus('Rim', 1.0, 0.12, (0, -0.5, BZ), 'M_Kit_MP_Gold', segs=56, rsegs=10), x=90))
# brass shield dial (slightly domed) with ticks
shield = SP('Shield', 1.0, (0, -0.52, BZ), 'M_Kit_MP_Brass', segs=40, rings=20, squash=(0.9, 0.18, 0.9)); p.append(shield)
for k in range(12):
    a = k / 12 * 2 * math.pi; big = k % 3 == 0
    tk = SB(f'Tick{k}', (0.07 if big else 0.045, 0.04, 0.2 if big else 0.12), (math.sin(a) * 0.72, -0.68, BZ + math.cos(a) * 0.72), 'M_Kit_MP_Dark', bevel=0.01, segs=1)
    p.append(rot(tk, y=-math.degrees(a)))
# angry face on the shield: slanted brows, glowing slit eyes, gritted grille mouth
for s in (-1, 1):
    p.append(rot(SB(f'Brow{s}', (0.42, 0.08, 0.1), (s * 0.3, -0.72, BZ + 0.32), 'M_Kit_MP_Dark', bevel=0.03, segs=2), y=s * -22))
    p.append(rot(SB(f'Eye{s}', (0.3, 0.06, 0.09), (s * 0.3, -0.71, BZ + 0.15), 'M_Kit_MP_EyeRed', bevel=0.03, segs=2), y=s * -14))
p.append(SB('Grille', (0.7, 0.07, 0.24), (0, -0.7, BZ - 0.32), 'M_Kit_MP_Dark', bevel=0.05, segs=2))
for k in range(5): p.append(SB(f'Tooth{k}', (0.06, 0.08, 0.2), (-0.24 + k * 0.12, -0.74, BZ - 0.32), 'M_Kit_MP_Mask', bevel=0.01, segs=1))
# twin bells + hammer + handle
for s in (-1, 1):
    bell = SP(f'Bell{s}', 0.48, (s * 0.68, 0.0, BZ + 1.12), 'M_Kit_MP_Gold', segs=32, rings=16, squash=(1, 1, 0.72)); p.append(rot(bell, y=s * 30))
    p.append(SP(f'BellKnob{s}', 0.09, (s * 0.92, 0.0, BZ + 1.5), 'M_Kit_MP_Gold', segs=12, rings=8))
    p.append(rot(CY(f'BellPost{s}', 0.06, 0.06, 0.4, (s * 0.46, 0.0, BZ + 0.92), 'M_Kit_MP_Gold', segs=10, bevel=0), y=s * 30))
p.append(CY('HammerRod', 0.04, 0.04, 0.5, (0, 0.08, BZ + 1.2), 'M_Kit_MP_Dark', segs=8, bevel=0))
p.append(SP('Hammer', 0.12, (0, 0.08, BZ + 1.48), 'M_Kit_MP_Dark', segs=12, rings=8, squash=(1.7, 1, 1)))
# thick legs + boots, big arms with hammer fists
for s in (-1, 1):
    p.append(tube(f'Leg{s}', [(s * 0.5, 0.05, 0.6), (s * 0.56, 0.05, 0.3), (s * 0.58, 0.0, 0.1)], [0.2, 0.19, 0.2], 'M_Kit_MP_Dark', segs=12))
    p.append(SP(f'Boot{s}', 1.0, (s * 0.6, -0.12, 0.12), 'M_Kit_MP_Dark', segs=20, rings=10, squash=(0.3, 0.45, 0.16)))
    p.append(K.torus(f'BootCuff{s}', 0.22, 0.05, (s * 0.58, 0.02, 0.28), 'M_Kit_MP_Gold', segs=20, rsegs=6))
    sh = (s * 1.08, 0.0, BZ + 0.3)
    p.append(SP(f'Shoulder{s}', 0.3, sh, 'M_Kit_MP_Gold', segs=18, rings=10))
    p.append(tube(f'Arm{s}', [sh, (s * 1.42, -0.1, BZ - 0.1), (s * 1.5, -0.3, BZ - 0.55)], [0.17, 0.15, 0.15], 'M_Kit_MP_Brute', segs=12))
    fist = CY(f'Fist{s}', 0.3, 0.3, 0.5, (s * 1.52, -0.36, BZ - 0.78), 'M_Kit_MP_Brass', segs=20, bevel=0.06); p.append(rot(fist, y=90))
    p.append(K.torus(f'FistBand{s}', 0.3, 0.05, (s * 1.52, -0.36, BZ - 0.78), 'M_Kit_MP_Dark', segs=24, rsegs=6))
    p.append(rot(K.torus(f'FistBand2{s}', 0.3, 0.05, (s * 1.52, -0.36, BZ - 0.78), 'M_Kit_MP_Dark', segs=24, rsegs=6), y=90))
# back: wind-up key
p.append(rot(CY('KeyStem', 0.07, 0.07, 0.4, (0, 0.65, BZ), 'M_Kit_MP_Gold', segs=10, bevel=0), x=90))
for s in (-1, 1): p.append(rot(SP(f'KeyWing{s}', 1.0, (s * 0.24, 0.88, BZ), 'M_Kit_MP_Gold', segs=16, rings=8, squash=(0.24, 0.05, 0.18)), y=0))
K.join(p, 'BRUTE_Bell'); K.export('BRUTE_Bell')

# ===================================================================== GEAR ROLLER (origin = gear centre, rolls around X)
K.reset()
p = []
g = gear('Gear', 0.86, 0.42, 12, (0, 0, 0), 'M_Kit_MP_Gold'); rot(g, y=90); p.append(g)     # disc in the YZ plane: rolls forward (-Y)
for k in range(12):   # spikes on every tooth
    a = (k + 0.5) / 12 * 2 * math.pi
    sp = CY(f'Spike{k}', 0.07, 0.0, 0.22, (0, 0, 0), 'M_Kit_MP_Metal', segs=8, bevel=0)
    sp.location = (0, math.cos(a) * 0.95, math.sin(a) * 0.95); sp.rotation_euler = (a - math.pi / 2, 0, 0); p.append(sp)
for s in (-1, 1):
    hub = CY(f'Hub{s}', 0.42, 0.36, 0.14, (s * 0.27, 0, 0), 'M_Kit_MP_Brute', segs=32, bevel=0.03); p.append(rot(hub, y=90))
    p.append(rot(K.torus(f'HubRim{s}', 0.4, 0.04, (s * 0.3, 0, 0), 'M_Kit_MP_Dark', segs=32, rsegs=6), y=90))
    # cyclops eye on both faces (one always faces the camera somewhere)
    p.append(rot(SP(f'Eye{s}', 1.0, (s * 0.35, 0, 0), 'M_Kit_MP_Mask', segs=20, rings=10, squash=(0.08, 0.22, 0.22)), y=0))
    p.append(SP(f'Pupil{s}', 1.0, (s * 0.41, 0, 0.02), 'M_Kit_MP_EyeRed', segs=16, rings=8, squash=(0.05, 0.1, 0.14)))
    p.append(rot(SB(f'Lid{s}', (0.06, 0.5, 0.08), (s * 0.4, 0, 0.21), 'M_Kit_MP_Dark', bevel=0.02, segs=1), x=-12 * s))
for k in range(6):   # spokes (cut-out read)
    a = k / 6 * 2 * math.pi
    sp = SB(f'Spoke{k}', (0.48, 0.07, 0.08), (0, math.cos(a) * 0.62, math.sin(a) * 0.62), 'M_Kit_MP_Dark', bevel=0.01, segs=1); sp.rotation_euler = (a, 0, 0); p.append(sp)
K.join(p, 'ROLLER_Gear'); K.export('ROLLER_Gear')

# ===================================================================== CUCKOO SNIPER (origin = body centre)
K.reset()
p = []
p.append(SB('House', (0.8, 0.6, 0.8), (0, 0, 0), 'M_Kit_MP_Cuckoo', bevel=0.06, segs=3))
for s in (-1, 1):   # pitched roof
    rf = SB(f'Roof{s}', (0.62, 0.78, 0.07), (s * 0.27, 0, 0.6), 'M_Kit_MP_Roof', bevel=0.02, segs=1); p.append(rot(rf, y=s * 40))
p.append(SB('RoofRidge', (0.08, 0.82, 0.08), (0, 0, 0.78), 'M_Kit_MP_Gold', bevel=0.02, segs=1))
p.append(SP('Dial', 1.0, (0, -0.31, -0.12), 'M_Kit_MP_Face', segs=24, rings=12, squash=(0.24, 0.04, 0.24)))
p.append(rot(SB('Door', (0.36, 0.06, 0.3), (0, -0.33, 0.24), 'M_Kit_MP_Dark', bevel=0.03, segs=2), x=0))
# the bird leaning out of its door: head, beak, angry eye, crest
p.append(SP('BirdHead', 0.2, (0, -0.48, 0.28), 'M_Kit_MP_Feather', segs=20, rings=12))
beak = CY('Beak', 0.09, 0.0, 0.3, (0, -0.75, 0.25), 'M_Kit_MP_Orange', segs=12, bevel=0); p.append(rot(beak, x=-90))
for s in (-1, 1):
    p.append(SP(f'BEye{s}', 0.055, (s * 0.1, -0.62, 0.33), 'M_Kit_MP_EyeRed', segs=10, rings=6))
    p.append(rot(SB(f'BBrow{s}', (0.12, 0.03, 0.03), (s * 0.1, -0.63, 0.4), 'M_Kit_MP_Dark', bevel=0.01, segs=1), y=s * -25))
for k in range(3): p.append(rot(SB(f'Crest{k}', (0.04, 0.05, 0.2), (0, -0.44 + k * 0.06, 0.5), 'M_Kit_MP_Orange', bevel=0.015, segs=1), x=20 - k * 20))
# wings: big flat feathered blades on the sides
for s in (-1, 1):
    for k in range(4):
        w = SB(f'Wing{s}_{k}', (0.9 - k * 0.15, 0.06, 0.2), (s * (0.75 - k * 0.04), 0.05 + k * 0.03, 0.18 - k * 0.14), 'M_Kit_MP_Feather' if k % 2 == 0 else 'M_Kit_MP_Glow', bevel=0.04, segs=2)
        p.append(rot(w, y=s * (-18 + k * 10)))
# pendulum + pine-cone weights below
p.append(CY('PendRod', 0.025, 0.025, 0.55, (0, -0.1, -0.68), 'M_Kit_MP_Gold', segs=8, bevel=0))
p.append(rot(CY('PendBob', 0.13, 0.13, 0.04, (0, -0.1, -0.98), 'M_Kit_MP_Gold', segs=20, bevel=0.01), x=90))
for s in (-1, 1):
    p.append(CY(f'Chain{s}', 0.012, 0.012, 0.6, (s * 0.2, 0.1, -0.7), 'M_Kit_MP_Metal', segs=6, bevel=0))
    p.append(SP(f'Cone{s}', 1.0, (s * 0.2, 0.1, -1.08), 'M_Kit_MP_Wood', segs=12, rings=8, squash=(0.08, 0.08, 0.16)))
K.join(p, 'CUCKOO_Sniper'); K.export('CUCKOO_Sniper')
print('ENEMIES DONE')
