# DREAM RUSH — แคมเปญห้าโลก / 2026-10-21

โปรเจกต์จริง: `C:\Pepes\final dojo` — Unity/URP, C#, FBX จากสายงาน Blender เดิม

สถานะ: แคมเปญห้าโลกมีเนื้อหาและทางจบในเกมแล้ว ผลตรวจสุดท้ายอยู่ด้านล่าง นี่เป็น playable candidate; ยังไม่ใช่การรับรอง final art, ความสนุกโดยผู้เล่นใหม่ หรือประสิทธิภาพเครื่องเป้าหมาย

## Claude ทำถึงไหนก่อนเริ่มรอบนี้

พบงาน M21 ต่อจาก M20: จำกัดพื้นที่ห้องเข้าถึงได้ราว 20×16 เมตร, ใช้ชุดเฟอร์นิเจอร์ `INT_*.fbx`, เพิ่มของตกแต่ง dojo/vault, street dressing (planter/ร้านรถเข็น/ป้ายข่าว/ที่จอดจักรยาน/ป้ายรถเมล์/รถจอด/แปลงดอกไม้และม้านั่ง) และปรับ facade ให้ไปทาง illustrated มากขึ้น ตัวฉาก Palace มีเฟอร์นิเจอร์เหล่านี้จริง ไม่ได้ดูจากคอมเมนต์อย่างเดียว งานเมือง/ห้องของ M21 ถูกเก็บไว้; รอบนี้เพิ่ม arrival แบบเจาะจงในฉากเดิม ไม่ rebuild เมืองทั้งฉาก

ก่อนรอบนี้ Galaxy มีสองโลกที่มีเป้าหมายฉาก และสาม placeholder งานรอบนี้เติมสามโลกตามชื่อที่อยู่ใน Galaxy เดิม ไม่ได้อ้างว่าไอเดียบอสอนาคตทั้งหมดใน GDD §32 สร้างเสร็จแล้ว

## สิ่งที่เล่นได้

| โลก | วงจรเล่น / ทางจบ | สิ่งที่เพิ่มหรือแก้ |
|---|---|---|
| 1 — The Dream Book | บทหนังสือห้าบท, เบาะดีด, portal, chase, บอสเก้าอี้ / ขึ้นไปนั่งจริง | แก้ป้าย CHAPTER 0 → 1, ช็อตเข้าบทและชนะ, รักษาการเคลื่อนไหวเดิม |
| 2 — Midnight Palace | ถนน/ราง, ห้อง, dojo, ปริศนาสามชั่วโมง, Count Midnight | เก็บ M21, เพิ่มช็อตเข้า/ชนะ, ตรวจ encounter ในฉาก Palace จริง |
| 3 — Toy Box Moon | เบาะส่งตัว, เกาะของเล่น/วงโคจร, ทำนองสามจุด, ตอบจังหวะสุดท้าย, พัก | ฉากและ director ใหม่, จุดพัก/ดาว/ทางเลือก |
| 4 — Bubble Bath Sea | หมุนวาล์วให้ตรง, เปิดสะพาน/ทางดีด, ขึ้นเรือข้ามคลื่นไปประภาคาร, เข้าฝั่ง | เรือพาตัวละครจริง, คลื่นมีสัญญาณเตือน, ทางลัดเปิดหลังแก้กลไก |
| 5 — Midnight Teacup | ระฆังตามลำดับ/จังหวะ, สะพานขึ้นโต๊ะสุดท้าย, last knock, พัก | ฉากจบและเครดิตต้นฉบับ, เพลงเครดิต, เร่งข้อความ/ข้าม/กลับห้องนอน |

Galaxy เปิดตาม clear record ที่เซฟไว้ของโลกก่อนหน้าทั้งลำดับ เซฟจริงไม่ได้ถูกเพิ่มชัยชนะเพื่อเปิดโลกทดสอบ ทางเริ่มฉากตรงใน Editor ยังใช้ QA ได้ ดาวและสถิติเวลา/ดาวสูงสุดของแต่ละโลกเป็นเป้าหมายเล่นซ้ำ ไม่บังคับเก็บครบเพื่อผ่าน

> **อัปเดต 2026-10-23:** ระบบการ์ดทั้งหมด (เลือกการ์ด, การ์ดที่ถือ, Dream Law, reroll/Lucky Pillow) ถูกนำออกตามคำขอผู้ใช้ เลือกโลกใน Galaxy แล้วเข้าโลกนั้นตรง ๆ ทุกโลกผ่านด้วยการเดิน กลไกในฉาก การต่อสู้ และการนั่ง/พักด้วยตนเอง ผลตรวจด้านล่างรันก่อนการนำการ์ดออก ดูผลตรวจหลังนำออกใน CARD_REMOVAL_REPORT.md

