"""
MIDNIGHT PALACE v3 facades (M21 — user: "outside looks semi-real, inside is low-poly: the art styles don't match").
One illustrated language for the whole city, matching the interiors and Pip: flat cel fills, a plum ink line around
every frame, two-tone glass (shade + one diagonal highlight), warm lit windows with curtains/plants, no noise, no grime,
no photo texture. Same file names and tile sizes as v2, so the builder's world-UV scale is unchanged.
usage: python3 -I make_city_art_v3.py <repo_root>
"""
import os, random, sys
from PIL import Image, ImageDraw, ImageFilter

ROOT = sys.argv[1]
OUT = os.path.join(ROOT, 'Assets/DreamRush/Art/MidnightPalace/Textures'); os.makedirs(OUT, exist_ok=True)
r = random.Random(2121)
INK = (57, 55, 77)
LIT = [(255, 222, 160), (255, 205, 140), (255, 236, 196), (250, 214, 176)]
CURTAIN = [(232, 140, 128), (120, 176, 168), (238, 196, 120), (176, 150, 214), (240, 232, 214)]


def save(n, im): im.save(os.path.join(OUT, n + '.png'))
def mix(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def window(d, e, box, glass, lit_p, arch=False, curtain=True, plant=False, ink=INK, w=3):
    """one illustrated window: ink frame, two-tone glass, maybe warmly lit with curtains, maybe a flower box."""
    x0, y0, x1, y1 = box
    lit = r.random() < lit_p
    if lit:
        c = r.choice(LIT); fill = c
    else:
        fill = glass
    if arch:
        rr = (x1 - x0) // 2
        d.pieslice([x0, y0, x1, y0 + 2 * rr], 180, 360, fill=fill, outline=ink, width=w)
        d.rectangle([x0, y0 + rr, x1, y1], fill=fill, outline=ink, width=w)
        d.line([x0 + w, y0 + rr, x1 - w, y0 + rr], fill=fill, width=w + 1)
        if lit: e.pieslice([x0, y0, x1, y0 + 2 * rr], 180, 360, fill=c); e.rectangle([x0, y0 + rr, x1, y1], fill=c)
    else:
        d.rectangle(box, fill=fill, outline=ink, width=w)
        if lit: e.rectangle([x0 + w, y0 + w, x1 - w, y1 - w], fill=c)
    if not lit:   # cel highlight: one diagonal band
        hw = max(3, (x1 - x0) // 5)
        for k in range(hw):
            xa = x0 + w + (x1 - x0) // 4 + k
            d.line([xa, y1 - w - 1, min(x1 - w - 1, xa + (y1 - y0) // 2), y0 + w + 1 + (arch and (x1 - x0) // 2 or 0)], fill=mix(glass, (255, 255, 255), 0.35))
    else:
        if curtain and r.random() < 0.7:   # curtains drawn back
            cc = r.choice(CURTAIN); cw = max(3, (x1 - x0) // 4)
            d.polygon([(x0 + w, y0 + w + 2), (x0 + w + cw, y0 + w + 2), (x0 + w + cw // 2, y1 - w)], fill=cc, outline=ink)
            d.polygon([(x1 - w, y0 + w + 2), (x1 - w - cw, y0 + w + 2), (x1 - w - cw // 2, y1 - w)], fill=cc, outline=ink)
            e.polygon([(x0 + w, y0 + w + 2), (x0 + w + cw, y0 + w + 2), (x0 + w + cw // 2, y1 - w)], fill=mix(cc, (0, 0, 0), 0.6))
            e.polygon([(x1 - w, y0 + w + 2), (x1 - w - cw, y0 + w + 2), (x1 - w - cw // 2, y1 - w)], fill=mix(cc, (0, 0, 0), 0.6))
        if r.random() < 0.25:   # a sleepy silhouette
            cx = r.randint(x0 + (x1 - x0) // 3, x1 - (x1 - x0) // 3); hy = y1 - (y1 - y0) // 2
            d.ellipse([cx - 4, hy - 9, cx + 4, hy - 1], fill=mix(c, INK, 0.55)); d.rectangle([cx - 6, hy, cx + 6, y1 - w], fill=mix(c, INK, 0.55))
            e.ellipse([cx - 4, hy - 9, cx + 4, hy - 1], fill=(30, 25, 20)); e.rectangle([cx - 6, hy, cx + 6, y1 - w], fill=(30, 25, 20))
    if plant:
        d.rectangle([x0 - 2, y1, x1 + 2, y1 + 6], fill=(150, 104, 76), outline=ink, width=2)
        for k in range(x0, x1, 7):
            d.ellipse([k, y1 - 6, k + 9, y1 + 2], fill=r.choice([(120, 176, 120), (98, 156, 110), (232, 140, 160)]), outline=ink, width=1)


def facade(name, size, wall, glass, cols, rows, win, lit_p, arch=False, sill=None, band=None, plant_p=0.0, trim=None, extra=None):
    S = size; base = Image.new('RGB', (S, S), wall); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    cw, ch = S // cols, S // rows
    for j in range(rows):
        y = j * ch
        if band: d.rectangle([0, y + ch - band[1], S, y + ch], fill=band[0]); d.line([0, y + ch - band[1], S, y + ch - band[1]], fill=INK, width=2)
        for i in range(cols):
            x = i * cw
            ww, wh = win
            box = [x + (cw - ww) // 2, y + (ch - wh) // 2 - (band[1] // 2 if band else 0), x + (cw + ww) // 2, y + (ch + wh) // 2 - (band[1] // 2 if band else 0)]
            if sill: d.rectangle([box[0] - 3, box[3], box[2] + 3, box[3] + sill], fill=mix(wall, (255, 255, 255), 0.35), outline=INK, width=2)
            window(d, e, box, glass, lit_p, arch=arch, plant=r.random() < plant_p)
            if extra: extra(d, e, box, x, y, cw, ch)
    if trim:
        for i in range(0, S, cw): d.line([i, 0, i, S], fill=trim, width=2)
    save('T_MP_' + name, base); save('T_MP_' + name + '_E', em.filter(ImageFilter.GaussianBlur(0.6)))
    return base


def shutters(d, e, box, x, y, cw, ch):
    if r.random() < 0.35:
        x0, y0, x1, y1 = box; sw = (x1 - x0) // 3
        col = r.choice([(96, 150, 146), (214, 156, 108), (150, 132, 196)])
        for xa in (x0 - sw - 2, x1 + 2):
            d.rectangle([xa, y0, xa + sw, y1], fill=col, outline=INK, width=2)
            for k in range(y0 + 5, y1 - 2, 5): d.line([xa + 2, k, xa + sw - 2, k], fill=mix(col, INK, 0.3))


def railing(d, e, box, x, y, cw, ch):
    x0, y0, x1, y1 = box
    d.rectangle([x + 4, y1 - 12, x + cw - 4, y1 + 4], outline=INK, width=2)
    for k in range(x + 8, x + cw - 6, 7): d.line([k, y1 - 12, k, y1 + 4], fill=INK, width=2)
    d.rectangle([x + 2, y1 + 4, x + cw - 2, y1 + 9], fill=(236, 226, 206), outline=INK, width=2)
    if r.random() < 0.4:   # a potted plant on the balcony
        px = r.randint(x + 10, x + cw - 20)
        d.rectangle([px, y1 - 8, px + 10, y1 + 4], fill=(196, 120, 90), outline=INK, width=1)
        d.ellipse([px - 6, y1 - 22, px + 16, y1 - 4], fill=(108, 168, 120), outline=INK, width=2)


def gear_tag(d, e, box, x, y, cw, ch):
    if r.random() < 0.12:
        cx, cy = x + cw - 14, y + 14
        d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=(214, 182, 110), outline=INK, width=2); d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=INK)


# Moonlit Plaza glass: teal curtain wall, thick mullions, cel reflections
facade('Glass', 512, (66, 88, 112), (110, 160, 180), 8, 8, (54, 52), 0.22, trim=INK)
# plaster town houses (was concrete): warm cream with shutters + sills
facade('Concrete', 512, (232, 214, 186), (120, 156, 176), 6, 8, (42, 40), 0.3, sill=5, extra=shutters, plant_p=0.2)
# residential balconies (lilac)
facade('Balcony', 512, (196, 184, 220), (120, 150, 186), 4, 8, (92, 38), 0.4, extra=railing)
# clockwork brick works: brown brick, arched factory windows, little gear badges
def brick():
    S = 512; base = Image.new('RGB', (S, S), (150, 102, 84)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for yy in range(0, S, 16):
        off = 0 if (yy // 16) % 2 == 0 else 16
        for xx in range(-off, S, 32):
            v = r.randint(-6, 6); d.rectangle([xx + 1, yy + 1, xx + 30, yy + 14], fill=(158 + v, 106 + v, 88 + v), outline=(120, 80, 70))
    for j in range(4):
        for i in range(4):
            x, y = i * 128, j * 128
            window(d, e, [x + 24, y + 22, x + 104, y + 112], (110, 140, 150), 0.35, arch=True, curtain=False)
            for k in range(1, 3): d.line([x + 24 + k * 27, y + 60, x + 24 + k * 27, y + 112], fill=INK, width=2)
            d.line([x + 24, y + 86, x + 104, y + 86], fill=INK, width=2)
            gear_tag(d, e, [0, 0, 0, 0], x, y, 128, 128)
    save('T_MP_Industrial', base); save('T_MP_Industrial_E', em.filter(ImageFilter.GaussianBlur(0.6)))
brick()
# neutral painted megablock panels (tinted per building in Unity): rounded windows on a pale grid
def roundwin(d, e, box, x, y, cw, ch):
    pass
facade('Panel', 512, (226, 222, 214), (130, 150, 170), 8, 8, (40, 34), 0.25, band=((210, 204, 194), 6))

# ---- streets: flat painted asphalt, clean lines (no wet noise / puddles)
S = 512
road = Image.new('RGB', (S, S), (74, 70, 92)); d = ImageDraw.Draw(road)
for _ in range(60):   # soft painterly patches, two tones only
    x, y = r.randint(0, S), r.randint(0, S); w, h = r.randint(30, 90), r.randint(14, 40)
    d.ellipse([x, y, x + w, y + h], fill=(80, 76, 98))
for y in range(0, S, 128): d.rounded_rectangle([S // 2 - 5, y + 10, S // 2 + 5, y + 78], 4, fill=(246, 214, 120))
for x in (16, S - 26): d.rectangle([x, 0, x + 10, S], fill=(236, 230, 222))
save('T_MP_RoadWet', road)
cross = Image.new('RGB', (S, S), (74, 70, 92)); d = ImageDraw.Draw(cross)
for x in range(0, S, 64): d.rounded_rectangle([x + 8, 6, x + 46, S - 6], 6, fill=(246, 222, 150), outline=INK, width=3)
save('T_MP_CrossYellow', cross)
walk = Image.new('RGB', (S, S), (196, 186, 178)); d = ImageDraw.Draw(walk)
for y in range(0, S, 64):
    for x in range(0, S, 64):
        v = r.randint(-6, 6); d.rounded_rectangle([x + 3, y + 3, x + 60, y + 60], 6, fill=(204 + v, 194 + v, 186 + v), outline=(150, 140, 146), width=2)
save('T_MP_Sidewalk', walk)
print('city art v3 done')
