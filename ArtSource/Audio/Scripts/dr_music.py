"""
DREAM RUSH music toolkit (M15): compose loops in code (pretty_midi), render them with FluidSynth + the GeneralUser GS
SoundFont (S. Christian Collins, free for commercial use — see docs/AUDIO_CREDITS.md), master with pedalboard, and
write seamless-loop OGGs (the release / reverb tail past the loop point is folded back onto the start).
"""
import numpy as np
import pretty_midi
import fluidsynth
import soundfile as sf
from pedalboard import Pedalboard, Compressor, Limiter, Reverb, HighShelfFilter, LowShelfFilter, Gain, HighpassFilter

SF2 = '/tmp/claude-0/sf/gu/GeneralUser-GS.sf2'
SR = 44100
PC = {'C': 0, 'C#': 1, 'Db': 1, 'D': 2, 'D#': 3, 'Eb': 3, 'E': 4, 'F': 5, 'F#': 6, 'Gb': 6, 'G': 7, 'G#': 8, 'Ab': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11}
QUAL = {'': [0, 4, 7], 'm': [0, 3, 7], '7': [0, 4, 7, 10], 'maj7': [0, 4, 7, 11], 'm7': [0, 3, 7, 10], 'sus4': [0, 5, 7], 'sus2': [0, 2, 7],
        'dim': [0, 3, 6], 'add9': [0, 4, 7, 14], 'm9': [0, 3, 7, 10, 14], 'maj9': [0, 4, 7, 11, 14], '9': [0, 4, 7, 10, 14], 'm7b5': [0, 3, 6, 10],
        'aug': [0, 4, 8], '6': [0, 4, 7, 9], 'm6': [0, 3, 7, 9], '7sus4': [0, 5, 7, 10], 'mmaj7': [0, 3, 7, 11], 'dim7': [0, 3, 6, 9], '7b9': [0, 4, 7, 10, 13]}
MODES = {'major': [0, 2, 4, 5, 7, 9, 11], 'minor': [0, 2, 3, 5, 7, 8, 10], 'dorian': [0, 2, 3, 5, 7, 9, 10], 'harmonic': [0, 2, 3, 5, 7, 8, 11],
         'mixolydian': [0, 2, 4, 5, 7, 9, 10], 'lydian': [0, 2, 4, 6, 7, 9, 11]}


def parse(ch):
    """'F#m7/C#' → (root pc, intervals, bass pc)."""
    bass = None
    if '/' in ch: ch, b = ch.split('/'); bass = PC[b]
    r = ch[:2] if len(ch) > 1 and ch[1] in '#b' else ch[:1]
    q = ch[len(r):]
    return PC[r], QUAL[q], (bass if bass is not None else PC[r])


