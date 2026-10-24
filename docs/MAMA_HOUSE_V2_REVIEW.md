# Mama house revision: roof, spatial layout, 50 prototype chores and visible strides

2026-10-24. Supersedes the compact 12-task House Study after the user's visual review rejected floating props, poorly oriented furniture, no roof and weak walking.

## Skill used and provenance

Installed `level-design` from [gamedev-skills/awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/level-design) with the bundled skill-installer. Local entry: `C:/Users/User/.codex/skills/level-design/SKILL.md`; read its pacing-and-flow reference. It is available for automatic discovery on the next turn; its instructions were read and applied directly in this turn. No third-party executable was run as part of the skill itself.

Applied the skill's player-metrics-first guidance, authored circulation, calm landing, dependency reachability and visual review. Installing a skill is not evidence of art quality or human playtest. The reference documents are instructions for the workflow, not a replacement for the user's larger-home request.

## House and circulation

- Old study: 259 square units of actual walkable floors. Revised: 808, **3.1197x**, measured from authored floor collider bounds. The character stays 1.15 units tall; furniture is not scaled up to enlarge the house.
- Ground floor: living, entry, dining, family hall, stair hall, play room, garage, kitchen, utility. Upper floor: kids bedroom, bathroom, Mama bedroom, upper hall, gallery, study, hobby room and landings.
- Storey 3.0, exterior/interior wall 2.85, door opening 1.9 wide with 2.19 clearance. Stairs rise 3.0 over a 6.0 run, about 26.6 degrees, with a separate controller ramp, treads and banister.
- Main gabled roof, lower kitchen/utility roof, porch canopy, filled gables, standing seams, ceilings and chimney are saved geometry. Exterior windows are genuine openings with sills/mullions. The previous opaque blue window panels are gone.
- Main circulation: entry -> family hall -> play/garage -> utility -> kitchen -> family hall. Upper route: stairs -> top landing -> gallery -> study / upper hall -> bedrooms / bath / hobby. The gallery and wide hall are calm navigation space, not rows of quest kiosks.
- Dining chairs are assemblies rotated toward their table, with legs placed on their floor. Tables have four legs at actual tabletop edges. Gallery photos mount on a solid wall. Cake is mounted on a physical fan pedestal/blades; no free-floating spinning cake.
- Pickup and delivery points snap to actual support colliders. Mattress/quilt surfaces support deliveries. Held objects are not hidden simply because they originally spawned upstairs.

## Controls and presentation

Open `Assets/MamasComing/Scenes/MamaHouseStudy.unity` and Play. 1â€“5 pick fruit, F6/F7 cycle, F8 slip; empty hands/upright required for switching. E interact/hold, Q place, RMB throw, G shove. F9 overhead plan of current floor, F10 exterior with the entire roof visible. TAB shows a scrollable list; ordinary gameplay has contextual prompts, not a 50-row screen overlay.

The roof is present in the saved scene and exterior mode. Interior mode hides roof renderers and overhead upper-floor renderers to preserve the orbit camera view; collision remains. `MamasKitchen` and build settings are not replaced.

## Walking and animation

Added `ReadableFruitGait` between imported Animator evaluation and carry/body presentation. Handles Banana's native bones plus the object-animation foot/glove groups used by Orange, Watermelon and WaxApple. Strides move each foot forward/back in world-aligned player space, lift the swing foot, and counter-swing free arms. Banana keeps its native body animation/carry rig. KidBody supplies whole-body sway and owns curling/tumbling after the gait layer. Apple retains its existing procedural walker.

Validation measures root-relative foot separation and lift over multiple real W-input frames, rather than just root displacement. Exported close-up frame sequences permit visual inspection. These are staged walks; transition aesthetics, difficult stairs/carry combinations and online matching still need human review.

## Prototype task catalog

There are 50 executable authored task instances in this separate study, using four shared mechanic families. They are **not 50 unique bespoke puzzle systems**, and are not promoted into the main networked disaster round. The original hazards/flood grid and Mother inspection route still depend on the old flat-house coordinates. Pacing, escalating consequences, shared two-machine task state and final task-specific art/audio remain integration work.

Delivery tasks require the correct physical pickup. Repairs have three hold-to-work stages. Timing requires three distinct green sweeps and resets on a miss. Relay latches remain open 35â€“45 seconds, allowing a solo route while testing handoff opportunities; they are not mandatory two-player gates. Dependencies are tested for a completion ordering.

