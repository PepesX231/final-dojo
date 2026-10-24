"""Clay: organic, sculpt-style modelling for headless Blender (bpy + numpy).

The workflow mirrors sculpting real clay instead of stacking primitives:

  1. Field   - rough the mass in with metaballs. Neighbouring blobs melt into each other
               (smooth union), negative blobs scoop clay out.
  2. Remesh  - voxel-remesh the melted mass into even, sculptable topology.
  3. Sculpt  - brush strokes on that surface: grab, inflate, crease, pinch, flatten,
               smooth (Taubin, no shrink), clay-texture noise and thumb prints.
  4. Finish  - paint regions into material slots, decimate to the game budget.

Everything is deterministic (seeded) so a character can be rebuilt from its script.
Never name a script after a stdlib module (e.g. inspect.py breaks `import bpy`).
"""
import bpy
import math
import numpy as np
from mathutils import Vector, Matrix, Quaternion

V3 = lambda x, y, z: np.array((x, y, z), float)
SURF = 0.575


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 24


def link(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


def mesh_from(ob):
    dg = bpy.context.evaluated_depsgraph_get()
    return bpy.data.meshes.new_from_object(ob.evaluated_get(dg))


# ------------------------------------------------------------------ field (metaball clay)
class Field:
    _n = 0

    def __init__(self, name, res=0.012):
        Field._n += 1
        tag = f"{name}Clay{Field._n:02d}"            # unique family name: fields never melt into each other
        self.mb = bpy.data.metaballs.new(tag)
        self.mb.resolution = res
        self.mb.render_resolution = res
        self.mb.threshold = 0.6
        self.ob = link(bpy.data.objects.new(tag, self.mb))

    def _el(self, kind, co, r, neg, stiff):
        # r is the visible surface radius; a stiffness-2 element at threshold .6 surfaces at ~.575 of its radius
        e = self.mb.elements.new(type=kind)
        e.co = tuple(co); e.radius = r / SURF; e.use_negative = neg; e.stiffness = stiff
        return e

    def ball(self, co, r, neg=False, stiff=2.0):
        return self._el('BALL', co, r, neg, stiff)

    def blob(self, co, r, size, rot=None, neg=False, stiff=2.0):
        """Ellipsoid. size = (sx, sy, sz) multipliers; rot = mathutils Quaternion/Euler."""
        e = self._el('ELLIPSOID', co, r, neg, stiff)
        e.size_x, e.size_y, e.size_z = size
        if rot is not None: e.rotation = rot.to_quaternion() if hasattr(rot, 'to_quaternion') else rot
        return e

    def rope(self, a, b, r, neg=False, stiff=2.0):
        """Capsule from a to b: limbs, stems, rolled clay snakes."""
        a, b = Vector(a), Vector(b)
        d = b - a
        e = self._el('CAPSULE', (a + b) / 2, r, neg, stiff)
        e.size_x = max(1e-4, d.length / 2)
        e.rotation = Vector((1, 0, 0)).rotation_difference(d.normalized())
        return e

    def chain(self, pts, r0, r1=None, stiff=2.0, step=None):
        """A rolled snake of clay through pts, radius tapering r0 -> r1."""
        r1 = r0 if r1 is None else r1
        n = len(pts) - 1
        for i in range(n):
            t = (i + 0.5) / n
            self.rope(pts[i], pts[i + 1], r0 + (r1 - r0) * t, stiff=stiff)

    def solid(self, voxel=0.011):
        me = mesh_from(self.ob)
        bpy.data.objects.remove(self.ob, do_unlink=True)
        return Sculpt.from_mesh(me, voxel)


# ------------------------------------------------------------------ sculpt (brush strokes on a mesh)
def smoothstep(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3 - 2 * t)


class Sculpt:
    def __init__(self, V, F):
        self.V = np.asarray(V, float)
        self.F = [list(f) for f in F]
        self.carve = np.zeros(len(self.V))    # accumulated crease depth: paints mouths/grooves later
        self._edges()

    @staticmethod
    def from_mesh(me, voxel=None):
        ob = link(bpy.data.objects.new('_tmp', me))
        if voxel:
            m = ob.modifiers.new('remesh', 'REMESH')
            m.mode = 'VOXEL'; m.voxel_size = voxel; m.adaptivity = 0.0; m.use_smooth_shade = True
            me2 = mesh_from(ob)
        else:
            me2 = me
        n = len(me2.vertices)
        V = np.empty(n * 3); me2.vertices.foreach_get('co', V)
        F = [tuple(p.vertices) for p in me2.polygons]
        bpy.data.objects.remove(ob, do_unlink=True)
        return Sculpt(V.reshape(-1, 3), F)

    def _edges(self):
        es = set()
        for f in self.F:
            for i in range(len(f)):
                a, b = f[i], f[(i + 1) % len(f)]
                es.add((a, b) if a < b else (b, a))
        self.E = np.array(sorted(es), int) if es else np.zeros((0, 2), int)
        self.deg = np.bincount(self.E.ravel(), minlength=len(self.V)).astype(float)
        self.deg[self.deg == 0] = 1

    # --- geometry queries
    def normals(self):
        N = np.zeros_like(self.V)
        for f in self.F:
            p = self.V[f]
            n = np.zeros(3)
            for i in range(len(f)):
                n += np.cross(p[i], p[(i + 1) % len(f)])
            N[f] += n
        L = np.linalg.norm(N, axis=1); L[L == 0] = 1
        return N / L[:, None]

    def _fast_normals(self):
        """Vectorised area-weighted normals for triangle/quad meshes."""
        N = np.zeros_like(self.V)
        tris = []
        for f in self.F:
            for i in range(1, len(f) - 1): tris.append((f[0], f[i], f[i + 1]))
        T = np.array(tris, int)
        a, b, c = self.V[T[:, 0]], self.V[T[:, 1]], self.V[T[:, 2]]
        n = np.cross(b - a, c - a)
        for k in range(3): np.add.at(N, T[:, k], n)
        L = np.linalg.norm(N, axis=1); L[L == 0] = 1
        return N / L[:, None]

    def surface(self, origin, direction):
        """First vertex hit by a ray from origin (approx): nearest vertex to the ray, front-most."""
        o = np.asarray(origin, float); d = np.asarray(direction, float); d = d / np.linalg.norm(d)
        rel = self.V - o
        t = rel @ d
        perp = np.linalg.norm(rel - np.outer(t, d), axis=1)
        score = perp + np.where(t < 0, 10, 0)
        near = np.argsort(score)[:12]
        i = near[np.argmin(t[near])]
        return self.V[i].copy()

    def falloff(self, c, r, scale=(1, 1, 1)):
        d = np.linalg.norm((self.V - np.asarray(c, float)) / np.asarray(scale, float), axis=1) / r
        return smoothstep(1.0 - d)

    # --- brushes
    def grab(self, c, r, delta, scale=(1, 1, 1)):
        self.V += self.falloff(c, r, scale)[:, None] * np.asarray(delta, float)

    def inflate(self, c, r, amount, scale=(1, 1, 1)):
        N = self._fast_normals()
        self.V += (self.falloff(c, r, scale) * amount)[:, None] * N

    def crease(self, c, r, depth, pinch=0.25, scale=(1, 1, 1)):
        """Press a fine tool into the clay: sharp falloff + pull the rims together."""
        N = self._fast_normals()
        f = self.falloff(c, r, scale)
        sharp = f ** 2.2
        self.V -= (sharp * depth)[:, None] * N
        self.V += (f * pinch)[:, None] * (np.asarray(c, float) - self.V) * 0.35
        self.carve += sharp * depth

    def flatten(self, c, r, normal, amount=1.0, scale=(1, 1, 1)):
        n = np.asarray(normal, float); n /= np.linalg.norm(n)
        f = self.falloff(c, r, scale) * amount
        dist = (self.V - np.asarray(c, float)) @ n
        self.V -= (f * np.maximum(dist, 0))[:, None] * n

    def clamp_floor(self, z=0.0, soft=0.02):
        """Sole: everything below z is pressed flat onto the table."""
        zz = self.V[:, 2]
        t = smoothstep((zz - z) / soft)               # ease into the press instead of a hard cut
        self.V[:, 2] = np.where(zz < z + soft, z + (np.maximum(zz, z) - z) * t + (zz - z).clip(0) * (1 - t) * 0.5, zz)

    def stroke(self, pts, r, fn, amount, **kw):
        """Drag a brush along a polyline, splitting the amount over the dabs."""
        P = [np.asarray(p, float) for p in pts]
        dabs = []
        for a, b in zip(P[:-1], P[1:]):
            n = max(1, int(np.linalg.norm(b - a) / (r * 0.35)))
            dabs += [a + (b - a) * (i / n) for i in range(n)]
        dabs.append(P[-1])
        k = amount / max(1, len(dabs)) * 2.2
        for d in dabs: fn(d, r, k, **kw)

    def smooth(self, iterations=4, lam=0.5, mu=-0.53, mask=None):
        m = np.ones(len(self.V)) if mask is None else mask
        for i in range(iterations):
            for w in (lam, mu):
                acc = np.zeros_like(self.V)
                np.add.at(acc, self.E[:, 0], self.V[self.E[:, 1]])
                np.add.at(acc, self.E[:, 1], self.V[self.E[:, 0]])
                avg = acc / self.deg[:, None]
                self.V += (w * m)[:, None] * (avg - self.V)

    def smooth_region(self, c, r, iterations=4):
        self.smooth(iterations, mask=self.falloff(c, r))

    def noise(self, amp, freq, seed=1, mask=None, waves=9):
        """Clay surface texture: sum of random plane waves along the normals."""
        rng = np.random.default_rng(seed)
        N = self._fast_normals()
        h = np.zeros(len(self.V))
        for i in range(waves):
            k = rng.normal(size=3); k = k / np.linalg.norm(k) * freq * rng.uniform(0.6, 1.4)
            h += np.sin(self.V @ k + rng.uniform(0, 6.283)) / waves
        if mask is not None: h *= mask
        self.V += (h * amp)[:, None] * N

    def thumbprints(self, count, r, depth, seed=3, region=None):
        """Little flattened presses left by fingers: what makes it read as hand-made."""
        rng = np.random.default_rng(seed)
        N = self._fast_normals()
        idx = np.arange(len(self.V)) if region is None else np.nonzero(region)[0]
        for i in rng.choice(idx, size=min(count, len(idx)), replace=False):
            c = self.V[i].copy(); n = N[i]
            self.flatten(c - n * depth, r * rng.uniform(0.7, 1.3), n, 0.8)

    def lumps(self, count, r, amount, seed=5, region=None):
        rng = np.random.default_rng(seed)
        idx = np.arange(len(self.V)) if region is None else np.nonzero(region)[0]
        for i in rng.choice(idx, size=min(count, len(idx)), replace=False):
            self.inflate(self.V[i].copy(), r * rng.uniform(0.6, 1.4), amount * rng.uniform(-0.6, 1.0))

    def remesh(self, voxel):
        s = Sculpt.from_mesh(self.to_mesh('_rm'), voxel)
        self.V, self.F, self.E, self.deg = s.V, s.F, s.E, s.deg
        self.carve = np.zeros(len(self.V))

    # --- output
    def to_mesh(self, name):
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.V.tolist(), [], self.F)
        me.validate(clean_customdata=False)
        for p in me.polygons: p.use_smooth = True
        return me

    def to_object(self, name):
        return link(bpy.data.objects.new(name, self.to_mesh(name)))


