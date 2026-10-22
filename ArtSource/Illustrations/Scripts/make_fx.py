"""Type B 2D effect sprites (GDD §5.7, §20.5): small, readable, paper-cut style. Blockout quality."""
import math, random, sys, os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
OUT=sys.argv[1]; os.makedirs(OUT, exist_ok=True)
CREAM=(245,231,211,255); INK=(48,42,61,255); APRICOT=(255,184,112,255); SKY=(143,213,237,255); LAV=(170,160,221,255); YEL=(255,214,140,255)
S=4  # supersample

def finish(img, size): return img.resize(size, Image.LANCZOS)

def burst(points=12, r1=0.46, r2=0.34, seed=1, fill=CREAM):
    rnd=random.Random(seed); W=256*S; c=W/2
    im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
    pts=[]
    for i in range(points*2):
        a=i*math.pi/points; r=(r1 if i%2==0 else r2)*W*(1+rnd.uniform(-0.05,0.05))
        pts.append((c+math.cos(a)*r, c+math.sin(a)*r))
    d.polygon([(x+10*S,y+12*S) for x,y in pts], fill=(48,42,61,110))
    d.polygon(pts, fill=fill, outline=INK, width=5*S)
    return im, d, W

# "!" surprise mark
im,d,W=burst(seed=3, fill=YEL)
d.rounded_rectangle([W*0.44,W*0.22,W*0.56,W*0.6], radius=8*S, fill=INK)
d.ellipse([W*0.43,W*0.66,W*0.57,W*0.8], fill=INK)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Exclaim.png'))

# sweat drop
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
cx,cy,r=W*0.5,W*0.62,W*0.24
tip=(cx,W*0.1)
# tangent points of the tip on the circle
dd=cy-tip[1]; a=math.asin(r/dd)
p1=(cx-math.sin(math.pi/2-a)*r*math.cos(a)/math.cos(a)*0+ -r*math.cos(a), cy- r*math.sin(a))
p2=(cx+r*math.cos(a), cy- r*math.sin(a))
shape=[tip,p2]+[(cx+math.cos(t)*r, cy+math.sin(t)*r) for t in [(-a)+k/40*(math.pi+2*a) for k in range(41)]]+[p1]
d.polygon([(x+8*S,y+10*S) for x,y in shape], fill=(48,42,61,110))
d.polygon(shape, fill=SKY, outline=INK, width=5*S)
d.ellipse([W*0.40,W*0.52,W*0.48,W*0.66], fill=(255,255,255,210))
finish(im,(256,256)).save(os.path.join(OUT,'FX_Sweat.png'))

# dizzy stars (stumble)
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
for k,(cx,cy,R) in enumerate([(0.3,0.45,0.17),(0.62,0.35,0.13),(0.7,0.68,0.11)]):
    pts=[]
    for i in range(10):
        a=-math.pi/2+i*math.pi/5; r=(R if i%2==0 else R*0.45)*W
        pts.append((cx*W+math.cos(a)*r, cy*W+math.sin(a)*r))
    d.polygon(pts, fill=YEL, outline=INK, width=4*S)
finish(im,(256,256)).save(os.path.join(OUT,'FX_DizzyStars.png'))

# poof cloud (reposition / landing puff)
rnd=random.Random(9); W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
for i in range(7):
    a=i/7*2*math.pi; r=W*0.18; cx=W/2+math.cos(a)*W*0.18; cy=W/2+math.sin(a)*W*0.14
    d.ellipse([cx-r,cy-r,cx+r,cy+r], fill=CREAM, outline=INK, width=4*S)
d.ellipse([W*0.32,W*0.32,W*0.68,W*0.68], fill=CREAM)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Poof.png'))

# zzz / tired
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
for k,(x,y,s) in enumerate([(0.18,0.62,0.22),(0.45,0.38,0.18),(0.68,0.16,0.14)]):
    s*=W; x*=W; y*=W
    d.line([(x,y),(x+s,y),(x,y+s),(x+s,y+s)], fill=INK, width=int(16*S), joint='curve')
    d.line([(x,y),(x+s,y),(x,y+s),(x+s,y+s)], fill=LAV, width=int(9*S), joint='curve')
finish(im,(256,256)).save(os.path.join(OUT,'FX_Tired.png'))
print('fx done')
# ---------------------------------------------------------------- M7 additions
WHITE=(255,252,240,255); GLUE=(250,246,232,255); PINK=(242,156,140,255); MINT=(150,214,180,255)

# camera flash burst (bright, 8 long rays + inner disc)
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im); c=W/2
for i in range(8):
    a=i*math.pi/4; L=W*(0.47 if i%2==0 else 0.33); w=W*0.06
    pts=[(c+math.cos(a+math.pi/2)*w, c+math.sin(a+math.pi/2)*w),(c+math.cos(a)*L, c+math.sin(a)*L),(c+math.cos(a-math.pi/2)*w, c+math.sin(a-math.pi/2)*w)]
    d.polygon(pts, fill=YEL, outline=INK, width=4*S)
