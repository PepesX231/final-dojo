---
name: dream-rush-audit
description: Audit, playtest and improve this DREAM RUSH Unity project across BedroomHub, DreamGalaxy, the five active Galaxy worlds. Use for cross-level bug audits, regression evidence and replay goals; reuse the existing specialist skills for controller, boss, art and Blender work.
---

# DREAM RUSH cross-level audit

## Verified project facts

- Unity 6000.3.11f1, URP 17.3.0, Input System 1.19.0, AI Navigation 2.0.11, Test Framework 1.6.0.
- Runtime: `Assets/DreamRush/Scripts/Runtime`; builders: `Scripts/Editor`; tests: `Assets/DreamRush/Tests`.
- Current Galaxy worlds with playable scene targets: `Dream_World1`, `Dream_MidnightPalace`, `Dream_ToyBoxMoon`, `Dream_BubbleBathSea`, `Dream_MidnightTeacup`; `Dream_Openworld` is legacy, not a third current campaign world. Loop: `BedroomHub` → `DreamGalaxy` → cards → world → bedroom.
- `Dream_Chair` is the preserved P0 prototype. `Test_MovementGym` and `Test_BossMidnight` are test scenes. `Art_Target` and `SampleScene` are not shipped levels.
- Recheck `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, build settings and scene routing before relying on these facts in a later version.

## Evidence and access

Read `CLAUDE.md`, `docs/GDD.md`, the relevant level design and the latest milestone reports. Documents describe intent and historic results; distinguish those from observations in the current run. The user broadened the audit to all existing levels on 2026-10-20; record significant changes in the decision log.

Inspect Git status before edits. Most game files may still be untracked: preserve a separate pre-edit backup and content hashes for every changed file. Do not reset or regenerate whole scenes to make a small runtime fix.

Use Unity MCP only after `mcpforunity://instances` confirms the intended project. An exposed tool is not proof of an editor connection. Do not install packages or rewrite MCP settings merely to claim connectivity. If unavailable, report that and use the existing `DevBridge` after checking its source and live status. Blender assets use the existing `ArtSource` pipeline; do not replace authored models with unreviewed generated content.

## Audit pass

1. Follow the user’s 2026-10-21 scope: gameplay tests in the five actual used world scenes, BedroomHub and DreamGalaxy only. Do not run unused maps, legacy Dream_Openworld, Test_MovementGym or Test_BossMidnight, or broad content-inventory suites. Archive focused PlayMode results before changing gameplay. Wait for the final completion entry; a `RUNNING` marker is not a result. Preserve current editor scene and do not open scenes while tests are running.
2. Cover the real hub/bed/settings/pause, sequential Galaxy/card selection, World 1 chapters and chair, Palace puzzles/interiors/Count Midnight, Moon launches/timing, Sea valves/boat, Teacup bells/finale/credits, and return-to-hub persistence.
3. Label each observation: code inspection, automated simulation, rendered image, keyboard play, or human subjective playtest. Teleporting through chapters does not prove the traversal path is playable.
4. Use `playtest-qa` for bug priority and repro steps. For a gameplay bug, add a focused behavioural test, observe its failure on the baseline, then fix the narrow cause and retest it plus affected suites.
5. Check static state and event subscriptions across scene reloads; collectible totals and duplicate awards; overlapping region/checkpoint triggers; camera state after teleport; pause/cutscene input; and per-level save attribution.
6. Use `game-design-review` before adding mechanics. Make replay goals optional; retain the existing movement envelope, card property/status interactions, manual sit requirement, enemy telegraphs and accessibility settings. Avoid increasing rewards or difficulty without a measured reason.
7. Capture the same scene, pose and resolution for visual comparisons. Frame-time samples from an unfocused Editor include editor overhead and throttling: label them accordingly, report sample count/p50/p95, and never claim GPU or release-build performance from those numbers.

## Deliverable

Append a dated report with changed paths, before/after evidence, exact test totals, fixed and open issues with repro/expected/actual/priority, game design decisions, performance conditions, and untested items. Update `docs/MILESTONE_REPORTS.md` only with results actually obtained. Keep the distinction between implemented content and a milestone that has passed human playtesting.

