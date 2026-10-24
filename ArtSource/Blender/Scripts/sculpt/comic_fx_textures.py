"""Comic VFX sprites for MAMA'S COMING, drawn as signed-distance shapes in numpy.

Bright, kid-friendly comic style: saturated fills, halftone dot shading, chunky ink outlines.
Run:  blender -b -P ArtSource/Blender/Scripts/sculpt/comic_fx_textures.py
Writes Assets/MamasComing/Art/ComicFx/Textures/FX_*.png (white-ish sprites are tinted at runtime).
"""
import bpy, os, math
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../..'))
OUT = ROOT + '/Assets/MamasComing/Art/ComicFx/Textures'
INK = np.array((0.11, 0.09, 0.2))


def grid(n):
    y, x = np.mgrid[0:n, 0:n] + .5
    return (x / n) * 2 - 1, 1 - (y / n) * 2


def polar(x, y): return np.hypot(x, y), np.arctan2(y, x)


def cover(d, px):
    """Anti-aliased coverage of the inside (d<0) of a distance field, px = pixel size in field units."""
    return np.clip(.5 - d / px, 0, 1)


def halftone(x, y, density, cells=26, angle=.4):
    """Dots whose radius grows with density (0..1): classic print shading."""
    c, s = math.cos(angle), math.sin(angle)
    u, v = (x * c - y * s) * cells, (x * s + y * c) * cells
    fu, fv = u - np.floor(u) - .5, v - np.floor(v) - .5
    r = np.sqrt(np.clip(density, 0, 1)) * .62
    return np.hypot(fu, fv) - r          # <0 inside a dot (in cell units)


def save(name, rgba):
    os.makedirs(OUT, exist_ok=True)
    n = rgba.shape[0]
    img = bpy.data.images.new(name, n, n, alpha=True)
    img.colorspace_settings.name = 'sRGB'
    img.pixels.foreach_set(np.flipud(rgba).astype(np.float32).ravel())
    img.filepath_raw = f'{OUT}/{name}.png'; img.file_format = 'PNG'
    img.save()
    print('FX_TEX', name)


def layer(base, color, alpha):
    a = alpha[..., None]
    rgb = base[..., :3] * (1 - a) + np.asarray(color) * a
    out = np.empty_like(base); out[..., :3] = rgb; out[..., 3] = np.maximum(base[..., 3], alpha)
    return out


def blank(n): return np.zeros((n, n, 4))


def burst(n=512, spikes=13, seed=4):
    """The POW! starburst: jagged star, yellow core into orange, halftone rim, white inner flash, ink outline."""
    rng = np.random.default_rng(seed)
    x, y = grid(n); r, a = polar(x, y)
    lens = rng.uniform(.78, .98, spikes); inner = rng.uniform(.5, .6, spikes)
    k = (a + math.pi) / (2 * math.pi) * spikes
    i = np.floor(k).astype(int) % spikes; f = k - np.floor(k)
    # each spike rises from its valley to a tip at mid-sector, then falls into the next valley
    edge =np.where(f < .5, inner[i] + (lens[i] - inner[i]) * (f / .5), lens[i] + (inner[(i + 1) % spikes] - lens[i]) * ((f - .5) / .5))
    d = r - edge
    px = 2.0 / n
    img = blank(n)
    img = layer(img, INK, cover(d - .045, px))                       # thick ink outline
    fill = cover(d, px)
    t = np.clip(r / .9, 0, 1)[..., None]
    col = np.array((1.0, .93, .2)) * (1 - t) + np.array((1.0, .45, .12)) * t
    img[..., :3] = img[..., :3] * (1 - fill[..., None]) + col * fill[..., None]
    dots = cover(halftone(x, y, np.clip((r - .35) / .5, 0, 1)) , .05) * fill * (r > .3)
    img = layer(img, (1.0, .32, .18), dots)                          # red-orange print dots near the rim
    core = cover(r - .28 - .03 * np.sin(a * spikes), px)
    img = layer(img, (1.0, 1.0, .9), core)                           # white flash in the middle
    return img


