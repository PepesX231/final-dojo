# M7 Visual Reconstruction — Audit, Visual Target, Plan

> Trigger: user review 2026-10-17 — the art direction scored 2/10. It reads as a graybox prototype with no
> Applebook identity. Reconstruct the visuals, keep the gameplay, and lock an **Art Target Scene** first before expanding.
> Engine: Unity 6000.3 / URP 17.3 (Forward+). Assets: Blender (bpy scripts) → FBX; textures painted by Python scripts.

## 1. Audit — what is still a placeholder

| Element | Now | Verdict |
|---|---|---|
| Floor (whole dream) | `GRAYBOX_` grid texture, square tiles | **REPLACE**: this is what makes it read as a prototype |
| Arena walls | lavender boxes 3 m × 0.5 m | **REPLACE**: rectangular, no story |
| Furniture (sofa, coffee table, lamp, ottoman, cushions, balcony) | Unity Cube / Cylinder primitives | **REPLACE** with sculpted assets |
| Background "floating furniture" | primitives at low saturation | **REPLACE** (depth layer) |
| Lighting | 1 directional + trilight ambient, flat | **REBUILD** (focal pool, colored shadows, post) |
| Material/shader | URP Lit, default smoothness everywhere | **REPLACE** with a single shading model (DreamToon) |
| Outlines | none | **ADD**, selective: player, chair, interactables (§5.6) |
| Camera (arena) | pitch 42°, distance 15.5 → apple takes ~6% of screen height | **REWORK** closer, lower 3/4 |
| Apple | model from the user's .blend; dark crimson skin; becomes a dot on screen | **KEEP the model**, fix readability (shading, outline, rim, camera, palette lift) |
| Chair | M7 model is OK but uses plain Lit | **KEEP + restyle** (shader, outline, seat cushion = sit target) |
| UI | Arial (LegacyRuntime), flat cream boxes | **REPLACE** with a storybook kit (Mali font, paper + ink + tape) |
| 2D cutouts (clouds/stars/moon/plants) | OK but scattered with no layout logic | **KEEP the style**, re-place them with purpose |
| VFX sprites | OK (paper-cut) | KEEP |
| Toy cars / tracks | primitives | REPLACE after the arena is locked |

## 2. Visual Target — "The Pop-up Storybook Living Room"

**One-sentence identity:** Pip's dream turns the living room into a **giant pop-up picture book** that opens in the night sky. The floor and walls are thick printed paper pages. The furniture and characters are soft, toy-like 3D objects standing on the page.

That makes the 2D/3D integration the identity itself, not decoration:
- **Floor = book pages**: cream paper printed with watercolour floorboards and ink lines. The page edges show stacked-paper thickness, the spine is a faint fold line, and a deep plum cloth cover sits underneath.
- **Walls = pop-up panels**: stand up from the page edge on fold tabs, with visible thickness. Printed wallpaper (lavender with a small moon/apple pattern), plus a drawn window, drawn frames and drawn light switches. Height ~1.7 m, so the camera sees over them and they don't block the view.
- **Furniture = sculpted toy objects** (soft corners, chunky, slightly crooked): fabric sofa, round coffee table, floor lamp (also the in-world light source), painted toy blocks, puffy cushions.
- **2D cutouts with a job**: cutout plants/clouds/stars stand on paper tabs (composition, depth); a poster on a wall (story); dotted arrows printed on the floor (navigation, e.g. the cushion route).
- **Background**: plum-indigo night, low saturation, with drifting paper clouds and other "pages" floating far away, giving depth without stealing attention.

**Colour script (arena):**
- Floor pages: warm cream/honey, medium-light.
- Walls: lavender (cool), lower value than the floor.
- Characters: the apple stays a strong rose-red; the chair is honey wood with a red-cream gingham seat cushion as the focal point; interactables get a single mint/apricot accent.
- Background: plum.
- Never use every accent at once (§5.3).

**Lighting:**
- One dominant warm key from the direction of the floor lamp (upper left-back), with soft shadows.
- Cool lavender ambient.
- A warm light pool over the centre of play fading toward cooler edges (focus), plus a subtle vignette.
- No glow, no bloom on everything.

**Shading (DreamToon, one shader for the whole game):**
- Soft 2-tone ramp (wide smoothstep), shadows tinted plum instead of black.
- Slight wrap lighting; a thin cream rim only on characters and interactables.
- Alpha-clip and double-sided support for paper.

