"""Original five-world score. Render via the existing dr_music toolkit and licensed GeneralUser GS.
CLI: python make_campaign_music.py --soundfont PATH --dll-dir PATH --out AUDIO_DIR --midi MIDI_DIR
Dependencies: numpy, pretty_midi, pyfluidsynth + native FluidSynth, soundfile, pedalboard.
No Minecraft melody, recording or poem is included.
"""
from pathlib import Path
import argparse,os,sys,json
p=argparse.ArgumentParser();p.add_argument('--soundfont',required=True);p.add_argument('--dll-dir');p.add_argument('--deps');p.add_argument('--toolkit');p.add_argument('--out',required=True);p.add_argument('--midi',required=True);a=p.parse_args()
if a.deps:sys.path.insert(0,a.deps)
if a.toolkit:sys.path.insert(0,a.toolkit)
if a.dll_dir:
 os.environ['PATH']=str(Path(a.dll_dir).resolve())+os.pathsep+os.environ.get('PATH','')
 _dll_handle=os.add_dll_directory(str(Path(a.dll_dir).resolve())) if hasattr(os,'add_dll_directory') else None
import numpy as np
import soundfile as sf
import dr_music
from dr_music import Song
dr_music.SF2=str(Path(a.soundfont).resolve())
out=Path(a.out);midi=Path(a.midi);out.mkdir(parents=True,exist_ok=True);midi.mkdir(parents=True,exist_ok=True)
RHYTHMS=[[(0,1),(1,.5),(1.5,.5),(2,1.5),(3.5,.5)],[(0,.5),(.5,1),(1.5,.5),(2,1),(3,1)],[(0,2),(2,.5),(2.5,.5),(3,1)],[(0,1.5),(1.5,.5),(2,2)]]
def moon():
 s=Song('ToyBoxMoon',84,'D','lydian',seed=231)
 s.set_prog(['Dmaj9','E6','F#m7','C#m7','Dmaj7','Bm7','E7sus4','E7','F#m7','Dmaj7','Eadd9','C#m7','Bm7','E7','Dmaj9','A7sus4'],repeat=2)
 lead=s.inst('celesta',8,vol=92,pan=70,reverb=65);harp=s.inst('harp',46,vol=72,pan=44,reverb=65);strings=s.inst('warmstrings',49,vol=58,reverb=65);bass=s.inst('upright',32,vol=68,pan=55,reverb=30)
 s.pad(strings,62,48,4);s.arp(harp,[0,2,1,3,2,1],.5,70,57,gate=1.3);s.bass(bass,[(0,'R',2),(2,'5',1.8)],36,64)
 s.melody(lead,RHYTHMS,72,91,vel=77,start=78,form='AABA',bars_per_phrase=4,rest_prob=.12)
 bells=s.inst('glockenspiel',9,vol=54,pan=92,reverb=70);s.arp(bells,[3,-1,-1,-1,2,-1,-1,-1],.5,84,42,gate=.6)
 return s,.19,-19

def sea():
 s=Song('BubbleBathSea',108,'G','major',swing=.22,seed=241)
 s.set_prog(['Gmaj7','Em7','Cmaj9','D7sus4 D7','Gmaj7','Bm7','Am7','D7','Em7','Bm7','Cmaj7','Am7 D7','Gmaj7','Cmaj7','G/D','D7'],repeat=2)
 lead=s.inst('clarinet',71,vol=91,pan=78,reverb=40);guitar=s.inst('nylon',24,vol=83,pan=36,reverb=35);strings=s.inst('strings',48,vol=51,reverb=55);bass=s.inst('acousticbass',32,vol=89,pan=58,reverb=20);dr=s.inst('brusheddrums',0,drum=True,vol=65,reverb=25)
 s.pad(strings,63,45,3);s.arp(guitar,[0,2,1,2,3,2,1,2],.5,63,68,gate=.55);s.bass(bass,[(0,'R',.8),(1.5,'5',.45),(2,'8',.8),(3.5,'5',.45)],35,83)
 s.melody(lead,RHYTHMS,67,86,vel=88,start=74,form='AABA',bars_per_phrase=4,rest_prob=.08)
 s.drums(dr,{36:[(0,70),(2,60)],37:[(1,57),(3,62)],42:[(.5,32),(1.5,30),(2.5,37),(3.5,35)]},fill_every=8,fill={38:[(2.5,48),(3,58),(3.5,66)],49:[(3.5,42)]})
 return s,.12,-17.5

