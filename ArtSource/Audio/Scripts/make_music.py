"""
DREAM RUSH — M15 soundtrack (user 14:40: "เสียงเพลงอีก sound effect อีก"). Eight original seamless loops composed in code
and rendered with GeneralUser GS. run: python3 -I make_music.py <out_dir> [names...]
  Bedroom   lobby / bedroom hub — music-box lullaby (F, 76)
  Dream     the chair dream — tip-toe pizzicato caper (D, 104)
  Boss      THE CHAIR — comic-epic chase (D minor, 146)
  World1    Densha world — bright anime rush (A, 132)
  Palace    Midnight Palace streets — phantom-thief acid jazz (C# dorian, 112, swung)
  Midnight  COUNT MIDNIGHT — dark clockwork waltz (E minor, 3/4, 168)
  Galaxy    galaxy select — floating starlight (Eb lydian, 84)
  Interior  palace rooms — cozy lo-fi lounge (Bb, 84, swung)
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dr_music import Song  # noqa: E402

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
ONLY = set(sys.argv[2:])
KICK, SNARE, RIM, CLAP, HAT, PHAT, OHAT, CRASH, RIDE, BELL, TAMB, SHAKER, WOOD_H, WOOD_L, TRI, TOM_L, TOM_M, TOM_H, CHIME = 36, 38, 37, 39, 42, 44, 46, 49, 51, 53, 54, 70, 76, 77, 81, 45, 47, 50, 84


def bedroom():
    s = Song('Bedroom', 76, 'F', 'major', seed=11)
    s.set_prog(['F', 'Dm7', 'Bbmaj7', 'C7sus4 C7', 'F/A', 'Bb', 'Gm7', 'C7', 'Dm', 'Am7', 'Bbmaj7', 'F/C', 'Gm7', 'Bb/C', 'Fmaj7', 'C7sus4'])
    box = s.inst('musicbox', 10, vol=96, pan=50, reverb=70)
    cel = s.inst('celesta', 8, vol=70, pan=80, reverb=70)
    pad = s.inst('strings', 49, vol=64, reverb=80)
    harp = s.inst('harp', 46, vol=78, pan=40, reverb=60)
    bass = s.inst('bass', 32, vol=78, reverb=30)
    s.pad(pad, 57, 54, 4)
    s.arp(harp, [0, 1, 2, 3, 2, 1], 0.5, 60, 58, gate=1.6)
    s.bass(bass, [(0, 'R', 1.8), (2, '5', 1.8)], 36, 80)
    s.melody(box, [[(0, 1), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 1.5), (1.5, 0.5), (2, 1), (3, 1)], [(0, 0.5), (0.5, 0.5), (1, 1), (2, 1.5), (3.5, 0.5)], [(0, 3), (3, 1)]],
             72, 89, vel=86, start=77, form='AABA')
    s.arp(cel, [-1, -1, 2, -1, -1, 3, -1, 1], 0.5, 84, 50, gate=0.6)
    return s


def dream():
    s = Song('Dream', 104, 'D', 'major', seed=23)
    s.set_prog(['D', 'Bm', 'G', 'A7', 'D', 'F#m', 'G Em7', 'A7', 'Bm', 'G', 'D/F#', 'Em7 A7', 'D', 'B7', 'Em7 A7', 'D'])
    piz = s.inst('pizz', 45, vol=96, pan=44, reverb=50)
    cl = s.inst('clarinet', 71, vol=94, pan=76, reverb=55)
    glock = s.inst('glock', 9, vol=60, pan=90, reverb=60)
    bsn = s.inst('bassoon', 70, vol=86, pan=60, reverb=40)
    pad = s.inst('strings', 48, vol=52, reverb=70)
    dr = s.inst('drums', 0, drum=True, vol=70, reverb=30)
    s.pad(pad, 62, 46, 3)
    s.arp(piz, [0, 2, 1, 2], 0.5, 64, 70, gate=0.4, accent=10)
    s.bass(bsn, [(0, 'R', 0.45), (1, '5', 0.45), (2, '8', 0.45), (3, '5', 0.45)], 40, 82)
    s.melody(cl, [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 0.5), (2.5, 0.5), (3, 1)], [(0, 1), (1, 0.5), (1.5, 0.5), (2, 1.5), (3.5, 0.5)], [(0, 0.75), (0.75, 0.25), (1, 1), (2, 2)]],
             67, 84, vel=90, start=74)
    s.arp(glock, [3, -1, -1, -1, 2, -1, -1, -1], 0.5, 84, 52, gate=0.5)
    s.drums(dr, {WOOD_H: [(0, 60), (1, 40), (2, 55), (3, 40)], SHAKER: [(0.5, 35), (1.5, 35), (2.5, 35), (3.5, 35)], TRI: [(0, 28)]},
            fill={WOOD_H: [(0, 60), (1, 40), (2, 55), (2.5, 55), (3, 60), (3.5, 70)], TRI: [(3, 50)]})
    return s


def boss():
    s = Song('Boss', 146, 'D', 'harmonic', seed=37)
    s.set_prog(['Dm', 'Dm', 'Bb', 'A7', 'Dm', 'Gm', 'Bb A', 'A7', 'Gm', 'Dm', 'Eb', 'A7', 'Dm', 'Bb', 'Gm A7', 'Dm A7'])
    st = s.inst('strings', 48, vol=96, pan=50, reverb=40)
    hi = s.inst('violins', 40, vol=82, pan=86, reverb=45)
    brass = s.inst('brass', 61, vol=96, pan=60, reverb=40)
    horn = s.inst('horns', 60, vol=84, pan=40, reverb=50)
    timp = s.inst('timpani', 47, vol=100, reverb=40)
    bass = s.inst('bass', 33, vol=100, reverb=10)
    dr = s.inst('drums', 16, drum=True, vol=96, reverb=25)
    s.arp(st, [0, 0, 2, 0, 1, 0, 2, 1], 0.5, 50, 84, gate=0.5, accent=14)
    s.bass(bass, [(0, 'R', 0.45), (0.5, 'R', 0.45), (1, '8', 0.45), (1.5, 'R', 0.45), (2, 'R', 0.45), (2.5, '5', 0.45), (3, '8', 0.45), (3.5, 'ap', 0.45)], 33, 98)
    s.pad(horn, 58, 70, 3, rhythm=[(0, 1.5), (1.5, 0.5), (2, 2)])
    s.melody(brass, [[(0, 1.5), (1.5, 0.5), (2, 1), (3, 1)], [(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 1), (1, 1), (2, 0.75), (2.75, 0.25), (3, 1)]],
             62, 81, vel=104, start=74, legato=0.85)
    s.arp(hi, [2, 1, 0, 1], 0.25, 86, 52, gate=0.6)
    for bar in range(s.bars):
        r = 38 if bar % 2 == 0 else 45
        s.note(timp, r, bar, 0, 0.5, 110); s.note(timp, r, bar, 2.5, 0.5, 90)
    s.drums(dr, {KICK: [(0, 110), (1.5, 95), (2, 105), (3.5, 90)], SNARE: [(1, 105), (3, 110)], HAT: [(x / 2, 70 if x % 2 else 85) for x in range(8)], CRASH: []},
            fill={KICK: [(0, 110), (2, 105)], SNARE: [(1, 100), (2.5, 90), (2.75, 95), (3, 105), (3.25, 110), (3.5, 115), (3.75, 120)], CRASH: []})
    s.drums(dr, {CRASH: [(0, 105)]}, bars=range(0, s.bars, 4))
    return s


def world1():
    s = Song('World1', 132, 'A', 'major', seed=41)
    s.set_prog(['Dmaj7', 'E', 'C#m7', 'F#m', 'Dmaj7', 'E', 'A', 'A7', 'Dmaj7', 'E/D', 'C#m7', 'F#m7', 'Bm7', 'C#m7', 'Dmaj7', 'E7sus4 E'])
    ep = s.inst('epiano', 4, vol=86, pan=44, reverb=45, chorus=40)
    lead = s.inst('lead', 80, vol=78, pan=70, reverb=50, chorus=30)
    lead2 = s.inst('lead_oct', 81, vol=56, pan=58, reverb=60)
    bass = s.inst('bass', 36, vol=100, reverb=10)
    pad = s.inst('pad', 50, vol=62, reverb=70)
    bell = s.inst('bell', 14, vol=55, pan=90, reverb=70)
    dr = s.inst('drums', 0, drum=True, vol=96, reverb=20)
    s.pad(pad, 62, 50, 4)
    s.pad(ep, 64, 64, 4, rhythm=[(0, 0.5), (0.75, 0.5), (1.5, 0.5), (2.5, 0.5), (3, 0.75)])
    s.bass(bass, [(0, 'R', 0.4), (0.5, '8', 0.3), (1, 'R', 0.4), (1.5, 'R', 0.3), (2, 'R', 0.4), (2.5, '8', 0.3), (3, '5', 0.4), (3.5, 'ap', 0.4)], 33, 96)
    rh = [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 0.5), (2.5, 1), (3.5, 0.5)], [(0, 1.5), (1.5, 0.5), (2, 0.5), (2.5, 0.5), (3, 1)], [(0, 0.75), (0.75, 0.75), (1.5, 0.5), (2, 2)]]
    s.melody(lead, rh, 69, 88, vel=96, start=76)
    for n in list(lead.notes): lead2.notes.append(type(n)(max(1, n.velocity - 20), n.pitch - 12, n.start, n.end))
    s.arp(bell, [0, 2, 3, 2], 1.0, 84, 46, gate=0.5)
    s.drums(dr, {KICK: [(0, 110), (1.5, 85), (2, 105), (2.75, 80)], SNARE: [(1, 100), (3, 105)], CLAP: [(3, 70)], HAT: [(x / 2, 60 if x % 2 else 78) for x in range(8)], OHAT: [(3.5, 70)]},
            fill={KICK: [(0, 110), (2, 100)], SNARE: [(1, 100), (2.5, 85), (3, 100), (3.25, 105), (3.5, 110), (3.75, 115)], HAT: [(0, 70), (0.5, 60), (1, 70), (1.5, 60)]})
    s.drums(dr, {CRASH: [(0, 100)]}, bars=range(0, s.bars, 8))
    return s


def palace():
    s = Song('Palace', 112, 'C#', 'dorian', swing=0.33, seed=53)
    s.set_prog(['C#m9', 'F#9', 'C#m9', 'F#9', 'Amaj7', 'G#7', 'C#m7', 'Bm7 E9', 'Amaj7', 'G#m7', 'F#m7', 'B7', 'Emaj7', 'G#7', 'C#m9', 'D#m7b5 G#7'])
    rh = s.inst('rhodes', 4, vol=88, pan=48, reverb=40, chorus=30)
    sax = s.inst('sax', 65, vol=96, pan=72, reverb=50)
    bass = s.inst('bass', 33, vol=104, reverb=10)
    strs = s.inst('strings', 48, vol=70, pan=36, reverb=60)
    vib = s.inst('vibes', 11, vol=60, pan=90, reverb=60)
    dr = s.inst('drums', 0, drum=True, vol=92, reverb=20)
    s.pad(rh, 62, 66, 4, rhythm=[(0, 0.4), (0.5, 0.4), (1.5, 1), (2.5, 0.4), (3, 0.9)])
    s.pad(strs, 69, 44, 3, rhythm=[(0, 0.2), (2.5, 0.2)])
    s.bass(bass, [(0, 'R', 0.7), (0.75, '8', 0.2), (1, '5', 0.4), (1.5, '7', 0.4), (2, 'R', 0.4), (2.5, '8', 0.3), (3, '3', 0.4), (3.5, 'ap', 0.45)], 30, 100)
    s.melody(sax, [[(0, 0.5), (0.5, 0.5), (1, 1.5), (2.5, 0.5), (3, 1)], [(0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 1), (1, 0.5), (1.5, 1), (2.5, 1.5)], [(0, 3.5)]],
             61, 80, vel=94, start=68, rest_prob=0.1)
    s.arp(vib, [-1, 2, -1, 3, -1, -1, 1, -1], 0.5, 79, 50, gate=0.8)
    s.drums(dr, {KICK: [(0, 105), (1.75, 75), (2.5, 95)], SNARE: [(1, 95), (3, 100)], RIM: [(2.75, 45)], HAT: [(x / 2, 55 if x % 2 else 72) for x in range(8)],
                 OHAT: [(3.5, 62)], TAMB: [(1, 40), (3, 40)]},
            fill={KICK: [(0, 105), (2, 95)], SNARE: [(1, 95), (2.5, 80), (3, 95), (3.5, 105), (3.75, 110)], HAT: [(0, 60), (0.5, 50), (1, 60)], OHAT: [(2, 70)]})
    s.drums(dr, {CRASH: [(0, 90)]}, bars=range(0, s.bars, 8))
    return s


def midnight():
    s = Song('Midnight', 168, 'E', 'harmonic', beats=3, seed=67)
    s.set_prog(['Em', 'Em', 'Am', 'Am', 'B7', 'B7', 'Em', 'E7', 'Am', 'D', 'G', 'C', 'F#m7b5', 'B7', 'Em', 'B7',
                'C', 'C', 'Am', 'Am', 'D', 'D', 'G', 'B7', 'Em', 'Am', 'F#m7b5', 'B7', 'Em', 'C', 'B7', 'B7'])
    harps = s.inst('harpsichord', 6, vol=82, pan=40, reverb=50)
    org = s.inst('organ', 19, vol=62, reverb=80)
    choir = s.inst('choir', 52, vol=74, pan=70, reverb=80)
    st = s.inst('strings', 44, vol=78, pan=56, reverb=60)
    lead = s.inst('violin', 40, vol=96, pan=60, reverb=55)
    bass = s.inst('bass', 43, vol=96, reverb=30)
    timp = s.inst('timpani', 47, vol=96, reverb=50)
    dr = s.inst('drums', 48, drum=True, vol=80, reverb=40)
    s.pad(org, 55, 46, 4)
    s.pad(choir, 64, 56, 3, rhythm=[(0, 3)])
    s.arp(harps, [0, 1, 2, 3, 2, 1], 0.5, 64, 70, gate=0.6, accent=12)
    s.pad(st, 60, 60, 3, rhythm=[(1, 0.4), (2, 0.4)])
    s.bass(bass, [(0, 'R', 0.9)], 31, 96)
    s.melody(lead, [[(0, 2), (2, 1)], [(0, 1), (1, 1), (2, 1)], [(0, 1.5), (1.5, 0.5), (2, 1)], [(0, 3)]], 64, 86, vel=96, start=76, bars_per_phrase=4, form='ABAC')
    for bar in range(s.bars):
        if bar % 2 == 0: s.note(timp, 40 if bar % 4 == 0 else 47, bar, 0, 0.6, 100)
    s.drums(dr, {WOOD_H: [(0, 70), (1, 45), (2, 45)], WOOD_L: [(0, 40)], BELL: []},
            fill={WOOD_H: [(0, 70), (1, 60), (1.5, 60), (2, 70), (2.5, 80)], CRASH: []}, fill_every=8)
    s.drums(dr, {CRASH: [(0, 90)], TRI: [(0, 60)]}, bars=range(0, s.bars, 8))
    return s


def galaxy():
    s = Song('Galaxy', 84, 'Eb', 'lydian', seed=79)
    s.set_prog(['Ebmaj9', 'F/Eb', 'Gm7', 'Cm9', 'Abmaj7', 'Bb/Ab', 'Gm7', 'Cm7', 'Ebmaj9', 'F/Eb', 'Dm7', 'Gm7', 'Abmaj7', 'Bb', 'Ebmaj9', 'F/Eb'])
    pad = s.inst('pad', 89, vol=76, reverb=90, chorus=60)
    halo = s.inst('halo', 94, vol=58, pan=40, reverb=90)
    cry = s.inst('crystal', 98, vol=64, pan=86, reverb=90)
    lead = s.inst('flute', 73, vol=82, pan=56, reverb=80)
    bass = s.inst('bass', 38, vol=82, reverb=30)
    dr = s.inst('drums', 24, drum=True, vol=56, reverb=50)
    s.pad(pad, 60, 60, 4); s.pad(halo, 72, 40, 3)
    s.arp(cry, [0, 1, 2, 3, 4, 3, 2, 1], 0.5, 72, 50, gate=1.2)
    s.bass(bass, [(0, 'R', 1.5), (1.5, '5', 0.5), (2, '8', 1.5), (3.5, '5', 0.5)], 36, 78)
    s.melody(lead, [[(0, 2), (2, 1), (3, 1)], [(0, 1.5), (1.5, 0.5), (2, 2)], [(0, 3), (3, 1)]], 70, 89, vel=80, start=79, form='ABAB')
    s.drums(dr, {KICK: [(0, 80), (2.5, 60)], CLAP: [(2, 50)], SHAKER: [(x / 2, 30 + 8 * (x % 2)) for x in range(8)], CHIME: []},
            fill={KICK: [(0, 80)], CHIME: [(3, 60)]})
    return s


def interior():
    s = Song('Interior', 84, 'Bb', 'major', swing=0.4, seed=97)
    s.set_prog(['Bbmaj7', 'Gm7', 'Cm7', 'F7', 'Dm7', 'G7', 'Cm7', 'F7', 'Ebmaj7', 'Ab7', 'Dm7', 'G7', 'Cm7', 'F7', 'Bbmaj7 G7', 'Cm7 F7'])
    rh = s.inst('rhodes', 4, vol=82, pan=50, reverb=50, chorus=30)
    vib = s.inst('vibes', 11, vol=82, pan=72, reverb=60)
    bass = s.inst('upright', 32, vol=96, reverb=20)
    git = s.inst('nylon', 24, vol=60, pan=30, reverb=45)
    dr = s.inst('drums', 40, drum=True, vol=74, reverb=30)
    s.pad(rh, 60, 58, 4, rhythm=[(0, 1.4), (1.5, 0.4), (2.5, 1.4)])
    s.bass(bass, [(0, 'R', 0.9), (1, '5', 0.9), (2, '8', 0.9), (3, 'ap', 0.9)], 34, 90)
    s.melody(vib, [[(0, 1), (1, 0.5), (1.5, 1.5), (3, 1)], [(0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 3), (3, 0.5), (3.5, 0.5)]], 65, 84, vel=84, start=74, rest_prob=0.12)
    s.arp(git, [-1, 1, 2, -1, 1, 3, -1, 2], 0.5, 60, 48, gate=0.7)
    s.drums(dr, {KICK: [(0, 70), (2.5, 55)], SNARE: [(1, 55), (3, 60)], RIDE: [(0, 55), (1, 50), (1.5, 40), (2, 55), (3, 50), (3.5, 40)], PHAT: [(1, 40), (3, 40)]})
    return s


SONGS = {'Bedroom': bedroom, 'Dream': dream, 'Boss': boss, 'World1': world1, 'Palace': palace, 'Midnight': midnight, 'Galaxy': galaxy, 'Interior': interior}
MIX = {'Bedroom': dict(reverb=0.16, bright=1.0, target_rms=-22.5), 'Galaxy': dict(reverb=0.2, bright=1.0, target_rms=-22.5), 'Interior': dict(reverb=0.1, target_rms=-21.5),
       'Boss': dict(reverb=0.08, target_rms=-18.0), 'Midnight': dict(reverb=0.12, target_rms=-18.5), 'World1': dict(reverb=0.08, target_rms=-18.5), 'Palace': dict(reverb=0.08, target_rms=-19.0)}

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for name, fn in SONGS.items():
        if ONLY and name not in ONLY: continue
        s = fn()
        y = s.master(s.render(), **MIX.get(name, {}))
        d = s.write_loop(os.path.join(OUT, f'MUS_{name}.ogg'), y)
        s.pm.write(os.path.join(OUT, f'_mid_{name}.mid'))
        print(f'{name}: {d:.1f}s loop, {s.bars} bars @ {s.bpm}')
