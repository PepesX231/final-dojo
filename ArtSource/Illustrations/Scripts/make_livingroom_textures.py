"""
Printed-page textures for the Pop-up Storybook Living Room (docs/VISUAL_RECONSTRUCTION.md §2).
One visual grammar for everything: cream paper + flat watercolour wash + one ink (#302A3D) wobbly line weight
+ subtle paper fibre. All tileable where noted. Output → Assets/DreamRush/Art/Environment/Textures/.

usage: python3 make_livingroom_textures.py <out_dir>
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
INK = (48, 42, 61)
CREAM = (245, 231, 211)
rng = np.random.default_rng(12)


# ---------------------------------------------------------------- helpers
def fibre(w, h, amount=6.0, fibres=600, seed=1):
    """paper tooth + short fibres, tileable (wraps)."""
    r = np.random.default_rng(seed)
    a = r.normal(0, amount * 0.45, (h, w))
    for _ in range(fibres):
        x, y = r.uniform(0, w), r.uniform(0, h)
        ang, L = r.uniform(0, math.pi), r.uniform(5, 18)
        for t in np.linspace(0, 1, int(L)):
            xi, yi = int(x + math.cos(ang) * L * t) % w, int(y + math.sin(ang) * L * t) % h
            a[yi, xi] -= r.uniform(0.5, 1.2) * amount
    img = Image.fromarray(np.clip(a + 128, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7))
    return (np.asarray(img).astype(np.float32) - 128.0)


def periodic_noise(w, h, scale, seed, octaves=3):
    """smooth tileable value noise (sum of sines with random phases)."""
    r = np.random.default_rng(seed)
    y, x = np.mgrid[0:h, 0:w]
    out = np.zeros((h, w), np.float32)
    amp = 1.0
    for o in range(octaves):
        for _ in range(4):
            kx, ky = r.integers(1, 3 + o * 2), r.integers(1, 3 + o * 2)
            ph = r.uniform(0, 2 * math.pi)
            out += amp * np.sin(2 * math.pi * (kx * x / w * scale + ky * y / h * scale) + ph)
        amp *= 0.5
    out -= out.min(); out /= out.max() + 1e-6
    return out


def wobbly_line(d, p0, p1, width, seed, color=INK, wob=1.6, wrap=None):
    r = random.Random(seed)
    n = max(2, int(math.dist(p0, p1) / 14))
    pts = []
    ph = r.uniform(0, 6.28)
    for i in range(n + 1):
        t = i / n
        x = p0[0] + (p1[0] - p0[0]) * t
        y = p0[1] + (p1[1] - p0[1]) * t
        nx, ny = -(p1[1] - p0[1]), (p1[0] - p0[0])
        L = math.hypot(nx, ny) + 1e-6
        o = math.sin(t * 9 + ph) * wob + r.uniform(-0.4, 0.4)
        pts.append((x + nx / L * o, y + ny / L * o))
    d.line(pts, fill=color, width=width, joint='curve')


def to_img(arr_rgb):
    return Image.fromarray(np.clip(arr_rgb, 0, 255).astype(np.uint8))


def wash(base, w, h, seed, strength=10):
    """flat watercolour wash: base colour with soft blotchy value shifts (tileable)."""
    n = periodic_noise(w, h, 1.0, seed, 3)
    arr = np.zeros((h, w, 3), np.float32)
    for c in range(3):
        arr[..., c] = base[c] + (n - 0.5) * strength
    return arr


S = 1024

# ---------------------------------------------------------------- 1. printed floorboards (tile = 4 m)
arr = wash(CREAM, S, S, 3, 8)
img = to_img(arr)
d = ImageDraw.Draw(img, 'RGBA')
boards = 16                      # 0.25 m boards along V; seams along U
bw = S / boards
r = random.Random(4)
board_cols = [(238, 218, 192), (241, 222, 196), (236, 214, 188), (239, 220, 194)]
FLOOR_INK = (176, 146, 128)   # softer printed line on the floor: less noise where the player jumps (§5.5)
for b in range(boards):
    x0 = b * bw
    # each board column: 2-3 planks with seams, wrapping vertically
    seams = sorted(r.uniform(0, S) for _ in range(r.randint(1, 2)))
    col = board_cols[r.randrange(len(board_cols))]
    d.rectangle([x0, 0, x0 + bw, S], fill=col + (255,))
    # dry-brush streaks (grain), very subtle
    for k in range(5):
        gx = x0 + r.uniform(4, bw - 4)
        d.line([(gx, 0), (gx + r.uniform(-3, 3), S)], fill=(205, 175, 140, 45), width=1)
    for s in seams:
        wobbly_line(d, (x0 + 2, s), (x0 + bw - 2, s), 2, r.randint(0, 999), FLOOR_INK)
        d.ellipse([x0 + 6, s - 9, x0 + 10, s - 5], fill=FLOOR_INK + (110,))           # nail dots
        d.ellipse([x0 + bw - 10, s - 9, x0 + bw - 6, s - 5], fill=FLOOR_INK + (110,))
    if r.random() < 0.35:                                                      # a knot
        ky = r.uniform(30, S - 30); kx = x0 + bw * r.uniform(0.3, 0.7)
        d.ellipse([kx - 7, ky - 11, kx + 7, ky + 11], outline=(170, 130, 100, 110), width=2)
for b in range(boards + 1):
    x = (b * bw) % S
    wobbly_line(d, (x, -10), (x, S + 10), 2, 100 + b, FLOOR_INK)
a = np.asarray(img.convert('RGB')).astype(np.float32) + fibre(S, S, 5, 900, 2)[..., None]
to_img(a).save(os.path.join(OUT, 'T_PageFloor.png'))

# ---------------------------------------------------------------- 2. wallpaper print (tile = 2 m wide x 2 m)
LAV = (164, 154, 214)
arr = wash(LAV, S, S, 7, 10)
img = to_img(arr)
d = ImageDraw.Draw(img, 'RGBA')
cream = (248, 238, 222, 255)
step = S / 6
for j in range(6):
    for i in range(6):
        cx = i * step + (step / 2 if j % 2 else 0) + step / 4
        cy = j * step + step / 2
        cx %= S
        if (i + j) % 3 == 0:     # crescent moon
            R = 26
            d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=cream)
            d.ellipse([cx - R + 13, cy - R - 6, cx + R + 13, cy + R - 6], fill=LAV + (255,))
        elif (i + j) % 3 == 1:   # little star
            pts = [(cx + math.cos(-math.pi / 2 + k * math.pi / 5) * (17 if k % 2 == 0 else 7),
                    cy + math.sin(-math.pi / 2 + k * math.pi / 5) * (17 if k % 2 == 0 else 7)) for k in range(10)]
            d.polygon(pts, fill=cream)
        else:                    # dot trio
            for k in range(3):
                d.ellipse([cx - 4 + k * 12 - 12, cy - 4, cx + 4 + k * 12 - 12, cy + 4], fill=(248, 238, 222, 200))
# baseboard band printed at the bottom of each tile (walls use V 0..1 per panel height)
d.rectangle([0, S - 70, S, S], fill=(232, 214, 190, 255))
wobbly_line(d, (-10, S - 70), (S + 10, S - 70), 4, 7)
wobbly_line(d, (-10, S - 40), (S + 10, S - 40), 2, 8)
a = np.asarray(img.convert('RGB')).astype(np.float32) + fibre(S, S, 5, 700, 5)[..., None]
to_img(a).save(os.path.join(OUT, 'T_Wallpaper.png'))

# ---------------------------------------------------------------- 3. page edge (stacked paper), tile along U
E = 512
arr = np.zeros((128, E, 3), np.float32)
for y in range(128):
    line = (y % 6 == 0)
    c = (226, 210, 188) if line else (246, 236, 220)
    arr[y, :, :] = c
arr += fibre(E, 128, 3, 120, 9)[..., None]
img = to_img(arr)
d = ImageDraw.Draw(img)
d.line([(0, 0), (E, 0)], fill=INK, width=3)
d.line([(0, 127), (E, 127)], fill=INK, width=3)
img.save(os.path.join(OUT, 'T_PageEdge.png'))

# ---------------------------------------------------------------- 4. round felt rug (not tiled)
R = 1024
img = Image.new('RGBA', (R, R), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
c = R / 2
rings = [(0.49, (176, 150, 196)), (0.43, (238, 196, 170)), (0.36, (176, 150, 196)), (0.3, (245, 226, 205)), (0.12, (238, 196, 170))]
for frac, col in rings:
    rr = frac * R
    d.ellipse([c - rr, c - rr, c + rr, c + rr], fill=col + (255,))
# scalloped stitch ring + little stars stitched (thread colour = cream)
for k in range(48):
    ang = k / 48 * 2 * math.pi
    x, y = c + math.cos(ang) * 0.395 * R, c + math.sin(ang) * 0.395 * R
    d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(248, 238, 222, 255))
for k in range(8):
    ang = k / 8 * 2 * math.pi + 0.2
    x, y = c + math.cos(ang) * 0.21 * R, c + math.sin(ang) * 0.21 * R
    pts = [(x + math.cos(-math.pi / 2 + i * math.pi / 5) * (22 if i % 2 == 0 else 9), y + math.sin(-math.pi / 2 + i * math.pi / 5) * (22 if i % 2 == 0 else 9)) for i in range(10)]
    d.polygon(pts, fill=(176, 150, 196, 255))
d.ellipse([c - 0.49 * R, c - 0.49 * R, c + 0.49 * R, c + 0.49 * R], outline=INK + (255,), width=5)
arr = np.asarray(img).astype(np.float32)
fuzz = rng.normal(0, 7, (R, R))
arr[..., :3] += fuzz[..., None]
img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))
img.save(os.path.join(OUT, 'T_RugRound.png'))

# ---------------------------------------------------------------- 5. sofa fabric (dusty rose, soft weave), tileable
F = 512
base = (205, 140, 150)
arr = wash(base, F, F, 21, 12)
y, x = np.mgrid[0:F, 0:F]
weave = (np.sin(x * 2 * math.pi / 8) * np.sin(y * 2 * math.pi / 8)) * 4
arr += weave[..., None]
arr += fibre(F, F, 3, 200, 22)[..., None]
to_img(arr).save(os.path.join(OUT, 'T_FabricRose.png'))

# ---------------------------------------------------------------- 6. gingham (chair cushion) tileable
G = 512
img = Image.new('RGB', (G, G), (246, 236, 222))
d = ImageDraw.Draw(img, 'RGBA')
n = 8
cw = G / n
for i in range(n):
    if i % 2 == 0:
        d.rectangle([i * cw, 0, (i + 1) * cw, G], fill=(226, 92, 100, 150))
        d.rectangle([0, i * cw, G, (i + 1) * cw], fill=(226, 92, 100, 150))
arr = np.asarray(img).astype(np.float32) + fibre(G, G, 3, 150, 31)[..., None]
to_img(arr).save(os.path.join(OUT, 'T_Gingham.png'))

# ---------------------------------------------------------------- 7. toy block faces atlas (2x2: moon, star, apple, heart)
B = 1024
img = Image.new('RGB', (B, B), (0, 0, 0))
d = ImageDraw.Draw(img)
face_cols = [(143, 213, 237), (255, 200, 140), (150, 214, 180), (242, 170, 160)]
for idx in range(4):
    ox, oy = (idx % 2) * 512, (idx // 2) * 512
    d.rectangle([ox, oy, ox + 511, oy + 511], fill=face_cols[idx])
    d.rounded_rectangle([ox + 40, oy + 40, ox + 471, oy + 471], radius=40, outline=(255, 248, 236), width=14)
    cx, cy = ox + 256, oy + 256
    if idx == 0:
        d.ellipse([cx - 120, cy - 120, cx + 120, cy + 120], fill=(255, 248, 236), outline=INK, width=8)
        d.ellipse([cx - 60, cy - 150, cx + 170, cy + 90], fill=face_cols[0])
    elif idx == 1:
        pts = [(cx + math.cos(-math.pi / 2 + k * math.pi / 5) * (140 if k % 2 == 0 else 60), cy + math.sin(-math.pi / 2 + k * math.pi / 5) * (140 if k % 2 == 0 else 60)) for k in range(10)]
        d.polygon(pts, fill=(255, 248, 236), outline=INK)
        d.line(pts + [pts[0]], fill=INK, width=8, joint='curve')
    elif idx == 2:
        d.ellipse([cx - 120, cy - 100, cx + 120, cy + 130], fill=(232, 93, 101), outline=INK, width=8)
        d.line([(cx, cy - 100), (cx + 10, cy - 160)], fill=INK, width=12)
        d.ellipse([cx + 15, cy - 175, cx + 95, cy - 135], fill=(120, 183, 139), outline=INK, width=6)
    else:
        pts = []
        for k in range(60):
            t = k / 60 * 2 * math.pi
            hx = 16 * math.sin(t) ** 3; hy = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
            pts.append((cx + hx * 8, cy - hy * 8))
        d.polygon(pts, fill=(255, 248, 236))
        d.line(pts + [pts[0]], fill=INK, width=8, joint='curve')
arr = np.asarray(img).astype(np.float32) + fibre(B, B, 4, 400, 41)[..., None]
to_img(arr).save(os.path.join(OUT, 'T_ToyBlock.png'))

# ---------------------------------------------------------------- 8. lamp shade fabric (warm cream pleats)
L = 512
arr = np.zeros((L, L, 3), np.float32)
x = np.arange(L)
pleat = (np.sin(x * 2 * math.pi / 32) * 0.5 + 0.5) * 14
for c, v in enumerate((246, 222, 180)):
    arr[..., c] = v - pleat[None, :]
arr += fibre(L, L, 3, 150, 51)[..., None]
img = to_img(arr)
d = ImageDraw.Draw(img)
d.rectangle([0, 0, L, 18], fill=(255, 184, 112)); d.rectangle([0, L - 18, L, L], fill=(255, 184, 112))
img.save(os.path.join(OUT, 'T_LampShade.png'))

# ---------------------------------------------------------------- 9. book cover cloth (deep plum, linen)
C = 512
arr = wash((74, 58, 104), C, C, 61, 10)
y, x = np.mgrid[0:C, 0:C]
arr += (np.sin(x * 2 * math.pi / 6) * 3 + np.sin(y * 2 * math.pi / 6) * 3)[..., None]
to_img(arr).save(os.path.join(OUT, 'T_BookCloth.png'))

# ---------------------------------------------------------------- 10. felt with blanket stitch border (cushions), tinted in Unity
F = 512
arr = np.full((F, F, 3), 246.0)
arr += rng.normal(0, 5, (F, F))[..., None]
arr = np.asarray(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.0))).astype(np.float32)
img = to_img(arr); d = ImageDraw.Draw(img)
for k in range(0, F, 32):             # blanket stitches along every edge (reads as sewn cushion piping)
    for (x0, y0, x1, y1) in ((k + 8, 18, k + 22, 18), (k + 8, F - 18, k + 22, F - 18), (18, k + 8, 18, k + 22), (F - 18, k + 8, F - 18, k + 22)):
        d.line([(x0, y0), (x1, y1)], fill=(255, 255, 255), width=5)
d.rectangle([0, 0, F - 1, 8], fill=(205, 205, 215)); d.rectangle([0, F - 9, F - 1, F - 1], fill=(205, 205, 215))
img.save(os.path.join(OUT, 'T_Felt.png'))

print('living room textures ->', OUT)
