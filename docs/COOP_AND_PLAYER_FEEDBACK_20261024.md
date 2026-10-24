# Co-op and player feedback — 24 October 2026

## Changes

- Fixed the Relay join race: register callbacks before the SDK starts connecting, register named messages at NetworkManager startup, and make the client request/retry the roster until it receives its real slot. Do not send slot-0 movement or interactions before initialization. Roster responses are validated and create the host's remote avatar. Repeated requests are safe; a running host also resends the start event.
- Added an illustrated first screen with three real in-game captures: stop the stove/leak, pick up an item, return it to its home or hide it. Thai/English, a co-op/sound settings button, a clear start button, and F1 to reopen. Solo instructions pause the round; online instructions block local input while the shared game continues. Mouse locking yields to the guide.
- Item descriptions now state the action: take the teddy to the bedroom and press Q beside the pillow. A destination marker remains visible at the screen edge while carrying a returnable prop, including its room and distance. Larger context copy and a wider footer improve legibility.
- Sprint collisions knock both players down into fruit and drop carried items. Wet-floor slips preserve running momentum. Knock-down rotation is based on travel distance around the body centre, followed by recovery. Separate limb renderers are hidden during the fully curled phase.
- Kept Apple's upright walking gait and its procedural animator. For Banana, Orange, Watermelon and WaxApple, smoothed animation transitions/playback, matched cadence to actual movement, reduced the extra body wobble, and stopped treating motor velocity against a wall as walking. Added the object-fruit body alias to prevent null body action poses.
- Sound controls now mute pooled effects, hazard loops and the detector's direct audio source. Hiss/gurgle fades use unscaled time so lowering effects volume in the paused settings menu works.

The authored house, character meshes, materials and Apple's walking code were retained. Existing user edits to MamasKitchen and build settings were not reset. Guide pictures were captured from the game; no generated replacement artwork was used. Solo gameplay has clearer instructions and feedback; a new solo mission campaign or new animation clips are not claimed.

## Verification

### Relay: actual private room code

The original client could connect but remained `mySlot=0, avatars=1`, with no host avatar. After the fix, a separately launched client joined the Editor host through a real private Relay code, received slot 1, and had two player avatars. Explicit camera renders from that client show the remote host with the P1 tag. The client picked up and returned a cup, approached the host, punched, and received the host's resulting movement from approximately `(4.3,0,3.0)` to `(4.3,0,1.2)`.

This uses two processes on one computer routed through Relay; it is not a three-player or separate-device latency test. The temporary opt-in camera probe used to render the hidden client was removed from Assets after the test.

### Finished-build LAN regression

Two separate player processes: the host began first, then the client joined six seconds later. Client acquired slot 1, both avatars existed, cup pickup succeeded, and the punch moved the host to the same final position mirrored by the client. No NullReferenceException, MissingReferenceException or managed Exception was found in either log. The subsequent cursor-only change was checked separately; it does not alter transport or roster code.

### UI, motion and sound

- Guide opens at Title, pauses solo time at 0, and loads all three pictures. Its layout was captured and inspected.
- Synthetic mouse-down while the guide is open during an online round leaves the cursor unlocked (`None`); closing the guide restores `Locked`. This prevents the game from stealing the guide's button clicks.
- Carrying the teddy in the living room displays the instruction to return it and an active `Bedroom · 12 m` destination marker.
- A staged collision gave both players KnockedDown=true, launch speeds 4.5 and 5.5.
- A staged slip reached LimbsOut=0 and approximately 39.5 degrees of travel-driven body rotation within the observation window; screenshot inspected.
- All four non-Apple fruit puppets entered Walk and moved their feet relative to their roots (approximately 0.07–0.15 m in the sampled interval). Their authored models/clips are reused; this is not a human assessment of animation feel.
- While paused with SFX/music at zero: water, fire, hiss and gurgle volumes reached zero, pooled effects and hazard sources were muted, and music approached zero. Test volume values were restored.
- Existing Banana rig/carry regression: all 32 functional checks passed, including authored wrist contacts and walking with carried props. The suite's final global “no exceptions” check failed because Unity repeatedly reported `TLS Allocator ALLOC_TEMP_TLS ... unfreed allocations`. The full suite is therefore **not** reported as 33/33 passing. The unrelated native allocator/Relay diagnostic remains for investigation; no log suppression or vendor package edits were applied.

Evidence: `docs/evidence/coop-and-feedback-20261024/`. Temporary test logs and probe build: `DevBridge/coop-fix/`.

## Try it

New player build: `Builds/MamasComing-CoopFix/MamasComing.exe`. Final rebuild after the cursor fix: **Succeeded, 0 build errors**, 64.06 seconds. This build opens the authored main house and its new guide directly. Use ESC → Host online / Join to share a room code. Both players should use this new build; an older build still has the original client initialization bug. Empty-hand left click / G / RB punches. F1 reopens instructions; Shift runs; Q returns items at the marked home; right click throws. Share the entire build folder, including its Data folder and DLLs.

No commit or push was performed.
