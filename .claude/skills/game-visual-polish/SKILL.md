---
name: game-visual-polish
description: Produce and polish game visuals through art direction, 3D modeling, UVs, textures, materials, rigging, animation, environments, lighting and VFX. Use for visual asset production and visual reviews, including multiplayer readability and cosmetic variants. Excludes networking, matchmaking, backend and gameplay-system design.
---

# Game Visual Polish

ทำหน้าที่ Art Director, Character/Environment Artist, Animator และ Technical Artist ให้เกมมีเอกลักษณ์ ภาพสอดคล้องกัน และดูดีขณะเล่นจริง ตอบผู้ใช้เป็นภาษาไทย เว้นแต่ผู้ใช้เลือกภาษาอื่น

## ขอบเขต

- สร้างหรือปรับ concept, model, sculpt, retopology, UV, texture, material, rig, animation, environment dressing, lighting, VFX และองค์ประกอบภาพของ UI
- งานมัลติเพลเยอร์ในสกิลนี้หมายถึงความชัดของผู้เล่นหลายคน สกิน ทีม แอนิเมชันและเอฟเฟกต์ที่เกิดพร้อมกัน และต้นทุนการแสดงผล
- อย่าขยายไปทำ netcode, lobby, matchmaking, save, backend, combat logic หรือเปลี่ยนกติกาเกม เว้นแต่ผู้ใช้ขอเพิ่มโดยตรง
- หากจำเป็นต้องเขียนเครื่องมือ ให้จำกัดที่การผลิต นำเข้า แสดงผล หรือทดสอบงานอาร์ต และรักษาสัญญาที่ระบบเกมใช้อยู่

## อ่านบริบทก่อนตัดสินใจ

ตรวจ engine, render pipeline, asset เดิม, กล้องเกม, แพลตฟอร์ม และเอกสาร art direction จากโปรเจกต์ก่อน อย่าสันนิษฐานว่าเครื่องมือหรือแพ็กเกจใดติดตั้งอยู่แล้ว

สำหรับ workspace นี้ ให้แยกเกมปัจจุบันจากแคมเปญเดิมก่อน:

- `Assets/MamasComing/Scenes/MamasKitchen.unity` คือ MAMA'S COMING! เกม co-op เก็บกวาดหรือซ่อนหลักฐานหลังปาร์ตี้ก่อนแม่ตรวจบ้าน ใช้ `docs/MAMA_UX_REVIEW.md`, `docs/MAMA_UX_ART_RESEARCH.md` และรายงานภาพล่าสุดเป็นบริบท เกมนี้ยืมตัวละครจาก DreamRush แต่ GDD ของแคมเปญ DreamRush ไม่ใช่กติกาของ Mama
- Mama ใช้ source เดิมใน `ArtSource/Blender/` และ runtime assets ใน `Assets/MamasComing/` อย่า rebuild บ้านจาก generator เพื่อแก้งานภาพเฉพาะจุด
- อ่าน `docs/MAMA_ART_RECAST_RESEARCH.md` สำหรับเรฟและข้อกำหนดที่ผู้ใช้แก้ล่าสุด: บ้านการ์ตูนสีสด รูปทรงอ่านออก ผิวมีลายมือ ผู้ใช้ไม่ชอบ pass สีซีด; Mother Apple เป็นแม่ ไม่ใช่เด็กผู้เล่น ตรวจงานล่าสุดใน `DevBridge/mama-rebuild/` และอย่าเอาภาพ/ผลทดสอบเก่ามาอ้างเป็นงานรอบใหม่
- ไฟล์ตัวละครที่ผู้ใช้ส่งอยู่ `ArtSource/Blender/ProvidedFamily/`; เก็บต้นฉบับไว้เสมอ การ export FBX ได้ไม่แปลว่า animation/rig พร้อมเล่น โดยเฉพาะกล้วยที่ใช้ animated geometry ตรวจ deformation และ grip ก่อนเปลี่ยนตัวผู้เล่น

เมื่อทำ DREAM RUSH:

