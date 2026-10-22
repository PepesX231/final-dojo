# DREAMLAND — Open World (Design v1, 2026-10-17)

User brief: "ขอแมพใหญ่ open world หนี มันๆ เมคคานิกโหดๆ สวยๆ VFX/ภาพแบบ Final Fantasy".
Scene: `Dream_Openworld.unity` (built by `OpenWorldBuilder`), entered from the Dream Galaxy (planet "DREAMLAND").

## Pillars
1. **Big & beautiful** — 640 × 640 m painted field at golden hour: rolling meadows, crystal cliffs, a lake with
   waterfalls, ancient ruins, floating islands, giant glowing crystals (FF field spirit, our toon language).
2. **Hunt and be hunted** — you chase THE CHAIR across the world; THE ALARM (a giant nightmare alarm clock) hunts *you*.
3. **A move set with skill ceiling** — double jump, dash (momentum carry), glide, ground pound + pound jump,
   speed rings, updrafts. Every move has VFX and SFX.

## Loop
- The chair carries **3 Dream Keys**. It sits in a region until you come close, then flees to a far landmark
  (NavMesh, scored away from you). It tires after a long run → *exhausted* (gold "!" + sweat) → sit 2 s → it drops a
  key, poofs, and reappears in another region. 3 keys = victory ("SWEET DREAMS").
- A **compass strip** (top centre) always shows the chair (red), the alarm (purple, only when near), pillow fires.
- **Pillow fires** = checkpoints and **safe zones** (the alarm will not enter their light).

## THE ALARM (nightmare)
| State | Behaviour | Readability |
|---|---|---|
| Roam | wanders between waypoints, ticking | ticking SFX, glowing yellow eyes |
| Notice | player within 34 m and in its 110° view, not in a safe zone → 0.8 s wind-up | bells shake, "!!" , screen edge pulse |
| Chase | runs at 9.5 m/s (walk 5.5, sprint 8, dash bursts 19) — you must dash, glide and use terrain | red vignette, ringing |
| Slam | when within 7 m: jumps and slams (ring shockwave, jump it) | 0.6 s crouch telegraph |
| Lose track | > 48 m or line of sight broken for 4 s, or you enter a safe zone | it stops and "searches" |
| Stunned | ground-pound within 5 m of it or a slam it misses into a crystal → 3 s dizzy | stars, can run past |
Contact = −1 Sleep moon + big knockback. Moons run out → wake up (defeat).

## Move set (PlayerAbilities)
| Move | Input | Notes |
|---|---|---|
| Double jump | Space in air | flip, lavender ring |
| Dash | L-Ctrl / RT | 1 per air; 0.65 s cooldown on ground; keeps 11 m/s after (sprint-jump-dash tech) |
| Glide | hold Space while falling | pillow parachute, 2.3 m/s fall, steer 9.5 m/s; updrafts lift you |
| Ground pound | C / B in air | hang 0.14 s, slam; stuns the alarm / trips the chair within 5 m |
| Pound jump | jump within 0.3 s of a pound landing | 1.5× height |
| Speed rings | fly through | +boost along the ring axis, chained routes |

## Regions
| Region | Where | Look | Play |
|---|---|---|---|
| Starfall Meadow | centre (spawn) | golden grass, lone trees, crystal stones | learn moves, first chair sighting |
| Crystal Cliffs | north | towering crystal spires, strata cliffs | climbing, updrafts up the cliff |
| Mirror Lake | east | lake, waterfalls from floating islands | glide between islands, speed-ring route |
| Clockwork Ruins | west | ancient arches and pillars | the alarm's lair, tight escape routes |
| Pine Hills | south | pine forest on rolling hills | hide from the alarm among trees |
