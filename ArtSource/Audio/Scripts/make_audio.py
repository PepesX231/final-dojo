"""
DREAM RUSH — procedural placeholder audio (P0, M7).

Every sound here is synthesised from scratch by this script (sine/noise/filters) — no samples, no third-party
material — so the output is original work, released by the project as CC0. See docs/AUDIO_CREDITS.md.

Direction (GDD §5.1 Applebook tone, §20.7 audio): soft, toy-like, music-box and wood; nothing harsh or
realistic-violent; short and readable; the chair sounds *wooden* (clacks, creaks, whistly squeaks).

usage: python3 make_audio.py <out_dir>        → <out_dir>/SFX_*.wav, MUS_*.wav
"""
import os
import sys
import wave

import numpy as np
from scipy import signal

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out'
os.makedirs(OUT, exist_ok=True)
SR = 32000
rng = np.random.default_rng(11)


# ---------------------------------------------------------------- primitives
def t_(d):
    return np.arange(int(SR * d)) / SR


def env(n, a=0.005, r=None, curve=4.0):
    """attack + exponential release envelope over n samples."""
    e = np.ones(n)
    na = max(1, int(SR * a))
    e[:na] = np.linspace(0, 1, na)
    tail = np.linspace(0, 1, n - na)
    e[na:] = np.exp(-curve * tail) if r is None else np.exp(-tail * (n - na) / (SR * r))
    return e


def sine_sweep(f0, f1, d, shape='exp'):
    t = t_(d)
    if shape == 'exp':
        f = f0 * (f1 / f0) ** (t / d)
    else:
        f = f0 + (f1 - f0) * t / d
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def noise(d):
    return rng.uniform(-1, 1, int(SR * d))


def bp(x, lo, hi, order=2):
    b, a = signal.butter(order, [lo / (SR / 2), hi / (SR / 2)], 'band')
    return signal.lfilter(b, a, x)


def lp(x, f, order=2):
    b, a = signal.butter(order, f / (SR / 2), 'low')
    return signal.lfilter(b, a, x)


def hp(x, f, order=2):
    b, a = signal.butter(order, f / (SR / 2), 'high')
    return signal.lfilter(b, a, x)


def bell(f, d, partials=((1, 1), (2.0, 0.35), (3.01, 0.15), (4.2, 0.06)), decay=3.5):
    t = t_(d)
    y = sum(a * np.sin(2 * np.pi * f * k * t) * np.exp(-t * decay * (1 + k * 0.6)) for k, a in partials)
    return y * env(len(t), a=0.002, curve=0.0001)


def wood(f, d=0.12, bright=1.0):
    """woody knock: damped resonances + click."""
    t = t_(d)
    y = (np.sin(2 * np.pi * f * t) * np.exp(-t * 55) + 0.5 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 90)
         + 0.25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t * 140) * bright)
    click = bp(noise(d), 1500, 6000) * np.exp(-t * 400) * 0.4 * bright
    return y + click


def pad(x, d):
    n = int(SR * d)
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n] if len(x) < n else x


def mix(*parts):
    n = max(len(p) for p in parts)
    y = np.zeros(n)
    for p in parts:
        y[:len(p)] += p
    return y


def at(x, start, total):
    y = np.zeros(int(SR * total))
    s = int(SR * start)
    e = min(len(y), s + len(x))
    y[s:e] += x[:e - s]
    return y


def norm(x, peak=0.8):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def fade(x, fi=0.003, fo=0.01):
    x = x.copy()
    a, b = int(SR * fi), int(SR * fo)
    if a: x[:a] *= np.linspace(0, 1, a)
    if b: x[-b:] *= np.linspace(1, 0, b)
    return x


def save(name, x, peak=0.8, loop=False):
    x = norm(x, peak) if loop else fade(norm(x, peak))  # loops are wrapped seamlessly; no fades
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


# ---------------------------------------------------------------- player
for i in range(3):  # soft padded footsteps on a carpet/paper floor
    d = 0.09
    t = t_(d)
    y = lp(noise(d), 900 + i * 150) * np.exp(-t * 60) + 0.6 * np.sin(2 * np.pi * (95 + i * 12) * t) * np.exp(-t * 45)
    save(f'SFX_Footstep_{i}', y, 0.6)

y = sine_sweep(320, 720, 0.16) * env(int(SR * 0.16), a=0.004, curve=3) + 0.3 * sine_sweep(640, 1440, 0.16) * env(int(SR * 0.16), curve=6)
save('SFX_Jump', y, 0.55)

