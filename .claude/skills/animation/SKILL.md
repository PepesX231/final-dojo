---
name: animation
description: Author and wire MAMA'S COMING character animation - fruit-kid move sets keyed in Blender with IK intent (squash/stretch, anticipation, overshoot) and played in Unity from player state. Use when adding/retiming a move, a new fruit, or when a move looks stiff.
---

# Animation — fruit kids

## Where things live
- `ArtSource/Blender/Scripts/sculpt/rig.py`: rig layout (Root, Body, Face, Arm_*/Arm_*_Lower siblings, Hand_* under
  the lower arm, Leg_*/Leg_*_Lower/Foot_*) — must match `KidActionAnimator`, which drives hands on top of clips.
- `rig.moves(persona)`: Idle 48f, Walk 40f, Run 24f, Jump 30f, Punch 18f, Taunt 48f, Hit 30f, Cheer 32f @24fps.
  Persona per fruit: `swagger` (hip sway), `bounce`, `heavy`, `arm` (swing size).
- Unity: `SculptKidMotion` (on the CHR holder) picks the state; `MamaSculptKids` builds the controller with those
  eight states and loops Idle/Walk/Run/Taunt/Cheer.

## Authoring a move
Write intent, not bone rotations: a `Pose` per normalised time t with body `off` (m), `tilt` (deg about the
pelvis), `sq` (+squash / -stretch, volume preserved), hand targets relative to the rest wrist, foot targets
(planted unless lifted), `foot_pitch`, `hand_turn`. `solve()` runs 2-bone IK and keys plain FK.
Use `keys(t, [(time, value), ...])` for eased beats.

Principles that make it read at gameplay distance:
- Anticipation before every action (jump crouch ~20%, punch wind-up twist ~25%).
- Snap: the contact frame arrives in 2–3 frames, then hold/overshoot (`back()` style).
- Squash on impact, stretch on launch; never change volume (handled by `sq`).
- Offset the arms half a cycle from the feet; heavy characters move less and later.
- Loops: last key equals the first (make_action loop=True does this).

## Gameplay mapping (SculptKidMotion)
Hit when stunned starts (+ ComicFx.Dizzy), Punch when KidActionAnimator plays Clip.Punch, Jump on
PlayerMotor3D.Jumped (+ dust puff) and while airborne, Run above `runAbove` or sprinting, Walk above .25 m/s,
Taunt after `tauntAfter` seconds standing still, `Cheer()` for wins. Clip speed follows planar speed.

## Check
Render pose stills (`kids.py` writes `<Kid>_pose_<Move>.png`), then play `Art/Sculpted/Showcase/MamaSculptShowcase.unity`
which cycles every kid through every move with the comic VFX.