ผู้เล่นต้องเดิน ใช้กลไก และพักด้วยตนเอง ไม่มีปุ่มชนะ

## ผลตรวจที่ทำจริง

- รอบรวมหลังแก้พลังศูนย์: `RESULT Failed  passed=40 failed=1 skipped=0 inconclusive=0 duration=587.7s` — World 1/Palace **32/32 ผ่าน**; ตัวทดสอบ Moon ไหลออกนอกระยะ E หนึ่งเคส เก็บไฟล์ fail ไว้
- หลังแก้การประคองตำแหน่งบอตและเพิ่มรายละเอียดสามโลก: `RESULT Passed  passed=10 failed=0 skipped=0 inconclusive=0 duration=207.8s` — **10/10 ผ่าน**, รวมเส้นทาง 3–5/เครดิต/พลังศูนย์/ปลดล็อก/คัตซีนจริง
- หลังเก็บ keyword shader เก่า: `RESULT Passed  passed=2 failed=0 skipped=0 inconclusive=0 duration=57.1s` — ตรวจภาพและคืนการควบคุมของ intro ทั้งห้าโลกและบท 2–5 ของ World 1 ซ้ำ พร้อมตรวจเพลงทั้งห้าเล่นจริงและ MusicTime เดิน (แยกจากการรับรองคุณภาพด้วยการฟัง)
- ครอบคลุม **42 เคสเกมเพล/คัตซีน** จากผล 32 + 10 ข้างต้น และเพิ่ม **1 เคสเสียงเล่นจริง** ในรอบ focused สองเคส (อีกหนึ่งเป็นการตรวจคัตซีนซ้ำ) ไม่ได้อ้างว่ารันทั้งหมดผ่านในการรันครั้งเดียว
- ภาพ/ผลดิบ: `all-five-used-gameplay-final-tests.txt`, `campaign-ten-final-after-detail-tests.txt`, `all-world-cinematics-after-shader-cleanup-tests.txt`, `VISUAL-EVIDENCE.md`
- Compile ของ Runtime/Editor/Tests ไม่มี C# error ในรอบสุดท้าย; build ตรวจ shader อีกครั้ง ผลจริงอยู่ใน windows-build-result.txt

รอบนี้ไม่รัน EditMode, Dream_Openworld, Test_MovementGym, Test_BossMidnight หรือ suite ตรวจ inventory ของแมพที่ไม่ได้ใช้ ตามคำสั่งผู้ใช้

แยกประเภทหลักฐาน:

- ด่าน 3–5: บอตขยับผ่าน PlayerCommand เดินจริง ใช้เบาะจริง ขึ้นเรือจริง และเลือกผ่าน InteractionSystem ไม่ teleport / ไม่เรียก win เพื่อผ่านเส้นทาง
- ด่านแรก/Palace: ทดสอบพฤติกรรมในฉากใช้งานจริง บางเคสจัดตำแหน่งหรือใช้ dev state เพื่อแยก encounter จึงไม่ใช่หลักฐานเล่นต่อเนื่องตั้งแต่ต้นจนจบโดยผู้เล่นใหม่
- Palace boss: ทดสอบท่าต่อสู้/หลบ/ระฆัง/สะท้อน/จบ encounter ใน Palace จริง; เคส phase transition มี health buffer 12 เพื่อไม่ให้การยืนดู bullet pattern จบเคสด้วยความตาย ไม่ใช่การอ้างว่าผู้เล่นเลือดปกติยืนเฉย ๆ แล้วรอด
- คัตซีน: ตรวจ skip, คืน input/FOV/controller/HUD และทบทวนภาพจาก runtime; ไม่ใช่ภาพ concept ที่สร้างแยก
- รูป `uishot` ใช้ camera render ที่นำ overlay UI เข้า camera/postprocessing อาจเกิดเส้นขอบหรือ chromatic split ต่างจาก GameView จริง จึงไม่ใช้รูปอย่างเดียวรับรองคุณภาพ HUD
- ไม่มี human keyboard playthrough หรือ blind playtest ใหม่ในรอบนี้ และไม่ได้รับรองความสนุกจากบอต

## Bug audit / ก่อน → หลัง

