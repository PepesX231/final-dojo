# DREAM RUSH — THE APPLE DREAM
## MASTER GAME DESIGN DOCUMENT · ART BIBLE · TECHNICAL SPECIFICATION
### Version 2.0 — Production Blueprint for Claude Opus

> **Current design override — 2026-10-23:** User requested complete removal of the card system. There is no draw, selection, equipped card, card input, cooldown, reroll upgrade, or Dream Law card. Current loop: BedroomHub → sleep → DreamGalaxy → chosen world → BedroomHub. Solve encounters with movement, timing, interaction, combat, and authored environmental mechanisms. Older card sections below are historical design, superseded by this decision; do not rebuild them.

**Project codename:** DREAM RUSH  
**Status:** Pre-production / First Playable Design  
**Genre:** 3D Action Platformer / Roguelite / Absurd Boss Rush / Action Puzzle  
**Perspective:** 3D with curated 2.5D sections and integrated 2D artwork  
**Visual Identity:** Applebook Surrealism  
**Primary Engine:** Unity 6, Universal Render Pipeline  
**3D Asset Creation:** Blender  
**2D Asset Creation:** Krita, Photoshop, Affinity Designer หรือเครื่องมือวาดที่เหมาะสม  
**Programming:** C#  
**Platform:** Windows PC  
**Player Count:** Single-player  
**Primary Development Goal:** A polished, replayable first boss experience

---

# 00 — IMPORTANT INSTRUCTIONS FOR THE DEVELOPMENT AI

You are acting as an experienced indie game development team consisting of:

- Creative Director
- Lead Game Designer
- 3D Platformer Gameplay Engineer
- Technical Architect
- Level Designer
- Boss Encounter Designer
- Art Director
- Blender Technical Artist
- 2D Illustrator
- Animation Director
- Shader Developer
- UI/UX Designer
- Sound Designer
- QA Engineer

Your responsibility is to transform this document into a coherent, playable, visually distinctive game.

**Do not immediately attempt to implement the entire full game.**

First, create a production-quality Vertical Slice containing:

1. A controllable 3D apple protagonist.
2. A fully explorable apple-themed bedroom hub.
3. Environmental menu interactions.
4. A bed used as the entrance to the dream.
5. A dream transition.
6. A three-card selection system.
7. One polished platforming/tutorial level.
8. A chase encounter against a living chair.
9. Multiple ways to defeat that chair.
10. A victory/wake-up loop.
11. A basic permanent bed-upgrade system.
12. A consistent art direction and production pipeline.

Every feature must be evaluated against three priorities:

**PLAYABILITY → FUN → VISUAL QUALITY**

Visual polish must never be used to conceal incomplete gameplay.

Do not replace working systems with visual mockups.

Do not generate hundreds of decorative objects before the player controller and first boss are enjoyable.

Do not claim systems are tested unless they have actually been run and inspected.

All uncertain technical assumptions must be tested in the project before they become implementation constraints.

---

# 01 — THE CORE CONCEPT

## 1.1 The Story

In a universe where everyone is an apple, there lives a young apple child.

This is not an ordinary human child wearing an apple costume.

**The child is an actual living apple.**

He has a face, arms, legs, emotions, a personality, a bedroom, a family, and an ordinary daily life.

To him, being an apple is perfectly normal.

His bedroom exists inside an apple civilization with apple-inspired architecture, objects, culture, advertisements, toys, furniture, and technology.

Every evening, he goes to bed.

But his dreams are completely absurd.

In one dream, a chair is terrified of being sat on.

In another, a massive ship cannot reach its destination.

Sometimes the Moon refuses to sleep.

Sometimes books refuse to end.

Sometimes ordinary household objects become enormous living creatures.

To wake up happy, the apple child must complete these dreams.

However, defeating a boss does not necessarily involve attacking it until its health reaches zero.

Instead, every boss has an absurd but understandable victory condition.

The chair must be sat on.

The Moon must fall asleep.

The ship must reach its destination.

The book must reach its final page.

The player enters each dream with randomized cards that can change abilities, create objects, transform the player, or modify the rules of the dream itself.

When the player loses, his apple mother wakes him up.

When the player wins, he sleeps peacefully.

Over multiple runs, the player can upgrade his bed, improve his comfort, unlock new dream abilities, and discover increasingly ridiculous interactions.

## 1.2 Core Fantasy

The player is a small apple child living inside an enormous imagination.

He must use movement, timing, spatial awareness, improvisation, and bizarre tools to solve seemingly impossible encounters.

The central fantasy is:

**I can solve this problem in a way the world never expected.**

## 1.3 Core Design Pillars

### A. Movement Is Fun

Running and jumping must feel satisfying independently of objectives.

### B. The Boss Is a Problem

Bosses are mechanical puzzles with personality, rather than ordinary HP targets.

### C. Three Cards, Many Possibilities

Randomness generates creative choices.

### D. The World Is Absurd but Consistent

The game can be ridiculous, but its gameplay rules must be readable and dependable.

### E. The Player Creates the Punchline

The funniest moments should emerge from player actions.

### F. Beautiful but Readable

Every art decision must reinforce gameplay clarity.

---

# 02 — RESEARCH-DRIVEN GAME DESIGN

## 2.1 Primary References

These games are design references, not visual templates to reproduce.

### SUPER MARIO 3D WORLD

Research focus:

- Fixed-direction and semi-fixed camera
- Three-dimensional jumping clarity
- Short, readable levels
- Clear use of environmental mechanics
- Carefully introduced challenges
- Consistent movement response

Application:

Use curated perspective camera angles to make platforming easier to read.

Do not copy Mario's model, animations, recognizable level layouts, enemies, or interface.

### SUPER MARIO 3D LAND

Research focus:

- Depth perception
- Shadows under characters
- Readable landing points
- Controlled camera angles
- Clear platform boundaries

Application:

Every important jump must have a visually identifiable landing area.

### A HAT IN TIME

Research focus:

- Character personality through movement
- Playful platforming
- Strong world identity
- Third-person traversal
- Exploration rewards

Application:

The apple child should feel expressive and enjoyable to control.

### TINYKIN

Research focus:

- Mixing 2D characters and 3D environments
- Colorful 3D world composition
- Readability of stylized assets
- Environmental scale
- Playful exploration

Application:

Develop a cohesive 2D/3D visual language for the Apple World.

### THE PLUCKY SQUIRE

Research focus:

- Visual transitions between 2D and 3D
- Art material consistency
- Paper-like surfaces
- Integrating illustrated elements into a three-dimensional scene

Application:

Paper cutouts and 2D decorations must have meaningful physical placement inside the world.

### BABA IS YOU

Research focus:

- Rule-based interaction
- Unexpected solutions
- Systemic puzzle design

Application:

Cards should alter defined object properties instead of triggering arbitrary scripted victories.

### SPELUNKY

Research focus:

- Emergent interactions
- Reusable systems
- Controlled randomness
- Replayability

Application:

Create a small set of dependable interacting mechanics before expanding the card collection.

## 2.2 Level Design Principle

Use the four-part structure commonly associated with Koichi Hayashida's design approach:

1. Introduce
2. Develop
3. Twist
4. Resolve

For the first level:

**Introduce:** Show that living furniture can move.

**Develop:** Teach the player to use jumping, positioning, and objects to overcome moving furniture.

**Twist:** Reveal that the boss is a chair that runs away whenever someone approaches.

**Resolve:** Challenge the player to combine the learned mechanics to successfully sit on it.

## 2.3 What We Should Not Learn from References

Avoid blindly copying:

- Mario's specific jump animations
- Nintendo's character proportions
- Another game's textures
- Another game's UI
- Existing bosses
- Existing music
- Existing level layouts

The goal is to learn *why* successful games work and develop an original visual and gameplay identity.

---

# 03 — WORLD BUILDING: APPLE WORLD

## 3.1 World Identity

The apple protagonist lives in a society of living apples.

Apple people have different personalities, appearances, occupations, ages, and ways of dressing.

Not every apple must look identical.

Some are green, some red, some yellow, and some have mixed colors.

The protagonist should retain one identifiable design throughout the entire game.

## 3.2 Physical World

Apple World is an intentionally peculiar civilization.

Buildings may have rounded, fruit-like architecture.

Public transportation may resemble fruit crates.

Technology may feature apple-seed-shaped controls.

Picture books may be printed on orchard-fiber paper.

Household furniture may contain details inspired by leaves, stems, seeds, and natural wooden forms.

However, the world must not be saturated with obvious apple symbols.

The goal is to make it feel like a functioning civilization, not a room full of apple clip art.

## 3.3 Cultural Identity

Examples of world details:

- Children's drawings of apple astronauts.
- A family photograph showing apple relatives.
- Posters of fictional apple musicians.
- A fruit-crate-inspired wardrobe.
- An alarm clock shaped like a seed.
- A local apple-world newspaper.
- A toy rocket made of household materials.
- A model ship on a bookshelf.
- A book about the Moon.
- A small chair that looks suspiciously similar to the first boss.

These details connect the bedroom to future dream levels.

## 3.4 Creative Direction

The humor can be surreal, fast, and unexpected, inspired by the premise of absurd animated universes.

However, do not copy Rick and Morty characters, its portals, visual designs, specific dialogue, or branded elements.

