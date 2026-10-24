# DREAM RUSH — audit หลัง Claude M20 และแก้ไขรอบปัจจุบัน

เริ่มตรวจ 20 ตุลาคม และทดสอบต่อถึง 21 ตุลาคม 2026 (Asia/Bangkok); โปรเจกต์ `C:/Pepes/final dojo`.

ตรวจโค้ด/เอกสารล่าสุดถึง M20, รัน Unity, ถ่ายภาพเปรียบเทียบจากกล้องคงที่ และรัน automated regression ใหม่ ผลเก่า M16 (52/91) แยกเก็บไว้ใน audit ก่อนหน้า **ไม่นำมาอ้างเป็นผลทดสอบเวอร์ชันปัจจุบัน**

**สถานะภาพ: REVISE.** รอบนี้ปรับพื้นฐานร่วมของภายใน/ภายนอกและแก้ตัวชี้/สายแขวนแล้ว แต่ยังไม่ได้ทำห้องทุกห้องและเมืองทั้งหมดให้ถึง final art ตามที่ผู้ใช้ต้องการ และไม่ได้พิสูจน์ว่าทุกด่านสนุกจาก hand-play

## สิ่งที่แก้จริง

| ID / priority | วิธีทำให้เห็นปัญหาก่อนแก้ | ก่อน | หลัง / หลักฐาน |
|---|---|---|---|
| PROG-01 / P1 | เซฟใหม่ → Galaxy → เลือก W2 → Confirm | `GalaxySelect` ตรวจเฉพาะ authored `locked=false`; เข้า Palace ได้ทันที | ใช้ `WorldAccess` ตรวจ persisted `Dream_World1.clears > 0` ทั้ง UI และ Confirm; เซฟใหม่ถูกปฏิเสธพร้อมเหตุผล; World 1 clear ที่ save/reload แล้วปลดล็อกได้ |
| NAV-01 / P2 | เปิด Palace แล้วดูเหนือเป้าหมายชั่วโมง/blackout | cylinder scale Y70 → สูง 140m เป็นเส้นยาวทั่ว skyline | 4 ตัวชี้เป็น marker สูง 4m เก็บ link `StolenHour`/`PuzzleReward`; fade ใกล้ลดเหลือ 1m และยังปิดเมื่อเป้าหมายสำเร็จ |
| NAV-02 / P2 | `HourBeacon` ที่ตั้ง XZ scale ไม่ใช่ 1 → รัน Update | pulse ทับความกว้างที่ผู้สร้างกำหนดเป็นประมาณ 1 ทุกเฟรม | pulse อิง authored base scale; test ใช้ X=.65/Z=.45 แล้วตรวจคงสัดส่วนและซ่อนหลัง collect |
| SAVE-01 / P1 | รัน legacy `BossMidnightPlayModeTests.ZeroHP_LandOnDial_Snooze` โดยไม่มี SaveManager injection | เทสต์เก่าใช้ default Editor save; full suite ครั้งแรกเขียน Test_BossMidnight result ลงเซฟจริง | เพิ่ม outer `PlayModeSaveIsolation` และรันใหม่พร้อม disk hash guard; ข้อมูลที่เขียนในครั้งแรกยังเก็บตามจริง ไม่มี baseline ให้ restore อย่างแม่นยำ |
| ART-01 / P2 | Palace → มองโคมใน Signatures ระหว่างถนน | เส้นตรงไม่มีจุดยึด; โคมอยู่ต่ำกว่าแนวเส้นโดยไม่มีสายต่อ; banners ลอย | สร้าง 54 ช่วงสายโค้ง มีเสา/ฐาน/หัวเสาและสายต่อถึงโคม; ซ่อนช่วงตกในแยก/คลองและ banner ที่ไม่มีโครงยึด; ไม่แตะ suspension bridge/grind rails |
| ART-02 / P1 / แก้บางส่วน | เดินเข้า Library/Cafe/Dojo แล้วเทียบ facade | พื้น/ผนังเรียบใหญ่, furniture ทรงพื้นฐาน, แสงนีออนกลบวัสดุ | เพิ่ม kit ไม้/กระดาษ/trim/กรอบภาพ/pendants/cups/books/runners ใน 50 ห้อง; ลด spec/rim/emission และ post bloom เฉพาะ Palace. **ยังมีห้องโล่งและ facade grunge ที่ต้อง repaint/model ต่อ** |
| PERF-01 / P2 / ตรวจพบระหว่างแก้ | ถ่าย street กล้องเดียวกันแล้วสุ่ม frame 10 วินาที | art pass รอบแรก mean 16.00ms vs ก่อน 11.52ms | รวม mesh ของตกแต่งตามห้อง/วัสดุและ cull ชุดตกแต่งไกล 32m โดยคำนึงทั้ง player/camera; final sample mean 11.41ms. ไม่ใช่ข้อสรุป release/GPU performance |

