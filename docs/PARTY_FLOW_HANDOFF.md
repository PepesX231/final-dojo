# Party flow implementation handoff — 2026-10-25

Status: implementation checkpoint, NOT release-ready. Paused at the user's requested approximately 20% remaining usage threshold. Last observed five-hour remaining usage: 19%.

## Git and safety
- Started from main a75e950, initially aligned with origin/main.
- Read requested commits eaa7053285c56d5248373c5a096c4f2f857a102f and 74eabe8f0f96bcbd981269c68f7e54d8c8588ae3; their trees match.
- Existing backup branch: backup/pre-party-flow-a75e950.
- No remote push, pull, reset, force push, or deletion of user work during this implementation.
- Preserve authored MamasKitchen. New kitchen geometry is targeted fridge interior/door and rotten food; no house rebuild.
- QA builds and screenshots are ignored, not source assets for commit.

## Implemented, with verification limits
1. Main-menu Play routes to co-op lobby; separate Co-op button hidden. Initial fruit preview resets to normal scale. Enter start removed. Host start requires at least two connected players; one host presses Start (not a separate ready vote per player).
2. Walking uses swept body separation, sprint collisions retain knockdown. Math check passed; moving two-client gameplay still needs verification.
3. Host-authoritative majority votes after defeat choose Lobby or Retry; one of two votes is insufficient. Disconnect recalculates votes. No end-to-end two-peer vote test completed yet.
4. Reliable fruit selection and scene rebinding preserve each slot's fruit across transitions. Lobby host Banana/client Watermelon verified in native client. Retry/Ranking fruit preservation still unverified.
5. Actual MC_Ranking scene, per-player points and completed-task list, selectable score rows, crown, stage and movable avatars. Scene created and initial screenshot reviewed. Latest enlarged avatar/crown layout and multiplayer movement need visual/runtime verification.
6. MC_Loading plus persistent tip curtain, six bilingual cards, next-tip button, asynchronous scene load, readiness acknowledgement, first world snapshot acknowledgement, transition epochs and shared release. Same flow used by Retry and scene transitions. Native client showed curtain; complete barrier remains unverified after latest fix.
7. Extinguisher spray aims toward a fire target, with remote spray rendering. Needs two-peer visual test at different angles.
8. Held-item filter shared between local selection and server validation; unrelated pickups such as chair cannot replace held bucket. Targeted held-bucket/chair check passed. Full tool compatibility audit still needed.
9. Cabinet opens near a carried pot; stash scoring added. Visual close/open/hide behavior needs playtest.
10. Fridge has visible interior/door and removable rotten food, disposed in bin. Targeted spoil -> pickup -> trash test passed. Network replication and visual door consistency need two-peer test.
11. TAB hint/button, danger-sorted quest rows, larger bars and bounded text. Needs full Thai/English resolution review while many problems are active.
12. Defeat camera pans toward causal problem and displays short reason, followed by votes. Needs true failure scenarios and two-peer visual test.
13. F8 toggles presentation mode (host authority): 1 Lobby, 2 Loading/Game, 3 Play, 4 Mom, 5 Ranking, 6 Failure. Current 4/6 require a gameplay scene; 6 from Ranking does not yet navigate back to gameplay. Finish this cross-scene behavior before presentation.
14. Per-player scoring added for hazard repair, props, pot cleaning, extinguisher, trash, cabinet. Duplicate award check passed. Audit mop/water/smoke and all quest completion paths for missing credit.

## Current blocker and latest patch
Native editor Host + Windows build Client successfully connected as two different fruits. On starting gameplay the new registry guard detected different scene signatures and correctly held the loading barrier. Neither player should start simulating until the registry and first snapshot match.

Latest patch (compiled and built, not yet verified end-to-end):
- Replaced global sibling-index paths with hierarchy names plus ordinal among siblings sharing the same name. Editor-only/unrelated objects must not shift network IDs.
- Protocol signature version 2; development registry manifests written to Application.persistentDataPath/MamaRegistry-Host.txt and MamaRegistry-Client.txt. Compare these FIRST if mismatch recurs. Do not remove the consistency check to hide the mismatch.
- Loading mismatch error persists and is sent to all peers; Return to Lobby remains available.
- Fixed unstable procedural body lean spring using bounded 1/120-second integration steps. Previous slow/background frames produced NaN rotations. Stress test under low frame rate still needed.
- Flow canvas uses Expand scaling so bottom controls remain visible in non-16:9 windows; loading heading spacing and Start-label auto-fit refined.

## Validation evidence
- Unity compiler: no errors after latest scripts.
- Windows development build build-fee3a1f75b: succeeded, 0 errors, 12 warnings, about 208 MB, 82 seconds.
- Latest executable: Builds/PartyFlowQA2/MamasComing.exe. QA build starts in Lobby intentionally; release build must start in MC_MainMenu.
- Prior build build-04a037448b also succeeded. Do not mix QA1 and QA2 protocol versions.
- Warnings largely Unity MCP/package assembly references and Unity-services/project-upload notices. Online Relay has NOT been verified.
- Native two-peer connection and lobby fruit selection verified on previous build; latest client launch had no targetable window at inspection, while its Player.log shows engine startup. Computer-use old-window recovery repeatedly reported minimized/user-input state. No claim of latest multiplayer test passing.
- Shell-background QA failed to connect in the restricted execution environment. These FAILED logs are not evidence that gameplay tests passed.
- Opt-in MamaCoopProbe under UNITY_EDITOR || DEVELOPMENT_BUILD provides repeatable assertions with -mamaflowqa -mamahost and client -mamaflowqa -mamajoin 127.0.0.1 -mamaload-delay 2. It has not completed a successful two-process run.
- Editor exited Play mode at pause. Native test executables may still be open/minimized.

## Next work, in order
1. Run latest build as Client against same-source Editor Host. Compare registry manifests if barrier mismatch persists. Test delayed client and disconnect/recovery; no timer/simulation before both ready.
2. Verify host visibility, distinct item ownership, walk block/sprint knockdown, two-vote Retry, fruit identity after Retry, Ranking movement, and return to connected Lobby. Resolve any failures before adding polish.
3. Recapture six loading tip photographs with hazards visibly active. Current photos use real house objects but some show clean/inactive scenes (not adequate final hazard illustrations).
4. Review Ranking latest scale/crown, vote panel, HUD and all Thai/English labels in 16:9 and non-16:9 windows. Improve visual quality and verify every quest awards the correct player.
5. Complete cross-scene presenter shortcuts, especially Failure from Ranking; document numbers in final delivery.
6. Test actual defeat causes, cabinet/pot, fridge/food/trash, extinguisher aim and held-item prompts in both peers.
7. Build a final title-first release candidate; run two standalone clients / preferably two computers. Online service/Relay test remains outstanding.
8. Review diff, report final tested scope and limitations; push only as an intentional separately recorded Git action. Do not describe this checkpoint as a finished release.

## Checkpoint Git outcome
Attempted local branch work/party-flow-validation and checkpoint commit, but sandbox denied writes to .git/refs and .git/index.lock. Neither branch nor commit was created. All source changes remain saved, uncommitted on main at a75e950. No push/pull occurred. Do not discard Changes in GitHub Desktop.