Similarly, the apple civilization must not use the Apple Inc. logo or branding.

Our apple characters should look like complete fruit characters, not bitten corporate icons.

---

# 04 — MAIN CHARACTER: THE APPLE CHILD

## 4.1 Character Identity

Temporary name: PIP

The name is provisional and may be changed.

Pip is a young living apple who wears pajamas and enjoys strange dreams.

Personality:

- Curious
- Playful
- Slightly clumsy
- Expressive
- Brave when imagining possibilities
- Excited by unusual discoveries

## 4.2 Character Design

### Body

A stylized apple-shaped body.

Avoid a mathematically perfect sphere.

The silhouette should be recognizable as an apple through:

- Slightly uneven rounded sides
- A subtle indentation at the top
- A small stem
- One asymmetrical leaf
- A rounded lower body

### Face

Large expressive eyes.

Simple eyebrows.

A small mouth with expressive shapes.

The face should support emotional reactions without needing realistic facial anatomy.

### Arms

Short flexible arms that read clearly while running.

### Legs

Short but expressive legs.

Large stylized feet can help communicate running, braking, jumping, and landing.

### Outfit

Soft bedtime clothes.

Suggested:

- Loose pajama top
- Short pajama pants
- Oversized slippers
- Small sleeping scarf or accessory

Avoid excessive costume complexity.

## 4.3 Character Proportions

Proposed modeling dimensions:

- Character height: 1.2 Unity units
- Body width: approximately 0.9 units
- Body depth: approximately 0.75 units
- Head/body integrated as one apple-shaped mass
- Arms: approximately 0.4 units
- Legs: approximately 0.35 units

These are visual design targets, not finalized collision dimensions.

## 4.4 Silhouette Requirements

The character must remain recognizable:

- Without textures
- As a shadow
- From the back
- At gameplay camera distance
- During running
- During jumping

The leaf and stem must not cover the face during ordinary camera movement.

## 4.5 Facial Rig

Recommended facial controls:

- Blink
- Smile
- Surprise
- Fear
- Anger
- Sleepy
- Determined
- Victory
- Hurt

Prefer a simple bone/mesh rig plus blend shapes or controlled texture swaps.

Avoid unnecessarily complex facial rigs in P0.

## 4.6 Animation List

Required animations:

1. Idle
2. Idle Curious
3. Walk
4. Run
5. Acceleration
6. Brake
7. Turn
8. Jump Anticipation
9. Jump Rise
10. Jump Apex
11. Fall
12. Landing
13. Hard Landing
14. Interact
15. Push
16. Pull
17. Pick Up
18. Carry
19. Throw
20. Sit
21. Lie Down
22. Sleeping
23. Wake Up
24. Victory
25. Hurt

For the first playable build, prioritize Idle, Walk, Run, Jump, Fall, Landing, Interact, Sit, Sleep, Wake Up, and Victory.

---

# 05 — ART BIBLE: APPLEBOOK SURREALISM

## 5.1 Art Style Definition

**Applebook Surrealism** is a stylized combination of:

- Soft 3D forms
- Handcrafted toy-like materials
- Illustrated 2D cutouts
- Playful surrealism
- Storybook color composition
- Expressive cartoon animation
- Clean environmental readability

The world should feel like a physical children's storybook that has somehow become real.

It should not look like generic mobile-game low-poly art.

It should not look like a mixture of unrelated asset packs.

It should not look like automatically generated concept art that contains random details without visual logic.

## 5.2 Shape Language

### Player

Rounded, asymmetrical, friendly shapes.

### Friendly Objects

Soft corners and stable silhouettes.

### Interactive Objects

Clear shapes, deliberate accents, recognizable handles or interaction points.

### Enemies

Stronger shape contrasts and exaggerated motion.

### Bosses

Ordinary objects with highly recognizable silhouettes and unexpectedly expressive movement.

### Environment

Large, simple shapes supporting the silhouette of the player.

## 5.3 Color Palette

Primary palette:

- Midnight Plum: #292137
- Apple Red: #E85D65
- Warm Cream: #F5E7D3
- Leaf Green: #78B78B
- Dream Lavender: #AAA0DD
- Apricot Orange: #FFB870
- Sky Blue: #8FD5ED
- Dark Ink: #302A3D

Color allocation:

- Player: warm red/rose and cream
- Safe areas: cream, lavender, warm wood
- Interactable objects: warm yellow or mint accents
- Hazards: high-contrast orange/red plus shape indicators
- Dream magic: lavender and cyan
- Background: lower saturation than gameplay objects

Do not use every accent simultaneously.

## 5.4 Material Language

### Apple Skin

- Soft stylized shading
- Gentle color variation
- Subtle surface imperfections
- No realistic wet fruit texture
- No heavy photorealistic gloss

### Wood

- Warm colors
- Simplified wood grain
- Rounded and slightly imperfect edges
- Subtle painted texture

### Fabric

- Soft shading
- Slight wrinkles
- Simple textile texture
- Rounded folds

### Paper

- Visible thickness where appropriate
- Printed illustrations
- Slightly irregular edges
- Very subtle fiber texture

### Metal

- Muted reflections
- Clean readable highlights
- No overwhelming metallic surfaces

## 5.5 Lighting Rules

Use one dominant light direction per environment.

Bedroom:

- Warm practical bedside light
- Cool moonlight from window
- Gentle ambient fill
- Soft contact shadows

Dream Stage:

- Warm playful lighting
- Clear floor shadows
- Strong focal lighting on important objects
- Less visual noise around jumping areas

Do not flood every asset with emissive materials or rim lights.

## 5.6 Outlines

Use selective outlines.

Possible applications:

- Main character
- Important interactables
- 2D illustration surfaces
- Certain boss details

Do not put identical black outlines on absolutely everything.

Test the game in motion before committing to a full-screen outline shader.

## 5.7 2D Elements Inside the 3D World

The game uses three types of 2D elements.

### Type A — Decorative 2D Cutouts

Examples:

- Paper clouds
- Painted stars
- Wall stickers
- Posters
- Cartoon insects
- Illustrated plants
- Floating scribbles

These are decorative unless explicitly assigned gameplay properties.

### Type B — Animated 2D Effects

Examples:

- Speed lines
- Exclamation marks
- Surprise effects
- Jump impact drawings
- Motion streaks
- Chair panic symbols

These follow objects or appear in world space.

### Type C — Functional 2D Objects

Examples:

- Paper switches
- Illustrated signs
- A paper bridge
- A painted door
- A 2D obstacle with a corresponding 3D collision shape

These must have clear interaction rules.

A 2D object should never unexpectedly block movement merely because it looks solid.

## 5.8 Billboard Rules

Some decorative sprites may rotate around the vertical axis to face the camera.

However:

- Posters attached to walls must remain attached to walls.
- Floor markings must stay aligned with the floor.
- Standing cutouts may use Y-axis billboarding.
- Full billboard rotation is reserved for effects and specific decorations.
- Sprites need consistent depth sorting.
- Avoid excessive transparent overdraw.
- Use alpha clipping where appropriate and visually acceptable.
- Preserve clear silhouettes during camera movement.

## 5.9 Art Consistency Rules

All assets must follow the same:

- Palette
- Scale convention
- Material style
- Outline treatment
- Lighting direction
- Texture density
- Shape language
- Animation exaggeration
- Shadow softness

A new asset should be reviewed alongside the player, bed, chair, and existing room props before being accepted.

## 5.10 Anti-AI-Slop Rules

Never use decorative detail simply to make a scene appear more complicated.

Every visual element must justify its presence through at least one of:

- Worldbuilding
- Navigation
- Gameplay
- Story
- Characterization
- Composition
- Environmental storytelling

Avoid:

- Random glowing particles everywhere
- Unnecessary floating objects
- Inconsistent texture styles
- Excessive gradients
- Excessive bloom
- Unreadable typography
- Random UI panels
- Generic fantasy props
- Overly shiny plastic surfaces
- Copy-pasted decorations
- Textures that contain malformed letters

Before creating a detailed asset, establish its shape, function, and visual role.

---

# 06 — BLENDER PRODUCTION PIPELINE

## 6.1 Modeling Philosophy

Use hand-authored stylized 3D models.

Low-poly or mid-poly geometry may be used where appropriate, but low polygon count alone does not define quality.

Quality comes from:

- Strong silhouette
- Intentional topology
- Consistent proportions
- Effective materials
- Good lighting
- Expressive animation
- Correct scale
- Clean integration

## 6.2 Blender Asset Workflow

For every asset:

1. Define visual purpose.
2. Create rough sketches or shape references.
3. Block out major forms.
4. Compare silhouette with the art bible.
5. Refine the geometry.
6. Set correct origin/pivot.
7. Apply intended transforms.
8. UV unwrap if needed.
9. Assign materials.
10. Create rig if animated.
11. Create required animations.
12. Export FBX or suitable interoperable format.
13. Import into Unity.
14. Verify scale, orientation, materials, normals, and rig.
15. Test under actual game lighting.
16. Approve or revise.

## 6.3 Naming Convention

Examples:

```text
CHR_AppleChild
BOSS_Chair
ENV_Bedroom_Wall_A
ENV_Bedroom_Floor
PROP_Bed
PROP_NightLamp
PROP_Desk
PROP_AppleClock
PROP_ToyShip
PROP_Bookshelf
DEC_PaperCloud
DEC_PaperStar
FX_ChairPanic
```