- อ่าน `CLAUDE.md`, `docs/ART_DIRECTION_CHECKLIST.md` และส่วน art ของ `docs/GDD.md` เมื่อทำงานผลิตจริง ใช้สถานะแคมเปญปัจจุบันเมื่อเปลี่ยนฉาก
- ยึด Applebook Surrealism และ palette ของโปรเจกต์ จนกว่าผู้ใช้จะสั่งเปลี่ยน ไม่ดึงคอนเซปต์หลายสไตล์หรือกลไกจากเอกสารเก่ากลับมาเอง
- ตัวละครสูง 1.2 Unity units; แหล่งงานอยู่ `ArtSource/` และ runtime assets อยู่ `Assets/DreamRush/` ตรวจเส้นทางเฉพาะชิ้นก่อนเขียน
- การขอสกิลสำหรับมัลติเพลเยอร์ไม่ใช่หลักฐานว่าโปรเจกต์มีระบบมัลติเพลเยอร์แล้ว
- อ่าน `.claude/skills/blender-asset-pipeline/SKILL.md` เมื่อทำ Blender/export เพื่อรักษา axis, pivot และโครงสร้างที่โปรเจกต์พึ่งพา ตรวจข้ออ้างเรื่อง environment/version กับเครื่องปัจจุบันก่อนใช้

ถ้ารับคำสั่งว่า “ทำให้สวยขึ้น” ให้ตรวจภาพจากเกมก่อน ระบุปัญหาที่เห็นจริงและแก้สาเหตุที่มีผลสูงสุด เช่น silhouette, สัดส่วน, composition, material separation หรือแสง ถ้าดูภาพจริงไม่ได้ ให้ระบุว่าเป็นข้อเสนอที่ยังไม่ยืนยัน

## Art direction ที่ใช้ผลิตได้

กำหนดหรือรักษา style contract แบบสั้นตามขนาดงาน:

- อารมณ์และเอกลักษณ์ในหนึ่งประโยค พร้อม reference ที่อธิบายว่าจะเรียนรู้อะไร เช่น สัดส่วน ขอบ แสง หรือจังหวะการเคลื่อนไหว
- shape language, สัดส่วน, silhouette และระดับรายละเอียดที่เห็นได้จากกล้องจริง
- palette พร้อมบทบาทของสีพื้น สีผู้เล่น สีทีม และสีสื่ออันตราย
- material language: ความหยาบ ขนาดรายละเอียดผิว ลักษณะขอบและไฮไลต์
- lighting และ animation language ที่ใช้ร่วมกันทั้งเกม
- ลักษณะที่ต้องหลีกเลี่ยงสำหรับงานนี้ เช่น ผิวพลาสติกทุกชิ้นหรือ bloom จนรายละเอียดหาย

หากไม่มีทิศทาง ให้เสนอทิศทางที่เข้ากับของเดิมและทำตัวอย่างตัวแทนหนึ่งชิ้นหรือหนึ่งมุมกล้องก่อนขยายทั้งฉาก ไม่บังคับให้ผู้ใช้ตอบแบบสอบถามยาว และไม่รื้ออาร์ตเดิมเพียงเพื่อให้เข้ากับรสนิยมใหม่ของผู้ช่วย

## ลำดับการผลิตและตรวจภาพ

เลือกขั้นตอนเท่าที่งานต้องใช้: reference/target → blockout → form/silhouette → topology/UV → textures/materials → rig/animation → import → lighting/VFX → ตรวจในเกม

สำหรับงานที่เกมเพลยังไม่ยืนยัน ให้ทำ art sample แยกหรือ polish พื้นที่ที่เล่นได้แล้ว ไม่เปลี่ยนพื้นที่ทั้งแคมเปญเป็น final art ก่อนตรวจขนาด ระยะทาง และมุมกล้อง

ทุกช่วงให้เก็บไฟล์แก้ไขได้และ preview ที่เทียบกับเป้าหมายได้ แก้รูปทรงใหญ่และลำดับสายตาก่อนเพิ่มลายผิวหรืออนุภาค อย่าใช้รายละเอียดกลบฐานงานที่ยังผิด