d = 0.22; t = t_(d)
y = np.sin(2 * np.pi * np.cumsum(140 * np.exp(-t * 6)) / SR) * np.exp(-t * 18) + 0.5 * lp(noise(d), 600) * np.exp(-t * 30)
save('SFX_Land', y, 0.7)

d = 0.35; t = t_(d)  # bwomp: lose a sleep moon
y = sine_sweep(520, 160, d) * env(len(t), a=0.01, curve=3.5) + 0.3 * sine_sweep(780, 240, d) * env(len(t), curve=5)
save('SFX_Hurt', y, 0.6)

# pinky-toe kick: wood bonk + springy wobble
y = mix(wood(260, 0.25), at(sine_sweep(900, 300, 0.4) * env(int(SR * 0.4), curve=4) * (1 + 0.3 * np.sin(2 * np.pi * 18 * t_(0.4))), 0.03, 0.45))
save('SFX_Kick', y, 0.8)

# ---------------------------------------------------------------- the chair (wooden, whistly, nervous)
for i in range(3):
    save(f'SFX_ChairStep_{i}', wood(520 + i * 60, 0.09, 0.8), 0.5)

d = 0.32; t = t_(d)  # gasp: quick rising whistle
y = sine_sweep(700, 1700, d * 0.6) ; y = pad(y, d) * env(len(t), a=0.01, curve=5)
y += 0.15 * bp(noise(d), 2000, 5000) * np.exp(-t * 12)
save('SFX_ChairGasp', y, 0.5)

d = 0.4; t = t_(d)  # yelp: two-step squeak with vibrato
f = np.where(t < 0.15, 1100, 1450) * (1 + 0.04 * np.sin(2 * np.pi * 22 * t))
y = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(t), a=0.008, curve=3)
save('SFX_ChairYelp', y, 0.45)

y = mix(*[at(wood(380 + 70 * k, 0.12, 1.0), 0.07 * k + 0.01 * (k % 2), 0.6) for k in range(5)])  # clattering legs
save('SFX_ChairStumble', y, 0.75)

d = 1.1; t = t_(d)  # tired: wobbly descending whistle sigh
f = 900 * (0.45 ** (t / d)) * (1 + 0.05 * np.sin(2 * np.pi * 6 * t))
y = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(t), a=0.05, curve=2.5) + 0.1 * lp(noise(d), 1200) * env(len(t), a=0.2, curve=2)
save('SFX_ChairTired', y, 0.4)

d = 0.7; t = t_(d)  # creak: slow FM sawtooth through a wood resonance
f = 85 + 40 * np.sin(2 * np.pi * 1.3 * t) + 20 * rng.standard_normal(len(t)).cumsum() / SR
saw = signal.sawtooth(2 * np.pi * np.cumsum(f) / SR)
y = bp(saw, 400, 2400) * env(len(t), a=0.08, curve=2) * (0.6 + 0.4 * np.abs(np.sin(2 * np.pi * 9 * t)))
save('SFX_ChairCreak', y, 0.55)

# ---------------------------------------------------------------- UI / flow
save('SFX_UI_Click', mix(wood(1600, 0.04, 0.5) * 0.6), 0.35)
d = 0.25; t = t_(d)
save('SFX_UI_Open', bp(noise(d), 1500, 7000) * env(len(t), a=0.03, curve=4) * (0.5 + 0.5 * np.sin(2 * np.pi * 30 * t)), 0.35)  # paper rustle
y = mix(*[at(bell(midi(n), 1.6, decay=2.0), 0.28 * k, 3.0) for k, n in enumerate([79, 76, 72, 67])])  # falling asleep
save('SFX_SleepChime', y, 0.45)
y = mix(at(bell(midi(76), 1.2, decay=2.5), 0, 1.6), at(bell(midi(72), 1.2, decay=2.5), 0.35, 1.6))  # morning ding-dong
save('SFX_WakeUp', y, 0.5)
y = mix(*[at(bell(midi(n), 1.4, decay=2.2), 0.11 * k, 2.2) for k, n in enumerate([72, 76, 79, 84, 88])])
save('SFX_Victory', y, 0.55)