## 6.4 Source File Management

Keep editable source files separate from imported game assets.

Example:

```text
ArtSource/
  Blender/
    Characters/
    Bosses/
    Environments/
    Props/
  Illustrations/
  Textures/
  Concepts/

UnityProject/
  Assets/
    DreamRush/
```

## 6.5 Blender to Unity Rules

- Establish a consistent unit scale.
- Verify coordinate axes on export and import.
- Preserve meaningful object pivots.
- Check normals.
- Verify armature and animations.
- Recreate or validate materials in URP.
- Keep source `.blend` files.
- Export versioned assets if major topology changes occur.
- Avoid destructive operations on the only source file.
- Reimport animated assets into a clean test scene before final integration.

## 6.6 Asset Quality Gate

An asset is accepted only if:

- Silhouette is readable.
- Scale is correct.
- Pivot is correct.
- Shading is clean.
- No obvious UV defects.
- No clipping in intended animation.
- Material matches art direction.
- Collision is appropriate.
- Asset works in Unity.
- Appearance remains clear from gameplay camera distance.

---

# 07 — THE APPLE BEDROOM HUB

## 7.1 Purpose

The bedroom is not a static main menu.

It is a fully explorable 3D hub.

The player controls the apple child directly and walks around the room.

Every important menu function is represented by a physical object.

The bedroom must feel like a real personal space.

It should support curiosity and reward exploration.

## 7.2 Desired Experience

When the player starts the game:

The camera opens inside a cozy apple bedroom.

Moonlight shines through a rounded window.

There are drawings on the walls.

A warm desk lamp illuminates the room.

The apple child stands near the center.

The bed appears comfortable and inviting.

A small prompt teaches movement.

The player is free to explore.

There should not be a large intrusive main menu covering the scene.

## 7.3 Hub Camera

Use a free third-person camera with limits suitable for an interior environment.

Requirements:

- Player-following camera
- Mouse/right-stick rotation
- Smooth camera orbit
- Camera collision
- Wall transparency or camera repositioning when obstructed
- Prevent camera clipping through geometry
- Adjustable sensitivity
- Reasonable default framing

Suggested starting parameters:

```yaml
field_of_view: 50
camera_distance: 5.5
camera_target_height: 0.8
pitch_min: -15
pitch_max: 55
default_pitch: 18
```

Values must be tuned through gameplay testing.

## 7.4 Hub Dimensions

Proposed starting room:

Approximately 12 × 10 Unity units.

Ceiling height approximately 4.5 units.

The room should not feel empty.

However, the player should be able to move freely without constant collision with decorative objects.

## 7.5 Hub Zones

### Zone A — Bed

Function:

- Start Dream
- Sleep Animation
- Dream Transition
- Access Bed Upgrade Interface

Interaction:

Approach bed.

Display:

**[E] GO TO SLEEP**

Optional secondary interaction:

**[Hold E] UPGRADE BED**

Alternatively, provide a distinct bedside upgrade object to avoid interaction ambiguity.

### Zone B — Bedside Lamp

Function:

- Open Settings

When the player interacts, the lamp emits a small click and a settings panel appears.

This must still be accessible from the Pause Menu for convenience.

### Zone C — Bookshelf

Function:

- Open Dream Diary

The player can inspect previous bosses, discovered solutions, and dream memories.

### Zone D — Wardrobe

Function:

- Cosmetics
- Character Customization

Not required for P0.

### Zone E — Desk

Function:

- Challenge Board
- Achievements
- Collectibles

Not required for P0.

### Zone F — Window

Function:

- Environmental storytelling
- View of Apple World
- Optional interaction

May show a quiet apple-themed neighborhood.

### Zone G — Wall Posters

Function:

- Lore
- Game Credits
- Small interactions

### Zone H — Alarm Clock

Function:

- Run History
- Statistics
- Optional Challenge Modes

Not required for P0.

## 7.6 Hub Layout Sketch

Illustrative top-down arrangement:

```text
+----------------------------------------+
|                WINDOW                  |
|                                        |
|  BOOKSHELF               POSTERS       |
|                                        |
|  DESK                    BED           |
|  [DIARY]                 [PLAY]        |
|                                        |
|  WARDROBE             BEDSIDE LAMP     |
|                       [SETTINGS]       |
|                                        |
|             PLAYER SPAWN               |
|                                        |
|                  DOOR                  |
+----------------------------------------+
```

This is a conceptual arrangement, not a mandatory final floor plan.

## 7.7 Bedroom Set Dressing

Suggested props:

- Bed with apple-seed-shaped headboard
- Soft blanket
- Pillows
- Bedside table
- Apple-seed alarm clock
- Rounded window
- Curtains
- Bookshelf
- Desk
- Family portrait
- Wardrobe
- Small stool
- Model ship
- Moon illustration
- Paper stars
- Toy blocks
- Small decorative plants
- Pencil drawings
- Night lamp
- Rug
- Children's shoes
- A tiny chair

Each asset should have a defined silhouette and material category.

Avoid placing apple logos on every object.

## 7.8 Environmental Storytelling

The first boss is a chair.

Place a tiny chair somewhere in the bedroom.

When interacted with, it may wobble or make a small sound.

This creates a subtle connection between the bedroom and the dream.

Place a picture book and a toy ship to foreshadow future boss concepts.

## 7.9 Hub Sound Design

- Soft nighttime ambience
- Clock ticking at low volume
- Gentle room tone
- Fabric sounds near the bed
- Soft footsteps
- Quiet interaction sound
- Optional distant neighborhood sounds

Music should feel safe and cozy.

## 7.10 Hub UI

Keep on-screen UI minimal.

Only show:

- Interaction prompt
- Small contextual hints
- Current Dream Memory if relevant
- Pause icon when needed

Do not show persistent cards, health bars, or combat HUD in the bedroom.

---

# 08 — DIEGETIC MENU SYSTEM

## 8.1 Principle

Menus should be integrated with the game world.

However, diegetic interfaces must not sacrifice usability.

## 8.2 Interactions

Bed → Play

Lamp → Settings

Bookshelf → Dream Diary

Wardrobe → Cosmetics

Desk → Challenges

Clock → Statistics

## 8.3 Menu Camera

On interaction:

1. Player movement is disabled.
2. Camera smoothly moves toward the object.
3. Object animation begins.
4. Menu appears as a world-space or screen-space interface.
5. Player can navigate using mouse, keyboard, or controller.
6. On close, camera returns to player.

## 8.4 Accessibility Fallback

All essential menus must also be accessible through Pause or a dedicated shortcut.

The player should not be forced to navigate the bedroom repeatedly just to change volume or controls.

---

# 09 — 3D PLAYER MOVEMENT

## 9.1 Control Philosophy

The movement should feel closer to a polished 3D platformer than a traditional action RPG.

Movement needs:

- Quick response
- Reliable jumping
- Clear acceleration
- Controlled deceleration
- Predictable air movement
- Forgiving landing detection
- Consistent collision

## 9.2 Control Mapping

Keyboard and mouse:

```text
WASD       Move
Mouse      Camera
Space      Jump
Shift      Sprint
E          Interact
Left Click Contextual basic action
Q          Active Card
R          Secondary ability / selected tool
1 / 2      Object Slots
Tab        Objective / Dream Guide
Esc        Pause
```

Gamepad support:

- Left Stick: Move
- Right Stick: Camera
- Bottom Face Button: Jump
- West Face Button: Interact
- Shoulder Button: Ability
- Trigger: Contextual Action

Exact mappings must be configurable.

## 9.3 Player Movement Configuration

Proposed tuning values:

```yaml
walk_speed: 5.5
sprint_speed: 8.0
ground_acceleration: 28.0
ground_deceleration: 35.0
air_acceleration: 13.0
rotation_speed_degrees: 720
jump_height: 2.1
gravity: 24.0
fall_gravity_multiplier: 1.35
coyote_time: 0.12
jump_buffer: 0.14
ground_snap_distance: 0.2
```

These values are prototypes, not final numbers.

## 9.4 Jumping

Required:

- Variable jump height
- Coyote time
- Jump buffering
- Air control
- Clear apex timing
- Landing feedback
- Ground shadow
- Reliable platform collision

Short tap should create a smaller jump.

Holding Jump should allow the full jump height.

## 9.5 Coyote Time

Allow jumping briefly after leaving a platform.

Initial target: 0.12 seconds.

## 9.6 Jump Buffer

If Jump is pressed shortly before landing, execute the jump once the character becomes grounded.

Initial target: 0.14 seconds.

## 9.7 Ground Shadow

Display a shadow or landing indicator underneath the player.

The shadow should follow the ground surface rather than the character's airborne height.

This helps determine landing position in a three-dimensional scene.

## 9.8 Movement Camera Relationship

Player movement should be camera-relative.

Forward input moves the player toward the camera's forward direction projected onto the horizontal plane.

The character should rotate smoothly toward movement direction.

## 9.9 Jump Collision

Use suitable ground checks and collision validation.

If using CharacterController, implement gravity explicitly and track grounded state carefully.

If using Rigidbody, prevent unstable sliding and unintended rotations.

Choose one player motor strategy after testing, rather than mixing conflicting approaches.

## 9.10 Optional Advanced Movement