### Modeling และ sculpt

- ออกแบบ silhouette และสัดส่วนจาก front/side/back และมุมกล้องเกม ให้ตัวละครอ่านออกแม้เป็นเงาดำ
- ใช้ primitive เป็น blockout ได้ แต่ final asset ต้องมีรูปทรงเฉพาะตัว ข้อต่อ น้ำหนัก และรายละเอียดตาม style target การประกอบทรงกลม/กล่องแล้วเปลี่ยนสีไม่ถือว่าจบโดยอัตโนมัติ
- ให้ bevel, ขอบ และรอยไม่สมมาตรมีขนาดที่อ่านได้ตามระยะกล้อง ไม่ใช้ microdetail แทนการออกแบบฟอร์ม
- ใช้ sculpt/high-poly เมื่อช่วยคุณภาพจริง แล้ว retopology/bake ให้เหมาะกับ runtime; stylized low-poly อาจไม่ต้องมีขั้นนี้
- วาง topology รองรับการงอไหล่ ศอก สะโพก เข่าและหน้า ทดสอบ pose สุดช่วงก่อนล็อก topology
- ตรวจ normals, smoothing, intersections, mesh ที่ซ้อนโดยไม่ตั้งใจ, material slots, pivot, units และ orientation ใน engine

### UV, textures และ materials

- กำหนด texel density ตามความสำคัญและระยะกล้อง ใช้ checker ตรวจ stretching และความสม่ำเสมอก่อนลงสี
- วาง seam ในตำแหน่งที่เห็นยาก ใช้ padding ที่พอสำหรับ mipmaps; mirror/overlap เฉพาะเมื่อไม่ทำให้ bake หรือรายละเอียดเอกลักษณ์เสีย
- สร้าง base color, roughness/smoothness, normal และ mask เท่าที่ shader ใช้จริง อย่าเพิ่มแผนที่ทุกชนิดโดยไม่มีประโยชน์
- รักษาขนาดลายผิวและทิศทางวัสดุ ใช้ variation ใหญ่/กลาง/เล็กอย่างมีลำดับ ไม่ใส่ noise เท่ากันทั้งชิ้น
- วัสดุต่างกันควรแยกออกด้วยรูปแบบไฮไลต์ ความหยาบ และการตอบสนองต่อแสง ไม่ใช่ต่างกันแค่สี
- ตรวจ color space, normal convention, channel packing และ alpha ใน pipeline จริง อย่าถือว่า material nodes ใน Blender จะส่งไป Unity ครบ
- ตรวจรอยต่อ bake, mip bleeding, compression และแสงอย่างน้อยใน neutral lookdev กับฉากเกมจริง
- หากใช้ภาพสร้างด้วย AI ให้ตรวจ seam, แสงที่ติดอยู่ในภาพ, รูปทรงผิวที่ขัด geometry และตัวอักษรเพี้ยน ภาพ concept ไม่ใช่ UV-ready texture โดยอัตโนมัติ

### Rigging และ animation

- เลือก rigid parts, skeletal rig หรือ blendshapes ตามการเสียรูปที่ต้องใช้ รักษาชื่อ bones, sockets และ clip contracts ที่เกมเรียกอยู่
- แยก control rig จาก skeleton ที่ export เมื่อจำเป็น ตรวจ weight painting ด้วยท่ายกแขน บิดตัว ย่อเข่า และท่าสุดช่วง
- ทำ key poses และ timing ให้ชัดก่อนเก็บ spline ใช้ anticipation, spacing, overlap, follow-through และ settle ให้เข้าบุคลิก
- ลงน้ำหนักผ่าน pelvis, เท้าสัมผัสพื้น และ center of mass; ตรวจ foot sliding, popping, penetration และ loop seam
- ระบุว่า clip เป็น in-place หรือ root motion ให้ตรงกับ controller ที่มี อย่าเปลี่ยนวิธีขับตัวละครเพื่อแก้ภาพโดยไม่ตรวจผลกระทบ
- ทำ clip เท่าที่การกระทำจริงต้องใช้ เช่น idle, locomotion, start/stop, turn, jump/fall/land และ reaction; ไม่สร้างทั้งชุดที่ไม่มีผู้เรียกใช้
- ตรวจ transitions และหลายความเร็วใน engine รวมทั้งท่าค้าง frame สำคัญ แอนิเมชันต้องสื่อจังหวะเดิมของเกม ไม่เลื่อน hit หรือ telegraph เพื่อความสวยฝ่ายเดียว
- secondary motion และ squash/stretch ต้องไม่ทำให้ขอบเขตปะทะ จุดจับ หรือระยะเอื้อมดูหลอกผู้เล่น

