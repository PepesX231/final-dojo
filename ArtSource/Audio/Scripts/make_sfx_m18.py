"""
DREAM RUSH — M18/M19 sounds (fists + the clockwork boss), synthesised (numpy; CC0, ours).
Punches are padded cartoon "thup / THWOMP"s (glove, not blade); the bell is a soft bronze toll; the winding key is a
ratchet "krrk-krrk". Shares helpers with make_sfx.py.  run: python3 -I make_sfx_m18.py <out_dir>
"""
import importlib.util, os, sys
import numpy as np
OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
here = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('sfxlib', os.path.join(here, 'make_sfx.py'))
# reuse the helper functions without re-rendering the whole M15 set
src = open(os.path.join(here, 'make_sfx.py')).read().split("os.makedirs(OUT, exist_ok=True)")[0]
g = {'__name__': 'sfxlib'}; sys.argv = [sys.argv[0], OUT]; exec(src, g)
SR, rng, t_, bp, sweep_noise, env, tone, mix, at, write = (g[k] for k in ('SR', 'rng', 't_', 'bp', 'sweep_noise', 'env', 'tone', 'mix', 'at', 'write'))
rng = np.random.default_rng(1818)
os.makedirs(OUT, exist_ok=True)

# ---- light punch: short air puff + padded glove thump (3 variants)
for i in range(3):
    d = 0.22; n = int(SR * d)
    puff = sweep_noise(0.09, 500 + 80 * i, 1400, 0.6) * env(int(SR * 0.09), 0.005, 0.03) * 0.35
    thup = tone(180 + 22 * i, d, 0.045, glide=85) * 1.0
    pad = bp(rng.normal(0, 1, n), 250, 1800) * env(n, 0.001, 0.02) * 0.45
    write(f'SFX_Punch_{i}', mix(at(puff, 0, d), at(mix(thup, pad), 0.035, d)), 0.6)
# ---- heavy punch / slam: big glove THWOMP with a sub drop and a little rubbery boing
for i in range(2):
    d = 0.6; n = int(SR * d)
    air = sweep_noise(0.16, 260, 1500, 0.6) * env(int(SR * 0.16), 0.02, 0.06) * 0.4
    body = tone(120 - 12 * i, 0.5, 0.11, glide=45) * 1.1
    sub = tone(52, 0.5, 0.2) * 0.6
    pad = np.tanh(bp(rng.normal(0, 1, int(SR * 0.5)), 150, 1600) * 2.5) * env(int(SR * 0.5), 0.001, 0.04) * 0.5
    boing = tone(330, 0.35, 0.12, glide=520) * 0.12 * np.sin(np.linspace(0, 40, int(SR * 0.35))) ** 2
    write(f'SFX_PunchHeavy_{i}', mix(at(air, 0, d), at(mix(body, sub, pad), 0.07, d), at(boing, 0.12, d)), 0.8)
# ---- bell toll: bronze partials (minor-third hum + strike), long soft tail, slow beat
d = 2.6; n = int(SR * d)
parts = [(220, 2.2, 0.9), (264, 1.6, 0.55), (330, 1.4, 0.5), (440, 1.1, 0.45), (587, 0.7, 0.3), (880, 0.45, 0.22), (1320, 0.25, 0.12)]
b = mix(*[tone(f, d, dec) * a * (1 + 0.08 * np.sin(np.linspace(0, 2 * np.pi * (2 + k), n))) for k, (f, dec, a) in enumerate(parts)])
strike = bp(rng.normal(0, 1, n), 1500, 6000) * env(n, 0.0005, 0.01) * 0.5
write('SFX_BellToll', mix(b, strike), 0.7)
# ---- ratchet: key being wound back / ripped loose — 6 clicks with a rising pitch
d = 0.5
clicks = [at(mix(bp(rng.normal(0, 1, int(SR * 0.03)), 2500, 7000) * env(int(SR * 0.03), 0.0003, 0.004), tone(1600 + k * 160, 0.03, 0.006) * 0.4), 0.07 * k, d) for k in range(6)]
write('SFX_Ratchet', mix(*clicks), 0.45)
# ---- strike warning tick-tock (clock tick for MIDNIGHT STRIKES telegraph)
d = 0.18
write('SFX_Tick', mix(bp(rng.normal(0, 1, int(SR * d)), 2000, 6000) * env(int(SR * d), 0.0004, 0.006) * 0.8, tone(2400, d, 0.01) * 0.3), 0.4)
print('m18 sfx ok')
