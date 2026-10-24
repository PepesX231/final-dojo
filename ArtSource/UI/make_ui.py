# Generates the original "craft paper & wood" UI sprite set for MAMA'S COMING! (no third-party art).
import sys, math, numpy as np
from PIL import Image, ImageDraw, ImageFilter
OUT = sys.argv[1]
rng = np.random.default_rng(7)
S = 2  # supersample

def noise(w, h, scale, seed):
    r = np.random.default_rng(seed)
    small = r.random((max(2, h // scale + 2), max(2, w // scale + 2)))
    im = Image.fromarray((small * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    return np.asarray(im).astype(np.float32) / 255.0

def fbm(w, h, seed, octaves=((64, .5), (24, .3), (8, .15), (3, .05))):
    a = np.zeros((h, w), np.float32)
    for i, (sc, amp) in enumerate(octaves):
        a += noise(w, h, sc, seed + i) * amp
    return a

def rough_mask(w, h, inset, radius, rough, seed, torn=False):
    """rounded-rect mask whose edge is displaced by noise (hand-cut paper)."""
    m = Image.new('L', (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([inset, inset, w - inset - 1, h - inset - 1], radius=radius, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(rough * 0.9))
    a = np.asarray(m).astype(np.float32) / 255.0
    n = fbm(w, h, seed, ((10, .6), (4, .4))) if torn else fbm(w, h, seed, ((30, .7), (10, .3)))
    a = a + (n - 0.5) * (0.9 if torn else 0.55)
    return np.clip((a - 0.5) * 6 + 0.5, 0, 1)

def outline_from_mask(a, px):
    m = Image.fromarray((a * 255).astype(np.uint8))
    grown = np.asarray(m.filter(ImageFilter.MaxFilter(px * 2 + 1))).astype(np.float32) / 255
    return grown

def to_img(rgb, a):
    rgb = np.clip(rgb, 0, 1); a = np.clip(a, 0, 1)
    arr = np.dstack([rgb * 255, a * 255]).astype(np.uint8)
    return Image.fromarray(arr, 'RGBA')

def compose(layers):
    """layers: list of (rgb HxWx3, alpha HxW) back-to-front."""
    h, w = layers[0][1].shape
    out_rgb = np.zeros((h, w, 3), np.float32); out_a = np.zeros((h, w), np.float32)
    for rgb, a in layers:
        if np.ndim(rgb) == 1: rgb = np.ones((h, w, 3), np.float32) * np.array(rgb, np.float32)
        na = a + out_a * (1 - a)
        out_rgb = (rgb * a[..., None] + out_rgb * out_a[..., None] * (1 - a[..., None])) / np.maximum(na[..., None], 1e-5)
        out_a = na
    return out_rgb, out_a

def hexc(s): s = s.lstrip('#'); return np.array([int(s[i:i+2], 16) / 255 for i in (0, 2, 4)], np.float32)

INK = hexc('3a2417')
SHADOW = hexc('1e120b')

def save(name, rgb, a, size):
    img = to_img(rgb, a).resize(size, Image.LANCZOS)
    img.save(f'{OUT}/{name}.png')

def shadow_of(a, dx, dy, blur, alpha):
    m = Image.fromarray((a * 255).astype(np.uint8))
    m = m.transform(m.size, Image.AFFINE, (1, 0, -dx, 0, 1, -dy)).filter(ImageFilter.GaussianBlur(blur))
    return np.asarray(m).astype(np.float32) / 255 * alpha

# ---------------------------------------------------------------- paper panel (sliced)
def paper(name, w, h, base='f1e4c6', edge='c9a676', seed=1, torn=False, tint=False):
    W, H = w * S, h * S
    body = rough_mask(W, H, 18 * S, 26 * S, 5 * S, seed, torn)
    ol = outline_from_mask(body, 3 * S)
    grain = fbm(W, H, seed + 10, ((40, .4), (12, .3), (3, .3)))
    fib = noise(W, H, 2, seed + 20)
    base_c = np.array([1, 1, 1], np.float32) if tint else hexc(base)
    edge_c = np.array([.82, .82, .82], np.float32) if tint else hexc(edge)
    # edge darkening (burnt rim) from distance to edge approximated by blurred mask
    bl = np.asarray(Image.fromarray((body * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(14 * S))).astype(np.float32) / 255
    rim = np.clip(1 - (bl - 0.5) * 2.2, 0, 1) ** 1.6
    shade = 0.93 + grain * 0.1 + (fib - 0.5) * 0.04
    rgb = base_c * shade[..., None]
    rgb = rgb * (1 - rim[..., None] * 0.55) + edge_c * rim[..., None] * 0.55
    sh = shadow_of(ol, 5 * S, 7 * S, 4 * S, 0.45)
    rgb, a = compose([(SHADOW, sh), (INK, ol), (rgb, body)])
    save(name, rgb, a, (w, h))

paper('ui_paper', 384, 384, seed=3)
paper('ui_paper_torn', 384, 384, seed=5, torn=True)
paper('ui_paint', 384, 384, seed=8, tint=True)          # white, tinted in Unity (buttons, tags)

# ---------------------------------------------------------------- wood plank (sliced horizontally)
def wood(name, w, h, seed=11, nails=True, col='9a6a43'):
    W, H = w * S, h * S
    body = rough_mask(W, H, 10 * S, 10 * S, 2 * S, seed)
    ol = outline_from_mask(body, 3 * S)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    warp = fbm(W, H, seed, ((80, 1.0), (20, .4))) * 18 * S
    grain = 0.5 + 0.5 * np.sin((yy + warp) / (5.5 * S))
    streak = noise(W, H, 3, seed + 3)
    c = hexc(col)
    shade = 0.80 + grain[..., None] * 0.14 + (streak[..., None] - .5) * 0.08
    rgb = c * shade
    # top bevel highlight / bottom shadow
    t = (yy - 10 * S) / (H - 20 * S)
    rgb = rgb * (1.12 - 0.28 * np.clip(t, 0, 1))[..., None]
    layers = [(SHADOW, shadow_of(ol, 4 * S, 7 * S, 4 * S, .5)), (INK, ol), (rgb, body)]
    if nails:
        for nx in (28 * S, W - 28 * S):
            d = np.sqrt((xx - nx) ** 2 + (yy - H / 2) ** 2)
            head = np.clip((7 * S - d), 0, 1)
            hl = np.clip(1 - np.sqrt((xx - nx + 2 * S) ** 2 + (yy - H / 2 + 2 * S) ** 2) / (4 * S), 0, 1)
            layers += [(INK, np.clip(9 * S - d, 0, 1)), (hexc('8d8d86') * (0.8 + 0.5 * hl[..., None]), head)]
    rgb, a = compose(layers)
    save(name, rgb, a, (w, h))

wood('ui_plank', 512, 128)
wood('ui_plank_dark', 512, 128, seed=13, col='5c3b26')

# ---------------------------------------------------------------- wood frame + sage metal plate (settings board, ref "MUSIC/SOUND")
def board(name, w, h, seed=21):
    W, H = w * S, h * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    outer = rough_mask(W, H, 8 * S, 18 * S, 2 * S, seed)
    ol = outline_from_mask(outer, 4 * S)
    warp = fbm(W, H, seed, ((80, 1.0), (20, .4))) * 20 * S
    grain = 0.5 + 0.5 * np.sin((yy + warp) / (6 * S))
    plank_lines = (np.abs(((yy / (H / 4.0)) % 1.0) - 0.0) < 0.012).astype(np.float32)
    wood_rgb = hexc('a06d43') * (0.8 + grain[..., None] * 0.16) * (1 - plank_lines[..., None] * 0.5)
    inset = 64 * S
    plate = rough_mask(W, H, inset, 14 * S, 1.5 * S, seed + 1)
    pol = outline_from_mask(plate, 3 * S)
    scratches = fbm(W, H, seed + 5, ((6, .5), (2, .5)))
    prgb = hexc('8fa58c') * (0.9 + scratches[..., None] * 0.14)
    # plate top highlight
    prgb = prgb * (1.08 - 0.16 * np.clip((yy - inset) / (H - 2 * inset), 0, 1))[..., None]
    layers = [(SHADOW, shadow_of(ol, 6 * S, 9 * S, 6 * S, .55)), (INK, ol), (wood_rgb, outer), (SHADOW, shadow_of(pol, 2 * S, 4 * S, 3 * S, .5) * outer), (INK, pol), (prgb, plate)]
    for (bx, by) in ((inset + 26 * S, inset + 26 * S), (W - inset - 26 * S, inset + 26 * S), (inset + 26 * S, H - inset - 26 * S), (W - inset - 26 * S, H - inset - 26 * S)):
        d = np.sqrt((xx - bx) ** 2 + (yy - by) ** 2)
        ang = np.arctan2(yy - by, xx - bx)
        hexr = 13 * S / np.cos(((ang + math.pi / 6) % (math.pi / 3)) - math.pi / 6)
        bolt = np.clip(hexr - d, 0, 1)
        bolto = np.clip(hexr + 3 * S - d, 0, 1)
        hl = np.clip(1 - np.sqrt((xx - bx + 4 * S) ** 2 + (yy - by + 4 * S) ** 2) / (12 * S), 0, 1)
        slot = ((np.abs((xx - bx) - (yy - by)) < 2.2 * S) & (d < 9 * S)).astype(np.float32)
        layers += [(INK, bolto), (hexc('a99bc0') * (0.85 + 0.35 * hl[..., None]), bolt), (INK, slot * bolt)]
    rgb, a = compose(layers)
    save(name, rgb, a, (w, h))

board('ui_board', 512, 512)

# ---------------------------------------------------------------- slider slot + knob (ref "MUSIC/SOUND")
def slot(name, w, h):
    W, H = w * S, h * S
    m = rough_mask(W, H, 6 * S, 16 * S, 1.2 * S, 31)
    ol = outline_from_mask(m, 3 * S)
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    rgb = hexc('3b2a22') * (0.6 + 0.6 * np.clip((yy - 6 * S) / (H - 12 * S), 0, 1))[..., None]
    hl = (np.abs(yy - (H - 10 * S)) < 1.5 * S).astype(np.float32) * m * 0.5
    rgb, a = compose([(INK, ol), (rgb, m), (hexc('c9b59a'), hl)])
    save(name, rgb, a, (w, h))

slot('ui_slot', 512, 64)

def knob(name, w, h, col='b8c8dc'):
    W, H = w * S, h * S
    m = rough_mask(W, H, 12 * S, 10 * S, 1.2 * S, 41)
    ol = outline_from_mask(m, 3 * S)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    g = np.clip(1.15 - (yy / H) * 0.4 - (xx / W) * 0.15, 0, 1.2)
    rgb = hexc(col) * g[..., None]
    bevel = ((yy < 22 * S) & (yy > 12 * S)).astype(np.float32) * m * 0.35
    rgb, a = compose([(SHADOW, shadow_of(ol, 3 * S, 5 * S, 3 * S, .5)), (INK, ol), (rgb, m), (np.array([1, 1, 1], np.float32), bevel)])
    save(name, rgb, a, (w, h))

knob('ui_knob', 96, 112)

# ---------------------------------------------------------------- tape strip
def tape(name, w, h, seed=51):
    W, H = w * S, h * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    jag = (noise(W, H, 3, seed) - .5) * 14 * S
    m = ((xx > 10 * S + jag) & (xx < W - 10 * S + jag) & (yy > 8 * S) & (yy < H - 8 * S)).astype(np.float32)
    m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8 * S))).astype(np.float32) / 255
    fib = fbm(W, H, seed + 2, ((20, .5), (4, .5)))
    rgb = hexc('efe2b0') * (0.92 + fib[..., None] * 0.12)
    rgb, a = compose([(SHADOW, shadow_of(m, 2 * S, 3 * S, 2 * S, .25)), (rgb, m * 0.82)])
    save(name, rgb, a, (w, h))

tape('ui_tape', 200, 64)

# ---------------------------------------------------------------- hex badge (tintable, ref 3 icons)
def hexagon(name, size, seed=61):
    W = H = size * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    cx, cy = W / 2 - 3 * S, H / 2 - 4 * S
    ang = np.arctan2(yy - cy, xx - cx)
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    R = W * 0.40
    hexr = R * math.cos(math.pi / 6) / np.cos(((ang) % (math.pi / 3)) - math.pi / 6)
    wob = (noise(W, H, 20, seed) - .5) * 4 * S
    body = np.clip(hexr + wob - d, 0, 1)
    ol = np.clip(hexr + wob + 7 * S - d, 0, 1)
    inner = np.clip(hexr * 0.8 - d, 0, 1)
    shade = 1.0 - np.clip((yy - cy) / R, -1, 1) * 0.08
    rgb = np.ones((H, W, 3), np.float32) * shade[..., None]
    rgb = rgb * (1 - (body - inner)[..., None] * 0.12)
    rgb, a = compose([(SHADOW, shadow_of(ol, 5 * S, 7 * S, 2 * S, .7)), (INK, ol), (rgb, body)])
    save(name, rgb, a, (size, size))

hexagon('ui_hex', 256)

# ---------------------------------------------------------------- comic burst (tintable, ref 3 speech bursts)
def burst(name, w, h, spikes=14, seed=71):
    W, H = w * S, h * S
    img = Image.new('L', (W, H), 0); d = ImageDraw.Draw(img)
    cx, cy = W / 2, H / 2; r = np.random.default_rng(seed); pts = []
    for i in range(spikes * 2):
        t = i / (spikes * 2) * 2 * math.pi
        k = 1.0 if i % 2 == 0 else 0.78 + r.random() * 0.06
        pts.append((cx + math.cos(t) * (W * 0.47) * k, cy + math.sin(t) * (H * 0.45) * k))
    d.polygon(pts, fill=255)
    body = np.asarray(img).astype(np.float32) / 255
    ol = outline_from_mask(body, 6 * S)
    rgb = np.ones((H, W, 3), np.float32)
    rgb, a = compose([(SHADOW, shadow_of(ol, 7 * S, 9 * S, 2 * S, .7)), (INK, ol), (rgb, body)])
    save(name, rgb, a, (w, h))

burst('ui_burst', 512, 300)

# ---------------------------------------------------------------- speech bubble (tintable), tail bottom-left
def bubble(name, w, h):
    W, H = w * S, h * S
    img = Image.new('L', (W, H), 0); d = ImageDraw.Draw(img)
    d.rounded_rectangle([14 * S, 12 * S, W - 18 * S, H - 46 * S], radius=40 * S, fill=255)
    d.polygon([(60 * S, H - 60 * S), (110 * S, H - 60 * S), (40 * S, H - 10 * S)], fill=255)
    body = np.asarray(img.filter(ImageFilter.GaussianBlur(1 * S))).astype(np.float32) / 255
    body = np.clip((body - .5) * 4 + .5, 0, 1)
    ol = outline_from_mask(body, 5 * S)
    rgb, a = compose([(SHADOW, shadow_of(ol, 5 * S, 7 * S, 2 * S, .6)), (INK, ol), (np.ones((H, W, 3), np.float32), body)])
    save(name, rgb, a, (w, h))

bubble('ui_bubble', 512, 256)

# ---------------------------------------------------------------- brush underline (tintable) for the selected menu item
def stroke(name, w, h, seed=81):
    W, H = w * S, h * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    t = xx / W
    thick = (H * 0.32) * (np.sin(np.clip(t, 0, 1) * math.pi) ** 0.35)
    wob = (noise(W, H, 40, seed) - .5) * 8 * S
    m = np.clip(thick - np.abs(yy - H / 2 - wob), 0, 1)
    dry = noise(W, H, 2, seed + 1)
    m = m * np.clip(0.75 + dry * 0.6, 0, 1)
    m = m * np.clip(np.minimum(t, 1 - t) * 18, 0, 1)
    rgb = np.ones((H, W, 3), np.float32)
    save(name, rgb, m, (w, h))

stroke('ui_stroke', 512, 64)

# ---------------------------------------------------------------- round coin / stamp (tintable) for timer + small icons
def coin(name, size, seed=91):
    W = H = size * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    cx, cy = W / 2 - 3 * S, H / 2 - 4 * S
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    wob = (noise(W, H, 16, seed) - .5) * 5 * S
    R = W * 0.41
    body = np.clip(R + wob - d, 0, 1)
    ol = np.clip(R + wob + 7 * S - d, 0, 1)
    ring = np.clip(np.abs(d - R * 0.82) < 2.5 * S, 0, 1).astype(np.float32) * 0.18
    rgb = np.ones((H, W, 3), np.float32) * (1 - ring[..., None])
    rgb, a = compose([(SHADOW, shadow_of(ol, 5 * S, 7 * S, 2 * S, .7)), (INK, ol), (rgb, body)])
    save(name, rgb, a, (size, size))

coin('ui_coin', 256)
print('done')

# ---------------------------------------------------------------- gear icon (tintable) for the settings button
def gear(name, size, teeth=8):
    W = H = size * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    cx, cy = W / 2 - 2 * S, H / 2 - 3 * S
    ang = np.arctan2(yy - cy, xx - cx); d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    R0, R1 = W * 0.30, W * 0.40
    tooth = (np.cos(ang * teeth) > 0.25).astype(np.float32)
    rad = R0 + (R1 - R0) * tooth
    body = np.clip(rad - d, 0, 1) * np.clip(d - W * 0.12, 0, 1)
    ol = np.clip(rad + 6 * S - d, 0, 1) * np.clip(d - W * 0.12 + 6 * S, 0, 1)
    rgb = np.ones((H, W, 3), np.float32) * (1 - 0.1 * np.clip((yy - cy) / R1, -1, 1))[..., None]
    rgb, a = compose([(SHADOW, shadow_of(ol, 4 * S, 6 * S, 2 * S, .7)), (INK, ol), (rgb, body)])
    save(name, rgb, a, (size, size))

gear('ui_gear', 128)
print('gear done')
