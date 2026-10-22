# LOBBY DESIGN — "THE NIGHT ROOM" (M11)

User brief (2026-10-20): the old lobby was weak; no separate title screen — **the lobby is the entry screen**; study
games with great lobbies and a unique "chaos" art style (Persona 5 / 3 Reload, Honkai: Star Rail); Persona-like is
fine but not always red/black — colours can change with the time of day.

## 1. References studied (what we take, not what we copy)
| Game | What makes its lobby work | What DREAM RUSH takes |
|---|---|---|
| Persona 5 / P5R | menu is a moving poster: ransom-note type, hard diagonals, every press animates | ransom logo (each letter own tile/font/tilt), stair-stepped diagonal menu, slash transition |
| Persona 3 Reload | one strong colour mood, water/moon motion, menu items that "lean in" | one palette per mood, selected item leans + grows, idle motion everywhere |
| Zenless Zone Zero | sticker-bomb chaos: tape, halftone, CRT grain, marquees | torn tape slabs, halftone corners, grain + scanlines, scrolling marquee bands, sticker storm |
| Honkai: Star Rail | the lobby is a *place* (the Astral Express) you can walk around | the bedroom IS the lobby; WALK AROUND drops you in, TAB brings the menu back |

## 2. The place
Pip's bedroom as a cut-away toy diorama (back walls N + W, open S + E with a toy fence) floating in the dream sky on a
three-layer cloud cake. All props are Blender models (`build_bedroom_kit.py`: apple-headboard bed, mushroom lamp,
bookshelf, desk, wardrobe, round window, arched door, rug, rocket, sheep plush, toy train, plant, clouds, crescent
moon, ZZZ, fence). Posters/photo drawn for the game (`make_lobby_art.py`). Walls/floor use DreamToon world-space UV.

## 3. Moods (real clock, `LobbyTheme`)
| Hours | Mood | Primary / Secondary / Accent |
|---|---|---|
| 05–11 | SUNNY POP | yellow / teal / pink |
| 11–17 | CANDY RUSH | hot pink / lime / cobalt |
| 17–20 | GOLDEN HOUR | orange / magenta / violet |
| 20–05 | MIDNIGHT MODE | cyan / hot pink / violet (nebula sky) |
Each mood also sets sky, key light, ambient, fog, wall/floor tint and practical lamps (`LobbyThemeDirector`).

## 4. Menu
DREAM (slash card "GOOD NIGHT, PIP!" → bed sequence → galaxy) · WALK AROUND · BED UPGRADES · DREAM DIARY · SETTINGS ·
WAKE UP. Each item: its own camera shot (`LobbyShot_*`, subject left of centre), Pip turns toward the object
(`LobbyFocus_*`), Thai subtitle, help strip. Keyboard/gamepad/mouse.
