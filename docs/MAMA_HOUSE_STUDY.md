> Historical first study. Superseded by [the roofed 50-task house revision](MAMA_HOUSE_V2_REVIEW.md) on 2026-10-24 after user feedback. Current scene uses the revision.

# Mama: fruit controls and two-storey house study

2026-10-24. User requested fruit test keys, whole-fruit tumbling, and a separate furnished house layout with more varied chores.

## How to play

Open `Assets/MamasComing/Scenes/MamaHouseStudy.unity` and press Play. This is an untimed layout/interaction sandbox with its own small HUD. It does not replace `MamasKitchen` or change build scene order.

- WASD / Shift / Space: move, run, jump. E: interact or hold. Q: place. RMB: throw.
- Development/Editor shortcuts: 1 Apple, 2 Banana, 3 Orange, 4 Watermelon, 5 Wax apple. F6 next / F7 previous. Switch with empty hands while upright; blocked in minigames/settings. Selected fruit is saved only after a successful swap. The existing network pose already carries the fruit id.
- F8: deliberately slip to inspect the full tumble and recovery. This shortcut also works in the main scene.
- Study only: F9 switches between walking and an overhead plan of the current floor. TAB toggles the 12-task list. Esc releases the pointer; click to resume looking.
- Main scene: the quiet context card advertises fruit shortcuts when empty handed with no nearby interaction. Existing H/TAB contextual UI remains.

## House layout

The ground floor footprint is an L within 14 x 13 units, versus the previous 30 x 14 plan. The smaller upper wing sits over the lounge/entry. Ground floor: lounge, entry/porch, dining, kitchen, utility and stair/play hall. Upstairs: bedroom, bath, study, hall and landing. Recessed porch, patio notch, distinct room colors, window frames, skirting, family photos, cabinets, appliances, bed, desk, bookcase and balustrade make the purpose of rooms visible.

Child height remains 1.15 units. Storey height 2.8, wall height 2.5, door width 1.6 and lintel clearance 2.0. Stairs have decorative treads and a separate smooth 32.5-degree controller ramp. Upper renderers hide from downstairs while their colliders remain. Floor plans show room labels; ordinary gameplay shows contextual information nearby.

No floor-aware fire/flood simulation, mother inspection route or real network session is enabled in the study. The original simulations assume the old flat-house coordinates; attaching them here would produce incorrect cross-floor behavior. The main scene is preserved. Interaction targeting now rejects objects more than 1.85 units above/below the player, preventing grabs through upstairs floors.

## Twelve additional prototype challenges

These are playable prototypes in the study, not twelve released quests added to the main round. The existing problem count changes during a round; it is not a fixed 28-item quest catalog.

1. Latch main water valve: opens a 22-second repair window.
2. Kitchen pipe: three hold-to-work stages before the valve expires.
3. Router: repair prerequisite, then three separate green timing sweeps; misses reset progress.
4. Yellow cushion: physically return to the sofa.
5. Trash bag: physically deliver to the utility bin.
6. Leftover pizza: move from lounge to dining table.
7. Teddy: carry up stairs to the bedroom.
8. Fish: return the escapee upstairs to bath water (no fish survival simulation in this study).
9. Washer: latch the spin mechanism for a timed handoff.
10. Lost towel: retrieve from upstairs and deliver while the washer latch is open.
11. Cake/fan: three separate timing sweeps to balance it.
12. Bike tools: bring the toolbox from the utility room to the bicycle.

Friends can divide routes and latch/delivery duties in the eventual co-op integration; 22-second latches give solo players a completion path. The study itself has no multiplayer connection flow or state replication for these new tasks. Hazard escalation, scoring, delivery mounting/polish and task pacing need integration and human playtesting before promotion into the main game. The tasks are intended to test route choice and interruptions rather than increase the number of always-visible HUD rows.

## Tumbling

`KidBody` accumulates a quaternion from actual planar displacement and body radius, then rotates around the measured fruit core. The orientation continues rotating rather than holding an 82-degree face-down pose. Recovery interpolates from the final tumble orientation, pops the limbs and hops upright. `PlayerController` temporarily reduces ground grip while down and preserves an initial 4.2–6.5 unit/s slip impulse. Controller collision/gravity remain in charge; this is stylized rolling, not a new rigidbody ragdoll. Remote bodies derive tumble from replicated travel; actual two-machine visual agreement is untested.

## Validation

Evidence is in `docs/evidence/mama-house-study/`. Staged automated input/contract checks cover all five digit selections and W movement, F6/F7 cycling, F8 tumble distance/rotation, limb recovery, actual motor stair traversal, dependency/empty-hand rejection, timed latch expiry, delivery/timing completion and F9 plan switching. Contract tests directly invoke some chore methods; they are not all end-to-end E-input tests. Camera captures exclude screen-space/IMGUI HUD. No human, two-machine, standalone build or performance certification is claimed.

Final result: study review 70/70 passed (01:36:47); original MamasKitchen UX/input regression 62/62 passed (01:37:46). Both Assembly-CSharp and Assembly-CSharp-Editor compiled with 0 errors / 0 warnings. Vendor MCP startup notices are separate from compiler diagnostics. No commit or push performed for this follow-up.

