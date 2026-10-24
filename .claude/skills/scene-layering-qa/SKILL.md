---
name: scene-layering-qa
description: Check and fix how a MAMA'S COMING level is layered before calling art done - z-fighting coplanar surfaces (flicker/blur), props floating in the air, props buried inside each other, stray objects under the floor or outside the house, and decals/trims placed on the wrong side. Use after building or dressing any level, after an art pass, and before screenshots or a PR.
---

# Scene layering QA

A level looks broken long before it plays broken: two boxes sharing a face flicker, a cup hovers 4 cm above the counter, a
picture frame is half inside the wall, a leftover cube sits under the floor. This skill is the checklist and tool for that.

## Tool

`Assets/MamasComing/Art/Style/Editor/MamaLayerAudit.cs`

- Menu **MamasComing ▸ Style ▸ Audit scene layering (report)** - writes `docs/evidence/style/layer_audit_<Scene>.txt`.
- Menu **... + fix thin z-fighting** - also nudges thin (< 3 cm), collider-free decoration 3 mm out of a shared plane.
- Batch: `MamaToonStyleTool.Batch` runs it for both levels.
- The witch level is authored at 12 cm-apple scale; the tool scales all tolerances by 0.25 there.

What it reports:

| Tag | Meaning | Usual fix |
|---|---|---|
| `Z-FIGHT` | two axis-aligned box faces in the same plane, same facing, overlapping, different materials | move the decoration 2-5 mm proud of the surface, or shrink the hidden one; never two full faces in one plane |
| `FLOATING` | prop with nothing below within 3 cm and nothing touching its sides | drop it onto its support (snap bottom to the surface) |
| `BURIED` | a prop more than 50% inside another prop | separate them, or merge into one authored asset |
| `BELOW-FLOOR` | renderer far under the floor level | delete strays left by builders |

## Rules when authoring

1. **One surface per plane.** Rugs, posters, tiles, decals, trims sit 2-5 mm proud (12 cm-apple scale: 0.5-1 mm). Do not stack
   two cubes whose faces coincide.
2. **Everything rests on something.** Props touch a support (floor, shelf, counter) or a wall; ceiling things are named
   `Lamp`/`Pendant`/`Fan`.
3. **Thin things are thin.** Paper, frames, rugs: give them real thickness (>= 5 mm) so depth precision and the ink pass
   read them as separate layers.
4. **Respect the camera.** Far clip / near clip ratio decides depth precision. Keep near clip >= 0.05 m in the house
   (0.01 m in the witch level) - a tiny near clip is the most common cause of distant flicker.
5. **Decide what belongs.** If an object has no gameplay or storytelling job in that room, remove it. Builders leave
   helpers - check `BELOW-FLOOR` and hidden roots before shipping.
6. **Never move gameplay collision for looks.** Nudges are only for collider-free decoration; anything with a solid
   collider is reported, and a human decides.

## Done means

- Audit report has 0 `Z-FIGHT` on visible surfaces, 0 `FLOATING`, 0 `BELOW-FLOOR`; every remaining `BURIED` is intended.
- A screenshot from the gameplay camera shows no flicker (look at it in motion, not only stills).
- Report the counts honestly in the PR; an audit pass is not proof of a human visual review.
