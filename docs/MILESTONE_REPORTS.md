# Milestone Reports — P0

## 2026-10-23 — MAMA'S COMING! readability pass (current game)

- Replaced the competing always-on checklist/markers with a compact goal, contextual item descriptions, optional held-H ideas and a Tab overview. Teddy names its bedroom/bed/pillow and gains a matching home silhouette nearby; off-screen optional destination labels stay visible. Oil fire offers environmental clues and alternative tools. New text switches between Thai/English. Reduced bloom/saturation in a cloned runtime profile and improved UI contrast.
- Fixed two reproduced interaction problems: returning a teddy could bounce it away from home; carrying a lid selected the nearby stove instead of the burning pot. Added immediate return confirmation and positive fire-resolution feedback.
- Files: MamasComing GameManager, PlayerController, MessyProp, MamaUI, new MamaUI.Readability/MamaReadability/MamaPresentation and Editor/MamaUxReview. Did not rebuild the house. Preserved the original dirty scene before changes; its final SHA256 matches that saved backup.
- Validation: Runtime/Editor compile with 0 errors; **31 focused staged-input checks passed** (`DevBridge/mama-ux/review-121606.txt`). Actual Game View captures reviewed. Earlier failures retained; exact external connector authorization rejection excluded, gameplay errors remain failures. Input/language preferences restored. Two pre-existing compile warnings remain.
- Limits/next: no uncoached human playthrough, controller session, lower-resolution visual audit or standalone build. Check whether a fresh player understands the goal, teddy home and fire alternatives without explanation. Full design/evidence: `docs/MAMA_UX_REVIEW.md`.

รูปแบบตาม GDD §35: Completed · Files · Tests run · Actual results · Known bugs · Design risks · Art findings · Next
หลักฐานอยู่ `docs/evidence/<Milestone>/`

---

## M0 — Foundation (2026-10-16) ✅

**Completed**
- Docs: P0 plan (dependencies/DoD/decision log), art checklist, asset manifest, test plan, research log (เฉพาะแหล่งที่อ่านจริง), technical architecture, reference notes, `CLAUDE.md`, GDD copy
- 8 project skills ใน `.claude/skills/` (GDD §27)
- Assemblies: Runtime / Editor / Tests.EditMode / Tests.PlayMode / DevBridge
- DevBridge (editor tool): ควบคุม Editor ผ่านไฟล์ — refresh, menu, exec, tests, play/stop, screenshot (render จากกล้องตรง ไม่พึ่ง Game view)

**Tests run** — compile ผ่าน 0 error (ดู `DevBridge/out/compile.log`); Test Runner เห็นทั้ง 2 test assemblies

**Known issues** — โฟลเดอร์ทดสอบ `.claude/skills/_probe/` ค้างอยู่ (ลบไม่ได้จากเครื่องมือ ลบเองได้)

---

## M1 — Apple Character Movement Prototype (2026-10-16) ✅ (รอผู้ใช้ประเมิน feel)