การปลดล็อกไม่ให้ใช้ยอดชนะใน diary, การชนะ `Dream_Chair`, Palace-only clear หรือ record ที่ clears=0 แทนการชนะ World 1. เซฟเก่าที่ยังไม่มีหลักฐานชนะ World 1 จะยังล็อกไว้; เงินและประวัติเดิมเก็บไว้ ไม่แก้เซฟจริงเพื่อปลดล็อกทดสอบ

## ผลทดสอบรอบใหม่

- `current-EditMode.txt`: RESULT Passed  passed=59 failed=0 skipped=0 inconclusive=0 duration=1.8s
- `current-focused-PlayMode.txt`: RESULT Passed  passed=4 failed=0 skipped=0 inconclusive=0 duration=2.6s
- `current-full-PlayMode.txt`: RESULT Passed  passed=108 failed=0 skipped=0 inconclusive=0 duration=416.1s

เคสใหม่ 4 เคสใช้ temporary saves แยกจากเซฟผู้ใช้. หลังรัน full suite ครั้งแรกพบว่า legacy boss tests บางตัวไม่ได้ inject save จึงเขียนผล `Test_BossMidnight` ลง Editor save จริง (ไฟล์เปลี่ยนเวลา 23:55:36). ไม่มี snapshot ก่อนรอบนี้สำหรับกู้ค่าทั้งหมดอย่างแม่นยำ จึงไม่เดาย้อน currency/diary/best time. เก็บไฟล์หลังพบเหตุไว้ที่ `editor-save-after-first-full-run.json`.

เพิ่ม `PlayModeSaveIsolation` เป็น outer SetUpFixture: ใช้ temporary SaveManager ครอบ PlayMode ทั้ง suite และตรวจ SHA256 ของไฟล์เซฟจริงก่อน/หลัง; อิง lifecycle ของ [Unity Test Framework](https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/test-framework-introduction/writing-tests/before-and-after-tests/reference-unitysetup-and-unityteardown). **ผลรันทดสอบหลังแก้การแยกเซฟ: Full rerun in progress; focused 4/4 passed with real-save hash unchanged.** การทดสอบปลดล็อกสร้าง clear record ในเซฟทดสอบเพื่อพิสูจน์ persistence/UI; ไม่ใช่การอ้างว่าเล่นชนะ World 1 ด้วยมือ. Full suite มี movement/camera, cards/status, chair victories, hub/bed/saves, World 1, legacy Dreamland, Palace exploration/interiors/puzzles/combat และ Count Midnight ตามชื่อเคสในผลดิบ

คอมไพล์สะอาดหลังแก้ข้อผิดพลาดใน test staging (`string.Join` overload และ encoding ของไฟล์ local); เก็บ log เต็มไว้ ไม่ปิดบังรอบแก้. Visual pass ตรวจ collider count ก่อน/หลังและไม่ยอม save หากจำนวนเปลี่ยน. เรียก pass ซ้ำกับฉากเดิมได้โดย replace เฉพาะ `StyleDressing`; ครั้งสองไม่สร้างเสา/สายซ้ำ (0 ช่วงใหม่)

