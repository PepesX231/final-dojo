"""Soft cartoon footsteps (user: running sounded like a heavy 'tuk-tuk-tuk'). Light rubber-sole pats: band-passed
noise click + a tiny squeaky tone, quiet (peak ~0.22), 6 variants with different pitch/brightness."""
import numpy as np, wave, sys, os
OUT = sys.argv[1]
SR = 44100
rng = np.random.default_rng(7)
def bp(x, lo, hi):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    X[(f < lo) | (f > hi)] = 0; return np.fft.irfft(X, len(x))
for i in range(6):
    n = int(SR * 0.075); t = np.arange(n) / SR
    noise = bp(rng.normal(0, 1, n), 900 + i * 120, 4200 + i * 300)
    env = np.exp(-t / (0.012 + i * 0.0015)) * (1 - np.exp(-t / 0.0015))
    click = noise / np.abs(noise).max() * env
    f0 = 520 + i * 45 + rng.uniform(-20, 20)
    squeak = np.sin(2 * np.pi * f0 * t * (1 + 0.6 * t)) * np.exp(-t / 0.018) * 0.25
    puff = np.sin(2 * np.pi * 140 * t) * np.exp(-t / 0.02) * 0.18
    y = click * 0.8 + squeak + puff
    y = y / np.abs(y).max() * 0.22
    with wave.open(os.path.join(OUT, f'SFX_Footstep_{i}.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype(np.int16).tobytes())
print('footsteps ok')
