# MAMA’S COMING! — Recast implementation

2026-10-23. Implements the updated saturated cartoon brief in `MAMA_ART_RECAST_RESEARCH.md`. This is a production pass under review, not final release art approval.

## Authored assets

20 original FBX prop models, **46,912 triangles total across the unique library**, each using one shared palette atlas material. This total is not the runtime scene triangle count. Models include plush teddy, hamster, goldfish, tied trash bag, hollow bin and separate hinged lid, cushion, cake, pizza box, bicycle, pump, two pipes, router, breaker panel, vase, toolbox, bucket, extinguisher and folded towel.

- Editable source: `ArtSource/Blender/MamaRecast/MamaRecast_PropLibrary.blend`.
- Reproducible generator: `ArtSource/Blender/Scripts/build_mama_recast.py`.
- Exact dimensions/triangle/material counts: `ArtSource/Blender/MamaRecast/manifest.json`.
- Runtime models/materials/atlas: `Assets/MamasComing/Art/Recast/`.
- Blender contact sheet: `DevBridge/mama-rebuild/prop-contact-sheet.jpg`; studio asset renders, **not an in-game screenshot**.

Mother Apple Kind/Angry uses the user's supplied FBXs, normalized to 1.8m, retaining skinned meshes and original facial geometry. Original Blender 5.2 files are preserved in `ArtSource/Blender/ProvidedFamily/`; all runtime work is derived. Blender installed locally is 4.5, so the 5.2 originals have not been overwritten.

Banana is imported as a derived FBX with rigid transform animation, but its Blender-only geometry deformation is frozen in that conversion. **It does not replace the playable child yet.** It needs a complete skeletal/geometry bake and hand/carry integration. The imported family meshes also need material-slot and triangle optimization before release.

## Scene and interactions

`MamaRecastBuilder.Apply` replaces visual children in the authored MamasKitchen scene, preserving gameplay objects and the existing authored house. It does not regenerate the level or shared player prefab. Old visual renderers are retained disabled. Visual-only home ghosts skip disabled originals.

Explicit collision exceptions: teddy is upright with a matching box; original full-solid bin colliders are disabled and replaced by four thin wall boxes. The new bin has an actual interior and lid pivot. Carry a trash bag, press E to open, then E to deposit; capacity is three. The bag is released, settled at the bin's destination, and no longer counted as displaced clutter. The inherited Cabinet state serializer includes the bin's primitive fields; actual online disposal still needs a two-machine check.

CarryGrip profiles give teddy/hamster/fish/bag/box separate contact points and hold anchors. The first third of the .6s grab retains the item's world pose, followed by the lift. A rigid-limb analytic solver places the elbow/wrist against those targets. An initial review exposed ~.147m carry wrist error on teddy. Moving the hold closer made the numeric grip pass but a staged closeup exposed torso penetration. The revision keeps the object in front of the torso and rolls the shoulders forward/inward into a cradle pose; the test now checks torso clearance separately from hand contact. Passing these checks is not a claim that all reach/turn/walk transitions have received animator approval.

Mother switches between the two supplied expressions and has an initial procedural limb walk layer. MotherApplePresentation is included in the existing world field synchronization registry for expression consistency. The original inspection route and rules remain in place.

The expanded review also exposed a pre-existing live-clone blink bug: cloning while AppleFace had eyelid materials active made a remote avatar cache the shut-eye set as its neutral face. SpawnPuppet now restores the source's cached open-eye set immediately before cloning; the review deliberately squeezes the source eyes shut before spawning friends. This small AppleFace API change is the only shared DreamRush runtime edit in this pass, and its original file is backed up with the Mama changes.

## Look and audio

Teal cabinets/walls, coral sofa and golden cushions replace the rejected muted palette. Color grading is Neutral tonemapping with restrained bloom, saturation +10, contrast +12 and exposure -.08, with warm key lighting. These are project choices, not reference-game settings.

Fire particles now use a warm light core → orange → red lifetime gradient with fading edges and bounded particle counts; existing simulation controls size/intensity. Fire-out steam and shape-coded friend tags remain. This is a refinement of the existing effect set, not a completed bespoke VFX library for every interaction.

Ten CC0 material foley clips override corresponding synthesized clips, with source names and licenses documented in `MAMA_RECAST_AUDIO_CREDITS.md`. Procedural music is revised to a sparse 108 bpm minor/swing arrangement, and ducks under important callouts. `music-preview.wav` is an export for listening review; peak/RMS measurements alone do not certify the mix.

## Validation and evidence

Focused staged virtual-keyboard tests and art diagnostics are in `DevBridge/mama-rebuild/ux/`. This review covers title/HUD, actual E/Q interactions, hold contacts, bin disposal, two mother expressions, foley imports, fire interactions and three offline avatars. Early failures and aborted editor sessions are retained alongside later results. Normal screenshots and explicitly staged closeups are separate.

Durable final evidence is copied to `docs/evidence/mama-recast/`: `review-final.txt`, `after-play.png`, `recast-bin-disposal.png`, `three-avatars.png`, studio `prop-contact-sheet.jpg`, and staged mother/carry views. `staged-hold-hamster.png` was taken in the open lounge after the completed input review because its original capture camera was occluded by a wall; it is an art inspection, not another E-input test.

Validation result: **63/63 passed** in the final staged-input run, after the carry-clearance and blink-clone fixes. Runtime/Editor compilation has zero errors and four existing warnings. Source-code whitespace checks pass; Unity's serialized scene still contains its normal empty-value trailing spaces. The earlier 42-check pass is retained as earlier evidence, not substituted for this run.

Final carry measurements: teddy left/right wrist target error 0.000m/0.000m, hamster 0.000m/0.000m in the recorded sample; torso/held world-bounds clearance is recorded separately in the review. These are staged sample measurements, not a guarantee across every pose. Exported music: peak .264, RMS .059, 17.777s, no non-finite samples. All ten foley clips load. The review does not count intentionally staged art camera views as player-input tests.

No human cooperative playtest, actual online session, standalone build or target-hardware performance certification has been performed for this pass. Full character rig completion, unique painted texture work across all furniture, a final VFX/sound mix and final art approval remain open.

## Preservation and editor recovery

Pre-pass script/editor/scene copies: `DevBridge/mama-rebuild/before/`. The editor later hung during assembly reload, with an external MCP authorization failure logged. The last saved scene was copied again before restarting the exact project editor. Its temporary recovery scene files were preserved under `before/RecoveredSceneFiles/` rather than deleted. No original supplied character source was overwritten.
