---
name: blender-sculpt-modeling
description: Model MAMA'S COMING characters and props in Blender like sculpting clay (metaball mass, voxel remesh, brush strokes), never by stacking primitives. Use for any new or revised character, creature or hero prop mesh, and before judging a model "done".
---

# Blender sculpt modelling (clay workflow)

The user's rule: a good model is *pushed and pulled like clay*, not boxes and spheres glued together.
Primitives may only be the first lump of clay; every visible form must come out of sculpt strokes.

## Tools (verified 2026-10-24)
- Blender 4.5 lives at `.tools/blender/blender-4.5.0-windows-x64/blender.exe` (gitignored). Run headless:
  `blender -b -P <script.py> -- <args>`. Blender's Python has numpy.
- Library: `ArtSource/Blender/Scripts/sculpt/clay.py` (sculpting), `rig.py` (fruit-kid rig + IK posed actions).
  Examples: `kids.py` (five fruit kids), `props.py` (whimsical props), `comic_fx_textures.py` (VFX sprites).
- No Blender MCP/plugin was available in the plugin catalog; everything is scripted and reproducible.

## Workflow
1. **Mass (Field)** — rough the volume with metaballs: `ball`, `blob` (ellipsoid), `rope`/`chain` (rolled snakes
   of clay for limbs, stems, handles). Radii are the *visible* radius. Close blobs melt together (smooth union);
   `neg=True` scoops clay out. One Field = one piece of clay; separate Fields never melt.
2. **Remesh** — `Field.solid(voxel)` gives even topology (voxel .007–.012 m for a 1 m kid).
3. **Sculpt** with `Sculpt` brushes, from big to small:
   - `grab(c, r, delta)` move a region; `inflate(c, r, amt)` swell (cheeks, brows, lips);
   - `crease(c, r, depth)` press a fine tool in (sockets, mouths, seams) — adds to `carve`;
   - `flatten(c, r, normal)` palette-knife planes (banana facets, a sit-able top, a sole);
   - `stroke(points, r, brush, amount)` drag any brush along a curve;
   - `smooth` (Taubin, no shrink), `smooth_region`;
   - hand-made feel last: `lumps`, `thumbprints`, `noise` (peel pores).
   `S.surface(origin, dir)` finds where a ray meets the clay — place features on the surface, not in space.
4. **Paint** — `paint(ob, materials, chooser(centres, normals))` assigns material slots per face (flat toon colours,
   cel shading comes from the ToyToon shader). Paint *before* `decimate` so boundaries survive.
5. **Budget** — `decimate(ob, tris)`: kid ≤ 15k tris total, prop ≤ 3–5k.
6. **Review renders** — Workbench with cavity + outline exposes lumpy/dented clay. Look at front, 3/4, side, back.
   Typical fixes: arms fused into the body (move shoulders out), pose left on the last keyed frame (reset
   `matrix_basis` before rendering), glints/pupils floating off the eye (offset toward the camera, not the normal).

## Style (2026-10-24 user direction)
Bright, kid-friendly comic toon: saturated fruit colours, chunky readable silhouettes, big expressive eyes with
lids for attitude, white cartoon gloves, colourful sneakers, ink-outline friendly forms (no thin spikes).

## Export
FBX: `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False,
primary_bone_axis='Y', secondary_bone_axis='X'`, `bake_anim_use_all_actions=True` for multiple clips
(takes arrive as `<Rig>|<Action>`). Character faces Blender -Y; its right side is -X. Keep the `.blend` in
`ArtSource/Blender/Sculpted/`. Unity side: `Mama's Coming/Sculpted Kids/*` menus (MamaSculptKids.cs).
