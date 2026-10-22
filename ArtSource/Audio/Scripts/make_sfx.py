"""
DREAM RUSH — M15 sound-effect set, synthesised from scratch (numpy; CC0, ours). Soft, toy-like, readable:
slashes are airy "fwip"s (no harsh metal), hits are cartoon "thwap/pop"s, coins chime, UI ticks are quiet.
run: python3 -I make_sfx.py <out_dir>
"""
import os, sys, wave
import numpy as np

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
SR = 44100
rng = np.random.default_rng(1515)


def t_(d): return np.arange(int(SR * d)) / SR


def bp(x, lo, hi):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    X[(f < lo) | (f > hi)] = 0
    return np.fft.irfft(X, len(x))


def sweep_noise(d, f0, f1, q=0.6):
    """noise through a moving band (frame-wise FFT filter)."""
    n = int(SR * d); x = rng.normal(0, 1, n); y = np.zeros(n); hop = 256; win = np.hanning(1024)
    for i in range(0, n - 1024, hop):
        k = i / max(1, n - 1024); fc = f0 * (f1 / f0) ** k
        seg = bp(x[i:i + 1024] * win, fc * (1 - q), fc * (1 + q))
        y[i:i + 1024] += seg
    return y / (np.abs(y).max() + 1e-9)


def env(n, a, d, shape=1.0):
    t = np.arange(n) / SR
    return (1 - np.exp(-t / max(a, 1e-4))) * np.exp(-t / d) ** shape


def tone(f, d, decay, kind='sine', glide=None):
    t = t_(d)
    fr = f if glide is None else f * (glide / f) ** (t / d)
    ph = 2 * np.pi * np.cumsum(np.full_like(t, 1.0) * fr) / SR
    w = np.sin(ph) if kind == 'sine' else (np.sign(np.sin(ph)) * 0.6 + np.sin(ph) * 0.4 if kind == 'square' else 2 * (ph / (2 * np.pi) % 1) - 1)
    return w * np.exp(-t / decay)


def mix(*xs):
    n = max(len(x) for x in xs); y = np.zeros(n)
    for x in xs: y[:len(x)] += x
    return y


def at(x, sec, total):
    y = np.zeros(int(SR * total)); s = int(SR * sec); y[s:s + len(x)] += x[:len(y) - s]; return y


def write(name, y, peak=0.6):
    y = y - np.mean(y)
    fade = min(len(y) // 4, int(SR * 0.012)); y[-fade:] *= np.linspace(1, 0, fade)
    y = y / (np.abs(y).max() + 1e-9) * peak
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype(np.int16).tobytes())


os.makedirs(OUT, exist_ok=True)

# ---- light slashes: airy leaf "fwip" (3 variants)
for i in range(3):
    d = 0.24; n = int(SR * d)
    air = sweep_noise(d, 700 + i * 120, 2600 + i * 300, 0.5) * env(n, 0.03, 0.07)
    shing = tone(4200 + i * 350, d, 0.05) * 0.12 * env(n, 0.02, 0.1)
    write(f'SFX_Swing_{i}', mix(air, shing), 0.5)
# ---- heavy slash: bigger, lower, with a body
for i in range(2):
    d = 0.38; n = int(SR * d)
    air = sweep_noise(d, 380 + i * 60, 1700, 0.6) * env(n, 0.05, 0.12)
    body = tone(160, d, 0.12, glide=90) * 0.35 * env(n, 0.04, 0.2)
    write(f'SFX_SwingHeavy_{i}', mix(air, body), 0.6)
# ---- dash whoosh
for i in range(2):
    d = 0.45; n = int(SR * d)
    w = sweep_noise(d, 300 + 80 * i, 1800 + 300 * i, 0.7) * np.sin(np.linspace(0, np.pi, n)) ** 1.5
    write(f'SFX_Whoosh_{i}', w, 0.45)
# ---- hits: cartoon thwap (click + pitch-drop thump + pop)
for i in range(3):
    d = 0.2; n = int(SR * d)
    click = bp(rng.normal(0, 1, n), 1500, 6000) * env(n, 0.0005, 0.008)
    thump = tone(210 + 25 * i, d, 0.06, glide=70) * 0.9
    pop = tone(900 + 140 * i, d, 0.018, glide=500) * 0.35
    write(f'SFX_Hit_{i}', mix(click * 0.7, thump, pop), 0.62)
for i in range(2):
    d = 0.42; n = int(SR * d)
    click = bp(rng.normal(0, 1, n), 1200, 7000) * env(n, 0.0005, 0.015)
    thump = tone(150 - 15 * i, d, 0.12, glide=42) * 1.0
    crunch = np.tanh(bp(rng.normal(0, 1, n), 200, 2500) * 3) * env(n, 0.001, 0.05) * 0.5
    sub = tone(55, d, 0.18) * 0.5
    write(f'SFX_HitHeavy_{i}', mix(click * 0.8, thump, crunch, sub), 0.75)
# ---- clank (vending machine / armour): inharmonic metal, short
for i in range(2):
    d = 0.5
    parts = [(523, 0.18, 1.0), (1371, 0.12, 0.6), (2213, 0.08, 0.45), (3170, 0.05, 0.3), (4410, 0.03, 0.2)]
    m = mix(*[tone(f * (1 + 0.04 * i), d, dec) * a for f, dec, a in parts])
    click = bp(rng.normal(0, 1, int(SR * d)), 2000, 8000) * env(int(SR * d), 0.0005, 0.006)
    write(f'SFX_Clank_{i}', mix(m, click * 0.6), 0.55)
