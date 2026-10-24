# LEVEL 2 — MIDNIGHT PALACE (replaces Dreamland's chair hunt)

User brief (2026-10-20): level 2 was boring; it no longer needs the chair. Persona-5 direction, a super cool boss,
an Elden-Ring-like fight is OK but it must look cool; open-world rule: **every 10 s the player meets something new,
beautiful and inviting**; Pip's art style changes per level; music and VFX must stop feeling generated.

## 1. Design review (game-design-review skill)
```
### Design Review: level-2 combat kit + COUNT MIDNIGHT
Verdict: ACCEPT (with the "snooze" finisher below)
Pillars served: A Movement Is Fun (jump/glide/pound still matter in the fight), B The Boss Is a Problem
Choice & trade-off: attack vs. keep stamina for a roll; lock-on (precise, narrow view) vs. free camera (see bullets);
  jump a low sweep (no stamina, risky timing) vs. roll (costs stamina, i-frames).
Interactions/risks: PlayerMotor3D (FaceDirection, MoveSpeedScale), PlayerAbilities (dash disabled → roll),
  SleepStability moons (+ blanket shield), input map (Attack/LockOn/tap-Sprint roll), camera (lock framing).
Dominant-strategy check: roll spam is capped by stamina; cheese from range impossible (melee only);
  a pure HP race is NOT how he dies — 0 HP opens the SNOOZE finisher (land on / hit the dial), posture breaks
  open criticals → the fight stays a readable problem (GDD §1.3 B), not just a damage sponge.
Feedback plan: red blade glow + yelp on every wind-up, crouch = low sweep, flicker = delayed swing,
  boss bar + posture bar, DREAM DODGE slow-mo, hit-stop, kneel + "POSTURE BREAK" toast, STOP THE CLOCK card.
Acceptance criteria:
- [x] 3-hit combo lands, costs stamina, builds posture (CM-02)
- [x] low sweep: standing = hit, jump = clear; timed roll = no hit + perfect dodge (CM-03)
- [x] posture break → kneel → critical on the dial (CM-04)
- [x] 50 % → STOP THE CLOCK bullet hell with gaps → phase 2, colour returns (CM-05)
- [x] 0 HP → SNOOZE finisher by landing on the dial (CM-06)
```
Decision Log: an HP bar boss is allowed for level 2 because the kill is still a mechanic (snooze the dial), and
the chair stays the World-1 boss (GDD scope note).

## 2. Controls (level 2)
| Action | Keyboard/Mouse | Gamepad |
|---|---|---|
| Light combo / hold = charged leap slam | LMB / J | RB |
| Dodge roll (no input = backstep) | tap Shift, Ctrl | tap LT, RT |
| Lock-on | R / middle mouse (also F / RMB) | R3 (also LB) |
| Jump / double jump / glide / pound | Space / Space / hold Space / C | A / A / hold A / B |

## 3. Combat numbers (prototype)
Stamina 100 (regen 62/s after 0.45 s) · roll 24 (i-frames 0.02–0.34 s, perfect ≤0.18 s) · light 11 · heavy 28.
Leaf blade 24/24/36 dmg, 10/10/18 posture, range 2.25 m, 110° arc · leap slam 70–112 dmg, 2.7 m radius.

## 4. COUNT MIDNIGHT — "The Twelfth Hour"
6 m clock knight: grandfather-clock body with a swinging pendulum, a clock-face head wearing a phantom-thief mask
(its hands show the player's real time), a night-sky cape, the HOUR hand (broad) and MINUTE hand (long) as swords.
HP 1400 · posture 300 · 6 moons for Pip.
- P1: HOUR CLEAVE (+delayed variant, shockwave line → jump), MINUTE SWEEP (low 360° → jump), TICK-TOCK (3 steps),
  PENDULUM LUNGE (13 m thrust → roll sideways).
- 50 %: STOP THE CLOCK — leaps to XII, world turns grey, bullet patterns: CLOCK RING (gapped numeral rings),
  GEAR SPIRAL, HAND RAIN (marked circles, one aimed at Pip), TICK WALL (Undertale wall with a moving gap).
- P2: faster, more delays, + GEAR BURST and TICK-TOCK→CLEAVE combo.
- Posture break → kneel 4.5 s → hit/land on the dial = CRITICAL (240). 0 HP → SNOOZE IT finisher → victory.

## 8. M14 — combat depth (user 2026-10-20: "monsters look the same, I fight the same one, Pip only goes hit-hit-hit")

### 8.1 Pip's move set
| Move | Input | Answer to |
|---|---|---|
| Leaf combo ×3 | LMB / J / RB | basic pressure |
| Charged LEAP SLAM | hold attack | Bell Brute guard, groups |
| AIR SPIN (×2 per jump) | attack in the air | Cuckoo Snipers, juggles |
| RISING SLASH | jump right after a landed hit | launcher → air combo |
| DASH THRUST | attack during / ≤0.25 s after a roll | piercing lunge, punish Gear Roller recovery |
| COUNTER ×2 | attack ≤0.8 s after a DREAM DODGE | rewards perfect timing |
| DREAM BURST | V / pad Y with a full DREAM meter (hits +7, perfect dodge +25) | crowd reset, launches everyone |
The blade now sits in Pip's right mitt (BladeRig turns the arm with the swing). (The old card binding on D-pad up was removed with the card system on 2026-10-23.)

### 8.2 Enemy roster (EnemyBase: HP, flash, knock-back, juggles, stun, ground-following, leash)
| Enemy | Silhouette | Behaviour | Counter |
|---|---|---|---|
| TICK-TOCK | small alarm clock, mask | waddle → hop slam ring | jump/roll, then free hits |
| BELL BRUTE | 2.6 m navy tank, brass shield face | front guard; RING POUND shock wave; BELL CHARGE | go behind, slam, jump the ring, roll the charge, punish the pant |
| GEAR ROLLER | spiked gear, cyclops eye | revs then rolls 14 m (armoured) | side-step; lure it into walls → dazed, ×1.6 damage |
| CUCKOO SNIPER | winged cuckoo-clock | hovers 4 m up, 3-orb fans | roll through, SLASH an orb back (homes on its sender), air spin |
Placement: mixed street squads (TTT, TTG, TTC, TTB, TTG), two Brutes guard the tower gate, Cuckoos over the rooftop run, Rollers on the glass canal, Ambush waves TTT → TGTC → BTTG.
