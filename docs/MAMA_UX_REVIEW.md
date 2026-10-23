# MAMA'S COMING! — observation-led UX, 2026-10-23

This review concerns `Assets/MamasComing/Scenes/MamasKitchen.unity`, the user's current game. DreamRush supplies the apple player/controller; its campaign GDD does not describe this game's loop.

## Problem

A new player cannot infer the goal, the teddy's home or how the kitchen fire works. The existing HUD displays a long priority list, several generic exclamation marks and a home marker whose label disappears off-screen. Decorative badges compete with the room and do not explain the affordances.

## Decisions

- Keep the player's choice of priorities, genuine cleaning versus concealment, and alternative fire responses. No mandatory sequence, automatic completion, new unlock gates or save migration.
- Put the goal and impending inspection on the title and a compact persistent goal card.
- Describe what the player is looking at or holding. A displaced ordinary prop names its usual room. Show a matching translucent home silhouette only in that room; holding H can reveal the destination earlier. Near home, explicitly show Q and confirm successful placement.
- State the oil/water relationship in the kitchen and item context. Explain a lid's relationship to air and an extinguisher's powder tradeoff. H offers ideas and highlights two alternatives; neither is selected automatically.
- Put the longer list behind Tab. Optional H hints disappear on release. Suppress other hint panels during minigames and settings. Avoid a fixed quest route.
- Keep the handmade paper palette, increase text contrast, reduce large badges and move useful item information into one consistent card. Lower bloom and saturation slightly in a cloned runtime volume profile. The authored house and shared materials are preserved.
- New copy supports Thai and English, including live language changes. English/Thai selection remains the existing user preference.
- Teddy's clue identifies its bed/pillow as well as the bedroom. Exact home placement also commits the pose to the Rigidbody, clears release velocity and sleeps the still-dynamic body. A focused input test exposed that the old drop path could bounce a correctly returned teddy out of place; the first failing run is retained.
- Returning a prop gives immediate confirmation rather than queueing an instruction phrased as if it still needs doing. When carrying a lid or towel at a burning pot, interaction targeting prefers that compatible action over the closer stove knob; the review reproduced the old misleading selection of the stove.

## Validation plan

Compile Runtime/Editor; inspect actual Game View captures; exercise normal Enter/E/Q/H/Tab input at staged positions for wake-up, teddy pickup/return, optional hints and a lid fire solution. Check Thai/English updates and context text fit. Preserve the original input settings and language preference after review.

Staged tests do not establish first-time discoverability or subjective fun. A fresh-player playtest without coaching remains necessary. No test changes the real DreamRush save.

## Evidence

Local before/after captures and timestamped results are stored in `DevBridge/mama-ux/`. The pre-existing unsaved scene was saved without reverting it, then backed up to `DevBridge/mama-ux/before/MamasKitchen.unity` before implementation. Shared scene/material edits were retained.

Initial focused review stopped at a hidden-wrapper assertion (text was hidden, wrapper was still active); the wrapper is now hidden too. The second review passed 16 checks before exposing the teddy placement physics issue. The next passed 23 checks before finding that the stove stole the burning-pot interaction while carrying a lid. Failure reports remain alongside the successful results.

Final: `DevBridge/mama-ux/review-121606.txt` — **31 checks passed**, using staged positions and normal Enter/E/Q/H/Tab input. Includes teddy stability after physics settles, a real lid interaction, replacement of the obsolete red fire banner after extinguishing, live Thai/English switching, context text fit and no gameplay exceptions. An earlier 30-check passing run is also preserved. The exact unrelated external MCP authorization rejection is excluded by the review's log filter; other gameplay errors fail the review.

Runtime and Editor compile with **0 errors**. Compilation also reports two existing warnings in KitchenSink (hidden inherited method) and Mop (unreachable code). Reviewed actual 1920×1080 Game View captures: `after-title.png`, `after-play.png`, `teddy-room-clue.png`, `teddy-optional-direction.png`, `teddy-home.png`, `fire-options.png`, `fire-solved.png`. These are in-engine captures, not generated concepts.

SHA256 of the authored scene still matches the saved pre-implementation backup. Input background settings, virtual keyboard and the original language preference are restored by the focused review. No standalone build, controller playthrough, broad campaign suite, lower-resolution visual review or uncoached new-player session was performed.