การวิเคราะห์ before ของ PROG-01/NAV-02 มาจากโค้ดเดิม; ART/NAV-01 มาจากโค้ดและภาพที่ผู้ใช้ส่ง/ภาพก่อนแก้ ไม่ได้อ้างว่ามี red test ของเคสใหม่ก่อน patch

## Before / after

- Library: `fixed-before-Library.png` → `final-after-Library.png`
- Cafe: `fixed-before-Cafe.png` → `final-after-Cafe.png`
- Dojo: `fixed-before-Dojo.png` → `final-after-Dojo.png`
- ถนน: `fixed-before-street.png` → `final-after-street.png`
- ตรวจภาพ after เพิ่ม Flat, Watchmaker, Vault อีกสามประเภท
- ภาพยืนยันสายโคมมีโครงยึด: `final-supported-lanterns.png`; ตัวชี้ชั่วโมงขนาดใหม่: `final-compact-objective.png`

ภาพ 1280×720 จาก camera render ของเกม; Editor render target ตอนเก็บเวลา 1920×1080. DevBridge `uishot` นำ overlay UI เข้า camera/postprocessing จึงเห็น chromatic split ในภาพที่อาจไม่ตรง GameView HUD จริง ไม่ใช้ภาพนี้อย่างเดียวฟันธงว่าฟอนต์จริงเบลอ. การย้ายกล้อง/teleport เพื่อ review ไม่ได้ยืนยันการเดินผ่านทั้งด่าน

## Performance ที่วัดได้

10-second Editor wall-frame samples; ไม่ใช่ GPU time และไม่ได้ควบคุม foreground/throttling/โหลดแอปอื่น

| sample | n | mean ms | p50 ms | p95 ms | managed heap MB |
|---|---:|---:|---:|---:|---:|
| ก่อน art pass | 869 | 11.52 | 10.19 | 19.66 | 633.8 |
| รอบแรก (ตรวจพบต้นทุนเพิ่ม) | 626 | 16.00 | 15.11 | 25.40 | 652.9 |
| หลังรวม mesh/cull/ปรับไฟ | 877 | 11.41 | 10.53 | 17.49 | 668.8 |

หนึ่ง sample ต่อสถานะไม่เพียงพออ้าง FPS ดีขึ้น. Heap หลังสูงขึ้น ~35MB ใน Editor; ต้อง profile standalone และการเปลี่ยนฉาก/GC เพิ่ม ไม่ประกาศว่า memory ดีขึ้น

## ไฟล์ที่เปลี่ยน

โค้ด 11 ไฟล์ในรอบนี้:
- Runtime/Flow: `GalaxySelect.cs`, `WorldAccess.cs` (ใหม่)
- Runtime/Palace: `Puzzles/HourBeacon.cs`, `PalaceDressingVisibility.cs` (ใหม่)
- Editor: `MidnightPalaceBuilder.cs`, `MidnightPalaceQuests.cs`, `PalaceStylePass.cs` (ใหม่), `PalaceReviewCommands.cs` (ใหม่)
- Tests: `EditMode/WorldAccessTests.cs`, `PlayMode/WorldAccessPlayTests.cs`, `PlayMode/PlayModeSaveIsolation.cs` (ใหม่)

ข้อมูลฉาก/ภาพ: `Dream_MidnightPalace.unity`, `PP_MidnightPalace.asset`, Palace materials ที่ระบุใน `changed-files.csv`; เพิ่ม 13 style materials, procedural wood/paper textures 2 ไฟล์ และ combined decorative mesh assets 282 ไฟล์ (+ Unity metadata). ใช้ shader/asset pipeline เดิม ไม่ได้สร้างภาพ AI หรือโมเดล Blender ใหม่

