"""Package final rendered evidence; run after render_pip.py completes."""
from pathlib import Path
import json, shutil, subprocess, sys
from PIL import Image, ImageDraw

P=Path(__file__).resolve().parent
root=P.parents[2]
qa=root/'DevBridge/pantry-showcase'
out=P/'Preview'
out.mkdir(exist_ok=True)
helper=Path('C:/Users/User/OneDrive/cc-blender-skill-main/cc-blender-skill-main/plugin/skills/animation-quality-gate/scripts/animation_contact_sheet.py')
shutil.copy2(qa/'hero.png',out/'Pip_Hero.png')
shutil.copy2(qa/'blender-validation.json',out/'blender-validation.json')
overview=[]
for kind,ms in [('Walk',83),('Run',61),('Hop',100),('Snatch',133),('Cheer',167)]:
    frames=sorted((qa/'frames').glob(kind+'_*.png'))
    assert len(frames)==12,(kind,len(frames))
    subprocess.run([sys.executable,str(helper),'--frames',*[str(f) for f in frames],'--out-image',str(out/(kind+'_ContactSheet.jpg')),'--out-report',str(out/(kind+'_ContactSheet.json')),'--cols','4'],check=True)
    imgs=[Image.open(f).convert('RGB').resize((560,560),Image.Resampling.LANCZOS) for f in frames]
    imgs[0].save(out/(kind+'.gif'),save_all=True,append_images=imgs[1:],duration=ms,loop=0,optimize=False)
    overview.extend(imgs)
overview[0].save(out/'Pip_MotionReel.gif',save_all=True,append_images=overview[1:],duration=[83]*12+[61]*12+[100]*12+[133]*12+[167]*12,loop=0,optimize=False)
if (qa/'Unity/review.txt').exists():
    unity=out/'Unity';unity.mkdir(exist_ok=True)
    for f in (qa/'Unity').iterdir():
        if f.is_file():shutil.copy2(f,unity/f.name)
print('PREVIEW_PACKAGE_READY',out)