| ID / Priority | ทำซ้ำ / ก่อนแก้ | แก้ / หลักฐานหลัง |
|---|---|---|
| CF-01 / P1 | โลก 3 เดินเข้าเบาะแรก; ยืนลอยสูงราว y=6.91 ทั้งที่พื้นแสดงต่ำ โลก 4–5 ติดขอบพื้นวงกลม | ตัว Cylinder ใหม่ใช้ CapsuleCollider ที่ป่องเมื่อบีบแบน เปลี่ยนเป็น MeshCollider ตรงรูปทรง; บอตผ่านเส้นทางจริง |
| CF-02 / P1 | โลก 5 เดินขึ้นเกาะที่สอง ติดแถว (-10.53,2.44,39.81) | ทางเชื่อมยกถึงระดับเกาะก่อนชนขอบ และใช้ชิ้นลาดต่อเนื่องแทนขั้นที่สูงเกินก้าว |
| CF-03 / P1 | ตรวจโค้ดเรือ: ส่ง ExternalTarget แต่ MovingPlatform.Update ไม่อ่านช่องนี้ | ให้ director ขยับเรือจริง ระบบ carry เดิมพาตัวละคร; หลังแก้ทดสอบนั่งเรือถึงท่าได้ (ไม่มี runtime before-failure ที่ไปถึงเรือในรอบแรก) |
| CF-04 / P2 | เข้า World 1 ป้ายแสดง CHAPTER 0; เคสก่อนแก้ fail จริง | แก้เลขแสดงผลเริ่ม 1 มีผล before/after แยกไฟล์ |
| CF-05 / P1 | โลก Moon เดินตกซ้ำจนพลังศูนย์ ไม่ตื่นกลับห้องนอน; natural-fall before fail จริง | GameFlow ตรวจพลังศูนย์ทุกโลก ไม่ผูกกับ BossEncounter เก้าอี้; ป้องกันพักชนะเมื่อหมดพลัง ตรวจไม่เพิ่ม clear และเสียฝันครั้งเดียว |
| CF-06 / P2 | กล้องเริ่มติดภาพประตู และ cushion checkpoint บัง Pip จากภาพ runtime | ย้ายประตูออกหลังกล้องและทำ cushion visual ให้แบน คง collider/gameplay ของ checkpoint |
| CF-07 / P2 | มี objective ว่างเดิมพร้อม panel ใหม่ซ้ำ | ใช้ DreamHUD/DreamSection เดิมให้มี title/objective จริง แยกตัวบอกจังหวะ และซ่อน HUD ระหว่างคัตซีน |
| CF-08 / P2 | เครดิต render มีเส้นขอบฉากทะลุพื้นหลัง และใช้ความสูงข้อความตายตัว | กล้อง UI-only/พัก StylePack ชั่วคราว วัดความสูงข้อความจริง คืน state หลังจบ/ขัดจังหวะ |
| CF-09 / P3 | กล้อง cinematic จับ root ใต้ landmark สูง / tempo ค้างหลัง finale | จับ bounds ของภาพ landmark และคืน BPM โลกเมื่อเพลงสงบกลับมา |

CF-01/02 และ visual authoring ของสามโลกใหม่เป็นข้อผิดพลาดในงานเพิ่มรอบนี้ ไม่ได้โยนเป็นบั๊กเดิมของ Claude

การแก้ชุดทดสอบแยกจากบั๊กเกม: บอตเคยพยายามเดินทะลุ pedestal, หยุดไกลเกิน range E และวิ่งเต็มแรงจนไหลเลยระฆัง จึงเพิ่มทางอ้อม/การชะลอ ไม่ลดเงื่อนไขชนะ; แก้ synthetic input เมื่อ Editor ไม่ได้ focus, ส่งสถานะปุ่มค้างต่อเนื่อง และล้าง transition ระหว่างเคส เก็บผล fail เดิมไว้ ไม่เปลี่ยนชื่อเป็น pass

เพิ่มรายละเอียดของสามโลกใหม่หลังได้ทางเล่น: รอยเย็บ/หมุดขอบฐาน, ขากล่องดนตรี/รถไฟของเล่น/ชั้นหนังสือ, กระเบื้องและก้อนสบู่ท่าเรือ, ตัวถ้วยเซรามิก/หูจับ/โคมไฟ/ชุดน้ำชาบนโต๊ะ ของตกแต่งชุดนี้ไม่มี collider และปิดเงาที่ไม่จำเป็น เพื่อรักษาทางเล่นเดิม แล้วเล่นเส้นทางซ้ำหลังแต่ง

มี independent cinematic timeout หนึ่งครั้งหลัง import shader; diagnostic rerun ด้วย timeout เดิมผ่าน จากนั้นเพิ่มการล้าง modal ตั้งต้นใน fixture และรัน cinematic/audio สองเคสผ่าน เก็บผล fail เดิมไว้ใน all-world-cinematics-independent-before-tests.txt ไม่ยืนยันสาเหตุเฉพาะของการ timeout ครั้งนั้น และไม่เพิ่มเวลาเพื่อทำให้ผ่าน

