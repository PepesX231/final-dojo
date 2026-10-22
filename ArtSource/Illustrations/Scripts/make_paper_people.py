"""
M17 PAPER PEOPLE (user: "make the walking characters 2D, all different, arranged 2.5D"): sixteen dream-city residents
drawn as paper-cutout stickers — flat shapes, sleepy faces, a thick white sticker border and a thin ink line —
for camera-facing cards in the Midnight Palace. Original designs (no IP). 256×512 RGBA each, feet at the bottom.
usage: python3 make_paper_people.py <repo_root>
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = sys.argv[1]
OUT = os.path.join(ROOT, 'Assets/DreamRush/Art/MidnightPalace/People'); os.makedirs(OUT, exist_ok=True)
W, H = 256, 512
INK = (34, 24, 44, 255)


def sleepy_eyes(d, cx, cy, s=1.0, open_=False):
    for k in (-1, 1):
        x = cx + k * 16 * s
        if open_:
            d.ellipse([x - 6 * s, cy - 7 * s, x + 6 * s, cy + 7 * s], fill=INK); d.ellipse([x - 2 * s, cy - 5 * s, x + 2 * s, cy - 1 * s], fill=(255, 255, 255, 255))
        else:
            d.arc([x - 9 * s, cy - 6 * s, x + 9 * s, cy + 6 * s], 20, 160, fill=INK, width=max(2, int(4 * s)))


def blush(d, cx, cy, s=1.0):
    for k in (-1, 1): d.ellipse([cx + k * 26 * s - 8 * s, cy - 4 * s, cx + k * 26 * s + 8 * s, cy + 4 * s], fill=(255, 140, 160, 200))


def smile(d, cx, cy, s=1.0): d.arc([cx - 8 * s, cy - 6 * s, cx + 8 * s, cy + 6 * s], 20, 160, fill=INK, width=max(2, int(3 * s)))


def legs(d, cx, top, col, shoe, spread=22, length=90, w=18):
    for k in (-1, 1):
        x = cx + k * spread
        d.rounded_rectangle([x - w / 2, top, x + w / 2, top + length], radius=8, fill=col)
        d.ellipse([x - w * 0.9 + k * 4, top + length - 10, x + w * 0.9 + k * 4, top + length + 12], fill=shoe)


def arms(d, cx, y, col, hand=(250, 240, 230, 255), spread=58, length=80, w=16, swing=8):
    for k in (-1, 1):
        x0 = cx + k * spread; x1 = x0 + k * swing
        d.line([x0, y, x1, y + length], fill=col, width=w); d.ellipse([x1 - 10, y + length - 10, x1 + 10, y + length + 10], fill=hand)


def body_pear(d, cx, top, bottom, wtop, wbot, col):
    pts = []
    for i in range(41):
        t = i / 40; y = top + (bottom - top) * t; ww = wtop + (wbot - wtop) * (t ** 0.8)
        pts.append((cx - ww, y))
    for i in range(40, -1, -1):
        t = i / 40; y = top + (bottom - top) * t; ww = wtop + (wbot - wtop) * (t ** 0.8)
        pts.append((cx + ww, y))
    d.polygon(pts, fill=col)


def finish(img, name):
    a = np.array(img)
    alpha = Image.fromarray(a[..., 3])
    border = alpha.filter(ImageFilter.MaxFilter(17))
    ink = alpha.filter(ImageFilter.MaxFilter(5))
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    white = Image.new('RGBA', (W, H), (255, 252, 246, 255)); out.paste(white, (0, 0), border)
    inkl = Image.new('RGBA', (W, H), INK); out.paste(inkl, (0, 0), ink)
    out.alpha_composite(img)
    # paper grain
    g = np.array(out).astype(np.float32); n = np.random.default_rng(hash(name) % 9999).normal(0, 5, g.shape[:2])[..., None]
    g[..., :3] = np.clip(g[..., :3] + n, 0, 255)
    out = Image.fromarray(g.astype(np.uint8))
    out.save(os.path.join(OUT, f'PPL_{name}.png'))


def new():
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)


CX = 128
# 1–3 clockfolk in pyjamas (three colourways)
for i, (pj, cap) in enumerate((((120, 150, 255, 255), (90, 120, 230, 255)), ((255, 150, 190, 255), (230, 110, 160, 255)), ((130, 220, 160, 255), (90, 190, 130, 255)))):
    im, d = new()
    legs(d, CX, 380, pj, (140, 110, 230, 255), length=86)
    body_pear(d, CX, 250, 400, 48, 74, pj)
    for k in range(3): d.ellipse([CX - 6, 290 + k * 30, CX + 6, 302 + k * 30], fill=(255, 210, 90, 255))
    arms(d, CX, 280, pj)
    d.ellipse([CX - 66, 120, CX + 66, 252], fill=(230, 180, 70, 255)); d.ellipse([CX - 54, 132, CX + 54, 240], fill=(255, 246, 222, 255))
    for a in range(12):
        ang = a / 12 * math.tau; d.line([CX + math.sin(ang) * 46, 186 - math.cos(ang) * 46, CX + math.sin(ang) * 52, 186 - math.cos(ang) * 52], fill=INK, width=3)
    sleepy_eyes(d, CX, 180); blush(d, CX, 200); smile(d, CX, 208)
    d.polygon([(CX - 60, 140), (CX + 50, 120), (CX + 96, 70), (CX + 40, 104)], fill=cap); d.ellipse([CX + 84, 56, CX + 112, 84], fill=(255, 255, 255, 255))
    finish(im, f'Clockfolk{i}')

# 4 sheep commuter with a briefcase
im, d = new()
legs(d, CX, 390, (60, 50, 70, 255), (30, 26, 40, 255), length=80, w=14)
for a in range(0, 360, 30): d.ellipse([CX + math.cos(math.radians(a)) * 54 - 42, 320 + math.sin(math.radians(a)) * 60 - 42, CX + math.cos(math.radians(a)) * 54 + 42, 320 + math.sin(math.radians(a)) * 60 + 42], fill=(250, 248, 240, 255))
d.rounded_rectangle([CX - 40, 160, CX + 40, 262], radius=34, fill=(70, 60, 80, 255))
for k in (-1, 1): d.ellipse([CX + k * 46 - 18, 172, CX + k * 46 + 18, 196], fill=(70, 60, 80, 255))
sleepy_eyes(d, CX, 210, 0.9); smile(d, CX, 236)
for a in range(0, 360, 45): d.ellipse([CX + math.cos(math.radians(a)) * 34 - 18, 150 + math.sin(math.radians(a)) * 10 - 18, CX + math.cos(math.radians(a)) * 34 + 18, 150 + math.sin(math.radians(a)) * 10 + 18], fill=(250, 248, 240, 255))
d.rounded_rectangle([CX + 50, 370, CX + 110, 420], radius=8, fill=(150, 90, 50, 255)); d.rectangle([CX + 70, 360, CX + 90, 372], fill=(90, 50, 30, 255))
d.polygon([(CX - 10, 270), (CX + 10, 270), (CX + 4, 340), (CX - 4, 340)], fill=(220, 60, 80, 255))
finish(im, 'Sheep')

# 5 pillow-head office worker
im, d = new()
legs(d, CX, 380, (40, 44, 70, 255), (20, 20, 30, 255), length=90, w=16)
d.polygon([(CX - 60, 250), (CX + 60, 250), (CX + 70, 400), (CX - 70, 400)], fill=(60, 70, 120, 255))
d.polygon([(CX - 16, 250), (CX + 16, 250), (CX, 300)], fill=(250, 250, 255, 255))
arms(d, CX, 262, (60, 70, 120, 255))
d.rounded_rectangle([CX - 80, 130, CX + 80, 250], radius=50, fill=(250, 244, 255, 255))
for k in (-1, 1): d.ellipse([CX + k * 78 - 10, 130 + (0 if k < 0 else 100) , CX + k * 78 + 10, 150 + (0 if k < 0 else 100)], fill=(250, 244, 255, 255))
sleepy_eyes(d, CX, 184); blush(d, CX, 204); smile(d, CX, 212)
d.rectangle([CX - 40, 300, CX - 10, 330], fill=(255, 220, 90, 255))
finish(im, 'Pillow')

# 6 cat salaryman
im, d = new()
legs(d, CX, 385, (30, 30, 40, 255), (10, 10, 16, 255), length=85, w=16)
d.polygon([(CX - 56, 250), (CX + 56, 250), (CX + 64, 400), (CX - 64, 400)], fill=(40, 40, 52, 255))
d.polygon([(CX - 12, 250), (CX + 12, 250), (CX + 6, 330), (CX - 6, 330)], fill=(230, 60, 60, 255))
arms(d, CX, 262, (40, 40, 52, 255))
d.ellipse([CX - 62, 140, CX + 62, 254], fill=(255, 170, 70, 255))
for k in (-1, 1): d.polygon([(CX + k * 56, 170), (CX + k * 30, 140), (CX + k * 62, 108)], fill=(255, 170, 70, 255))
for k in (-1, 1): d.line([CX + k * 20, 214, CX + k * 70, 206], fill=INK, width=2); d.line([CX + k * 20, 220, CX + k * 70, 224], fill=INK, width=2)
sleepy_eyes(d, CX, 194); d.polygon([(CX - 5, 210), (CX + 5, 210), (CX, 216)], fill=(255, 120, 150, 255)); smile(d, CX, 222)
finish(im, 'Cat')

# 7 moon kid with a balloon (short)
im, d = new()
d.line([CX + 60, 300, CX + 80, 120], fill=INK, width=2); d.ellipse([CX + 40, 60, CX + 120, 140], fill=(255, 90, 120, 255)); d.ellipse([CX + 56, 74, CX + 72, 92], fill=(255, 200, 210, 255))
legs(d, CX, 420, (255, 210, 90, 255), (90, 120, 230, 255), length=60, w=16, spread=16)
body_pear(d, CX, 330, 430, 34, 50, (90, 120, 230, 255))
arms(d, CX, 340, (90, 120, 230, 255), spread=40, length=50, w=12)
d.ellipse([CX - 52, 230, CX + 52, 334], fill=(255, 236, 160, 255)); d.ellipse([CX - 10, 220, CX + 70, 300], fill=(0, 0, 0, 0))
d.pieslice([CX - 52, 230, CX + 52, 334], 90, 270, fill=(255, 236, 160, 255))
sleepy_eyes(d, CX - 20, 280, 0.7, open_=True); smile(d, CX - 22, 304, 0.8)
finish(im, 'MoonKid')

# 8 teacup barista
im, d = new()
legs(d, CX, 390, (60, 40, 30, 255), (30, 20, 16, 255), length=80, w=16)
d.rounded_rectangle([CX - 56, 260, CX + 56, 400], radius=20, fill=(110, 70, 50, 255)); d.rectangle([CX - 46, 300, CX + 46, 400], fill=(250, 246, 236, 255))
arms(d, CX, 272, (110, 70, 50, 255))
d.chord([CX - 74, 110, CX + 74, 262], 0, 180, fill=(250, 250, 255, 255)); d.rectangle([CX - 74, 150, CX + 74, 186], fill=(250, 250, 255, 255))
d.ellipse([CX - 74, 140, CX + 74, 164], fill=(150, 90, 50, 255))
d.arc([CX + 56, 160, CX + 104, 220], 270, 90, fill=(250, 250, 255, 255), width=12)
for k in (-1, 0, 1): d.arc([CX + k * 26 - 10, 80, CX + k * 26 + 10, 136], 180, 360, fill=(255, 255, 255, 200), width=4)
sleepy_eyes(d, CX, 210); blush(d, CX, 226); smile(d, CX, 234)
finish(im, 'Teacup')

# 9 sleepy bear in a bathrobe
im, d = new()
legs(d, CX, 400, (130, 90, 60, 255), (240, 230, 220, 255), length=70, w=24)
body_pear(d, CX, 250, 420, 60, 84, (150, 190, 240, 255)); d.line([CX - 30, 260, CX + 20, 410], fill=(110, 150, 210, 255), width=8)
arms(d, CX, 270, (150, 190, 240, 255), spread=70, w=22)
d.ellipse([CX - 70, 120, CX + 70, 260], fill=(150, 104, 70, 255))
for k in (-1, 1): d.ellipse([CX + k * 54 - 22, 108, CX + k * 54 + 22, 152], fill=(150, 104, 70, 255))
d.ellipse([CX - 30, 196, CX + 30, 246], fill=(220, 190, 150, 255)); d.ellipse([CX - 8, 204, CX + 8, 216], fill=INK)
sleepy_eyes(d, CX, 180); smile(d, CX, 228, 0.8)
d.ellipse([CX + 40, 100, CX + 90, 150], fill=(255, 255, 255, 255)); d.text((CX + 52, 108), 'z', fill=INK)
finish(im, 'Bear')

# 10 owl night guard with a lantern
im, d = new()
legs(d, CX, 400, (230, 170, 60, 255), (230, 170, 60, 255), length=60, w=10, spread=18)
d.ellipse([CX - 70, 180, CX + 70, 420], fill=(110, 80, 60, 255)); d.ellipse([CX - 44, 250, CX + 44, 410], fill=(230, 210, 180, 255))
for k in (-1, 1): d.ellipse([CX + k * 30 - 26, 190, CX + k * 30 + 26, 242], fill=(255, 255, 255, 255)); d.ellipse([CX + k * 30 - 12, 204, CX + k * 30 + 12, 228], fill=INK)
d.polygon([(CX - 8, 240), (CX + 8, 240), (CX, 258)], fill=(255, 180, 40, 255))
d.polygon([(CX - 60, 180), (CX - 40, 150), (CX - 20, 190)], fill=(110, 80, 60, 255)); d.polygon([(CX + 60, 180), (CX + 40, 150), (CX + 20, 190)], fill=(110, 80, 60, 255))
d.rectangle([CX - 50, 150, CX + 50, 166], fill=(40, 60, 120, 255)); d.rectangle([CX - 30, 120, CX + 30, 152], fill=(40, 60, 120, 255))
d.line([CX + 70, 300, CX + 90, 330], fill=INK, width=3); d.rounded_rectangle([CX + 74, 330, CX + 110, 380], radius=6, fill=(255, 220, 120, 255))
finish(im, 'Owl')

# 11 candle grandma
im, d = new()
legs(d, CX, 410, (240, 230, 220, 255), (160, 70, 90, 255), length=60, w=14)
body_pear(d, CX, 240, 420, 50, 80, (190, 90, 120, 255)); d.rectangle([CX - 80, 360, CX + 80, 372], fill=(255, 220, 120, 255))
arms(d, CX, 262, (190, 90, 120, 255))
d.rounded_rectangle([CX - 40, 120, CX + 40, 250], radius=16, fill=(255, 248, 230, 255))
d.polygon([(CX - 40, 130), (CX - 30, 160), (CX - 20, 128)], fill=(255, 248, 230, 255))
d.line([CX, 120, CX, 96], fill=INK, width=3); d.ellipse([CX - 14, 52, CX + 14, 100], fill=(255, 180, 60, 255)); d.ellipse([CX - 7, 66, CX + 7, 96], fill=(255, 240, 160, 255))
d.ellipse([CX - 30, 166, CX - 6, 190], outline=INK, width=3); d.ellipse([CX + 6, 166, CX + 30, 190], outline=INK, width=3); d.line([CX - 6, 178, CX + 6, 178], fill=INK, width=3)
smile(d, CX, 212); blush(d, CX, 200, 0.9)
finish(im, 'Candle')

# 12 alarm-clock punk
im, d = new()
legs(d, CX, 385, (40, 40, 60, 255), (220, 60, 80, 255), length=85, w=16)
d.polygon([(CX - 54, 250), (CX + 54, 250), (CX + 60, 400), (CX - 60, 400)], fill=(30, 30, 40, 255))
for k in range(5): d.ellipse([CX - 40 + k * 20, 300, CX - 34 + k * 20, 306], fill=(200, 200, 210, 255))
arms(d, CX, 262, (30, 30, 40, 255))
d.ellipse([CX - 64, 124, CX + 64, 252], fill=(230, 50, 70, 255)); d.ellipse([CX - 50, 138, CX + 50, 238], fill=(255, 250, 240, 255))
for k in (-1, 1): d.ellipse([CX + k * 50 - 22, 96, CX + k * 50 + 22, 136], fill=(255, 210, 60, 255))
for k in range(5): d.polygon([(CX - 30 + k * 15, 124), (CX - 22 + k * 15, 70 - (k % 2) * 16), (CX - 14 + k * 15, 124)], fill=(80, 230, 200, 255))
sleepy_eyes(d, CX, 180, open_=True); d.line([CX - 10, 214, CX + 10, 214], fill=INK, width=4)
finish(im, 'Alarm')

# 13 bunny with an umbrella
im, d = new()
d.chord([CX - 110, 40, CX + 50, 170], 180, 360, fill=(120, 200, 255, 255)); d.line([CX - 30, 104, CX - 30, 300], fill=INK, width=4)
legs(d, CX, 400, (255, 255, 255, 255), (255, 160, 190, 255), length=70, w=18)
body_pear(d, CX, 270, 412, 44, 64, (255, 200, 120, 255))
arms(d, CX, 280, (255, 200, 120, 255), spread=46)
d.ellipse([CX - 56, 170, CX + 56, 282], fill=(255, 255, 255, 255))
for k in (-1, 1): d.ellipse([CX + k * 26 - 14, 70 + (20 if k > 0 else 0), CX + k * 26 + 14, 190], fill=(255, 255, 255, 255)); d.ellipse([CX + k * 26 - 6, 86 + (20 if k > 0 else 0), CX + k * 26 + 6, 180], fill=(255, 190, 210, 255))
sleepy_eyes(d, CX, 224); blush(d, CX, 242); smile(d, CX, 250)
finish(im, 'Bunny')

# 14 star idol (sparkly)
im, d = new()
legs(d, CX, 390, (255, 255, 255, 255), (255, 80, 160, 255), length=80, w=14, spread=16)
d.polygon([(CX - 40, 260), (CX + 40, 260), (CX + 80, 400), (CX - 80, 400)], fill=(255, 90, 180, 255))
arms(d, CX, 270, (255, 230, 240, 255), spread=46, swing=-30, length=60)
pts = []
for k in range(10):
    a = -math.pi / 2 + k * math.pi / 5; rr = 84 if k % 2 == 0 else 40; pts.append((CX + math.cos(a) * rr, 180 + math.sin(a) * rr))
d.polygon(pts, fill=(255, 220, 60, 255))
sleepy_eyes(d, CX, 180, 0.9, open_=True); blush(d, CX, 198, 0.8); smile(d, CX, 206, 0.8)
for (x, y) in ((40, 120), (220, 300), (60, 330)): d.polygon([(x, y - 14), (x + 4, y - 4), (x + 14, y), (x + 4, y + 4), (x, y + 14), (x - 4, y + 4), (x - 14, y), (x - 4, y - 4)], fill=(255, 255, 255, 255))
finish(im, 'Star')

# 15 mushroom kid
im, d = new()
legs(d, CX, 420, (240, 230, 210, 255), (120, 80, 60, 255), length=60, w=16, spread=16)
body_pear(d, CX, 320, 430, 36, 50, (100, 170, 120, 255))
arms(d, CX, 330, (100, 170, 120, 255), spread=40, length=50, w=12)
d.ellipse([CX - 40, 240, CX + 40, 330], fill=(250, 236, 214, 255))
d.chord([CX - 90, 160, CX + 90, 300], 180, 360, fill=(230, 60, 70, 255))
for (x, y, r) in ((-40, 196, 14), (20, 180, 12), (56, 214, 10), (-10, 214, 8)): d.ellipse([CX + x - r, y - r, CX + x + r, y + r], fill=(255, 255, 255, 255))
sleepy_eyes(d, CX, 280, 0.8); smile(d, CX, 300, 0.8)
finish(im, 'Mushroom')

# 16 wind-up robot toy
im, d = new()
legs(d, CX, 390, (160, 170, 190, 255), (90, 100, 120, 255), length=80, w=22)
d.rounded_rectangle([CX - 60, 250, CX + 60, 400], radius=12, fill=(120, 200, 220, 255)); d.rectangle([CX - 30, 290, CX + 30, 340], fill=(255, 210, 80, 255))
for k in range(3): d.ellipse([CX - 22 + k * 18, 306, CX - 12 + k * 18, 316], fill=(230, 60, 80, 255))
arms(d, CX, 262, (160, 170, 190, 255), w=18)
d.rounded_rectangle([CX - 56, 140, CX + 56, 252], radius=16, fill=(200, 210, 230, 255)); d.rectangle([CX - 40, 170, CX + 40, 210], fill=(40, 50, 70, 255))
for k in (-1, 1): d.ellipse([CX + k * 18 - 9, 180, CX + k * 18 + 9, 198], fill=(120, 255, 220, 255))
d.line([CX, 140, CX, 110], fill=INK, width=4); d.ellipse([CX - 10, 96, CX + 10, 116], fill=(255, 90, 120, 255))
d.ellipse([CX + 56, 300, CX + 90, 330], fill=(230, 190, 60, 255)); d.ellipse([CX + 80, 284, CX + 110, 346], fill=(230, 190, 60, 255))
finish(im, 'Robot')
print('paper people done:', len([f for f in os.listdir(OUT) if f.endswith('.png')]))
