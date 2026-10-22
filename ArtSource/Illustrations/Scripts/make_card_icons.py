"""Card illustrations (GDD §17.4) — blockout pictograms in Applebook paper style. Replace with painted art later."""
import math, os, sys
from PIL import Image, ImageDraw
OUT=sys.argv[1]; os.makedirs(OUT, exist_ok=True)
S=4; W=256*S
INK=(48,42,61,255); CREAM=(245,231,211,255); RED=(232,93,101,255); GREEN=(120,183,139,255); LAV=(170,160,221,255)
APR=(255,184,112,255); SKY=(143,213,237,255); YEL=(255,214,140,255); WHITE=(252,250,246,255); GRAY=(180,176,186,255); BROWN=(150,98,64,255)
LW=6*S
def new(): 
    im=Image.new('RGBA',(W,W),(0,0,0,0)); return im, ImageDraw.Draw(im)
def save(im,name): im.resize((256,256),Image.LANCZOS).save(os.path.join(OUT,name+'.png'))
def shadow(d, shape, *a, **k):
    pass
def apple(d,cx,cy,r,col=RED):
    d.ellipse([cx-r,cy-r*0.9,cx+r,cy+r*0.95],fill=col,outline=INK,width=LW)
    d.line([(cx,cy-r*0.85),(cx+r*0.12,cy-r*1.25)],fill=BROWN,width=LW+2*S)
    d.ellipse([cx+r*0.1,cy-r*1.35,cx+r*0.55,cy-r*1.1],fill=GREEN,outline=INK,width=LW//2)

# SUPER GLUE: tube + blob
im,d=new()
d.rounded_rectangle([W*.22,W*.18,W*.62,W*.62],radius=30*S,fill=WHITE,outline=INK,width=LW)
d.rectangle([W*.22,W*.3,W*.62,W*.42],fill=APR)
d.polygon([(W*.36,W*.62),(W*.48,W*.62),(W*.45,W*.74),(W*.39,W*.74)],fill=GRAY,outline=INK)
d.ellipse([W*.3,W*.74,W*.82,W*.9],fill=(240,236,200,255),outline=INK,width=LW)
d.ellipse([W*.58,W*.6,W*.7,W*.74],fill=(240,236,200,255),outline=INK,width=LW//2)
save(im,'CARD_SuperGlue')
# CAMERA FLASH
im,d=new()
for i in range(8):
    a=i*math.pi/4; d.line([(W*.5+math.cos(a)*W*.36,W*.5+math.sin(a)*W*.36),(W*.5+math.cos(a)*W*.47,W*.5+math.sin(a)*W*.47)],fill=YEL,width=LW*2)
d.rounded_rectangle([W*.2,W*.32,W*.8,W*.72],radius=24*S,fill=SKY,outline=INK,width=LW)
d.rectangle([W*.3,W*.24,W*.46,W*.32],fill=GRAY,outline=INK,width=LW//2)
d.ellipse([W*.37,W*.38,W*.63,W*.64],fill=WHITE,outline=INK,width=LW)
d.ellipse([W*.44,W*.45,W*.56,W*.57],fill=INK)
d.rectangle([W*.66,W*.37,W*.75,W*.44],fill=YEL,outline=INK,width=LW//2)
save(im,'CARD_CameraFlash')
# FISHING ROD
im,d=new()
d.line([(W*.15,W*.85),(W*.7,W*.15)],fill=BROWN,width=LW*2)
d.ellipse([W*.2,W*.62,W*.34,W*.76],fill=GRAY,outline=INK,width=LW//2)
d.line([(W*.7,W*.15),(W*.78,W*.62)],fill=INK,width=S*3)
d.arc([W*.7,W*.58,W*.86,W*.74],start=0,end=200,fill=INK,width=LW)
apple(d,W*.6,W*.82,W*.09,col=LAV)
save(im,'CARD_FishingRod')
# BE A GRANDPA: apple with glasses + cane + beard
im,d=new()
apple(d,W*.45,W*.52,W*.26,col=(214,120,120,255))
for cx in (W*.36,W*.54):
    d.ellipse([cx-W*.07,W*.42,cx+W*.07,W*.54],fill=(255,255,255,140),outline=INK,width=LW)
d.line([(W*.43,W*.48),(W*.47,W*.48)],fill=INK,width=LW)
d.chord([W*.3,W*.56,W*.6,W*.82],start=0,end=180,fill=WHITE,outline=INK,width=LW//2)
d.line([(W*.78,W*.35),(W*.78,W*.9)],fill=BROWN,width=LW*2)
d.arc([W*.66,W*.25,W*.82,W*.45],start=180,end=360,fill=BROWN,width=LW*2)
save(im,'CARD_BeAGrandpa')
# DECOY APPLE: dashed outline apple + question
im,d=new()
apple(d,W*.42,W*.55,W*.24,col=(240,200,200,255))
for i in range(0,360,30):
    a=math.radians(i); r=W*.33
    d.arc([W*.42-r,W*.55-r,W*.42+r,W*.55+r],start=i,end=i+14,fill=LAV,width=LW)
d.text((W*.68,W*.15),"?",fill=INK)
d.ellipse([W*.66,W*.12,W*.86,W*.32],fill=YEL,outline=INK,width=LW//2)
d.line([(W*.76,W*.17),(W*.76,W*.24)],fill=INK,width=LW); d.ellipse([W*.745,W*.26,W*.775,W*.29],fill=INK)
save(im,'CARD_DecoyApple')
# BOUNCY CUSHION
im,d=new()
d.rounded_rectangle([W*.15,W*.55,W*.85,W*.8],radius=40*S,fill=SKY,outline=INK,width=LW)
d.line([(W*.5,W*.58),(W*.5,W*.77)],fill=INK,width=LW//2)
for k,x in enumerate((W*.3,W*.5,W*.7)):
    d.arc([x-W*.08,W*.2+k%2*W*.05,x+W*.08,W*.45],start=200,end=340,fill=APR,width=LW)
d.polygon([(W*.5,W*.08),(W*.42,W*.22),(W*.58,W*.22)],fill=APR,outline=INK)
save(im,'CARD_BouncyCushion')
# DREAM LAWS (forbidden sign over icon)
def forbid(d):
    d.ellipse([W*.08,W*.08,W*.92,W*.92],outline=(214,70,80,255),width=LW*3)
    d.line([(W*.22,W*.78),(W*.78,W*.22)],fill=(214,70,80,255),width=LW*3)
im,d=new()  # NO JUMP: spring legs
for i in range(5): d.arc([W*.35,W*.3+i*W*.08,W*.65,W*.42+i*W*.08],start=180 if i%2 else 0,end=360 if i%2 else 180,fill=INK,width=LW)
apple(d,W*.5,W*.25,W*.12)
forbid(d); save(im,'LAW_NoJump')
im,d=new()  # NO SPRINT: shoe with speed lines
d.chord([W*.25,W*.4,W*.75,W*.75],start=180,end=360,fill=APR,outline=INK,width=LW)
d.rectangle([W*.25,W*.57,W*.8,W*.66],fill=CREAM,outline=INK,width=LW//2)
for y in (W*.45,W*.52,W*.6): d.line([(W*.08,y),(W*.2,y)],fill=INK,width=LW)
forbid(d); save(im,'LAW_NoSprint')
im,d=new()  # SLIPPERY FLOOR: wavy lines + sliding apple
for k in range(3):
    pts=[(W*.12+i*W*.04, W*(.62+k*.09)+math.sin(i*0.9)*W*.02) for i in range(20)]
    d.line(pts,fill=SKY,width=LW)
apple(d,W*.45,W*.42,W*.13)
d.line([(W*.68,W*.36),(W*.84,W*.36)],fill=INK,width=LW); d.line([(W*.7,W*.44),(W*.86,W*.44)],fill=INK,width=LW)
save(im,'LAW_Slippery')
print('icons done')