## เพลงและคัตซีน

เพิ่มเพลงต้นฉบับห้าเพลง รวมประมาณ 6 นาที 43 วินาที: ToyBoxMoon 84 BPM, BubbleBathSea 108, MidnightTeacup 92 (3/4), DreamFinale 112, Credits 70 มี MIDI และสคริปต์ทำซ้ำใน ArtSource/Audio

ใช้เครื่องดนตรีจาก [GeneralUser GS](https://github.com/mrbumpy409/GeneralUser-GS) ของ [S. Christian Collins](https://www.schristiancollins.com/generaluser.php) ผ่าน FluidSynth แนบ license และเพิ่ม AUDIO_CREDITS ก่อนนำเข้า WAV; Unity import เป็น streaming Vorbis quality 0.6 ตามกฎเดิม ไม่มีตัวอย่างเสียง/บทกวี/เพลง Minecraft ที่คัดลอกมา

`music-render-metrics.json`: RMS เป้าหมาย −19 / −17.5 / −19 / −16.5 / −20 dBFS, ไม่มี clipped samples ใน master การวัดนี้ไม่ได้แปลว่ามีคนฟังรับรอง mix แล้ว ดู `SOUNDTRACK.md` และ `soundtrack-preview.wav`

คัตซีนมีการเคลื่อนกล้อง/FOV, caption, letterbox, เสียงและ impact cue ที่เคารพลดแสงกระพริบ/ลดสั่น ข้ามได้ เครดิตเป็นข้อความไทยต้นฉบับบนท้องฟ้าดาว เลื่อนอัตโนมัติ เร่งได้ และกลับบ้านได้

## ไฟล์ / เครื่องมือ / การรักษาข้อมูล

ดู `changed-files.csv` สำหรับรายการและ SHA256 แยกไฟล์เพิ่ม/แก้เดิม/ไฟล์ที่ generator แตะหรือ importer ตรวจ และ metadata ที่แนบเพื่อเก็บ GUID (ไม่ใช่ทุกแถวมีหลักฐานว่า byte เปลี่ยน) และ `source-before-campaign-five.zip` สำหรับ source baseline M21

หมวดหลัก: Runtime/Campaign, WorldAccess/GalaxySelect/GameFlowManager, ChapterDirector, AudioManager, CampaignWorldBuilder/ReviewCommands, สามฉากใหม่/การเชื่อม Galaxy/arrival ใน Palace, เพลง+MIDI+license, gameplay tests และ .claude/skills/dream-rush-audit/SKILL.md

Builder รอบแรกเรียกตัวสร้าง player prefab ซ้ำโดยไม่จำเป็น รวมถึง AppleCharacterSetup ที่ตั้งค่า material ตัวละครและ importer อีกครั้ง จากนั้นแก้เป็น reuse prefab เดิมแล้ว ไม่มี hash ก่อนรอบนี้ของ prefab/material/importer กลุ่มนั้น จึงไม่อ้างว่า byte-identical และไม่เดา rollback; mesh FBX ไม่ได้ถูกเขียนใหม่ ทดสอบ gameplay ด่านแรกซ้ำและตรวจภาพหลังจากนั้น ส่วน source เมือง/ห้อง M21 ตรวจเทียบ hash แยกไว้

เซฟจริงเทียบกับสำเนาก่อนรอบห้าโลก: **SHA256 ตรงกัน** (`5b5ae584b7f9d2a97a2bd582249982a4b0bffa82b0bda83aceee998f7e47de85`); ทุก run มีผล before/after แยกไว้ใน `*-save-hash.txt` และ suite guard ตรวจซ้ำ ดู campaign-real-save-proof.json

source ของ M21 สี่ไฟล์ข้างต้น **ตรงกับ baseline ทุกไฟล์** ดู m21-preservation-proof.json มี backup ฉาก Palace ก่อนเพิ่ม arrival ใน DevBridge/out และสำเนาที่แนบในหลักฐาน

บันทึก M20 ก่อนหน้านี้เคยพบ legacy suite เขียนเซฟจริงและแก้การแยกเซฟแล้ว รายงานเก่าเก็บเหตุการณ์ไว้ รอบนี้มี baseline จริงก่อนเริ่มและตรวจแฮชจนจบ จึงไม่อ้างว่าเซฟไม่เคยถูกแตะในประวัติทั้งหมด

Unity MCP server มีเครื่องมือให้เรียก แต่ `mcpforunity://instances` ตอบ instance_count=0; ไม่อ้างว่าเชื่อม Editor สำเร็จ การทำงานใน Editor ใช้ DevBridge ที่มีอยู่จริง ไม่มีการติดตั้ง cloud generation plugin หรือใช้ API key ใหม่

## Build / Performance

Windows development player สร้างสำเร็จจาก BedroomHub + DreamGalaxy + ห้าโลกเท่านั้น ไม่มี legacy/test scenes ใน player ที่แจก

```text
result=Succeeded
errors=0
warnings=0
bytes=207215550
seconds=48.9
executable=C:/Users/User/Documents/Codex/2026-10-20/referenced-chatgpt-conversation-this-is-an/outputs/campaign-five/Windows-Development/DREAM_RUSH.exe
scenes=Assets/DreamRush/Scenes/BedroomHub.unity,Assets/DreamRush/Scenes/DreamGalaxy.unity,Assets/DreamRush/Scenes/Dream_World1.unity,Assets/DreamRush/Scenes/Dream_MidnightPalace.unity,Assets/DreamRush/Scenes/Dream_ToyBoxMoon.unity,Assets/DreamRush/Scenes/Dream_BubbleBathSea.unity,Assets/DreamRush/Scenes/Dream_MidnightTeacup.unity
```

บิลด์นี้ยังไม่ได้เปิดเล่นแบบ standalone ด้วยมือ ผล gameplay ข้างต้นรันใน Unity Editor ไม่ใช้ build success แทนหลักฐานเล่นจริง
ตรวจเปิดโปรเซส standalone แบบ headless 12 วินาทีโดยไม่ส่ง input: โปรเซสยังอยู่ ไม่มี exception ที่ตรวจพบ แล้วหยุดเฉพาะโปรเซสที่สร้าง เซฟจริง SHA256 ตรงกัน ดู standalone-boot-result.json / standalone-headless-boot.log นี่ไม่ใช่การตรวจภาพ GPU หรือเล่นเกมเพลใน standalone

แก้คำเตือน `_FORWARD_PLUS` โดยนำเฉพาะ keyword เก่าออกจาก DreamToon/DreamTerrain ตาม Lit.shader ของ URP ที่ติดตั้งอยู่ เก็บ shader เดิมไว้ใน shaders-before-keyword-cleanup.zip แล้วตรวจภาพและ build ใหม่ ไม่มีการเปลี่ยนค่ารูปลักษณ์ของวัสดุ

เพลงใช้ streaming; วัสดุ/props ใช้ของที่มีอยู่ และเก็บระบบ culling ของ Palace เดิม ผล frame time M20 ก่อนหน้าเป็นเพียง unfocused Editor sample ไม่ใช่ benchmark ของแคมเปญใหม่ ไม่มีผล GPU/frame-time ของเครื่องเป้าหมายหรือผลเล่น standalone ใหม่ที่จะใช้อ้าง FPS

## สิ่งที่ยังต้องทำก่อนเรียก finished release

1. Human playthrough ต่อเนื่องของทั้งห้าโลก โดยเฉพาะ World 1/Palace ที่รอบนี้ตรวจเป็นส่วน ๆ และ playtest ผู้เล่นใหม่เพื่อยืนยันความสนุก/ความชัดของเป้าหมาย
2. Art review ยังเป็น REVISE: ชุดสี/วัสดุเป็น stylized ร่วมกันแล้ว แต่สามโลกใหม่ยังมี silhouette/พื้นที่โล่งและรายละเอียดระดับ playable blockout บางส่วน ยังไม่ใช่งานภาพสุดท้ายตาม brief; เมือง M21 ต้องตรวจทุกมุม/ห้องจริงเพิ่มเติม
3. ฟังและปรับ music/SFX mix บนลำโพง/หูฟังจริง, กรอกชื่อทีมเครดิตจริงตามที่ต้องการ และตรวจขนาดหน้าจอ/คอนโทรลเลอร์ที่ใช้จริง
4. Profile build บนเครื่องเป้าหมายและแก้คอขวดตามข้อมูล โดยเฉพาะเมือง/ห้อง/เงา Palace; console มีข้อความลดความละเอียด shadow atlas ใน Editor ไม่ใช่ crash
5. ไอเดียโลก/บอสอื่นใน GDD เดิมเป็น backlog แยกจากแคมเปญห้าโลกนี้

ไม่ได้ใช้เลข milestone เป็นหลักฐานว่า art ผ่านหรือเล่นด้วยมือแล้ว
