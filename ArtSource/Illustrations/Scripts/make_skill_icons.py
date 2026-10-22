"""M18 skill-bar icons: chunky white glyphs with a plum ink outline (the HUD tints them). 256 px, transparent."""
import math, sys
from PIL import Image, ImageDraw, ImageFilter
OUT = sys.argv[1]
S = 256

def mitt(d, cx, cy, r, ang=0):
    # a cartoon boxing mitt: big round fist + thumb bump + cuff
    ca, sa = math.cos(ang), math.sin(ang)
    def P(x, y): return (cx + x * ca - y * sa, cy + x * sa + y * ca)
    def circ(x, y, rr): X, Y = P(x, y); d.ellipse([X - rr, Y - rr, X + rr, Y + rr], fill=255)
    circ(0, 0, r); circ(-r * 0.55, r * 0.45, r * 0.5); circ(r * 0.35, -r * 0.35, r * 0.6)
    cuff = [P(-r * 1.55, -r * 0.55), P(-r * 0.6, -r * 0.75), P(-r * 0.6, r * 0.75), P(-r * 1.55, r * 0.55)]
    d.polygon(cuff, fill=255)

def finish(mask, name):
    ink = mask.filter(ImageFilter.MaxFilter(17))
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    img.paste((26, 18, 41, 255), (0, 0), ink)
    img.paste((255, 255, 255, 255), (0, 0), mask)
    img.save(f"{OUT}/SK_{name}.png")

def new(): m = Image.new("L", (S, S), 0); return m, ImageDraw.Draw(m)

# COMBO: two mitts punching with impact ticks
m, d = new(); mitt(d, 150, 110, 42, 0); mitt(d, 120, 165, 34, 0.15)
for a in (-0.6, 0, 0.6): d.line([200 + 22 * math.cos(a), 110 + 22 * math.sin(a), 200 + 46 * math.cos(a), 110 + 46 * math.sin(a)], fill=255, width=12)
finish(m, "Combo")
# SLAM: double fist hammering a crack
m, d = new(); mitt(d, 128, 92, 46, math.pi / 2)
d.line([40, 205, 216, 205], fill=255, width=14)
for x, h in ((70, 30), (128, 44), (186, 30)): d.polygon([(x - 12, 196), (x, 196 - h), (x + 12, 196)], fill=255)
finish(m, "Slam")
# AIR SPIN: swirl around a fist
m, d = new()
for i in range(3):
    a0 = i * 120
    d.arc([30, 40, 226, 216], a0, a0 + 80, fill=255, width=18)
mitt(d, 128, 128, 34, 0.3)
finish(m, "Spin")
# RISING: uppercut mitt with up arrow
m, d = new(); mitt(d, 128, 120, 42, -math.pi / 2)
d.polygon([(128, 18), (88, 58), (168, 58)], fill=255)
d.arc([60, 150, 196, 250], 200, 340, fill=255, width=14)
finish(m, "Rising")
# DASH PUNCH: rocket mitt with speed lines
m, d = new(); mitt(d, 170, 128, 44, 0)
for y, l in ((90, 70), (128, 100), (166, 70)): d.line([110 - l, y, 100, y], fill=255, width=14)
finish(m, "Dash")
# COUNTER: star + mitt
m, d = new()
pts = []
for i in range(10):
    r = 110 if i % 2 == 0 else 50; a = -math.pi / 2 + i * math.pi / 5
    pts.append((128 + r * math.cos(a), 132 + r * math.sin(a)))
d.polygon(pts, fill=255)
cut = Image.new("L", (S, S), 0); cd = ImageDraw.Draw(cut); mitt(cd, 132, 136, 30, 0)
m = Image.composite(Image.new("L", (S, S), 0), m, cut.filter(ImageFilter.MaxFilter(9)))
m.paste(255, (0, 0), cut)
finish(m, "Counter")
# BURST: radiant burst
m, d = new()
for i in range(12):
    a = i * math.pi / 6; r = 118 if i % 2 == 0 else 86
    d.polygon([(128 + 30 * math.cos(a - 0.25), 128 + 30 * math.sin(a - 0.25)), (128 + r * math.cos(a), 128 + r * math.sin(a)), (128 + 30 * math.cos(a + 0.25), 128 + 30 * math.sin(a + 0.25))], fill=255)
d.ellipse([78, 78, 178, 178], fill=255)
finish(m, "Burst")
print("ok")
