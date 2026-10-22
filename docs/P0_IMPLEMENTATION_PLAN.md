# DREAM RUSH — P0 Implementation Plan
## Apple Bedroom & Chair Boss Vertical Slice

> Source of Truth: `docs/GDD.md` (Master GDD v2.0) — ถ้าเอกสารนี้ขัดกับ GDD ให้ GDD ชนะ และบันทึกเหตุผลใน "Decision Log" ท้ายไฟล์
> ลำดับความสำคัญทุกการตัดสินใจ: **PLAYABILITY → FUN → VISUAL QUALITY**

---

## 0. Environment Audit (ตรวจจริง 2026-10-16)

| รายการ | สถานะจริง | ผลต่อแผน |
|---|---|---|
| Unity project `C:\Pepes\final dojo` | Unity **6000.3.11f1**, URP 17.3.0, template เปล่า (SampleScene) | ใช้โปรเจกต์นี้เป็น UnityProject ตรง ๆ (ไม่สร้างซ้อน) |
| Input System | 1.19.0, `activeInputHandler: 1` (new only) | ใช้ Input System อย่างเดียว |
| AI Navigation | 2.0.11 | ใช้ NavMeshSurface + NavMeshAgent ให้เก้าอี้ |
| Test Framework | 1.6.0 | EditMode + PlayMode tests |
| Cinemachine | **ไม่มี** | P0 เขียน camera เอง (Hub orbit + Stage curated) — คุม feel ได้ละเอียด ไม่เพิ่ม dependency; ทบทวนอีกครั้งตอน M4 |
| TextMeshPro | อยู่ใน uGUI 2.0 แต่ยังไม่ import TMP Essentials | Graybox UI ใช้ uGUI Text + builtin font ก่อน; Art pass ค่อยเลือก font ที่รองรับไทย |
| Unity Editor | เปิดอยู่บนเครื่องผู้ใช้ — ควบคุมผ่าน computer-use ได้ | ทดสอบจริงได้ |
| Blender บนเครื่องผู้ใช้ | **ไม่ได้ติดตั้ง** | ใช้ Blender 5.2.2 (bpy) บน cloud workspace สร้าง/แก้/export asset แล้วส่งไฟล์เข้าโปรเจกต์ |
| Asset ตัวละคร | `Cheeky_Apple_Chibi_v3.blend/.glb` (multi-part, ไม่มี rig, สูง ~3.5 m, ท่ามือประสาน) | ต้อง re-scale เป็น 1.2u, แยกมือซ้าย/ขวาเพื่อวิ่งได้, origin ที่เท้า |
| Git | repo มี 1 commit (Initial check-in) | ยังไม่ commit แทนผู้ใช้ (การลบ lock file ถูกบล็อกจากฝั่งเครื่องมือ) |

**DevBridge**: เพื่อให้ "ทดสอบจริง" ได้ซ้ำ ๆ โดยไม่ต้องคลิกทุกครั้ง จะมี editor tool `Assets/DreamRush/Editor/DevBridge` (asmdef แยก ไม่พึ่ง game code) ที่อ่านคำสั่งจาก `DevBridge/cmd/*.txt` (refresh / รัน menu / รันเทสต์ / play / stop / screenshot) และเขียนผล compile, console log, test result ลง `DevBridge/out/` — เป็นเครื่องมือ dev เท่านั้น ไม่ติดไปกับ build

---

## 1. Milestones & Dependencies

```text
M0 Foundation ──► M1 Apple Movement ──► M2 Bedroom Hub ──► M3 Sleep→Dream Transition
                        │                     │                      │
                        │                     └──► (Lamp Settings)   ▼
                        └──────────────────────────────────► M4 Chair Stage Graybox
                                                                     │
                                                                     ▼
                                                             M5 Chair AI + Baseline Victory
                                                                     │
                                                                     ▼
                                                             M6 3-Card System + Alt Solutions
                                                                     │
                                                                     ▼
                                                     M7 Art / Anim / VFX / SFX / UI pass
                                                                     │
                                                                     ▼
                                                     M8 Playtest, Bugfix, Polish (loop)
```

กฎ: ห้ามเริ่ม milestone ถัดไปถ้า acceptance ของตัวก่อนหน้า (ส่วนที่เป็น gameplay) ยังไม่ผ่านการทดสอบจริง ยกเว้นงาน art source ที่ทำขนานได้ (Blender re-topology/rig) แต่ห้ามเอาเข้า scene ก่อน graybox ผ่าน

### M0 — Foundation
- โฟลเดอร์ `Assets/DreamRush/*`, asmdef (Runtime / Editor / Tests.EditMode / Tests.PlayMode / DevBridge)
- docs + `.claude/skills/*` (8 skills ตาม GDD §27) + `CLAUDE.md`
- **Acceptance**: โปรเจกต์ compile 0 error, Test Runner เห็น test assemblies

