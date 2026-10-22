"""Tileable world noise for DreamTerrain/DreamGrass/DreamWater (R: large soft clumps, G: small detail, B: very large).
usage: python3 make_world_noise.py <out_png>"""
import sys, numpy as np
from PIL import Image
N = 512
rng = np.random.default_rng(42)
fy = np.fft.fftfreq(N)[:, None]; fx = np.fft.fftfreq(N)[None, :]
f = np.sqrt(fx * fx + fy * fy) + 1e-6
def periodic(power, lo, hi):
    spec = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) * f ** (-power)
    spec[(f < lo) | (f > hi)] = 0
    a = np.real(np.fft.ifft2(spec)); a = (a - a.min()) / (a.max() - a.min()); return a
r = periodic(2.0, 1 / 256, 1 / 8)
g = periodic(1.2, 1 / 64, 1 / 2)
b = periodic(2.4, 1 / 512, 1 / 32)
img = np.stack([r, g, b], -1)
Image.fromarray((img * 255).astype(np.uint8)).save(sys.argv[1])
print('noise ->', sys.argv[1])
