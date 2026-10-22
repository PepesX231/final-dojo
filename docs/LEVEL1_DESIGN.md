# World 1 — "The Chair Who Wouldn't Be Sat On" (Level Design v2)

> Replaces the single-arena P0 layout as the *player-facing* first level. Dream_Chair.unity stays as the
> automated-test prototype (all P0 tests keep running on it). The new level is a separate scene: **Dream_World1.unity**.
> Sources and reasoning: `docs/RESEARCH_V2.md` §4.

## Core idea — the dream is a book whose chapters are drawn in different styles

Pip falls asleep, and the dream turns into a picture book. Every time the chair escapes into a page, the book flips
to a new **chapter drawn in a different art style**. The apple, the chair and the paper "page-portal" frame stay
constant, which keeps the mash-up coherent instead of random.

| # | Chapter | Art style (reference spirit, all original) | New mechanic (teach → develop → twist → drop) | Length |
|---|---|---|---|---|
| 0 | **Dream Fall** | starry nebula, floating pages | falls past pages, lands on the open book (cinematic + movement warm-up) | 0:30 |
| 1 | **The Pop-up Picture Book** | soft storybook 3D (our Applebook look) | **pop-up pages**: hinged pages flip up into stairs → fold back on a timer → catapult pages fling you | 4:00 |
| 2 | **The Paper Theater** | 2.5D diorama, golden spotlights, tilt-shift DOF (HD-2D spirit) | **side-scrolling stage sets**: sliding cardboard sets → crumbling cardboard bridge → step "behind the scenery" to swap layers | 3:30 |
| 3 | **The Toy Galaxy** | floating toy planetoids in a nebula (Galaxy spirit) | **launch cushions**: bounce → chained launches between islands → moving target islands | 3:30 |
| 4 | **The Phantom Shelf-tops** | red/black/white comic panels, halftone, bold slashes (Persona spirit) | **the chase**: the chair runs ahead; books and toys roll at you; getting hit = lose distance; Dream Laws flip the rules | 3:00 |
| 5 | **Cathedral of the Throne** | gothic fog, candles, giant hall, ominous boss card (Souls parody) | **boss: THE CHAIR, Throne of Refusal** in 3 phases | 5:00 |

Total first clear ≈ 20 min. The main path stays forgiving (Astro Bot rule); harder optional routes hold the
**3 Dream Stars per chapter** (collectibles shown on the galaxy map).

## Landmark ("weenie")

**The Throne Tower**, a stack of giant books topped by a glowing throne, can be seen far away in the sky of every
chapter, in the direction of travel. It is the player's goal from the very first second.

## Flow and checkpoints

- Each chapter ends at a **page-portal**: a giant standing page you run into, which plays the page-turn transition.
- Checkpoints are **"pillow fires"** (a parody of a Souls bonfire) at every chapter start and mid-chapter. Falling
  into the void costs one Sleep moon and respawns you at the last pillow.
- The chair appears in **every chapter**: it kicks the pinky toe (ch1), runs across the background (ch2), waves from
  a far planet (ch3), gets chased (ch4) and is fought (ch5). It is a recurring character, not just a final boss.

## Chapter layouts (metres; each chapter has its own origin, so they never overlap visually)

### Ch1 Pop-up Picture Book — origin (0,0,0), footprint 44 × 110

```
 z=110  [PAGE-PORTAL]  ← chair dives into the book's spine after the pinky-toe kick
 z=95   chair glimpse + kick cutscene on the "Living Room" spread (the P0 Art Target, re-used)
 z=70   CATAPULT PAGES (twist): stand on a page that snaps up and flings you onto the high shelf
 z=45   TIMED PAGES (develop): a staircase of pages that fold flat again after 3 s, so hurry
 z=20   POP-UP STAIRS (teach): walk near → pages flip up into steps (no timer)
 z=0    landing spot (book centre, rug), pillow fire #1
```

