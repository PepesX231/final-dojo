---
name: game-design-review
description: Review a proposed DREAM RUSH gameplay mechanic, enemy, boss behaviour or level beat before it is implemented. Use before adding any significant mechanic or changing a core rule.
---

# Game Design Review — DREAM RUSH

## Purpose
Check that a proposed mechanic supports fun, replayability and coherent player decisions, and stays true to `docs/GDD.md` (Source of Truth) and the pillars in GDD §1.3.

## When to activate
- Before implementing a new mechanic, enemy, boss state or victory method
- When a playtest reveals a dominant strategy or confusion
- When the user's brief conflicts with the GDD (record the resolution in the plan's Decision Log)

## Relevant files
`docs/GDD.md` (§1.3 pillars, §2 research, §11–19 chair level, §18 dream logic; §17 cards are historical, removed 2026-10-23) · `docs/P0_IMPLEMENTATION_PLAN.md` · `docs/TEST_PLAN.md`

## Procedure
Answer each question in 1–3 sentences. If any answer is "unknown", the mechanic is not ready.
1. **Player problem** — what problem/feeling does it solve or create? Which pillar (A–F) does it serve?
2. **Meaningful choice** — what decision does the player now make that they did not before? Is there a cost/trade-off?
3. **Interactions** — list every existing system it touches (movement, camera, environmental mechanisms, statuses, chair FSM, Sleep Stability, save). Any rule it breaks?
4. **Dominant strategy** — can it trivialise the chair or make other solutions pointless? Does it bypass "player must still approach and sit" (§18.6)? If yes → redesign.
5. **Readability** — can the player tell, without text, when it is active and why it succeeded/failed? Which feedback (anim/VFX/SFX/UI) carries it?
6. **Scope** — is it required for P0? If not, defer and note it.
7. **Test** — write the acceptance criteria and at least one automated test idea.

## Red flags (reject or revise)
- Scripted instant wins (`if mechanism == X && boss == Chair → win`)
- Randomness that can produce a useless draw in the first run
- Mechanics that need a long text explanation
- Visual polish standing in for missing gameplay

## Output format
```
### Design Review: <mechanic>
Verdict: ACCEPT | REVISE | DEFER | REJECT
Pillars served: ...
Choice & trade-off: ...
Interactions/risks: ...
Dominant-strategy check: ...
Feedback plan: ...
Acceptance criteria:
- [ ] ...
Tests: ...
```
