# Card removal — 2026-10-23

User request: “เอาระบบ การ์ด ออกไป ให้หมดเลย”. This supersedes the card sections of the historic GDD.

The current flow is BedroomHub → sleep → DreamGalaxy → selected world → wake/home. Removed the card draw/selection step, run card/reroll state, Q and gamepad card binding, player card component, card HUD, Dream Law scheduler/badge, card summary row, Lucky Pillow purchase/effect, card-only runtime scripts, builder, definitions/library, icons, UI frame, sounds and source generators. Removed card-only tests and updated the remaining progression tests. General combat and environmental effects remain; shared feedback now uses general-purpose sounds and paper particles. Boss/chapter title splashes and combat skill indicators are presentation, not a card mechanic.

Patched the existing player prefab and affected scene components directly, without regenerating the player or rebuilding worlds. Card instructions in World 1's authored objectives and final trial now describe observation, timing, routes and manual seating. The diary lists the remaining environmental victory methods. Existing `pillowLevel` is inert legacy save data; no real save or currency was migrated. Upgrade and sound enum IDs retain the surviving serialized values.

Backups and original SHA256 hashes are in `DevBridge/card-removal-backup/manifest.json` (local, ignored by Git). BedroomHub was not edited by this task. Concurrent Palace scene changes were not edited or reverted by this task.

## Validation

- Static serialized-asset audit: no references to deleted asset/script GUIDs remain in Assets YAML. Removed runtime card directory is empty/absent.
- Unity 6000.3.11f1 compilation: Runtime, Editor, EditMode tests and PlayMode tests compiled with zero errors and zero warnings in the relevant assembly results.
- First focused run: **1 passed, 2 failed**. Preserved in `DevBridge/card-free-before-tests.txt`. The strict scene check found pre-existing missing-script problem-solving nodes in World 1; the flow test also received an unrelated external MCP authorization error.
- Fixed the node serialization by giving `CognitiveTrialNode` its own C# script and stable GUID and updating the six existing scene references. No node transforms or gameplay data were regenerated. The original scene backup contains the old embedded MonoScript references.
- The new fixture expects only the exact external connector authorization error; game errors remain failures. It does not change plugin settings, credentials or connection permissions.
- Final focused PlayMode run: **3 passed, 0 failed, 0 skipped**, 7.7 seconds of test execution, all five used scenes covered by the scene-loading case. Evidence: `DevBridge/card-free-final-tests.txt`. The second run's **2 passed, 1 failed** result is preserved in `DevBridge/card-free-second-tests.txt`; its remaining failure was the background-thread connector error. The expectation handler now receives threaded logs and still matches only that exact external error.
- Runtime screenshot reviewed: `DevBridge/card-free-world1.png` shows World 1 objective/stability UI with no card slot. This is the existing camera-render capture path, with its known rendering differences from Game View; it is not final-art approval.
- Remaining bed-upgrade EditMode regressions: **4 passed, 0 failed**, 0.4 seconds. Evidence: `DevBridge/card-free-bed-upgrades-tests.txt`.

Unity stalled during domain reload before the first real run. The user explicitly approved restarting the stuck Editor. Tests use the namespace-wide temporary-save fixture, which logged that the real Editor save SHA256 was unchanged after the focused runs, including the final passing run.

The focused tests exercise real sleep/Galaxy/World 1/wake flow, load all five used worlds, check player/HUD/input and missing scripts, check the six puzzle nodes, and verify that an old pillow save cannot purchase the removed upgrade. These are automated regressions, not a human playthrough or subjective fun evaluation.
