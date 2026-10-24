# Research Log
รูปแบบตาม GDD §33 · บันทึกเฉพาะแหล่งที่ **อ่านจริง** ในเซสชันนั้น — ถ้าเข้าไม่ได้ให้เขียนว่าเข้าไม่ได้

---

## R-001 — 3D jump readability (shadow / depth)
- **Design Question:** ผู้เล่นจะกะจุดลงในโลก 3D ได้อย่างไรโดยไม่ต้องหมุนกล้องบ่อย
- **Sources Consulted:** Iwata Asks — Super Mario 3D Land Vol.1 ch.3 (https://iwataasks.nintendo.com/interviews/3ds/super-mario-3d-land/0/3) — อ่านผ่าน WebFetch 2026-10-16
- **What the Source Actually Supports:** ทีมใช้เงามาตรฐานใต้วัตถุลอยและเงาผู้เล่นตกลงบนศัตรูเพื่อช่วยเล็ง; ลดรายละเอียดบนจอเพื่อความสบายตา; เกมต้องอ่านได้แม้ปิด 3D
- **Proposed Application:** `BlobShadow` ตามพื้นผิวใต้ผู้เล่นเสมอ (GDD §9.7) + เงาใต้ของลอย (การ์ด/เป้าหมาย) + เก้าอี้มีเงาชัด; ลดของรกรอบจุดกระโดด
- **Trade-offs:** blob shadow ซ้อนกับเงา realtime ของ directional light → ต้องคุมความเข้ม/อาจปิด cast shadow ของตัวละครในบางมุม
- **Prototype Test:** MOV-12 + manual ใน Movement Gym (gap/step stations)
- **Decision:** ใช้ blob shadow + realtime shadow ของฉาก (ตัวละครยัง cast shadow ได้ ปรับภายหลังตาม playtest)
- **Date:** 2026-10-16

## R-002 — Four-step level structure
- **Design Question:** จะเรียง tutorial → boss ให้ผู้เล่นค้นพบเองได้อย่างไร
- **Sources Consulted:** Game Developer — "The secret to Mario level design" (https://www.gamedeveloper.com/design/the-secret-to-i-mario-i-level-design) — อ่าน 2026-10-16. GDC talk "Thinking in 3D" **ยังไม่ได้ดู** (ต้องมีบัญชี/ไม่ได้เข้าถึง)
- **What the Source Actually Supports:** Hayashida เทียบกับการ์ตูน 4 ช่อง (kishōtenketsu): Introduce → Develop → Twist (คิดใหม่) → Conclude (โชว์ความชำนาญ); มี core concept ชัดตั้งแต่ต้นจนจบด่าน
- **Proposed Application:** Core concept ของด่าน = "เฟอร์นิเจอร์มีชีวิตหนีเมื่อเข้าใกล้" — Intro: เห็นเก้าอี้ไกล ๆ/เฟอร์นิเจอร์ขยับ; Develop: Little Stool หนีแบบเส้นทางสั้นคาดเดาได้; Twist: บอสเก้าอี้หนีฉลาด + เตะนิ้วก้อย + การ์ดข้อห้าม; Conclude: รวมทุกอย่างเพื่อนั่งให้ได้
- **Trade-offs:** ถ้า Develop สั้นไป ผู้เล่นไม่เข้าใจว่าต้อง "ต้อน" ไม่ใช่ "วิ่งตาม"
- **Prototype Test:** M4 playtest metric "เวลาที่เข้าใจ objective"
- **Decision:** ใช้ตาม GDD §2.2/§13
- **Date:** 2026-10-16

## R-003 — Jump implementation (gravity, variable jump, coyote, buffer)
- **Design Question:** jump ที่ "แน่นอน + ให้อภัย" ทำอย่างไร
- **Sources Consulted:** GMTK Platformer Toolkit — "Behind the Code" devlog (https://gmtk.itch.io/platformer-toolkit/devlog/395523/behind-the-code) — อ่าน 2026-10-16; ผลค้นหาอื่น (uhiyama-lab CharacterController notes, bugnet isGrounded flicker) เห็นเฉพาะหัวข้อ ยังไม่ได้อ่านเนื้อหา
- **What the Source Actually Supports:** คำนวณความเร็วเริ่มจาก jump height (`v=√(2gh)`); gravity multiplier ตอนตก; jump cut = เปลี่ยนเป็น gravity มากขึ้นเมื่อปล่อยปุ่มขณะขึ้น; coyote counter ระหว่างลอยแบบไม่ได้กระโดด (~0.2s ในตัวอย่าง); buffer เก็บคำขอกระโดดชั่วคราว. ความเห็นท้ายบทความชี้บั๊ก "super jump" เมื่อ multiplier ไม่ถูก reset และความเร็วเริ่มควรคิดจาก gravity พื้นฐาน
- **Proposed Application:** `JumpMath.InitialVelocity(h, g_base)` เสมอ (ไม่ใช้ multiplier ปัจจุบัน); `JumpAssist` แยก coyote/buffer เป็น pure C#; coyote ใช้ 0.12s ตาม GDD (ไม่ใช่ 0.2) — ทดลองปรับใน playtest; reset สถานะ jump-cut ทุกครั้งที่ jump ใหม่
- **Trade-offs:** coyote ยาวเกิน = รู้สึก "ลอย"; ใช้ตัวเลข GDD ก่อน
- **Prototype Test:** MOV-01..07
- **Decision:** ใช้ทั้งหมด + test กันบั๊ก super jump (buffered jump ตอนลงพื้นต้องสูงเท่าปกติ)
- **Date:** 2026-10-16

## R-004 — CharacterController vs Rigidbody (Unity Engineering)
- **Design Question:** motor แบบไหนเหมาะกับ platformer ของเรา
- **Sources Consulted:** ประสบการณ์/เอกสาร Unity CharacterController (ยังไม่ได้เปิดหน้า docs ในเซสชันนี้) — ต้องยืนยันด้วย prototype
- **What we must verify in-engine:** `isGrounded` กระพริบตอนลงทางลาด → ต้องมี ground snap; การชนเพดาน (`CollisionFlags.Above`); ความเร็วสะสมเมื่อดันผนัง
- **Decision:** CharacterController + snap + reconcile velocity หลัง Move — ยืนยันด้วย MOV-09..11
- **Date:** 2026-10-16

## รอค้นคว้า (ก่อนถึง milestone ที่เกี่ยวข้อง)
- Boss: fair pursuit/escape scoring บน NavMesh (M5) · Card rule systems — Baba Is You (M6) · 2D/3D integration — Plucky Squire interview (M7) · Blender→Unity FBX axis/scale (M7, ทดสอบจริง)
- ลิงก์ YouTube จากผู้ใช้ 3 อันอ่านไม่ได้ (rate limit), 1 อันเป็นตัวอย่าง Cyberpunk Edgerunners 2 (ไม่เกี่ยว) — รอผู้ใช้ยืนยัน


## 2026-10-21 — Campaign scope and design decision
The user authorized continuing the five planets already named in Galaxy through World 5, selected detailed stylized Palace interiors/exteriors, and requested gameplay tests only in used levels. New themes: launch/tune the Moon; valves/boat/alternate launches in the Bath Sea; ordered bells/last knock/rest in Teacup. Optional stars and persisted best times/stars are replay goals. Status/property card effects assist timing; none directly grants victory. Existing GDD future-boss concepts remain backlog, not silently replaced as completed work.

Original soundtrack rendered with the project's dr_music toolkit + FluidSynth/GeneralUser GS; full instrument license recorded in ArtSource/Audio/Licenses and AUDIO_CREDITS. Original Thai ending uses contemplative scrolling presentation, with no copied Minecraft poem/music. Evidence and limitations: CAMPAIGN_FIVE_REPORT.md.
