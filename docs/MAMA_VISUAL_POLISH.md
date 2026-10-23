# MAMA'S COMING! — visual and UX polish, 2026-10-23

## Scope and observed problem

The user's active scene is `Assets/MamasComing/Scenes/MamasKitchen.unity`. This is the cooperative party-cleanup game: stop household disasters, clean or conceal their evidence, and survive Mom's inspection. DreamRush's campaign GDD is not this game's design specification. The isolated WitchTest is also a separate scene.

Actual Game View inspection found two independent HUD owners: MamaUI and QuestDirector. The latter kept eight task rows and remote-room markers over gameplay even after the earlier readability pass hid MamaUI's list. Objective, timer, disaster telemetry, context, thought bubbles and events competed with the house. Bright floor-water caustics, saturated wallpaper/cabinets and hard furniture edges compounded the problem.

The authored scene already had uncommitted changes before this work. Its complete working copy was preserved in `DevBridge/visual-polish/before/Assets/MamasComing/Scenes/MamasKitchen.unity`; no scene builder or player-prefab regeneration was used.

## Implemented

- House/task details now open with Tab, with the party board as the single task overview. Holding H temporarily suppresses the board. Remote-room exclamation marks belong to the requested overview instead of remaining on-screen.
- The default HUD has a compact goal, room, rectangular timer and small notes/help reminder. Context is shown only for a held/targeted object or nearby oil fire; generic empty-space instruction cards are hidden. Context/prompt sizes and borders were reduced.
- Title copy is shorter and the title card sits on the left, leaving the house and character visible. Existing wake-up input and settings/co-op access remain.
- Speech, emergency messages and routine confirmations share one notification lane. Higher-priority messages defer lower-priority timers. Removed the call-banner scaling pop and reduced the full-screen alarm tint.
- Original Blender models: hollow rolled-rim cookware body, domed rolled-edge lid, crafted furniture edge and soft cushion. Reproducible source: `ArtSource/Blender/Scripts/build_mama_polish.py`; editable source: `ArtSource/Blender/MamaPolish/MamaCraftedKit.blend`.
- Applied rounded meshes to 73 existing furniture surfaces. Reused current renderer references, so burn/interaction hooks still address the same objects. No collision meshes were replaced.
- Added five authored procedural textures: restrained wood, ceramic tile, wallpaper stripe, fabric and tapered flame. Assigned 1,218 material slots through 308 cloned materials, preserving original materials and references outside the scene. Sage cabinets, cream walls and muted upholstery reserve stronger colour for characters and useful objects.
- Softer warm key light, less saturation/bloom, matte dry surfaces, subtler water normal/reflection and no pool-like caustic detail on floor puddles. Flood simulation/volume thresholds remain the existing rules.
- Flames have bounded emission, controlled shape/rotation and a smaller light footprint. Smoke cards are smaller, less opaque and bounded to 480 particles. Fire-out produces ten short-lived steam particles. None of this changes fire intensity, growth, spread or extinguishing rules.
- Found and fixed a multiplayer presentation bug through actual puppet creation: `Material.color` returned an inappropriate colour for DreamToon, and a renderer-wide override flattened all material slots. Remote players appeared black. The presentation code now reads `_BaseColor` and writes per material slot, preserving eyes/skin/gloves and accenting clothing teal/gold. Shape-coded P2/P3 tags use dark backing for contrast.
- Burnt/wet cloth tinting now excludes particle renderers so cloth effects cannot recolour their own flames.

The existing character model, rig and procedural action animations are retained. This pass does not claim a new character sculpt, retopology or complete animation set.

## Validation and evidence

- Runtime and Editor compiled with **0 errors**. Four existing warnings remain in Hazard, ToolItem, KitchenSink and Mop.
- Final expanded staged-input review: **42/42 passed**, `DevBridge/visual-polish/ux/review-175700.txt`. Two earlier 37-check runs also passed and remain available. Enter/E/Q/H/Tab input covers wake-up, optional overview/hints, teddy pickup/home placement and stability, a real lid extinguish interaction, Thai/English switching, context fit, notification overlap, remote material preservation, clothing accents, shape-coded identity and flame-tint isolation.
- Puppet tests used the real `NetSession.SpawnPuppet` path with three avatars and `NetGame.Online == false`. No host, client, Relay session or external service was opened. This is a presentation test, not network synchronization validation.
- Captured and visually inspected all nine rooms from staged gameplay-camera positions at 1280×720; contact sheet: `DevBridge/visual-polish/room-contact-sheet.jpg`. Also inspected 1920×1080 title, gameplay, held-item/home, fire and three-avatar captures. Room teleports do not establish continuous traversal.
- The authoring pass compared serialized collider settings/types plus world positions/scales before and after: unchanged. Raw proof and counts: `DevBridge/visual-polish/authoring.txt`.
- Imported mesh bounds: pot approximately 0.43×0.27×0.43 m; lid 0.45×0.06×0.45 m. Pot 1,920 triangles, lid 1,664; each furniture mesh 300 triangles. Source inventory: `ArtSource/Blender/MamaPolish/manifest.json`.
- An initial `MaterialPropertyBlock.isEmpty` probe was inconclusive because renderer blocks can contain unrelated properties; the final check examines the actual colour override instead. Earlier black-avatar captures are retained as failure evidence, not final results.
- The unrelated IvanMurzak connector continues reporting an existing authorization rejection. The focused review excludes that exact message; gameplay exceptions still fail the review. No connector credentials or settings were changed.
- After adding the tag backing, Runtime/Editor compiled again with zero errors and a targeted offline visual smoke check confirmed two backed P2/P3 tags (`tag-contrast-check.txt`, `after-three-avatars.png`). The 42-case run precedes that backing-only change. Source diff whitespace checks pass; Unity's serialized scene still emits empty `m_Name: ` fields flagged as trailing spaces.

Selected durable screenshots and the final test log are copied under `docs/evidence/mama-visual-polish/`. Full local originals, pre-edit files, captures and intermediate results remain under `DevBridge/visual-polish/`.

## Art review and remaining limits

Verdict: **REVISE for final-release art approval**. This is an implemented presentation pass with in-engine evidence. The house is quieter, tools/disasters read more clearly and the repeated checklist is removed from normal play. Remaining final-art work includes more bespoke silhouettes for secondary household props and reconciling the heavily inked character with the softer environment.

No uncoached new-player session, human cooperative playthrough, standalone build or target-hardware/GPU profiling was performed. Frame-rate or subjective-fun improvements are not claimed. Existing gameplay timing, alternative solutions, cleaning versus concealment, progression and network transport remain outside this presentation change.
