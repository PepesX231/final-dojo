"""Procedural chalk art for the MAMA'S COMING! loading screen (original artwork).
Outputs: chalkboard.png (1920x1080), chalk_kid_sheet.png (4x4 frames, 512px each), chalk_dots.png"""
import math, random, sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
rng = np.random.default_rng(7)
random.seed(7)

def grain(w, h, lo=0.35, hi=1.0, scale=1):
    n = rng.random((h // scale + 1, w // scale + 1))
    img = Image.fromarray((n * 255).astype(np.uint8)).resize((w, h), Image.NEAREST if scale == 1 else Image.BILINEAR)
    a = np.asarray(img).astype(np.float32) / 255
    return lo + (hi - lo) * a

class Chalk:
    """Draws chalk strokes into a float RGBA canvas (supersampled x2)."""
    def __init__(self, w, h, ss=2):
        self.w, self.h, self.ss = w, h, ss
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
    def _layer(self):
        return Image.new("L", (self.w * self.ss, self.h * self.ss), 0)
    def _commit(self, mask, color, alpha, rough=0.55, blur=0.7):
        m = mask.resize((self.w, self.h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(blur))
        m = np.asarray(m).astype(np.float32) / 255
        g = grain(self.w, self.h, 0.0, 1.0)
        tex = np.clip((g - (1 - rough) * 0.6) / 0.6, 0, 1) * 0.55 + 0.45  # grainy, a few dry holes
        m = m * tex * alpha
        c = np.array(color, np.float32) / 255
        out_a = m + self.a * (1 - m)
        safe = np.maximum(out_a, 1e-5)
        self.rgb = (c * m[..., None] + self.rgb * (self.a * (1 - m))[..., None]) / safe[..., None]
        self.a = out_a
    def jit(self, pts, j):
        return [(x + random.uniform(-j, j), y + random.uniform(-j, j)) for x, y in pts]
    def line(self, pts, width=6, color=(240, 240, 230), alpha=1.0, jitter=1.2, passes=2):
        mask = self._layer(); d = ImageDraw.Draw(mask); s = self.ss
        for p in range(passes):
            q = self.jit(pts, jitter)
            d.line([(x * s, y * s) for x, y in q], fill=255 if p == 0 else 170, width=max(1, int(width * s * (1 if p == 0 else 0.6))), joint="curve")
            for x, y in (q[0], q[-1]):
                r = width * s * 0.5; d.ellipse((x * s - r, y * s - r, x * s + r, y * s + r), fill=255 if p == 0 else 170)
        self._commit(mask, color, alpha)
    def ellipse(self, cx, cy, rx, ry, width=6, color=(240, 240, 230), alpha=1.0, rot=0.0, n=48, start=0.0, end=1.0):
        pts = []
        for i in range(n + 1):
            t = (start + (end - start) * i / n) * 2 * math.pi
            x, y = math.cos(t) * rx, math.sin(t) * ry
            pts.append((cx + x * math.cos(rot) - y * math.sin(rot), cy + x * math.sin(rot) + y * math.cos(rot)))
        self.line(pts, width, color, alpha)
    def dot(self, x, y, r, color=(240, 240, 230), alpha=1.0):
        mask = self._layer(); d = ImageDraw.Draw(mask); s = self.ss
        d.ellipse(((x - r) * s, (y - r) * s, (x + r) * s, (y + r) * s), fill=255)
        self._commit(mask, color, alpha, rough=0.3)
    def hatch(self, poly_mask_fn, box, color, alpha=0.35, spacing=9, angle=-0.7, width=3):
        """Coloured-chalk shading: parallel strokes clipped to a shape mask."""
        x0, y0, x1, y1 = box
        mask = self._layer(); d = ImageDraw.Draw(mask); s = self.ss
        L = (x1 - x0) + (y1 - y0)
        ca, sa = math.cos(angle), math.sin(angle)
        for k in np.arange(-L, L, spacing):
            cx, cy = (x0 + x1) / 2 + k * -sa, (y0 + y1) / 2 + k * ca
            a = (cx - ca * L, cy - sa * L); b = (cx + ca * L, cy + sa * L)
            a = (a[0] + random.uniform(-2, 2), a[1] + random.uniform(-2, 2))
            d.line([(a[0] * s, a[1] * s), (b[0] * s, b[1] * s)], fill=random.randint(150, 255), width=int(width * s))
        clip = self._layer(); poly_mask_fn(ImageDraw.Draw(clip), s)
        mask = Image.fromarray(np.minimum(np.asarray(mask), np.asarray(clip)))
        self._commit(mask, color, alpha, rough=0.8, blur=0.6)
    def image(self):
        rgba = np.dstack([self.rgb * 255, self.a * 255]).clip(0, 255).astype(np.uint8)
        return Image.fromarray(rgba, "RGBA")

WHITE = (244, 242, 232); RED = (236, 110, 98); GREEN = (140, 205, 120); YELLOW = (246, 214, 110); BLUE = (130, 196, 230); PINK = (240, 150, 190)

# ---------------------------------------------------------------- chalkboard background
def board():
    W, H = 1920, 1080
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    base = np.array([34, 50, 43], np.float32)
    # big soft smudges (eraser clouds)
    lowf = Image.fromarray((rng.random((9, 16)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)
    lowf = np.asarray(lowf).astype(np.float32) / 255
    mid = Image.fromarray((rng.random((45, 80)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)
    mid = np.asarray(mid).astype(np.float32) / 255
    vign = 1 - 0.38 * (((xx / W - 0.5) * 1.6) ** 2 + ((yy / H - 0.5) * 1.9) ** 2)
    shade = (0.82 + 0.28 * lowf + 0.07 * mid) * vign
    img = base[None, None, :] * shade[..., None]
    img += (rng.random((H, W, 1)) - 0.5) * 7  # fine grain
    # eraser swipes: wide arcs of chalk dust
    dust = Image.new("L", (W, H), 0); d = ImageDraw.Draw(dust)
    for _ in range(9):
        cx, cy = rng.uniform(0, W), rng.uniform(0, H); r = rng.uniform(180, 420)
        a0 = rng.uniform(0, 360)
        for k in range(14):
            rr = r + k * 7
            d.arc((cx - rr, cy - rr * 0.6, cx + rr, cy + rr * 0.6), a0, a0 + rng.uniform(40, 110), fill=int(rng.uniform(10, 26)), width=9)
    dust = np.asarray(dust.filter(ImageFilter.GaussianBlur(6))).astype(np.float32) / 255
    img += dust[..., None] * np.array([210, 220, 210]) * 0.55
    bg = Image.fromarray(img.clip(0, 255).astype(np.uint8), "RGB").convert("RGBA")

    c = Chalk(W, H)
    A = 0.22  # faint doodles, the UI sits on top
    # mop (top-left)
    c.line([(120, 90), (250, 330)], 7, WHITE, A)
    for i in range(7): c.line([(240 + i * 4, 320), (225 + i * 9, 395 + (i % 2) * 8)], 5, WHITE, A)
    # bucket + bubbles (bottom-left is reserved for the animation -> mid-left)
    c.line([(70, 560), (88, 660), (178, 660), (196, 560)], 6, WHITE, A); c.ellipse(133, 560, 63, 14, 6, WHITE, A)
    for (x, y, r) in [(110, 520, 16), (150, 500, 11), (175, 470, 8), (128, 470, 6)]: c.ellipse(x, y, r, r, 4, BLUE, A + 0.05)
    # frying pan (top right)
    c.ellipse(1690, 140, 82, 70, 7, WHITE, A); c.line([(1765, 165), (1890, 210)], 9, WHITE, A)
    for k in range(3): c.line([(1660 + k * 22, 70 - k * 4), (1650 + k * 22, 40), (1668 + k * 22, 15)], 4, WHITE, A * 0.8)
    # wrench (right middle)
    c.line([(1760, 470), (1860, 600)], 10, WHITE, A); c.ellipse(1750, 455, 26, 26, 6, WHITE, A, start=0.15, end=0.85)
    # spray bottle (bottom right)
    c.line([(1720, 1010), (1720, 880), (1745, 850), (1800, 850), (1825, 880), (1825, 1010), (1720, 1010)], 6, WHITE, A)
    c.line([(1750, 850), (1755, 815), (1800, 815), (1820, 800)], 6, WHITE, A)
    for k in range(4): c.dot(1840 + k * 14, 795 - k * 5, 3, WHITE, A)
    # little house (top centre-left)
    c.line([(560, 140), (620, 85), (680, 140), (680, 200), (560, 200), (560, 140)], 6, WHITE, A)
    c.line([(605, 200), (605, 165), (630, 165), (630, 200)], 5, WHITE, A)
    # stars and swirls
    def star(x, y, r, col):
        pts = []
        for i in range(11):
            t = -math.pi / 2 + i * math.pi / 5; rr = r if i % 2 == 0 else r * 0.45
            pts.append((x + math.cos(t) * rr, y + math.sin(t) * rr))
        c.line(pts, 4, col, A + 0.06)
    for (x, y, r, col) in [(420, 70, 22, YELLOW), (1320, 60, 18, WHITE), (1500, 330, 16, YELLOW), (340, 980, 14, WHITE), (1550, 1000, 20, PINK), (980, 1030, 12, YELLOW)]: star(x, y, r, col)
    for (x, y) in [(1150, 110), (820, 1020), (1620, 690)]:
        pts = [(x + math.cos(t) * t * 4, y + math.sin(t) * t * 4) for t in np.linspace(0, 12, 60)]; c.line(pts, 4, WHITE, A * 0.8)
    # a few hearts / fruit faces
    c.ellipse(1460, 210, 30, 30, 5, RED, A + 0.05); c.line([(1460, 180), (1466, 166)], 4, GREEN, A + 0.05); c.dot(1450, 205, 3, WHITE, A + 0.1); c.dot(1470, 205, 3, WHITE, A + 0.1)
    c.ellipse(1460, 216, 10, 6, 3, WHITE, A, start=0.05, end=0.45)
    out = Image.alpha_composite(bg, c.image())
    out.convert("RGB").save(os.path.join(OUT, "chalkboard.png"), optimize=True)

# ---------------------------------------------------------------- animated chalk kid (mopping)
def kid_frame(f, n):
    S = 512
    c = Chalk(S, S)
    t = f / n * 2 * math.pi
    bob = -abs(math.sin(t)) * 10            # two bounces per stroke
    sq = 1 + math.sin(2 * t + 1) * 0.04     # squash & stretch
    cx, cy = 230, 250 + bob
    rx, ry = 92 / sq ** 0.5, 92 * sq
    # floor + puddle being mopped
    c.line([(40, 448), (470, 450)], 5, WHITE, 0.55, jitter=1.5)
    sweep = math.sin(t) * 70
    mx = 330 + sweep
    c.ellipse(345, 446, 120 - 20 * (f / n), 10, 4, BLUE, 0.5, n=36)
    # legs (walking in place) + sneakers
    for side, ph in ((-1, 0), (1, math.pi)):
        hx = cx + side * 32; hy = cy + ry - 10
        lift = max(0, math.sin(t + ph)) * 16
        fx = hx + math.sin(t + ph) * 14; fy = 440 - lift
        c.line([(hx, hy), ((hx + fx) / 2 + side * 3, (hy + fy) / 2), (fx, fy)], 7, WHITE, 0.95)
        c.ellipse(fx + 10, fy + 2, 24, 11, 5, PINK, 0.9)
    # body: coloured hatch + outline
    def body_mask(d, s):
        d.ellipse(((cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s), fill=255)
    c.hatch(body_mask, (cx - rx, cy - ry, cx + rx, cy + ry), RED, 0.75, spacing=9, angle=-0.6 + f * 0.0, width=3)
    c.ellipse(cx, cy, rx, ry, 7, WHITE, 1.0, n=60)
    c.ellipse(cx - 2, cy - ry + 4, 22, 9, 5, WHITE, 0.9, start=0.5, end=1.0)   # apple dimple
    # stem + leaf (lags behind the bounce)
    lag = math.sin(t - 0.6) * 6
    c.line([(cx, cy - ry + 2), (cx + 4 + lag * 0.3, cy - ry - 26)], 6, (180, 140, 90), 0.95)
    lx, ly = cx + 6 + lag * 0.5, cy - ry - 20
    c.ellipse(lx + 22, ly - 4, 24, 11, 5, GREEN, 0.95, rot=-0.4 + lag * 0.02)
    # face (looks toward the mop)
    ex = cx + 22
    blink = (f % n) in (n - 2,)
    for k in (-1, 1):
        if blink: c.line([(ex + k * 22 - 9, cy - 18), (ex + k * 22 + 9, cy - 18)], 5, WHITE, 1)
        else:
            c.ellipse(ex + k * 22, cy - 20, 12, 15, 4, WHITE, 1)
            c.dot(ex + k * 22 + 4, cy - 18, 5.5, WHITE, 1)
    c.ellipse(ex, cy + 14, 20, 12, 5, WHITE, 1, start=0.05, end=0.45)  # grin
    c.dot(ex - 40, cy + 8, 7, PINK, 0.6); c.dot(ex + 38, cy + 8, 7, PINK, 0.6)  # cheeks
    # mop: handle from hands to the floor; arms grip it
    bot = (mx + 30, 430); top = (max(cx + rx + 6, bot[0] - 110), 205 + bob * 0.5)
    c.line([top, bot], 8, YELLOW, 0.95)
    for k, u in ((0, 0.3), (1, 0.55)):
        hx = top[0] + (bot[0] - top[0]) * u; hy = top[1] + (bot[1] - top[1]) * u
        sx = cx + rx * (0.82 if k else 0.7); sy = cy + (24 if k else -6)
        c.line([(sx, sy), ((sx + hx) / 2, (sy + hy) / 2 + 12), (hx, hy)], 7, WHITE, 0.95)
        c.dot(hx, hy, 9, WHITE, 1)
    # mop strands swish behind the motion
    vel = math.cos(t)
    for i in range(9):
        a = (i - 4) * 0.13 - vel * 0.35
        c.line([(bot[0] + (i - 4) * 3, bot[1] - 4), (bot[0] + math.sin(a) * 34 + (i - 4) * 5, bot[1] + 18)], 4, WHITE, 0.85)
    # bubbles rising from the puddle (pop near the top)
    for b in range(5):
        ph = (f / n + b / 5) % 1.0
        bx = 300 + b * 38 + math.sin(ph * 9 + b) * 10; by = 440 - ph * 260; br = 6 + b % 3 * 4
        if ph < 0.88: c.ellipse(bx, by, br, br, 3, BLUE, 0.9 * (1 - ph * 0.5), n=24)
        else:
            for k in range(6):
                ang = k / 6 * 2 * math.pi
                c.line([(bx + math.cos(ang) * br, by + math.sin(ang) * br), (bx + math.cos(ang) * br * 1.8, by + math.sin(ang) * br * 1.8)], 3, BLUE, 0.8)
    # sparkle on the clean floor
    sp = (f * 2) % n
    if sp < 4:
        x, y, r = 120, 420, 8 + sp * 3
        c.line([(x - r, y), (x + r, y)], 3, YELLOW, 0.9); c.line([(x, y - r), (x, y + r)], 3, YELLOW, 0.9)
    return c.image()

def sheet():
    n = 16; cols = 4; S = 512
    out = Image.new("RGBA", (S * cols, S * (n // cols)), (0, 0, 0, 0))
    for f in range(n):
        random.seed(100 + f)  # chalk wobble changes each frame (boiling lines)
        out.paste(kid_frame(f, n), ((f % cols) * S, (f // cols) * S))
    out.save(os.path.join(OUT, "chalk_kid_sheet.png"), optimize=True)

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    (board() if 'bg' in sys.argv else None); sheet()
    print("ok")