def puff(n=256, lobes=7, seed=2):
    """Cartoon dust/steam puff: white bumpy cloud, ink outline, halftone shadow on its lower right."""
    rng = np.random.default_rng(seed)
    x, y = grid(n)
    d = np.full(x.shape, 9.0)
    for i in range(lobes):
        ang = i / lobes * 2 * math.pi + rng.uniform(-.2, .2)
        rr = rng.uniform(.28, .36)
        cx, cy = math.cos(ang) * .38, math.sin(ang) * .32
        d = np.minimum(d, np.hypot(x - cx, y - cy) - rr)
    d = np.minimum(d, np.hypot(x, y) - .45)
    px = 2.0 / n
    img = blank(n)
    img = layer(img, INK, cover(d - .06, px))
    fill = cover(d, px)
    img = layer(img, (1, 1, 1), fill)
    shade = np.clip((x - y) * .6 + .1, 0, 1)
    img = layer(img, (.78, .82, .95), cover(halftone(x, y, shade, 14), .06) * fill)
    return img


def star(n=128, points=5):
    x, y = grid(n); r, a = polar(x, y)
    k = (a + math.pi / 2) / (2 * math.pi) * points
    f = np.abs(k - np.round(k))
    edge = .38 + (.82 - .38) * (1 - f * 2) ** 1.6
    d = r - edge
    px = 2.0 / n
    img = blank(n)
    img = layer(img, INK, cover(d - .1, px))
    img = layer(img, (1, 1, 1), cover(d, px))
    img = layer(img, (1, 1, 1), cover(np.hypot(x + .15, y - .2) - .12, px))
    return img


def speedlines(n=512, count=40, seed=8):
    """Radial action lines with a clear hole in the middle (impact frames)."""
    rng = np.random.default_rng(seed)
    x, y = grid(n); r, a = polar(x, y)
    img = blank(n)
    px = 2.0 / n
    for i in range(count):
        ang = rng.uniform(-math.pi, math.pi); w = rng.uniform(.006, .02)
        r0 = rng.uniform(.45, .7)
        da = np.angle(np.exp(1j * (a - ang)))
        width = w * np.clip((r - r0) / .3, 0, 1)
        d = np.abs(da) * r - width
        img = layer(img, (1, 1, 1), cover(d, px) * (r > r0) * (r < .98))
    return img


def splash(n=256, seed=6):
    """Comic water splash: crown of drops, white highlight, ink outline. Tint blue at runtime."""
    rng = np.random.default_rng(seed)
    x, y = grid(n)
    d = np.hypot(x / 1.25, (y + .5) / .45) - .55                    # puddle
    for i in range(7):
        ang = math.radians(25 + i * 22)
        cx, cy = math.cos(ang) * .55, math.sin(ang) * .65 - .3
        rr = rng.uniform(.08, .13)
        drop = np.hypot(x - cx, (y - cy) * .8) - rr
        d = np.minimum(d, drop)
    px = 2.0 / n
    img = blank(n)
    img = layer(img, INK, cover(d - .05, px))
    img = layer(img, (1, 1, 1), cover(d, px))
    img = layer(img, (.75, .9, 1), cover(halftone(x, y, np.clip(-y * .8, 0, 1), 12), .06) * cover(d, px))
    return img


def ring(n=256):
    """Shockwave ring, ink-edged."""
    x, y = grid(n); r = np.hypot(x, y)
    px = 2.0 / n
    img = blank(n)
    d = np.abs(r - .8) - .07
    img = layer(img, INK, cover(d - .03, px))
    img = layer(img, (1, 1, 1), cover(d, px))
    return img


save('FX_ComicBurst', burst())
save('FX_ComicBurstB', burst(spikes=10, seed=9))
save('FX_InkPuff', puff())
save('FX_Star', star())
save('FX_SpeedLines', speedlines())
save('FX_Splash', splash())
save('FX_Ring', ring())