# ------------------------------------------------------------------ finishing
def material(name, color, rough=0.55, metal=0.0, emit=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get('Principled BSDF')
    c = tuple(int(color[i:i + 2], 16) / 255 for i in (1, 3, 5)) if isinstance(color, str) else color
    lin = tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)
    b.inputs['Base Color'].default_value = (*lin, 1)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    m.diffuse_color = (*lin, 1)
    m['hex'] = color if isinstance(color, str) else ''
    return m


def paint(ob, mats, chooser):
    """chooser(centre[n,3], normal[n,3]) -> slot index per face."""
    me = ob.data
    for m in mats: me.materials.append(m)
    n = len(me.polygons)
    C = np.empty(n * 3); me.polygons.foreach_get('center', C); C = C.reshape(-1, 3)
    N = np.empty(n * 3); me.polygons.foreach_get('normal', N); N = N.reshape(-1, 3)
    idx = np.asarray(chooser(C, N), int)
    me.polygons.foreach_set('material_index', idx)


def paint_vertex_mask(ob, mats, slot_per_vertex):
    """Face slot = majority slot of its vertices (for masks computed per sculpt vertex)."""
    me = ob.data
    for m in mats: me.materials.append(m)
    idx = []
    for p in me.polygons:
        s = [slot_per_vertex[v] for v in p.vertices]
        idx.append(max(set(s), key=s.count))
    me.polygons.foreach_set('material_index', idx)


def decimate(ob, tris):
    ob.data.calc_loop_triangles()
    have = len(ob.data.loop_triangles)
    if have <= tris: return have
    m = ob.modifiers.new('budget', 'DECIMATE'); m.ratio = tris / have
    with bpy.context.temp_override(object=ob, active_object=ob):
        bpy.ops.object.modifier_apply(modifier=m.name)
    ob.data.calc_loop_triangles()
    return len(ob.data.loop_triangles)


def tri_count(ob):
    ob.data.calc_loop_triangles(); return len(ob.data.loop_triangles)


def uv_sphere(name, r, co, scale=(1, 1, 1), seg=20, rings=12):
    import bmesh
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    bm.to_mesh(me); bm.free()
    ob = link(bpy.data.objects.new(name, me))
    for p in me.polygons: p.use_smooth = True
    ob.scale = scale; ob.location = tuple(co)
    return ob


def apply_xf(ob):
    ob.data.transform(Matrix.LocRotScale(ob.location, ob.rotation_euler, ob.scale))
    ob.location = (0, 0, 0); ob.rotation_euler = (0, 0, 0); ob.scale = (1, 1, 1)
