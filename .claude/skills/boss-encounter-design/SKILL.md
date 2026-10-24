---
name: boss-encounter-design
description: Design, implement and validate the DREAM RUSH chair boss (and later bosses) — fair flee AI, escape destination scoring, fatigue/stumble openings, multiple victory methods, anti-softlock and seat validation. Use whenever boss behaviour, arena layout or win conditions change.
---

# Boss Encounter Design — The Chair That Refused

## Purpose
Make the chair a readable, funny, fair *problem* rather than an HP sponge (GDD §11–16, §18, §20.4).

## Relevant files
`docs/GDD.md` §12–16, §18 · `Assets/DreamRush/Scripts/Runtime/Boss/` · `Assets/DreamRush/Scripts/Runtime/DreamLogic/` · `Assets/DreamRush/Tests/*/Boss*` · scene `Dream_Chair`

## Required state machine
IDLE → ALERT → FLEE ⇄ REPOSITION, FLEE/PANIC → STUMBLE | EXHAUSTED, ANY → ROOTED | FROZEN, social → OFFER_SEAT, SIT_AVAILABLE → SITTING → DEFEATED.
Every transition has: trigger condition, minimum dwell time, animation/VFX/SFX cue. Draw it (text diagram) in the review.

## Fairness rules
1. Sprinting player is faster than chair normal speed (config check: `sprint_speed > chair_normal_speed`).
2. Escape destinations come from **reachable NavMesh candidates** scored by: path length, distance from player, obstacle proximity, route quality, recency penalty, weighted random pick. Never `pos + away*dist` directly.
3. Openings: hesitation, awkward turn, stumble, look-back, exhaustion (≈3 s). Exhaustion must happen at a **reachable** point.
4. Anti-softlock: out of arena / off NavMesh / stuck / no path → visible dreamlike reposition (never a silent teleport in front of the player).
5. Seat validation: correct boss, range, sit-compatible state, reachable seat, player not stunned, sit anim started, **2.0 s continuous**.
6. Victory triggers exactly once; no state leaks into the next run.
7. Card effects act through `DreamProperty` + statuses; the player must still approach and sit.

## Validation procedure
1. EditMode: escape scoring (unreachable rejected, recency penalty works), fatigue accumulation/decay, seat timer.
2. PlayMode bot test: ≥50 seeds × 90 s simulated chase → assert chair always on NavMesh & inside arena bounds, ≥1 reachable opening per 30 s, no exceptions, victory once when bot sits.
3. Manual: win by baseline (no card) + each implemented card method; note time-to-win.
4. Humour check: does each state read from body language alone?

## Output
```
### Boss Review: <change>
State diagram: ...
Tuning (config): ...
Bot test: seeds=, softlocks=, out-of-bounds=, avg openings/30s=
Victory methods verified: [ ] Exhaust [ ] Rooted [ ] Frozen [ ] Pulled [ ] Grandpa [ ] Shortcut [ ] Obstacle
Issues: ...
```
