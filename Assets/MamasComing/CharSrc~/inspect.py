
import bpy, sys
s = bpy.context.scene
print('SCENE', s.name, 'frames', s.frame_start, s.frame_end, 'fps', s.render.fps, 'unit', s.unit_settings.scale_length)
for o in bpy.data.objects:
    ad = o.animation_data
    act = ad.action.name if ad and ad.action else None
    print('OBJ', o.name, o.type, 'parent=', o.parent.name if o.parent else None, 'ptype=', o.parent_type, 'bone=', o.parent_bone, 'act=', act, 'loc=', tuple(round(v,3) for v in o.location), 'dim=', tuple(round(v,3) for v in o.dimensions), 'mods=', [m.type for m in o.modifiers])
    if o.type == 'ARMATURE':
        for b in o.data.bones: print('   BONE', b.name, 'parent=', b.parent.name if b.parent else None)
for a in bpy.data.actions: print('ACTION', a.name, a.frame_range[:])