def tea():
 s=Song('MidnightTeacup',92,'A','minor',beats=3,swing=.12,seed=251)
 s.set_prog(['Am9','Fmaj7','Dm7','E7','Am7','Cmaj7','Dm7','E7','Fmaj7','C/E','Dm7','G7','Cmaj7','Fmaj7','Bm7b5','E7'],repeat=2)
 piano=s.inst('feltpiano',0,vol=84,pan=58,reverb=55);vibes=s.inst('vibraphone',11,vol=70,pan=82,reverb=65);cello=s.inst('cello',42,vol=71,pan=38,reverb=50);bass=s.inst('bass',32,vol=68,reverb=20)
 s.pad(cello,55,42,3);s.arp(piano,[0,1,2,3,2,1],.5,64,61,gate=1.25);s.bass(bass,[(0,'R',1.5),(2,'5',.7)],36,69)
 r=[[(0,1),(1,.5),(1.5,.5),(2,1)],[(0,1.5),(1.5,.5),(2,1)],[(0,2),(2,1)]]
 s.melody(vibes,r,70,88,vel=69,start=76,form='AABA',bars_per_phrase=4,rest_prob=.15)
 return s,.2,-19

def finale():
 s=Song('DreamFinale',112,'E','harmonic',seed=261)
 s.set_prog(['Em','Em','Cmaj7','Am7 B7','Em','G','Am7','B7','Cmaj7','G/B','Am9','B7','Em','Cmaj7','Am7 B7','Em B7'],repeat=2)
 st=s.inst('ostinato',48,vol=94,pan=38,reverb=45);horn=s.inst('horns',60,vol=88,pan=72,reverb=50);choir=s.inst('choir',52,vol=52,reverb=70);bass=s.inst('contrabass',43,vol=91,reverb=35);dr=s.inst('orchestraldrums',0,drum=True,vol=81,reverb=40)
 s.arp(st,[0,1,2,1,0,2,3,2],.5,60,79,gate=.62,accent=11);s.pad(choir,64,43,4);s.bass(bass,[(0,'R',.9),(1.5,'R',.45),(2,'5',.9),(3.5,'R',.45)],32,89)
 s.melody(horn,RHYTHMS,59,79,vel=91,start=67,form='AABA',bars_per_phrase=4,rest_prob=.1)
 s.drums(dr,{36:[(0,95),(2,82)],38:[(1,67),(3,78)],51:[(.5,42),(1.5,37),(2.5,44),(3.5,40)],41:[(2.5,46)]},fill_every=8,fill={47:[(2,72)],45:[(2.5,75)],43:[(3,83)],41:[(3.5,88)],49:[(3.5,74)]})
 return s,.15,-16.5

def credits():
 s=Song('Credits',70,'F','major',seed=271)
 s.set_prog(['Fmaj9','Dm7','Bbmaj7','C7sus4 C7','F/A','Bbmaj7','Gm7','C7','Dm7','Am7','Bbmaj9','F/C','Gm7','Bb/C','Fmaj9','C7sus4'],repeat=2)
 piano=s.inst('piano',0,vol=80,pan=57,reverb=72);celesta=s.inst('celesta',8,vol=55,pan=82,reverb=78);pad=s.inst('strings',49,vol=51,reverb=80);harp=s.inst('harp',46,vol=56,pan=35,reverb=65);bass=s.inst('bass',32,vol=61,reverb=35)
 s.pad(pad,60,41,4);s.arp(harp,[0,1,2,3,2,1,-1,-1],.5,62,48,gate=1.7);s.bass(bass,[(0,'R',3.8)],35,60)
 s.melody(piano,RHYTHMS,64,85,vel=73,start=77,form='AABA',bars_per_phrase=4,rest_prob=.14)
 s.arp(celesta,[-1,-1,2,-1,-1,-1,3,-1],.5,83,38,gate=.9)
 return s,.25,-20

metrics=[]
for make in [moon,sea,tea,finale,credits]:
 song,reverb,rms_target=make();print('Rendering',song.name,flush=True)
 song.pm.write(str(midi/('MUS_'+song.name+'.mid')))
 y=song.master(song.render(tail=5),reverb=reverb,bright=.5,target_rms=rms_target)
 n=int(round(song.length()*dr_music.SR));body=y[:n].copy();tail=y[n:];fold=min(len(tail),n);body[:fold]+=tail[:fold]
 peak=float(np.max(np.abs(body)));rms=float(np.sqrt(np.mean(body*body)));body*=min(10**(rms_target/20)/max(rms,1e-9),.88/max(peak,1e-9))
 fade=350;body[:fade]*=np.linspace(0,1,fade)[:,None];body[-fade:]*=np.linspace(1,0,fade)[:,None]
 path=out/('MUS_'+song.name+'.wav')
 import wave
 with wave.open(str(path),'wb') as wav:
  wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(dr_music.SR);wav.writeframes((np.clip(body,-1,1)*32767).astype('<i2').tobytes())
 metrics.append({'track':song.name,'seconds':len(body)/dr_music.SR,'bpm':song.bpm,'peak_db':float(20*np.log10(np.max(np.abs(body))+1e-9)),'rms_db':float(20*np.log10(np.sqrt(np.mean(body*body))+1e-9)),'clipped_samples':int((np.abs(body)>=1).sum()),'file':path.name})
 print(metrics[-1],flush=True)
(out/'campaign-music-metrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf8')
