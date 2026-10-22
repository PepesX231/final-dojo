"""
VFX particle textures (docs/RESEARCH_V2.md §3): big simple hard-edged shapes, hand-made, crunched highlights.
White/greyscale so ParticleSystem colour-over-lifetime tints them. Output → Assets/DreamRush/Art/VFX/Textures/
usage: python3 make_vfx_textures.py <out_dir>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
S = 256


def save(name, img):
    img.save(os.path.join(OUT, name + '.png'))


def blank():
    return Image.new('RGBA', (S, S), (255, 255, 255, 0))


# cartoon puff: 5 overlapping circles, hard edge + inner soft shade (dust / smoke)
img = blank(); d = ImageDraw.Draw(img)
r = random.Random(3)
for k in range(6):
    a = k / 6 * 2 * math.pi
    cx, cy, rr = S / 2 + math.cos(a) * 38, S / 2 + math.sin(a) * 30, r.uniform(48, 62)
    d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(255, 255, 255, 255))
d.ellipse([S / 2 - 64, S / 2 - 64, S / 2 + 64, S / 2 + 64], fill=(255, 255, 255, 255))
arr = np.array(img).astype(np.float32)
y, x = np.mgrid[0:S, 0:S]
shade = np.clip(((x - S * 0.35) + (y - S * 0.35)) / (S * 1.2), 0, 1)       # lower-right shade band (crunched)
arr[..., :3] = 255 - (shade[..., None] > 0.45) * 38
save('VFX_Puff', Image.fromarray(arr.astype(np.uint8)))

# 4-point sparkle star (hard), with a tiny round core
img = blank(); d = ImageDraw.Draw(img); c = S / 2
pts = []
for k in range(8):
    a = k * math.pi / 4 - math.pi / 2
    rr = 120 if k % 2 == 0 else 22
    pts.append((c + math.cos(a) * rr, c + math.sin(a) * rr))
d.polygon(pts, fill=(255, 255, 255, 255))
save('VFX_Sparkle', img)

# soft glow dot (dust motes / embers) — the only soft texture
g = np.exp(-((x - c) ** 2 + (y - c) ** 2) / (2 * 34.0 ** 2))
img = np.zeros((S, S, 4), np.uint8); img[..., :3] = 255; img[..., 3] = (np.clip(g * 1.3, 0, 1) * 255).astype(np.uint8)
save('VFX_Glow', Image.fromarray(img))

# shockwave ring (hard outer edge, soft inner fade)
dd = np.sqrt((x - c) ** 2 + (y - c) ** 2) / (S / 2)
ring = np.where((dd < 0.98) & (dd > 0.72), np.clip((dd - 0.72) / 0.2, 0, 1), 0.0)
img = np.zeros((S, S, 4), np.uint8); img[..., :3] = 255; img[..., 3] = (ring * 255).astype(np.uint8)
save('VFX_Ring', Image.fromarray(img))

# telegraph disc: dashed outer ring + faint fill (boss attack warning, tinted red in Unity)
ang = np.arctan2(y - c, x - c)
dash = (np.mod(ang * 12 / np.pi, 2) < 1.25)
tele = np.where((dd < 0.98) & (dd > 0.88) & dash, 1.0, 0.0) + np.where(dd < 0.88, 0.22, 0.0)
img = np.zeros((S, S, 4), np.uint8); img[..., :3] = 255; img[..., 3] = (np.clip(tele, 0, 1) * 255).astype(np.uint8)
save('VFX_Telegraph', Image.fromarray(img))

# speed line streak (long, tapered)
W, H = 512, 64
img = Image.new('RGBA', (W, H), (255, 255, 255, 0)); d = ImageDraw.Draw(img)
d.polygon([(0, H / 2), (W * 0.15, H / 2 - 10), (W, H / 2 - 2), (W, H / 2 + 2), (W * 0.15, H / 2 + 10)], fill=(255, 255, 255, 255))
img.save(os.path.join(OUT, 'VFX_Streak.png'))

# god-ray strip: vertical soft beam with tapered ends (additive)
W, H = 128, 512
yy, xx = np.mgrid[0:H, 0:W]
beam = np.exp(-((xx - W / 2) / (W * 0.22)) ** 2) * np.clip(1 - yy / H, 0, 1) ** 0.7 * np.clip(yy / (H * 0.08), 0, 1)
img = np.zeros((H, W, 4), np.uint8); img[..., :3] = 255; img[..., 3] = (np.clip(beam, 0, 1) * 255).astype(np.uint8)
save('VFX_Beam', Image.fromarray(img))

# confetti paper bit (rectangle, hard)
img = blank(); d = ImageDraw.Draw(img)
d.rectangle([S * 0.2, S * 0.38, S * 0.8, S * 0.62], fill=(255, 255, 255, 255))
save('VFX_Confetti', img)

# Z letter (sleep), hand-drawn stroke
img = blank(); d = ImageDraw.Draw(img)
d.line([(60, 70), (196, 70), (60, 190), (196, 190)], fill=(255, 255, 255, 255), width=30, joint='curve')
save('VFX_Z', img)
print('vfx textures ->', OUT)
