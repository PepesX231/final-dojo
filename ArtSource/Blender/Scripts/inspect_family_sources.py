import bpy, json, os, sys
files=sys.argv[sys.argv.index('--')+1:]
report=[]
for path in files:
    bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
    report.append({'file':path,'version':list(bpy.data.version),'objects':[{'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'vertices':len(o.data.vertices) if o.type=='MESH' else None,'dimensions':list(o.dimensions),'materials':[m.name for m in o.data.materials if m] if o.type=='MESH' else [],'groups':[g.name for g in o.vertex_groups] if o.type=='MESH' else []} for o in bpy.data.objects if o.type in ('MESH','ARMATURE','EMPTY')], 'rigs':[{'name':r.name,'bones':[b.name for b in r.data.bones]} for r in rigs],'actions':[{'name':a.name,'range':list(a.frame_range)} for a in bpy.data.actions]})
root=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
out=os.path.join(root,'DevBridge','mama-rebuild');os.makedirs(out,exist_ok=True)
with open(os.path.join(out,'family-source-inspection.json'),'w',encoding='utf8') as f:json.dump(report,f,ensure_ascii=False,indent=2)
print('SOURCE_INSPECTION '+json.dumps([{'file':x['file'],'objects':len(x['objects']),'rigs':x['rigs'],'actions':x['actions']} for x in report],ensure_ascii=False))
