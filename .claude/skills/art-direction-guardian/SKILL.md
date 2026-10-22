---
name: art-direction-guardian
description: Review any DREAM RUSH visual asset, material, lighting setup, 2D cutout, VFX or UI screen against Applebook Surrealism and approve, request revisions or reject. Use before an asset enters a scene and before every art pass sign-off.
---

# Art Direction Guardian — Applebook Surrealism

## Purpose
Prevent inconsistent or "AI-slop" art. Reject assets that are impressive but do not fit (GDD §5, §6.6, §20.5, §23.1).

## Relevant files
`docs/ART_DIRECTION_CHECKLIST.md` (the checklist — use every section) · `docs/ASSET_MANIFEST.md` (record verdicts) · `docs/REFERENCE_NOTES.md` · `ArtSource/` · `Assets/DreamRush/Art`, `Materials`, `UI`, `VFX`

## Procedure
1. **Context first**: state the asset's purpose (worldbuilding/navigation/gameplay/story/character/composition/storytelling). No purpose → REJECT.
2. **Look at it**: render or screenshot it (Blender render or Unity Game view) at gameplay camera distance, next to the player (1.2 u), bed, chair and existing props, under actual game lighting. Never review from file names alone.
3. **Silhouette test**: flat black silhouette, from front/side/back, while moving if animated.
4. Run every section A–I of the checklist; cite the specific failing item.
5. Palette check: sample dominant colours; compare to tokens; background less saturated than gameplay objects.
6. IP check: nothing traceable to a reference game's model, layout, UI, character or music; no Apple Inc. logo/bitten apple.
7. Write the verdict into `docs/ASSET_MANIFEST.md`.

## Verdicts
- **APPROVED** — all sections pass
- **REVISE** — list concrete, actionable corrections (e.g. "reduce leaf size 20% — covers right eye at 18° pitch")
- **REJECTED** — wrong style/purpose/IP risk; explain why it cannot be fixed by tweaks

## Output
```
### Art Review: <asset>
Purpose: ...
Evidence: <screenshot/render path>
Silhouette: pass/fail – ...
Checklist failures: [C3] ..., [I] ...
Verdict: APPROVED | REVISE | REJECTED
Corrections: 1. ... 2. ...
```