**Completed**
- `PlayerMotor3D` (CharacterController): camera-relative, accel/decel/turnaround, air control, variable jump (jump-cut gravity), coyote 0.12s, buffer 0.14s, fall multiplier, ground snap, ceiling bonk, ตัดความเร็วที่ดันกำแพง, integration แบบ average-velocity → ความสูงกระโดดไม่ขึ้นกับ FPS
- `JumpAssist`, `MovementMath`, `JumpMath` (pure C#), `PlayerMotorConfig` (SO ค่าตาม GDD §9.3)
- `PlayerInputReader` (Input System, KB+M + gamepad, actions สร้างในโค้ด → remap ได้)
- `ThirdPersonCameraController` (orbit, sphere-cast collision, กัน mouse spike ตอน lock cursor)
- `BlobShadow` (ตามพื้นผิวใต้ตัว), `PlayerSquashStretch` (jump stretch / land squash / lean / bank)
- **ตัวละครจริงจากโมเดลของผู้ใช้**: Blender pipeline (`ArtSource/Blender/Scripts/build_apple_game.py`) → 1.2 m, 14,945 tris, แยก Body/Leaf/Arm_L/Arm_R/Leg_L/Leg_R, bake ผิวแอปเปิ้ล, แก้ normals กลับด้าน → `AppleProceduralAnimator` (idle มือประสานแบบต้นฉบับ, เดิน/วิ่งแกว่งแขนขา, ลอยแล้วเก็บขา, ใบไม้ follow-through)
- Movement Gym graybox (`DreamRush/Build/Movement Gym`): บันได 0.5–2.5, gap 3–7 m, slope 20/35/50°, อุโมงค์เพดาน 1.8, camera corridor, ledge/coyote, slalom
- Dev: HUD (F3), กล้องมองหน้า F4 / หลัง F5, `DevCommands`

**Tests run (ผลจริงจาก Unity 6000.3.11f1)**
| Suite | ผล |
|---|---|
| EditMode | **15/15 passed** |
| PlayMode | **13/13 passed** |
- MOV-06 full jump apex = **2.100** (target 2.1) · MOV-07 tap = **1.096** (52% ของ full) · MOV-03 buffered jump = **2.100** (ไม่มี super jump) · MOV-08 ถึง walk speed **0.200 s**, หยุด **0.167 s** · MOV-10 ลงทางลาด 35°: ลอย **0/120** เฟรม · MOV-14 ยืนนิ่ง 30–1000 fps ไม่หลุด ground
- Manual (computer-use): WASD/Shift/Space ผ่าน Input System จริงทำงาน, ชนกำแพงขอบเขตไม่ทะลุ, walk cycle ถ่ายเป็นชุดภาพ `docs/evidence/M1/apple_walk_*.png`

**Jump envelope (คำนวณจาก config, ใช้ออกแบบด่าน)** — สูงสุด 2.1 m · gap บนพื้นเรียบ ≈ 4.3 m (walk) / ≈ 6.2 m (sprint) — ยืนยันด้วยมือใน gym ยังไม่ครบทุกสถานี

**Bugs found & fixed ระหว่างทาง**
1. [P1] กล้องหมุนพรวดตอน lock cursor (mouse delta spike) → ข้าม 3 เฟรม + clamp delta
2. [P1] FBX จาก Blender: ชิ้นลูก (arms/leaf) เพี้ยนแกน → ใช้ hierarchy แบนราบ + virtual parenting ใน animator (บันทึกใน blender-asset-pipeline skill)
3. [P1] โมเดลต้นฉบับ normals ของตัว/ตา/รูม่านตากลับด้าน → ตาหายใน Unity → `normals_make_consistent`
4. [P2] โมเดลหันผิดทาง (-Z) เมื่อไม่ใช้ Apply Transform → ยืนยันแล้วว่า front = +Z

**Known issues / not tested**
- ยังไม่ได้ทดสอบด้วย gamepad จริง (ไม่มีจอยต่อ) · ยังไม่ได้วัด gap สูงสุดด้วยมือครบทุกช่อง
- Label TextMesh ใน gym อ่านกลับด้านเมื่อมองจากด้านหลัง (graybox เท่านั้น)
- ยังไม่มีเสียง/VFX (ตามลำดับ M7)

**Design risks** — "สนุกไหม" ต้องให้ผู้ใช้ลองเล่นจริง (F3 ดูค่า); ค่า coyote/buffer/jump-cut อาจต้องจูน

**Art findings (art-direction-guardian)**
- CHR_AppleChild v1 game: silhouette ชัดทุกมุม, หน้า "cheeky" อ่านออกที่ระยะ 3 m, ไม่มีโลโก้/รอยกัด → **REVISE (minor)**: สีผิวเป็น crimson เข้มกว่า Apple Red ของ palette (#E85D65); ไม่มีชุดนอนตาม GDD §4.2 — รอผู้ใช้ตัดสิน
- Graybox gym ใช้สีตาม palette functional (walkable cream / hazard apricot / interactable mint)

**Next** — M2 Bedroom Hub graybox + Interaction System + Lamp Settings + Pause fallback

---

## M2 — Bedroom Hub (graybox) + M3 — Sleep → Dream → Wake loop (2026-10-16) ✅

**Completed**
- `BedroomHub` scene (builder `DreamRush/Build/Bedroom Hub`): ห้อง 12×10×4.5 ตาม layout GDD §7.6 — เตียงหัวเตียงรูปเมล็ด (PLAY), โคมข้างเตียง (SETTINGS), ชั้นหนังสือ (DREAM DIARY) + เรือของเล่น/หนังสือดวงจันทร์ (foreshadow), โต๊ะ + **เก้าอี้จิ๋วที่สั่นเมื่อจิ้ม** (foreshadow boss), ตู้เสื้อผ้า (decor), หน้าต่างกลมเห็นเมืองแอปเปิ้ลกลางคืน, โปสเตอร์/รูปครอบครัว/ประตู (เนื้อเรื่องสั้น ๆ), พรม, ของเล่น
- แสงตาม GDD §5.5: แสงจันทร์เย็นลอดหน้าต่างเป็นแพทช์บนพื้น + โคมอุ่น + โคมโต๊ะ + ambient
- `InteractionSystem` (registry + scoring ใกล้/หันหน้า, ไม่ต้องใช้ trigger collider), `Interactable` base, Bed/Menu/Message/Wobble interactables
- UI ทำจากโค้ดสไตล์กระดาษ storybook: prompt `[E] GO TO SLEEP`, toast, panel stack, Settings (sensitivity/invert/volume/text size/reduce shake/flash/subtitles → บันทึก), Dream Diary (วิธีชนะ 7 แบบ ซ่อนเป็น ??? + hint), Pause (Esc) เข้าถึง Settings/Diary ได้เสมอ + WAKE UP ในฝัน
- `PlayerUIBridge`: เปิดเมนู = ล็อก movement + กล้อง, ปิด = คืน control; กล้องเลื่อนไปหาวัตถุ (focus pose) แล้วกลับ
- `SaveManager`: versioned JSON, atomic write, migrate v0, backup ไฟล์เสีย
- `GameFlowManager`: Hub → FallingAsleep → (CardDraw hook สำหรับ M6) → Dream → Victory/WakingUp → Hub; กระโดดขึ้นเตียง-นอนหงาย-กล้องไปที่หมอน-เสียงเบาลง-**paper-peel transition** (แถบกระดาษพับเข้า/ลอกออก ไม่ใช่ portal วงกลม); ตื่นข้างเตียง + Run Summary + Dream Memory (+10 ชนะ, +5 ค้นพบวิธีใหม่, +2 แพ้)
- `SceneContext`: เล่นจาก scene ไหนก็ได้; `Dream_Chair` placeholder สำหรับทดสอบวงจร (M4 จะแทนที่)

**Tests run (Unity 6000.3.11f1)**
| Suite | ผล |
|---|---|
| EditMode | **24/24 passed** (+ save roundtrip/atomic/migration/corrupt/clamp, diary, interaction scoring) |
| PlayMode | **15/15 passed** (+ HUB-02 bed prompt & lamp→settings ล็อก/คืน input, **FLOW-01 Hub→Dream→Hub ×5**: ไม่มี state รั่ว, ไม่มี singleton ซ้ำ, timeScale=1, ตื่นที่ wake pose, save ถูกต้อง 39 memory) |
- Manual/visual: ถ่ายทุกขั้นของวงจรจริงใน Play Mode → `docs/evidence/M2_M3/`

**Bugs found & fixed** — test เก่าโดนเรขาคณิตของ scene ก่อนหน้า (กำแพงห้องนอน) → `TestScenes.Isolate()`; กล้องตอนเริ่มชนกำแพงใต้ → ย้าย spawn; โคมไฟร้อนจัด → ลด intensity + ปิดเงาโป๊ะ; ท่านอนมือบังหน้า → animator โหมด sleeping

**Known issues / not done yet**
- ตายังไม่หลับ (ต้องมี face expression — M7), แถบ transition ยังเป็นสีเรียบ (paper texture — M7), ปีนเตียงเป็น hop arc ไม่ใช่ animation ปีน
- Bed Upgrade (GDD §21) ยังไม่ทำ — วางแผนไว้ M7 ตาม GDD Phase H
- Diary/Settings ยังไม่รองรับภาษาไทย (font) — ต้องตัดสินใจเรื่อง localization
- ยังไม่ได้ทดสอบ gamepad navigation ในเมนูด้วยจอยจริง

**Design risks** — ผู้เล่นใหม่จะรู้ไหมว่าต้องไปที่เตียง? (prompt ขึ้นเมื่อใกล้เท่านั้น) → ต้องดู metric "time to discover bed" ใน playtest; อาจเพิ่ม hint เบา ๆ หลัง 60 วิ

**Art findings** — ห้อง graybox อ่านง่าย มีอารมณ์ cozy จากแสงจันทร์+โคมแล้ว; สีผนังลาเวนเดอร์อิ่มตัวต่ำกว่าเตียง/ผู้เล่น ✓; UI กระดาษครีม-หมึกเข้ม ✓

**Next** — M4 Chair stage graybox (sections A–E + arena 32×25 + Little Stool + stage camera)

---

## M4 — Chair Stage Graybox (2026-10-16) ✅

**Completed** (`DreamRush/Build/Dream Chair` → `Dream_Chair.unity`)
- โครงสร้าง 4 จังหวะ (GDD §2.2/§13) เป็นเส้นตรงเหนือ "dream void": **A Arrival** (ตกลงช้า ๆ แบบขนนก, พรมวงกลมบอกจุดลง) → **B Tutorial** (เดิน / ออตโตมันสูง 0.6 ต้องกระโดด / เบาะลอย gap 1.2–2.3 m) → **C Little Stool** (เก้าอี้เล็กวิ่งหนีตามเส้นทางคาดเดาได้, เหนื่อยเป็นช่วง, จับแล้ว **สะพานกระดาษ (2D Type C) กางออก** ข้ามช่องว่าง 6.5 m ที่กระโดดข้ามไม่ได้) → **D Card Test** (ของเล่นไขลานเดินไปมา รอการ์ด M6) → **E Arena 32×25** ตาม §14.3 (A low platform, B balcony 2 m + บันไดเบาะ, D cushion route, E movable block, โซฟา/โต๊ะ/โคมเป็นที่กำบัง, สวิตช์, กำแพงมุมมน, เก้าอี้บอส graybox)
- `StageCameraController` มุม 3/4 คงที่ (pitch 30°, back 12/up 7, FOV 45 ตาม GDD), look-ahead, damping, peek ±25°, `StageCameraZone` ต่อ section (arena: pitch 40°, distance 20), boss framing 2 เป้า (พร้อมใช้ใน M5)
- `DreamLevelManager` (checkpoint 5 จุด, ตกฉากแล้ว respawn + event `Fell` สำหรับ Sleep Stability), `DreamSection` + `DreamHUD` (objective ตาม section)
- 2D cutouts กระดาษ (Type A: เมฆ/ดาว/พระจันทร์/ต้นไม้ — สร้างจากสคริปต์ `ArtSource/Illustrations/Scripts/make_cutouts.py`, ขอบตัดมือ+ความหนากระดาษ+เส้นหมึก), material lit+alpha clip ให้รับแสงเดียวกับ 3D, billboard เฉพาะแกน Y, poster ติดผนังไม่หมุน (§5.8), ไม่มี collider บนของตกแต่ง (§5.7)
- พื้นหลังเฟอร์นิเจอร์ยักษ์ลอยเอียง saturation ต่ำ (§5.3)

**Tests run** — EditMode **24/24**, PlayMode **19/19**
- LVL-01 bot (motor จริง + input สคริปต์) เดินจากจุดเกิดถึงห้องเก้าอี้เล็กใน 7.3 s sim, **ตก 0 ครั้ง**
- LVL-02 เก้าอี้เล็กวิ่งหนี 3.7 m แล้วโดนจับ (เดินธรรมดาก็จับได้), สะพานกางและเดินข้ามได้ไม่ตก
- LVL-03 ตก void → respawn ที่ checkpoint ล่าสุด + event · LVL-04 arena มีครบทุกองค์ประกอบ, bot ปีนบันไดเบาะขึ้น balcony 2 m ได้

**Known issues** — สะพานกระดาษตอนพับตั้งสูง 6.7 m บังกล้องถ้าวาร์ปข้ามไป (เล่นปกติไม่เกิด); กำแพงใต้ของ arena บังตัวละครเล็กน้อยตอนเข้า; void ยังเป็นแค่สีฟ้า (ต้องมี paper sky ใน art pass)

**Design risks** — tutorial อาจง่ายไป (ตั้งใจให้ forgiving) → วัดด้วย playtest; เก้าอี้เล็กจับง่าย (ปรับแล้ว: notice 6.5, speed 4.8)

**Next** — M5 Chair AI (state machine 13 states, escape scoring บน NavMesh, fatigue, stumble, anti-softlock, seat 2 s) + Sleep Stability + chase pushback

---

## M5 — Chair Boss AI + Sleep Stability (2026-10-16/17) ✅

**Completed**
- `BossChairController`: state machine (Dormant → Reveal → Idle/Alert/Flee/Panic/Reposition/Hesitate → Stumble/Exhausted → Sitting → Defeated) ที่ step แบบ deterministic ได้ (เหมือน motor ผู้เล่น) เพื่อให้ bot test ล็อกจังหวะกันได้
- `EscapeDestinationSelector`: ให้คะแนนจุดหนีบน NavMesh (ระยะจากผู้เล่น, มุมไม่สวนผู้เล่น, ไม่ติดมุม, path จริง) + โหมด relaxed/squeeze เมื่อถูกต้อนเข้ามุม; `ChairFatigue`: วิ่งหนี/panic สะสมความเหนื่อย → Exhausted (ช่องให้นั่ง); พักแล้วฟื้น
- ฉากเปิดตัว: เก้าอี้หันมา → พุ่งเข้ามา → **เตะนิ้วก้อยเท้า** (ผู้เล่นกระเด็น + "OUCH!!") → วิ่งหนี ตาม pitch
- เมื่อถูกต้อนมุม: ถ้ามีกล่องที่ผู้เล่น **ดันมาขวางจริง** (≤4 m) → สะดุด (วิธีชนะ "Obstacle"); ถ้าไม่มี → เบียดหลุดแล้ว panic (การต้อนมุมเฉย ๆ จึงไม่ใช่ทางลัด)
- กระโดดตัดหน้าจากที่สูง → Stumble (วิธีชนะ "Shortcut"); anti-softlock `SafeReposition`
- นั่งต้องค้าง **2 วินาทีต่อเนื่อง** ผ่าน `SitForSecondsWinCondition` (กระโดดออก = เก้าอี้หนีทันที)
- `SleepStability` (3 + mattress level, blanket shield) — ตก void/โดนชน = เสีย 1; หมด = ตื่น (แพ้); ความเหนื่อยของเก้าอี้ลดลงเมื่อผู้เล่นโดน (ระยะห่างกลับมา ตาม pitch)
- Hint เบา ๆ ที่ 60 s และ 110 s

**Tests** — PlayMode `BossChairPlayModeTests` + `BossRevealPlayModeTests`
- CHAIR-03 bot 50 seeds (ผู้เล่น bot ใช้ motor จริง ไม่ใช้การ์ด): **ชนะ 50/50, เฉลี่ย 26.8 s, invalid frames 0, softlock recoveries 0** (`docs/evidence/M5_M6/chair_bot_stats.txt`)
- reveal kick ยิงครั้งเดียว, เก้าอี้อยู่บน NavMesh ตลอด, นั่งไม่ครบ 2 s ไม่ชนะ, ชนะครั้งเดียว
- Manual: ถ่ายภาพ reveal (`rv_*.png`), chase (`chase_*.png`), นั่ง→ชนะ→กลับห้องนอน (`sit_*.png`)

**Decision (บันทึกแล้วใน Decision Log)** — `exhaustionThreshold` GDD 18 → **36** และ panic rate 1.5: ค่าตาม GDD ทำให้ bot ชนะใน ~9 s (ไม่มีความตื่นเต้น)

**Known issues / risks**
- bot ชนะทุก seed ด้วย Exhaustion ใน ~27 s → ยัง "ง่ายสำหรับ bot ที่ไล่ตรง ๆ" ผู้เล่นจริงอาจช้ากว่านี้มาก — ต้องวัดใน playtest (เป้า: ชนะครั้งแรก 60–150 s)
- เก้าอี้ยังเป็น graybox (กล่องไม้ + ตา cutout) — model จริงอยู่ใน M7

---

## M6 — 3-Card Draw, 6 Starter Cards, Dream Laws (2026-10-17) ✅

**Completed**
- `CardDefinition` (ScriptableObject) + `CardLibrary` (Resources) + `CardDrawManager.DrawThree` (seeded, ไม่ซ้ำ)
- **หน้าเลือกการ์ด 3 ใบ** ระหว่างหลับ (`CardSelectPanel`, เมาส์/1·2·3/จอย) → เก็บใน `RunState.ChosenCardId` → ถือเข้าฝัน; HUD มุมซ้ายล่างแสดงการ์ด + `[Q] READY` / นับถอยหลัง cooldown
- การ์ดทำงานผ่าน **Dream Logic** (DreamProperty + IEffectReceiver) ไม่ hard-code ชนะ — ทุกใบแค่ "เปิดช่อง" ผู้เล่นยังต้องเดินไปนั่ง 2 s เอง:
  | การ์ด | ผล | วิธีชนะที่บันทึกใน Diary |
  |---|---|---|
  | SUPER GLUE | เก้าอี้ติดพื้น (Rooted) | Rooted |
  | CAMERA FLASH | เก้าอี้ตาพร่า หยุดนิ่ง (Frozen) | Frozen |
  | FISHING ROD | ดึงเก้าอี้มาหา (ตาม NavMesh อ้อมเฟอร์นิเจอร์) แล้วมึน | Pulled |
  | BE A GRANDPA | ผู้เล่นกลายเป็นคุณปู่ช้าลง → เก้าอี้ "ลุกให้นั่ง" (SocialResponsive) | Grandpa |
  | DECOY APPLE | แอปเปิลหลอก เก้าอี้หนีตัวปลอมแทน | — (ช่วยต้อน) |
  | BOUNCY CUSHION | เบาะเด้งสูง ขึ้น balcony เพื่อกระโดดตัดหน้า | Shortcut |
- **Dream Laws** = การตีความ "การ์ดข้อห้าม" ใน pitch: ระหว่างไล่ จะมีกฎสุ่มประกาศล่วงหน้า 2 s แล้วมีผล 15 s — NO JUMPING / NO RUNNING / BUTTERED FLOOR (ลื่น) พร้อม badge แดงมุมขวาบน
- Toy (ของเล่นไขลานใน Section D) ตอบสนองการ์ดทุกใบ = ที่ฝึกใช้การ์ดก่อนเจอบอส

**Tests run**
| Suite | ผล |
|---|---|
| EditMode | **39/39 passed** (+ DrawThree ไม่ซ้ำ/seeded/ครบทุกใบ, action→property mapping, win condition 2 s) |
| PlayMode | **38/38 passed** (+ CardSystem 10 tests: Glue/Flash/Rod/Grandpa → นั่งชนะด้วยวิธีที่ถูกต้อง, เล็งผิดไม่ใช้การ์ด+ไม่เข้า cooldown, cooldown กันใช้ซ้ำ, decoy, cushion เด้ง vy>10, Dream Law ห้ามแล้วคืนค่า, UI เลือกการ์ดเก็บค่าถูกใบ) |
- Manual ใน Play Mode จริง: Hub → นอน → **หน้าเลือกการ์ด** (`m6_cardselect.png`) → เลือก → ฝันพร้อมการ์ดใน HUD (`m6_dream_hud.png`) → ใช้การ์ดครบ 6 ใบกับเก้าอี้จริง (`m6_use_*.png`), เห็น Dream Law "BUTTERED FLOOR" ขึ้นเอง

**Bugs found & fixed**
- FISHING ROD ดึงเป็นเส้นตรงแล้ว **ติดโต๊ะกาแฟ** (NavMesh raycast หยุด) และหมดเวลา 1.2 s ก่อนถึง → เปลี่ยนเป็นดึงตาม NavMesh path ที่ 8 m/s และดึงจนถึงตัว (สูงสุด 4 s) — test ใหม่ครอบกรณีมีโต๊ะขวาง
- Toast ข้อความทับป้าย "THE CHAIR" → ย้าย toast ลงใต้ป้าย
- `CardSelectPanel.Run` อ่าน panel หลัง Destroy; string มี newline จริงใน `DreamHUD` → แก้แล้ว
- DevBridge เพิ่มคำสั่ง `uishot` (render UI overlay ผ่านกล้อง) — ถ่าย UI ได้โดยไม่ต้องโฟกัส editor

**Known issues / not done**
- ร่างคุณปู่ (สีเทา + ไม้เท้า) มองไม่ค่อยออกจากกล้อง arena ที่ไกล → M7 ต้องมี silhouette ชัด (หลังค่อม/หนวด) + VFX ตอนแปลงร่าง
- ยังไม่มี VFX/SFX เฉพาะการ์ด (กาวกระเด็น, แฟลชขาวเต็มจอ, สายเบ็ด) — M7
- ยังไม่ได้ทดสอบด้วยจอยจริง / ผู้เล่นจริง
- การ์ดยังเป็นภาษาอังกฤษ (ฟอนต์ไทย = ตัดสินใจ localization ใน M7)

**Design risks** — Grandpa ทรงพลังมาก (8 s, เก้าอี้ยืนให้นั่ง) → อาจต้องเพิ่มเงื่อนไข "เดินช้า ๆ เข้าไป" ใน playtest; Dream Law ทับกับการ์ด (เช่น NO JUMPING + Bouncy Cushion) ควรเป็น "combo ที่สนุก" ไม่ใช่ทางตัน → ตอนนี้เบาะยังเด้งได้ (การเด้ง ≠ การกระโดด) — ตั้งใจไว้

**Next** — M7 Art/Anim/VFX/SFX/UI pass

---

## M7 — Art / Animation / VFX / SFX / UI pass (2026-10-17) ✅ (P0 quality)

**Completed**
- **BOSS_Chair model** (`ArtSource/Blender/Scripts/build_chair_boss.py` → `BOSS_Chair.fbx`, 10.5k tris): honey wood with painted grain, rounded and slightly uneven, arched backrest, eyes/pupils/brows on the backrest, small mouth and blush. Same footprint as the graybox, so gameplay did not change. Art review **APPROVED** (`ASSET_MANIFEST.md`).
- **Chair body language** (`ChairAnimator`): four legs moving independently (trot when running, wilder strides when panicking, tangled when stumbling, heels dug in when pulled, a polite bow when offering the seat, flailing when sat on), worried brows, pupils that shrink when frightened, blinking, eyes that follow the player, wooden footsteps.
- **Tiny chair in the bedroom** uses the same model at 0.42× with no face (foreshadowing).
- **Apple's face** (`AppleFace`): blinks (sometimes twice), closes its eyes while sleeping, and screws them shut when hit or kicked. Works by swapping the eye material slots so only the ink lid lines remain, no new rig needed.
- **Be a Grandpa** now has round glasses, white eyebrows and moustache, a hunch and a cane, so it reads from the arena camera.
- **VFX** (paper-cut, new sprites from `make_fx.py`):
  - FX_Flash (plus a full-screen white flash with a Reduce Flashing setting)
  - FX_GlueSplat (puddle decal under the chair)
  - Fishing line drawn with a sag
  - FX_Dust (jumping / landing)
  - FX_Ouch (pinky-toe kick and toy-car hits)
  - FX_Heart (offer seat)
  - FX_Confetti (victory)
  - Cardboard decoy standee
  - Pillow-shaped Bouncy Cushion
- **Camera shake** on the kick, hard landings and hits, with a Reduce Screen Shake setting.
- **Audio** (`AudioManager` + 30 SFX + 3 music loops synthesised by `ArtSource/Audio/Scripts/make_audio.py`, our own work, CC0, see `docs/AUDIO_CREDITS.md`):
  - Footsteps, jump, land, wooden chair steps, the chair's whistle voice (gasp / yelp / tired / creak)
  - Every card, UI, falling asleep / waking up, Dream Law warning, victory
  - Bedroom music → dream music → boss music, crossfaded, with Music and SFX volume settings that now actually work
- **Sleep/wake transition** now uses paper pages with fibres, torn edges and faint star/moon doodles (`make_paper.py`).
- **Bed Upgrade (GDD §21.3–21.5)**:
  - Dream Jar at the foot of the bed → UPGRADE BED panel.
  - Upgrades: Cloud Mattress (3 levels, +1 Sleep moon each), Lucky Pillow (1 card reroll per dream, with a REROLL [R] button on the card-select screen), Comfort Blanket (blocks the first hit).
  - Buying saves immediately and the bed changes visibly (cloud puffs, lucky pillow, patchwork quilt).
- **Toy-car hazards** (pitch "hit an obstacle and lose distance"): 2 wind-up cars run lanes painted on the floor during the fight.
  - Getting hit: knocked back, lose 1 Sleep moon, and the chair recovers some stamina (it gets further away).
  - You can jump over them; there is a cooldown so one car can't hit twice.

**Tests run**
| Suite | Result |
|---|---|
| EditMode | **45/45 passed** (+ AudioAssetTests: every sound the code calls has a clip; BedUpgradeTests: pay once, caps, not enough memory, effects) |
| PlayMode | **42/42 passed** (+ card feedback: flash appears and fades on its own, glue decal, fishing line, fail sound; upgrade panel → save + bed visuals + next dream has 4 Sleep moons and a shield; Lucky Pillow reroll once; toy car: hit costs 1 moon + knockback, jumping over is safe, no double hit) |
| Chair bot 50 seeds | 50/50, average 26.3 s, invalid frames 0, softlock 0 (unchanged after the new model) |

- Manual / visual (Play Mode, screenshots in `docs/evidence/M7/`):
  - 11 chair states up close (`m7_faces.png`, `m7_st_*.png`)
  - Kick + OUCH (`m7_kick_*`), all 6 cards with FX (`m7_fx_*`)
  - Grandpa face/side (`m7_grandpa.png`), apple eyes open/closed (`m7_face.png`)
  - Upgrade panel before/after + bed (`m7_upgrades.png`, `m7_hubart.png`)
  - Toy cars in the arena (`m7_cars.png`), paper transition (`m7_trans.png`)

**Bugs found & fixed during M7**
- Testing from DevBridge with Unity in the background froze play mode → `PlayerSettings.runInBackground = true` + `QueuePlayerLoopUpdate`.
- The chair's eyes overflowed the backrest at 1.5×. Fixed by capping eye size and showing fear with smaller pupils; the brows also looked angry at rest, so they now sit slightly worried by default.
- Wood texture was too red and zebra-striped; changed to softer honey grain.
- Grandpa cane floated away from the hand → moved next to the right hand (`m7_grandpa_cane.png`), but the hand doesn't actually grip it yet (the hands stay clasped).

**Known issues / limitations (honest)**
- The sounds are procedural placeholders. I can't listen to them myself, so they were checked numerically only (peak, no clipping, smooth loop seams). **The user needs to listen and give feedback.**
- No ink outline yet; the chair's mouth doesn't change shape; there is no dedicated camera view for the bed panel (it uses the jar's view toward the bed); the jar glass is opaque.
- The chair passes through toy cars (cars only check distance to the player).
- UI text is still English; Thai needs a font + localization decision.
- No playtest with a real person or a gamepad yet (→ M8).

**Design risks**
- Toy cars may annoy rather than challenge. The lanes are visible and jumpable, but this needs real-player testing.
- The Lucky Pillow reroll is fairly strong when there are only 6 cards.

**Next** — M8 Playtest/QA/Polish

---

## M8 — Presentation Overhaul v2: VFX, UI v3, Lobby, Galaxy select, new Pip, World 1 (2026-10-17) 🟡 in progress

Scope = the user's v2 brief (reference-quality look, Persona×FF UI, lobby, galaxy level select, remade models,
goofy movement, close 3rd-person camera, a BIG first level with style-mixed chapters and a boss with hard-to-reach skills).
Research and decisions: `docs/RESEARCH_V2.md`; level design: `docs/LEVEL1_DESIGN.md`.

**Features**
- **VFX library** (`Presentation/VFX.cs`, `VFXLibrary`, `Shaders/DreamParticle.shader`, textures from `make_vfx_textures.py`):
  pooled particle bursts (DustPuff, Poof, LandingRing, ImpactRing, StarBurst, CardBurst, Confetti, Zzz) with
  anticipation→impact→dissipation curves and a point-light flash on every impact (HD-2D trick). Hooked into landing,
  skids, card use, chair kick/heart/defeat.
- **UI v3** (`UI/UIStyle.cs`, sprites `make_ui_kit_v3.py`, fonts Kanit Black Italic + Chakra Petch, OFL):
  3-colour Persona system (plum / cream / apple red + lavender select), skewed slabs, chamfered FF windows, hard shadows,
  halftone/stripe textures, overshoot entrances (`UIEnter`), slab buttons that grow/shift/point when selected (`SlabButtonFX`).
  Rebuilt: HUD (objective slab, moon icons that pop when lost, boss name + TIRED meter, dream-law burst, card bottom-right),
  card select (fanned glossy cards with sheen + burst behind the highlighted one), pause/settings/diary frames,
  diagonal jagged-wipe transition, toasts, prompt, **boss intro card** and chapter cards.
- **Lobby / title** (`UI/TitleScreen.cs`): over the live bedroom; camera swings to a different shot of Pip per menu item,
  stair-stepped Persona menu on a red band, DREAM RUSH! logo, FF help bar, DOF + key/rim light. Once per session.
- **Dream Galaxy level select** (`Flow/GalaxySelect.cs`, `GalaxySelectBuilder`, Blender `build_galaxy_kit.py`):
  nebula sky shader, 4 storybook planets (World 1 open; 3 locked previews), Pip on a flying pillow, chapter list panel.
  Flow is now Hub → sleep → **Galaxy** → cards → chosen level (`GameFlowManager.LevelSelectStep`; tests skip it).
- **Pip v2 model** (`build_apple_v2.py`, 12.3k tris, same part contract): big eyes with shines, brows, open grin + tongue,
  blush, white mitts, lavender sneakers, painted skin texture. Old model kept as backup in `ArtSource/Blender/Characters/`.
- **World 1 — `Dream_World1.unity`** (`World1Builder`, runtime `Scripts/Runtime/World1/`): 5 chapters ≈ 1.7 km of level,
  - Ch1 Pop-up Picture Book: pop-up stairs → timed pages → catapult pages, living-room spread, chair cameo.
  - Ch2 Paper Theater: side-rail 2.5D camera, sliding cardboard sets, crumbling bridge, back-layer route behind scenery.
  - Ch3 Toy Galaxy: bounce cushion → chained launch pads with star-bit arcs → orbiting islands.
  - Ch4 Phantom Shelf-tops: CHASE the chair (rubber-banded, throws rolling books, "IT GOT AWAY!" reset).
  - Ch5 Cathedral of the Throne: candle hall, fog gate, Ø44 m arena around the rotating Throne Tower, boss.
  - Per-chapter sky/fog/ambient/lights/post volume (`ChapterDirector`), page-portals with chapter title cards,
    pillow-fire checkpoints, 15 Dream Stars, Throne Tower landmark visible from every chapter.
- **Boss skills** (`Boss/ChairSkills.cs`, World 1 only): Shockwave Stomp (hop + red telegraph + expanding ring, jump it),
  Cushion Volley (red circles 1 s before 3–5 cushions land), Musical Chairs (4 decoys shuffle; the real one has the
  gingham seat; decoys poof and bonk). Phase escalates with fatigue; skills never run during an opening.
- Player motor: `LaunchBallistic` (exact launch-pad arcs); `MovingPlatform` carry (incl. rotation / orbit).

**Tests run**
- EditMode 45/45, PlayMode 42/42 after VFX + UI v3 (Dream_Chair suites unchanged).
- New `World1PlayModeTests` (W1-01…06: scene content, pop-up stairs climbable, launch arc accuracy, portal → chapter 2
  with side rail, chase fail/reset, stomp + volley hit a careless player) — **result pending (device disconnected
  during the run)**.

**Known issues / not done yet (honest)**
- Boss P3 "Throne Climb" is not implemented (P3 = faster skills + volley of 5 for now).
- Ch2 Paper Theater is the roughest chapter visually (dark, needs an art pass); several chapters still use primitive
  shapes for structure (islands, shelves, stage) with toon materials.
- New Pip model has not been reviewed in motion with the goofy animator in every scene; sleep pose/snot bubble on the
  new model not verified yet.
- No real-player playtest of World 1 yet; difficulty of timed pages / orbit jumps unknown.

---

## M9 — DREAMLAND open world + move set (2026-10-17 → 10-19) ✅ built + tested

Brief: "แมพใหญ่ open world หนี มันๆ เมคคานิกโหดๆ สวยๆ VFX/ภาพแบบ Final Fantasy". Design: `docs/OPENWORLD_DESIGN.md`.

**Features**
- `Dream_Openworld.unity` (`OpenWorldBuilder`, menu *DreamRush/Build/Dreamland (open world)*): 640 × 640 m toon mesh
  terrain (100 chunks, `DreamlandTerrain` height field shared with runtime) with 5 regions — Starfall Meadow, Crystal
  Cliffs (26 m plateau), Mirror Lake (+ 7 floating islands, waterfalls, speed-ring route), Clockwork Ruins (arch ring,
  pillars), Pine Hills — and a mountain rim. Baked NavMesh asset (lake excluded).
- FF golden-hour look: `FantasySky` (gradient, sun + halo, painted clouds, stars, moon), `DreamTerrain` (grass clumps,
  golden patches, strata cliffs, sand, painted paths), `DreamGrass` (instanced wind-swept tufts with back-lit tips,
  lazy 32 m chunks), `DreamWater` (swell, fresnel, glints), emissive crystals with pulsing lights, ACES grade + bloom,
  pollen motes around the camera, updraft streaks, god rays.
- Blender kit `build_openworld_kit.py`: 3 round trees, pine, 2 crystal clusters + crystal spire, 2 boulders, 2 floating
  islands, ruin arch + pillar, **THE ALARM** monster, Dream Key, speed ring; galaxy planet `PLANET_Dreamland`.
- **Move set** (`PlayerAbilities`, open world only): double jump (flip), dash (air ×1, ground cooldown, momentum carry),
  glide (pillow parachute), ground pound + pound jump; motor gained `Dash`, `SetVelocity`, fall clamp, updraft accel.
- **Hunt** (`OpenWorldChair`): flees between 16 landmarks by NavMesh, stamina → exhausted → land on it / press E → sit 2 s
  → Dream Key; reappears far away, faster each time. 3 keys = victory.
- **Be hunted** (`NightmareAlarm`): roam → notice (0.8 s) → chase 9.5 m/s → telegraphed slam shockwave; loses you
  out of sight / far / in a pillow-fire `SafeZone`; ground pound stuns it.
- HUD: compass strip (chair, alarm, pillow fires), keys counter, danger vignette, region title cards.
- Galaxy: new unlocked world **W2 DREAMLAND**.

**Tests (run 2026-10-19, Unity 6000.3.11f1)** — full PlayMode suite **52/52 passed** (102.9 s), incl. OW-01…04:
- OW-01 content + NavMesh reachability between all landmarks ✅
- OW-02 move set: jump 2.10 m, double jump 3.88 m, dash 4.68 m in 0.35 s ✅ (threshold raised from 0.22 s window — the
  terrain at spawn slopes slightly so the short window was flaky at 2.43 m)
- OW-03 chair: exhaust → sit → Dream Key ✅ · OW-04 alarm notices/chases, loses you in a SafeZone ✅

**Played / looked at in engine** — meadow spawn, vista to the cliffs, lake + floating islands, ruins, the alarm hunting
(red vignette + region card), the exhausted chair, galaxy W2 panel.

**Fixes during verification** — Blender `join` kept the first part's transform (alarm lay sideways, trees sunk) →
transforms applied before export; alarm faced backwards; `DreamParticle` lacked `[MainColor]`; terrain strata moiré;
inland sand (now G-mask only); islands too pink; empty objective slab.

**Known issues / risks** — no full 3-key playthrough by a human yet (balance of chair speed vs. alarm untested by feel);
grass draws only near the player (pop-in at ~90 m); W1 Ch2 Paper Theater still graybox-ish; boss P3 Throne Climb not built;
new Pip v2 has no sleep pose yet.

**Next** — human playtest + tuning, FF haze / canopy polish, Ch2 art, P3 climb, Pip v2 sleep pose.

## M10 — World 1 boss P3 "Throne Climb" + Dreamland full loop (2026-10-19) ✅ built + tested

**Features**
- `ThroneClimb` (Boss): when THE CHAIR enters P3 (fatigue ≥ 66 %) it crouches (0.5 s telegraph), leaps onto the
  Throne Tower top and kicks the gold throne away. 8 books slide out of the stack as a rotating spiral staircase
  (14°/s, each step a carrying `MovingPlatform`). From the top it lobs pillows: red circle 1.1 s ahead (rides the
  step it lands on), hit = knock-back + a moon. Reach the top → chair Exhausted → sit 2 s → win ("ThroneClimb").
  Fall off → it recovers.
- `BossChairController`: new `ChairState.Perched` + `IsPerched` (no NavMesh locomotion / softlock teleport while up
  there; cancelled sits, expiring cards or the fishing rod put it back on the throne). `ChairAnimator`: smug perched pose.
- Ch5 tower re-art: core collider hidden; 8 hand-stacked round books (covers, page block, spine, gold band).
- Dreamland grass draw distance 95 → 150 m with sqrt thinning (no pop-in line near the player).
- DevCommands `ThroneClimb start|step,N|view`.

**Tests (Unity 6000.3.11f1)** — PlayMode **55/55**, EditMode **45/45**.
- W1-07: leap → Perched on top (y = 9.0), stairs out, chair stays up 1.5 s; **bot physically jumps step 1→8 on the
  rotating stairs** (lands within 0.04 m of each top) and onto the top → chair Exhausted.
- W1-08: a pillow hits a climber standing still; at the top `TrySit` → Victory, method "ThroneClimb".
- OW-05: 3 chases → 3 keys → `Won`; chair reappears 247 m / 381 m away; stamina 26 → 32.5 → 48.8, speed 7.6 → 8.4.

**Played / looked at** — tower before/after the leap, stairs out, Pip on step 4, a pillow knocking Pip off ("OUCH").
Bug found in play: the pillow aim ray hit Pip's own capsule → fixed (skip the player's colliders).

**Known issues / risks** — the third-person camera does not frame the tower top while climbing (needs a boss-look
assist); pillow rhythm (2.6 s) not tuned by a human; no full human playthrough of W1 or Dreamland yet.

**Next** — climb camera assist, human playtest + tuning, Ch2 Paper Theater art, Pip v2 sleep pose.

## M11 — THE NIGHT ROOM: the lobby is the entry screen (2026-10-20) ✅ built + tested

**Features** (docs/LOBBY_DESIGN.md)
- No title / press-start: boot → bedroom lobby. After every dream you wake into the lobby again.
- Bedroom rebuilt from graybox into a floating cut-away diorama: 21 Blender models (`build_bedroom_kit.py`),
  game-drawn posters/wallpaper/floor (`make_lobby_art.py`), DreamToon `_WorldUV` for walls/floors.
- "Chaos pop" lobby UI (`TitleScreen`, same API): ransom-note logo, live clock card, marquee bands, halftone + grain +
  scanlines, stair-stepped menu with jagged burst + RGB-split echo + Thai subtitle, apple pointer, sticker storm,
  P5-style slash transition, per-item camera shots, Pip looks at the object.
- `LobbyTheme` ×4 moods by the real clock + `LobbyThemeDirector` (sky FantasySky/NebulaSky, light, walls, lamps).
- WALK AROUND → free roam with a "TAB ◆ LOBBY" chip; TAB/Start returns.
- New OFL fonts (Google Fonts): Bungee, Dela Gothic One, Bowlby One, Rubik Mono One.
- Fixed latent fake-null `??` AddComponent bugs (lobby, CardEffectResolver, DevCommands).

**Tests** — EditMode 48/48 (new LOB-01…03: hour→mood mapping, palettes distinct + WCAG contrast ≥4.5, override).
PlayMode HubFlow 4/4 (new LOB-04 boot straight into lobby/6 items/shots/WALK→free roam→back; LOB-05 DREAM → sleep).
**Looked at** — all 6 shots × midnight; morning, afternoon, sunset; walk mode.

**Known issues** — shots are composed for 16:9; the SETTINGS shot looks past the fence into the sky (fine but plain);
the toy train reads a bit like a stool at 1.25×; menu sounds are still the old placeholder SFX (M15 music/SFX pass).

## M12 — Combat kit + COUNT MIDNIGHT (graybox) (2026-10-20) ✅ prototype tested

**Features** (docs/LEVEL2_DESIGN.md) — `PlayerCombat` (lock-on, leaf-blade 3-hit combo, hold → charged leap slam,
dodge roll with i-frames + backstep, DREAM DODGE slow-mo, stamina, hit-stop, lock reticle, ER-style lock camera),
input map (Attack, LockOn, tap-Sprint roll), motor hooks (`FaceDirection`, `MoveSpeedScale`), `CountMidnight`
(graybox body with live clock face, 4 P1 attacks, STOP THE CLOCK, phase 2, posture/kneel/critical, SNOOZE finisher),
`MidnightBullets` (4 Undertale-style patterns, pooled, jumpable low bullets), `BossBarUI` (HP + chip, posture,
stamina), graybox arena `Test_BossMidnight` (`BossArenaBuilder`), DevCommands `Midnight`.

**Tests** — new PlayMode `BossMidnightPlayModeTests` 6/6: CM-01 wake/bar/lock-on · CM-02 combo 3 hits, −84 HP,
posture 38 · CM-03 sweep hits standing / jump clears / timed roll dodges (perfect dodge 1) · CM-04 posture break →
critical −240 · CM-05 time stop: 292 bullets, grey volume, back to colour, phase 2 · CM-06 snooze victory.
Found while testing: blanket shield soaked the first hit (tests now count landed hits); combo range 1.9 m missed when
the boss backed off → range 2.25 m, boss only backs off under 2.2 m.

**Looked at** — graybox screenshots of lock-on, cleave wind-up/hit, sweep, time-stop bullets. Graybox camera lost
the boss's head when close → lock camera now pulls back +1.8 m and looks up.
**Next (M13)** — real boss model (Blender), Persona-style level 2 around it.

## M13 — MIDNIGHT PALACE (level 2, Persona-style open city) + model quality pass (2026-10-20) ✅ built + tested

**Features**
- **Look:** a full-screen comic pass (`ComicPost.shader` + `DreamComic` renderer feature) with ink outlines, print-dot
  halftone, posterized light, saturation, speed lines and glitch. Each scene sets it through its `StylePack`. Halftone
  dots now only darken, never lift, so blacks stay crisp (fixes the washed-out first build).
- **Pip dresses per level** (`StylePack.Outfit`). Level 2 gets the phantom mask + mini top hat; World 1 gets the
  Densha-style headphones.
- **Count Midnight model** (Blender, `CM_CountMidnight.fbx`). It is wired to the M12 fight by `CountMidnightSetup`.
- **Midnight Palace** (`Dream_MidnightPalace`, `MidnightPalaceBuilder`):
  - A neon city at 11:59 with a street grid, 4 grand avenues and 4 district moods.
  - **3 STOLEN HOURS**, one from each challenge: Rooftop Run (E), Gear Works (N) and Shadow Ambush (W).
  - The hours open the clock-tower gate. The route then goes gear spiral → updraft → sky rail → the dial at 92 m.
- **Mechanics:**
  - `GrindRail` + `GrindController` (snap, ride at speed, hop off).
  - `TickMinion` enemies and `AmbushChallenge` waves.
  - `StolenHour`, `PalaceDirector` (district moods, objective, gate, sky-dial fall rescue).
- **The 10-second rule:** `WonderDirector` tracks novelty (rings, pads, districts, hours, waves). If nothing new
  happens for 7.5 s it fires an ambient set piece: paper planes, sky whale, fireworks, shooting stars, calling cards,
  sky tram, time skip, moon wink or neon wave.
- **Footsteps:** 6 new soft step sounds, at least 0.17 s apart, quieter, and every other step skipped while
  sprinting (the "ตึกๆๆ" complaint).
- **Model quality pass:**
  - **Pip v3** (`build_apple_v3.py`): 5-lobed apple base; iris + lash line + teeth; veined double leaf; 3-finger
    mitts with rolled cuffs; real sneakers (toe cap, accent band, laces, star badge, heel tab); a star dream-pillow
    backpack so the back view reads. Body size and pivots are unchanged; outfits were re-fitted.
  - **Count Midnight v2:** bent sleeves with flared cuffs and long fingers; winged phantom mask with angled slits;
    a wicked toothed grin; lapels, shirt-front, cravat and ruby pin; pointed epaulettes; striped legs, tall boots
    with spurs. The neck no longer pokes through the dial.
  - **Chair:** lathe-turned legs and posts; finials; 3 spindles; scalloped apron; tufted cushion with piping;
    catch-lights in the pupils; wobbly nervous mouth.
  - **Tick minion v2:** gold-rimmed dial with ticks; mini mask; toothy grin; tilted bells + hammer; mitts; jester
    shoes; jagged shadow cape.
- `TitleScreen.CloseUI` no longer throws when the lobby is torn down mid-fade.

**Tests**
- New PlayMode `MidnightPalacePlayModeTests` 5/5:
  - MP-01 content (3 hours, gate, ≥6 rails, ≥10 minions, ≥10 wonders, boss above 85 m, outfit, ink on).
  - MP-02 a bot runs the streets for 60 s (6/8 waypoints) and gets **max novelty gap 7.5 s** (12 novelties).
  - MP-03 rail grinds at 13.2 m/s and the jump hops off.
  - MP-04 ambush: 2 waves, then the hour is revealed.
  - MP-05 hours open the gate; every gear step is reachable; the sky rail ends at the dial.
- Full suites before the model pass: EditMode 48/48, PlayMode 68/68. After the model pass: EditMode 48/48,
  PlayMode 68/68 (all green).

**Looked at:** in-game screenshots of the spawn, avenue, tower, rooftop, gear works and plaza (before and after the
contrast fix); Pip front, side and back in Blender, plus his back in game; outfits on Pip v3; the boss arena with
CM v2; the chair; the minion.

**Known issues**
- The card HUD still shows in level 2.
- Pip's backpack colour reads pale under some lights.
- The boss intro camera crops CM's head at close range.
- The phantom mask's lower edge sits close to Pip's grin.
- Galaxy W2 still uses the Dreamland planet model.
- **Not yet hand-played end-to-end.** Bot routes and tests only.

**Next:** M14 (World 1 Densha style + more fun), M15 (new music + Cyberpunk/Undertale VFX).

## M14 — "make it actually look and play better": global volume fix, Densha World 1, combat depth, lobby bugs (2026-10-20) ✅ built + tested

**User feedback (10:41)**
- Level 1 hadn't changed and the global volume looked bad.
- Level 2 monsters looked repetitive; the player kept fighting the same monster.
- Pip only had hit-hit-hit, and the sword wasn't held properly.
- Lots of bugs, including in the lobby.

**Root cause found: post-processing was never applied in any scene**
- Every builder created `VolumeProfile` components with `p.Add<T>()` but never called `AddObjectToAsset`.
- The saved profiles held `{fileID: 0}`, so every scene silently fell back to the URP template's `SampleSceneProfile`
  (Neutral tonemapping, no grading).
- That is why World 1/Palace colours were grey (whites capped at ~82 %, 209/255) and why tuning the post settings did nothing.
- Fixed in all 5 builders (World 1, Palace, Lobby, Boss arena, Dreamland): components are now saved, plus `SaveAssets`.
- World 1/Palace/Arena use Tonemapping None (flat comic colour). The lobby uses Neutral +0.25 EV.

**World 1 → Densha-anime look**
- `StylePack` (Densha):
  - ink 1.0 at 2.1 px, teal print dots in real shadows only, posterize, +30 % saturation, speed lines;
  - NEW global `_DR_CelHard` (razor cel edge on every DreamToon material) and `_DR_ShadowTint` (cool-blue shadows);
  - Pip wears the headphones.
- Per-chapter skies (FantasySky anime gradients): Ch1 morning blue/lemon, Ch3 noon cobalt + cumulus, Ch4 golden-hour
  orange/indigo chase, Ch5 blue hour.
- Per-chapter halftone/shadow tint via `ChapterInfo`; chromatic aberration; crisper bloom; a softer god-ray material.
- Palette pushed to yellow/teal/blue/navy.
- More fun: yellow train-track grind rails (shared `GrindRail`) — Ch1 rail line over the timed pages, Ch3 sky rail
  around the orbit puzzle, Ch4 every shelf edge is a rail that bridges the jump gaps.

**Level 2 combat depth** (docs/LEVEL2_DESIGN.md §8)
- **Moves:**
  - AIR SPIN (×2 per jump);
  - RISING SLASH launcher (jump after a landed hit), which chains into AIR SPIN juggles;
  - DASH THRUST (attack during/after a roll, piercing);
  - COUNTER ×2 after a DREAM DODGE;
  - DREAM meter (+7 per hit, +25 per perfect dodge) → **DREAM BURST** on V / pad Y (launches everything, glitch flash).
- New `CombatHUD`: stamina + DREAM bars and a move-list strip (first 40 s / hold Tab).
- **Sword in the hand:** new `BladeRig` (runs after the procedural animator) aims Arm_R along the swing and pins the
  grip to the mitt. New Blender model `PIP_LeafBlade` (leaf blade, veins, star guard, twine grip).
- **Enemy roster** on a shared `EnemyBase` (HP, flash, knock-back, juggles, stun, ground-follow, leash, drop-in):
  - BELL BRUTE: front guard, ring-pound shock wave, bell charge, pant opening.
  - GEAR ROLLER: armoured roll; crashes into walls → dazed, ×1.6 damage.
  - CUCKOO SNIPER: hovers and fires 3-orb fans; slash an orb back and it homes in on its sender.
  - TICK-TOCK rebuilt on the base.
  - 3 new Blender models: BRUTE_Bell, ROLLER_Gear, CUCKOO_Sniper.
- **Placement:** mixed street squads, 2 Brutes at the tower gate, Cuckoos over the rooftops, Rollers on the canal.
  Ambush waves TTT → TGTC → BTTG.
- **Bugs fixed:**
  - The gamepad RB fired attack AND card → card moved to D-pad up.
  - The dream-card HUD showed in level 2 → hidden in combat levels.
  - TickMinion duplicate serialized field (`_dropTarget`).

**Lobby bugs**
- BED/DIARY/SETTINGS panels opened UNDER the lobby overlay (menu, logo and marquee covered them; panels unreadable)
  → the overlay fades out while a panel is open; going back closes panels.
- The DREAM shot framed Pip from behind and under the logo → re-framed Pip as the hero, and he turns to face the lens.
- Close-up furniture shots were blocked by a giant Pip → he is hidden there.
- A resting mouse cursor stole the default selection (also made LOB-04 flaky) → hover selects only after the mouse moves.
- Walk mode: the camera slid into the plant/wardrobe → auto box colliders on furniture.
- `TitleScreen.CloseUI` no longer throws when torn down.

**Tests**
- New PlayMode `CombatKitPlayModeTests` 6/6:
  - CK-01 grip stays 0.39 m from the shoulder at rest and mid-swing.
  - CK-02 rising launches + air spin juggles.
  - CK-03 Brute guard blocks front light hits, takes heavy/back hits; the shock ring is jumpable.
  - CK-04 a reflected orb downs the Cuckoo.
  - CK-05 Roller is armoured while rolling and crashes into a wall (bonus damage).
  - CK-06 DREAM BURST.
- New `World1StylePlayModeTests` 2/2: Densha look on, every volume profile has its components; rails + snapping.
- MP-04 updated to mixed waves (≥3 enemy types).
- EditMode 48/48. Full PlayMode run: see below.

**Looked at** — World 1 all chapters (before/after), Palace street/gate/canal, every move freeze-framed, lobby menu +
every panel + walk-mode corners.

**Known issues**
- Ch2 (paper theatre) is still the weakest chapter visually (big flat backdrop).
- The new enemies have no bespoke attack SFX yet (reuse existing).
- Not hand-played end-to-end.
- The "Game UI design improvements" chat can't be read from here — waiting for the user to paste it.

## M15 — Pip v4 + run feel, every building enterable, city edge, skill VFX, soundtrack + SFX, HUD pass (2026-10-20) ✅ built + tested

User brief (14:40):
- Better character model (arms/legs) and walk/run animation, run VFX and sounds.
- Better UI/UX.
- Level 2: every building can be entered, everything has a purpose, no falling off the map.
- Skill VFX.
- Music and SFX — polish the two levels we have.

**Character + movement**
- Pip v4 (`build_apple_v4.py`): jointed arms (puffy sleeve → elbow → forearm + mitt) and legs (thigh + shorts cuff →
  knee → striped sock + sneaker).
- The procedural animator bends knees/elbows: arms pump (no windmill), knees fold on the forward swing and in the air.
- Heel ribbon trails while sprinting, plus a kick-off whoosh + dust ring.

**Level 2 — enterable buildings with purpose**
- 70+ doors, one on every city building, glowing in their room's colour (special rooms also get a diamond beacon).
- Six pocket rooms (x ≥ 1000, y = 300), re-dressed per building from its seed (resident, dialogue, minutes):
  - **Dreamer's Flat**: a Clockfolk resident gives gossip + real level hints; minutes sit on the furniture.
  - **Watchmaker**: the shop (extra moon, stamina, Dream ink, sharper leaf, warm milk).
  - **Moonlight Cafe**: rest (moons refill) + becomes your respawn point.
  - **Shadow Dojo**: strike the gong for 3 seeded mixed waves → 15 minutes.
  - **Clock Library**: draws the HOUR COMPASS (HUD arrows + distance to every remaining stolen hour).
  - **Time Vault**: spinning-gear climb to a chest (12 minutes).
- New currency, LOST MINUTES:
  - Earned from enemy drops, rooftop/alley caches, vending machines you slash, rooms, the dojo and the vault.
  - Spent at the Watchmaker.
  - Wallet shown on the HUD.
- New NPC model `NPC_Clockfolk`: pocket-watch head, nightcap, pyjamas tinted per resident.
  Persona-style talk box (`DialoguePanel`).
- City edge:
  - A ring of tall towers + invisible walls up to 400 m.
  - Falling below the city teleports you back to the last solid ground (no game over).

**Skill VFX** (reference: golden HD-2D embers + flare)
- `SlashArc`: a crescent mesh that sweeps open, then tail-fades.
  - Diagonal / reversed / wide 3-hit combo.
  - Vertical rising slash, 360° air spin and leap slam, thrust streak.
  - Burst = two crossed rings.
- New particle kinds:
  - `Embers` (drifting hot sparks).
  - `HitSpark` (anime impact streaks).
  - `Flare` + `FlareStreak` (backlit bloom + anamorphic streak).
  - `Rays` (god rays).
- Hits spawn sparks (embers on heavy/counter hits); the charge sparks embers; Burst = flare + rays + embers.

**Audio**
- 8 original music loops composed in code (`make_music.py`), rendered with the GeneralUser GS SoundFont via FluidSynth,
  mastered, and written as seamless OGG loops: Bedroom, Dream, Boss, World1, Palace, Midnight (3/4 clockwork waltz),
  Galaxy, Interior. The old 13–23 s WAV placeholders were deleted.
  - Music streams (import rule `AudioImportRules`).
- 25 new SFX (`make_sfx.py`): swing ×3, heavy swing, whoosh, hit ×3, heavy hit, clank, burst, coin, door, grind loop,
  reflect, enemy "!", shock/gong, UI hover/back, pickup.
- Wired to: combo/air/rising/thrust/burst, hits, enemy notice, Brute shock ring, reflected orbs, the rail grind loop
  (pitch follows speed), menus (hover tick / back), coins, doors and the dojo gong.

**UI/UX**
- New HUD pieces: wallet, place tag inside rooms, hour compass, talk box, shop.
- The move list now tucks away after 25 s into a small "TAB ◆ ท่าต่อสู้ทั้งหมด" tab.
- The stamina bar fades when it is full.

**Tests**
- New `PalaceInteriorPlayModeTests` 7/7:
  - PI-01: every kind of door exists and every "outside" point is clear.
  - PI-02: enter flat / collect / leave, and minutes stay collected.
  - PI-03: the shop.
  - PI-04: the edge holds + fall rescue.
  - PI-05: the dojo's 3 waves + reward.
  - PI-06: the library compass + vending machines.
  - PI-07: a slash spawns + all 8 music loops and the new SFX load.
- **EditMode 48/48. PlayMode full run 82/83 → the one failure (PI-02 assumed exactly one coin collected; the magnet
  grabbed two) was fixed and PI 7/7 re-run; Palace subset 12/12 after the final rebuild.**
- World 1 rebuilt (applies the M14 exposure tweak).

**Looked at:** doors from the street, all 6 rooms, the city edge, every slash freeze-framed in slow motion, talk box +
shop + HUD.

**Not verified / known issues**
- I cannot listen to audio: music and SFX were checked by level / peak / loop-seam / spectrogram only — the user's ears
  are the real test.
- Interiors still read quite pink/purple (district halftone + StylePack tint carry inside).
- Rooms are shared per type (pocket rooms), so two cafés look the same apart from the resident.
- Not hand-played end-to-end.
- "Game UI design improvements" notes still not received.

## M16 — Level 2 city rebuilt from the user's Night City references (2026-10-20) ✅ built + tested

User (16:29, 16 Cyberpunk 2077 screenshots): "level 2 sucks — everywhere I look it's the same building; the special objects are
nothing; you stacked the same box into a wall at the edge — look at the refs and see how a game feels alive".

**What the refs teach** (no assets, logos or brand names copied — every brand is invented for the dream city)
- Silhouette variety and layers above the street matter more than building count.
- Busy street level, constant motion, a real horizon.

**Buildings** (`MidnightPalaceCity.cs`)
- Every tower is a podium of lit shops plus one of six archetypes:
  - setback tiers with lit ledges;
  - slim shaft + cantilevered crown;
  - painted megablock (tinted panels + colour bands + add-on modules);
  - round glass tower with rings;
  - residential slab with real balconies + roof palms;
  - twin towers + skybridges.
- 14% are landmark skyscrapers (60–105 m).
- Building details: tall vertical ad screens, wide ads, antenna clusters with red beacons, roof steam, skybridges across alleys.
- New texture set (`make_city_art_v2.py`):
  - Facades: glass curtain wall, concrete strip-window, balcony, industrial louvre, neutral panel (tinted ×6).
  - Storefronts with shelves/goods/shoppers.
  - 16 ad screens for invented dream brands (PILLOW COLA, NEKO RAMEN, MOON MILK, SNOOZE BANK, ZZZ HOTEL, LULLABY.FM,
    DREAM CORP, APPLE JUMP).
  - Wet road, yellow crossing, grey sidewalk.

**Layers above the street**
- A monorail loop with a 4-car train; pillars on the curb, not in the road.
- Two elevated highways with traffic; skybridges; three rings of flying cars.
- Four giant holograms: whale, dreamer idol, car, clockfolk.

**Street life**
- ~200 pedestrians (new low-poly `PED_Dreamer`, tinted) walking every sidewalk; kiosk queues; a crowd looking up at the tower.
- ~100 hover cars (`CAR_Hover`). Street cars brake for Pip instead of driving through him.
- Street furniture: yellow crossings at every junction, traffic lights, lamps, palms, food kiosks, benches/bins, steam vents.
- District signatures:
  - A turning neon ferris wheel (Candy Rooftops).
  - Giant gears turning on the walls (Clockwork Canal).
  - Lantern cables + hanging banners across the streets (Neon Alley / Canal).
  - Neon gates over the four grand avenues.

**Edge**
- The box wall was deleted; the edge is now a ring road + lit sea wall on a bay.
- Over the water: buoy lights, a lit suspension bridge with traffic, and six far districts as hazy skyline clusters with
  gaps (the invisible wall stays).

**Look**
- Less purple wash: neutral/teal moonlight, darker district fogs, saturation 26 → 8, halftone/posterize reduced.
- Lower window and ad emission, so lights read against dark buildings.
- Road specular reduced: it was producing a giant white blob in front of the camera.

**New models + runtime**
- Blender (`build_city_life.py`): CAR_Hover, PED_Dreamer, PALM_Tree, KIOSK_Food, TRAFFIC_Light, MONO_Car, BENCH_Bin.
- Runtime: PathMover, RandomTint, Hologram, SteamVent.
- Static batching flags on all city geometry.

**Tests**
- EditMode 48/48.
- PlayMode full 83/83 (after the curb-pillar fix — MP-02's bot had been stuck on monorail pillars in the street).
- Palace subset 12/12 after the final rebuild (doors still clear: kiosks keep 6 m from door exits).

**Not verified / known issues**
- Performance not profiled (≈300 movers, thousands of static boxes) — needs a check on the user's machine.
- Pedestrians and cars have no collisions (dream logic: Pip walks through people).
- Not hand-played.

## M17 — Level 2 content: puzzles, walk-in buildings, sunset, paper-cutout people (2026-10-20) ✅ built + tested

User: "level 2 has no content", "buildings: press E and warp — I want to walk straight in", "the mood feels stifling",
people should be "2D, varied, placed 2.5D". User picked: sunset, paper-cutout people, level 2 first.

**Walk-in buildings (no more warp)**
- Every door lot now has a hollow podium: floor, ceiling, open front with a 3 m doorway and windows, furnished per type
  (Cafe, Shop, Library, Flat, Dojo, Vault, Watchmaker…), sized to the room; paper customers sit inside.
- `InteriorDirector` detects rooms by bounds (no teleport). Music switches when you walk in. `BuildingDoor` no longer
  takes E; `RoomExit` removed (file left as an empty stub — delete was not permitted on the device).

**Puzzles + route (`MidnightPalaceQuests.cs`)**
- South: 3×3 neon CIRCUIT (punch tiles to rotate) → lights the blackout → a lift to the monorail station.
- Monorail: stops at 2 stations, Pip can ride on the roof (`TrainDriver` + `MovingPlatform`).
- East rooftops: spinning clock-hand fans; ring a FREEZE BELL to stop them.
- North gear tower: REWIND lever rebuilds the broken clock-slab stairs for a while.
- West fountain: push 3 gift crates onto 3 plates to break the seal → the ambush can start.
- Optional east circuit → a 10-minute cache. Sky beacons over every goal + chevron arrows on the floor.
- Objective panel is now a checklist (✓/◆) per district.

**Look**
- `SkyClock`: golden sunset → blue hour → midnight as the stolen hours come back (sky, sun, fog, ambient).
- 16 paper-cutout characters (`make_paper_people.py`): billboard cards with a paper flip and walk bob.

**Tests**: EditMode 55/55 (3 new circuit-flow tests). PlayMode PZ-01..06 (new) + palace subset passed; MP-04 updated.

**Not verified**: not hand-played; performance not profiled.

## M18 — Fists instead of the sword, Pip's new face, natural walk, monster movement, skill UI (2026-10-20) ✅ built + tested

**Fists** (user: "remove the sword, use fists")
- Leaf blade and `BladeRig` removed. New `FistRig` drives Pip's arms after the procedural animator:
  - jab (right) → cross (left) → uppercut; double-fist ground slam (charge); spinning fists in the air; uppercut launcher;
    rocket punch (dash); barrage + palm blast (DREAM BURST).
  - wind-up → snap (overshoot) → recover curve; rubber-hose arm stretch on the snap; the mitt swells on impact.
- Punch VFX: shock rings around the fist, speed-line cones, ground shockwave; comic onomatopoeia pops (POW / BAM / WHAM /
  POP, `ComicPop`, pooled billboards) on counters, finishers, slams, bursts.
- New SFX (`make_sfx_m18.py`): glove punches ×3, heavy punch ×2, bell toll, ratchet, tick. Credits updated.
- All in-game texts changed from "slash/sword" to "punch" (NPC lines, shop item, boss prompts, move list).

**Pip's face**: rebuilt in `build_apple_v4.py` — big glossy dark eyes with amber iris + 2 shines, tiny brows, wide blush,
small open smile. The phantom mask outfit is off in level 2 (it covered the eyes).

**Natural walk** (`AppleProceduralAnimator`)
- Body is highest on the passing pose and dips after each heel contact (it used to peak at contact — read as hopping).
- Smaller sway/twist/arm swing; arms trail the legs (overlap); swing foot lifts off the ground at walk speed.
- Lean into acceleration, rock back on stops (spring-damped, small settle).

**Monster movement** (`EnemyBase`)
- `Steer`: acceleration-based movement + separation, so packs fan out instead of stacking.
- `Gait`: step bob, lean from velocity/acceleration, side waddle.
- Attack tokens: only 2 enemies commit to an attack at once; the rest circle at ~4 m.
- Tick-Tock wind-up squash + lean back, stretch on take-off / squash on landing. Brute and Roller use steering; the Roller
  sidles around Pip.

**Skill UI/UX** (`SkillBar`)
- Bottom-right cards with drawn icons (`make_skill_icons.py`), name and input for each move.
- A card is dim when you can't use it, glows gold + bobs + shows a timer when the move is usable right now (counter window,
  dash-punch window, launcher after a hit, air spin while airborne), and kicks red when used.
- DREAM BURST is a diamond that fills with the meter.
- The objective panel grows with multi-line checklists instead of spilling over the SLEEP moons.

**Tests**: CK-01 rewritten (the jab extends the mitt forward, no sword in the hierarchy); CK-07 new (skill bar: 6 cards,
air spin lights in the air). PlayMode full run 103/104 (the 1 failure was the new CM-07, fixed in M19).

**Not verified**: not hand-played with a gamepad; the punch arcs are reviewed only in slowed screenshots.

## M19 — Count Midnight redesign: a boss with mechanics, not just hit + dodge (2026-10-20) ✅ built + tested

User: "the boss is dumb, you just hit and dodge". Research applied: bosses as tests of the level's lessons (Zelda / Mario
Odyssey), readable telegraphs + punish windows (Souls), crowd-turn and reaction prompts (Arkham), arena reading
(raid "stand in the safe spot" mechanics).

