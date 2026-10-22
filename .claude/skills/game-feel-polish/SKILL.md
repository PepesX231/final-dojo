---
name: game-feel-polish
description: Improve and verify the responsiveness and emotional impact of DREAM RUSH controls and interactions — jump arc, acceleration, landing feedback, squash/stretch, camera response, audio feedback and readability. Use when tuning feel or after a playtest complaint about controls.
---

# Game Feel Polish — DREAM RUSH

## Principles (GDD §20)
- Response first: feedback layers on top of an already-correct mechanic; never delay input for animation.
- Selective exaggeration: squash/stretch, anticipation, follow-through — small, fast (≤0.15 s for landing squash recovery to 80%).
- Camera feedback is subtle: small landing dip, no constant shake; respect "reduce screen shake" setting.
- Every action has 3 channels: motion (anim/transform), visual (VFX), audio (SFX). Missing channel = note it.

## Relevant files
`PlayerMotorConfig` asset · `PlayerSquashStretch` · `AppleProceduralAnimator` · camera controllers · VFX/Audio managers

## Procedure
1. **Baseline**: record current values + a short description/gif/screenshot of the feel. Note the specific complaint.
2. Change one variable group at a time (acceleration group, jump group, air group, camera group, feedback group).
3. Re-run movement tests (feel changes must not break MOV-xx).
4. Play the gym loop: run → jump gap → land → turn → brake → step-up. Rate 1–5 for: responsiveness, jump arc, landing satisfaction, turning, camera comfort.
5. Keep or revert. Log before/after.

## Useful targets (starting points, validate in play)
| Metric | Target |
|---|---|
| Time to full walk speed | 0.15–0.25 s |
| Stop time from walk | 0.12–0.2 s |
| Full jump air time (flat) | 0.7–0.9 s |
| Apex hang | fall gravity ×1.3–1.6 |
| Landing squash | scale Y 0.75–0.85, recover ≤0.15 s |
| Jump stretch | scale Y 1.1–1.2 |

## Output
```
### Feel Pass: <area>
Before: values + rating
Change: ...
After: values + rating
Tests: MOV pass x/y
Keep/Revert: ...
```