Optional: a hidden page behind the sofa leads to Dream Star 1; a high catapult arc reaches Star 2 on the lamp shade.

### Ch2 Paper Theater (2.5D) — origin (0,0,400), runs along +X, depth 6 m

- The camera locks to the side on a rail (orthographic feel, FOV 30, distance 16).
- Layers: foreground cutouts (blurred DOF), the play strip, and backdrop flats. Golden spotlights with god rays.
- **Teach**: sliding cardboard platforms on stage rails (moved by "stagehand" ropes).
- **Develop**: a cardboard bridge whose planks fall a moment after you step on them.
- **Twist**: doors in the scenery swap you between the front and back layers, where the back layer's route continues.
- The chair is silhouetted in the backdrop spotlight, running the same way (storytelling).

### Ch3 Toy Galaxy — origin (0,0,800)

- Floating round islands (toy drums, blocks, a spinning top) in a nebula; star-bits as a trail of guidance sparkles.
- **Teach**: a bounce cushion to a near island.
- **Develop**: a launch cushion fires you along a dotted arc to far islands (no input needed, readable).
- **Twist**: the target islands orbit, so time your launch.
- A little stool patrols the final island (the P0 enemy).

### Ch4 Phantom Shelf-tops (CHASE) — origin (0,0,1200)

- A winding path on the tops of giant bookshelves at night, in red/black/white with halftone skies and bold slashes.
- The chair runs a spline ahead of you. If it gets more than 18 m ahead for 5 s, "IT GOT AWAY!" plays and you
  retry from the chase start.
- Hazards it throws back: rolling books (jump them), toppling dominoes, and a Dream Law that turns NO RUNNING on
  for 8 s. A hit = knockback + the chair gains distance (the pitch).
- It ends when the chair crashes through stained glass into the Cathedral.

### Ch5 Cathedral of the Throne (BOSS) — origin (0,0,1600)

- A fog gate, then a long hall of candles and banners (the apple crest), then a circular arena Ø 44 m with the
  **Throne Tower** (rotating book stack, 9 m) in the middle.
- Boss intro card in Souls style: **"THE CHAIR — Throne of Refusal"** in big serif letters, a slow fade, and a gong.

## The boss — skills that make it as hard as possible to reach (but always fair and telegraphed)

| Phase (fatigue) | Skills | Telegraph (anticipation) | Counter-play |
|---|---|---|---|
| **P1** 0–33% | Flee + poof-reposition (P0 AI) · **Shockwave Stomp**: hops and slams, an expanding ring travels outward | backrest arches, 0.6 s, red ring decal | jump over the ring; you lose distance if hit |
| **P2** 33–66% | + **Cushion Volley**: fires 3 cushions in arcs · + **Musical Chairs**: 4 decoy chairs pop out and shuffle for 4 s | red landing circles 1 s before impact · music-box jingle | dodge the circles; the *real* chair is the one with the gingham cushion (decoys have plain seats); sitting on a decoy poofs it and knocks you back |
| **P3** 66–100% | **Throne Climb**: the chair hops to the top of the Throne Tower and throws pillows down while the book stack rotates | the chair pulls back its leg, 0.5 s | climb the rotating books; at the top it is exhausted → sit 2 s → win |

- Every card still works through Dream Logic (glue roots it, the flash freezes it, the rod even pulls it down off the
  tower).
- At the end the boss turns silly (Astro Bot rule): it bursts into tears, offers its seat, and confetti falls.

## What stays from P0

The chair AI (flee, fatigue, openings, sit 2 s), cards, Dream Laws, Sleep Stability, bed upgrades, save, audio —
all reused. The new level adds content and skills on top of them.

## Build order

1. Close camera + goofy animation (felt everywhere)
2. Look: volumes, light shafts, VFX library
3. UI v3
4. Galaxy select
5. Ch5 Cathedral boss — the payoff
6. Ch1
7. Ch4 chase
8. Ch3
9. Ch2

Each chapter goes block-out → test → art; tests are added per chapter.