Do not implement initially unless testing demonstrates a need:

- Double Jump
- Wall Jump
- Ground Pound
- Long Jump
- Roll
- Dive
- Air Dash

The first playable game must be enjoyable with basic running and jumping.

---

# 10 — CAMERA DESIGN

## 10.1 Two Primary Camera Modes

### HUB CAMERA

Free third-person camera.

Used inside the bedroom.

### DREAM STAGE CAMERA

Controlled perspective camera.

Used during platforming and boss encounters.

## 10.2 Dream Stage Camera Style

Inspired by the clarity of Super Mario 3D World.

Use a camera that follows the player at a curated angle.

Starting direction:

- Three-quarter viewpoint
- Perspective projection
- Moderate distance
- Limited camera rotation
- Clear view of the ground
- Clear view of nearby platforms

Suggested starting configuration:

```yaml
projection: Perspective
field_of_view: 45
distance: 12
height_offset: 7
look_ahead: 2
camera_damping: 0.18
```

These are starting values only.

## 10.3 Boss Camera

The camera should frame both player and chair whenever practical.

Use a camera target group or custom dynamic framing.

When the chair runs away, the camera may subtly shift to show the escape direction.

Avoid sudden rotations.

## 10.4 Camera Transitions

Required:

- Bedroom exploration
- Bed interaction close-up
- Falling asleep transition
- Stage introduction
- Boss reveal
- Boss chase
- Victory close-up
- Wake-up sequence

## 10.5 2.5D Sections

The stage may include special sections where movement is temporarily constrained to a plane.

These must be explicit gameplay moments.

Entering a 2.5D section:

1. Camera changes to a suitable side or angled view.
2. Input is projected onto the permitted movement plane.
3. Transition is visually readable.
4. Player movement remains consistent.
5. Exit transition restores free 3D movement.

Do not confuse 2.5D camera sections with 2D decorative art.

These are separate design features.

For P0, the core chair arena remains fully 3D.

---

# 11 — FIRST DREAM: THE CHAIR

## 11.1 Level Name

**THE CHAIR THAT REFUSED**

World 1 — The Living Room That Won't Sit Still

## 11.2 One-Sentence Concept

A perfectly ordinary chair is terrified of anyone sitting on it, and the player must chase it across an absurd living room before finally managing to sit down.

## 11.3 Why This Boss Exists

The first boss must teach the unique identity of DREAM RUSH.

Players should immediately understand that:

- Bosses have unusual objectives.
- Movement skills matter.
- Cards create alternative solutions.
- Objects have consistent properties.
- Clever decisions are rewarded.

## 11.4 First-Level Emotional Sequence

The player should experience:

1. Curiosity
2. Comfort
3. Surprise
4. Amusement
5. Determination
6. Discovery
7. Satisfaction

## 11.5 Target Duration

First-time player:

Approximately 8–12 minutes including tutorial.

Returning player:

Approximately 3–6 minutes.

Boss chase:

Approximately 2–4 minutes on the first encounter.

These times must be validated through player testing.

---

# 12 — THE CHAIR: CHARACTER DESIGN

## 12.1 Boss Identity

A small wooden chair with an exaggerated personality.

It is not an evil villain.

It is a terrified object that refuses to be sat on.

## 12.2 Appearance

Create a handcrafted stylized wooden chair.

Important features:

- Rounded wooden structure
- Slightly uneven proportions
- Four expressive legs
- Curved backrest
- Soft beveled edges
- Warm wood material
- Expressive eyes integrated into the upper backrest
- Optional small mouth
- Visible seat surface

The chair must remain immediately recognizable as an actual chair.

## 12.3 Silhouette

From far away, it must look like a chair.

Do not add so many eyes, limbs, horns, decorations, or fantasy features that it becomes visually confusing.

## 12.4 Personality

The chair is:

- Nervous
- Dramatic
- Easily startled
- Fast but clumsy
- Stubborn
- Surprisingly expressive

## 12.5 Animation List

- Idle
- Suspicious
- Notice Player
- Panic
- Run
- Sharp Turn
- Slip
- Jump
- Tired
- Catch Breath
- Frozen
- Rooted
- Tempted
- Offer Seat
- Being Sat On
- Defeated

## 12.6 Key Comedy Principle

The chair should behave like a living creature trying to avoid an awkward social situation.

Its body language should be funny even without dialogue.

---

# 13 — FIRST LEVEL STRUCTURE

## 13.1 Level Overview

The first dream contains five gameplay sections.

### SECTION A — ARRIVAL

The apple child falls gently into a strange living room.

### SECTION B — PLATFORMING TUTORIAL

The player learns to run and jump.

### SECTION C — MOVING FURNITURE

The player encounters animated furniture.

### SECTION D — FIRST CARD INTERACTION

The player experiments with the selected card.

### SECTION E — CHAIR BOSS ARENA

The player discovers the chair and attempts to sit on it.

## 13.2 Level Flow

```text
BEDROOM
   |
   v
FALL ASLEEP
   |
   v
THREE-CARD CHOICE
   |
   v
ARRIVAL PLATFORM
   |
   v
MOVEMENT TUTORIAL
   |
   v
MOVING FURNITURE
   |
   v
CARD EXPERIMENT AREA
   |
   v
CHAIR BOSS REVEAL
   |
   v
CHASE / PUZZLE
   |
   +---- VICTORY ----> SWEET DREAM
   |
   +---- FAILURE ----> MOM WAKES YOU
```

## 13.3 Arrival Area

The player lands on a soft carpet.

The room appears enormous compared with the apple child.

Furniture floats at unusual angles in the background.

Paper clouds and illustrated stars decorate the environment.

A distant chair is visible.

At first, it appears ordinary.

The player can move freely.

The game introduces basic controls through a short environmental prompt.

## 13.4 Movement Tutorial

Use a simple series of three challenges:

### Challenge 1

Walk across a small carpeted area.

### Challenge 2

Jump onto a low ottoman.

### Challenge 3

Jump across a small gap between cushions.

Each challenge should be safe and forgiving.

Do not start with complex moving platforms.

## 13.5 Moving Furniture Area

A small stool runs away when approached.

Unlike the boss, it has a predictable short path.

The player learns:

- Living furniture reacts to proximity.
- Furniture can change movement direction.
- Moving objects can be approached strategically.

This is the first introduction to the chair's main mechanic.

## 13.6 Card Experiment Area

Provide an object compatible with the selected starting card.

If the selected card is Super Glue:

A moving object can be rooted.

If Camera Flash:

A moving object can be frozen.

If Fishing Rod:

A distant object can be pulled closer.

The player should understand the card through experimentation, not through a lengthy explanation.

## 13.7 Boss Reveal

The player enters a larger room.

The camera pulls back.

One chair sits in the center.

There are no visible threats.

The player approaches.

The chair turns toward the player.

Its eyes widen.

A musical pause.

The chair suddenly runs away.

Title:

**THE CHAIR**

Objective:

**SIT ON THE CHAIR**

This should be a memorable and funny reveal.

---

# 14 — BOSS ARENA DESIGN

## 14.1 Arena Requirements

The arena must be a designed playground for chasing, positioning, jumping, and card usage.

It should not be an empty square.

It should also not contain so many props that the chair becomes difficult to track.

## 14.2 Proposed Arena Scale

Approximately 32 × 25 Unity units.

Main floor:

- Broad carpet
- Solid boundaries
- Rounded corners
- Clear walkable lanes

Platforms:

- 2 low furniture platforms
- 1 shortcut platform
- 1 slightly elevated balcony or cushion route

Interactive props:

- 2 movable cushions
- 1 small switch
- 1 movable toy block
- 1 furniture obstacle
- 1 card-compatible test object

## 14.3 Layout Concept

```text
+------------------------------------------------+
|                                                |
|  LOW PLATFORM         BALCONY / SHORTCUT       |
|     [A]                       [B]               |
|                                                |
|               CENTRAL CARPET                   |
|                                                |
|        FURNITURE          CHAIR START          |
|        OBSTACLES              [C]              |
|                                                |
|                                                |
|  CUSHION ROUTE          MOVABLE BLOCK          |
|      [D]                     [E]               |
|                                                |
|              PLAYER ENTRANCE                   |
+------------------------------------------------+
```

## 14.4 Space Roles

### Central Carpet

Primary chase area.

### Low Platform

Shortcut for a skilled player.

### Balcony

Provides visibility over the chair's route.

### Cushion Route

Alternative movement path.

### Movable Block

Allows the player to manipulate the chair's escape route.

### Furniture Obstacles

Provide temporary cover and cornering opportunities.

## 14.5 Arena Readability

The chair should be visible most of the time.

When temporarily obscured, use:

- Sound direction
- Motion effects
- A short positional indicator
- Camera framing
- Environmental reactions

Do not permanently track the chair through all walls.

---

# 15 — CHAIR BOSS AI

## 15.1 Core Behavior

The chair runs away from the player.

However, it must not behave as an infinitely perfect escape AI.

The encounter must be designed so the player can learn and exploit its behavior.

## 15.2 State Machine

```text
IDLE
ALERT
FLEE
PANIC
REPOSITION
STUMBLE
EXHAUSTED
ROOTED
FROZEN
OFFER_SEAT
SIT_AVAILABLE
SITTING
DEFEATED
```

