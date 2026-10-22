---
name: blender-asset-pipeline
description: Create or modify DREAM RUSH stylized 3D assets in Blender (bpy scripts) and bring them into Unity — modeling, scale, pivots, UVs, materials, rigid-part rigs, animation, FBX export and Unity import validation. Use for any .blend work or model import.
---

# Blender Asset Pipeline — DREAM RUSH

## Environment facts (verified 2026-10-16)
- The user's PC has **no Blender install**. Blender 5.2.2 runs headless via the `bpy` Python module in the cloud workspace. Scripts must be reproducible `.py` files kept in `ArtSource/Blender/Scripts/`.
- Never name a script after a stdlib module (e.g. `inspect.py` breaks `import bpy`). Run with `python3 -I script.py`.
- Source `.blend` files live in `ArtSource/Blender/<Category>/`; exported FBX goes to `Assets/DreamRush/Art/<Category>/`.

## Conventions
- 1 Blender unit = 1 m = 1 Unity unit. Player height 1.2.
- Character front = **-Y** in Blender, Z up. Export FBX with `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True` → Unity front = +Z, identity rotations. **Verified 2026-10-16: this only works for direct children of the root** (nested children import with Z-up local frames; `bakeAxisConversion` did not fix it). Keep the hierarchy FLAT and do attachment in code. Always run `DreamRush/Validate/...` after import.
- Pivot: characters at feet centre; props at their natural base/hinge.
- Apply transforms before export; recalc normals outside; no n-gons on deforming parts.
- Names per GDD §6.3 (`CHR_`, `BOSS_`, `PROP_`, `ENV_`, `DEC_`, `FX_`); child parts named for code lookup (e.g. `Leg_L`, `Arm_R`, `Hand_R`, `Leaf`, `Stem`).
- Materials: simple Principled BSDF with base colour from palette; procedural nodes are not portable — bake or rebuild as URP materials in Unity.

## Procedure (GDD §6.2, condensed)
1. Purpose + silhouette target (sketch/reference).
2. Script block-out → render front/side/back silhouettes → art-direction-guardian review.
3. Refine; set origins; apply transforms; UV if textured.
4. Rig: P0 uses **rigid-part hierarchy** (each part parented to a pivot empty/bone) + procedural animation in Unity; skinned armature only if a part must bend.
5. Export FBX (+ keep .blend). Version file names when topology changes (`_v2`).
6. Unity: import into a clean test scene, check scale (bounds height), forward axis, normals, materials, hierarchy names.
7. Test under game lighting at gameplay camera distance. Approve or revise.

## Quality checks
- [ ] Bounds height matches spec (±2%)
- [ ] Origin at expected point; forward axis verified in Unity
- [ ] No flipped normals (render with backface culling)
- [ ] Triangle budget: character ≤ 15k tris, prop ≤ 3k, boss ≤ 10k (P0 guide)
- [ ] Part names match what code expects
- [ ] Source .blend + script saved

## Output
Source file path, export path, render/screenshot evidence, measured bounds/tri count, Unity validation result.
