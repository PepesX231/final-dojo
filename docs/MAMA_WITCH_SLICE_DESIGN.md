# Apple & the Witch — isolated playable art / interaction test

User direction, 23 October 2026: real human-scale room, tiny apple, beautiful wand, centre-screen aiming, click to levitate, scroll to change distance, manual placement. One wand and one rule set. Original Mama scene remains separate.

## Design review — ACCEPT for a test slice

- Player problem: a 10–12 cm apple cannot lift human household objects. Magic makes that scale difference playable.
- Choice/tradeoff: choose which object to move first and where to stand. A large floating book obscures more of the view and requires a wider clear path; range is limited. Moving close to the destination is safer than stretching the range.
- Loop: observe a cup, a grimoire and a bottle out of place → aim and lift → adjust distance and walk → set down into a matching saucer, book stack or bottle rack → receive immediate feedback. Three tidy objects finish the test. Mother is a human-sized witch, revealed on completion or timeout.
- Freedom: no mandatory task order. Incorrect placements can be picked up again. A dropped object is not automatically transported to its home. Correct placement requires the actual object to be inside the receiver area, then a deliberate release.
- Readability: physical receptacles, generated illustrated spell pages, one context prompt, generated modern navy/ivory/peach UI sprites. H/Tab shows optional help, not a compulsory route.
- Scope: a single counter-top encounter in a human-sized kitchen. No claim that several maps or a full witch campaign exists. Climbing/jumping can be explored on book-sized steps; it is not a required progression gate in this slice.
- Test: real input can lift/scroll/release, colliders block dragging through surfaces, wrong receiver rejects the prop, all three correct releases trigger success, time expires into mother arrival, restart restores every object. Check actual rendered geometry and generated sprite usage.

## Scale and rendering

Metres throughout: working surface 0.94 m, room about 3.7 × 3.4 × 2.76 m, apple controller about 0.12 m, cup about 0.10 m, bottle about 0.205 m, wand about 0.105 m, mother body human-sized with a taller hat. Gameplay uses stylized jump/gravity tuning while retaining these world dimensions.

Visual thesis: handcrafted apple-witch domestic fantasy. Sage cabinetry, warm ivory, wood, plum leather, brass and peach magic. Broad hand-painted albedo textures, restrained bloom, directional window light, soft shadows and actual modeled bevels/details. Avoid depth-of-field blur over interactive objects.

## Asset pipeline

- Original models authored with reproducible Blender operations in `ArtSource/Blender/Scripts/mama_witch_slice.py`, source `.blend` and triangle manifest retained. Portable official Blender 4.5 used locally because no installed Blender/addon or configured cloud 3D provider was present.
- Built-in imagegen produced a transparent original UI skin atlas, a four-material hand-painted albedo atlas and the illustrated spellbook page. UI panels/icons use these raster assets; live Thai text and input state remain engine UI so they stay readable and functional.
- Borrow the existing apple character/controller, with a new local movement configuration and standalone level/UI. Do not edit its shared prefab or the original MamasKitchen scene.

## Art review status

Runtime and Editor compilation passed; the final staged input/physics review passed 29/29 after the arrival framing correction. Actual Game View screenshots were inspected. This is a usable single-encounter art/interaction prototype. Release-art verdict remains REVISE: unify character/environment shading, improve window/wall dressing and mother animation, and obtain human usability/fun feedback. Evidence and practical limits: `docs/MAMA_WITCH_IMPLEMENTATION.md`.