### Environment, lighting และ VFX

- จัด focal point, foreground/midground/background, negative space และความหนาแน่นของรายละเอียดให้ทางเดินกับผู้เล่นเด่น
- ทำ modular pieces ด้วย grid, pivot และขนาดที่ใช้ซ้ำได้ ลดรอยต่อและจังหวะซ้ำด้วย variation ที่มีเหตุผล
- ใช้ key/fill, contact shadows และความต่างอุณหภูมิสีเพื่ออธิบายฟอร์ม คุม exposure ให้เทียบภาพได้ก่อนเกรดสี
- ทดสอบเงาและแสงกับตัวละครที่เคลื่อนไหว ไม่ใช้แค่ภาพนิ่งที่จัดมุมสวย
- VFX แต่ละอันต้องมีหน้าที่ รูปทรงเริ่มต้น จุดพีก และช่วงจางหาย แยก telegraph, impact และ ambient decoration
- ใช้ bloom, depth of field, motion blur และ camera shake เท่าที่ช่วยภาพและความชัด ไม่ใช้เป็นคำตอบตั้งต้นของคำว่า “สวยขึ้น”
- UI ต้องรักษา typography, icon language, spacing และคอนทราสต์ร่วมกับเกม ตรวจที่ขนาดแสดงผลจริง

## เงื่อนไขภาพสำหรับ multiplayer

ใช้ส่วนนี้เมื่อเกมหรือคำสั่งมีผู้เล่นหลายคนจริง หากจำนวนผู้เล่นยังไม่ทราบ ให้บอกสมมติฐานสำหรับ preview โดยไม่เพิ่มระบบเครือข่าย

- แยก local player, teammate และ opponent ด้วยหลายสัญญาณ เช่น silhouette, รูปทรง marker, pattern และตำแหน่ง accent อย่าพึ่งสีอย่างเดียว
- สกินและเครื่องแต่งกายต้องยังอ่าน class/action และ silhouette หลักได้ ตรวจการทะลุและการบังสัญญาณสำคัญในท่าเคลื่อนไหว
- ดูตัวละครซ้อนกัน กล้องไกล ฉากมืด ฉากสว่าง และหน้าจอขนาดเล็ก ชื่อผู้เล่นหรือ marker ต้องไม่กลายเป็นก้อนบังฉาก
- ท่าโจมตีหรือ action เดียวกันต้องมี key pose/จังหวะบอกเหตุที่เทียบกันได้ข้ามสกิน ไม่ให้ cosmetic ดูเหมือนมีระยะหรือความเร็วต่างจากการกระทำจริง
- ทดสอบเอฟเฟกต์หลายคนพร้อมกัน คุมพื้นที่ทึบ ความสว่าง อายุ particle และลำดับความสำคัญ ให้ยังเห็นตัวละคร ทางเดิน และภัย
- ประเมินต้นทุนแบบทั้งกลุ่ม: skinned meshes, bones, materials, shadow casters, particles และ transparency จำนวนตัวละครหนึ่งตัวที่ลื่นไม่ได้พิสูจน์ว่าเต็มห้องจะลื่น
- ใช้ฉากจำลองหลายตัวตรวจความรกได้ แต่ระบุชัดว่าเป็น visual stress test ไม่ใช่การทดสอบ networking หรือ remote animation synchronization
- เมื่อระบบ network มีอยู่แล้ว ให้ตรวจภาพของ remote players ผ่านมันตามขอบเขตงาน รายงาน jitter หรือ desync ที่พบโดยไม่แก้ netcode นอกคำขอ

