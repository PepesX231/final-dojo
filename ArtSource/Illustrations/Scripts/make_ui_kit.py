"""
Storybook UI kit (docs/VISUAL_RECONSTRUCTION.md §2 UI): one paper, one ink, one tape — every UI surface is made
from these 9-slice pieces so menus, HUD, cards and prompts belong to the same world as the pop-up pages.
Output → Assets/DreamRush/Resources/DreamRush/UI/  (Resources so UIFactory can load them at runtime)

usage: python3 make_ui_kit.py <out_dir>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
INK = (48, 42, 61, 255)
PAPER = (247, 236, 218)
S = 2   # supersample


def fibre_rgba(w, h, base, amount=5, seed=1):
    r = np.random.default_rng(seed)
    a = np.zeros((h, w, 4), np.float32)
    a[..., :3] = base
    a[..., :3] += r.normal(0, amount * 0.5, (h, w))[..., None]
    for _ in range(int(w * h / 900)):
        x, y = r.uniform(0, w), r.uniform(0, h); ang, L = r.uniform(0, math.pi), r.uniform(4, 14)
        for t in np.linspace(0, 1, int(L)):
            xi, yi = int(x + math.cos(ang) * L * t), int(y + math.sin(ang) * L * t)
            if 0 <= xi < w and 0 <= yi < h: a[yi, xi, :3] -= amount
    a[..., 3] = 255
    return a


def wobble_rect(w, h, inset, radius, seed, jitter=1.6):
    """points of a hand-drawn rounded rectangle."""
    r = random.Random(seed)
    pts = []
    corners = [(w - inset - radius, inset + radius, -90), (w - inset - radius, h - inset - radius, 0),
               (inset + radius, h - inset - radius, 90), (inset + radius, inset + radius, 180)]
    for cx, cy, a0 in corners:
        for k in range(7):
            a = math.radians(a0 + k * 15)
            pts.append((cx + math.cos(a) * radius, cy + math.sin(a) * radius))
        # straight run with tiny wobble to next corner
    out = []
    for i in range(len(pts)):
        p0, p1 = pts[i], pts[(i + 1) % len(pts)]
        n = max(1, int(math.dist(p0, p1) / (10 * S)))
        for j in range(n):
            t = j / n
            out.append((p0[0] + (p1[0] - p0[0]) * t + r.uniform(-jitter, jitter) * S, p0[1] + (p1[1] - p0[1]) * t + r.uniform(-jitter, jitter) * S))
    return out


def paper_piece(name, w, h, radius, fill, ink_w=3, seed=1, shadow=True, torn=False):
    W, H = w * S, h * S
    base = fibre_rgba(W, H, fill, 4, seed)
    mask = Image.new('L', (W, H), 0)
    pts = wobble_rect(W, H, 6 * S, radius * S, seed, 2.2 if torn else 1.0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), 'RGBA')
    img.putalpha(mask)
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    out.alpha_composite(img)
    d = ImageDraw.Draw(out)
    if ink_w:
        d.line(pts + [pts[0]], fill=INK, width=ink_w * S, joint='curve')
    out = out.resize((w, h), Image.LANCZOS)
    out.save(os.path.join(OUT, name + '.png'))


# panels / notes / buttons (9-slice borders are set in UIFactory)
paper_piece('UI_Paper', 256, 256, 26, (252, 249, 244), 3, 1)        # near-white: Image.color tints it
paper_piece('UI_PaperTorn', 256, 256, 14, (253, 250, 246), 0, 2, torn=True)        # notes (no ink frame: lighter)
paper_piece('UI_Button', 128, 128, 30, (252, 248, 242), 3, 3)
paper_piece('UI_ButtonHot', 128, 128, 30, (255, 196, 128), 3, 4)
paper_piece('UI_Chip', 96, 96, 40, (250, 248, 252), 2, 5)                        # small rounded chip (tags)

# tape strip (semi-transparent washi tape with torn ends)
W, H = 160 * S, 48 * S
img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
r = random.Random(7)
top = [(x, r.uniform(0, 3) * S) for x in range(0, W + 1, 8 * S)]
bot = [(x, H - r.uniform(0, 3) * S) for x in range(W, -1, -8 * S)]
left = [(r.uniform(0, 6) * S, y) for y in range(H, 0, -6 * S)]
right = [(W - r.uniform(0, 6) * S, y) for y in range(0, H, 6 * S)]
poly = top + right + bot + left
d.polygon(poly, fill=(170, 160, 221, 190))
for k in range(0, W, 22 * S):                                                     # little printed dots on the tape
    d.ellipse([k + 8 * S, H / 2 - 3 * S, k + 14 * S, H / 2 + 3 * S], fill=(250, 244, 232, 200))
img.resize((160, 48), Image.LANCZOS).save(os.path.join(OUT, 'UI_Tape.png'))

# ribbon banner (boss name) — 9-slice horizontally (ends fixed)
W, H = 512 * S, 112 * S
img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
red = (232, 93, 101, 255); dark = (176, 62, 76, 255)
d.polygon([(0, 30 * S), (70 * S, 30 * S), (70 * S, 100 * S), (0, 100 * S), (26 * S, 65 * S)], fill=dark, outline=INK)
d.polygon([(W, 30 * S), (W - 70 * S, 30 * S), (W - 70 * S, 100 * S), (W, 100 * S), (W - 26 * S, 65 * S)], fill=dark, outline=INK)
d.line([(0, 30 * S), (70 * S, 30 * S), (70 * S, 100 * S), (0, 100 * S), (26 * S, 65 * S), (0, 30 * S)], fill=INK, width=3 * S)
d.line([(W, 30 * S), (W - 70 * S, 30 * S), (W - 70 * S, 100 * S), (W, 100 * S), (W - 26 * S, 65 * S), (W, 30 * S)], fill=INK, width=3 * S)
d.rounded_rectangle([48 * S, 8 * S, W - 48 * S, 86 * S], radius=10 * S, fill=red, outline=INK, width=3 * S)
d.line([(60 * S, 20 * S), (W - 60 * S, 20 * S)], fill=(250, 214, 210, 255), width=2 * S)
img.resize((512, 112), Image.LANCZOS).save(os.path.join(OUT, 'UI_Ribbon.png'))

# moons (Sleep Stability): full = cream moon with ink, empty = dashed outline
for name, full in (('UI_MoonFull', True), ('UI_MoonEmpty', False)):
    W = 96 * S
    img = Image.new('RGBA', (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    c, R = W / 2, 38 * S
    mask = Image.new('L', (W, W), 0); md = ImageDraw.Draw(mask)
    md.ellipse([c - R, c - R, c + R, c + R], fill=255)
    md.ellipse([c - R + 26 * S, c - R - 10 * S, c + R + 26 * S, c + R - 10 * S], fill=0)
    if full:
        col = Image.new('RGBA', (W, W), (255, 226, 150, 255)); img.paste(col, (0, 0), mask)
        edge = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(5))
        ink = Image.new('RGBA', (W, W), INK); img.paste(ink, (0, 0), edge)
        d.ellipse([c - 18 * S, c - 6 * S, c - 10 * S, c + 2 * S], fill=INK)        # sleepy closed eye
    else:
        edge = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3))
        arr = np.array(edge); yy, xx = np.mgrid[0:W, 0:W]
        arr[((np.arctan2(yy - c, xx - c) * 6) % 2) < 1] = 0                       # dashed
        ink = Image.new('RGBA', (W, W), (120, 108, 150, 255)); img.paste(ink, (0, 0), Image.fromarray(arr))
    img.resize((96, 96), Image.LANCZOS).save(os.path.join(OUT, name + '.png'))

# keycap (prompts)
paper_piece('UI_Key', 64, 64, 14, (255, 250, 240), 3, 9)
print('ui kit ->', OUT)
