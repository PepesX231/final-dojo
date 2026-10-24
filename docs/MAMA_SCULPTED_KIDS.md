# MAMA'S COMING — sculpted fruit kids, comic VFX, chiptune soundtrack, whimsical props

Style direction (user, 2026-10-24): comic-toon, **bright and kid-friendly**; models must be sculpted like clay,
not built from stacked primitives.

## Fruit kids (Apple, Banana, Orange, Watermelon, WaxApple)
- Source: `ArtSource/Blender/Scripts/sculpt/kids.py` (+ `clay.py`, `rig.py`). Rebuild:
  `.tools/blender/blender-4.5.0-windows-x64/blender.exe -b -P ArtSource/Blender/Scripts/sculpt/kids.py`
- Method: metaball clay mass → voxel remesh → brush strokes (eye sockets, brow ridges, cheeks, scooped "D" grin with
  teeth and tongue, banana facets with a palette knife, watermelon sliced top, wax-apple lobes) → thumbprints and
  lumps for a hand-made surface → face-painted toon slots → decimate.
- Output: `Assets/MamasComing/Art/Sculpted/<Kid>/<Kid>_Sculpt.fbx`, 13.5–16.4k triangles each (eyes included).
- Rig: same bone names/layout as the other fruit rigs, so KidActionAnimator still drives the hands.
- Clips (24 fps): Idle 48, Walk 40, Run 24, Jump 30, Punch 18, Taunt 48, Hit 30, Cheer 32. Per-fruit persona
  (banana swaggers and bounces most, watermelon is heavy).
- Unity: `Mama's Coming/Sculpted Kids/Build Prefabs And Use In Game` builds `Prefabs/Characters/Sculpted/PF_Kid_*_Sculpt`
  and points `Resources/MamaKidCatalog` at them (all five, apple included). `Use Original Kids` restores the old catalog.
  `SculptKidMotion` picks clips from player state and fires VFX.
- KidLook: a catalog apple now replaces the scene's apple when `KidLook.Apply(pc, Apple)` runs (HousePlayground does
  this on spawn). Scenes that never call Apply for the apple keep the original scene apple.

## Comic VFX
- Sprites: `ArtSource/Blender/Scripts/sculpt/comic_fx_textures.py` → `Art/ComicFx/Textures` (starbursts with halftone,
  ink puff, star, speed lines, splash, shock ring). Materials: `Mama's Coming/Comic FX/Build Materials`.
- Runtime: `ComicFx.Pow / Puff / Dizzy / Splash` (pooled billboards, shader `MamasComing/ComicSprite`).
- Wired: every PowPop word (punch/bump/land) now gets a starburst + speed lines + ring + stars and an ink outline;
  sculpted kids puff dust on jump and hard landings and get dizzy stars when stunned.

## Soundtrack (original compositions)
- `ArtSource/Audio/chiptune.py` (pulse leads, triangle bass, noise drums, arpeggios, echo) → `Assets/MamasComing/Audio/Music`:
  `MC_TidyUpRumble` (in-level loop, 140 bpm), `MC_MamasComing` (panic chase, 168 bpm), `MC_HomeSweetMess` (menu, 96 bpm),
  `MC_WinJingle`. The in-level loop also goes to `Resources/MamaFoley/music.ogg`, which MamaAudio prefers over its
  procedural music. The other three are ready for the menu / Mama-arrival hooks owned by other threads.

## Props (decor only)
`ArtSource/Blender/Scripts/sculpt/props.py` → `Art/Sculpted/Props`, prefabs via `Build Prop Prefabs`:
curly-trunk teapot, crescent-moon floor lamp, toadstool stool, jelly-bean beanbag, snail-shell wall clock, bubble soap.
Not placed in any level (house layout belongs to the house thread).

## Evidence and limits
- Blender renders: `docs/evidence/sculpted-kids/*_turn_*.png`, `*_pose_*.png`, `PROP_*.png`;
  Unity import renders: `docs/evidence/sculpted-kids/unity_lineup_*.png`.
- Compiled with 0 errors. Not yet tested: a human play session with the new kids, network play with mixed fruits,
  and how the music sounds in context (levels were checked numerically: peak ≤ 0.71, no clipping).
- Known: the mushroom stool's spots mostly missed the cap, and the punch clip reads best from the side. Gameplay arm
  overrides (KidActionAnimator) still take over the arms during chores.