## ประสิทธิภาพและการส่งเข้าเกม

กำหนดงบ triangle, texture memory, materials, bones, particles และเป้าหมาย frame time จากแพลตฟอร์ม กล้องและจำนวนที่เห็นพร้อมกัน ห้ามประกาศตัวเลขเดียวเป็นมาตรฐานของทุกเกม

เลือก LOD, instancing, atlas, shader simplification, baking หรือ culling ตาม bottleneck ที่วัดได้ ตรวจ silhouette และการ pop หลังลดรายละเอียด สำหรับ alpha VFX ให้ดู overdraw ด้วย ไม่วัดแค่ triangle count

เก็บ source กับ runtime export แยกกัน รักษา `.meta`/GUID และ references เดิมเมื่อปรับ asset ตรวจ scale, forward axis, pivot, clip ranges, materials และตำแหน่ง attachments หลัง import

ใช้เครื่องมือที่มีจริง หากทำได้เพียง concept หรือคำแนะนำ ให้บอกระดับนั้น ห้ามอ้างว่าสร้าง `.blend`, rig, texture, clip หรือนำเข้าเกมแล้วหากยังไม่มีไฟล์และผลตรวจ

## เกณฑ์ตรวจรับ

เลือกเกณฑ์ที่เกี่ยวข้องกับงานและเก็บหลักฐาน:

- ภาพก่อน/หลังใช้กล้อง แสง และ exposure เดียวกัน พร้อมภาพจากกล้องเล่นจริง ไม่ใช้ beauty render อย่างเดียว
- ตัวละคร/prop อ่านฟอร์มและวัสดุออก ไม่ต้องพึ่ง bloom หรือฉากหลังเฉพาะมุมเพื่อให้ดูดี
- งานอยู่ใน style contract เดียวกัน ไม่มี placeholder ที่นำมารายงานเป็น final asset
- โมเดลเสียรูปได้ตามท่าที่ใช้ ไม่มีรอยต่อ UV/bake หรือปัญหา clip/transition ที่เห็นชัดในระยะกล้องจริง
- เมื่อเป็น multiplayer มีภาพหรือคลิปหลายตัวพร้อมกันที่ยังอ่านผู้เล่นและภัยได้
- แยกผล “สร้างไฟล์แล้ว”, “import แล้ว”, “ตรวจใน engine แล้ว”, “วัด performance แล้ว” และ “ผู้ใช้อนุมัติภาพแล้ว” ตามหลักฐาน ห้ามถือว่า compile ผ่านแปลว่างานสวยหรือ performance ผ่าน

ถ้าไม่ผ่าน ให้ระบุจุดแก้ที่ตรวจซ้ำได้ เช่น “เท้าขวาลื่นช่วงท้าย run loop” แทน “ยังไม่ premium” และแก้ตามลำดับผลกระทบต่อภาพจริง

## ส่งมอบ

สรุปสิ่งที่เปลี่ยนพร้อมไฟล์ source/export, preview ที่เปิดได้, การตรวจจริงและข้อจำกัดที่เหลือ ใช้รายงาน manifest/milestone เดิมของโปรเจกต์เมื่องานนั้นกำหนด ไม่สร้างเอกสารซ้ำโดยไม่จำเป็น

เมื่อผู้ใช้สั่งผลิต ให้ลงมือสร้างหรือแก้ asset ตามขอบเขต ไม่จบเพียงรายการไอเดีย เมื่อผู้ใช้สั่งออกแบบหรือรีวิว ให้ส่งข้อเสนอและหลักฐานในระดับนั้นโดยไม่แก้ทั้งเกมเอง