## 15.3 State Transitions

### IDLE → ALERT

Player enters awareness radius.

### ALERT → FLEE

Player approaches within escape range.

### FLEE → PANIC

Player remains close or blocks the escape route.

### FLEE → REPOSITION

Chair must choose a new safe escape destination.

### FLEE/PANIC → STUMBLE

Chair makes an abrupt turn, collides with a suitable obstacle, or triggers a designed stumble condition.

### FLEE/PANIC → EXHAUSTED

Chair accumulates fatigue from running.

### ANY VALID STATE → ROOTED

A rooting effect successfully applies.

### ANY VALID STATE → FROZEN

A freezing effect successfully applies.

### VALID SOCIAL STATE → OFFER_SEAT

Special interaction changes the chair's willingness to be sat on.

### SIT_AVAILABLE → SITTING

Player successfully interacts and passes seating validation.

### SITTING → DEFEATED

Required sitting duration is completed.

## 15.4 Proposed AI Parameters

```yaml
awareness_radius: 7.0
escape_trigger_radius: 4.0
chair_normal_speed: 6.1
chair_panic_speed: 7.2
chair_acceleration: 12.0
chair_turn_rate: 280
exhaustion_threshold: 18.0
exhaustion_duration: 3.0
stumble_duration: 1.5
reposition_interval: 1.2
minimum_escape_distance: 5.0
maximum_escape_distance: 11.0
```

These are tuning targets.

The sprinting player should be faster than the chair's normal movement.

The chair's path must still be challenging because of its turns, obstacles, and route choices.

## 15.5 Escape Destination Selection

Do not simply set the chair destination to:

CurrentPosition + DirectionAwayFromPlayer * Distance

That can cause:

- Wall collision
- Unreachable destinations
- Repetitive movement
- Infinite loops
- Unfair escape patterns

Instead:

1. Identify valid reachable candidate positions.
2. Calculate path length.
3. Reject unreachable positions.
4. Evaluate distance from player.
5. Evaluate nearby obstacles.
6. Evaluate escape route quality.
7. Penalize recently visited destinations.
8. Select a weighted candidate.
9. Move using valid navigation.
10. Recalculate when blocked.

## 15.6 Fairness Rule

The chair must not always choose the mathematically optimal escape route.

Occasionally it should:

- Hesitate
- Turn awkwardly
- Stumble
- Pause to look at the player
- Recover after panic
- Become exhausted

These reactions provide readable opportunities for the player.

## 15.7 Fatigue System

The chair has a hidden fatigue value.

Running increases fatigue.

Resting decreases fatigue.

When fatigue reaches the threshold, the chair enters EXHAUSTED for approximately 3 seconds.

A tired chair must remain reachable.

Do not trigger exhaustion only when the chair is behind an inaccessible obstacle.

## 15.8 Anti-Softlock

If the chair:

- Falls out of the arena
- Enters invalid navigation
- Gets stuck inside a prop
- Cannot calculate a path
- Is trapped outside playable space

Trigger a safe reposition with visible dreamlike feedback.

Do not silently teleport the chair in front of the player without explanation.

## 15.9 Seat Validation

The player wins only by performing the required seating interaction.

Validation includes:

- Correct boss entity
- Player in valid range
- Chair in a sit-compatible state
- Seat position reachable
- Player not currently stunned
- Sitting animation initiated
- Duration threshold completed

The player must sit for 2 continuous seconds.

Entering the seat area alone does not count.

---

# 16 — WAYS TO DEFEAT THE CHAIR

## 16.1 Baseline Method: Chase and Exhaust

The player runs after the chair.

The chair repeatedly escapes.

The player uses shortcuts and movement skill to stay close.

Eventually the chair gets tired.

The player approaches during exhaustion.

Press E to sit.

Maintain sitting for 2 seconds.

Victory.

This method must work without requiring a specific card.

## 16.2 Super Glue

The player uses Super Glue on the chair.

The chair becomes rooted.

The player approaches.

Press E.

Victory.

The glue must be subject to target range and effect duration.

## 16.3 Camera Flash

The player positions the chair in front of the camera.

Activate the ability.

The chair becomes frozen temporarily.

The player runs to the seat.

Victory.

## 16.4 Fishing Rod

The player uses Fishing Rod to pull the chair toward a reachable area.

The chair may resist before being reeled in.

The player must approach and sit during the recovery window.

## 16.5 Be a Grandpa

The player transforms into an elderly apple.

The chair changes its behavior.

Instead of running away immediately, it hesitates and offers its seat.

The player must approach before the transformation expires.

This interaction should have a unique animation.

## 16.6 Shortcut Method

The player jumps onto a higher platform.

The chair passes underneath or near the platform.

The player takes a shortcut and lands in front of the chair.

The chair becomes startled and stumbles.

The player sits during the opening.

## 16.7 Obstacle Method

The player pushes a movable block into the chair's escape route.

The chair changes direction.

The player uses the blocked route to corner it.

The chair panics, becomes briefly vulnerable, and can be sat on.

## 16.8 Secret Method: Become a Chair

This is an optional future-card interaction.

The player transforms into a chair.

Special boss behavior may cause the original chair to sit on the player.

This should be counted as a distinct secret condition, not automatically treated as ordinary seating.

It must be clearly defined in the Boss Rule System.

## 16.9 Solution Discovery

When the player discovers a new method, record it in the Dream Diary.

Examples:

- Exhaustion Victory
- Rooted Victory
- Frozen Victory
- Pulled Victory
- Grandpa Victory
- Shortcut Victory
- Obstacle Victory

Reward creative discovery, but do not require all methods to complete the level.

---

# 17 — THREE-CARD SYSTEM FOR THE FIRST LEVEL

## 17.1 Core Requirement

At dream entry, the player receives three randomized card options.

The player chooses one.

## 17.2 Tutorial Design

For the very first run, use a controlled random draw from a starter pool.

Every offered card must:

- Have an understandable effect
- Work somewhere in the tutorial
- Be useful against the chair
- Support a different style of play

Example starter card pool:

### SUPER GLUE

Stops a moving object.

### CAMERA FLASH

Freezes a target temporarily.

### FISHING ROD

Pulls an object toward the player.

### BE A GRANDPA

Changes social behavior of certain objects.

### DECOY APPLE

Creates a temporary decoy.

### BOUNCY CUSHION

Creates a small jump-assist platform.

The first draw selects three distinct cards from this curated starter pool.

## 17.3 Why Not Completely Unrestricted Randomness?

The full game can have strange items with many applications.

However, during the first playable experience, unrestricted randomness may generate cards that do not teach anything useful.

The goal is to establish trust that card choices matter.

## 17.4 Card UI

Cards should look like illustrated paper cards from a dreambook.

Each card needs:

- Illustration
- Name
- Category
- Short effect
- Optional cooldown
- Clear selection feedback

Do not fill cards with unnecessarily long descriptions.

## 17.5 Card Activation

Gameplay sequence:

1. Select target if required.
2. Check range.
3. Check cooldown.
4. Validate target.
5. Play activation animation.
6. Apply effect.
7. Play feedback.
8. Update ability UI.

## 17.6 Card Data

Define:

```text
Card ID
Display Name
Card Type
Rarity
Target Mode
Range
Cooldown
Duration
Supported Properties
Effect Definition
VFX Reference
SFX Reference
Icon
Description
```

## 17.7 Inventory

First playable:

- One equipped active card
- One optional carried object
- Basic interaction independent of cards

Later versions:

- Two active ability slots
- Two object slots
- Passive effects
- One Dream Law slot

---

# 18 — THE DREAM LOGIC SYSTEM

## 18.1 Goal

Make objects interact through reusable properties and effects.

## 18.2 Example Chair Properties

```text
SitAble
Movable
Rootable
Freezeable
Pullable
SocialResponsive
BossEntity
```

## 18.3 Example Card Actions

```text
Root
Freeze
Pull
ChangeBehavior
SpawnDecoy
CreatePlatform
```

## 18.4 Interaction Pipeline

```text
Card Used
  ↓
Target Selection
  ↓
Target Validation
  ↓
Property Check
  ↓
Effect Application
  ↓
Object State Change
  ↓
Boss Reaction
  ↓
Victory Evaluation
  ↓
Feedback
```

## 18.5 Required Interfaces

Suggested:

```csharp
public interface IInteractable
{
    bool CanInteract(InteractionContext context);
    void Interact(InteractionContext context);
}

public interface IEffectReceiver
{
    bool CanReceive(EffectContext context);
    void ApplyEffect(EffectContext context);
}

public interface IBossWinCondition
{
    bool IsSatisfied(BossContext context);
}
```

These are architectural examples, not complete implementation.

## 18.6 Design Restriction

Do not implement:

```text
If card is Super Glue and boss is Chair:
    Instant Win
```

Instead:

Super Glue creates Rooted status.

Rooted status restricts chair movement.

The player still needs to approach and sit.

This preserves actual Gameplay.

---

# 19 — FIRST LEVEL ENEMIES

Use a small enemy set with meaningful behaviors.

## 19.1 SEEDLING

A tiny apple seed with legs.

Behavior:

- Patrol
- Chase when close
- Small bump attack

Purpose:

Teach movement and basic combat.

## 19.2 PAPER CRITTER

An animated paper drawing.

Visual:

