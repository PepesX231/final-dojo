"""MAMA'S COMING original chiptune soundtrack (bright, bouncy, a little quirky; Undertale-era 8-bit flavour).

All melodies are original compositions written for this game. Pure numpy synthesis:
pulse-wave leads (12.5/25/50% duty), triangle bass, noise drums, fast chord arpeggios, vibrato and a short echo.
Run with Blender's Python (has numpy + an OGG encoder):
  blender -b -P ArtSource/Audio/chiptune.py
Writes Assets/MamasComing/Audio/Music/*.ogg and the in-level loop to Resources/MamaFoley/music.ogg
(MamaAudio loads clips named "music" from Resources/MamaFoley over its procedural fallback).
"""
import os, sys, wave, math
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
OUT = ROOT + '/Assets/MamasComing/Audio/Music'
FOLEY = ROOT + '/Assets/MamasComing/Resources/MamaFoley'
TMP = ROOT + '/ArtSource/Audio/_wav'
SR = 32000
NAMES = {'C': 0, 'C#': 1, 'D': 2, 'D#': 3, 'E': 4, 'F': 5, 'F#': 6, 'G': 7, 'G#': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11, 'Eb': 3, 'Ab': 8}


def midi(n):
    name, octv = n[:-1], int(n[-1])
    return 12 * (octv + 1) + NAMES[name]


def hz(m): return 440.0 * 2 ** ((m - 69) / 12)


# ------------------------------------------------------------------ instruments
def pulse(f, n, duty=.25, vib=0.0, vib_rate=5.5, delay=.12):
    t = np.arange(n) / SR
    v = vib * np.clip((t - delay) / .15, 0, 1) * np.sin(2 * math.pi * vib_rate * t)
    ph = np.cumsum(f * 2 ** (v / 12) / SR)
    return np.where((ph % 1) < duty, 1.0, -1.0)


def tri(f, n):
    ph = np.cumsum(np.full(n, f / SR))
    x = ph % 1
    return (4 * np.abs(x - .5) - 1) * 1.0


