# Pip — The Pantry Bandit

Original model and motion study for reviewing the supplied Blender workflows. This is an isolated art prototype, separate from Mama Is Coming's characters, house and gameplay.

## Open and play

- Editable Blender source: `Pip_PantryBandit.blend` (Blender 4.5).
- Unity scene: `Assets/Showcases/PantryBandit/Scenes/PantryMotionLab.unity`.
- Unity prefab: `Assets/Showcases/PantryBandit/Prefabs/Pip.prefab`.
- Enter Play: the six actions cycle automatically. Click Game View for keyboard input.

| Input | Preview |
| --- | --- |
| 1–6 | Idle, Walk, Run, Hop, Snatch, Cheer |
| WASD / Left Shift | Move / run on the preview stage |
| Space / E | Hop / reach for the practice cheese |
| Tab / R | Toggle automatic demonstration / reset |
| O / right mouse drag | Orbit camera / change viewing angle |

## Art choices

A readable mouse silhouette: upright round ears, tapered muzzle, two incisors, whiskers and a long continuous tail. Teal vest, coral scarf, cream belly and navy boots provide clear material and colour separation. Cheese has recessed geometric cavities. Milk carton has a folded top and a custom milk badge. All geometry was authored for this study; no downloaded character meshes.

The character is 33,272 triangles, 26 bones and five material slots sharing one 512×512 painted palette. It is a stylized soft-toy study with rounded surfaces, not a minimum-polygon mobile asset. Facial expression is static. Solid body parts use rigid bone weights; the continuous tail blends across bones. The prototype does not include a production facial rig, full-body IK, cloth simulation or multiplayer integration.

Six bone-baked FBX clips reproduce the model's motion in Unity. Walk/run include alternating foot lifts, body bob, arm swing and trailing secondary movement. Hop includes anticipation, airtime and landing. Clips are in-place; the isolated demo controller supplies translation. Movement speed is for inspection, without production foot locking, collision or slope handling.

Snatch is an authored reach/crouch motion. Unity supplies a separate reachable cheese prop and attaches it between the hands during the grasp phase; it returns to the platter afterwards. The Blender Snatch contact sheet shows the skeletal pose study without that Unity prop binding. This is not a persistent gameplay inventory or arbitrary-object pickup system.

## Skills used

Read directly from the two user-supplied folders:

- `blender-skills-main/.claude/skills/`: blender-director, character-artist, rigging, animation, unity-export, asset-pipeline reference.
- `cc-blender-skill-main/plugin/skills/`: blender-modeling, blender-animation, blender-materials, blender-lighting, blender-export, animation-quality-gate, backend references.

Used the provided `animation_contact_sheet.py` helper to generate all five contact sheets and reports. Blender MCP was unavailable, so the assets were built in a new, isolated local Blender background process. No shared interactive Blender file was overwritten.

## Evidence and review

`Preview/` contains the rendered hero, five animated GIFs, a combined reel, five contact sheets with JSON frame-difference reports, Blender validation, and Unity camera captures/input review.

Blender validation: 11/11 structural/export checks, including normalized weights, six actions, no scale keyframes and matching Idle/Walk/Run/Cheer loop endpoints. Render review used 12 samples per action; numerical frame-difference warnings alone are not a visual quality judgment.

Visual review: silhouettes remain readable during steps and airtime; no whole-frame morph, texture swap or material flicker. Initial export had a stale-parent rig evaluation bug that broke loop seams; the final rig solves transforms directly and passes endpoint validation. Initial Unity typography obscured the face; reduced world-space type and closer orthographic framing address that. Blender's soft studio render and Unity's real-time lighting intentionally differ; Unity screenshots are the evidence for actual imported appearance.

Unity input review is scripted device input, not a human playtest. Consult `Preview/Unity/review.txt` for the final check count and measurements. It exercises all six number keys, actual animated bone changes, walk/run displacement, reset, E, Space and runtime errors in this scene only.

## Rebuild

`build_pip.py` runs in Blender background and recreates the original scene, texture and FBX exports. Pass `-- --no-render` to skip renders. `render_pip.py` opens the saved source and renders the hero and sampled action frames into ignored `DevBridge/pantry-showcase/`. Run `package_preview.py` using Python with Pillow to package those renders and contact sheets.

In Unity, use **Showcases → Pantry Bandit → Build Isolated Motion Lab** outside Play mode with the current scene saved. It rebuilds only the isolated showcase scene/assets. Build Settings are not changed.