**WINDING-KEY ARMOUR**
- A big brass key spins on his back. While wound, punches from the front CLANG (no damage; hint toast).
- Ways to unwind:
  - punch the key from behind (he turns to face you, so you get behind him after his big swings);
  - after HOUR CLEAVE the hour hand is stuck in the dial for ~1.6 s — punch it (3 hits ≈ 1 turn);
  - freeze him with a bell.
- Fully unwound → he RUNS DOWN: kneels, dial critical, then winds himself back up (ratchet).

**ARENA BELLS (×4)**
- The same stop-the-clock bells taught on the east roofs.
- Punch one: it freezes time in a bubble — bullets vanish, and if he's inside he freezes mid-attack with his armour open.
- 11 s recharge (gold ring on the floor). The fight becomes "bait him to a bell, then ring it".

**BACKHAND**: loiter behind him for ~0.45 s and he spins with a backhand that covers all round (he guards the key).

**STOP THE CLOCK (50 %)**: three GOLDEN numerals come at Pip among the bullets — punch them back into his face. Three
reflected = the time stop ends early and he starts phase 2 dazed (run down).

**MIDNIGHT STRIKES (phase 2)**: he calls two hours. Those numerals light up gold; the dial ticks faster for 3.2 s and then
detonates everywhere else. One of the two numerals is always near Pip.

