# Asset Manifest — P0 Vertical Slice
สถานะ: `TODO` → `GRAYBOX` → `BLOCKOUT` → `IN_REVIEW` → `APPROVED` (ผ่าน art-direction-guardian) → `INTEGRATED` (ทดสอบใน Unity แล้ว)
Source อยู่ `ArtSource/`, ไฟล์เข้าเกมอยู่ `Assets/DreamRush/Art/` · ตั้งชื่อตาม GDD §6.3

## P0 — Character
| ID | ไฟล์ | สถานะ | Art Review | หมายเหตุ |
|---|---|---|---|---|
| CHR_AppleChild (graybox) | สร้างจากโค้ด `PlayerRigBuilder` | GRAYBOX | n/a | primitive apple proxy สำหรับ M1 |
| CHR_AppleChild (source) | `ArtSource/Blender/Characters/CHR_AppleChild_Source_v3.blend` | SOURCE | — | ของผู้ใช้ v3 (ห้ามแก้ทับ) |
| CHR_AppleChild (game v1) | `Assets/DreamRush/Art/Characters/AppleChild/CHR_AppleChild.fbx` (+`ArtSource/.../CHR_AppleChild_Game_v1.blend`, script `build_apple_game.py`) | INTEGRATED | REVISE minor: สีผิวเข้มกว่า palette, ไม่มีชุดนอน (รอผู้ใช้) | 1.198 m, 14,945 tris, front +Z ยืนยันใน Unity |
| CHR_AppleChild rig | flat rigid parts Body/Leaf/Arm_L/Arm_R/Leg_L/Leg_R | INTEGRATED | — | pivots: shoulders/hips/stem base |
| Anim: Idle, Walk, Run, Jump(rise/apex), Fall, Land | procedural (`AppleProceduralAnimator` + `PlayerSquashStretch`) | INTEGRATED | APPROVED (ทดสอบภาพ) | idle = มือประสานตามต้นฉบับ |
| Anim: Interact, Sit, Lie Down, Sleep, Wake Up, Victory | procedural poses / Timeline | TODO | — | |
| Face: Blink, Surprise, Sleepy, Hurt, Victory | texture/mesh swap | TODO | — | |

## P0 — Bedroom
| ID | สถานะ | หน้าที่ |
|---|---|---|
| ENV_Bedroom_Floor / Wall_A..D / Ceiling | TODO | ขอบเขตห้อง 12×10×4.5 |
| PROP_Bed (+Pillow, Blanket, upgrade variants L1–L3) | TODO | Play / Bed upgrade |
| PROP_Nightstand, PROP_NightLamp | TODO | Settings |
| PROP_Bookshelf | TODO | Dream Diary |
| PROP_Window (+Curtains, moonlight, apple-town view) | TODO | storytelling + key light |
| PROP_Rug, PROP_Desk, PROP_Door | TODO | composition / future menus |
| PROP_TinyChair | **Done** — same model ×0.42, face hidden (foreshadowing) | foreshadow boss (wobble) |
| PROP_ToyShip, PROP_MoonBook | TODO | foreshadow future bosses |
| PROP_AppleClock (seed shape) | TODO | future stats |

## P0 — Chair Stage
| ID | สถานะ | หน้าที่ |
|---|---|---|
| ENV_Dream_ArrivalCarpet, Tutorial ottoman/cushions | TODO | §13.3–13.4 |
| ENEMY_LittleStool | TODO | สอนพฤติกรรมหนี §19.3 |
| BOSS_Chair (+rig, anim set §12.5) | **APPROVED (P0)** — `build_chair_boss.py` → `BOSS_Chair.fbx` 10.5k tris, flat parts (Body, 4 legs w/ hip pivots, eyes, pupils, brows, face); procedural anim set in `ChairAnimator` (idle tap, alert, trot/panic gait, hesitate, stumble, exhausted, rooted, frozen, offer seat, pulled, sat-on struggle, defeated, blink, pupil look-at). Review: `ArtSource/Renders/BOSS_Chair/*.png`, `docs/evidence/M7/m7_st_*.png`. Notes: no ink outline yet; grain slightly busy at 15 m | boss |
| ENV_Arena (32×25) floor/bounds/low platform/balcony/cushion route | TODO | §14 |
| PROP_MovableBlock, PROP_Cushion ×2, PROP_Switch | TODO | obstacle/shortcut methods |
| Card props: glue target, flash camera stand, fishing hook point | TODO | Card experiment §13.6 |

## P1 — 2D / UI / Audio
| ID | สถานะ |
|---|---|
| DEC_PaperCloud set, DEC_PaperStar, posters, illustrated plants, stickers | TODO |
| FX_ChairPanic, FX_SpeedLines, FX_JumpDust, FX_LandPuff, FX_Surprise, FX_Glue, FX_Freeze, FX_Confetti | TODO |
| UI: prompt, card frame + 6 icons, Sleep Stability, objective, boss bar, upgrade panel, run summary | **Graybox done** (code-built uGUI; `CARD_*.png` ×6 + `LAW_*.png` ×3 จาก `make_card_icons.py`) · upgrade panel TODO (M7) |
| Audio: bedroom amb/music, dream music, boss music (5 states), footsteps, jump, land, chair wood steps, card SFX, victory, wake-up | TODO (ทุกไฟล์ต้องระบุแหล่ง+license) |

## Source Library (ไม่ใช่ asset เกม)
| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `ArtSource/Concepts/References/REF_01..10.png` | ภาพอ้างอิงจากผู้ใช้ (ดู REFERENCE_NOTES.md) — ห้ามนำเข้าเกม |

### Art Review: BOSS_Chair (2026-10-17)
Purpose: character/gameplay — the first boss; must read as "a chair" from the arena camera and show its mood up close.
Evidence: `ArtSource/Renders/BOSS_Chair/{front,threequarter,side,back}.png`, `docs/evidence/M7/m7_st_*.png`, `m7_faces.png`
Silhouette: pass — four legs + seat + ladder back read as a chair at 15 m (§12.3); face is the only fantasy feature.
Checklist: palette warm wood vs cream floor/lavender walls ✓ (stands out); soft bevels ✓; uneven legs/arched top ✓; no IP traits ✓.
Revisions done during review: wood too red/zebra → softer honey grain; eyes overflowed the board at 1.5× → eye scale capped, fear shown by pinpoint pupils; rest brows read angry → slight worry at rest; top rail +8 cm.
Verdict: APPROVED for P0. Later: optional ink outline pass, mouth shapes per state.

### Art Review: Palace detailed stylized pass (2026-10-20)
Purpose: supported wayfinding/fixtures and coherent inhabited interiors. Evidence: docs/PALACE_STYLE_REVIEW.md and current-m20-polish rendered captures. Verdict: REVISE — material/detail foundation implemented; furniture silhouettes, empty-room/plaza composition, facade repaint, palette and motion checks remain. No final art approval claimed.

