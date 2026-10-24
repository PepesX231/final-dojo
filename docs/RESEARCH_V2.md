# Research v2 — Presentation Overhaul (2026-10-17)

> **Note 2026-10-23:** the DREAM RUSH card system (card draw/selection, active cards, Dream Laws, Lucky Pillow rerolls) has been removed. Card mentions below are historical records of earlier milestones, not the current game. See `docs/CARD_REMOVAL_REPORT.md`.

> Trigger: the user rejected the Art Target ("ยังกาก") and supplied references: a glossy illustrated card-select concept,
> Octopath HD-2D golden light, an Eiyuden village, Mario Galaxy floating paths, and the Astro Bot galaxy level select.
> They asked for: a much bigger and better first level, a close third-person camera, goofy movement, a cheeky sleep
> animation, a galaxy-style level select after sleeping, a Persona × FF UI with fierce fonts, great lighting and VFX,
> and better models.
> Only sources that were actually opened are listed. Each finding comes with the decision it drives.

## 1. UI / UX — Persona 5 × Final Fantasy, not "AI slop"

**Sources**
- [Visual Design of Games: Persona 5](https://mechanicsofmagic.com/2022/04/23/visual-design-of-games-persona-5/)
- [Sakurai — The aims of UI (Famitsu 515)](https://sourcegaming.info/2016/11/24/the-aims-of-ui-sakurais-famitsu-column-vol-515/)
- [Astro Bot UI explained](https://www.ludo.guide/guide/astro-bot/getting-started/ui-explained)

**Findings**
- Persona 5 runs on 3 colours (red/black/white) plus 1 accent for the selected item. The typeface "screams this is our style". The selected item grows in size, the background character changes pose with the menu, and every information type has a fixed screen zone. Elements are ranked core > supportive > extraneous, and the extraneous ones get the least visual weight.
- Sakurai: the UI is a "drawer" that should match the game's identity (Persona's UI acts in the show), and it must leave room for localisation (Thai lines are taller).
- Astro Bot HUD: health orbs top-left, collectibles top-right, power-up next to health, boss intro card plus a top health bar.

**Decisions** (the "anti-slop" rules are ours, from the review)
1. **Three-colour UI system per screen**: Midnight Plum + Cream + **Apple Red**, with Dream Lavender as the *only* accent for the selected item. No gradients on panels.
2. **Typography with attitude, Thai-capable**:
   - **Kanit Black Italic** for titles and big numbers.
   - **Chakra Petch Bold** for HUD and labels (angular, SAO/Persona-like).
   - **Mali** kept only for diegetic "storybook" text such as notes and the diary.
   - All three are Cadson Demak / OFL and support Thai.
3. **Angular composition**: panels are skewed parallelograms (−8°), title text is rotated slightly, and menus stair-step diagonally. The selected item scales ×1.12, shifts right, and gets a red slab behind it.
4. **Motion is the craft**:
   - Every panel enters with an overshoot slide and every press gives a squash.
   - Screen changes use a diagonal wipe in red and black (no fades).
   - Numbers count up; nothing just appears.
5. **Fixed zones**:
   - Objective: top-left
   - Sleep moons: top-left under the objective
   - Boss name and bar: top-centre
   - Card hand: bottom-right
   - Prompts: next to the target in world space
   - Toasts: top-centre under the boss bar
6. **The background acts**: the title and lobby show the apple in a pose that changes with the menu option (Persona trick).
7. **No slop**:
   - No stock icons and no random sparkles; every glyph is drawn for this game.
   - Never more than two typefaces on one screen.
   - Text never sits on an unreadable background.
   - Every animation curve is ease-out-back or ease-in-out, never linear.

## 2. Lighting — HD-2D-style diorama light in URP

**Sources**
- [Unreal spotlight: Octopath HD-2D](https://www.unrealengine.com/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true)
- [How to create godrays in Unity URP](https://www.gamedeveloper.com/production/how-to-create-godrays-in-unity-urp)

**Findings**
- HD-2D adds a point light together with every combat VFX, so effects light and shadow the scene.
- God rays in URP can be faked cheaply with additive, view-aligned, stretched particles. They tint ochre, fade alpha in and out over lifetime, and use a noise module so they shimmer.
- From the user's references themselves: strong warm backlight, bloom on the hottest values, shallow depth of field (tilt-shift miniature), floating dust motes, and saturated sky gradients.

**Decisions**
- Per-zone **Volume profiles**: tonemapping, colour grading, bloom (threshold ~0.9), **DOF Bokeh / tilt-shift** for the 2.5D zone, vignette.
- **Light shafts**: mesh cones and particle shafts in additive DreamToon-unlit, plus dust-mote particles inside them.
- **Every VFX spawns a short point light** (HD-2D trick): card flash, chair stomp, landing on cushions.
- Skies use a gradient and nebula shader (no flat colour), so the level reads as "in the middle of a dream".

## 3. VFX

**Sources**
- [VFX Apprentice — timing](https://www.vfxapprentice.com/blog/the-soul-of-effects-what-is-timing-in-vfx)
- [VFX Apprentice — style guides](https://www.vfxapprentice.com/blog/creating-vfx-style-guides-for-games)

**Findings**
- Timing is Anticipation → Impact → Dissipation, with the main action inside the **first 25% of the lifetime**. Curves are non-linear. Faster than a heartbeat reads as aggressive; under 60 BPM reads as soothing (heal/sleep).
- VFX sit brighter than the characters and darker than the UI. Use a limited palette (analogous/triadic), "crunched" gradients in highlights, and big simple hard-edged shapes with hand-made textures. Secondary elements still need contrast.

**Decisions**
- VFX library built from real ParticleSystems (Unity) with hand-painted sprite sheets in our ink/paper grammar:
  - dust puff, star burst, impact ring, speed lines, sparkle trail, Zzz bubble
  - telegraph ring for boss skills (red, grows during anticipation)
  - confetti, card-activation burst
- Every effect is authored as A/I/D: anticipation 0.05–0.4 s, impact frame, dissipation curve.

## 4. Level design — a BIG first level that hooks players

**Sources**
- [Nintendo 4-step (Kishōtenketsu) — MCV](https://www.mcvuk.com/development/video-nintendos-level-design-secrets-in-four-steps)
- [3D platformer level design — gamedesignskills](https://gamedesignskills.com/game-design/3d-platformer-level-design/)
- [Astro Bot director interview — CGM](https://www.cgmagonline.com/interviews/team-asobi-nicolas-doucet-astro-bot)
- [Astro Bot Rescue Mission visual style — PS Blog](https://blog.playstation.com/archive/2019/01/24/how-japan-studio-defined-the-visual-style-of-its-ps-vr-hit-astro-bot-rescue-mission)

**Findings**
- Each 3D Mario stage teaches, develops, twists and then **drops** a mechanic within about 5 minutes.
- Landmarks and the camera keep the player oriented. Use clear sightlines, rest areas after stress, frequent checkpoints, optional harder routes with better rewards, and verticality.
- Block out first, then art.
- Astro Bot: every planet must leave a distinct memory, and repeated ideas get cut. Keep the main path easy for families with hard side challenges ("easy to play, hard to master"). **60 fps over detail**, clarity before detail (contrast first, then bevels). Surprises are saved to the end.
- Bosses open intimidating, then reveal weaknesses and turn silly, which leads to a humorous ending.
- Nearly every object reacts to touch, so the world feels like an interactive diorama.

**Decisions**: see `docs/LEVEL1_DESIGN.md`. In short, five "chapters" of a dream book, each in a different art style and each with its own kishōtenketsu mechanic. A landmark (the Throne Tower) is visible from everywhere, plus a chase sequence and a 3-phase boss.

## 5. Camera

**Sources**
- [Common 3rd-person camera problems — Game Developer](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)

**Findings**
- Occlusion: dither or fade the geometry near the camera rather than snapping.
- Recenter behind the player when the stick is idle and return to the programmed position.
- Zoom out when sprinting or in combat. Use a camera boom that only follows vertically after the character rises past a threshold (stable jumps).
- Too close (Dark Souls 3) clips into enemies, and a fixed camera (3D World) can't inspect.

**Decisions**
- **Close follow camera behind the apple**: distance ~5.5 m, pitch 16°, FOV 55. The player orbits freely, with auto-recentre after 1.2 s idle.
- Vertical deadzone: the camera ignores jumps unless the player lands higher.
- Sprint pulls out to 7 m. **Boss focus** (hold `Tab`/`L2`) frames the chair, and the boss fight also zooms out automatically.
- Side-scroll rails in the 2.5D chapter.
- Fade any occluder within 1 m of the line to the player (dither).

## 6. Animation — goofy, readable, never slow

**Sources**: the Astro Bot interview above (game feel and 60 fps first; jump/run must feel good). The rest comes from our own knowledge of the 12 animation principles.

**Decisions** for the procedural apple animator:
- **Walk**: a waddle — body roll ±10°, hip sway, arms swinging wide and out of phase, head bob.
- **Sprint**: a cartoon "wheel" run where the legs circle in a blur, arms windmill overhead flailing, the body leans 25° forward, the cheeks wobble, and dust puffs spawn every step.
- **Skid**: a quick reversal plants both feet, leans back, kicks up a dust arc and plays a squeak.
- **Air**: arms flap like trying to fly at the jump peak; panic flail when falling far.
- **Idle**: breathing, then a fidget after 6 s (look around, scratch, yawn).
- **Sleep (cheeky)**: lies belly-up in a starfish pose, snores with a growing **snot bubble** in sync with breathing, scratches its belly, the leg twitches (dream-running). The bubble pops when the dream transition starts.

## 7. Models / Blender

**Sources**
- [Top free Blender add-ons 2025 — BlenderHub](https://blenderhub.beehiiv.com/p/top-10-free-blender-addons-you-need-in-2025-951a1403f74256a5)
- [Blender Studio — Geometry Nodes from scratch](https://studio.blender.org/training/geometry-nodes-from-scratch/introduction-to-node-tools-in-blender-40)

**Finding**: the quality comes from technique more than plug-ins: subdivision surface modelling, bevels, Geometry Nodes for scatter/variation, and baking.

**Honest constraint**: this session runs Blender headless (`bpy` 5.2) from scripts. Extensions downloaded from extensions.blender.org aren't available offline here, so I use Blender's **built-in** tools only:
- Subdivision, Bevel, Remesh, Shrinkwrap, Geometry Nodes and Cycles baking
- Cycles renders for the **card illustrations** (the same 3D characters lit like key art), which is how the reference-style card art becomes achievable without AI images

**Decisions**
- Re-model the chair and props with subdivision modelling: chunkier, rounder, more personality, carved details.
- Render card art in Blender with rim light and coloured backgrounds.
- Keep the user's apple design (it is his character) and improve its shading, face and animation.

## 8. Fonts (downloaded from github.com/google/fonts, OFL)
- Kanit (Black Italic, ExtraBold Italic)
- Chakra Petch (Bold, SemiBold)
- Mali (SemiBold, Bold) — already in the project