**Boss HUD**: 3 key diamonds (spin while wound) + one status line ("ต่อยกุญแจข้างหลัง · ระฆัง · เข็มที่ปัก" / "หยุดเวลา!
รัวหมัด" / "เข็มปักพื้น!" / "หมดลาน! ต่อยหน้าปัด"). The combat HUD and skill bar move up above the boss bar; no duplicate
stamina bar.

**Bug found by tests**: the crit check counted a blow from behind on the same frame he knelt → instant critical. The dial
critical now needs Pip in front and not the same frame.

**Tests**
- Boss suite 12/12: CM-02 rewritten (front clangs, back unwinds); new CM-07 run-down → critical → rewind, CM-08 bell
  freeze, CM-09 stuck hand, CM-10 golden reflect, CM-11 strikes safe pads, CM-12 backhand.
- Final run after M17–M19: EditMode 55/55, PlayMode full 104/104.

**Not verified / risks**
- Not hand-played: tuning (bell radius 7 m, stuck 1.6 s, unwind amounts) needs a real playthrough.
- The stuck hour hand is only highlighted by a pulsing glow + flares; it may need a stronger "PUNCH!" marker.
- Golden numerals must be punched while facing them (the punch arc is ±55°); lock-on helps.

## M20 — No red, comet punches, move list removed, map tidy (2026-10-20) ✅ built + tested
- UI accent red → neon teal (`UIStyle.Red` value, name kept); transition caption starburst removed.
- Comet punches (user reference): flame trails on the mitts (`FistRig`) + a flaming flying mitt on uppercut / rocket punch /
  burst (`FlyingFist`, vertex-coloured alpha flame material).
- Move-list panel + TAB tab removed (the skill bar covers it).
- Map: grand-avenue palms moved out of the roads (avenue strip is centred at 5, not 0); roads are built in segments and
  skip the gear-works canal (no road / crossings across the canal); new `Tidy` pass deletes street props and people that
  land on a road or inside a building (11 + 8 on the last build).
- Tests: PlayMode full 104/104. Not hand-played.

## Post-M20 audit — progression and detailed stylized Palace (2026-10-20)
- User selected detailed stylized exteriors AND interiors. Applied targeted pass to existing scene (50 rooms, 54 supported lantern spans, 4 compact objective markers); preserved collider count and gameplay links. No full city rebuild.
- Normal Galaxy entry to Midnight Palace requires persisted Dream_World1 clear. Added 4 EditMode and 4 PlayMode behavioural tests, including save reload, fresh-save rejection, marker lifecycle and decorative culling.
- Fresh results: RESULT Passed  passed=59 failed=0 skipped=0 inconclusive=0 duration=1.8s / RESULT Passed  passed=4 failed=0 skipped=0 inconclusive=0 duration=2.6s / RESULT Passed  passed=108 failed=0 skipped=0 inconclusive=0 duration=416.1s. Full evidence and limitations: docs/AUDIT_AFTER_M20.md.
- Editor street sample mean/p95: before 11.52/19.66 ms, first pass 16.00/25.40 ms, refined 11.41/17.49 ms. Not standalone/GPU proof; heap increased in Editor.
- Art verdict REVISE: interiors/empty lots still need composition and detailed furniture; facade repaint remains. No manual full-game win, gamepad playtest, standalone profile, Blender mesh repair or future-world implementation claimed.
- Unity MCP instances=0; existing DevBridge used. Skill updated to current Unity/Blender pipeline and evidence constraints.

### Save-isolation correction (2026-10-21)
The first post-M20 full run (108/108) was functionally green but legacy BossMidnight tests wrote into the real Editor save (last write 23:55:36). Temporary saves applied to the new fixtures, not the entire old suite. Added outer PlayModeSaveIsolation and rerunning all tests with a physical-save SHA256 guard. Snapshot after discovery preserved; no exact pre-run snapshot exists, so no guessed currency/diary/best-time rollback performed. Final rerun result follows in AUDIT_AFTER_M20.md.


## 2026-10-21 — Five-world campaign continuation (after Claude M21)
Preserved M21 city/interior builder hashes and authored furniture. Added worlds 3–5, sequential save gates, world/chapter/victory cinematics, original five-track MIDI soundtrack and ending credits, and central zero-stability wake handling. Fixed CHAPTER 0, flattened-cylinder collision, bridge entry steps, boat driver, initial camera/checkpoint occlusion and credit backdrop/text height.

Validation: World 1/Palace 32 selected gameplay cases passed in the combined run; the run had one Moon bot-position failure (40/41 total). After bot recovery and the final non-colliding art pass, all 10 campaign gameplay/presentation cases passed. Cinematic/image verification repeated after the two deprecated shader-keyword removals. No unused maps/EditMode inventory suite was run. Windows development player built from only seven active scenes. Exact raw totals/build details/limits: docs/CAMPAIGN_FIVE_REPORT.md.

Not a release-art approval or human fun/performance certification. No real-save unlocks granted; campaign baseline/final SHA256 match. Prior M20 save pollution remains documented historically.


## 2026-10-23 — Remove the card system (user request)

- Removed draw/selection, equipped-card controller and input, cooldowns, rerolls/Lucky Pillow, Dream Laws, HUD/summary, card data/assets/sounds, card builders/generators and card-only tests. Sleep now goes through Galaxy directly into the chosen world. Kept combat, movement and environmental mechanisms. Updated current design overrides and authored World 1 instructions.
- Patched the existing player prefab/scene references, without rebuilding the player or worlds. Preserved unrelated BedroomHub/Palace and other concurrent user changes. Legacy pillow save field is inert; real saves/currency/unlocks were not edited.
- Strict scene regression exposed six pre-existing World 1 puzzle nodes using an embedded MonoScript for a secondary class. Extracted CognitiveTrialNode.cs and replaced only those script references with its stable GUID; the six nodes deserialize in the passing test.
- Validation: relevant Runtime/Editor/EditMode/PlayMode assemblies compiled with 0 errors; focused card-free PlayMode **3/3 passed** (all five used worlds plus real sleep/Galaxy/world/wake), remaining bed-upgrade EditMode **4/4 passed**. Temporary-save suite verified the real Editor save SHA256 unchanged. Static audit found no references to deleted card GUIDs.
- Recorded earlier failures: first **1 passed / 2 failed**, second **2 passed / 1 failed**; final **3 passed / 0 failed**. A specific unrelated background MCP authorization error is expected by the new fixture; game errors remain failures. Unity domain-reload hang required a restart explicitly approved by the user.
- Runtime World 1 screenshot reviewed with no card HUD. This is a camera-render capture, not final-art approval. No human keyboard playthrough, standalone build/performance test, or subjective fun claim in this change.
- Details, changed systems and evidence: docs/CARD_REMOVAL_REPORT.md; local originals and hashes: DevBridge/card-removal-backup/manifest.json.

## 2026-10-23 — Isolated tiny-apple witch test

Built `Assets/MamasComing/WitchTest/Scenes/AppleWitch_Test.unity`: real-scale kitchen with a 12cm apple, one levitation wand, three manually placed props, a human-scale witch mother's arrival, generated raster UI and painted material textures. Ten authored Blender models and editable/reproducible source retained. Original Mama scene is not replaced.

Focused staged virtual-keyboard/mouse and swept-physics review: final 29/29 passed after the arrival framing correction, at `DevBridge/witch-test/review-latest.txt`. Actual Game View evidence and limits are in `docs/MAMA_WITCH_IMPLEMENTATION.md`. This is a playable single encounter, not a multi-map campaign, human playtest, standalone performance certification or release-art approval.

## 2026-10-23 — MAMA'S COMING! presentation and visual polish

Active game confirmed as MamasKitchen, separate from the DreamRush campaign and WitchTest. Removed competing always-on task/room-marker overlays; Tab now owns the house overview and H temporarily replaces it. Compact title/HUD/context and a single notification lane keep the house visible. Applied 73 rounded furniture surfaces, original Blender pot/lid models, five authored textures, cloned materials, calmer lighting and restrained fire/smoke/water presentation. Added fire-out steam and shape-coded friend labels. A three-avatar check exposed black remote characters; fixed per-material _BaseColor clothing accents while retaining facial colours. Cloth/burn tinting excludes particle renderers.

Validation: Runtime/Editor 0 compile errors (four existing warnings). Expanded staged-input review 42/42 passed; nine room camera captures at 720p, 1080p title/gameplay/fire checks, and offline three-avatar inspection. Collider settings and world transforms unchanged in the authoring pass. Preserved the user's pre-existing scene edits in the pre-edit backup. No scene rebuild or player-prefab regeneration.

Evidence and precise limits: docs/MAMA_VISUAL_POLISH.md and docs/evidence/mama-visual-polish/. This does not certify final art, human cooperative play, networking, a standalone build or target-hardware performance. Existing character rig/action animations retained.

## 2026-10-23 — Mama Recast research and asset pass

Followed the user's revised saturated cartoon brief and supplied Pinterest/house/family references. Detailed primary-source research, alternate directions, fabric/material guidance and project-specific budgets are in `docs/MAMA_ART_RECAST_RESEARCH.md`. Authored 20 original prop models (46,912 triangles across the unique library), shared palette UVs and editable Blender source; integrated the prop replacements into the existing scene (30 replacements in the final reapply, with the already-mounted router retained). Imported supplied Mother Apple Kind/Angry as the mother. Banana's derived conversion is retained but does not yet replace a playable child because its original geometry deformation still needs proper rig conversion.

Added carry contact/cradle profiles, real hollow disposal bins with a hinged lid and bag acceptance, ten licensed CC0 foley overrides, revised 108 bpm procedural music and callout ducking, warm lifetime fire gradients, and corrected the live-clone blink material bug exposed by the expanded review. Authoring preserves the existing house and rules; the upright teddy and hollow-bin colliders are documented exceptions to collider preservation.

Final staged virtual-input review **63/63 passed**, zero compile errors/four existing warnings. Includes actual E/Q interactions, hand-contact and torso-clearance samples, bin deposit, mother moods, foley loading, fire resolution, bilingual compact HUD and three offline avatars deliberately cloned during a source blink. Independent staged art views accompany normal game screenshots in `docs/evidence/mama-recast/`. Full implementation, recovery history and remaining work are in `docs/MAMA_RECAST_IMPLEMENTATION.md`. No human/online playtest or final-art/performance certification is claimed.
