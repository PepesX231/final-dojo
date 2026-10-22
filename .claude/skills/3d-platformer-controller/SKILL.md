---
name: 3d-platformer-controller
description: Build, tune or validate the DREAM RUSH apple player controller and its cameras (movement, jumping, coyote time, jump buffer, ground detection, camera-relative input, camera collision, blob shadow). Use whenever player movement or camera code changes.
---

# 3D Platformer Controller — DREAM RUSH

## Purpose
Keep the apple child's movement responsive, reliable and readable (GDD §9, §10, §20.1).

## Relevant files
- `Assets/DreamRush/Scripts/Runtime/Player/` — `PlayerMotor3D`, `JumpAssist`, `MovementMath`, `JumpMath`, `PlayerMotorConfig`, `PlayerInputReader`
- `Assets/DreamRush/Scripts/Runtime/CameraSystem/` — `ThirdPersonCameraController`, stage camera
- `Assets/DreamRush/Scripts/Runtime/Presentation/BlobShadow.cs`
- `Assets/DreamRush/Tests/EditMode/Player*`, `Assets/DreamRush/Tests/PlayMode/Player*`
- Scene `Assets/DreamRush/Scenes/Test_MovementGym.unity` (rebuild: menu `DreamRush/Build/Movement Gym`)

## Non-negotiable behaviour
| Feature | Requirement |
|---|---|
| Input | Camera-relative, projected on ground plane; analog magnitude respected; motor never reads Input System directly (uses `PlayerCommand`) |
| Jump | `v0 = √(2·g·h)` from **base** gravity; variable height (jump-cut gravity while rising & button released); no double jump |
| Forgiveness | Coyote 0.12 s, buffer 0.14 s (config); buffered jump on landing reaches the same full height |
| Gravity | Fall multiplier only when vy<0; clamp terminal speed; ceiling hit zeroes upward vy |
| Ground | Snap down ≤ snap distance when walking off slopes/steps; no bouncing on 35° descent |
| Walls | No speed stored while pushing into a wall |
| Rotation | Rotate toward move direction at config deg/s; never snap 180° in one frame unless intentional |
| Camera | Collision via sphere cast ignoring player; fast pull-in, smooth ease-out; pitch clamped; sensitivity configurable |
| Shadow | Blob shadow on the surface below the player at all heights |
| Responsiveness | Jump starts on the frame of input (or landing for buffered) — animation never delays it (§20.2) |

## Procedure
1. Change config values in `PlayerMotorConfig` (asset), not hard-coded numbers.
2. Run EditMode + PlayMode tests (DevBridge: `tests EditMode`, `tests PlayMode`). All MOV-xx must pass.
3. Play `Test_MovementGym`: walk every station (steps, gaps, slopes, tunnel, camera wall, coyote ledge). Record the jump envelope: max step height reachable, max gap at walk & sprint.
4. If level geometry depends on jump distances, update the envelope table in `docs/MILESTONE_REPORTS.md`.

## Quality checks
- [ ] All MOV tests green (paste actual counts)
- [ ] Measured full-jump apex within ±0.1 of `jumpHeight`
- [ ] Short tap ≥30% lower than full jump
- [ ] No stuck spots / tunnelling in gym
- [ ] Camera never shows inside of walls in gym corridor
- [ ] Controller and keyboard both work (if a gamepad is available; otherwise state "not tested")

## Output
Implementation diff summary + config values + test results + measured envelope + known issues.