## Current Palace brief (user decision, 2026-10-20)

Use detailed stylized interiors AND exteriors, illustrated matte wood/paper/brass, sunset-to-midnight with limited neon accents, fitting Pip and the paper residents. Do not flatten everything into bare primitives or mix gritty facades with untextured pink rooms. Run `art-direction-guardian`; a partial material pass is REVISE until composition, silhouettes and motion are actually verified.

Normal Galaxy entry to `Dream_MidnightPalace` requires a persisted `Dream_World1` clear through `Flow.WorldAccess`. A prototype Chair win, an arbitrary diary win count, a failed run or a Palace-only record is not proof of World 1 completion. Use temporary saves for tests; never grant a real user's unlock for QA. Direct editor scene starts stay available.

`PalaceStylePass.Apply` / menu `DreamRush/Art/Apply detailed stylized Palace` applies targeted decoration/materials to the existing Palace scene; the full builder also calls `DressBuiltScene`. Back up the scene and affected Palace materials/profile before using it. Preserve quest references, room bounds, dynamic spawn roots, colliders, rails, fans, bells, train and boss. New decor is combined per room/material and distance-culled by `PalaceDressingVisibility`; never put gameplay children under `StyleDressing`.

Do not call a tall objective beacon a physics cable bug: inspect `HourBeacon` and its builder. Decorative lantern `Signatures/Cable` spans are separate from bridge suspension and gameplay rails. Keep the compact marker's authored scale when pulsing and preserve completion hiding.

The 2026-10-21 user authorized finishing the five worlds already named in Galaxy. CampaignRoute is the runtime route. Preserve the historic GDD future-boss ideas as backlog, not claims about these implemented scenes. See docs/CAMPAIGN_FIVE_REPORT.md for actual validation and remaining art/human playtest work.

## Suite-wide save isolation (post-M20 finding)
Legacy boss tests previously wrote Test_BossMidnight wins into the default Editor save. PlayModeSaveIsolation now wraps the whole PlayMode namespace with a temporary SaveManager and verifies the real file hash. Do not assume an entire suite is isolated because a few fixtures inject saves. Snapshot the real save before every full run and compare SHA256 after cleanup. Do not invent a rollback when no pre-run snapshot exists. Never use test-scene records to unlock World 2.


## Five-world authoring and presentation

Preserve Claude M21 Palace facades and illustrated INT_ furnishings. CampaignWorldBuilder owns only worlds 3–5 and a targeted Galaxy routing patch; do not rebuild World 1 or Palace to change campaign flow. Back up any existing scene before a targeted edit.

Flat Cylinder primitives need MeshCollider for their real mesh: the default CapsuleCollider becomes an invisible rounded obstacle when flattened. Bridges must meet island top height before crossing the island edge. LaunchPad.visual must be a unit-scale pivot separate from the stable collision surface. MovingPlatform.ExternalTarget is not consumed by its current Update; campaign boat motion is owned by StoryWorldDirector.

Cinematics must allow skip, hide gameplay HUD, restore prior input/camera/FOV/HUD states even when interrupted, respect reduced shake/flashes, and avoid blocking normal menus. Credits use original text/music, a clean UI-only camera backdrop and measured text height. Never copy Minecraft’s poem, recordings or music. Music masters/MIDI sources/license are in ArtSource/Audio and streaming AudioClip import rules are existing.

Gameplay bots must walk/launch/ride and use InteractionSystem; do not teleport, directly call win, or silently reduce victory conditions. Slow down near targets and navigate around solid props. Label synthetic input and bot runs separately from human keyboard play. Use temporary saves and verify the real save SHA256 after cleanup.


Never regenerate the player prefab as a convenience while building new worlds. Load PF_Player.prefab first. BuildPlayerPrefab calls AppleCharacterSetup, which can rewrite character materials and FBX import settings. If regeneration is actually required, snapshot the prefab, actor materials and importer metadata first, and verify the affected existing levels afterward.
