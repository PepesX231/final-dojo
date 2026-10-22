"""
MIDNIGHT PALACE v2 textures (M16 — user: "everywhere I look it's the same building"). Reference read: Night City
(Cyberpunk 2077) — NOT copied: no logos, brands or layouts; we only take the lessons (material variety, layered ads,
street-level storefronts, wet roads, yellow crossings). Every brand here is invented for the dream city.
  facades  : Glass (curtain wall), Concrete (strip windows + grime), Balcony (residential), Industrial (louvres),
             Panel (neutral grid for tinted megablocks), Storefront (ground floor shops)
  ads      : T_MP_AdV0..7 (tall, self-lit), T_MP_AdH0..7 (wide, self-lit)
  streets  : T_MP_RoadWet, T_MP_CrossYellow
usage: python3 make_city_art_v2.py <repo_root>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = sys.argv[1]
OUT = os.path.join(ROOT, 'Assets/DreamRush/Art/MidnightPalace/Textures'); os.makedirs(OUT, exist_ok=True)
FONTS = os.path.join(ROOT, 'Assets/DreamRush/Resources/DreamRush/Fonts')
CJK = '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc'
def F(n, s):
    if n == 'cjk': return ImageFont.truetype(CJK, s, index=0)
    return ImageFont.truetype(os.path.join(FONTS, n), s)
def save(n, im): im.save(os.path.join(OUT, n + '.png'))
r = random.Random(1616); rng = np.random.default_rng(1616)
LIT = [(255, 214, 150), (255, 196, 120), (205, 235, 255), (150, 220, 255), (255, 240, 210)]


def grime(im, amt=14, streaks=True):
    a = np.asarray(im).astype(np.float32)
    n = rng.normal(0, amt, a.shape[:2])[..., None]
    a += n
    if streaks:   # rain streaks under ledges
        H, W = a.shape[:2]
        for _ in range(W // 6):
            x = rng.integers(0, W); y0 = rng.integers(0, H); L = rng.integers(20, H // 2)
            a[y0:y0 + L, x:x + rng.integers(1, 4)] *= rng.uniform(0.82, 0.95)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def lit(c, k=1.0): return tuple(int(v * k) for v in c)


# ------------------------------------------------------------------ facades
def glass():
    S = 512; base = Image.new('RGB', (S, S), (16, 34, 52)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for gy in range(0, S, 32):
        floor_on = r.random() < 0.3
        for gx in range(0, S, 32):
            sh = r.randint(-6, 6)
            d.rectangle([gx + 1, gy + 1, gx + 30, gy + 30], fill=(18 + sh, 44 + sh, 70 + sh * 2))
            # sky reflection gradient
            d.line([gx + 2, gy + 3, gx + 29, gy + 3], fill=(60, 110, 150))
            if (floor_on and r.random() < 0.7) or r.random() < 0.07:
                c = r.choice(LIT[2:] if floor_on else LIT); k = r.uniform(0.5, 1.0)
                e.rectangle([gx + 2, gy + 4, gx + 29, gy + 29], fill=lit(c, k)); d.rectangle([gx + 2, gy + 4, gx + 29, gy + 29], fill=lit(c, 0.45 * k))
        d.rectangle([0, gy + 30, S, gy + 31], fill=(120, 150, 170))
    for gx in range(0, S, 32): d.rectangle([gx, 0, gx, S], fill=(90, 120, 140))
    save('T_MP_Glass', base); save('T_MP_Glass_E', em.filter(ImageFilter.GaussianBlur(0.8)))


def concrete():
    S = 512; base = Image.new('RGB', (S, S), (118, 112, 106)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for gy in range(0, S, 40):
        d.rectangle([0, gy, S, gy + 3], fill=(92, 88, 84))
        for gx in range(0, S, 64):
            w = r.choice([48, 52, 56])
            d.rectangle([gx + 4, gy + 14, gx + 4 + w, gy + 28], fill=(26, 30, 40))
            if r.random() < 0.3:
                c = r.choice(LIT); k = r.uniform(0.6, 1.0)
                d.rectangle([gx + 4, gy + 14, gx + 4 + w, gy + 28], fill=lit(c, 0.5 * k)); e.rectangle([gx + 4, gy + 14, gx + 4 + w, gy + 28], fill=lit(c, k))
            if r.random() < 0.15: d.rectangle([gx + 10, gy + 30, gx + 26, gy + 37], fill=(70, 72, 78))   # AC box
    save('T_MP_Concrete', grime(base, 10)); save('T_MP_Concrete_E', em.filter(ImageFilter.GaussianBlur(0.8)))


def balcony():
    S = 512; base = Image.new('RGB', (S, S), (214, 206, 190)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for gy in range(0, S, 64):
        d.rectangle([0, gy + 46, S, gy + 52], fill=(240, 236, 226))             # slab edge
        for gx in range(0, S, 128):
            d.rectangle([gx + 8, gy + 6, gx + 120, gy + 44], fill=(30, 36, 52))  # glass doors
            if r.random() < 0.4:
                c = r.choice(LIT[:2] + [LIT[4]]); k = r.uniform(0.6, 1.0)
                d.rectangle([gx + 8, gy + 6, gx + 120, gy + 44], fill=lit(c, 0.5 * k)); e.rectangle([gx + 8, gy + 6, gx + 120, gy + 44], fill=lit(c, k))
                if r.random() < 0.5: d.rectangle([gx + 70, gy + 6, gx + 120, gy + 44], fill=(150, 90, 110)); e.rectangle([gx + 70, gy + 6, gx + 120, gy + 44], fill=(60, 30, 40))
            for bx in range(gx + 6, gx + 124, 6): d.line([bx, gy + 34, bx, gy + 46], fill=(70, 74, 86))   # rail
            d.line([gx + 6, gy + 34, gx + 122, gy + 34], fill=(70, 74, 86), width=2)
        d.rectangle([0, gy + 52, S, gy + 63], fill=(196, 188, 172))
    save('T_MP_Balcony', grime(base, 8)); save('T_MP_Balcony_E', em.filter(ImageFilter.GaussianBlur(0.8)))


def industrial():
    S = 512; base = Image.new('RGB', (S, S), (54, 56, 62)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for gx in range(0, S, 8): d.line([gx, 0, gx, S], fill=(44, 46, 52), width=3)          # louvres
    for gy in range(0, S, 128):
        d.rectangle([0, gy, S, gy + 10], fill=(80, 82, 90))
        for gx in range(0, S, 128):
            if r.random() < 0.5:
                d.rectangle([gx + 20, gy + 30, gx + 108, gy + 70], fill=(24, 28, 34))
                if r.random() < 0.6: c = (255, 170, 80) if r.random() < 0.5 else (120, 255, 220); e.rectangle([gx + 20, gy + 30, gx + 108, gy + 70], fill=lit(c, 0.7)); d.rectangle([gx + 20, gy + 30, gx + 108, gy + 70], fill=lit(c, 0.35))
    for x in range(0, S, 40): d.polygon([(x, S - 18), (x + 20, S - 18), (x + 40, S), (x + 20, S)], fill=(230, 170, 30))   # caution stripe
    save('T_MP_Industrial', grime(base, 9)); save('T_MP_Industrial_E', em.filter(ImageFilter.GaussianBlur(0.8)))


def panel():
    """neutral light concrete grid — tinted per building (orange / mint / teal / cream blocks like big painted megablocks)."""
    S = 512; base = Image.new('RGB', (S, S), (220, 220, 220)); em = Image.new('RGB', (S, S)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    for gy in range(0, S, 32):
        for gx in range(0, S, 64): d.rectangle([gx + 2, gy + 2, gx + 61, gy + 29], fill=(206 + r.randint(-8, 8),) * 3)
        if gy % 64 == 0:
            for gx in range(0, S, 16):
                d.rectangle([gx + 3, gy + 10, gx + 12, gy + 22], fill=(40, 44, 56))
                if r.random() < 0.22: c = r.choice(LIT); e.rectangle([gx + 3, gy + 10, gx + 12, gy + 22], fill=lit(c, 0.8)); d.rectangle([gx + 3, gy + 10, gx + 12, gy + 22], fill=lit(c, 0.6))
    save('T_MP_Panel', grime(base, 9)); save('T_MP_Panel_E', em.filter(ImageFilter.GaussianBlur(0.6)))


def storefront():
    """ground-floor band (1024×256): 4 small shops — framed glass, a lit ceiling strip, shelves of colourful goods,
    a counter, a shopper or two, a door; one may be shuttered. Interiors are warm/cool white so neon reads on top."""
    W, H = 1024, 256; base = Image.new('RGB', (W, H), (40, 38, 46)); em = Image.new('RGB', (W, H)); d = ImageDraw.Draw(base); e = ImageDraw.Draw(em)
    interiors = [(255, 236, 205), (225, 240, 255), (255, 214, 190), (230, 255, 236), (255, 228, 240)]
    goods = [(255, 80, 120), (255, 200, 40), (60, 200, 255), (120, 230, 120), (190, 120, 255), (255, 140, 40)]
    for i in range(4):
        x0 = i * 256 + 10; x1 = x0 + 236; top = 64
        d.rectangle([x0 - 8, 20, x1 + 8, H], fill=(58, 56, 66))
        if r.random() < 0.85:
            ic = r.choice(interiors)
            # interior: vertical gradient (brighter at the ceiling), back wall
            for y in range(top, H - 8):
                k = 0.95 - 0.45 * (y - top) / (H - top)
                d.line([x0, y, x1, y], fill=lit(ic, k * 0.55)); e.line([x0, y, x1, y], fill=lit(ic, k * 0.55))
            e.rectangle([x0 + 6, top + 4, x1 - 6, top + 9], fill=(255, 255, 250)); d.rectangle([x0 + 6, top + 4, x1 - 6, top + 9], fill=(250, 250, 245))
            # shelves with goods
            for sy in (top + 34, top + 74, top + 114):
                d.rectangle([x0 + 10, sy, x1 - 70, sy + 4], fill=(90, 84, 96))
                gx = x0 + 14
                while gx < x1 - 76:
                    gw = r.randint(6, 12); gh = r.randint(10, 26); gc = r.choice(goods)
                    d.rectangle([gx, sy - gh, gx + gw, sy], fill=lit(gc, 0.75)); e.rectangle([gx, sy - gh, gx + gw, sy], fill=lit(gc, 0.45)); gx += gw + r.randint(2, 5)
            # counter + shoppers
            d.rectangle([x1 - 64, H - 70, x1 - 8, H - 8], fill=(70, 64, 80))
            for k in range(r.randint(0, 2)):
                px = r.randint(x0 + 30, x1 - 100); col = (34, 30, 44)
                d.ellipse([px, H - 92, px + 22, H - 70], fill=col); d.rounded_rectangle([px - 4, H - 72, px + 26, H - 8], radius=8, fill=col)
            # glass: mullions + a door, reflections
            for mx in range(x0, x1, 59): d.rectangle([mx, top, mx + 3, H - 8], fill=(30, 28, 36))
            d.rectangle([x0, top, x1, top + 3], fill=(30, 28, 36)); d.rectangle([x0, H - 10, x1, H - 6], fill=(30, 28, 36))
            for k in range(3):
                rx = r.randint(x0, x1 - 40); d.line([rx, H - 10, rx + 40, top], fill=lit(ic, 0.75), width=2)
        else:
            for y in range(top, H - 8, 7): d.line([x0, y, x1, y], fill=(104 + r.randint(-6, 6), 100, 112), width=4)
            d.text((x0 + 70, top + 70), r.choice(['CLOSED', 'BRB', 'zzz']), fill=(220, 60, 90), font=F('Bungee-Regular.ttf', 34))
        d.rectangle([x0 - 8, 20, x1 + 8, top - 4], fill=(26, 22, 32))   # sign band (real signs are separate quads)
    save('T_MP_Storefront', base); save('T_MP_Storefront_E', em.filter(ImageFilter.GaussianBlur(0.8)))


# ------------------------------------------------------------------ ads (self-lit screens; invented dream brands)
def grad(W, H, c0, c1, vertical=True):
    a = np.linspace(0, 1, H if vertical else W)
    g = np.array([np.array(c0) * (1 - t) + np.array(c1) * t for t in a])
    img = np.repeat(g[:, None, :], W, 1) if vertical else np.repeat(g[None, :, :], H, 0)
    return Image.fromarray(img.astype(np.uint8))


def mascot(d, kind, cx, cy, s, col):
    if kind == 'apple':
        d.ellipse([cx - s, cy - s * 0.9, cx + s, cy + s], fill=(235, 40, 60)); d.rectangle([cx - s * 0.05, cy - s * 1.3, cx + s * 0.08, cy - s * 0.8], fill=(80, 50, 30))
        d.ellipse([cx + s * 0.05, cy - s * 1.35, cx + s * 0.6, cy - s * 1.0], fill=(90, 210, 110)); d.ellipse([cx - s * 0.5, cy - s * 0.5, cx - s * 0.2, cy - s * 0.15], fill=(255, 200, 210))
    elif kind == 'sheep':
        for a in range(0, 360, 40): d.ellipse([cx + math.cos(math.radians(a)) * s * 0.6 - s * 0.45, cy + math.sin(math.radians(a)) * s * 0.5 - s * 0.45, cx + math.cos(math.radians(a)) * s * 0.6 + s * 0.45, cy + math.sin(math.radians(a)) * s * 0.5 + s * 0.45], fill=(250, 248, 240))
        d.ellipse([cx - s * 0.35, cy - s * 0.3, cx + s * 0.35, cy + s * 0.4], fill=(60, 50, 70)); d.ellipse([cx - s * 0.2, cy - s * 0.05, cx - s * 0.08, cy + s * 0.07], fill=(255, 255, 255)); d.ellipse([cx + s * 0.08, cy - s * 0.05, cx + s * 0.2, cy + s * 0.07], fill=(255, 255, 255))
    elif kind == 'moon':
        d.ellipse([cx - s, cy - s, cx + s, cy + s], fill=(255, 236, 160)); d.ellipse([cx - s * 0.4, cy - s * 1.1, cx + s * 1.4, cy + s * 0.9], fill=col)
    elif kind == 'cup':
        d.rounded_rectangle([cx - s * 0.6, cy - s * 0.7, cx + s * 0.6, cy + s], radius=int(s * 0.2), fill=(250, 250, 255)); d.rectangle([cx - s * 0.6, cy - s * 0.2, cx + s * 0.6, cy + s * 0.3], fill=col)
        d.arc([cx + s * 0.4, cy - s * 0.3, cx + s * 1.0, cy + s * 0.5], 270, 90, fill=(250, 250, 255), width=int(s * 0.15))
        for k in (-1, 0, 1): d.arc([cx + k * s * 0.3 - s * 0.15, cy - s * 1.4, cx + k * s * 0.3 + s * 0.15, cy - s * 0.8], 180, 360, fill=(255, 255, 255), width=4)
    elif kind == 'noodle':
        d.chord([cx - s, cy - s * 0.3, cx + s, cy + s], 0, 180, fill=(240, 60, 50)); d.rectangle([cx - s, cy + s * 0.32, cx + s, cy + s * 0.36], fill=(255, 220, 120))
        for k in range(6): d.arc([cx - s * 0.8 + k * s * 0.25, cy - s * 0.9, cx - s * 0.4 + k * s * 0.25, cy + s * 0.4], 180, 360, fill=(255, 230, 150), width=5)
        d.line([cx + s * 0.2, cy - s * 1.2, cx + s * 0.9, cy + s * 0.1], fill=(140, 90, 60), width=6); d.line([cx + s * 0.4, cy - s * 1.3, cx + s * 1.0, cy], fill=(140, 90, 60), width=6)
    elif kind == 'clock':
        d.ellipse([cx - s, cy - s, cx + s, cy + s], fill=(255, 245, 220), outline=(255, 190, 60), width=int(s * 0.12))
        d.line([cx, cy, cx, cy - s * 0.7], fill=(30, 20, 40), width=int(s * 0.1)); d.line([cx, cy, cx + s * 0.5, cy + s * 0.2], fill=(30, 20, 40), width=int(s * 0.1))
    elif kind == 'eye':
        d.ellipse([cx - s, cy - s * 0.5, cx + s, cy + s * 0.5], fill=(255, 255, 255)); d.ellipse([cx - s * 0.4, cy - s * 0.4, cx + s * 0.4, cy + s * 0.4], fill=col); d.ellipse([cx - s * 0.15, cy - s * 0.15, cx + s * 0.15, cy + s * 0.15], fill=(10, 10, 20))
    elif kind == 'pillow':
        d.rounded_rectangle([cx - s, cy - s * 0.6, cx + s, cy + s * 0.6], radius=int(s * 0.4), fill=(250, 240, 255)); d.line([cx - s * 0.5, cy, cx - s * 0.2, cy], fill=(150, 130, 180), width=4); d.line([cx + s * 0.2, cy, cx + s * 0.5, cy], fill=(150, 130, 180), width=4)


ADS = [  # (headline, sub, font, bg0, bg1, accent, mascot)
    ('PILLOW COLA', 'feel the fluff', 'BowlbyOne-Regular.ttf', (255, 70, 110), (120, 20, 90), (255, 230, 120), 'pillow'),
    ('NEKO RAMEN', 'ラーメン 24時間', 'DelaGothicOne-Regular.ttf', (255, 150, 30), (160, 40, 20), (255, 245, 210), 'noodle'),
    ('MOON MILK', 'ฝันหวานทุกแก้ว', 'Kanit-BlackItalic.ttf', (60, 120, 255), (20, 20, 90), (255, 236, 160), 'cup'),
    ('SNOOZE BANK', 'save every minute', 'RubikMonoOne-Regular.ttf', (20, 200, 170), (10, 50, 80), (255, 255, 255), 'clock'),
    ('ZZZ HOTEL', '眠らない街', 'Bungee-Regular.ttf', (170, 80, 255), (40, 10, 80), (130, 255, 230), 'moon'),
    ('LULLABY .FM', '99.9 — all night', 'ChakraPetch-Bold.ttf', (255, 60, 200), (30, 10, 60), (0, 240, 255), 'sheep'),
    ('DREAM CORP', 'we see your dreams', 'DelaGothicOne-Regular.ttf', (230, 230, 240), (90, 100, 130), (255, 60, 80), 'eye'),
    ('APPLE JUMP', 'ENERGY SODA', 'Bungee-Regular.ttf', (120, 255, 120), (20, 110, 60), (255, 60, 80), 'apple'),
]


def ad(i, W, H, tall):
    head, sub, fnt, c0, c1, acc, m = ADS[i]
    im = grad(W, H, c0, c1); d = ImageDraw.Draw(im)
    for k in range(0, W + H, 40): d.line([k, 0, k - H, H], fill=lit(c0, 1.08) if k % 80 else lit(c1, 0.9), width=10)   # dynamic stripes
    im = Image.blend(im, grad(W, H, c0, c1), 0.55); d = ImageDraw.Draw(im)
    if tall:
        mascot(d, m, W // 2, int(H * 0.36), int(W * 0.33), c1)
        words = head.split(' ')
        y = int(H * 0.6)
        for w in words:
            f = F(fnt, 64); bb = d.textbbox((0, 0), w, font=f); s = 64 * min(1.0, (W - 24) / max(1, bb[2] - bb[0]))
            f = F(fnt, int(s)); d.text((W / 2 + 3, y + 3), w, font=f, fill=(20, 10, 30), anchor='mt'); d.text((W / 2, y), w, font=f, fill=acc, anchor='mt'); y += int(s * 1.05)
        fs = F('cjk' if any(ord(ch) > 0x2E80 for ch in sub) else ('Kanit-Bold.ttf' if any('฀' <= ch <= '๿' for ch in sub) else 'ChakraPetch-Bold.ttf'), 26)
        d.text((W / 2, H - 60), sub, font=fs, fill=(255, 255, 255), anchor='mm')
    else:
        mascot(d, m, int(W * 0.78), H // 2, int(H * 0.3), c1)
        f = F(fnt, 120); bb = d.textbbox((0, 0), head, font=f); s = 120 * min(1.0, (W * 0.58) / max(1, bb[2] - bb[0]))
        f = F(fnt, int(s)); d.text((44 + 5, H * 0.42 + 5), head, font=f, fill=(20, 10, 30), anchor='lm'); d.text((44, H * 0.42), head, font=f, fill=acc, anchor='lm')
        fs = F('cjk' if any(ord(ch) > 0x2E80 for ch in sub) else ('Kanit-Bold.ttf' if any('฀' <= ch <= '๿' for ch in sub) else 'ChakraPetch-Bold.ttf'), 46)
        d.text((48, H * 0.72), sub, font=fs, fill=(255, 255, 255), anchor='lm')
    # screen scanlines + frame
    a = np.asarray(im).astype(np.float32); a[::4] *= 0.88; im = Image.fromarray(a.astype(np.uint8)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=(20, 16, 30), width=6)
    return im


for i in range(len(ADS)):
    save(f'T_MP_AdV{i}', ad(i, 256, 1024, True)); save(f'T_MP_AdH{i}', ad(i, 1024, 512, False))

# ------------------------------------------------------------------ streets
S = 512
a = np.full((S, S, 3), (28, 28, 38), np.float32) + rng.normal(0, 6, (S, S, 1))
for _ in range(40):   # puddles (darker, they'll be shiny)
    cx, cy, rr = rng.integers(0, S, 2).tolist() + [int(rng.integers(10, 50))]
    yy, xx = np.ogrid[:S, :S]; m = ((xx - cx) ** 2 / (rr * 1.8) ** 2 + (yy - cy) ** 2 / rr ** 2) < 1; a[m] *= 0.7
road = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)); d = ImageDraw.Draw(road)
for y in range(0, S, 128): d.rectangle([S // 2 - 4, y, S // 2 + 4, y + 70], fill=(240, 200, 70))
for x in (14, S - 22): d.rectangle([x, 0, x + 8, S], fill=(215, 215, 225))
save('T_MP_RoadWet', road)
cross = Image.new('RGB', (S, S), (30, 30, 40)); d = ImageDraw.Draw(cross)
for x in range(0, S, 64): d.rectangle([x + 6, 0, x + 44, S], fill=(250, 205, 40))
save('T_MP_CrossYellow', grime(cross, 8, False))

walk = Image.new('RGB', (S, S), (92, 94, 102)); d = ImageDraw.Draw(walk)
for y in range(0, S, 64):
    for x in range(0, S, 64): v = 104 + r.randint(-8, 8); d.rectangle([x + 2, y + 2, x + 61, y + 61], fill=(v, v + 2, v + 8))
save('T_MP_Sidewalk', grime(walk, 7, False))
for fn in (glass, concrete, balcony, industrial, panel, storefront): fn()
print('city art v2 done')
