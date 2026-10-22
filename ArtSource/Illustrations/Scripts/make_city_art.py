"""
MIDNIGHT PALACE textures (docs/LEVEL2_DESIGN.md §5): facades with lit-window emission maps, street surfaces,
neon signs and Persona-flavoured "calling card" billboards that foreshadow COUNT MIDNIGHT. All drawn here.
usage: python3 make_city_art.py <repo_root>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = sys.argv[1]
OUT = os.path.join(ROOT, 'Assets/DreamRush/Art/MidnightPalace/Textures'); os.makedirs(OUT, exist_ok=True)
FONTS = os.path.join(ROOT, 'Assets/DreamRush/Resources/DreamRush/Fonts')
def F(n, s): return ImageFont.truetype(os.path.join(FONTS, n), s)
def save(n, im): im.save(os.path.join(OUT, n + '.png'))
INK = (14, 10, 28); WHITE = (250, 246, 240)
r = random.Random(42)

# ------------------------------------------------------------------ facades: base (wall + dark glass) and emission (lit glass)
def facade(name, wall, trim, kind):
    S = 512
    base = Image.new('RGB', (S, S), wall); em = Image.new('RGB', (S, S), (0, 0, 0))
    d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    lit_cols = [(255, 214, 140), (255, 190, 120), (130, 230, 255), (255, 120, 200), (255, 240, 200)]
    if kind == 'apartment':
        for gy in range(0, S, 64):
            d.rectangle([0, gy + 58, S, gy + 63], fill=trim)                    # floor slab line
            for gx in range(0, S, 64):
                x0, y0 = gx + 12, gy + 12
                d.rectangle([x0 - 3, y0 - 3, x0 + 42, y0 + 40], fill=trim)
                d.rectangle([x0, y0, x0 + 39, y0 + 37], fill=(20, 22, 48))
                if r.random() < 0.55:
                    c = r.choice(lit_cols); k = r.uniform(0.6, 1.0)
                    e.rectangle([x0, y0, x0 + 39, y0 + 37], fill=tuple(int(v * k) for v in c))
                    d.rectangle([x0, y0, x0 + 39, y0 + 37], fill=tuple(int(v * 0.55) for v in c))
                    if r.random() < 0.4:   # a silhouette / curtain
                        d.rectangle([x0 + 22, y0 + 4, x0 + 39, y0 + 37], fill=(40, 26, 60)); e.rectangle([x0 + 22, y0 + 4, x0 + 39, y0 + 37], fill=(20, 10, 30))
                d.line([x0 + 19, y0, x0 + 19, y0 + 37], fill=trim, width=2)
    elif kind == 'office':
        for gy in range(0, S, 48):
            d.rectangle([0, gy, S, gy + 14], fill=trim)
            band_lit = r.random() < 0.7
            for gx in range(0, S, 32):
                x0 = gx + 2
                c = r.choice(lit_cols[2:4] + [lit_cols[0]]) if band_lit and r.random() < 0.75 else None
                fill = (18, 26, 52) if c is None else tuple(int(v * 0.5) for v in c)
                d.rectangle([x0, gy + 16, x0 + 28, gy + 45], fill=fill)
                if c: e.rectangle([x0, gy + 16, x0 + 28, gy + 45], fill=tuple(int(v * r.uniform(0.5, 0.9)) for v in c))
    else:   # old shop / brick with arched windows
        for gy in range(0, S, 16):
            off = 0 if (gy // 16) % 2 else 16
            for gx in range(-16, S, 32):
                cc = tuple(max(0, min(255, v + r.randint(-10, 10))) for v in wall)
                d.rectangle([gx + off, gy, gx + off + 30, gy + 14], fill=cc)
        for gy in (60, 316):
            for gx in range(28, S, 128):
                d.rectangle([gx - 6, gy - 6, gx + 78, gy + 156], fill=trim)
                d.rectangle([gx, gy + 30, gx + 72, gy + 150], fill=(22, 18, 40)); d.ellipse([gx, gy, gx + 72, gy + 64], fill=(22, 18, 40))
                if r.random() < 0.7:
                    c = r.choice(lit_cols)
                    for dd in (d, e):
                        col = tuple(int(v * (0.55 if dd is d else 0.9)) for v in c)
                        dd.rectangle([gx + 4, gy + 30, gx + 68, gy + 146], fill=col); dd.ellipse([gx + 4, gy + 4, gx + 68, gy + 62], fill=col)
                d.line([gx + 36, gy + 4, gx + 36, gy + 146], fill=trim, width=3)
    save(f'T_MP_Facade{name}', base); save(f'T_MP_Facade{name}_E', em.filter(ImageFilter.GaussianBlur(0.6)))

facade('A', (52, 40, 92), (88, 70, 140), 'apartment')
facade('B', (26, 36, 70), (60, 84, 140), 'office')
facade('C', (92, 40, 64), (150, 90, 110), 'shop')

# ------------------------------------------------------------------ streets
S = 512
road = Image.new('RGB', (S, S), (34, 30, 52)); d = ImageDraw.Draw(road)
a = np.asarray(road).astype(np.float32); a += np.random.default_rng(1).normal(0, 5, a.shape); road = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)); d = ImageDraw.Draw(road)
for y in range(0, S, 128): d.rectangle([S // 2 - 5, y, S // 2 + 5, y + 64], fill=(250, 210, 80))
for x in (16, S - 24): d.rectangle([x, 0, x + 8, S], fill=(220, 220, 235))
save('T_MP_Road', road)
walk = Image.new('RGB', (S, S), (70, 62, 104)); d = ImageDraw.Draw(walk)
for y in range(0, S, 64):
    for x in range(0, S, 64): d.rectangle([x + 2, y + 2, x + 61, y + 61], fill=(84 + r.randint(-6, 6), 76, 122))
save('T_MP_Sidewalk', walk)
cross = Image.new('RGB', (S, S), (34, 30, 52)); d = ImageDraw.Draw(cross)
for x in range(0, S, 64): d.rectangle([x + 8, 0, x + 40, S], fill=(240, 240, 250))
save('T_MP_Crosswalk', cross)

# ------------------------------------------------------------------ neon signs (base dark board + emission text)
SIGNS = [('NO SLEEP CAFÉ', 'Bungee-Regular.ttf', (255, 60, 170)), ('TICK TOCK BAR', 'BowlbyOne-Regular.ttf', (0, 230, 255)),
         ('24H DREAMS', 'Kanit-BlackItalic.ttf', (255, 220, 60)), ('MIDNIGHT', 'DelaGothicOne-Regular.ttf', (170, 120, 255)),
         ('XII', 'BowlbyOne-Regular.ttf', (255, 70, 90)), ('ฝันดีนะ', 'Kanit-BlackItalic.ttf', (120, 255, 190)),
         ('SNOOZE', 'RubikMonoOne-Regular.ttf', (255, 150, 40)), ('OPEN LATE', 'Bungee-Regular.ttf', (90, 200, 255)),
         ('MOON MART', 'BowlbyOne-Regular.ttf', (255, 240, 140)), ('CLOCKWORK', 'DelaGothicOne-Regular.ttf', (255, 190, 80)),
         ('อย่าเพิ่งตื่น', 'Kanit-BlackItalic.ttf', (255, 90, 200)), ('COUNT ME IN', 'Bungee-Regular.ttf', (0, 255, 200))]
for i, (txt, fnt, col) in enumerate(SIGNS):
    W, H = 512, 192
    base = Image.new('RGB', (W, H), (18, 12, 32)); em = Image.new('RGB', (W, H), (0, 0, 0))
    for im, k in ((base, 0.55), (em, 1.0)):
        dd = ImageDraw.Draw(im)
        c = tuple(int(v * k) for v in col)
        dd.rounded_rectangle([8, 8, W - 8, H - 8], radius=22, outline=c, width=7)
        f = F(fnt, 96)
        bb = dd.textbbox((0, 0), txt, font=f); tw = bb[2] - bb[0]
        size = 96 if tw < W - 70 else int(96 * (W - 70) / tw)
        f = F(fnt, size)
        dd.text((W / 2, H / 2), txt, font=f, fill=c, anchor='mm')
    em = Image.blend(em.filter(ImageFilter.GaussianBlur(6)), em, 0.6)
    save(f'T_MP_Sign{i:02d}', base); save(f'T_MP_Sign{i:02d}_E', em)

# ------------------------------------------------------------------ calling-card billboards (P5 flavour, our own words)
def ransom(im, x, y, text, size, fonts, pal, tilt=9):
    for ch in text:
        if ch == ' ': x += size * 0.4; continue
        f = F(r.choice(fonts), size); bg, fg = r.choice(pal)
        d0 = ImageDraw.Draw(im); bb = d0.textbbox((0, 0), ch, font=f); w, h = bb[2] - bb[0], bb[3] - bb[1]
        t = Image.new('RGBA', (w + 20, h + 20), bg + (255,)); ImageDraw.Draw(t).text((10 - bb[0], 10 - bb[1]), ch, font=f, fill=fg)
        t = t.rotate(r.uniform(-tilt, tilt), expand=True, resample=Image.BICUBIC)
        im.alpha_composite(t, (int(x), int(y + r.uniform(-size * 0.1, size * 0.1)))); x += t.width * 0.9
    return x

BILL = [('WE WILL STEAL', 'YOUR SLEEP', (230, 30, 70)), ('THE 12TH HOUR', 'IS COMING', (20, 200, 230)), ('SNOOZE?', 'DENIED.', (255, 200, 0)), ('COUNT', 'MIDNIGHT', (160, 70, 255))]
for i, (l1, l2, accent) in enumerate(BILL):
    W, H = 1024, 512
    im = Image.new('RGBA', (W, H), INK + (255,)); d = ImageDraw.Draw(im)
    d.polygon([(0, H * 0.62), (W, H * 0.18), (W, H * 0.48), (0, H)], fill=accent + (255,))
    for y in range(0, H, 18):     # halftone stripe field
        for x in range(0, W, 18):
            rr = 4 * (x / W)
            if rr > 0.8: d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(255, 255, 255, 40))
    # top-hat + clock silhouette (the Count's calling card)
    cx, cy = 820, 190
    d.ellipse([cx - 110, cy - 110, cx + 110, cy + 110], fill=WHITE + (255,)); d.ellipse([cx - 92, cy - 92, cx + 92, cy + 92], fill=INK + (255,))
    d.line([cx, cy, cx, cy - 70], fill=WHITE + (255,), width=12); d.line([cx, cy, cx + 50, cy + 20], fill=WHITE + (255,), width=12)
    d.rectangle([cx - 70, cy - 210, cx + 70, cy - 110], fill=INK + (255,), outline=WHITE + (255,), width=6); d.rectangle([cx - 120, cy - 118, cx + 120, cy - 100], fill=INK + (255,), outline=WHITE + (255,), width=6)
    pal = [(WHITE, INK), (INK, WHITE), (accent, INK), ((255, 255, 255), accent)]
    fonts = ['Kanit-BlackItalic.ttf', 'Bungee-Regular.ttf', 'DelaGothicOne-Regular.ttf', 'BowlbyOne-Regular.ttf']
    ransom(im, 40, 60, l1, 66, fonts, pal)
    ransom(im, 60, 220, l2, 84, fonts, pal)
    d.text((44, 440), '— COUNT MIDNIGHT', font=F('Kanit-BlackItalic.ttf', 46), fill=WHITE + (255,))
    save(f'T_MP_Bill{i}', im.convert('RGB'))
    em = Image.new('RGB', (W, H), (0, 0, 0)); save(f'T_MP_Bill{i}_E', em)   # billboards are lit by spotlights, not self-lit
print('city art done')
