import bpy,json,math
from pathlib import Path
P=Path(__file__).resolve().parent;ROOT=P.parents[2];OUT=ROOT/'DevBridge/pantry-showcase'
bpy.ops.wm.open_mainfile(filepath=str(P/'Pip_PantryBandit.blend'),load_ui=False,use_scripts=False)
arm=bpy.data.objects['ARM_Pip'];mesh=bpy.data.objects['GEO-Pip_skinned'];checks=[]
def check(ok,description):
 checks.append({'pass':bool(ok),'description':description})
 if not ok:print('FAIL',description)
check(len(arm.data.bones)==26,'26-bone generic skeleton')
check(sum(len(p.vertices)-2 for p in mesh.data.polygons)<35000,'Character below 35k triangle showcase budget')
check(all(len(v.groups)>0 for v in mesh.data.vertices),'Every vertex has a bone influence')
err=max(abs(sum(g.weight for g in v.groups)-1) for v in mesh.data.vertices)
check(err<.001,'Skin weights normalized; max error %.6f'%err)
loops={}
for k in ['Idle','Walk','Run','Cheer']:
 a=bpy.data.actions['AN_Pip_'+k];arm.animation_data.action=a;bpy.context.scene.frame_set(1)
 initial={p.name:p.matrix.copy() for p in arm.pose.bones};bpy.context.scene.frame_set(round(a.frame_range.y))
 diff=max(abs(initial[p.name][i][j]-p.matrix[i][j]) for p in arm.pose.bones for i in range(4) for j in range(4));loops[k]=diff
 check(diff<.001,k+' loop seam matrices match')
check(len([a for a in bpy.data.actions if a.name.startswith('AN_Pip_')])==6,'Six baked actions exist')
check(not any(f.data_path.endswith('scale') for a in bpy.data.actions if hasattr(a,'fcurves') for f in a.fcurves),'No scale-key animation required by export')
check(all(m.node_tree.nodes.get('Principled BSDF') for m in mesh.data.materials),'All skin materials use exportable Principled BSDF')
report={'checks':checks,'passed':sum(c['pass'] for c in checks),'total':len(checks),'loop_matrix_max_error':loops,'weight_max_error':err,'notes':['Original segmented character, rigid weights on solid parts, blended tail weights.','Clips are in-place. World travel is the isolated Unity demo controller.','No shape-key/handler-only effects are required by exported FBX.']}
(OUT/'blender-validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
