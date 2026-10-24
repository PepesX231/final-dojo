# MAMA'S COMING! — two-storey fruit-scale house (first level)

2026-10-24. Rebuilds `MamasKitchen` (the real networked round, not a study scene) as a house twice the size of the old plan, with a second floor, and grows the round from about 28 to 50 problems.

## Scale and plan

- `HouseLayout.Scale = 2`: the old 30 x 14 m plan, its furniture and fixtures are rebuilt at 2x, so the fruit kids are small in a big house. Carried props (pot, mop, bucket, towels, cups, teddy...) and the characters keep their own size; chairs grow with the furniture.
- Ground floor (60 x 28 m): living room, laundry, dining, garage along the front; staircase hall, bathroom, kitchen, game room along the back; a hallway through the middle. Ground walls 5.6 m, doors 4.4 m.
- Upper floor (x 0..30, top of slab at 5.85 m): kid's bedroom (moved up from the ground floor and turned so its door faces the upstairs hall), Mom's room, party loft (the DJ rig moved up from the game room), landing and upstairs hall.
- Circulation: one staircase from the ground-floor staircase hall to the landing, and a one-way laundry-chute hatch in the upstairs hall that drops you onto a crash mat in the ground hallway. Up by the stairs, down by the chute = a loop instead of a dead end.
- Water and smoke still simulate on the ground floor only (`FloodSim`/`SmokeSim` are 2-D grids). Every water/smoke problem stays downstairs.
- Flat roof terrace for the exterior; the camera never shows it in play. While your kid is downstairs `FloorCutaway` stops rendering everything on the upper floor (colliders stay).

## Two-floor rules in the runtime

- `HouseLayout` owns the room list (14 rooms, with floors), `RoomOf`, `FloorOf`, `FloorYAt`.
- Interaction targeting ignores anything more than 3 m above/below you. Mom only notices mess, hiding spots and water on the floor she is on. Water warnings only count downstairs rooms.
- Mom's route climbs the stairs (route points carry height). The hamster's run network includes the stairs.
- Splats, extinguisher powder and the hallway runner land on whichever floor they are on.

## The 50 problems

Existing (kept, moved with their rooms): bathtub flood, main water valve, clogged drain, curry/gas stove fire, smoke alarm, smoke venting (windows + fan), mopping floodwater, breaker/power cuts, water heater (two valves together), burst kitchen pipe (wrench), toaster-oven pizza, open fridge, cake sparklers, cracked fish tank, saving the fish, DJ speakers (now in the loft), soap-foam washer, clogged toilet (plunger), iron on Mom's dress (now upstairs), loose hamster (now runs the stairs), cake on the ceiling fan, broken vase, flipped sofa (two friends), trash bags, party cups, pizza boxes, confetti/soda splats, putting things back / hiding spots.

New (`GadgetHazard`, `MamaLevelBuilderGadgets.cs`):

| # | Where | Problem | Twist |
|---|---|---|---|
| 29 | Living | Dusty the robot vacuum shooting confetti | runs away from you; corner it, then Simon |
| 30 | Living | Boiling lava lamp | timing; fails into a fire |
| 31 | Living | Howling karaoke machine | annoys the neighbours |
| 32 | Kitchen | Ice-maker avalanche | needs the wrench (also needed by the burst pipe); melts into floodwater |
| 33 | Kitchen | Blender with no lid | needs the pot lid (also needed for the grease fire) |
| 34 | Kitchen | Spoon in the garbage disposal | tug; fails into a power cut |
| 35 | Garage | Haunted garage door | two buttons at opposite walls, pressed together |
| 36 | Hallway | Grandfather clock chiming 13 | fail = Mom comes home 45 s earlier |
| 37 | Laundry | Crayons in the dryer | rainbow smoke and wax splats |
| 38 | Dining | Mango the parrot screaming "PARTY!" | cover the cage with a towel, or Mom hears it (severity 3) |
| 39 | Game room | Popcorn machine | mash; fails into a grease fire |
| 40 | Bedroom | Gloop the escaped glow slime | runs away; scoop it with the bucket (carry it upstairs) |
| 41 | Bedroom | Bubble machine | tug the plug; soapy floor |
| 42 | Bedroom | Collapsing pillow fort | hold to prop it up |
| 43 | Upstairs hall | Smart speaker recorded the party | wires; fail = it messages Mom, she comes home earlier |
| 44 | Mom's room | Glitter bomb | needs the mop (carry it upstairs) |
| 45 | Mom's room | Turbo treadmill | two switches together: hold the belt + hit stop |
| 46 | Mom's room | Streamers on the chandelier | drag a chair upstairs to reach |
| 47 | Party loft | Telescope aimed at the neighbours | timing; annoys the neighbours |
| 48 | Party loft | Disco foam cannon | only safe with the power OFF: one friend trips the breaker in the garage, one fixes it upstairs |
| 49 | Landing | Smoking hoverboard charger | tug; fails into a fire |
| 50 | Upstairs hall | Beanbag jammed in the chute shortcut | plunger; until then the shortcut is blocked |

Shared tools create real co-op tension: one lid, one wrench, one plunger, one bucket, one mop exist for several problems on different floors.

## Rebuilding

`Mama's Coming > Build House Level` (or `MamaTwoStoreyReview.BuildAndCapture` in batch) regenerates the whole scene from code and re-runs the art passes (`MamaVisualPolish`, `MamaRecastBuilder`). Any hand edits made directly in the old scene are not carried over; the previous scene stays in git history.