# ---- dream burst: rising shimmer → boom → sparkle tail
d = 1.6; n = int(SR * d)
sh = mix(*[tone(f, 0.45, 0.6, glide=f * 2) * np.linspace(0, 1, int(SR * 0.45)) ** 2 * 0.25 for f in (523, 659, 784, 1175)])
swell = sweep_noise(0.45, 400, 5000, 0.5) * np.linspace(0, 1, int(SR * 0.45)) ** 2 * 0.4
boom = mix(tone(110, 1.1, 0.35, glide=38) * 1.1, bp(rng.normal(0, 1, int(SR * 1.1)), 80, 1800) * env(int(SR * 1.1), 0.002, 0.18) * 0.5)
sparkle = mix(*[at(tone(f, 0.5, 0.18), 0.05 * k, 1.1) * 0.18 for k, f in enumerate((2093, 2637, 3136, 2349, 2794, 3520, 4186))])
write('SFX_Burst', mix(at(mix(sh, swell), 0, d), at(boom, 0.42, d), at(sparkle, 0.5, d)), 0.85)
# ---- coin (lost minute): two-tone chime, 2 pitches
for i, (a, b) in enumerate(((1319, 1976), (1480, 2217))):
    d = 0.4
    c = mix(tone(a, 0.07, 0.05, 'square') * 0.5, at(mix(tone(b, 0.33, 0.12, 'square') * 0.45, tone(b * 2, 0.33, 0.06) * 0.2), 0.065, d))
    write(f'SFX_Coin_{i}', c, 0.38)
# ---- door: soft wooden creak + latch + a little dream chime
d = 0.9; n = int(SR * d)
f = 95 + 25 * np.sin(np.linspace(0, 7, n)) + rng.normal(0, 4, n).cumsum() * 0.02
creak = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * env(n, 0.05, 0.25) * 0.25
creak = bp(creak, 200, 2400)
latch = at(bp(rng.normal(0, 1, int(SR * 0.05)), 1500, 6000) * env(int(SR * 0.05), 0.0005, 0.006), 0.02, d) * 0.8
chime = at(mix(tone(1568, 0.6, 0.25) * 0.3, at(tone(2093, 0.55, 0.22) * 0.25, 0.09, 0.6)), 0.25, d)
write('SFX_Door', mix(creak, latch, chime), 0.5)
# ---- grind loop (1 s, seamless): bright metal hiss with sparkly ripples
d = 1.0; n = int(SR * d)
g = bp(rng.normal(0, 1, n * 2), 2500, 7000)[:n] * (0.7 + 0.3 * np.sin(np.linspace(0, 2 * np.pi * 12, n)))
g += bp(rng.normal(0, 1, n), 400, 1200) * 0.25
cf = int(SR * 0.05); g[:cf] = g[:cf] * np.linspace(0, 1, cf) + g[-cf:] * np.linspace(1, 0, cf)
write('SFX_Grind', g[:n - cf], 0.3)
# ---- reflect: "ting!" + upward whoosh
d = 0.5; n = int(SR * d)
write('SFX_Reflect', mix(tone(2637, d, 0.12) * 0.8, tone(3951, d, 0.08) * 0.5, sweep_noise(d, 800, 5000, 0.4) * env(n, 0.01, 0.07) * 0.5), 0.55)
# ---- enemy alert "!": two rising blips
d = 0.3
write('SFX_EnemyAlert', mix(tone(620, 0.08, 0.05, 'square', glide=760) * 0.6, at(tone(880, 0.12, 0.07, 'square', glide=1100) * 0.6, 0.09, d)), 0.42)
# ---- shock / gong: low boom + gong partials
d = 1.4; n = int(SR * d)
gong = mix(*[tone(f, d, dec) * a for f, dec, a in ((98, 0.9, 1.0), (196.5, 0.7, 0.5), (271, 0.6, 0.45), (409, 0.45, 0.35), (563, 0.3, 0.25), (760, 0.2, 0.15))])
write('SFX_Shock', mix(gong * (1 + 0.15 * np.sin(np.linspace(0, 2 * np.pi * 9, n))), tone(60, d, 0.25, glide=35) * 0.6), 0.7)
# ---- UI hover: tiny soft tick · UI back: descending two-tone
write('SFX_UiHover', mix(tone(2400, 0.03, 0.006) * 0.6, bp(rng.normal(0, 1, int(SR * 0.03)), 3000, 8000) * env(int(SR * 0.03), 0.0003, 0.003) * 0.4), 0.18)
write('SFX_UiBack', mix(tone(880, 0.07, 0.04) * 0.6, at(tone(587, 0.12, 0.06) * 0.6, 0.06, 0.18)), 0.3)
# ---- pickup: quick sparkle arpeggio
d = 0.45
write('SFX_Pickup', mix(*[at(tone(f, 0.25, 0.08) * 0.5 + tone(f * 2, 0.25, 0.04) * 0.15, 0.045 * k, d) for k, f in enumerate((1047, 1319, 1568, 2093))]), 0.45)
print('sfx ok')