# ---------------------------------------------------------------- music (short seamless loops)
def render_loop(bars, bpm, beats_per_bar, voices, length_pad=0.0):
    beat = 60.0 / bpm
    total = bars * beats_per_bar * beat
    y = np.zeros(int(SR * (total + 2.0)))
    for (start_beat, dur_beats, note, vol, kind) in voices:
        s = start_beat * beat
        d = dur_beats * beat
        f = midi(note)
        if kind == 'box':      # music box
            x = bell(f, min(2.2, d + 1.2), decay=2.6) * vol
        elif kind == 'pad':
            tt = t_(d + 0.8)
            x = (np.sin(2 * np.pi * f * tt) + 0.4 * np.sin(2 * np.pi * f * 2.003 * tt) + 0.2 * np.sin(2 * np.pi * f * 0.5 * tt))
            x *= np.minimum(1, tt / 0.6) * np.minimum(1, np.maximum(0, (d + 0.8 - tt) / 0.8)) * vol
        elif kind == 'pluck':  # pizzicato bass
            tt = t_(0.5)
            x = lp(signal.sawtooth(2 * np.pi * f * tt) * np.exp(-tt * 9), 900) * vol
        elif kind == 'marimba':
            x = bell(f, 0.6, partials=((1, 1), (4.0, 0.25), (9.2, 0.05)), decay=7) * vol
        elif kind == 'tick':
            x = wood(1200, 0.05, 0.6) * vol
        else:
            continue
        a = int(SR * s)
        y[a:a + len(x)] += x[:max(0, len(y) - a)]
    n = int(SR * total)
    tail = y[n:]
    loop = y[:n].copy()
    loop[:len(tail)] += tail[:n]          # wrap ringing tails → seamless loop
    return loop


# Bedroom lullaby (3/4, 72 bpm, F major, 8 bars) — original melody
mel = [(0, 1, 72), (1, 1, 77), (2, 1, 76), (3, 2, 74), (5, 1, 72), (6, 1, 69), (7, 1, 72), (8, 1, 77),
       (9, 3, 76), (12, 1, 74), (13, 1, 76), (14, 1, 77), (15, 2, 79), (17, 1, 77), (18, 1, 76), (19, 1, 74), (20, 1, 72), (21, 3, 77)]
voices = [(b, d, n, 0.55, 'box') for b, d, n in mel]
for bar, root in enumerate([53, 50, 46, 48, 53, 50, 46, 48]):
    voices += [(bar * 3, 1, root + 12, 0.2, 'box'), (bar * 3 + 1, 1, root + 19, 0.13, 'box'), (bar * 3 + 2, 1, root + 24, 0.13, 'box')]
    voices += [(bar * 3, 3, root, 0.06, 'pad')]
save('MUS_Bedroom', render_loop(8, 72, 3, voices), 0.45, loop=True)

# Dream (4/4, 84 bpm, D lydian-ish floaty, 8 bars)
voices = []
chords = [[50, 57, 64, 69], [52, 59, 66, 71], [47, 54, 62, 66], [52, 56, 64, 71]] * 2
for bar, ch in enumerate(chords):
    for k, n in enumerate(ch):
        voices.append((bar * 4, 4, n, 0.05, 'pad'))
    for s in range(8):
        voices.append((bar * 4 + s * 0.5, 0.5, ch[(s * 3) % 4] + 24, 0.12 if s % 2 == 0 else 0.07, 'box'))
melody = [(0, 2, 81), (2, 2, 80), (4, 3, 78), (8, 2, 76), (10, 2, 78), (12, 4, 73), (16, 2, 81), (18, 2, 83), (20, 3, 85), (24, 2, 83), (26, 2, 81), (28, 4, 78)]
voices += [(b, d, n, 0.32, 'box') for b, d, n in melody]
save('MUS_Dream', render_loop(8, 84, 4, voices), 0.45, loop=True)

# Boss chase (4/4, 148 bpm, bouncy, A minor → C, 8 bars)
voices = []
bass = [45, 45, 48, 48, 41, 41, 43, 43]
for bar, r in enumerate(bass):
    for s in range(4):
        voices.append((bar * 4 + s, 0.5, r + (12 if s % 2 else 0), 0.45, 'pluck'))
        voices.append((bar * 4 + s + 0.5, 0.25, 0, 0.2, 'tick'))
lead = [69, 72, 76, 74, 72, 71, 69, 71, 72, 76, 79, 77, 76, 74, 72, 74,
        77, 76, 74, 72, 74, 72, 71, 69, 71, 72, 74, 76, 74, 71, 69, 0]
for i, n in enumerate(lead):
    if n: voices.append((i, 0.75, n + 12, 0.28, 'marimba'))
save('MUS_Boss', render_loop(8, 148, 4, voices), 0.5, loop=True)

print('audio done ->', OUT, len([f for f in os.listdir(OUT) if f.endswith('.wav')]), 'files')
