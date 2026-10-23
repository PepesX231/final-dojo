# Apple & the Witch — implementation and evidence

Isolated scene: `Assets/MamasComing/WitchTest/Scenes/AppleWitch_Test.unity`.
Open in Unity and press Play, then Enter or the peach start button.

## Play loop

WASD walks on the counter, mouse aims, Space jumps. Aim at clutter and left-click to levitate. Wheel changes distance, Q/E rotates, left-click releases. A release in the matching physical receiver settles the object; otherwise it drops where it is and can be lifted again. Right-click/X deliberately drops without accepting placement. H/Tab held opens the illustrated spellbook; Escape pauses; R restarts after the ending.

Three tasks, any order: cup to saucer, grimoire to book stack, potion bottle to rack. Carry objects over obstructions, then lower them. Range is limited; the grimoire slows walking more and needs a wider clear path. The apple loses eight seconds if it falls off the counter and respawns. Fallen props return to their starting location. The human witch mother arrives at success or after 210 seconds.

## Art that is actually in the scene

- Ten authored Blender models: carved vine-and-apple wand, hollow handled cup, bottle, closed grimoire, saucer, rack, book stack, illustrated open book, detailed cabinet/window/shelf/stool/plant environment, and stylized human mother.
- Editable source: `ArtSource/Blender/MamaWitch/WitchKitchen_Kit_v1.blend`. Reproducible authoring/export script: `ArtSource/Blender/Scripts/mama_witch_slice.py`. Triangle inventory: `ArtSource/Blender/MamaWitch/model-manifest.json`.
- Generated PNG UI panels, buttons, mouse/scroll icons and reticle are live game assets. Live text retains readable Thai and changing state. Original PNG and generation specifications: `docs/MAMA_WITCH_GENERATED_ASSETS.md`.
- Painted wood/sage/ivory/plum albedo texture used by actual materials, generated illustrated spellbook used on a mesh and in optional notes, warm/cool lights, soft shadows, restrained bloom, ACES and contrast. Wand-tip light and an animated ribbon connect the wand to the held object, with raised-wand pose and short synthesized audio feedback.
- Units are metres: 0.94m countertop; 0.12m apple controller at unit root scale; 0.10m cup; 0.205m bottle; about 0.10m wand. Character art is scaled under the motor root so inherited ground probes use correct units.

The earlier kitchen concept PNG is not a gameplay screenshot and is not used as a fake scene background. Actual Game View captures are under `DevBridge/witch-test/`.

## Validation and limits

The staged input/physics review is `WitchPlayReview.Run`. It uses virtual keyboard/mouse events for start, hints, pause, target, lift, wheel and release; stages the player's position to isolate each interaction. It is not a continuous human playthrough. A solid test blocker verifies swept held-volume movement; wrong receivers must reject objects. Input focus settings and virtual devices are restored afterward. Specific pre-existing external connector authorization errors are excluded from game-error assertions; game errors are failures.

Review found and fixed: atlas crop bleeding into neighbouring artwork, stretched sliced button edges, distance increasing with zero wheel input, small-character controller step offset/ground-probe scale mismatch, and upward camera orbit colliding with the counter and zooming into the apple. An initial low carry route correctly hit the bottle rack; the review was changed to lift over obstacles before lowering, preserving that gameplay rule.

Final staged review: **29/29 passed**, including the repeat after the arrival framing correction. See `DevBridge/witch-test/review-latest.txt` and the Game View captures listed below.

Scope/limits: one playable counter-top prototype, not several maps or a release. No human fun/usability sign-off, gamepad certification, standalone build or GPU/performance certification. Mother has a simple entrance rather than a final rigged cinematic. No new save/progression system. Thai prototype font currently uses OS fallback. Full-game original household UX changes are documented separately in `MAMA_UX_REVIEW.md`.

Portable official Blender 4.5 lives under ignored `.tools/`; source and FBX exports are retained without requiring the portable executable in Git. Existing unrelated project changes were preserved.

## Why the generated concept looks more finished

A concept generator invents detail, composition, soft light and surface variation together in one still. A game needs those decisions encoded in geometry, UVs, materials, lighting, camera and feedback under changing viewpoints. Adding bloom or increasing triangle count alone does not supply that work. This slice therefore changes modeled silhouettes/bevels, material palette and texture, light/shadow balance, camera placement and raster UI together. Polygon budgets are per silhouette/function, not an instruction to maximize every mesh. Further art work should unify the apple's outlined toon shading with the environment, improve wall/window dressing and mother animation, then check performance in a standalone build.

## Final implementation review — 23 October 2026

- Runtime and Editor scripts compile with zero errors. Two existing warnings in `KitchenSink.cs` and `Mop.cs` remain outside this slice.
- The final staged keyboard/mouse/physics review passed **29/29 after the arrival-camera correction**; evidence is `DevBridge/witch-test/review-latest.txt`. Earlier 9/15/21-check failures were retained/described rather than counted as passes. They exposed real wheel/camera/range issues and test staging above books. One rerun was started before the Play transition finished and its initial state guard failed with zero checks; the scene was rearmed at its title before the passing repeat. The runner waits in game frames, with unscaled waits during pause.
- Game View screenshots, not concept renders: `title-reviewed.png`, `play-reviewed.png`, `notes-reviewed.png`, `levitation-reviewed.png`, `placing-cup.png`, `placing-book.png`, `placing-bottle.png`, `mother-arrival-reviewed.png`, `success-reviewed.png`, `timeout-reviewed.png` in `DevBridge/witch-test/`.
- Final presentation corrections: reduce the tiny wand light to match centimetre-scale distances, close two open room sides, turn mother toward the apple and stage the arrival at the front counter lip so the window sill does not hide her. This short ending shot repositions the apple after active play has ended.
- UI artwork currently displayed: cream/navy cards, peach button, mouse-left badge and reticle. Dark button, scroll badge and wand emblem are also imported as reusable sprites; not every atlas cell is displayed by this prototype.
- Editor required restarts after repeated domain-reload stalls. An unrelated installed MCP connector also reported authorization failures. Those observations do not establish a definitive root cause. No token/account configuration was modified and no plugin was removed. Domain reload was temporarily disabled for the review; original Play Mode options are restored after testing.
- Visual verdict: usable art/interaction prototype; **REVISE for full release art**. Actual modeled props and generated texture/UI are present, but the apple/environment shading should be unified, wall/window dressing and mother animation need further art work, and a human playtest is still needed for fun/usability. This is not a claim to match the concept illustration in every view.
