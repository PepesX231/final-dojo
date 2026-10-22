# Art Direction Checklist — Applebook Surrealism
ใช้คู่กับ skill `art-direction-guardian` · อ้างอิง GDD §5, §6, §20

ทุก asset ต้องผ่านครบก่อนเข้า scene (ยกเว้น graybox ที่ติดป้าย `GRAYBOX_` ชัดเจน)

## A. Purpose (ทำไมมันอยู่ตรงนี้)
- [ ] ระบุหน้าที่อย่างน้อย 1 ข้อ: Worldbuilding / Navigation / Gameplay / Story / Characterization / Composition / Environmental storytelling (§5.10)
- [ ] ถ้าเป็น gameplay object: บอกได้จาก silhouette + สี ว่า "เดินได้ / โต้ตอบได้ / อันตราย"

## B. Shape Language (§5.2)
- [ ] Player: กลม ไม่สมมาตร เป็นมิตร — ไม่ใช่ทรงกลมเป๊ะ
- [ ] Friendly props: มุมมน ฐานมั่นคง
- [ ] Interactables: มีจุดจับ/ปุ่ม/ขอบชัด + accent mint หรือ warm yellow
- [ ] Enemies/Boss: contrast รูปทรงแรงกว่า แต่ยัง "เป็นของธรรมดา" (เก้าอี้ต้องดูออกว่าเป็นเก้าอี้จากไกล §12.3)
- [ ] Environment: ฟอร์มใหญ่เรียบ ช่วยขับ silhouette ผู้เล่น

## C. Palette (§5.3)
| Token | Hex | ใช้กับ |
|---|---|---|
| Midnight Plum | #292137 | เงา/พื้นหลังกลางคืน |
| Apple Red | #E85D65 | ผู้เล่น |
| Warm Cream | #F5E7D3 | พื้นที่ปลอดภัย, UI paper |
| Leaf Green | #78B78B | ใบ/ธรรมชาติ, interact accent |
| Dream Lavender | #AAA0DD | dream magic, safe area |
| Apricot Orange | #FFB870 | hazard (คู่กับ shape indicator) |
| Sky Blue | #8FD5ED | dream magic (cyan) |
| Dark Ink | #302A3D | ตัวหนังสือ/outline |
- [ ] Background saturation ต่ำกว่า gameplay objects
- [ ] ไม่ใช้ accent ทุกสีพร้อมกันในเฟรมเดียว
- [ ] Hazard ไม่สื่อด้วยสีอย่างเดียว (มี shape indicator)

## D. Materials (§5.4)
- [ ] Apple skin: soft stylized, color variation เบา ๆ, **ไม่เงาเปียก/ไม่ photoreal gloss**
- [ ] Wood: อุ่น, ลายไม้ simplified, ขอบมนไม่เป๊ะ
- [ ] Fabric: soft shading, รอยย่นนิดหน่อย
- [ ] Paper: มีความหนา, ขอบไม่เรียบ, fiber จาง ๆ
- [ ] Metal: reflection หม่น ไฮไลต์อ่านง่าย
- [ ] ไม่มี texture ตัวอักษรเพี้ยน (malformed letters)

## E. Lighting (§5.5)
- [ ] 1 ทิศแสงหลักต่อ environment
- [ ] Bedroom: โคมข้างเตียงอุ่น + แสงจันทร์เย็นจากหน้าต่าง + ambient fill + contact shadow
- [ ] Dream: เงาพื้นชัด, focal light ที่ของสำคัญ, จุดกระโดดไม่รก
- [ ] ไม่ emissive/rim light ทุกชิ้น, ไม่ bloom จัด

## F. Outlines (§5.6)
- [ ] เฉพาะ: ตัวละครหลัก, interactables สำคัญ, 2D surfaces, รายละเอียด boss
- [ ] ทดสอบใน motion ก่อนใช้ full-screen outline

## G. 2D in 3D (§5.7–5.8)
- [ ] ระบุ Type A (decor) / B (animated FX) / C (functional + collider ตรงรูป)
- [ ] Poster ติดผนัง, floor marking แนบพื้น, standing cutout หมุนแค่แกน Y, full billboard เฉพาะ FX
- [ ] alpha clip เมื่อทำได้, sorting คงที่, overdraw ต่ำ
- [ ] 2D ที่ดูทึบต้องไม่บล็อกการเดินโดยไม่มีกติกา

## H. Scale & Technical (§4.3, §6.5–6.6)
- [ ] ผู้เล่นสูง 1.2u, กว้าง ~0.9, ลึก ~0.75; เทียบ asset ใหม่กับ player/bed/chair เสมอ
- [ ] Pivot ถูก (ตัวละคร = เท้า, ประตู = บานพับ), transforms applied, normals ถูก
- [ ] Silhouette อ่านออก: ไม่มี texture / เป็นเงา / จากด้านหลัง / ระยะกล้องเกม / ขณะวิ่ง / ขณะกระโดด (§4.4)
- [ ] ใบ+ก้านไม่บังหน้าในมุมกล้องปกติ
- [ ] Collider เหมาะสม, ทำงานใน Unity, ดูชัดที่ระยะกล้อง gameplay

## I. Anti-AI-Slop (§5.10) — ถ้าเจอข้อใด = REJECT
- อนุภาคเรืองแสงทั่วฉาก · ของลอยไร้เหตุผล · texture style ปนกัน · gradient เยอะ · bloom เกิน · ตัวอักษรอ่านไม่ออก · panel UI สุ่ม · prop แฟนตาซีทั่วไป · พลาสติกเงาวับ · ตกแต่ง copy-paste · โลโก้ Apple Inc. / ลูกแอปเปิ้ลถูกกัด
- ห้ามลอก: โมเดล/แอนิเมชัน/เลย์เอาต์/ศัตรู/UI/เพลงของเกมอ้างอิง (Mario, Astro Bot ฯลฯ) — เรียน "ทำไม" ไม่ใช่ "อะไร"

## J. ผลตรวจ
บันทึกใน `docs/ASSET_MANIFEST.md` คอลัมน์ Art Review: `APPROVED` / `REVISE: <ข้อแก้>` / `REJECTED: <เหตุผล>`