2D cutout standing inside the 3D scene.

Behavior:

- Moves along a predictable path
- Has a clear collision shape
- Can be stopped by applicable cards

Purpose:

Introduce functional 2D elements inside 3D environments.

## 19.3 LITTLE STOOL

A smaller piece of living furniture.

Behavior:

- Runs away when approached
- Stops after a short distance

Purpose:

Teach the chair's central behavior before the boss.

## 19.4 Enemy Design Constraints

The first level should not contain too many enemies.

Combat must support the platforming and boss-learning experience.

Do not turn the first level into a long combat gauntlet.

---

# 20 — GAME FEEL

## 20.1 Movement Feel

Prioritize:

- Immediate response
- Expressive body movement
- Good acceleration
- Clear braking
- Pleasant jump arc
- Soft landings
- Reliable collisions

## 20.2 Animation Principles

Use:

- Squash and stretch
- Anticipation
- Follow-through
- Overlapping action
- Exaggeration
- Strong poses

Apply them selectively.

Gameplay response must remain fast.

Do not delay jumping just to finish a long anticipation animation.

## 20.3 Apple Character Reactions

Examples:

While sprinting:

The leaf trails slightly behind.

While braking:

The body leans forward and feet slide briefly.

While jumping:

The apple stretches slightly.

At jump apex:

The pose becomes more open.

On landing:

The body compresses briefly.

When surprised:

Eyes widen and the leaf becomes rigid.

## 20.4 Chair Reactions

When seeing player:

Quick head/backrest snap.

When running:

Legs move rapidly and unevenly.

When turning:

Seat rotates with visible momentum.

When exhausted:

The chair bends and trembles.

When seated on:

The chair reacts dramatically before accepting defeat.

## 20.5 VFX Rules

Use short, readable effects:

- Jump dust
- Landing puff
- Speed lines
- Surprise mark
- Card activation flash
- Freeze effect
- Glue effect
- Victory confetti

Prefer small 2D illustrations used as VFX over enormous volumetric effects.

## 20.6 Camera Feedback

- Small landing response
- Limited chase framing
- Short victory zoom
- Brief cinematic pause

Avoid constant screen shake.

---

# 21 — BED UPGRADE SYSTEM

## 21.1 Core Concept

The bed is the player's permanent progression system.

Better sleep improves the quality and stability of dreams.

## 21.2 Upgrade Categories

### Mattress

Increases maximum Sleep Stability.

### Pillow

Improves basic movement or attack-related properties.

### Blanket

Provides defensive bonuses.

### Bed Frame

Improves starting loadout flexibility.

### Night Lamp

Provides extra card reroll opportunities.

### Dreamcatcher

Improves strategic choices during a run.

## 21.3 First Playable Scope

Only implement three upgrades initially.

**Cloud Mattress**

+1 maximum Sleep Stability per upgrade level, capped by a defined limit.

**Lucky Pillow**

One additional card reroll opportunity per run at its first meaningful level.

**Comfort Blanket**

Provides one small protective effect at the start of a run.

## 21.4 Bed Upgrade Interaction

Approach bed or bedside upgrade panel.

Select Upgrade Bed.

Camera focuses on the bed.

A simple upgrade UI appears.

When purchasing:

- Currency is deducted.
- Bed appearance changes if supported.
- Upgrade level increases.
- Permanent save updates.
- Next run applies the new bonus.

## 21.5 Visual Progression

Level 1:

Simple bed.

Level 2:

Softer mattress and additional pillow.

Level 3:

Better blanket and warm decorations.

Later:

Improved bedding, dreamcatchers, toys, and meaningful cosmetic details.

---

# 22 — DREAM AND WAKE-UP SYSTEM

## 22.1 Beginning a Run

Sequence:

1. Walk to bed.
2. Press E.
3. Apple child climbs onto bed.
4. Character lies down.
5. Eyes close.
6. Ambient sound becomes quieter.
7. Camera moves toward the pillow.
8. Room transforms into a stylized dream transition.
9. Card selection appears.
10. Dream level begins.

## 22.2 Dream Transition Art

Avoid generic circular portals.

Suggested concept:

The bedroom illustration begins to peel apart like layers of paper.

The ceiling becomes a paper sky.

The walls fold outward.

The bed floats briefly.

The camera passes through a page-like transition into the dream.

Use 2D overlays combined with controlled 3D motion.

## 22.3 Defeat

When Sleep Stability reaches zero:

- Gameplay stops.
- Dream colors become desaturated.
- The scene folds away.
- A distant voice calls the child.
- Camera returns to bedroom.
- Apple mother is heard or briefly shown.
- The child wakes up.
- Run Summary appears.

## 22.4 Victory

After defeating the final boss for the current run:

- The chair becomes calm.
- The player celebrates.
- Dream rewards appear.
- A peaceful transition begins.
- The child is shown sleeping comfortably.
- SWEET DREAMS appears.
- Rewards are saved.

For the P0 single-boss prototype, defeating the chair completes the demo run.

---

# 23 — UI/UX

## 23.1 UI Identity

Use illustrated storybook UI.

Visual characteristics:

- Cream-colored paper
- Dark ink text
- Hand-drawn borders
- Soft rounded corners
- Limited accent colors
- Minimal visual clutter

## 23.2 Bedroom UI

Only contextual interaction prompts.

Do not show a large permanent main menu.

## 23.3 Dream HUD

Required:

- Sleep Stability
- Active Card
- Cooldown
- Objective
- Optional directional indicator
- Pause

## 23.4 Boss UI

Example:

```text
THE CHAIR
OBJECTIVE: SIT ON IT
```

When a valid sitting interaction begins:

```text
SITTING...
1.2 / 2.0 SEC
```

## 23.5 Guidance

If the player makes no progress for an extended period, provide optional contextual hints.

Example:

First hint:

"The chair seems tired after running."

Second hint:

"Maybe you can catch it when it stops."

Do not force hints on every player.

## 23.6 Accessibility

Required:

- Adjustable camera sensitivity
- Remappable controls
- Controller support
- Adjustable text size
- Reduced screen shake
- Reduced flashing
- Subtitle support
- Visual indicators for important sounds
- Pause access to settings
- Camera assist options

---

# 24 — SOUND DESIGN

## 24.1 Bedroom

Mood:

Warm, quiet, comfortable.

Sound palette:

- Soft music box
- Gentle nighttime ambience
- Quiet clock ticking
- Fabric movement
- Soft footsteps

## 24.2 Dream Stage

Mood:

Curious, playful, strange.

Sound palette:

- Light percussion
- Plucked instruments
- Unusual household sounds
- Short musical responses

## 24.3 Chair Boss

The music must react to the chase.

States:

- Suspicious
- Running
- Panicking
- Exhausted
- Victory

The chair should have distinctive wooden footstep sounds.

## 24.4 Musical Humor

The boss reveal should feel disproportionately dramatic.

A chair running away from being sat on is not a traditional epic battle.

That contrast is the joke.

## 24.5 Victory Sound

A short dramatic musical sting.

A chair creak.

A small pause.

Then playful celebratory music.

---

# 25 — UNITY PROJECT ARCHITECTURE

## 25.1 Technical Stack

- Unity 6
- C#
- Universal Render Pipeline
- Unity Input System
- Cinemachine 3 compatible with chosen editor version
- Unity AI Navigation
- Animator
- ScriptableObjects
- Shader Graph
- Unity Test Framework
- Versioned JSON Save System

## 25.2 Scene Organization

```text
Bootstrap
MainMenuFallback
BedroomHub
Dream_Chair
RunSummary
```

The MainMenuFallback scene is optional if loading directly into the bedroom is reliable.

## 25.3 Main Systems

```text
GameFlowManager
PlayerMotor3D
PlayerAnimationController
ThirdPersonCameraController
CameraModeDirector
InteractionSystem
DreamTransitionController
CardDrawManager
CardInventory
CardEffectResolver
DreamPropertySystem
StatusEffectSystem
EnemyController
BossChairController
BossWinConditionEvaluator
DreamLevelManager
BedUpgradeManager
SaveManager
UIManager
AudioManager
VFXManager
```

## 25.4 Important Design Separation

Separate:

- Player movement
- Player input
- Character animation
- Camera
- Card effects
- Boss behavior
- Boss victory conditions
- Save data

Do not combine these into one large script.

## 25.5 Boss Architecture

Chair Boss components:

```text
BossChairRoot
  VisualModel
  Animator
  Collider
  NavigationController
  ChairAI
  ChairFatigue
  ChairStatusReceiver
  ChairSeatInteraction
  BossWinCondition
  AudioSource
  VFXAnchors
```

## 25.6 Data-Driven Design

Store card and boss definitions separately from runtime state.

Use ScriptableObjects for authoring data.

Use runtime instances for cooldowns, status effects, and other changing values.

Use versioned JSON for persistent player progression.

## 25.7 Navigation

Use NavMesh or another tested pathfinding approach for the chair.

If the boss can jump between platforms, create explicitly supported traversal links and associated animations.

Do not allow the chair to select destinations it cannot reach.

## 25.8 2D/3D Integration

Implement:

- 3D models for gameplay geometry
- 2D cutout meshes or sprites for decoration
- Consistent sorting and occlusion
- Material presets
- Sprite atlases
- Controlled billboarding
- Optional colliders for functional 2D objects