def noise(n, seed=0, period=1):
    rng = np.random.default_rng(seed)
    x = rng.choice((-1.0, 1.0), size=n // period + 1)
    return np.repeat(x, period)[:n]


def env(n, a=.004, d=.08, s=.6, r=.04):
    t = np.arange(n) / SR; L = n / SR
    e = np.where(t < a, t / a, np.where(t < a + d, 1 - (1 - s) * (t - a) / d, s))
    rel = np.clip((L - t) / r, 0, 1)
    return e * rel


def kick(n):
    t = np.arange(n) / SR
    f = 150 * np.exp(-t * 28) + 45
    return np.sin(2 * math.pi * np.cumsum(f) / SR) * np.exp(-t * 14)


def snare(n):
    t = np.arange(n) / SR
    return (noise(n, 3, 2) * .8 + np.sin(2 * math.pi * 190 * t) * .4) * np.exp(-t * 22)


def hat(n, open_=False):
    t = np.arange(n) / SR
    return noise(n, 7, 1) * np.exp(-t * (18 if open_ else 70))


# ------------------------------------------------------------------ sequencing
class Song:
    def __init__(self, bpm, bars):
        self.step = 60 / bpm / 4                 # one 16th note, seconds
        self.len = int(bars * 16 * self.step * SR)
        self.mix = np.zeros(self.len + SR)

    def at(self, s16): return int(round(s16 * self.step * SR))

    def put(self, pos, x, gain):
        self.mix[pos:pos + len(x)] += x[:max(0, len(self.mix) - pos)] * gain

    def melody(self, bars, start_bar, gain=.22, duty=.25, octave=0, vib=.25, gate=.92, decay=.15):
        s = start_bar * 16
        for bar in bars:
            for note, ln in bar:
                if note != '-':
                    n = self.at(ln * gate)
                    x = pulse(hz(midi(note) + octave), n, duty, vib) * env(n, d=decay, s=.55)
                    self.put(self.at(s), x, gain)
                s += ln

    def bass(self, chords, start_bar, gain=.32, pattern='octave'):
        for i, ch in enumerate(chords):
            for half, root in enumerate(ch if isinstance(ch, list) else [ch, ch]):
                m = midi(root)
                for k in range(4):
                    s = (start_bar + i) * 16 + half * 8 + k * 2
                    mm = m + (12 if pattern == 'octave' and k % 2 else 0) + (7 if pattern == 'walk' and k == 2 else 0)
                    n = self.at(1.8)
                    self.put(self.at(s), tri(hz(mm), n) * env(n, d=.05, s=.8, r=.02), gain)

    def arps(self, chords, start_bar, gain=.07, octave=12):
        quality = {'': (0, 4, 7), 'm': (0, 3, 7), '7': (0, 4, 10), 'm7': (0, 3, 10)}
        for i, ch in enumerate(chords):
            parts = ch if isinstance(ch, list) else [ch, ch]
            for half, c in enumerate(parts):
                q = 'm7' if c.endswith('m7') else '7' if c.endswith('7') else 'm' if c.endswith('m') else ''
                root = c[:len(c) - len(q)]
                base = midi(root + '4') + octave
                iv = quality[q]
                for k in range(8):
                    s = (start_bar + i) * 16 + half * 8 + k
                    n = self.at(.95)
                    self.put(self.at(s), pulse(hz(base + iv[k % 3]), n, .125) * env(n, d=.03, s=.4, r=.01), gain)

    def drums(self, bars, start_bar, gain=.5, fill_every=8, busy=1):
        for b in range(bars):
            s0 = (start_bar + b) * 16
            fill = fill_every and (b % fill_every == fill_every - 1)
            for k in range(16):
                p = self.at(s0 + k)
                if k in (0, 8) or (busy and k == 10): self.put(p, kick(self.at(3)), gain * .9)
                if k in (4, 12): self.put(p, snare(self.at(3)), gain * .55)
                if k % 2 == 0: self.put(p, hat(self.at(1), k == 14), gain * .16)
                if fill and k >= 12: self.put(p, snare(self.at(1)), gain * .4)

    def render(self, echo=.18):
        x = self.mix[:self.len].copy()
        d = self.at(3)                                             # dotted-8th echo, wraps for a seamless loop
        x = x + np.roll(x, d) * echo
        x = np.tanh(x * 1.1) * .82                                 # gentle glue, never clips
        return x


def N(s):
    """'E5:2 G5:2 -:4' -> [('E5',2),('G5',2),('-',4)] for one bar."""
    out = []
    for tok in s.split():
        n, l = tok.split(':'); out.append((n, float(l)))
    assert abs(sum(l for _, l in out) - 16) < 1e-6, s
    return out


# ------------------------------------------------------------------ the songs (original)
def tidy_up_rumble():
    """In-level loop: bouncy, mischievous, C major, 140 bpm."""
    A = [N('E5:2 G5:2 C6:2 G5:2 A5:4 G5:4'), N('E5:2 C5:2 E5:2 A5:2 G5:6 E5:2'),
         N('F5:2 A5:2 C6:2 A5:2 D6:4 C6:2 A5:2'), N('G5:2 F5:2 E5:2 D5:2 B4:4 D5:4'),
         N('E5:2 G5:2 C6:2 G5:2 A5:4 G5:4'), N('E5:2 C5:2 E5:2 A5:2 C6:4 B5:2 A5:2'),
         N('A5:2 C6:2 F5:2 A5:2 B5:2 D6:2 G5:2 B5:2'), N('C6:8 G5:2 E5:2 C5:4')]
    B = [N('-:2 A5:1 A5:1 A5:2 G5:2 A5:4 C6:4'), N('B5:2 G5:2 D5:2 G5:2 B5:8'),
         N('-:2 G5:1 G5:1 G5:2 F5:2 G5:4 B5:4'), N('A5:2 E5:2 C5:2 E5:2 A5:8'),
         N('F5:2 A5:2 C6:2 F6:4 E6:2 D6:2 C6:2'), N('D6:4 B5:4 G5:4 D6:4'),
         N('E6:2 D6:2 C6:2 G5:2 E5:2 G5:2 C6:4'), N('D6:2 C6:2 A5:2 F#5:2 G5:4 -:4')]
    cA = ['C', 'Am', 'F', 'G', 'C', 'Am', ['F', 'G'], 'C']
    cB = ['F', 'G', 'Em', 'Am', 'F', 'G', 'C', ['D7', 'G']]
    bA = ['C2', 'A1', 'F1', 'G1', 'C2', 'A1', ['F1', 'G1'], 'C2']
    bB = ['F1', 'G1', 'E1', 'A1', 'F1', 'G1', 'C2', ['D2', 'G1']]
    s = Song(140, 32)
    s.melody(A, 0); s.melody(B, 8, duty=.5, gain=.2); s.melody(A, 16)
    s.melody(A, 16, gain=.08, duty=.125, octave=-5, vib=0)        # a little harmony on the repeat
    s.melody(B, 24, duty=.5, gain=.2); s.melody(B, 24, gain=.07, duty=.125, octave=12, vib=0)
    for st, (cs, bs) in enumerate(((cA, bA), (cB, bB), (cA, bA), (cB, bB))):
        s.bass(bs, st * 8)
        if st: s.arps(cs, st * 8)
    s.drums(32, 0)
    return s.render()


def mamas_coming():
    """Panic chase when Mama's car pulls in: A minor, 168 bpm, frantic but goofy."""
    A = [N('A5:1 A5:1 -:2 A5:2 C6:2 B5:2 A5:2 G5:2 E5:2'), N('F5:2 A5:2 C6:2 F6:2 E6:4 C6:4'),
         N('D6:2 B5:2 G5:2 D6:2 C6:2 B5:2 G5:4'), N('G#5:2 B5:2 E6:4 D6:2 B5:2 G#5:4'),
         N('A5:1 A5:1 -:2 A5:2 C6:2 B5:2 A5:2 G5:2 E5:2'), N('F5:2 A5:2 C6:2 F6:2 G6:4 F6:2 E6:2'),
         N('D6:2 E6:2 F6:2 D6:2 B5:2 C6:2 D6:4'), N('E6:8 B5:2 G#5:2 E5:4')]
    B = [N('E6:2 E6:2 D6:2 C6:2 G5:4 C6:4'), N('B5:2 B5:2 A5:2 G5:2 D6:8'),
         N('A5:2 F5:2 D5:2 F5:2 A5:4 D6:4'), N('B5:4 G#5:4 E5:4 G#5:4'),
         N('E6:2 G6:2 E6:2 C6:2 G5:4 E6:4'), N('D6:2 B5:2 G5:2 B5:2 D6:4 G6:4'),
         N('F6:2 E6:2 D6:2 C6:2 A5:4 F5:4'), N('E5:2 G#5:2 B5:2 E6:2 G#6:4 -:4')]
    cA = ['Am', 'F', 'G', 'E', 'Am', 'F', 'G', 'E7']
    cB = ['C', 'G', 'Dm', 'E', 'C', 'G', 'F', 'E7']
    bA = ['A1', 'F1', 'G1', 'E1', 'A1', 'F1', 'G1', 'E1']
    bB = ['C2', 'G1', 'D2', 'E1', 'C2', 'G1', 'F1', 'E1']
    s = Song(168, 32)
    for st, (mel, cs, bs) in enumerate(((A, cA, bA), (B, cB, bB), (A, cA, bA), (B, cB, bB))):
        s.melody(mel, st * 8, duty=.125 if st % 2 == 0 else .25, gain=.21, vib=.35)
        s.bass(bs, st * 8)
        s.arps(cs, st * 8, gain=.065)
    s.melody(A, 16, gain=.08, duty=.5, octave=-12, vib=0)
    s.drums(32, 0, gain=.55, fill_every=4)
    return s.render(echo=.12)


def home_sweet_mess():
    """Menu / lobby: cosy music-box waltz-feel in F, 96 bpm."""
    A = [N('C5:4 F5:4 A5:4 G5:2 F5:2'), N('A5:6 D5:2 F5:8'), N('D5:4 F5:4 Bb5:4 A5:2 G5:2'), N('G5:12 -:4'),
         N('C5:4 F5:4 A5:4 C6:4'), N('D6:6 C6:2 A5:8'), N('Bb5:4 D6:4 C6:4 E5:4'), N('F5:12 -:4')]
    c = ['F', 'Dm', 'A#', 'C', 'F', 'Dm', ['A#', 'C'], 'F']
    b = ['F1', 'D2', 'A#1', 'C2', 'F1', 'D2', ['A#1', 'C2'], 'F1']
    s = Song(96, 16)
    s.melody(A, 0, duty=.5, gain=.17, vib=.2, decay=.3)
    s.melody(A, 8, duty=.25, gain=.17, vib=.25, decay=.3)
    s.melody(A, 8, duty=.125, gain=.06, octave=12, vib=0, decay=.2)
    for st in (0, 1):
        s.bass(b, st * 8, gain=.24, pattern='walk')
        s.arps(c, st * 8, gain=.055)
    for bar in range(8, 16):                                       # soft brush beat on the second pass
        for k in (0, 8): s.put(s.at(bar * 16 + k), kick(s.at(3)), .25)
        for k in (4, 12): s.put(s.at(bar * 16 + k), hat(s.at(2), True), .08)
    return s.render(echo=.25)


def win_jingle():
    s = Song(150, 2)
    s.melody([N('C5:1 E5:1 G5:1 C6:1 E6:2 G6:2 -:1 E6:1 G6:6'), N('C7:8 -:8')], 0, duty=.25, gain=.25, vib=.3, gate=.95)
    s.melody([N('-:8 G5:2 C6:6'), N('E6:8 -:8')], 0, duty=.125, gain=.1)
    s.bass([['C2', 'G1'], 'C2'], 0, gain=.3)
    return s.render(echo=.2)


def write(name, x, dests):
    os.makedirs(TMP, exist_ok=True)
    wav = f'{TMP}/{name}.wav'
    pcm = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(wav, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    for d in dests:
        os.makedirs(os.path.dirname(d), exist_ok=True)
        to_ogg(wav, d)
    print('SONG', name, round(len(x) / SR, 1), 's ->', [os.path.relpath(d, ROOT) for d in dests])


def to_ogg(wav, ogg):
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.sequence_editor_create()
    strip = sc.sequence_editor.strips.new_sound('s', wav, 1, 1) if hasattr(sc.sequence_editor, 'strips') else sc.sequence_editor.sequences.new_sound('s', wav, 1, 1)
    sc.render.fps = 100
    sc.frame_start = 1; sc.frame_end = strip.frame_final_end
    sc.render.ffmpeg.audio_mixrate = SR
    bpy.ops.sound.mixdown(filepath=ogg, check_existing=False, container='OGG', codec='VORBIS', format='S16', mixrate=SR, channels='MONO', bitrate=128)


write('MC_TidyUpRumble', tidy_up_rumble(), [OUT + '/MC_TidyUpRumble.ogg', FOLEY + '/music.ogg'])
write('MC_MamasComing', mamas_coming(), [OUT + '/MC_MamasComing.ogg'])
write('MC_HomeSweetMess', home_sweet_mess(), [OUT + '/MC_HomeSweetMess.ogg'])
write('MC_WinJingle', win_jingle(), [OUT + '/MC_WinJingle.ogg'])
