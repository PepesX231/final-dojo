---
name: playtest-qa
description: Find, reproduce, prioritise and retest DREAM RUSH bugs, balance problems, unclear objectives, softlocks and performance issues using automated tests, seeded bot runs and playtest sessions. Use at the end of every milestone and after any bug fix.
---

# Playtest & QA — DREAM RUSH

## Relevant files
`docs/TEST_PLAN.md` (test IDs & criteria) · `docs/MILESTONE_REPORTS.md` (evidence) · `Assets/DreamRush/Tests/**` · `DevBridge/out/*` (compile/console/test logs)

## Procedure
1. **Automated**: run EditMode then PlayMode (DevBridge `tests EditMode` / `tests PlayMode`). Paste real totals (passed/failed/skipped) and failing test names. Never summarise as "all pass" without the file.
2. **Console audit**: scan `out/console.log` for errors/warnings/exceptions during play; each one becomes an issue or is justified.
3. **Seeded runs**: for random systems (chair AI) run fixed seed lists; record seed with every bug.
4. **Manual play** following the milestone's manual cases; capture screenshots for each bug.
5. **Prioritise**:
   - P0 crash/softlock/progress blocker/data loss
   - P1 core loop broken or very frustrating (input, camera, unfair boss)
   - P2 noticeable but workaround exists
   - P3 polish
6. **Fix → retest**: rerun the exact repro + related automated tests; mark Retested with evidence.
7. **Playtest sessions** (GDD §31): don't coach; record metrics in §31.3; ask §31.4 questions.

## Bug format
```
[P1] Player — buffered jump gives extra height
Repro: Movement Gym, hold jump while falling from step 2.0, release before landing
Expected: normal full jump height (2.1)  Actual: 2.6
Seed/Scene: Test_MovementGym  Evidence: out/console.log L120, screenshot
Status: Open | Fixed (commit/file) | Retested ✅
```

## Output
Prioritised issue list + test totals + retest status + "not tested" list (be explicit about what could not be verified).


## Current user scope — 2026-10-21
For the five-world campaign task the user's instruction overrides the generic broad-suite step above: test gameplay only in BedroomHub, DreamGalaxy, Dream_World1, Dream_MidnightPalace, Dream_ToyBoxMoon, Dream_BubbleBathSea and Dream_MidnightTeacup. Do not run unused legacy/test scenes or map-inventory checks. Use focused PlayMode filters and archive before/after results.

Keep physical traversal/normal E targeting separate from staged encounter positioning/dev states and from human keyboard play. A phase-transition probe with extra health does not validate normal-health survivability. Synthetic held input must be kept active when the Editor loses focus, with prior input settings restored afterward. Never silently increase timeouts, teleport to call a route complete, or invoke win to mask a failure.

See docs/CAMPAIGN_FIVE_REPORT.md and the project dream-rush-audit skill for the current route and source safety.