class Song:
    def __init__(self, name, bpm, key, mode='major', beats=4, swing=0.0, seed=1):
        self.name, self.bpm, self.beats, self.swing = name, bpm, beats, swing
        self.key = PC[key]; self.scale = [(self.key + s) % 12 for s in MODES[mode]]
        self.pm = pretty_midi.PrettyMIDI(initial_tempo=bpm)
        self.rng = np.random.default_rng(seed)
        self.prog = []          # chord per bar (or list of chords per bar)
        self.bars = 0
        self._insts = {}

    # ---------------------------------------------------------------- time
    def sec(self, bar, beat):
        b = beat
        if self.swing and abs(b * 2 - round(b * 2)) < 1e-6 and (round(b * 2) % 2 == 1):
            b += self.swing * 0.5 * 0.5            # push the off-8th later
        return (bar * self.beats + b) * 60.0 / self.bpm

    def inst(self, name, program, drum=False, vol=100, pan=64, reverb=40, chorus=0):
        if name in self._insts: return self._insts[name]
        i = pretty_midi.Instrument(program=program, is_drum=drum, name=name)
        for num, val in ((7, vol), (10, pan), (91, reverb), (93, chorus)):
            i.control_changes.append(pretty_midi.ControlChange(num, val, 0.0))
        self.pm.instruments.append(i); self._insts[name] = i
        return i

    def note(self, inst, pitch, bar, beat, dur, vel=90):
        s = self.sec(bar, beat); e = self.sec(bar, beat + dur)
        if pitch < 0 or pitch > 127 or e <= s: return
        inst.notes.append(pretty_midi.Note(int(np.clip(vel, 1, 127)), int(pitch), s, e))

    # ---------------------------------------------------------------- harmony helpers
    def set_prog(self, chords, repeat=1):
        """chords: list per bar; an entry may be 'C' or 'C Am' (two halves) etc."""
        p = []
        for c in chords: p.append(c.split())
        self.prog = p * repeat; self.bars = len(self.prog)

    def chord_at(self, bar, beat):
        cs = self.prog[bar % len(self.prog)]
        k = min(int(beat / (self.beats / len(cs))), len(cs) - 1)
        return cs[k], k, self.beats / len(cs)

    def segments(self):
        for bar in range(self.bars):
            cs = self.prog[bar]; L = self.beats / len(cs)
            for k, c in enumerate(cs): yield bar, k * L, L, c

    @staticmethod
    def voicing(c, center=60, prev=None, n=4):
        r, iv, _ = parse(c)
        pcs = [(r + i) % 12 for i in iv]
        cands = []
        for base in range(center - 12, center + 12):
            if base % 12 != pcs[0] and base % 12 not in pcs: continue
        # build close-position voicings for each inversion; pick nearest to prev / center
        best, bd = None, 1e9
        for inv in range(len(pcs)):
            order = pcs[inv:] + pcs[:inv]
            for start in range(center - 10, center + 6):
                if start % 12 != order[0]: continue
                v = [start]
                for pc in order[1:]:
                    x = v[-1] + 1
                    while x % 12 != pc: x += 1
                    v.append(x)
                v = v[:n]
                d = sum(abs(a - b) for a, b in zip(v, prev)) if prev and len(prev) == len(v) else abs(np.mean(v) - center) * 2
                if d < bd: bd, best = d, v
        return best

    def pad(self, inst, center=60, vel=60, n=4, rhythm=None):
        prev = None
        for bar, beat, L, c in self.segments():
            v = self.voicing(c, center, prev, n); prev = v
            hits = rhythm or [(0, L)]
            for (b, d) in hits:
                if b >= L: continue
                for p in v: self.note(inst, p, bar, beat + b, min(d, L - b) * 0.98, vel + self.rng.integers(-4, 5))

    def arp(self, inst, pattern, step, center=72, vel=70, gate=0.9, accent=None, octaves=1):
        """pattern: chord-tone indices (−1 = rest; ≥ len wraps up an octave)."""
        for bar, beat, L, c in self.segments():
            r, iv, _ = parse(c)
            base = center - ((center - r) % 12)
            tones = [base + i for i in iv]
            tones = tones + [t + 12 for t in tones] + [t + 24 for t in tones]
            k = 0; t = 0.0
            while t < L - 1e-6:
                idx = pattern[k % len(pattern)]
                if idx >= 0:
                    vv = vel + (accent if accent and k % len(pattern) == 0 else 0) + self.rng.integers(-5, 6)
                    self.note(inst, tones[idx % len(tones)], bar, beat + t, step * gate, vv)
                k += 1; t += step

    def bass(self, inst, pattern, octave=36, vel=95):
        """pattern: [(beat, 'R'|'5'|'8'|'3'|'7'|'b7'|'-1'|'ap', dur)] per chord segment (beats relative)."""
        segs = list(self.segments())
        for si, (bar, beat, L, c) in enumerate(segs):
            r, iv, b = parse(c)
            root = octave + ((b - octave) % 12)
            nxt = parse(segs[(si + 1) % len(segs)][3])[2]
            for (pb, deg, d) in pattern:
                if pb >= L: continue
                p = {'R': root, '5': root + 7 if root + 7 < octave + 14 else root - 5, '8': root + 12, '3': root + iv[1],
                     '7': root + (iv[3] if len(iv) > 3 else 10), '-1': root - 12,
                     'ap': octave + ((nxt - octave) % 12) + (1 if (nxt - b) % 12 > 6 else -1)}[deg]
                self.note(inst, p, bar, beat + pb, min(d, L - pb), vel + self.rng.integers(-6, 4) + (6 if pb == 0 else 0))

    def drums(self, inst, pattern, bars=None, vel=1.0, fill_every=8, fill=None):
        """pattern: {gm_note: [(beat, vel), ...]} for one bar; fill: same for the last bar of each `fill_every`."""
        rng = range(self.bars) if bars is None else bars
        for bar in rng:
            pat = fill if fill and (bar % fill_every) == fill_every - 1 else pattern
            for n, hits in pat.items():
                for (b, v) in hits:
                    self.note(inst, n, bar, b, 0.1, v * vel + self.rng.integers(-6, 6))

    # ---------------------------------------------------------------- melody
    def scale_pitches(self, lo, hi):
        return [p for p in range(lo, hi + 1) if p % 12 in self.scale]

    def melody(self, inst, rhythms, lo, hi, vel=88, start=None, form='AABA', bars_per_phrase=2, legato=0.92, end_long=True, rest_prob=0.0):
        """Phrase-based melody: motif (A) is re-snapped to each new chord so it repeats but fits; B is a fresh motif a 3rd higher."""
        sp = self.scale_pitches(lo, hi)
        rng = self.rng

        def chord_tones(bar, beat):
            c, _, _ = self.chord_at(bar, beat); r, iv, _ = parse(c)
            return [p for p in sp if (p - r) % 12 in [i % 12 for i in iv]]

        def make_motif(seed_pitch, bar0, nbar):
            notes = []; p = seed_pitch
            for b in range(nbar):
                rh = rhythms[rng.integers(len(rhythms))]
                for (beat, dur) in rh:
                    strong = abs(beat - round(beat)) < 1e-6 and int(round(beat)) % 2 == 0
                    if strong or dur >= 1.0:
                        ct = chord_tones(bar0 + b, beat)
                        ct = sorted(ct, key=lambda x: abs(x - p) + (0 if abs(x - p) <= 4 else 3) + rng.random() * 2.5)
                        p = ct[0] if ct else p
                    else:
                        i = min(range(len(sp)), key=lambda k: abs(sp[k] - p))
                        step = rng.choice([-2, -1, -1, 1, 1, 2])
                        if p > hi - 4: step = -abs(step)
                        if p < lo + 4: step = abs(step)
                        p = sp[int(np.clip(i + step, 0, len(sp) - 1))]
                    notes.append((b, beat, dur, p))
            return notes

        def resnap(motif, bar0, shift=0):
            out = []
            for (b, beat, dur, p) in motif:
                q = p + shift
                strong = abs(beat - round(beat)) < 1e-6 and int(round(beat)) % 2 == 0
                pool = chord_tones(bar0 + b, beat) if (strong or dur >= 1) else sp
                q = min(pool, key=lambda x: abs(x - q)) if pool else q
                out.append((b, beat, dur, q))
            return out

        start = start or sp[len(sp) // 2]
        phrases = {}
        n = 0; bar = 0
        while bar < self.bars:
            letter = form[n % len(form)]
            nb = min(bars_per_phrase, self.bars - bar)
            if letter not in phrases:
                seedp = start + (4 if letter == 'B' else 0)
                phrases[letter] = make_motif(seedp, bar, nb)
                mot = phrases[letter]
            else:
                mot = resnap(phrases[letter], bar, shift=int(rng.choice([0, 0, 2, -2])))
            last_phrase = bar + nb >= self.bars or (n % len(form)) == len(form) - 1
            for i, (b, beat, dur, p) in enumerate(mot):
                if rest_prob and rng.random() < rest_prob and i not in (0, len(mot) - 1): continue
                d = dur * legato
                if end_long and last_phrase and i == len(mot) - 1:
                    c, _, _ = self.chord_at(bar + b, beat); r = parse(c)[0]
                    p = min([x for x in sp if x % 12 == r] or [p], key=lambda x: abs(x - p))
                    d = max(d, self.beats - beat - 0.1)
                self.note(inst, p, bar + b, beat, d, vel + (8 if abs(beat) < 1e-6 else 0) + rng.integers(-6, 5))
            bar += nb; n += 1

    # ---------------------------------------------------------------- render
    def length(self):
        return self.sec(self.bars, 0)

    def render(self, tail=4.0, gain=0.45):
        fs = fluidsynth.Synth(samplerate=float(SR), gain=gain)
        fs.setting('synth.reverb.active', 1); fs.setting('synth.chorus.active', 1)
        sfid = fs.sfload(SF2)
        ev = []
        ch_next = 0
        for inst in self.pm.instruments:
            if inst.is_drum: ch = 9
            else:
                ch = ch_next if ch_next != 9 else 10; ch_next = ch + 1
            fs.program_select(ch, sfid, 128 if inst.is_drum else 0, 0 if inst.is_drum else inst.program)
            if inst.is_drum and inst.program: fs.program_select(ch, sfid, 128, inst.program)
            for c in inst.control_changes: ev.append((c.time, 0, ch, c.number, c.value))
            for n in inst.notes:
                ev.append((n.end, 1, ch, n.pitch, 0)); ev.append((n.start, 2, ch, n.pitch, n.velocity))
        ev.sort(key=lambda e: (e[0], e[1]))
        total = int((self.length() + tail) * SR)
        out = []; cur = 0
        for (t, kind, ch, a, b) in ev:
            s = int(t * SR)
            if s > cur: out.append(np.array(fs.get_samples(s - cur), dtype=np.int16)); cur = s
            if kind == 0: fs.cc(ch, a, b)
            elif kind == 1: fs.noteoff(ch, a)
            else: fs.noteon(ch, a, b)
        if total > cur: out.append(np.array(fs.get_samples(total - cur), dtype=np.int16))
        fs.delete()
        y = np.concatenate(out).astype(np.float32) / 32768.0
        return y.reshape(-1, 2)

    def master(self, y, reverb=0.12, bright=1.5, target_rms=-17.0):
        board = Pedalboard([HighpassFilter(30), LowShelfFilter(cutoff_frequency_hz=120, gain_db=1.0), HighShelfFilter(cutoff_frequency_hz=7000, gain_db=bright),
                            Reverb(room_size=0.55, wet_level=reverb, dry_level=1.0, width=1.0),
                            Compressor(threshold_db=-18, ratio=2.5, attack_ms=12, release_ms=160)])
        z = board(y.T, SR).T
        rms = 20 * np.log10(np.sqrt(np.mean(z ** 2)) + 1e-9)
        z = z * (10 ** ((target_rms - rms) / 20))
        z = Pedalboard([Limiter(threshold_db=-2.0, release_ms=120)])(z.T, SR).T
        return z

    def write_loop(self, path, y):
        L = int(round(self.length() * SR))
        body = y[:L].copy(); tail = y[L:]
        n = min(len(tail), L)
        body[:n] += tail[:n]                 # fold the release/reverb tail onto the loop start → seamless
        pk = np.abs(body).max()
        if pk > 0.89: body *= 0.89 / pk         # the folded tail may push peaks over the limiter: trim, never clip
        import subprocess, tempfile
        tmp = tempfile.mktemp(suffix='.wav'); sf.write(tmp, body, SR, subtype='PCM_16')
        subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', '5', path], check=True)
        import os; os.remove(tmp)
        return len(body) / SR