### M1 — Apple Character Movement Prototype (ความสำคัญ #1)
- `PlayerMotor3D` (CharacterController), `JumpAssist` (coyote/buffer, pure C#), `MovementMath`, `PlayerMotorConfig` (SO, ค่าตาม GDD §9.3)
- `PlayerInputReader` (Input System, keyboard+gamepad, สร้าง actions ในโค้ด → remap ได้)
- `ThirdPersonCameraController` (orbit, collision, sensitivity)
- `BlobShadow` (เงาตามพื้นผิว ไม่ตามความสูงตัว — GDD §9.7), `PlayerSquashStretch` (feel)
- Graybox "Movement Gym": บันได 0.5–2.5u, gap 3–7u, slope 20/35/50°, อุโมงค์เพดานต่ำ, กำแพงทดสอบกล้อง, ขอบสำหรับ coyote
- Placeholder apple (primitive) → แทนด้วย model จริงเมื่อผ่าน asset gate
- **Acceptance**: EditMode + PlayMode tests ผ่าน; เล่นจริง: เดินทุกทิศสัมพันธ์กล้อง, jump สูงเต็ม ≈2.1u / แตะสั้นต่ำกว่าชัดเจน, coyote+buffer ทำงาน, ไม่ติดขอบ/ไม่ทะลุ, กล้องไม่ทะลุกำแพง; "วิ่งในห้องเปล่าแล้วสนุก" (ประเมินโดยผู้ใช้)

### M2 — Bedroom Hub (Graybox ก่อน)
- ห้อง 12×10×4.5u ตาม layout GDD §7.6, collision ชัด, ไม่มีของเกะกะทางเดิน
- `InteractionSystem` (IInteractable, prompt ใกล้สุด/หันหน้า), Bed [E] GO TO SLEEP, Lamp → Settings, Bookshelf → Dream Diary (stub ข้อมูลจริงจาก save), Tiny Chair wobble (foreshadow)
- `MenuFocusCamera` (กล้องเลื่อนไปหาวัตถุ → panel → คืนกล้อง), Pause menu fallback (Esc) เข้าถึง Settings ได้เสมอ
- Settings: camera sensitivity, invert Y, master volume, reduce shake/flash, text size (เก็บใน save)
- **Acceptance**: เดินสำรวจได้อิสระ, เข้า dream ได้โดยไม่มีปุ่ม Play แบบ main menu, เปิด/ปิดเมนูแล้วคืน control ถูกต้อง

### M3 — Sleep & Dream Transition
- `GameFlowManager` (state: Hub → Sleeping → CardDraw → Dream → Victory/Wake → Hub), scene load แบบ additive-safe
- Sleep sequence: เดินเข้าเตียง → นอน → เสียงเบาลง → กล้องเข้าหมอน → paper-peel transition (2D overlay + 3D motion; ห้าม portal วงกลม)
- Wake (แพ้): desaturate → fold → เสียงแม่เรียก → ตื่นในห้อง → Run Summary
- **Acceptance**: วงจร Hub→Dream→Hub ซ้ำได้ ≥5 รอบโดยไม่มี state รั่ว (test อัตโนมัติ)

### M4 — Chair Stage Graybox (3D Platformer + 2D illustration hooks)
- Section A–E ตาม GDD §13 (Arrival, Tutorial 3 challenges, Moving Furniture/Little Stool, Card Experiment, Boss Arena 32×25)
- `StageCameraController` (curated 3/4 view, look-ahead, boss framing player+chair)
- จุดวาง 2D cutout (Type A/B/C) เป็น placeholder ที่มีกติกาชัด
- **Acceptance**: ด่าน navigable และสนุกก่อนมีอาร์ต; ระยะกระโดดทุกจุดอยู่ใน envelope ที่วัดจาก M1

### M5 — Chair AI + Baseline Victory
- State machine 13 states (GDD §15.2), escape destination scoring (§15.5) บน NavMesh, fatigue/exhaust, stumble, anti-softlock (§15.8), seat validation 2s (§15.9)
- Chair reveal: เก้าอี้ "เตะนิ้วก้อย" แล้ววิ่งหนี (จาก pitch ของผู้ใช้) → title THE CHAIR / SIT ON THE CHAIR
- Sleep Stability + ชนสิ่งกีดขวาง = เสีย stability และเก้าอี้ได้ระยะเพิ่ม (pitch)
- **Acceptance**: ชนะด้วย Chase & Exhaust ได้โดยไม่ใช้การ์ด; bot test 50 seeds ไม่มี softlock, เก้าอี้ไม่ออกนอก arena, victory trigger ครั้งเดียว

### M6 — 3-Card System + Alternative Solutions
- `CardDefinition` (SO ตาม §17.6), `CardDrawManager` (3 ใบไม่ซ้ำจาก starter pool, seeded), `CardInventory`, `CardEffectResolver` → `StatusEffectSystem` ผ่าน `DreamProperty` (ห้าม if card==X && boss==Chair → win)
- Starter 6: Super Glue(Root), Camera Flash(Freeze), Fishing Rod(Pull), Be a Grandpa(ChangeBehavior→OfferSeat), Decoy Apple, Bouncy Cushion
- **Dream Law / การ์ดข้อห้าม** (จาก pitch): เช่น "ห้ามกระโดด", "ห้ามวิ่งเร็ว" สุ่มขึ้นระหว่างไล่ล่า — เป็น rule modifier ผ่านระบบเดียวกัน
- Shortcut & Obstacle victories, Dream Diary บันทึกวิธีชนะที่ค้นพบ
- **Acceptance**: ≥3 วิธีชนะแบบใช้การ์ดทำงานถูกต้อง + test; invalid target ไม่ trigger; cooldown ถูก

### M7 — Art / Animation / VFX / SFX / UI Pass
- แทน graybox ด้วย asset ที่ผ่าน `art-direction-guardian` เท่านั้น; ตัวละครแอปเปิ้ลจาก Blender (rigid-part rig + procedural limb animation), เก้าอี้ boss, ห้องนอน props ตาม manifest
- Material presets (Apple Skin/Wood/Fabric/Paper/Metal), selective outline, lighting ตาม §5.5
- VFX 2D: jump dust, landing puff, speed lines, surprise mark, glue/freeze, confetti
- SFX/music ตาม §24 (ระบุแหล่ง/license ทุกไฟล์)
- Bed upgrade 3 อย่าง + Run Summary + Dream Memory currency (versioned JSON save)

### M8 — Playtest, Bug Fixing, Polish
- ตาม `docs/TEST_PLAN.md` §Playtest; ใช้ `playtest-qa` skill จัด priority bug

---

## 2. Order of Work ภายใน Session นี้

1. M0 → M1 (code + tests + graybox gym) → ทดสอบใน Unity จริง → รายงาน
2. ตรวจ asset แอปเปิ้ลใน Blender (art review) ขนานกัน — ยังไม่ใส่ใน gameplay จนกว่า movement จะผ่าน
3. M2 → M3 → M4 → M5 → M6 ตามลำดับ แต่ละอันต้องมี test/หลักฐาน

---

## 3. Definition of Done (ทุก milestone)

- [ ] compile 0 error / 0 warning ใหม่จากโค้ดเรา
- [ ] EditMode/PlayMode tests ที่เกี่ยวข้องผ่าน (บันทึกผลจริงใน `docs/MILESTONE_REPORTS.md`)
- [ ] เล่นทดสอบใน Play Mode จริง + screenshot หลักฐาน
- [ ] ผ่าน skill review ที่เกี่ยวข้อง (design / controller / boss / art / architecture / feel / qa)
- [ ] รายงาน: ไฟล์ที่สร้าง/แก้, การทดสอบ, ผลจริง, bug ที่รู้, ความเสี่ยงด้านดีไซน์, ข้อสังเกต art, milestone ถัดไป

---

## 4. Risks

| Risk | ผลกระทบ | การรับมือ |
|---|---|---|
| ไม่มี Blender บนเครื่องผู้ใช้ | แก้ asset ต้องผ่าน cloud | เก็บ `.blend` source ใน `ArtSource/`, export FBX ผ่าน script ที่ reproducible |
| โมเดลแอปเปิ้ลท่ามือประสาน + ไม่มีชุดนอน (GDD ระบุชุดนอน) | วิ่งแกว่งแขนไม่ได้ / ไม่ตรง GDD §4.2 | แยกมือซ้าย/ขวา, ท่า idle = มือประสาน; ชุดนอนเป็นคำถามให้ผู้ใช้ตัดสิน |
| Chair AI หนีเก่งเกิน/โง่เกิน | ไม่สนุก | ค่า tuning ใน SO + bot test หลาย seed + playtest |
| กล้อง 3D ทำให้กะระยะกระโดดยาก | หงุดหงิด | blob shadow, landing area ชัด, stage camera มุมคงที่ |
| การ์ดสุ่มไม่มีประโยชน์ | ทำลาย trust | starter pool คัดแล้ว ทุกใบใช้ได้ทั้ง tutorial และ boss |

---

## 5. Decision Log

| วันที่ | การตัดสินใจ | เหตุผล |
|---|---|---|
| 2026-10-16 | ใช้โปรเจกต์ `final dojo` เป็น UnityProject; docs/skills/ArtSource อยู่ที่ root ของโปรเจกต์ | โปรเจกต์มีอยู่แล้ว เปิดใน Editor อยู่ |
| 2026-10-16 | CharacterController แทน Rigidbody | คุม feel แบบ platformer ได้แน่นอน, ไม่มี physics jitter, GDD §9.9 อนุญาต |
| 2026-10-16 | Camera เขียนเองแทน Cinemachine ใน P0 | ไม่มี package, ต้องการ collision/framing เฉพาะ, ลด dependency |
| 2026-10-16 | ตัวละครใช้ rigid multi-part + procedural animation ก่อน skinned rig | โมเดลเป็นชิ้นแยกอยู่แล้ว, ตอบสนองทันที (GDD §20.2 ห้ามหน่วง jump เพื่อรอ anim), เพิ่ม rig เต็มทีหลังได้ |
| 2026-10-16 | รวม "การ์ดข้อห้าม" + "ชนแล้วเสียระยะ" จาก pitch เข้าเป็น Dream Law ใน M6 / pushback ใน M5 | ผู้ใช้ระบุใน brief; GDD มี "Dream Law slot" และ card ที่ "modify the rules of the dream" รองรับ |
| 2026-10-16 | Chair `exhaustionThreshold` 18 → 36, fatigue panic rate 1.5 (เบี่ยงจาก GDD §15) | ค่าตาม GDD: bot ไล่ตรง ๆ ชนะใน ~9 s ไม่มีการไล่ล่า; 36 ได้ ~27 s สำหรับ bot — ปรับต่อจาก playtest จริง |
| 2026-10-16 | ต้อนมุมเฉย ๆ ไม่ทำให้เก้าอี้สะดุด ต้องมีกล่องที่ผู้เล่นดันมาขวาง (วิธี "Obstacle") | กันวิธีชนะราคาถูกที่ไม่ต้องคิด; ให้รางวัลกับการใช้ฉาก (GDD §16.7) |
| 2026-10-17 | Fishing Rod ดึงตาม NavMesh path (ไม่ใช่เส้นตรง) และดึงจนถึงตัว (cap 4 s) | เส้นตรงติดเฟอร์นิเจอร์แล้วการ์ดดูเหมือนพัง (พบจาก test) |
| 2026-10-17 | เสียง P0 = สังเคราะห์เองทั้งหมดด้วยสคริปต์ (CC0) | ไม่มี asset เสียงที่ license ชัดเจนในโปรเจกต์; ได้เสียงครบทุก event ทันทีและปลอดภัยเรื่องลิขสิทธิ์ — แทนด้วยงาน sound designer ได้ภายหลังโดยไม่แก้โค้ด (โหลดตามชื่อไฟล์) |
| 2026-10-17 | หลับตาแอปเปิล = สลับ material ช่องตา (ไม่ทำ blendshape) | โมเดลของผู้ใช้ไม่มี shape key; ไม่แก้ไฟล์ต้นฉบับ |
| 2026-10-17 | Bed upgrade ราคา Mattress 20/35/55, Pillow 30, Blanket 25 | ชนะ 1 ครั้ง ≈ 10–15 memory → ซื้อชิ้นแรกได้หลังชนะ ~2 ครั้ง; ต้องปรับจาก playtest |
| 2026-10-17 | "ชนสิ่งกีดขวางแล้วเสียระยะ" = รถของเล่นวิ่งเลนในอารีนา | ทำให้ pitch เป็นรูปธรรม, telegraph ด้วยรางบนพื้น, กระโดดข้ามได้ (platformer skill) |

### Decision update — 2026-10-20

| Date | Decision | Reason |
|---|---|---|
| 2026-10-20 | User broadened scope beyond P0 to existing World 1 and Palace; selected detailed stylized art inside/out; World 2 requires persisted World 1 clear | Preserve Claude M17–M20 and legacy prototypes; menu placeholders are not implemented levels; audit/art status recorded separately in CURRENT_CAMPAIGN_STATUS.md and AUDIT_AFTER_M20.md |


### Decision update — 2026-10-23

| Date | Decision | Reason |
|---|---|---|
| 2026-10-23 | Remove the complete card system and Dream Laws, including reroll progression, UI, input, assets, builders and card-only tests. Enter the selected world directly. Keep movement, combat and environmental mechanisms. | Explicit user request: “เอาระบบ การ์ด ออกไป ให้หมดเลย”. This supersedes historic card requirements. Legacy pillow save data is inert; no real save was edited. |