Do not assume SpriteRenderer and MeshRenderer will automatically produce the desired depth behavior.

Test the shaders and camera configuration together.

---

# 26 — PROJECT FOLDER STRUCTURE

```text
DreamRush/
├── CLAUDE.md
├── README.md
├── docs/
│   ├── GDD.md
│   ├── ART_BIBLE.md
│   ├── TECHNICAL_ARCHITECTURE.md
│   ├── CHAIR_LEVEL_DESIGN.md
│   ├── CARD_SYSTEM.md
│   ├── ASSET_MANIFEST.md
│   ├── TEST_PLAN.md
│   └── RESEARCH_LOG.md
│
├── .claude/
│   └── skills/
│       ├── game-design-review/
│       │   └── SKILL.md
│       ├── 3d-platformer-controller/
│       │   └── SKILL.md
│       ├── boss-encounter-design/
│       │   └── SKILL.md
│       ├── art-direction-guardian/
│       │   └── SKILL.md
│       ├── blender-asset-pipeline/
│       │   └── SKILL.md
│       ├── unity-system-architecture/
│       │   └── SKILL.md
│       ├── game-feel-polish/
│       │   └── SKILL.md
│       └── playtest-qa/
│           └── SKILL.md
│
├── ArtSource/
│   ├── Blender/
│   ├── Illustrations/
│   ├── Concepts/
│   └── Textures/
│
└── UnityProject/
    ├── Assets/
    │   └── DreamRush/
    │       ├── Art/
    │       ├── Audio/
    │       ├── Animations/
    │       ├── Materials/
    │       ├── Shaders/
    │       ├── Prefabs/
    │       ├── Scenes/
    │       ├── Scripts/
    │       ├── ScriptableObjects/
    │       ├── UI/
    │       ├── VFX/
    │       └── Tests/
    ├── Packages/
    └── ProjectSettings/
```

---

# 27 — CLAUDE SKILLS: REQUIRED WORKFLOWS

Create project-specific skills according to the official Claude Code Skills format.

Each skill should have:

- Valid YAML frontmatter
- Clear description
- Explicit activation purpose
- Relevant project files
- Instructions
- Quality checks
- Output expectations

Do not create dozens of redundant skills.

Create only skills that improve the development workflow.

## SKILL 01 — game-design-review

Purpose:

Check whether proposed mechanics support fun, replayability, and coherent player decisions.

Activation:

Before adding a significant gameplay mechanic.

Required analysis:

- What player problem does this solve?
- What meaningful choice does this add?
- How does it interact with existing mechanics?
- Does it create a dominant strategy?
- Can the player understand the result?
- How will this be tested?

Output:

A short design review and acceptance criteria.

## SKILL 02 — 3d-platformer-controller

Purpose:

Build and validate the player controller.

Focus:

- Camera-relative movement
- Acceleration
- Jump buffering
- Coyote time
- Ground detection
- Camera collision
- Movement responsiveness
- Landing readability

Output:

Controller implementation, configuration, and movement test scene.

## SKILL 03 — boss-encounter-design

Purpose:

Design and implement the chair encounter.

Focus:

- Fair AI
- Escape destination quality
- Exposed vulnerability windows
- Multiple victory methods
- Anti-softlock logic
- Boss feedback
- Repeated-play enjoyment

Output:

Boss behavior diagrams, implementation, and test cases.

## SKILL 04 — art-direction-guardian

Purpose:

Prevent inconsistent game art.

Focus:

- Applebook Surrealism
- Shape language
- Color palette
- Material consistency
- Silhouette clarity
- 2D/3D integration
- Visual hierarchy
- In-game readability

The skill must reject assets that are visually impressive but do not fit the game.

Output:

Art review, specific corrections, and approval/rejection.

## SKILL 05 — blender-asset-pipeline

Purpose:

Create production-ready stylized assets in Blender.

Focus:

- Modeling
- Scale
- UV
- Materials
- Rigging
- Animation
- Export
- Unity integration

Output:

Editable source file, exported model, prefab, and validation results.

## SKILL 06 — unity-system-architecture

Purpose:

Review C# architecture and subsystem boundaries.

Focus:

- SOLID principles where useful
- Data-driven definitions
- Testability
- Scene lifecycle
- Memory
- Event safety
- Prefab management
- Error handling

Output:

Implementation review and necessary changes.

## SKILL 07 — game-feel-polish

Purpose:

Improve the responsiveness and emotional impact of controls and interactions.

Focus:

- Jump arc
- Acceleration
- Landing feedback
- Squash/stretch
- Camera response
- Audio feedback
- Readability

Output:

Before/after tuning description and playtest validation.

## SKILL 08 — playtest-qa

Purpose:

Detect bugs, balance problems, unclear objectives, and frustrating interactions.

Focus:

- Reproducible bugs
- Seed-based tests
- Player behavior
- Boss clear rates
- Failed interactions
- Softlocks
- Performance
- Regression tests

Output:

Prioritized issue list, reproduction steps, test evidence, and retest status.

---

# 28 — FIRST ASSET MANIFEST

## 28.1 Priority 0: Character

- Apple child 3D model
- Apple child rig
- Idle animation
- Run animation
- Jump animation
- Fall animation
- Landing animation
- Sit animation
- Sleep animation
- Wake-up animation

## 28.2 Priority 0: Bedroom

- Floor
- Walls
- Ceiling
- Bed
- Pillow
- Blanket
- Nightstand
- Night lamp
- Bookshelf
- Window
- Curtains
- Rug
- Desk
- Door

## 28.3 Priority 0: Chair Stage

- Arena floor
- Room boundaries
- Chair boss
- Chair rig
- Chair animation set
- Low platform
- Cushion platforms
- Movable block
- Decorative furniture
- Card interaction props

## 28.4 Priority 1: 2D Decoration

- Paper cloud set
- Paper stars
- Wall posters
- Illustrated plants
- Chair panic symbols
- Speed-line effects
- Jump effects
- Small environmental stickers

## 28.5 Priority 1: UI

- Interaction prompt
- Card frame
- Card icons
- Sleep Stability
- Objective display
- Boss status
- Upgrade panel
- Run summary

## 28.6 Priority 1: Audio

- Bedroom ambience
- Bedroom music
- Dream music
- Chair boss music
- Footsteps
- Jump
- Land
- Chair movement
- Card effects
- Victory
- Wake-up

---

# 29 — DEVELOPMENT ORDER

## PHASE A — PROJECT FOUNDATION

1. Create Unity project.
2. Configure URP.
3. Configure Input System.
4. Configure Cinemachine.
5. Set up folder structure.
6. Establish art references and naming.
7. Create test scene.

Acceptance:

Project opens cleanly and builds without critical errors.

## PHASE B — APPLE CHARACTER

1. Block out the apple model in Blender.
2. Create initial materials.
3. Rig character.
4. Create minimum animations.
5. Import to Unity.
6. Build movement controller.
7. Implement camera-relative movement.
8. Implement jumping.
9. Tune game feel.

Acceptance:

Moving around an empty room must feel enjoyable.

## PHASE C — BEDROOM HUB

1. Graybox bedroom.
2. Implement hub camera.
3. Place bed.
4. Create interaction system.
5. Connect bed to sleep transition.
6. Connect lamp to settings.
7. Create initial bedroom assets.
8. Add lighting and sound.

Acceptance:

Player can freely explore and enter the dream without using a conventional main-menu Play button.

## PHASE D — CHAIR LEVEL GRAYBOX

1. Create tutorial area.
2. Create moving furniture.
3. Create arena.
4. Implement stage camera.
5. Test platform distances.
6. Test camera readability.
7. Test traversal shortcuts.

Acceptance:

The stage is navigable and enjoyable before final art.

## PHASE E — CHAIR AI

1. Implement navigation.
2. Implement alert/flee.
3. Implement panic.
4. Implement fatigue.
5. Implement stumble.
6. Implement seating interaction.
7. Implement boss victory.
8. Implement recovery logic.

Acceptance:

Player can catch and sit on the chair through the baseline method.

## PHASE F — CARD SYSTEM

1. Create card definitions.
2. Create three-choice UI.
3. Implement inventory.
4. Implement targeting.
5. Implement effects.
6. Create six starter cards.
7. Integrate card effects with chair.
8. Test multiple victory methods.

Acceptance:

At least three card-based alternative solutions work correctly.

## PHASE G — ART PASS

1. Replace graybox with consistent assets.
2. Refine player model.
3. Refine chair model.
4. Add illustrated 2D cutouts.
5. Create material presets.
6. Implement lighting.
7. Add VFX.
8. Improve animation transitions.

Acceptance:

The game has a coherent and distinctive visual identity.

## PHASE H — AUDIO & UI POLISH

1. Add SFX.
2. Add music.
3. Improve UI feedback.
4. Add boss intro.
5. Add victory animation.
6. Add wake-up transition.
7. Add basic bed upgrade UI.

## PHASE I — PLAYTEST & REFINEMENT

1. Test with new players.
2. Record play sessions where permission is given.
3. Measure boss completion.
4. Identify confusing interactions.
5. Tune movement.
6. Tune chair AI.
7. Tune card effects.
8. Fix bugs.
9. Test multiple seeds.
10. Verify the full loop.

---

# 30 — FIRST PLAYABLE ACCEPTANCE TESTS

