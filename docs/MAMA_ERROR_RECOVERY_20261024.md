# Mama compile / Git recovery — 2026-10-24

Active game: MAMA'S COMING!, `Assets/MamasComing/Scenes/MamasKitchen.unity`.

## Fixed

- Removed unresolved stash-apply conflict markers in KidBody (18 conflict blocks) and PlayerController (1 block). Compared both Git stages and the merged text first: the upstream version retains selected-fruit startup, the apple template, knock-down/reset handling, the public hold-clip API, active-character binding, and matching recovery timing. No distinct newer stash feature was discarded. Both original stages, conflicted files and the pre-fix Git index were preserved under `DevBridge/error-recovery-20261024/`. Git's unmerged index entries are resolved.
- GameManager contained a partial two-storey WIP change without HouseLayout, plus a literal backtick-r/backtick-n that commented out XZ. Restored its previously committed nine-room logic from `0c9dbb8`, after verifying that these were the only differences and that the authored scene still uses the original layout. The two-storey WIP commit `a487560` remains in history; its scaled room table was not imposed on the current scene.
- Removed unreachable disabled mop-drip code and unused FX buffer assignments. Marked three intentional inherited-member shadows explicitly with `new`, preserving their behavior and serialized field names.

Compilation after the fixes: **Assembly-CSharp 0 errors / 0 warnings**, Editor assembly 0 errors. The source whitespace check excludes normal Unity YAML empty-value formatting.

## Fresh validation

- Mama staged virtual-input review: **62/62 passed**, initially `review-003724.txt` at 00:37, and again after restoring the original editor path in `review-010331.txt` at 01:03 Bangkok time on 24 October.
- Banana staged offline rig/carry review: **33/33 passed**, `review-003932.txt`, 24 October 00:39 Bangkok time.
- No remaining conflict markers in project C# or unmerged Git index entries.
- Git LFS object integrity: `Git LFS fsck OK`.
- Scanned 2,005 serialized Mama files against deleted DreamRush asset GUIDs, allowing moved assets with preserved GUIDs: **0 references to removed targets**. Raw audit: `DevBridge/error-recovery-20261024/dependency-audit.json`.

These are automated/staged Editor checks, not human or real online multiplayer approval. The unused historical campaign was not restored or regenerated.

Final durable evidence: `docs/evidence/mama-error-recovery-20261024/`. The post-restore runtime check confirms GameManager initialized, player ready, and zero missing scripts. Scene file contents were preserved while testing the rejected short-path workaround.

## What the thousands of Git changes are

Initial status: 3,171 entries, mostly 3,044 staged deletions and 96 detected renames. These are chiefly the old DreamRush campaign's models, materials, code, tests and resources, not Unity Library/Temp cache. Rename/deletion totals can shift as Git detects matching files differently.

The user confirmed that unused DreamRush content should go. Required apple art, movement and shaders remain under `Assets/MamasComing/Shared/AppleKid/`, with their GUIDs retained. Some source namespaces remain `DreamRush` for compatibility. The main Mama and Banana reviews pass after that cleanup.

Excluded the new machine-local `.mcp.json` and new automatic `Assets/_Recovery/0 (9).unity` + meta from staging, preserving them on disk. Added ignores for future local MCP/recovery files. Existing already-tracked historical recovery scenes are unaffected by this ignore rule. Added four missing Unity `.meta` files beside the already-staged menu folder/scripts.

## Commit recommendation

**Commit the verified work, but keep separate purposes reviewable.** The file count alone is not a reason to reject a commit.

1. Compile/conflict fixes, warning cleanup, `.gitignore`, current project instructions and this report.
2. Removal of unused DreamRush content and relocation of Mama's required shared assets, including matching `.meta` files and path updates.
3. New Banana assets / unfinished menu work separately, with their WIP status explicit where relevant.

Keep `Assets/` plus `.meta`, `Packages/`, `ProjectSettings/` and editable source assets needed to reproduce the game. Binary art/audio uses the existing LFS attributes. Do not add Library, Temp, DevBridge output, tool downloads or machine-local MCP endpoints.

Commit records a version locally; push sends those commits to GitHub. No commit, push, history rewrite or blanket staging reset was performed during this repair. The pre-existing large staged cleanup remains available for review. Automatically added AI-tool package extensions, if still present in `Packages/manifest.json`, are separate from the gameplay fixes and should not be swept into the cleanup unnoticed.

## Editor tooling notice

The AI Game Developer package unconditionally logs an advisory as an Error whenever `Application.dataPath` contains a space. This is separate from game compilation. Tested the existing Windows short path `C:\Pepes\FINALD~1` without moving the project. It removed that advisory but Unity's MonoScript-to-class mapping then failed for some components, so the short-path workaround was rejected and the editor was reopened through the unchanged original `C:\Pepes\final dojo` path. No broken alias scene was saved. The vendor's space-path advisory may remain on startup; no package-cache log suppression or project-directory move was applied. Runtime/Editor compiler errors and the game regressions are reported separately from this advisory.

The old AI Game Developer connection also attempted an unavailable localhost server on port 24915. Stopped that failed connection using its public `KeepConnected=false` / disconnect API, retaining the plugin and its config on disk. The separate Coplay Unity MCP connection remains available for this project. Re-enable the old connection using its normal Start control when that server is available. The previous KeepConnected value was recorded under the recovery folder. This is a local tooling setting, not a gameplay change or plugin uninstall.
