"""
MIDNIGHT PALACE kit (level 2, docs/LEVEL2_DESIGN.md §5). Night city of clocks: hero CLOCK_Tower, rooftop clutter,
street furniture, gear platforms, the TICK-TOCK minion, the stolen-hour shard and set-piece props (sky whale, paper
plane, sky tram). Conventions: one FBX per prop, origin at the floor centre, front = Blender -Y (= Unity +Z),
materials M_Kit_MP_* remapped to DreamToon in Unity (MidnightPalaceBuilder).
run: python3 -I build_city_kit.py
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Art', 'MidnightPalace', 'Models'), os.path.join('ArtSource', 'Blender', 'MidnightPalace'))
SB, CY, SP = K.soft_box, K.cyl, K.uv_sphere


def rot(o, x=0, y=0, z=0):
    o.rotation_euler = (math.radians(x), math.radians(y), math.radians(z)); return o


def finish(name, parts):
    K.join(parts, name); K.export(name)


def gear(name, r, thick, teeth, center, mat, hole=0.35):
    """flat gear lying in XY (axis Z): toothed ring + spokes + hub."""
    bm = bmesh.new()
    n = teeth * 4
    top, bot = [], []
    for k in range(n):
        a = k / n * 2 * math.pi
        rr = r * (1.0 if (k % 4) in (1, 2) else 0.86)
        top.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, thick / 2)))
        bot.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, -thick / 2)))
    ti, bi = [], []
    for k in range(n):
        a = k / n * 2 * math.pi
        ti.append(bm.verts.new((math.cos(a) * r * hole, math.sin(a) * r * hole, thick / 2)))
        bi.append(bm.verts.new((math.cos(a) * r * hole, math.sin(a) * r * hole, -thick / 2)))
    for k in range(n):
        j = (k + 1) % n
        bm.faces.new((top[k], top[j], ti[j], ti[k])); bm.faces.new((bot[j], bot[k], bi[k], bi[j]))
        bm.faces.new((top[j], top[k], bot[k], bot[j])); bm.faces.new((ti[k], ti[j], bi[j], bi[k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = K.from_bm(name, bm, mat); o.location = center
    hub = CY(name + 'Hub', r * 0.18, r * 0.18, thick * 1.4, center, mat, segs=20, bevel=0.02)
    parts = [o, hub]
    for s in range(4):
        sp = SB(f'{name}Spoke{s}', (r * 0.66, r * 0.09, thick * 0.8), (0, 0, 0), mat, bevel=0.02, segs=1)
        sp.location = (center[0] + math.cos(s * math.pi / 2) * r * 0.5, center[1] + math.sin(s * math.pi / 2) * r * 0.5, center[2])
        rot(sp, z=s * 90)
        parts.append(sp)
    return K.join(parts, name)


# ===================================================================== CLOCK_Tower (24 × 24 base, 64 m + spire)
K.reset()
p = []
p.append(SB('Plinth', (26, 26, 4), (0, 0, 2), 'M_Kit_MP_Stone', bevel=0.3))
p.append(SB('Shaft1', (22, 22, 20), (0, 0, 14), 'M_Kit_MP_Brick', bevel=0.2))
p.append(SB('Ledge1', (24, 24, 1.2), (0, 0, 24.6), 'M_Kit_MP_Stone', bevel=0.2))
p.append(SB('Shaft2', (19, 19, 18), (0, 0, 34), 'M_Kit_MP_Brick', bevel=0.2))
p.append(SB('Ledge2', (21, 21, 1.2), (0, 0, 43.6), 'M_Kit_MP_Stone', bevel=0.2))
p.append(SB('ClockRoom', (18, 18, 14), (0, 0, 51), 'M_Kit_MP_Dark', bevel=0.3))
for side in range(4):   # four giant clock faces + gold rims + hands at 11:59
    a = side * math.pi / 2
    n = Vector((math.sin(a), -math.cos(a), 0))
    c = Vector((0, 0, 51)) + n * 9.05
    face = rot(CY(f'Face{side}', 6.4, 6.4, 0.4, c, 'M_Kit_MP_Face', segs=64, bevel=0.05), x=90, z=math.degrees(a))
    rim = rot(K.torus(f'Rim{side}', 6.6, 0.45, c + n * 0.1, 'M_Kit_MP_Gold', segs=64, rsegs=10), x=90, z=math.degrees(a))
    p += [face, rim]
    for k in range(12):
        ang = k / 12 * 2 * math.pi
        t = SB(f'Tick{side}_{k}', (0.5 if k % 3 else 0.9, 0.2, 1.4 if k % 3 == 0 else 0.9), (0, 0, 0), 'M_Kit_MP_Dark', bevel=0.05, segs=1)
        lat = Vector((math.cos(a), math.sin(a), 0))
        t.location = c + n * 0.25 + lat * math.sin(ang) * 5.3 + Vector((0, 0, math.cos(ang) * 5.3))
        rot(t, z=math.degrees(a), y=-math.degrees(ang) if side % 2 == 0 else math.degrees(ang))
        p.append(t)
    lat = Vector((math.cos(a), math.sin(a), 0))
    hh = SB(f'HourHand{side}', (0.8, 0.25, 4.2), c + n * 0.45 + Vector((0, 0, 2.0)) - lat * 0.2, 'M_Kit_MP_Dark', bevel=0.08, segs=1)
    mh = SB(f'MinHand{side}', (0.5, 0.25, 5.6), c + n * 0.55 + Vector((0, 0, 2.7)) - lat * 0.1, 'M_Kit_MP_Dark', bevel=0.06, segs=1)
    rot(hh, z=math.degrees(a)); rot(mh, z=math.degrees(a))
    p += [hh, mh, rot(CY(f'Hub{side}', 0.7, 0.7, 0.6, c + n * 0.5, 'M_Kit_MP_Gold', segs=24), x=90, z=math.degrees(a))]
p.append(SB('Crown', (20, 20, 1.6), (0, 0, 58.8), 'M_Kit_MP_Gold', bevel=0.3))
for sx in (-1, 1):   # four corner turrets
    for sy in (-1, 1):
        p.append(CY(f'Turret{sx}{sy}', 1.6, 1.6, 12, (sx * 10, sy * 10, 52), 'M_Kit_MP_Brick', segs=16, bevel=0.1))
        p.append(CY(f'TurretCap{sx}{sy}', 2.0, 0.0, 4.5, (sx * 10, sy * 10, 60.2), 'M_Kit_MP_Roof', segs=16, bevel=0))
p.append(CY('Spire', 10.5, 0.4, 16, (0, 0, 67.5), 'M_Kit_MP_Roof', segs=4, bevel=0.1))
p.append(SP('SpireOrb', 1.4, (0, 0, 76.5), 'M_Kit_MP_Glow', segs=20, rings=12))
for side in range(4):   # lit arched windows down the shaft (emissive slabs)
    a = side * math.pi / 2; n = Vector((math.sin(a), -math.cos(a), 0)); lat = Vector((math.cos(a), math.sin(a), 0))
    for row in range(5):
        z = 8 + row * 7.5
        w = 11 if z < 24 else 9.5
        for col in (-1, 0, 1):
            win = SB(f'Win{side}_{row}_{col}', (1.6, 0.3, 3.4), Vector((0, 0, z)) + n * (w + 0.05) + lat * col * 4.5, 'M_Kit_MP_Window', bevel=0.08, segs=1)
            rot(win, z=math.degrees(a)); p.append(win)
finish('CLOCK_Tower', p)

# ===================================================================== rooftop + street props
K.reset()   # WATER_Tank
p = [CY('Tank', 1.6, 1.6, 3.0, (0, 0, 4.2), 'M_Kit_MP_Wood', segs=24, bevel=0.06), CY('Roof', 1.75, 0.2, 1.2, (0, 0, 6.3), 'M_Kit_MP_Roof', segs=24, bevel=0)]
for k in range(3): p.append(K.torus(f'Band{k}', 1.62, 0.05, (0, 0, 3.1 + k * 1.1), 'M_Kit_MP_Dark', segs=32, rsegs=6))
for sx in (-1, 1):
    for sy in (-1, 1): p.append(CY(f'Leg{sx}{sy}', 0.12, 0.12, 2.8, (sx * 1.1, sy * 1.1, 1.4), 'M_Kit_MP_Dark', segs=8, bevel=0))
finish('WATER_Tank', p)

K.reset()   # ANTENNA
p = [CY('Mast', 0.08, 0.05, 6, (0, 0, 3), 'M_Kit_MP_Dark', segs=8, bevel=0)]
for k in range(4): p.append(rot(CY(f'Bar{k}', 0.03, 0.03, 1.8 - k * 0.3, (0, 0, 3.5 + k * 0.7), 'M_Kit_MP_Dark', segs=6, bevel=0), y=90))
p.append(SP('Blink', 0.15, (0, 0, 6.1), 'M_Kit_MP_Red', segs=12, rings=8))
finish('ANTENNA', p)

K.reset()   # AC_Unit
p = [SB('Box', (1.4, 1.0, 1.0), (0, 0, 0.5), 'M_Kit_MP_Metal', bevel=0.06), rot(CY('Fan', 0.35, 0.35, 0.05, (0, -0.51, 0.5), 'M_Kit_MP_Dark', segs=20, bevel=0), x=90)]
finish('AC_Unit', p)

K.reset()   # STREET_Lamp (curly post with a little clock)
p = [CY('Base', 0.3, 0.35, 0.4, (0, 0, 0.2), 'M_Kit_MP_Dark', segs=16, bevel=0.03), CY('Post', 0.1, 0.08, 4.8, (0, 0, 2.6), 'M_Kit_MP_Dark', segs=12, bevel=0)]
p.append(rot(K.torus('Curl', 0.45, 0.07, (0.45, 0, 5.0), 'M_Kit_MP_Dark', segs=24, rsegs=6), x=90))
p.append(SP('Lamp', 0.32, (0.9, 0, 4.7), 'M_Kit_MP_LampGlow', segs=16, rings=10, squash=(1, 1, 1.2)))
p.append(CY('Hood', 0.45, 0.2, 0.25, (0.9, 0, 5.05), 'M_Kit_MP_Dark', segs=16, bevel=0.02))
p.append(rot(CY('Clock', 0.36, 0.36, 0.14, (0, -0.0, 3.4), 'M_Kit_MP_Face', segs=24, bevel=0.02), x=90))
finish('STREET_Lamp', p)

K.reset()   # VENDING machine (glowing front)
p = [SB('Body', (1.1, 0.8, 2.0), (0, 0, 1.0), 'M_Kit_MP_Red', bevel=0.06), SB('Panel', (0.9, 0.05, 1.2), (0, -0.4, 1.25), 'M_Kit_MP_Window', bevel=0.03)]
for k in range(4): p.append(SB(f'Btn{k}', (0.12, 0.04, 0.12), (0.25, -0.42, 0.5 + k * 0.0) if k == 0 else (0.25, -0.42, 0.4 + k * 0.12), 'M_Kit_MP_Gold', bevel=0.02, segs=1))
finish('VENDING', p)

# ===================================================================== GEAR_Platform (r 3.2) and HOUR_Shard
K.reset(); finish('GEAR_Platform', [gear('Gear', 3.2, 0.6, 14, (0, 0, -0.3), 'M_Kit_MP_Gold')])
K.reset()
shard = SB('Shard', (0.35, 0.18, 1.6), (0, 0, 0.8), 'M_Kit_MP_Shard', bevel=0.06, segs=2)
bm = bmesh.new(); bm.from_mesh(shard.data)
for v in bm.verts:
    t = (v.co.z - 0.0) / 1.6
    v.co.x *= 1.0 - 0.85 * max(0, (t - 0.55) / 0.45)
bm.to_mesh(shard.data); bm.free()
finish('HOUR_Shard', [shard, K.torus('Ring', 0.45, 0.05, (0, 0, 0.35), 'M_Kit_MP_Gold', segs=32, rsegs=8)])

# ===================================================================== MINION_Tick (1.5 m alarm-clock shadow in a mask)
K.reset()
p = [rot(CY('Body', 0.55, 0.55, 0.42, (0, 0, 0.95), 'M_Kit_MP_MinionBody', segs=32, bevel=0.08), x=90)]
p.append(rot(CY('Face', 0.46, 0.46, 0.05, (0, -0.22, 0.95), 'M_Kit_MP_Face', segs=32, bevel=0.01), x=90))
for s in (-1, 1):
    p.append(SP(f'Bell{s}', 0.26, (s * 0.36, 0, 1.55), 'M_Kit_MP_Gold', segs=16, rings=10, squash=(1, 1, 0.75)))
    p.append(CY(f'Leg{s}', 0.06, 0.05, 0.55, (s * 0.25, 0, 0.3), 'M_Kit_MP_Dark', segs=8, bevel=0))
    p.append(SP(f'Foot{s}', 0.12, (s * 0.25, -0.06, 0.05), 'M_Kit_MP_Dark', segs=12, rings=8, squash=(1, 1.5, 0.6)))
    p.append(SB(f'Eye{s}', (0.16, 0.04, 0.07), (s * 0.15, -0.27, 1.05), 'M_Kit_MP_Eye', bevel=0.02, segs=1))
p.append(SB('Mask', (0.68, 0.05, 0.2), (0, -0.25, 1.05), 'M_Kit_MP_Mask', bevel=0.06, segs=2))
p.append(SB('Handle', (0.7, 0.08, 0.08), (0, 0, 1.75), 'M_Kit_MP_Gold', bevel=0.03, segs=1))
p.append(rot(SB('Hand', (0.06, 0.03, 0.34), (0.05, -0.29, 0.9), 'M_Kit_MP_Dark', bevel=0.01, segs=1), y=35))
finish('MINION_Tick', p)

# ===================================================================== set pieces
K.reset()   # SKY_Whale (made of gears and a night-sky skin), 28 m
p = [SP('Body', 1.0, (0, 0, 0), 'M_Kit_MP_Whale', segs=40, rings=20, squash=(4.5, 14, 4))]
p.append(SP('Belly', 1.0, (0, -1, -1.6), 'M_Kit_MP_WhaleBelly', segs=32, rings=16, squash=(3.6, 11, 2.6)))
for s in (-1, 1):
    p.append(rot(SP(f'Fin{s}', 1.0, (s * 5.0, -3, -1.5), 'M_Kit_MP_Whale', segs=20, rings=10, squash=(4, 1.8, 0.4)), y=s * 25))
    p.append(SP(f'Eye{s}', 0.6, (s * 3.9, -8.5, 0.6), 'M_Kit_MP_Glow', segs=16, rings=10))
p.append(rot(SP('Tail', 1.0, (0, 14.5, 1.2), 'M_Kit_MP_Whale', segs=20, rings=10, squash=(6, 2.2, 0.5)), x=12))
for k in range(5):
    g = gear(f'G{k}', 1.4 + (k % 2) * 0.6, 0.4, 10, (0, 0, 0), 'M_Kit_MP_Gold')
    g.location = (0, -6 + k * 3.5, 3.7 - abs(k - 2) * 0.3); rot(g, y=90 if k % 2 else 0); p.append(g)
finish('SKY_Whale', p)

K.reset()   # PAPER_Plane
bm = bmesh.new()
v = [bm.verts.new(c) for c in [(0, -1.0, 0), (-0.75, 0.6, 0.05), (0, 0.45, -0.18), (0.75, 0.6, 0.05)]]
bm.faces.new((v[0], v[1], v[2])); bm.faces.new((v[0], v[2], v[3]))
o = K.from_bm('Plane', bm, 'M_Kit_MP_Paper')
sol = o.modifiers.new('s', 'SOLIDIFY'); sol.thickness = 0.02; K.apply_mods(o)
finish('PAPER_Plane', [o])

K.reset()   # SKY_Tram (yellow sky-rail car with a P5-ish red stripe)
p = [SB('Car', (2.6, 7.0, 2.6), (0, 0, 1.6), 'M_Kit_MP_Tram', bevel=0.35)]
p.append(SB('Stripe', (2.65, 7.05, 0.3), (0, 0, 1.2), 'M_Kit_MP_Red', bevel=0.1))
for s in (-1, 1):
    for k in range(4): p.append(SB(f'Win{s}{k}', (0.06, 1.2, 0.9), (s * 1.3, -2.4 + k * 1.6, 2.1), 'M_Kit_MP_Window', bevel=0.05, segs=1))
p.append(SB('Front', (2.0, 0.06, 1.1), (0, -3.52, 2.1), 'M_Kit_MP_Window', bevel=0.05, segs=1))
for s in (-1, 1): p.append(SP(f'Light{s}', 0.18, (s * 0.8, -3.55, 0.9), 'M_Kit_MP_LampGlow', segs=12, rings=8))
p.append(SB('Hanger', (0.3, 0.3, 1.2), (0, 0, 3.4), 'M_Kit_MP_Dark', bevel=0.05))
finish('SKY_Tram', p)
print('CITY KIT DONE')
