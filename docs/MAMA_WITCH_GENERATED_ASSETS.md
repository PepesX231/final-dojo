# Witch test — generated raster assets

Created 23 October 2026 with the built-in imagegen tool. The supplied moodboard informed navy/ivory/peach, whitespace and restrained contours; the apple/witch artwork is original. These PNGs are imported as real textures and sprites in the test scene. Text is live Unity text for Thai readability, menus and changing timer/object state.

| File | Role | Generation specification |
|---|---|---|
| `Assets/MamasComing/WitchTest/UI/AppleWitchUI.png` | Eight UI graphics: cream panel, dark panel, peach button, dark button, mouse-left glyph, scroll glyph, apple wand icon, reticle | Original production UI atlas on transparency, four columns and two rows, no text or phone mockup; dark navy/plum, warm ivory, thin contours, peach accents, generous negative space, subtle paper, original leaf motifs. Panel/button centers opaque; icons on transparent ground. Reference image supplied by user. |
| `Assets/MamasComing/WitchTest/Textures/WitchSurfaces.png` | Actual model albedo texture | Square four equal quadrants: warm understated honey/walnut wood grain; muted sage painted wood; warm ivory ceramic/stone mottling; muted plum cloth/leather grain. Flat hand-painted base color, no scene, baked highlights, labels, grid or PBR-map composites. |
| `Assets/MamasComing/WitchTest/Textures/SpellPages.png` | Printed pages on the open book and illustrated help overlay | Top-down 2:1 original apple-witch parchment spread. Left: carved apple wand lifting a teacup. Right: cup settles on saucer, book on book stack, bottle in rack. Fine navy line, peach/sage, botanical leaf margins, no readable text, no perspective/shadows. |

Sprite regions are cropped from the generated PNG and saved as Sprite assets by `WitchSceneBuilder.MakeSkin`. This operation imports the generated artwork; it does not redraw panels as procedural UI shapes. Material UV scale/offset selects the four generated texture regions. Original uncropped PNGs remain intact.

The Blender models, collision geometry, camera, lights and runtime interactions are real 3D scene content. The concept image from the earlier research is not used as a background pretending to be gameplay.