d.ellipse([c-W*0.17,c-W*0.17,c+W*0.17,c+W*0.17], fill=WHITE, outline=INK, width=5*S)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Flash.png'))

# glue splat (ground decal, seen from above): blobby puddle + droplets + shine
rnd=random.Random(21); W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im); c=W/2
pts=[]
for i in range(64):
    a=i/64*2*math.pi; r=W*(0.35+0.035*math.sin(a*5+0.7)+0.02*math.sin(a*3))
    pts.append((c+math.cos(a)*r, c+math.sin(a)*r))
d.polygon(pts, fill=GLUE, outline=INK, width=5*S)
for k in range(6):
    a=rnd.uniform(0,2*math.pi); rr=W*rnd.uniform(0.4,0.46); s2=W*rnd.uniform(0.025,0.045)
    x,y=c+math.cos(a)*rr, c+math.sin(a)*rr
    d.ellipse([x-s2,y-s2,x+s2,y+s2], fill=GLUE, outline=INK, width=3*S)
d.ellipse([c-W*0.2,c-W*0.18,c-W*0.06,c-W*0.1], fill=(255,255,255,255))
finish(im,(256,256)).save(os.path.join(OUT,'FX_GlueSplat.png'))

# small dust puff (jump / land / run) — 3 soft balls, no heavy outline
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
for (x,y,r) in [(0.32,0.6,0.2),(0.55,0.5,0.24),(0.74,0.64,0.16)]:
    d.ellipse([(x-r)*W,(y-r)*W,(x+r)*W,(y+r)*W], fill=CREAM, outline=(120,108,130,255), width=3*S)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Dust.png'))

# heart (offer seat — "please, sit")
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
pts=[]
for i in range(60):
    t=i/60*2*math.pi
    x=16*math.sin(t)**3; y=13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t)
    pts.append((W/2+x*W*0.026, W*0.5-y*W*0.026))
d.polygon([(x+8*S,y+10*S) for x,y in pts], fill=(48,42,61,110))
d.polygon(pts, fill=PINK, outline=INK, width=5*S)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Heart.png'))

# confetti (victory) — paper bits in palette colours
rnd=random.Random(5); W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
cols=[APRICOT,SKY,LAV,YEL,PINK,MINT]
for k in range(16):
    x,y=rnd.uniform(0.12,0.88)*W, rnd.uniform(0.12,0.88)*W; a=rnd.uniform(0,math.pi); w,h=W*0.07,W*0.035
    pts=[(x+math.cos(a)*w-math.sin(a)*h, y+math.sin(a)*w+math.cos(a)*h),(x-math.cos(a)*w-math.sin(a)*h, y-math.sin(a)*w+math.cos(a)*h),
         (x-math.cos(a)*w+math.sin(a)*h, y-math.sin(a)*w-math.cos(a)*h),(x+math.cos(a)*w+math.sin(a)*h, y+math.sin(a)*w-math.cos(a)*h)]
    d.polygon(pts, fill=cols[k%len(cols)], outline=INK, width=2*S)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Confetti.png'))

# "OUCH!" pain burst (pinky toe kick)
im,d,W=burst(points=10, r1=0.48, r2=0.3, seed=11, fill=PINK)
try:
    f=ImageFont.truetype('DejaVuSans-Bold.ttf', 42*S)
except Exception:
    f=ImageFont.load_default()
tw=d.textlength('OUCH!', font=f); d.text((W/2-tw/2, W*0.4), 'OUCH!', font=f, fill=INK)
finish(im,(256,256)).save(os.path.join(OUT,'FX_Ouch.png'))
print('fx m7 done')


# decoy standee: "cardboard you" — apple drawn on cardboard, with a visible cut edge and a crooked smile
W=256*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im); c=W/2
CARD=(205,160,110,255); RED=(214,72,72,255)
d.ellipse([W*0.1,W*0.16,W*0.9,W*0.92], fill=CARD, outline=INK, width=5*S)            # cardboard cut-out edge
d.ellipse([W*0.16,W*0.22,W*0.84,W*0.86], fill=RED, outline=INK, width=4*S)            # painted apple
d.polygon([(W*0.5,W*0.24),(W*0.47,W*0.05),(W*0.53,W*0.05)], fill=(130,80,45,255), outline=INK)  # stem
d.ellipse([W*0.52,W*0.03,W*0.74,W*0.13], fill=MINT, outline=INK, width=3*S)            # leaf
for ex in (0.38,0.62):
    d.ellipse([W*(ex-0.07),W*0.42,W*(ex+0.07),W*0.58], fill=WHITE, outline=INK, width=3*S)
    d.ellipse([W*(ex-0.03),W*0.47,W*(ex+0.03),W*0.56], fill=INK)
d.arc([W*0.38,W*0.55,W*0.64,W*0.72], start=20, end=160, fill=INK, width=4*S)          # slightly crooked smile
finish(im,(256,256)).save(os.path.join(OUT,'FX_DecoyStandee.png'))
print('decoy done')
