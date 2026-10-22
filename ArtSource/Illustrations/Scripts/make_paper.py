"""Paper page texture for the sleep/wake transition (GDD §22.2 paper peel, §5 Applebook tone).
White-ish so UI tint colours it; fibres + tooth + deckled (torn) long edges + faint ink doodles."""
import math, random, sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
OUT = sys.argv[1]
W, H = 256, 1024
rng = np.random.default_rng(4)
base = np.full((H, W), 238.0)
base += rng.normal(0, 4, (H, W))                                   # tooth
for _ in range(260):                                               # fibres
    x, y = rng.uniform(0, W), rng.uniform(0, H); a = rng.uniform(0, math.pi); L = rng.uniform(6, 22)
    for t in np.linspace(0, 1, int(L)):
        xi, yi = int(x + math.cos(a) * L * t), int(y + math.sin(a) * L * t)
        if 0 <= xi < W and 0 <= yi < H: base[yi, xi] -= rng.uniform(6, 14)
img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6)).convert('RGBA')
d = ImageDraw.Draw(img)
ink = (120, 110, 140, 70)
rnd = random.Random(8)
for _ in range(7):                                                 # faint doodles: stars and crescent moons
    cx, cy, r = rnd.uniform(40, W - 40), rnd.uniform(60, H - 60), rnd.uniform(10, 22)
    if rnd.random() < 0.6:
        pts = [(cx + math.cos(-math.pi / 2 + i * math.pi / 5) * (r if i % 2 == 0 else r * 0.45),
                cy + math.sin(-math.pi / 2 + i * math.pi / 5) * (r if i % 2 == 0 else r * 0.45)) for i in range(10)]
        d.line(pts + [pts[0]], fill=ink, width=2)
    else:
        d.arc([cx - r, cy - r, cx + r, cy + r], 60, 300, fill=ink, width=2)
a = np.array(img)
alpha = np.full((H, W), 255.0)
for y in range(H):                                                 # deckled long edges
    for side in (0, 1):
        e = 6 + 4 * math.sin(y * 0.09 + side * 2) + 3 * math.sin(y * 0.31 + side) + rng.uniform(0, 3)
        for x in range(int(e) + 2):
            xx = x if side == 0 else W - 1 - x
            alpha[y, xx] = 0 if x < e - 1.5 else 255 * (x - (e - 1.5)) / 1.5
a[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
Image.fromarray(a).save(os.path.join(OUT, 'T_PaperStrip.png'))
print('paper done')