อัปเดต skill เฉพาะโปรเจกต์ `.claude/skills/dream-rush-audit/SKILL.md` และแก้ `CLAUDE.md` ที่ยังอ้าง scope P0 อย่างเดียว. เพิ่ม current campaign status, art review, bug report และบันทึก milestone/asset verdict/decision ตามงานจริง

`changed-files.csv` รวม SHA256 ของโค้ด/scene/material/asset ที่เปลี่ยนในรอบนี้; `source-before-and-after.zip` สำหรับ review source; `before-art-rollback.zip` เก็บ scene/material/profile ก่อน visual pass. ไม่ reset repository และไม่ rebuild เมืองทับงานของ Claude

## งานค้างที่ยังเปิด

| priority | ทำซ้ำ/สังเกต | งานถัดไป |
|---|---|---|
| P1 | Library/Watchmaker ใหญ่แต่เฟอร์นิเจอร์ชิดผนังและพื้นที่กลางว่างมาก | เปลี่ยน composition และเพิ่ม reading/workshop vignettes มีหน้าที่; silhouette เฟอร์นิเจอร์มีรายละเอียดจริง; อย่าแก้ด้วยสุ่ม props |
| P1 | ภายนอกยังใช้ procedural facade แหล่งเดิม ส่วนภายในเป็น kit ใหม่ | repaint facade ให้ illustrated matte, ยืนยัน palette/texel density และผิวงานทั้งสองฝั่งจาก gameplay camera |
| P1 | เขตถนน/plaza/reserved lots บางจุดโล่ง | ผูก landmark/activity กับเส้นทางหลัก/ทางเลือก; วัด time-to-next-interest ใน playthrough ก่อนเติม; ห้ามอุด fan run, canal, road lanes, stairs หรือ dojo |
| P1 | หลายด่านยังเป็นชื่อในเมนู ไม่มี scene | ดู `STATUS-AND-REMAINING.md`; สรุป GDD future bosses เทียบ placeholders แล้วสร้าง vertical slice โลกถัดไป ไม่ได้สร้างโลกเหล่านี้ในรอบนี้ |
| P1 | ยังไม่มี session คนเล่นเริ่มต้นจนชนะทั้งสองโลก | เล่น keyboard/gamepad ต่อเนื่อง, เก็บ death/retry/route/confusion/enjoyment; จูน boss punish windows/novelty/replay ตามผล |
| P2 | Console เตือนลดความละเอียด shadow maps เพื่อใส่ 2048 atlas | profile lights/shadows ใน standalone ก่อนปรับ budget; ไม่เพิ่ม atlas บนสมมติฐานว่าแก้ทุกเครื่อง |
| P2 | EditorBuildSettings ยังเปิด SampleScene และ Test scenes เพื่อทดสอบ | เตรียม production build scene list ก่อน release; ไม่มี standalone release build ในรอบนี้ |
| P2 | import log พบ self-intersecting polygon บน `CM_Blade_Hour` | ตรวจและแก้ mesh ต้นทางใน Blender แล้ว reimport/verify; ยังไม่ได้ทำในรอบนี้ |
| P2 | capture ภายในยังติดสีชมพู/ม่วงและ highlight บางพื้นที่แรง | ตรวจ actual GameView exposure/light overlap/sky states; art verdict ยัง REVISE |

## MCP และขอบเขตการเข้าถึง

Unity MCP server มีอยู่ แต่ `mcpforunity://instances` ตอบ `instance_count: 0`; จึงไม่อ้างว่าเชื่อม Editor ผ่าน MCP แล้ว ใช้ DevBridge ที่ติดตั้งในโปรเจกต์จริงเพื่อ compile/play/capture/tests. ไม่ติดตั้ง connector, Unity package หรือ Blender add-on ใหม่. ไม่มี keyboard/gamepad hand-play, standalone build, GPU profile หรือ external human playtest ในรอบนี้


