# Verification — 24 October 2026

Windows Development player build: **Succeeded, 0 errors**, 97.59 seconds. Test player: `DevBridge/fast-iteration/player/MamaReview.exe` (main house scene only).

Unity Editor runtime checks:

- Original catalog: PF_AppleKid, PF_Kid_Banana, PF_Kid_Orange, PF_Kid_Watermelon, PF_Kid_WaxApple. Active apple: CHR_AppleChild.
- UpperFloor inactive, bedroom mapped to ground-floor room 4, 23 active-scene disaster components, zero missing MonoBehaviours.
- Solo round selects 10 disaster devices, initially 2 urgent.
- Settings: solo timeScale 0, online timeScale 1; localized Quit button exists.
- Grab lease sets BeingGrabbed. Online grab transport is implemented but has not yet been exercised by the two-process bot.
- Staged punch against a carrying offline puppet: Held becomes null, planar launch speed 7.5. This directly invokes the swing, not a complete mouse-input playthrough.
- Banana rig/carry review: **33/33 passed**; full result in banana-review.txt.
- Water straight-alpha setting confirmed; final Game View and settings captures inspected.

Local networking test: two separately launched instances of the built player, localhost, headless, 35 seconds. Sandbox launches stalled during startup with thread/network errors. A permitted run outside the sandbox completed the following:

- Both peers registered two avatars with active renderers.
- Client requested fridge interaction; client's mirrored state reported Fixed=True. This randomized round may already have left the fridge inactive, so this is not standalone proof of the repair transition.
- Client acquired a party cup and dropped it; Settled=True.
- Client punch animation arrived on the host, host accepted a hit at distance 1.03, both peers emitted BOOP, and the host moved from (8.8,0,6.0) to (6.4,0.1,5.0). The client mirrored the same final position.
- Round state remained Playing and water state continued updating. Compact peer logs are in host-local.txt and client-local.txt.
- Test processes were stopped after the bounded run.

Headless visible=0 is expected and **does not verify friend visibility on a rendered screen**. Still required: rendered two-peer review, network grabs, four-player stress/latency tests, real internet Relay, all fruit action transitions, full layout/support/collision review, and human solo/co-op balance feedback. Existing authored clips were reused; no new 30-frame run cycle for every fruit is claimed.

Editor was returned to MC_MainMenu, stopped, scene clean. No commit or push was made.
