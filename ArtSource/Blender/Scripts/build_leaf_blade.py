"""
PIP_LeafBlade — Pip's sword (M14 model pass): a long apple-leaf blade with a lighter midrib and side veins, a gold
star guard, a twine-wrapped stem grip and an acorn-cap pommel. Grip centre at the origin, blade along Blender +Z
(Unity +Y after export) so PlayerCombat's pivot "up" is the blade direction. ~1.35 m.
run: python3 -I build_leaf_blade.py   → Assets/DreamRush/Resources/DreamRush/Weapons/PIP_LeafBlade.fbx
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402
import dr_bkit as K  # noqa: E402

K.set_out(os.path.join('Assets', 'DreamRush', 'Resources', 'DreamRush', 'Weapons'), os.path.join('ArtSource', 'Blender', 'Characters'))
K.reset()
p = []
# blade: lofted leaf outline, thick at the midrib, thin serrated edges, slight curve
L, W = 1.05, 0.17
bm = bmesh.new(); rows = []
N, M = 28, 9
for i in range(N + 1):
    t = i / N
    w = W * (math.sin(min(1.0, t * 1.08) * math.pi) ** 0.7) * (1 - 0.15 * t) + 0.02 * (1 - t) + 0.002
    serr = 1.0 + 0.06 * (1 if i % 3 == 0 else 0) * (t > 0.15)
    row = []
    for j in range(M):
        s = (j / (M - 1)) * 2 - 1
        x = s * w * serr
        thick = 0.028 * (1 - abs(s) ** 1.6) + 0.003
        z = 0.12 + t * L
        y = 0.04 * math.sin(t * math.pi) * (1 - 0.3 * abs(s))          # gentle curve forward
        row.append((x, y, z, thick))
    rows.append(row)
front = [[bm.verts.new((x, y - th, z)) for x, y, z, th in r] for r in rows]
back = [[bm.verts.new((x, y + th, z)) for x, y, z, th in r] for r in rows]
for i in range(N):
    for j in range(M - 1):
        bm.faces.new((front[i][j], front[i][j + 1], front[i + 1][j + 1], front[i + 1][j]))
        bm.faces.new((back[i][j + 1], back[i][j], back[i + 1][j], back[i + 1][j + 1]))
for i in range(N):
    for j in (0, M - 1):
        bm.faces.new((front[i][j], front[i + 1][j], back[i + 1][j], back[i][j]) if j == 0 else (front[i + 1][j], front[i][j], back[i][j], back[i + 1][j]))
bm.faces.new([front[0][j] for j in range(M)] + [back[0][j] for j in reversed(range(M))])
bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-4)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
blade = K.from_bm('Blade', bm, 'M_Blade_Leaf'); p.append(blade)


def tube(name, pts, radii, mat, segs=8):
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


# midrib + veins (raised, lighter) on both faces
for side in (-1, 1):
    rib = [(0, side * 0.032 + 0.04 * math.sin(t * math.pi), 0.12 + t * L * 0.84) for t in [k / 10 for k in range(11)]]
    p.append(tube(f'Rib{side}', rib, [0.012 - 0.008 * k / 10 for k in range(11)], 'M_Blade_Vein', segs=6))
    for k in range(5):
        t0 = 0.15 + k * 0.15; z0 = 0.12 + t0 * L
        for sx in (-1, 1):
            vein = [(0, side * 0.03 + 0.04 * math.sin(t0 * math.pi), z0), (sx * 0.07, side * 0.022 + 0.04 * math.sin(t0 * math.pi), z0 + 0.09)]
            p.append(tube(f'Vein{side}{k}{sx}', vein, [0.006, 0.003], 'M_Blade_Vein', segs=5))
# gold star guard, grip, pommel
guard = K.star('Guard', 0.12, 0.055, 0.05, (0, 0, 0.1), 'M_Blade_Gold', bevel=0.01); guard.rotation_euler = (math.radians(90), 0, 0); p.append(guard)
p.append(K.cyl('Grip', 0.03, 0.028, 0.24, (0, 0, -0.03), 'M_Blade_Grip', segs=12, bevel=0.005))
for k in range(5): p.append(K.torus(f'Wrap{k}', 0.032, 0.008, (0, 0, -0.12 + k * 0.045), 'M_Blade_Twine', segs=14, rsegs=5))
p.append(K.uv_sphere('Pommel', 0.05, (0, 0, -0.17), 'M_Blade_Gold', segs=14, rings=8, squash=(1, 1, 0.8)))
p.append(K.uv_sphere('PommelCap', 0.045, (0, 0, -0.205), 'M_Blade_Grip', segs=14, rings=8, squash=(1.1, 1.1, 0.5)))
K.join(p, 'PIP_LeafBlade'); K.export('PIP_LeafBlade')
