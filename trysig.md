# Mama's Coming — quick playable iteration, 24 October 2026

## Direction

Use the supplied apple/banana references: cheerful fruit, big readable faces, white gloves, soft purple shoes, playful movement, bright family-game interiors. Reuse the existing original fruit prefabs, preserve their textures, and use the same catalog in the lobby and game. A new source repository has not been provided yet. Do not replace these with another newly invented cast.

## Implemented

- ESC settings has a localized Quit game button. In the Editor it stops Play; a player build calls Application.Quit. Solo settings pauses; network settings keeps simulation/networking running and blocks local actions.
- Tap LMB / G / RB with empty hands to punch on release. Holding for 0.32 seconds restrains the friend in front; release, distance, stun, menu, and a short network lease break the grab. The victim moves at 12% speed and cannot jump while held; the grabber moves at 45%. This iteration restrains rather than physically dragging the victim with a joint.
- Removed running-into-friends knockback; player collider pairs ignore one another. Punches still knock down and drop carried items.
- Owner movement remains responsive; host owns household simulation and item outcomes. Replicate airborne state, suppress held-tool use while the menu is open, and snap long-distance teleports instead of interpolating across the house. Grab requests are routed through the host and checked for sender, distance, and floor.
- Four network slots, with separate spawn points. Existing Unity Netcode + Multiplayer Services/Relay retained; no new plugin or paid asset dependency.
- Host chooses up to 10 / 18 / 26 disaster devices for 1 / 2 / 3+ players, capped by available authored devices. At most 2 / 3 / 4 scheduled devices are urgent simultaneously. Core stove/flood/clutter and chain reactions are additional problems; this is not a promise of exactly that many total chores. Selection is frozen at round start; reconnecting/late joining does not reroll the house.
- Removed upper-floor access/content from the active main scene. Moved the bedroom into the former staircase footprint, moved its pickup homes, and removed upstairs Mom-route stops. Original upper content is retained inactive for recovery. The separate house-study prototype is unchanged.
- Chair home rotations face their tables. Warmer high key light, softer ambient fill, brighter grading, less vignette, and lower camera framing. Existing texture maps retained.
- Fixed the flood surface's straight-alpha/premultiplied-blend mismatch that painted dry floor cyan. Water retains puddle edges, ripples, and visible floor beneath. Added a wet-floor explanation; ordinary walking no longer randomly slips.
- Smoothed fruit animation speed and airborne squash transitions. Gait arm overrides yield to actions. Existing authored animation clips are reused; a new 30-frame bespoke run cycle for every fruit has not been authored.

## Validation

- Unity runtime: original five-model catalog, CHR_AppleChild active, 23 ground-floor hazard components, solo selects 10 with 2 urgent, upper floor inactive, zero missing scripts.
- Runtime settings checks: solo menu timeScale=0, online menu timeScale=1, localized Quit button exists, grab lease is applied.
- Existing Banana staged rig/carry regression: 33/33 passed. This tests bone binding, grip contacts, pickup reach, and carrying while walking; it is not a human playtest.
- Captured and inspected the live Game View to isolate the cyan-floor problem; hiding water removed it and straight-alpha blending corrected it.
- Windows player build succeeded with 0 errors. A two-process localhost headless run verified avatar registration, cup pickup/drop, punch relay and matching post-hit positions. Details and limits are in [RESULTS.md](docs/evidence/mama-fast-iteration/RESULTS.md).

## Review next

This is an implementation and regression pass, not certification that every house layout issue or animation defect is gone. Review all fruit motion in game, check reachable tools and device dependencies, test real internet Relay with friends and packet loss, and tune the problem budgets from completion times. Do not describe a local multi-process test as a real two-machine internet test.

## Research

- [Unity interpolation](https://docs-multiplayer.unity3d.com/netcode/2.0.0/learn/clientside_interpolation/): smooth received snapshots while keeping ownership explicit.
- [Unity client anticipation](https://docs-multiplayer.unity3d.com/netcode/2.1.1/advanced-topics/client-anticipation/): NGO does not automatically supply full rollback/prediction; installing a plugin alone does not guarantee matching screens.
- [Unity 6000.3 animation blend trees](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/animation-animator-controller/animation-state-machines/animation-blend-trees/class-blend-tree): blend movement changes rather than hard-resetting animation each frame.