## Movement

- Character moves consistently in all intended directions.
- Jump height responds correctly.
- Player lands predictably.
- Camera does not clip into walls.
- Character does not get stuck on ordinary geometry.
- Player can use both keyboard and controller if supported in the build.

## Bedroom

- All essential interactions work.
- Bed starts the dream.
- Lamp opens settings.
- Closing menus restores control.
- Returning from a dream places the player correctly.

## Cards

- Exactly three distinct starter cards appear.
- Player chooses one.
- Selected card enters inventory.
- Cooldown works.
- Invalid targets do not trigger effects.
- Card feedback matches the effect.

## Chair

- Chair notices the player.
- Chair flees.
- Chair remains within valid gameplay space.
- Chair occasionally creates a reachable opening.
- Chair can become exhausted.
- Chair reacts to valid card effects.
- Seat interaction requires correct conditions.
- Victory triggers only once.

## Failure

- Sleep Stability reaching zero triggers wake-up.
- Gameplay stops correctly.
- Bedroom loads.
- The player can start again.
- No temporary boss/card state leaks into the next run.

## Progression

- Dream Memory is awarded correctly.
- Bed upgrades deduct currency once.
- Upgrades affect subsequent runs.
- Persistent data loads correctly after restarting.

## Art

- Character silhouette is readable.
- Objects follow the art bible.
- 2D decorations are integrated correctly.
- Gameplay objects stand out.
- Lighting does not obscure landing surfaces.
- UI remains legible during movement.

---

# 31 — PLAYTEST PLAN

## 31.1 Participants

Start with approximately 8–12 testers who have not been told the chair solution.

## 31.2 Session

Ask the tester to:

1. Explore the bedroom.
2. Discover how to start the dream.
3. Select a card.
4. Learn movement.
5. Complete the tutorial.
6. Face the chair.
7. Attempt to win.
8. Decide whether to replay.

Do not explain the answer to the boss unless the test has intentionally switched into an assisted mode.

## 31.3 Metrics

Track:

- Time to discover bed interaction
- Time to learn jumping
- Number of missed landings
- Number of camera corrections
- Chosen card
- Card use frequency
- Chair chase duration
- Number of successful seating attempts
- Victory method
- Number of failed attempts
- Requests for hints
- Voluntary replay

## 31.4 Qualitative Questions

Ask after the session:

- Which part was most enjoyable?
- Which part was frustrating?
- Was the chair funny?
- Did the card feel useful?
- Did you understand why you succeeded?
- Did you understand why you failed?
- Would you like to defeat the chair another way?
- Did the art make the game memorable?
- What would you change first?

## 31.5 Recommended Prototype Success Criteria

Provisional targets:

- Most testers understand how to begin a dream without assistance.
- Most testers understand the chair's victory objective.
- At least 60% complete the chair encounter without direct solution coaching.
- At least half voluntarily choose to replay or explore another solution in the test setting.
- No critical softlocks.
- No major input or camera defects.
- Movement satisfaction median at least 4/5.

These are project test targets, not published industry benchmarks.

---

# 32 — FUTURE GAME EXPANSION

After the chair Vertical Slice succeeds, expand toward the complete Roguelite.

## Future Bosses

### THE TITANIC

Objective:

Bring the ship to its destination.

Focus:

Traversal, spatial manipulation, scale-changing cards.

### THE MOON

Objective:

Make the Moon fall asleep.

Focus:

Environmental state, sound, gravity, and timing.

### THE NEVERENDING BOOK

Objective:

Create an ending.

Focus:

Rule manipulation, paper and story interactions.

### THE KNOCKER

Objective:

Stop the knocking.

Focus:

Rhythm, sound, timing, and environmental puzzles.

Use an original character and sound design rather than copying existing meme artwork.

## Future Roguelite Features

- Multiple randomized dream stages
- Larger card collection
- More enemies
- Dream anomalies
- Bed upgrade trees
- Dream Diary
- Challenge runs
- Hidden victory methods
- Additional apple-world locations

These must remain secondary until the first level passes testing.

---

# 33 — RESEARCH DOCUMENTATION REQUIREMENTS

Before implementing complex features, perform focused research and record the findings.

Create RESEARCH_LOG.md with:

```text
Research Topic
Design Question
Sources Consulted
What the Source Actually Supports
Proposed Application
Trade-offs
Prototype Test
Decision
Date
```

Do not copy techniques without understanding why they fit the project.

## Mandatory Research Areas

### 3D Platforming

- Camera-relative movement
- Jump forgiveness
- Ground shadows
- Camera tracking
- Landing precision

### 3D Level Design

- Four-part level progression
- Player guidance
- Spatial readability
- Multiple traversal routes
- Boss arena layout

### Art Direction

- Shape language
- Cohesive palettes
- Stylized materials
- 2D/3D integration
- Silhouette readability

### Blender Production

- Model export
- Character rigging
- Facial animation
- Material portability
- Consistent scale

### Unity Engineering

- CharacterController or Rigidbody trade-offs
- Cinemachine configuration
- NavMesh
- Input System
- URP materials
- ScriptableObjects
- Test Framework

### Game Feel

- Acceleration
- Jump arc
- Animation timing
- Impact feedback
- Camera motion
- Sound response

### Boss Design

- Fair pursuit behavior
- Multiple win conditions
- Readable state changes
- Softlock prevention
- Replayability

---

# 34 — TECHNICAL REFERENCE LIBRARY

Official technical resources:

**Unity Character Controller**  
https://docs.unity3d.com/Manual/class-CharacterController.html

**Unity Input System**  
https://docs.unity3d.com/Manual/com.unity.inputsystem.html

**Unity Cinemachine**  
https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html

**Unity AI Navigation**  
https://docs.unity3d.com/Manual/com.unity.ai.navigation.html

**Unity ScriptableObject**  
https://docs.unity3d.com/Manual/class-ScriptableObject.html

**Unity Test Framework**  
https://docs.unity3d.com/Manual/com.unity.test-framework.html

**Unity Model Import**  
https://docs.unity3d.com/Manual/FBXImporter-Model.html

**Blender Manual**  
https://docs.blender.org/manual/en/latest/

**Claude Code Skills**  
https://code.claude.com/docs/en/skills

**Agent Skills Specification**  
https://agentskills.io/specification

Game design case studies:

**Nintendo — Super Mario 3D World Developer Interview**  
https://iwataasks.nintendo.com/interviews/wiiu/super-mario-3d-world/0/2/

**Nintendo — Super Mario 3D Land Developer Interview**  
https://iwataasks.nintendo.com/interviews/3ds/super-mario-3d-land/0/3/

**GDC — Thinking in 3D: The Development of Super Mario 3D Land**  
https://www.gdcvault.com/play/1015833/Thinking-In-3D-The-Development

**Game Developer — The Secret to Mario Level Design**  
https://www.gamedeveloper.com/design/the-secret-to-i-mario-i-level-design

**The Plucky Squire — 2D/3D Development Interview**  
https://www.unrealengine.com/developer-interviews/the-plucky-squire-blends-2d-and-3d-visuals-in-aesthetically-pleasing-and-mechanically-interesting-ways

**GDC — Ten Principles for Good Level Design**  
https://www.gdcvault.com/play/1019023/Ten-Principles-for-Good-Level

Some GDC material may require an account or specific access. Do not claim to have watched material that is inaccessible.

---

# 35 — FINAL INSTRUCTIONS TO CLAUDE OPUS

Read the entire document.

Treat it as the authoritative initial design specification.

Before implementation:

1. Inspect the available project and tools.
2. Check whether Blender and Unity can actually be operated in the current environment.
3. Identify missing dependencies.
4. Review the art bible.
5. Establish gameplay architecture.
6. Create a prioritized implementation plan.
7. Create necessary project-specific Claude Skills.
8. Start with the smallest playable movement prototype.

If graphical editing tools are available, create and inspect assets in Blender and test them inside Unity.

If a tool is unavailable, explicitly report the limitation. Do not pretend a Blender model, Unity scene, screenshot, test, or build has been created when it has not.

Never compromise the game's core creative identity simply to complete tasks quickly.

Never prioritize large quantities of generated assets over consistency.

Never replace a real mechanic with a cosmetic animation and report it as functional.

At each milestone, report:

- Completed features
- Files changed
- Tests run
- Actual results
- Known bugs
- Design risks
- Art consistency findings
- Next milestone

## First Actual Assignment

Build the Apple Character Movement Prototype.

Then build the Apple Bedroom Hub.

Then build the Chair Stage Graybox.

Then implement the Chair Boss behavior and baseline victory method.

Then implement the three-card system and alternative chair solutions.

Then perform the art pass, animation polish, and sound design.

**The first real goal is not a complete Roguelite.**

**The first real goal is a charming apple character in a beautiful bedroom, who falls asleep, enters a strange living room, and has an incredibly fun encounter with a chair that refuses to be sat on.**

## Final Vision

DREAM RUSH should make ordinary things extraordinary.

It should make a chair feel like a memorable boss.

It should make an apple feel like a real character.

It should make random cards feel like opportunities for invention.

And it should make the player want to enter the next dream.

**DREAM RUSH — WHERE THE IMPOSSIBLE IS JUST ANOTHER BAD DREAM.**

**END OF MASTER GDD V2.0**