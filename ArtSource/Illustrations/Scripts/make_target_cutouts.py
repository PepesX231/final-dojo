"""
2D cutouts for the Art Target / living room (GDD §5.7 Type A, each with a purpose):
- DEC_PaperPlant    (composition: frames the sofa corner, adds a green accent at the wall line)
- DEC_Poster_Grandpa (story: a child's crayon drawing of Grandpa in his comfy chair — foreshadows the BE A GRANDPA card)
- DEC_Window_Night  (worldbuilding: it is night outside the dream room)
Style: same ink (#302A3D), same paper white cut border, same offset paper shadow as make_cutouts.py.
usage: python3 make_target_cutouts.py <out_dir>
"""
import math, os, random, sys
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
INK = (48, 42, 61, 255); PAPER = (250, 244, 232, 255); S = 4


def finish(im, size, name, border=True):
    if border:  # paper cut border: dilate alpha, paint white under the art, then a soft offset shadow
        a = im.split()[3]
        grown = a.filter(ImageFilter.MaxFilter(13 * S // 2 * 2 + 1))
        base = Image.new('RGBA', im.size, (0, 0, 0, 0))
        shadow = Image.new('RGBA', im.size, (48, 42, 61, 0))
        shadow.putalpha(grown.point(lambda v: int(v * 0.35)))
        base.alpha_composite(shadow, (10 * S // 2, 12 * S // 2))
        white = Image.new('RGBA', im.size, PAPER); white.putalpha(grown)
        base.alpha_composite(white)
        base.alpha_composite(im)
        im = base
    im.resize(size, Image.LANCZOS).save(os.path.join(OUT, name + '.png'))


def wob(d, pts, w, seed=0, closed=True, fill=None):
    r = random.Random(seed)
    q = [(x + r.uniform(-1.5, 1.5) * S, y + r.uniform(-1.5, 1.5) * S) for x, y in pts]
    if fill: d.polygon(q, fill=fill)
    d.line(q + ([q[0]] if closed else []), fill=INK, width=w * S, joint='curve')


# ---------------------------------------------------------------- potted plant
W, H = 512 * S, 768 * S
im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
leaf = (120, 183, 139, 255); leaf2 = (98, 160, 120, 255)
for k, (ang, L, wdt) in enumerate([(-60, 300, 110), (-25, 360, 120), (5, 380, 125), (35, 340, 115), (65, 290, 100), (-90, 240, 90), (95, 230, 90)]):
    a = math.radians(ang - 90)
    bx, by = W / 2, H * 0.62
    tip = (bx + math.cos(a) * L * S, by + math.sin(a) * L * S)
    nx, ny = -math.sin(a), math.cos(a)
    mid = ((bx + tip[0]) / 2, (by + tip[1]) / 2)
    pts = [(bx, by), (mid[0] + nx * wdt / 2 * S, mid[1] + ny * wdt / 2 * S), tip, (mid[0] - nx * wdt / 2 * S, mid[1] - ny * wdt / 2 * S)]
    # smooth leaf outline via many points (quadratic bezier both sides)
    def bez(p0, p1, p2, n=16):
        return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in [i / n for i in range(n + 1)]]
    poly = bez(pts[0], pts[1], pts[2]) + bez(pts[2], pts[3], pts[0])[1:]
    wob(d, poly, 5, k, fill=leaf if k % 2 else leaf2)
    d.line([(bx, by), tip], fill=INK, width=3 * S)
pot = [(W * 0.32, H * 0.62), (W * 0.68, H * 0.62), (W * 0.62, H * 0.95), (W * 0.38, H * 0.95)]
wob(d, pot, 6, 9, fill=(236, 150, 104, 255))
d.rectangle([W * 0.3, H * 0.6, W * 0.7, H * 0.66], fill=(246, 172, 120, 255), outline=INK, width=6 * S)
finish(im, (512, 768), 'DEC_PaperPlant')

# ---------------------------------------------------------------- child's crayon drawing: Grandpa in his chair (in a frame)
W, H = 640 * S, 512 * S
im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.rounded_rectangle([0, 0, W - 1, H - 1], radius=18 * S, fill=(196, 140, 92, 255), outline=INK, width=7 * S)   # wooden frame
d.rectangle([34 * S, 34 * S, W - 34 * S, H - 34 * S], fill=(252, 248, 238, 255), outline=INK, width=4 * S)        # paper
r = random.Random(3)
def crayon_line(pts, col, w=9):
    for k in range(3):   # crayon = several jittery passes
        q = [(x + r.uniform(-3, 3) * S, y + r.uniform(-3, 3) * S) for x, y in pts]
        d.line(q, fill=col, width=int(w * S * 0.8), joint='curve')
# chair (brown crayon)
ch = (150, 96, 60, 255)
crayon_line([(350 * S, 380 * S), (350 * S, 300 * S), (470 * S, 300 * S), (470 * S, 380 * S)], ch)
crayon_line([(350 * S, 300 * S), (350 * S, 170 * S), (470 * S, 170 * S), (470 * S, 300 * S)], ch)
# grandpa apple (red scribble circle) with white moustache + glasses + cane
cx, cy, R = 300 * S, 260 * S, 80 * S
pts = [(cx + math.cos(t / 30 * 2 * math.pi) * R * (1 + r.uniform(-0.04, 0.04)), cy + math.sin(t / 30 * 2 * math.pi) * R * 0.92) for t in range(31)]
d.polygon(pts, fill=(232, 110, 112, 255))
crayon_line(pts, (200, 70, 80, 255), 8)
for gx in (-30, 30):
    d.ellipse([cx + (gx - 18) * S, cy - 38 * S, cx + (gx + 18) * S, cy - 2 * S], outline=INK, width=5 * S)
crayon_line([(cx - 30 * S, cy + 48 * S), (cx, cy + 60 * S), (cx + 30 * S, cy + 48 * S)], INK, 5)   # smile
crayon_line([(cx - 42 * S, cy + 22 * S), (cx, cy + 14 * S), (cx + 42 * S, cy + 22 * S)], (245, 245, 245, 255), 12)  # moustache
crayon_line([(cx - 105 * S, cy + 20 * S), (cx - 110 * S, cy + 130 * S)], (120, 80, 50, 255), 8)
# little sun + "hearts" in the corner, all crayon
crayon_line([(120 * S, 100 * S), (150 * S, 80 * S), (170 * S, 110 * S), (140 * S, 135 * S), (120 * S, 100 * S)], (250, 196, 90, 255), 10)
finish(im, (640, 512), 'DEC_Poster_Grandpa', border=False)

# ---------------------------------------------------------------- night window (drawn, with curtains)
W, H = 512 * S, 640 * S
im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.rounded_rectangle([40 * S, 30 * S, W - 40 * S, H - 90 * S], radius=150 * S, fill=(58, 48, 92, 255), outline=INK, width=8 * S)
d.ellipse([300 * S, 110 * S, 380 * S, 190 * S], fill=(250, 238, 200, 255))
d.ellipse([322 * S, 98 * S, 402 * S, 178 * S], fill=(58, 48, 92, 255))
for (sx, sy) in [(140, 160), (210, 260), (360, 320), (120, 380)]:
    d.ellipse([(sx - 6) * S, (sy - 6) * S, (sx + 6) * S, (sy + 6) * S], fill=(250, 238, 200, 255))
d.line([(W / 2, 40 * S), (W / 2, H - 92 * S)], fill=INK, width=8 * S)
d.line([(44 * S, 300 * S), (W - 44 * S, 300 * S)], fill=INK, width=8 * S)
cur = (236, 150, 160, 255)
wob(d, [(20 * S, 20 * S), (130 * S, 20 * S), (100 * S, 330 * S), (150 * S, H - 60 * S), (20 * S, H - 60 * S)], 6, 4, fill=cur)
wob(d, [(W - 20 * S, 20 * S), (W - 130 * S, 20 * S), (W - 100 * S, 330 * S), (W - 150 * S, H - 60 * S), (W - 20 * S, H - 60 * S)], 6, 5, fill=cur)
d.rounded_rectangle([10 * S, H - 100 * S, W - 10 * S, H - 60 * S], radius=10 * S, fill=(232, 208, 178, 255), outline=INK, width=6 * S)
finish(im, (512, 640), 'DEC_Window_Night')
print('target cutouts done')
