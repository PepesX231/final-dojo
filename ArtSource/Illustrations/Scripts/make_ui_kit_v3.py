"""
UI kit v3 — Persona x Final Fantasy (docs/RESEARCH_V2.md §1).
3-colour system (Midnight Plum / Cream / Apple Red) + Dream Lavender accent. Hard edges, skewed slabs,
angular-cut FF windows, halftone + stripe textures, a starburst callout and a rough brush stroke.
Everything is drawn here for this game (no stock icons). Tintable pieces are white.
usage: python3 make_ui_kit_v3.py <out_dir>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
SS = 4  # supersample for clean anti-aliased hard edges

PLUM = (26, 18, 41)
CREAM = (250, 240, 220)
RED = (229, 41, 61)
LAV = (184, 158, 255)


def save(name, img):
    img.save(os.path.join(OUT, name + '.png'))


def canvas(w, h, fill=(255, 255, 255, 0)):
    return Image.new('RGBA', (w * SS, h * SS), fill)


def down(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def poly(d, pts, fill):
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=fill)


# 1 Slab: skewed parallelogram (buttons, tags, selection bar). 9-slice L/R 34.
W, H, K = 256, 96, 26
img = canvas(W, H); d = ImageDraw.Draw(img)
poly(d, [(K, 0), (W, 0), (W - K, H), (0, H)], (255, 255, 255, 255))
save('UI3_Slab', down(img, W, H))

# 2 Cut: angular chamfered panel (tintable). 9-slice 22.
W = H = 128; C = 18
img = canvas(W, H); d = ImageDraw.Draw(img)
poly(d, [(0, 0), (W - C, 0), (W, C), (W, H), (C, H), (0, H - C)], (255, 255, 255, 255))
save('UI3_Cut', down(img, W, H))

# 3 Window (FF x SAO): plum fill, cream double rule, chamfered corners, corner diamonds. Not tinted. 9-slice 28.
W = H = 192; C = 20
img = canvas(W, H); d = ImageDraw.Draw(img)
def chamfer(inset, c):
    return [(inset, inset), (W - c - inset * 0.4, inset), (W - inset, c + inset * 0.4), (W - inset, H - inset),
            (c + inset * 0.4, H - inset), (inset, H - c - inset * 0.4)]
poly(d, chamfer(0, C), PLUM + (238,))
# outer rule
for t in range(3):
    pts = chamfer(5 + t * 0.33, C)
    d.line([(x * SS, y * SS) for x, y in pts + [pts[0]]], fill=CREAM + (255,), width=SS)
pts = chamfer(11, C)
d.line([(x * SS, y * SS) for x, y in pts + [pts[0]]], fill=CREAM + (120,), width=int(SS * 1.2))
for (cx, cy) in [(11, 11), (W - 11, H - 11)]:
    r = 6
    poly(d, [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], RED + (255,))
save('UI3_Window', down(img, W, H))

# 4 Starburst callout (Persona "!" burst). white.
W = H = 512
img = canvas(W, H); d = ImageDraw.Draw(img)
rnd = random.Random(7); pts = []
N = 22
for k in range(N * 2):
    a = k / (N * 2) * 2 * math.pi
    rr = (238 if k % 2 == 0 else 168) * rnd.uniform(0.88, 1.0)
    pts.append((W / 2 + math.cos(a) * rr, H / 2 + math.sin(a) * rr * 0.82))
poly(d, pts, (255, 255, 255, 255))
save('UI3_Burst', down(img, W, H))

# 5 Halftone tile (dots), alpha
S = 64
img = canvas(S, S); d = ImageDraw.Draw(img)
for (cx, cy) in [(16, 16), (48, 48)]:
    r = 9
    d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=(255, 255, 255, 255))
save('UI3_Halftone', down(img, S, S))

# 6 Stripes tile (45°)
S = 64
img = canvas(S, S); d = ImageDraw.Draw(img)
for o in range(-S, S * 2, 32):
    poly(d, [(o, 0), (o + 12, 0), (o + 12 - S, S), (o - S, S)], (255, 255, 255, 255))
save('UI3_Stripes', down(img, S, S))

# 7 Brush stroke (title backing) — rough dry edge, white
W, H = 1024, 220
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
rng = np.random.default_rng(5)
top = 30 + 10 * np.sin(xx[0] / 60) + rng.normal(0, 3, W).cumsum() * 0.05
bot = H - 30 - 12 * np.sin(xx[0] / 85 + 1) + rng.normal(0, 3, W).cumsum() * 0.05
alpha = ((yy > top[None, :]) & (yy < bot[None, :])).astype(np.float32)
# taper ends + dry streaks
taper = np.clip(np.minimum(xx / 110, (W - xx) / 60), 0, 1)
streak = (rng.random(H)[:, None] > 0.12).astype(np.float32)
dry = np.where(xx > W * 0.8, streak, 1.0) * np.where(xx < 70, streak, 1.0)
alpha = alpha * dry * (rng.random((H, W)) < taper * 1.15)
a = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7))
img = Image.new('RGBA', (W, H), (255, 255, 255, 0)); img.putalpha(a)
save('UI3_Brush', img)

# 8 Diamond (bullets / FF cursor)
S = 64
img = canvas(S, S); d = ImageDraw.Draw(img)
poly(d, [(32, 2), (62, 32), (32, 62), (2, 32)], (255, 255, 255, 255))
save('UI3_Diamond', down(img, S, S))

# 9 Pointer: Persona-like angular cursor (white, tint red)
W, H = 128, 96
img = canvas(W, H); d = ImageDraw.Draw(img)
poly(d, [(0, 10), (124, 48), (0, 86), (30, 48)], (255, 255, 255, 255))
save('UI3_Pointer', down(img, W, H))

# 10 Moons (HUD sleep) — not tinted: cream crescent, plum ink, red rim; empty = dashed outline
S = 96
for full in (True, False):
    img = canvas(S, S); d = ImageDraw.Draw(img)
    m = Image.new('L', (S * SS, S * SS), 0); md = ImageDraw.Draw(m)
    md.ellipse([8 * SS, 8 * SS, 88 * SS, 88 * SS], fill=255)
    md.ellipse([30 * SS, 0, 106 * SS, 76 * SS], fill=0)
    if full:
        ink = m.filter(ImageFilter.MaxFilter(4 * SS + 1))
        img.paste(PLUM + (255,), (0, 0), ink)
        img.paste(CREAM + (255,), (0, 0), m)
        # red inner shading on the lower belly
        sh = Image.new('L', m.size, 0); ImageDraw.Draw(sh).ellipse([14 * SS, 20 * SS, 94 * SS, 100 * SS], fill=255)
        sh = Image.fromarray((np.array(m) * (np.array(sh) == 0)).astype(np.uint8))
        img.paste((255, 200, 120, 255), (0, 0), sh)
    else:
        ring = Image.fromarray(((np.array(m.filter(ImageFilter.MaxFilter(3 * SS + 1))) > 0) &
                                (np.array(m.filter(ImageFilter.MinFilter(3 * SS + 1))) == 0)).astype(np.uint8) * 255)
        img.paste(CREAM + (150,), (0, 0), ring)
    save('UI3_Moon' + ('Full' if full else 'Empty'), down(img, S, S))

# 11 KeyCap: angular key (white face, plum ink frame) 9-slice 16 — not tinted
W = H = 64; C = 10
img = canvas(W, H); d = ImageDraw.Draw(img)
poly(d, [(0, 0), (W - C, 0), (W, C), (W, H), (C, H), (0, H - C)], PLUM + (255,))
poly(d, [(4, 4), (W - C - 2, 4), (W - 4, C + 2), (W - 4, H - 8), (C + 2, H - 8), (4, H - C - 6)], CREAM + (255,))
save('UI3_Key', down(img, W, H))

# 12 Wipe edge: big jagged diagonal (transition), white
W, H = 512, 1024
img = canvas(W, H); d = ImageDraw.Draw(img)
pts = [(W, 0), (W, H)]
teeth = 9
for k in range(teeth, -1, -1):
    y = H * k / teeth
    x = 120 + (60 if k % 2 else 0) - y * 0.1
    pts.append((x, y))
poly(d, pts, (255, 255, 255, 255))
save('UI3_Wipe', down(img, W, H))

# 13 Card frame (card select): chamfered card with inner frame lines, not tinted (cream face, plum frame, red corner)
W, H = 360, 520; C = 34
img = canvas(W, H); d = ImageDraw.Draw(img)
poly(d, [(0, 0), (W - C, 0), (W, C), (W, H), (C, H), (0, H - C)], PLUM + (255,))
poly(d, [(10, 10), (W - C - 4, 10), (W - 10, C + 4), (W - 10, H - 10), (C + 4, H - 10), (10, H - C - 4)], CREAM + (255,))
poly(d, [(W - C - 4, 10), (W - 10, 10), (W - 10, C + 4)], RED + (255,))
poly(d, [(10, H - C - 4), (10, H - 10), (C + 4, H - 10)], RED + (255,))
# (divider drawn in UI code)
# d.line([(22 * SS, 330 * SS), ((W - 22) * SS, 330 * SS)], fill=PLUM + (255,), width=3 * SS)
save('UI3_Card', down(img, W, H))
print('ui kit v3 ->', OUT)
