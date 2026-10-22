"""Procedural placeholder paper cutouts (GDD §5.7 Type A). Deterministic (seeded).
Paper look: irregular hand-cut edge, a darker 'thickness' rim offset down-right, faint fibre noise.
These are BLOCKOUT illustrations — replace with hand-drawn art in the art pass (keep names/sizes)."""
import math, random, sys, os
from PIL import Image, ImageDraw, ImageFilter
OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
CREAM=(245,231,211); LAV=(170,160,221); APRICOT=(255,184,112); INK=(48,42,61); SKY=(143,213,237); PLUM=(41,33,55)

def jitter_poly(points, amp, rnd):
    return [(x+rnd.uniform(-amp,amp), y+rnd.uniform(-amp,amp)) for x,y in points]

def paper(shape_fn, size, color, seed, rim=(0,0,0)):
    rnd = random.Random(seed)
    W,H = size
    mask = Image.new('L', size, 0); d = ImageDraw.Draw(mask)
    shape_fn(d, rnd, W, H)
    mask = mask.filter(ImageFilter.GaussianBlur(0.8)).point(lambda v: 255 if v>128 else 0)
    img = Image.new('RGBA', size, (0,0,0,0))
    # thickness rim: shifted darker copy
    rimc = tuple(int(c*0.72) for c in color)
    shifted = Image.new('L', size, 0); shifted.paste(mask, (6,7))
    img.paste(Image.new('RGBA', size, rimc+(255,)), (0,0), shifted)
    # paper face with fibre noise
    face = Image.new('RGBA', size, color+(255,))
    px = face.load()
    for y in range(0,H,2):
        for x in range(0,W,2):
            n = rnd.randint(-6,6)
            c = tuple(max(0,min(255,v+n)) for v in color)
            for dy in (0,1):
                for dx in (0,1):
                    if x+dx<W and y+dy<H: px[x+dx,y+dy]=c+(255,)
    img.paste(face, (0,0), mask)
    # soft ink outline (selective outline, §5.6)
    edge = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3))
    img.paste(Image.new('RGBA', size, INK+(200,)), (0,0), edge)
    return img

def cloud(d, rnd, W, H):
    cx, cy = W/2, H*0.58
    blobs = [(-0.28,0.05,0.22),(-0.08,-0.12,0.27),(0.15,-0.05,0.24),(0.32,0.08,0.17),(0.0,0.12,0.25)]
    for bx,by,r in blobs:
        r *= W*(0.95+rnd.uniform(-0.05,0.05))
        pts=[(cx+bx*W+math.cos(a)*r, cy+by*H+math.sin(a)*r*0.8) for a in [i/40*2*math.pi for i in range(40)]]
        d.polygon(jitter_poly(pts, W*0.006, rnd), fill=255)
    d.rectangle([cx-W*0.38, cy, cx+W*0.40, cy+H*0.22], fill=255)

def star(d, rnd, W, H):
    cx, cy = W/2, H/2; R=W*0.44; r=W*0.2; pts=[]
    for i in range(10):
        a = -math.pi/2 + i*math.pi/5
        rr = (R if i%2==0 else r)*(1+rnd.uniform(-0.04,0.04))
        pts.append((cx+math.cos(a)*rr, cy+math.sin(a)*rr))
    d.polygon(jitter_poly(pts, W*0.008, rnd), fill=255)

def moon(d, rnd, W, H):
    d.ellipse([W*0.08,H*0.08,W*0.92,H*0.92], fill=255)
    d.ellipse([W*0.32,H*0.02,W*1.08,H*0.78], fill=0)

def plant(d, rnd, W, H):
    d.rectangle([W*0.47,H*0.45,W*0.53,H*0.98], fill=255)
    for i,(ox,oy,s) in enumerate([(-0.18,0.35,0.22),(0.17,0.28,0.24),(0.0,0.12,0.2),(-0.12,0.6,0.16),(0.13,0.55,0.17)]):
        cx,cy=W*(0.5+ox),H*oy; r=W*s
        pts=[(cx+math.cos(a)*r*0.55, cy+math.sin(a)*r) for a in [k/30*2*math.pi for k in range(30)]]
        d.polygon(jitter_poly(pts, W*0.006, rnd), fill=255)

items = [('DEC_PaperCloud_A', cloud, (512,320), CREAM, 1), ('DEC_PaperCloud_B', cloud, (512,320), (232,226,246), 7),
         ('DEC_PaperStar', star, (256,256), (255,214,140), 3), ('DEC_PaperMoon', moon, (256,256), CREAM, 5),
         ('DEC_IllustratedPlant', plant, (256,384), (146,196,160), 11)]
for name, fn, size, col, seed in items:
    paper(fn, size, col, seed).save(os.path.join(OUT, name + '.png'))
    print('wrote', name)
