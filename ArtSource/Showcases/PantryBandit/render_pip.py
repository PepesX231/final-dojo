import bpy
from pathlib import Path
P=Path(__file__).resolve().parent;ROOT=P.parents[2];OUT=ROOT/'DevBridge/pantry-showcase'
bpy.ops.wm.open_mainfile(filepath=str(P/'Pip_PantryBandit.blend'),load_ui=False,use_scripts=False)
sc=bpy.context.scene;arm=bpy.data.objects['ARM_Pip'];arm.animation_data.action=bpy.data.actions['AN_Pip_Idle'];sc.frame_set(1)
sc.render.filepath=str(OUT/'hero.png');bpy.ops.render.render(write_still=True)
sc.render.resolution_x=700;sc.render.resolution_y=700;sc.cycles.samples=12
for kind in ['Walk','Run','Hop','Snatch','Cheer']:
 action=bpy.data.actions['AN_Pip_'+kind];arm.animation_data.action=action;end=int(action.frame_range.y)
 for i in range(12):
  sc.frame_set(1+round((end-1)*i/12));sc.render.filepath=str(OUT/'frames'/('%s_%02d.png'%(kind,i)));bpy.ops.render.render(write_still=True)
 print('RENDERED',kind,flush=True)
print('FINAL_RENDERS_COMPLETE',flush=True)
