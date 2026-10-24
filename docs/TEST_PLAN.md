# Test Plan — P0
ใช้คู่กับ skill `playtest-qa` · เกณฑ์มาจาก GDD §30–31

## 1. ระดับการทดสอบ
| ระดับ | เครื่องมือ | ใช้กับ |
|---|---|---|
| Unit (EditMode) | Unity Test Framework, `Assets/DreamRush/Tests/EditMode` | logic บริสุทธิ์: JumpAssist, MovementMath, JumpMath, escape scoring, fatigue, save versioning |
| Simulation (PlayMode) | `Assets/DreamRush/Tests/PlayMode` — step motor ด้วย dt คงที่ + scripted input | jump height จริง, coyote/buffer กับ collider จริง, ติดผนัง/ขอบ, chair AI ไม่ออก arena (หลาย seed), victory ครั้งเดียว |
| Manual Play | Play Mode ใน Editor (ผ่าน computer-use + ผู้ใช้) | feel, กล้อง, ความอ่านง่าย |
| Playtest | ผู้ทดสอบ 8–12 คนที่ไม่รู้ทางแก้ | §31 |

ผลทุกครั้งบันทึกใน `docs/MILESTONE_REPORTS.md` พร้อมจำนวน pass/fail จริง — **ห้ามรายงานว่าผ่านถ้ายังไม่ได้รัน**

## 2. M1 Movement — Test Cases
| ID | Test | ประเภท | เกณฑ์ผ่าน |
|---|---|---|---|
| MOV-01 | JumpMath: v0 = √(2gh) | EditMode | error < 1e-4 |
| MOV-02 | Coyote: กระโดดได้ภายใน 0.12s หลังหลุดพื้น, ไม่ได้หลัง 0.12s | EditMode | ตรง |
| MOV-03 | Buffer: กดก่อนแตะพื้น ≤0.14s → กระโดดตอนแตะพื้น; >0.14s → ไม่ | EditMode | ตรง |
| MOV-04 | ไม่มี double jump (กดซ้ำกลางอากาศ) | EditMode + PlayMode | ไม่กระโดดซ้ำ |
| MOV-05 | Camera-relative: input W = ทิศกล้องฉายบนพื้น ทุก yaw | EditMode | angle error < 0.5° |
| MOV-06 | Full jump peak ≈ jump_height (2.1) | PlayMode | ±0.1u |
| MOV-07 | Short tap jump ต่ำกว่า full อย่างน้อย 30% | PlayMode | ตรง |
| MOV-08 | ถึง walk speed ภายใน ~0.25s, หยุดภายใน ~0.2s | PlayMode | ตาม config |
| MOV-09 | เดินชนกำแพงแล้วไม่สะสมความเร็ว (ปล่อยแล้วไม่พุ่ง) | PlayMode | speed หลังชน ≈ 0 ในแนวตั้งฉากกำแพง |
| MOV-10 | เดินลงทางลาด 35° ไม่ลอยกระดอน (ground snap) | PlayMode | grounded ต่อเนื่อง |
| MOV-11 | ชนเพดานตอนกระโดด → ตกทันที ไม่ค้าง | PlayMode | vy ≤ 0 ภายใน 1 เฟรม |
| MOV-12 | Blob shadow อยู่บนพื้นใต้ผู้เล่นขณะลอย | PlayMode | |shadow.y - ground.y| < 0.05 |
| MOV-13 | กล้องไม่ทะลุกำแพง (camera collision) | PlayMode | ไม่มี collider ระหว่าง pivot กับกล้อง |
| MOV-M1 | Manual: เดิน/วิ่ง/กระโดด Movement Gym ครบทุกสถานี | Manual | ไม่มีติด/ทะลุ; ผู้ใช้ให้ feel ≥4/5 |

## 3. M2–M6 (ร่าง — ขยายเมื่อถึง milestone)
- HUB-01..: interaction prompt ใกล้สุด, เปิด/ปิดเมนูคืน control, Pause→Settings, กลับจาก dream ที่ spawn ถูก
- FLOW-01: Hub→Dream→Hub ×5 ไม่มี state รั่ว (boss/status reset)
- CHAIR-01..: notice/flee/อยู่ใน NavMesh เสมอ/exhaust reachable/anti-softlock/seat 2s ต่อเนื่อง/victory once — รัน bot 50 seeds ✅ (`BossChairPlayModeTests`, `BossRevealPlayModeTests`)
- ~~CARD-01 / CARD-M1 / CARD-M2~~: นำออก 2026-10-23 พร้อมระบบการ์ด (`CardLogicTests`, `CardSystemPlayModeTests` ถูกลบแล้ว) ดู `docs/CARD_REMOVAL_REPORT.md`
- CARDFREE-01: Hub→sleep→Galaxy→World 1→wake ไม่มีขั้นเลือกการ์ด, โหลดทั้งห้าโลก, ไม่มี missing script, save เก่าที่มี `pillowLevel` ซื้อ upgrade ที่ถูกลบไม่ได้ (`CardFreeCampaignPlayTests`)
- WIN-M2 (manual, M8): ผู้เล่นจริงค้นพบวิธีชนะเก้าอี้อย่างน้อย 2 วิธีจากสภาพแวดล้อม/การเคลื่อนไหวโดยไม่บอก — ยังไม่ได้ทำ
- SAVE-01..: versioned JSON, upgrade หักเงินครั้งเดียว, โหลดหลังรีสตาร์ท ✅ (`BedUpgradeTests`, `ProgressionPlayModeTests`)
- AUD-01: ทุก Sfx/Music enum มีไฟล์จริง ✅ (`AudioAssetTests`) · AUD-M1 (manual): ผู้ใช้ฟังเสียงจริง — **ยังไม่ได้ทำ**
- HAZ-01: รถของเล่นชน = -1 moon + กระเด็น, กระโดดข้ามปลอดภัย, ไม่ชนซ้ำ ✅

## 4. Bug Report Format
`[P0-P3] <ระบบ> — สรุป` · Repro steps · Expected · Actual · Seed/scene · Evidence (log/screenshot) · Status (Open/Fixed/Retested)