**Outline:** ink lines (#302A3D), screen-space ~2.5 px, only on the player, chair, interactables and important props. Pages and walls use printed ink lines instead.

**Camera (arena):** pitch ≈ 32°, distance ≈ 10–11 m, FOV 40, pulling back to about 13 m when the chair is far. Target: the apple fills ≥ 12% of screen height, and the chair stays in frame ≥ 90% of chase time (measured with the bot).

**UI = storybook kit:**
- Font: **Mali** (Cadson Demak, OFL; supports Thai and Latin).
- Paper notes with torn edges and tape; hand-inked frames.
- Sleep shown as moons; the card in hand as a real tilted card with tape; the boss name on a ribbon; prompts as paper tags with keycaps.

## 3. Replacement plan (controlled passes)

1. **Shading foundation**: `DreamToon.shader`, `InkOutline.shader`, `DreamLook` (materials + outline helper), URP post Volume profile (tonemapping, light grading, plum vignette).
2. **Art Target assets** (Blender scripts + painted textures): page floor, wall-page panel, sofa, coffee table, floor lamp, toy block, puffy cushion, chair seat cushion; textures for floorboards print, wallpaper print, felt rug, sofa fabric, paper edge.
3. **`Art_Target.unity`** (one area ~16 × 12 m): apple + chair + floor + 2 wall pages + 4 furniture pieces + 2D cutouts + real HUD, shot from the real gameplay camera.
   - Iterate until it passes §4.
   - **No expanding until it passes.**
4. **Arena reconstruction (Section E)** with the same standard; the gameplay layout (§14.3) and colliders/NavMesh stay put, only the visuals change.
5. Sections A–D + Bedroom hub, in order.
6. Toy cars / hazards / VFX polish to match.

## 4. Acceptance criteria for the Art Target (art-direction-guardian)

- [ ] **Identity test**: with the logo and the apple removed, the image still reads as "a dream inside a children's picture book".
- [ ] **Readability**: the apple ≥ 12% of screen height and the chair ≥ 15%. Squinting at the image, the player and chair are the first two things you see.
- [ ] **Value structure**: three clear planes (background darkest / walls mid / floor light), with characters contrasting the floor (measured ΔL ≥ 20).
- [ ] **Cohesion**: one shader, one ink colour, one paper texture, one font; every element appears in the colour script.
- [ ] **No slop**: every element has a stated purpose (§5.10); no random glow or particles.
- [ ] **Gameplay intact**: EditMode and PlayMode all green, chair bot ≥ 90% wins, chair in frame ≥ 90% of chase.
- [ ] **User sign-off** on the Art Target screenshots before the full level is expanded.

## 5. Art Target — status (2026-10-17, iteration 7)

Scene: `Assets/DreamRush/Scenes/Art_Target.unity`, built by `DreamRush/Build/Art Target Scene` (`ArtTargetBuilder.cs`). Every captured image is a real render from Unity Play Mode. `DevBridge/art_cycle.sh` rebuilds the scene and captures the gameplay, overview and close views.

| Iteration | Change | Reason |
|---|---|---|
| v1 | DreamToon + book floor + walls + kit + cutouts | first look |
| v2–v3 | key light 1.45 → 0.8, focus pool 26 → 3.5, ambient 0.6 → 0.35 | overexposed: cushion, table and lamp turned white |
| v3 | single shared ink material asset | runtime-created outline materials don't survive in prefabs (the apple rendered magenta) |
| v4 | Storybook UI: Mali font, paper 9-slice, tape, ribbon, moons, real card | UI read as debug |
| v4 | floorboard ink lines softened | floor was noisy, competing with the characters (§5.5) |
| v5 | apple skin: palette Apple Red tint + 42% texture | the user's texture is dark/mottled; tinted pale it became salmon |
| v5 | post: contrast 16, saturation +12, vignette 0.2, bloom 0.15 | image was hazy and flat |
| v6 | butter-yellow table, saturated sky cushion + felt stitching, larger cover, bookmark ribbon | blues washed out under lavender light; the "book" read needed a cue |
| v7 | spine shadow softer/narrower, poster/window enlarged | the spine line cut the composition |

Evidence: `docs/evidence/VR_ArtTarget/` (00_first_render, at_v7_game/overview/close).

Acceptance check (iteration 7):
- Identity: pop-up book read via page walls, edges, spine and bookmark ✓ (pending user confirmation)
- Readability: apple ≈ 16% of screen height in gameplay, chair ≈ 24%; both are the first things you see ✓
- Value structure: background darkest / walls mid / floor light ✓; red apple vs cream floor ✓
- Cohesion: one shader, one ink, one paper, one font ✓
- Gameplay: EditMode 45/45, PlayMode 42/42 after the change ✓
- **User sign-off: pending** → do not expand to the whole level until approved