| # | Authored challenge | Shared mechanic |
|---|---|---|
| 1 | Latch main water | Relay |
| 2 | Seal kitchen pipe | Sequence |
| 3 | Restore router signal | Timing |
| 4 | Return lounge cushion | Delivery |
| 5 | Bag rubbish in utility | Delivery |
| 6 | Save leftover pizza | Delivery |
| 7 | Bring teddy to bed | Delivery |
| 8 | Return escaped fish | Delivery |
| 9 | Latch washer drum | Relay |
| 10 | Load missing bath towel | Delivery |
| 11 | Balance cake fan | Timing |
| 12 | Bring bike repair tools | Delivery |
| 13 | Bring controller to game table | Delivery |
| 14 | Return picture to entry bench | Delivery |
| 15 | Put spare cushion on kids bed | Delivery |
| 16 | Return Mama's cushion | Delivery |
| 17 | Store craft tools | Delivery |
| 18 | Take spare pipe to garage | Delivery |
| 19 | Put vase on bedside table | Delivery |
| 20 | Sort second rubbish bag | Delivery |
| 21 | Return kitchen towel | Delivery |
| 22 | Carry picnic leftovers | Delivery |
| 23 | Rescue plush from garage | Delivery |
| 24 | Return hobby-room bucket | Delivery |
| 25 | Put pump on garage bench | Delivery |
| 26 | Move birthday cake to table | Delivery |
| 27 | Carry extinguisher downstairs | Delivery |
| 28 | Retune noisy television | Timing |
| 29 | Tighten dining chair | Sequence |
| 30 | Level the game table | Timing |
| 31 | Adjust garage brake cable | Sequence |
| 32 | Prime utility pump | Timing |
| 33 | Reset oven timer | Timing |
| 34 | Secure fridge handle | Sequence |
| 35 | Unblock bath drain | Sequence |
| 36 | Tune study radio | Timing |
| 37 | Fix craft stool | Sequence |
| 38 | Tighten kids bed frame | Sequence |
| 39 | Quiet Mama's wardrobe | Timing |
| 40 | Unstick bathroom drawer | Sequence |
| 41 | Balance dryer load | Timing |
| 42 | Repair porch hinge | Sequence |
| 43 | Synchronise living lamp | Timing |
| 44 | Latch hobby clamp | Relay |
| 45 | Secure hobby shelf | Sequence |
| 46 | Latch kitchen supply | Relay |
| 47 | Seal bathroom tap | Sequence |
| 48 | Sort study cable plugs | Sequence |
| 49 | Tune game-room speaker | Timing |
| 50 | Final family-room sound check | Timing |

## Evidence and practical limits

`docs/evidence/mama-house-v2/` contains the final review, compiler result, exterior/interior/plan captures and walking animations. The automated review uses real virtual-keyboard input for fruit changes, W movement, slips, camera toggles and one E pickup/delivery. The remaining task completion checks are direct method/contract tests, not 50 end-to-end human interactions. Furniture/support geometry, dependency reachability and stair motor traversal have separate checks. Camera captures exclude screen-space HUD.

No human multiplayer session, final visual approval, standalone build or target-hardware performance measurement is claimed. House scale, layout and the visible walk frames should be reviewed in Play mode before integrating the main simulations. No commit or push is included in this follow-up.

## Final recorded results

- House revision: **175 checks passed**, 2026-10-24 02:18:39. Includes support after physical delivery and current-floor visibility after cross-storey delivery.
- Existing Banana carry/rig review: **33 passed**, 02:14:44. Existing MamasKitchen UX/input review: **62 passed**, 02:15:59.
- Latest compilation of both game/runtime and Editor assemblies: **0 errors / 0 warnings**. MCP vendor startup notices are separate from compiler diagnostics.
- The first visual review exposed cancelled gable normals in reused mesh assets and a cached-floor membership bug for moved props. Gable buffers/normals now update explicitly; prop visibility follows held state/current height rather than initial parent. Final exterior and interior captures were re-inspected after these fixes.
- Original MamasKitchen scene and EditorBuildSettings have no diff in this follow-up. Editor left on the revised separate house in Edit mode; Play starts a fresh untimed study with the original selected fruit restored by the test harness.
