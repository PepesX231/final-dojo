"""
Lobby art (docs/LOBBY_DESIGN.md): bedroom textures + "chaos pop" UI sprites. Everything drawn here for this game.
  textures  -> Assets/DreamRush/Art/Bedroom/Textures   (wallpaper, floor, wainscot, posters, photo)
  ui        -> Assets/DreamRush/Resources/DreamRush/Lobby (white/tintable unless noted)
usage: python3 make_lobby_art.py <repo_root>
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

ROOT = sys.argv[1]
TEX = os.path.join(ROOT, 'Assets/DreamRush/Art/Bedroom/Textures'); os.makedirs(TEX, exist_ok=True)
UI = os.path.join(ROOT, 'Assets/DreamRush/Resources/DreamRush/Lobby'); os.makedirs(UI, exist_ok=True)
FONTS = os.path.join(ROOT, 'Assets/DreamRush/Resources/DreamRush/Fonts')
def F(name, size): return ImageFont.truetype(os.path.join(FONTS, name), size)

INK = (26, 18, 41); CREAM = (250, 240, 220); RED = (229, 41, 61); LAV = (184, 158, 255)
YEL = (255, 214, 0); CYAN = (40, 220, 210); PINK = (255, 90, 170); BLUE = (46, 75, 216)
SS = 4


def tile_safe(img):
    """make a texture tile: blend with its half-offset copy across the seams."""
    a = np.asarray(img).astype(np.float32); h, w = a.shape[:2]
    b = np.roll(np.roll(a, h // 2, 0), w // 2, 1)
    y = np.abs(np.linspace(-1, 1, h))[:, None]; x = np.abs(np.linspace(-1, 1, w))[None, :]
    m = np.clip(np.maximum(x, y) * 1.6 - 0.6, 0, 1)[..., None]
    return Image.fromarray((a * (1 - m) + b * m).astype(np.uint8))


# ------------------------------------------------------------------------------------------------ textures
def wallpaper():
    """soft vertical stripes + scattered stars/moons/apples; near-white so the theme tints it."""
    S = 512; img = Image.new('RGB', (S * 2, S * 2), (246, 244, 250)); d = ImageDraw.Draw(img)
    for i in range(0, S * 2, 64):
        d.rectangle([i, 0, i + 26, S * 2], fill=(236, 233, 246))
    r = random.Random(4)
    def star(cx, cy, R, c):
        pts = []
        for k in range(10):
            a = k / 10 * 2 * math.pi - math.pi / 2; rr = R if k % 2 == 0 else R * 0.45
            pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
        d.polygon(pts, fill=c)
    grid = 128
    for gy in range(0, S * 2, grid):
        for gx in range(0, S * 2, grid):
            ox = (gy // grid % 2) * grid // 2
            cx, cy = gx + ox + grid // 2 + r.randint(-6, 6), gy + grid // 2 + r.randint(-6, 6)
            kind = (gx // grid + gy // grid * 3) % 3
            col = (200, 192, 226)
            if kind == 0: star(cx % (S * 2), cy, 20, col)
            elif kind == 1:   # crescent
                d.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], fill=col)
                d.ellipse([cx - 8, cy - 22, cx + 24, cy + 12], fill=(246, 244, 250) if (cx // 64) % 2 else (236, 233, 246))
            else:   # tiny apple
                d.ellipse([cx - 15, cy - 13, cx + 15, cy + 15], fill=(232, 210, 222))
                d.line([cx, cy - 13, cx + 3, cy - 22], fill=(200, 190, 210), width=4)
                d.ellipse([cx + 3, cy - 26, cx + 15, cy - 18], fill=(210, 226, 210))
            if r.random() < 0.5:
                d.ellipse([cx + 40, cy + 30, cx + 46, cy + 36], fill=col)
    img = img.resize((S, S), Image.LANCZOS)
    img.save(os.path.join(TEX, 'T_BR_Wallpaper.png'))


def floor():
    S = 512; a = np.zeros((S, S, 3), np.float32); r = np.random.default_rng(3)
    plank_h = S // 6
    base = np.array([196, 134, 92], np.float32)
    for i in range(6):
        y0 = i * plank_h; tone = r.uniform(-14, 14)
        off = r.integers(0, S)
        for x in range(S):
            seam = ((x + off) % (S // 2)) < 3
            for y in range(y0, y0 + plank_h):
                g = math.sin((x + off) * 0.035 + math.sin(y * 0.2 + i) * 2.4) * 6 + math.sin(x * 0.4 + y) * 1.5
                c = base + tone + g
                if seam or y - y0 < 3: c = c * 0.62
                a[y, x] = c
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    img.save(os.path.join(TEX, 'T_BR_Floor.png'))


def wainscot():
    S = 256; img = Image.new('RGB', (S, S), (250, 248, 252)); d = ImageDraw.Draw(img)
    for x in range(0, S, 32):
        d.rectangle([x, 0, x + 3, S], fill=(214, 208, 228)); d.rectangle([x + 4, 0, x + 6, S], fill=(255, 255, 255))
    img.save(os.path.join(TEX, 'T_BR_Wainscot.png'))


def halftone(d, box, color, step=14, maxr=6, direction=(1, 0)):
    x0, y0, x1, y1 = box
    for y in range(y0, y1, step):
        for x in range(x0, x1, step):
            t = ((x - x0) / max(1, x1 - x0)) * direction[0] + ((y - y0) / max(1, y1 - y0)) * direction[1]
            rr = maxr * max(0.0, min(1.0, t))
            if rr > 0.6: d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=color)


def ransom(d, x, y, text, fonts, size, colors, r, pad=10, tilt=8):
    """P5-style ransom-note lettering: each letter its own tile, font, colour, tilt."""
    out = []
    for ch in text:
        if ch == ' ': x += size * 0.45; continue
        f = F(r.choice(fonts), size)
        bg, fg = r.choice(colors)
        bb = f.getbbox(ch); w, h = bb[2] - bb[0], bb[3] - bb[1]
        tile = Image.new('RGBA', (w + pad * 2, h + pad * 2), bg + (255,))
        td = ImageDraw.Draw(tile); td.text((pad - bb[0], pad - bb[1]), ch, font=f, fill=fg)
        tile = tile.rotate(r.uniform(-tilt, tilt), expand=True, resample=Image.BICUBIC)
        out.append((tile, int(x), int(y + r.uniform(-size * 0.08, size * 0.08))))
        x += tile.width * 0.92
    return out, x


def posters():
    r = random.Random(7)
    # 1 APPLE ASTRONAUT — pop sunburst, halftone, big type
    W, H = 600, 800
    img = Image.new('RGBA', (W, H), BLUE + (255,)); d = ImageDraw.Draw(img)
    for k in range(24):
        a0 = k / 24 * 2 * math.pi
        d.polygon([(W / 2, H * 0.45), (W / 2 + math.cos(a0) * 900, H * 0.45 + math.sin(a0) * 900),
                   (W / 2 + math.cos(a0 + 0.13) * 900, H * 0.45 + math.sin(a0 + 0.13) * 900)], fill=(70, 105, 235, 255))
    halftone(d, (0, 0, W, H), (90, 130, 255, 255), 16, 5, (0, 1))
    d.ellipse([170, 200, 430, 460], fill=RED + (255,), outline=INK + (255,), width=10)          # apple
    d.ellipse([215, 260, 385, 400], fill=(200, 235, 255, 255), outline=INK + (255,), width=8)   # helmet glass
    d.ellipse([255, 300, 290, 340], fill=INK + (255,)); d.ellipse([310, 300, 345, 340], fill=INK + (255,))
    d.line([300, 200, 312, 160], fill=INK + (255,), width=10); d.ellipse([308, 140, 360, 172], fill=(120, 210, 110, 255), outline=INK + (255,), width=6)
    for k in range(5):
        sx, sy = r.randint(30, W - 30), r.randint(30, 180)
        d.regular_polygon((sx, sy, r.randint(10, 22)), 4, rotation=45, fill=YEL + (255,))
    t, _ = ransom(d, 40, 540, 'APPLE', ['Kanit-BlackItalic.ttf', 'Bungee-Regular.ttf', 'DelaGothicOne-Regular.ttf'], 86,
                  [(YEL, INK), (CREAM, RED), (INK, CREAM), (RED, CREAM)], r)
    for tile, x, y in t: img.alpha_composite(tile, (x, y))
    t, _ = ransom(d, 30, 660, 'ASTRONAUT', ['Kanit-BlackItalic.ttf', 'BowlbyOne-Regular.ttf', 'RubikMonoOne-Regular.ttf'], 54,
                  [(CREAM, INK), (PINK, CREAM), (CYAN, INK), (YEL, INK)], r)
    for tile, x, y in t: img.alpha_composite(tile, (x, y))
    img.convert('RGB').save(os.path.join(TEX, 'T_BR_Poster1.png'))

    # 2 DREAM RUSH — gig poster, pink/yellow, tape
    img = Image.new('RGBA', (W, H), PINK + (255,)); d = ImageDraw.Draw(img)
    d.polygon([(0, 260), (W, 120), (W, 520), (0, 660)], fill=YEL + (255,))
    halftone(d, (0, 0, W, H), (255, 140, 200, 255), 14, 6, (1, 0))
    t, _ = ransom(d, 30, 210, 'DREAM', ['Kanit-BlackItalic.ttf', 'DelaGothicOne-Regular.ttf', 'Bungee-Regular.ttf'], 110,
                  [(INK, YEL), (CREAM, RED), (RED, CREAM), (INK, CREAM)], r)
    for tile, x, y in t: img.alpha_composite(tile, (x, y))
    t, _ = ransom(d, 90, 370, 'RUSH!', ['Kanit-BlackItalic.ttf', 'BowlbyOne-Regular.ttf', 'Bungee-Regular.ttf'], 120,
                  [(BLUE, CREAM), (INK, PINK), (CREAM, INK)], r)
    for tile, x, y in t: img.alpha_composite(tile, (x, y))
    d.text((40, 640), 'LIVE TONIGHT IN YOUR DREAMS', font=F('ChakraPetch-Bold.ttf', 30), fill=INK + (255,))
    d.text((40, 690), 'ฝันให้สุด - 00:00', font=F('Kanit-BlackItalic.ttf', 44), fill=CREAM + (255,), stroke_width=4, stroke_fill=INK + (255,))
    img.convert('RGB').save(os.path.join(TEX, 'T_BR_Poster2.png'))

    # 3 WANTED: THE CHAIR
    img = Image.new('RGBA', (W, H), (244, 228, 190, 255)); d = ImageDraw.Draw(img)
    d.rectangle([20, 20, W - 20, H - 20], outline=INK + (255,), width=8)
    d.text((W / 2, 110), 'WANTED', font=F('BowlbyOne-Regular.ttf', 110), fill=RED + (255,), anchor='mm')
    # chair silhouette
    d.rectangle([220, 260, 380, 290], fill=INK + (255,)); d.rectangle([220, 160, 240, 290], fill=INK + (255,)); d.rectangle([360, 160, 380, 290], fill=INK + (255,))
    for k in range(3): d.rectangle([240, 180 + k * 28, 360, 192 + k * 28], fill=INK + (255,))
    for x in (225, 365): d.rectangle([x, 290, x + 12, 420], fill=INK + (255,))
    d.ellipse([262, 205, 282, 225], fill=CREAM + (255,)); d.ellipse([318, 205, 338, 225], fill=CREAM + (255,))
    d.text((W / 2, 500), 'THE CHAIR', font=F('DelaGothicOne-Regular.ttf', 70), fill=INK + (255,), anchor='mm')
    d.text((W / 2, 580), 'refuses to be sat on', font=F('ChakraPetch-Bold.ttf', 34), fill=INK + (255,), anchor='mm')
    d.text((W / 2, 680), 'REWARD: 1 COMFY NAP', font=F('Kanit-BlackItalic.ttf', 46), fill=RED + (255,), anchor='mm')
    img.convert('RGB').save(os.path.join(TEX, 'T_BR_Poster3.png'))

    # 4 family photo (4 apples squinting)
    img = Image.new('RGBA', (600, 450), (255, 236, 196, 255)); d = ImageDraw.Draw(img)
    d.rectangle([0, 300, 600, 450], fill=(150, 210, 120, 255))
    for i, (x, s, c) in enumerate([(110, 90, RED), (250, 100, (240, 90, 80)), (390, 60, RED), (500, 80, (200, 60, 70))]):
        y = 300 - s * 0.6
        d.ellipse([x - s / 2 * 1.1, y - s / 2, x + s / 2 * 1.1, y + s / 2], fill=c + (255,), outline=INK + (255,), width=5)
        d.arc([x - s * 0.25, y - s * 0.12, x - s * 0.05, y + s * 0.04], 200, 340, fill=INK + (255,), width=5)
        d.arc([x + s * 0.05, y - s * 0.12, x + s * 0.25, y + s * 0.04], 200, 340, fill=INK + (255,), width=5)
        d.arc([x - s * 0.18, y + s * 0.05, x + s * 0.18, y + s * 0.3], 20, 160, fill=INK + (255,), width=5)
    d.ellipse([480, 40, 560, 120], fill=(255, 220, 90, 255))
    img.convert('RGB').save(os.path.join(TEX, 'T_BR_Photo.png'))


# ------------------------------------------------------------------------------------------------ UI sprites
def ui():
    r = random.Random(9)
    def save(n, im): im.save(os.path.join(UI, n + '.png'))
    # rough tile (letter background): slightly wobbly quad
    W = H = 128; im = Image.new('RGBA', (W * SS, H * SS)); d = ImageDraw.Draw(im)
    pts = [(6 + r.uniform(-4, 4), 8 + r.uniform(-4, 4)), (122 + r.uniform(-4, 4), 4 + r.uniform(-3, 3)), (124 + r.uniform(-3, 3), 120 + r.uniform(-4, 4)), (4 + r.uniform(-3, 3), 124 + r.uniform(-3, 3))]
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=(255, 255, 255, 255)); save('LB_Tile', im.resize((W, H), Image.LANCZOS))
    # torn tape strip (9-slice horizontally): zig-zag ends
    W, H = 256, 72; im = Image.new('RGBA', (W * SS, H * SS)); d = ImageDraw.Draw(im)
    pts = [(14, 0)];
    pts += [(W - 14 + (6 if k % 2 else -4), k * H / 8) for k in range(9)]
    pts += [(14 + (6 if k % 2 else -4), H - k * H / 8) for k in range(9)]
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=(255, 255, 255, 255)); save('LB_Tape', im.resize((W, H), Image.LANCZOS))
    # jagged highlight (the selected menu item's spiky backdrop)
    W, H = 512, 160; im = Image.new('RGBA', (W * SS, H * SS)); d = ImageDraw.Draw(im)
    pts = []
    n = 26
    for k in range(n):
        t = k / (n - 1); x = 10 + t * (W - 20)
        pts.append((x + r.uniform(-6, 6), 16 + (r.uniform(0, 24) if k % 2 else 0)))
    for k in range(n):
        t = 1 - k / (n - 1); x = 10 + t * (W - 20)
        pts.append((x + r.uniform(-6, 6), H - 16 - (r.uniform(0, 24) if k % 2 else 0)))
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=(255, 255, 255, 255)); save('LB_Jag', im.resize((W, H), Image.LANCZOS))
    # halftone corner gradient (dots grow toward bottom-right)
    S = 512; im = Image.new('RGBA', (S, S)); d = ImageDraw.Draw(im)
    for y in range(0, S, 16):
        for x in range(0, S, 16):
            t = math.hypot(x / S, y / S) / 1.41
            rr = 7.5 * max(0, t - 0.25) / 0.75
            if rr > 0.5: d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(255, 255, 255, 255))
    save('LB_HalfCorner', im)
    # dot tile (repeat)
    im = Image.new('RGBA', (32, 32)); d = ImageDraw.Draw(im); d.ellipse([10, 10, 22, 22], fill=(255, 255, 255, 255)); save('LB_Dot', im)
    # scanlines tile (repeat)
    im = Image.new('RGBA', (8, 8)); d = ImageDraw.Draw(im); d.rectangle([0, 0, 8, 1], fill=(255, 255, 255, 255)); save('LB_Scan', im)
    # grain tile
    a = (np.random.default_rng(1).random((256, 256)) * 255).astype(np.uint8)
    im = Image.fromarray(np.dstack([np.full_like(a, 255)] * 3 + [a // 3])); save('LB_Grain', im)
    # stickers: sparkle, Z, apple, ring, splat, arrow, heart-moon
    def sticker(name, draw_fn, S=256):
        im = Image.new('RGBA', (S * SS, S * SS)); d = ImageDraw.Draw(im); draw_fn(d, S * SS); im = im.resize((S, S), Image.LANCZOS); save(name, im)
    def spark(d, S):
        c = S / 2; pts = []
        for k in range(8):
            a = k / 8 * 2 * math.pi; rr = S * 0.48 if k % 2 == 0 else S * 0.11
            pts.append((c + math.cos(a) * rr, c + math.sin(a) * rr))
        d.polygon(pts, fill=(255, 255, 255, 255))
    sticker('LB_Spark', spark)
    def zed(d, S):
        f = F('BowlbyOne-Regular.ttf', int(S * 0.8)); d.text((S / 2, S / 2), 'Z', font=f, fill=(255, 255, 255, 255), anchor='mm')
    sticker('LB_Z', zed)
    def apple(d, S):
        d.ellipse([S * 0.12, S * 0.22, S * 0.88, S * 0.94], fill=(255, 255, 255, 255))
        d.polygon([(S * 0.5, S * 0.28), (S * 0.56, S * 0.05), (S * 0.62, S * 0.06), (S * 0.56, S * 0.3)], fill=(255, 255, 255, 255))
        d.ellipse([S * 0.58, S * 0.04, S * 0.86, S * 0.2], fill=(255, 255, 255, 255))
    sticker('LB_Apple', apple)
    def ring(d, S): d.ellipse([S * 0.06, S * 0.06, S * 0.94, S * 0.94], outline=(255, 255, 255, 255), width=int(S * 0.08))
    sticker('LB_Ring', ring)
    def splat(d, S):
        c = S / 2; rr = random.Random(3)
        d.ellipse([c - S * 0.3, c - S * 0.3, c + S * 0.3, c + S * 0.3], fill=(255, 255, 255, 255))
        for k in range(11):
            a = rr.uniform(0, 6.28); L = rr.uniform(0.3, 0.47) * S; w = rr.uniform(0.05, 0.1) * S
            ex, ey = c + math.cos(a) * L, c + math.sin(a) * L
            d.line([c, c, ex, ey], fill=(255, 255, 255, 255), width=int(w)); d.ellipse([ex - w, ey - w, ex + w, ey + w], fill=(255, 255, 255, 255))
    sticker('LB_Splat', splat)
    def arrow(d, S):
        d.polygon([(S * 0.05, S * 0.35), (S * 0.6, S * 0.35), (S * 0.6, S * 0.12), (S * 0.97, S * 0.5), (S * 0.6, S * 0.88), (S * 0.6, S * 0.65), (S * 0.05, S * 0.65)], fill=(255, 255, 255, 255))
    sticker('LB_Arrow', arrow)
    def crescent(d, S):
        d.ellipse([S * 0.05, S * 0.05, S * 0.95, S * 0.95], fill=(255, 255, 255, 255))
        d.ellipse([S * 0.32, S * -0.02, S * 1.1, S * 0.72], fill=(0, 0, 0, 0))
    sticker('LB_Moon', crescent)
    # big diagonal slash band (for the menu background) – a long parallelogram with ragged right edge
    W, H = 1024, 256; im = Image.new('RGBA', (W * SS, H * SS)); d = ImageDraw.Draw(im)
    pts = [(60, 0), (W, 0)] + [(W - (r.uniform(0, 30) if k % 2 else 0), k * H / 10) for k in range(11)] + [(W, H), (0, H)]
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=(255, 255, 255, 255)); save('LB_Band', im.resize((W, H), Image.LANCZOS))
    # clock face ring with ticks
    S = 256; im = Image.new('RGBA', (S * SS, S * SS)); d = ImageDraw.Draw(im); c = S * SS / 2
    d.ellipse([SS * 8, SS * 8, S * SS - SS * 8, S * SS - SS * 8], outline=(255, 255, 255, 255), width=SS * 10)
    for k in range(12):
        a = k / 12 * 2 * math.pi; r0 = S * SS * 0.36; r1 = S * SS * (0.43 if k % 3 else 0.41)
        d.line([c + math.cos(a) * r0, c + math.sin(a) * r0, c + math.cos(a) * r1, c + math.sin(a) * r1], fill=(255, 255, 255, 255), width=SS * (8 if k % 3 == 0 else 4))
    save('LB_ClockRing', im.resize((S, S), Image.LANCZOS))


wallpaper(); floor(); wainscot(); posters(); ui()
print('lobby art done')
