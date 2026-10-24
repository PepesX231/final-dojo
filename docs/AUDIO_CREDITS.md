# Audio Credits & Licenses — DREAM RUSH P0

The original P0 synthesis set in `Assets/DreamRush/Resources/DreamRush/Audio/` is **original, procedurally synthesised** by
`ArtSource/Audio/Scripts/make_audio.py` (numpy/scipy: sine, noise, filters, envelopes). For that P0 set, no samples, loops,
presets or recordings from any third party are used. The project releases these files as **CC0 1.0**.

| File(s) | What it is | Source | License |
|---|---|---|---|
| `SFX_Footstep_0..2` | soft padded steps (3 variations) | make_audio.py | CC0 (original) |
| `SFX_Jump`, `SFX_Land`, `SFX_Hurt` | player jump / landing thump / lose a Sleep moon | make_audio.py | CC0 |
| `SFX_Kick` | chair kicks the pinky toe (wood bonk + spring wobble) | make_audio.py | CC0 |
| `SFX_ChairStep_0..2`, `SFX_ChairGasp`, `SFX_ChairYelp`, `SFX_ChairStumble`, `SFX_ChairTired`, `SFX_ChairCreak` | the chair's wooden body + whistly "voice" | make_audio.py | CC0 |
| `SFX_UI_Click`, `SFX_UI_Open` | menus | make_audio.py | CC0 |
| `SFX_SleepChime`, `SFX_WakeUp`, `SFX_Victory` | flow stings (music-box bells) | make_audio.py | CC0 |
| `SFX_Swing_0..2`, `SFX_SwingHeavy_0..1`, `SFX_Whoosh_0..1`, `SFX_Hit_0..2`, `SFX_HitHeavy_0..1`, `SFX_Clank_0..1`, `SFX_Burst`, `SFX_Coin_0..1`, `SFX_Door`, `SFX_Grind` (loop), `SFX_Reflect`, `SFX_EnemyAlert`, `SFX_Shock`, `SFX_UiHover`, `SFX_UiBack`, `SFX_Pickup` | M15 combat / world / UI set | `ArtSource/Audio/Scripts/make_sfx.py` (numpy synthesis) | CC0 (original) |
| `SFX_Punch_0..2`, `SFX_PunchHeavy_0..1`, `SFX_BellToll`, `SFX_Ratchet`, `SFX_Tick` | M18/M19 fists (glove thumps) + clockwork boss (bronze bell, winding-key ratchet, strike tick) | `ArtSource/Audio/Scripts/make_sfx_m18.py` (numpy synthesis) | CC0 (original) |
| `MUS_Bedroom`, `MUS_Dream`, `MUS_Boss`, `MUS_World1`, `MUS_Palace`, `MUS_Midnight`, `MUS_Galaxy`, `MUS_Interior` (OGG, 26–50 s seamless loops) | M15 soundtrack: original compositions written in code (`ArtSource/Audio/Scripts/make_music.py` + `dr_music.py`, MIDI in `ArtSource/Audio/MIDI/`) | rendered with **GeneralUser GS v2.0.3** SoundFont by S. Christian Collins (https://www.schristiancollins.com/generaluser) via FluidSynth | compositions: CC0 (ours); SoundFont license: "use without restriction for your own music creation, private or commercial" (see its LICENSE.txt) |

The card SFX (`SFX_Card_*`) and `SFX_LawWarn` were deleted with the card system on 2026-10-23.

Status: P0 SFX are **placeholder quality**; the M15 set + soundtrack replace the old 13–23 s music placeholders (the old WAV loops were deleted).
P0 note: They are tuned to the Applebook tone (soft, toy-like, music box and wood,
nothing harsh) and loop seamlessly, but a composer/sound designer pass is planned before any release.
Rule for future additions: every new audio file gets a row here with source + license before it enters the project.


## Five-world campaign soundtrack

| Files | Composition / rendering | License |
|---|---|---|
| MUS_ToyBoxMoon.wav, MUS_BubbleBathSea.wav, MUS_MidnightTeacup.wav, MUS_DreamFinale.wav, MUS_Credits.wav | Original seeded MIDI compositions, make_campaign_music.py + dr_music.py; rendered with FluidSynth 2.6.1 and GeneralUser GS SoundFont. MIDI sources included. | Original composition/render: project-owned; GeneralUser GS permits music creation, private or commercial. Full terms: ArtSource/Audio/Licenses/GeneralUser-GS-LICENSE.txt. |

Sources: [GeneralUser GS](https://github.com/mrbumpy409/GeneralUser-GS), [author](https://www.schristiancollins.com/generaluser.php), [FluidSynth](https://github.com/FluidSynth/fluidsynth).
These tracks contain sampled instruments from the SoundFont; the P0 no-samples statement does not apply to them. No Minecraft poem, recording or composition was copied. WAV masters import as streaming Vorbis in Unity. Measured RMS targets -19/-17.5/-19/-16.5/-20 dBFS; zero clipped samples. Subjective listening approval remains pending.