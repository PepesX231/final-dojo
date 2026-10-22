# DREAM RUSH — The Apple Dream (Unity project: `final dojo`)

## Source of Truth
- `docs/GDD.md` = Master GDD v2.0 (ผู้ใช้กำหนดให้เป็น Source of Truth). เอกสารอื่นต้องไม่ขัด GDD; ถ้าจำเป็นต้องเบี่ยง ให้บันทึกใน Decision Log ของ `docs/P0_IMPLEMENTATION_PLAN.md`
- ขอบเขตตามคำขอผู้ใช้ 2026-10-20: ตรวจ/พัฒนาระบบที่มีจริงทั้ง World 1 และ Midnight Palace (งานเดิมถึง M20); P0 Chair ยังเก็บไว้เป็น prototype. ห้ามนับ placeholder เป็นด่านที่สร้างแล้ว
- ลำดับ: Movement → Bedroom Hub → Sleep transition → Chair stage graybox → Chair AI → 3 cards → Art/Anim/VFX/SFX/UI → Playtest

## Hard Rules
1. PLAYABILITY → FUN → VISUAL QUALITY. ห้ามข้าม graybox/prototype ไปทำฉากสวย
2. ห้ามรายงานว่าระบบเสร็จ/ผ่าน ถ้ายังไม่ได้ compile + รัน test + เล่นจริง — ระบุสิ่งที่ยังไม่ได้ทดสอบตรง ๆ
3. ห้ามใช้ art asset ที่ไม่ผ่าน `docs/ART_DIRECTION_CHECKLIST.md` (Applebook Surrealism); ห้ามลอก IP อ้างอิง; ห้ามโลโก้ Apple Inc.
4. การ์ดห้าม hard-code ชนะบอส — ต้องผ่าน property/status (GDD §18.6)
5. ทุก milestone เพิ่มรายงานใน `docs/MILESTONE_REPORTS.md`: features, files, tests run, actual results, known bugs, design risks, art findings, next

## Layout
```
docs/                 GDD, plan, art checklist, asset manifest, test plan, research log, architecture, reports
.claude/skills/       9 project skills (GDD §27) — ใช้ตรวจคุณภาพแต่ละระบบ
ArtSource/            .blend / illustrations / references (ไม่เข้า build)
Assets/DreamRush/     Scripts/{Runtime,Editor}, Tests/{EditMode,PlayMode}, Editor/DevBridge, Scenes, Prefabs, Materials, Art, ...
DevBridge/            คำสั่ง/ผลลัพธ์ของ DevBridge (generated, gitignored)
```

## Working with the Editor (DevBridge)
Editor tool `DreamRush.DevBridge` อ่านไฟล์คำสั่งใน `DevBridge/cmd/*.txt` (1 คำสั่งต่อไฟล์, ทำตามลำดับชื่อไฟล์) แล้วเขียนผลลง `DevBridge/out/`:
- `refresh` — AssetDatabase.Refresh (compile) · ผล compile → `out/compile.log`
- `menu <Menu/Path>` · `exec <Namespace.Type, Assembly>.<Method>` (static, ไม่มี args)
- `tests EditMode|PlayMode [nameFilter]` → `out/tests_<mode>.txt`
- `open <Assets/...unity>` · `play` · `stop` · `screenshot <file.png>` · `status`
- Console ทั้งหมด → `out/console.log`; สถานะ editor → `out/status.txt`

## Conventions
- Namespaces `DreamRush.<System>`; ScriptableObject สำหรับข้อมูล authoring, runtime state แยก
- ชื่อ asset ตาม GDD §6.3 (`CHR_`, `BOSS_`, `ENV_`, `PROP_`, `DEC_`, `FX_`), graybox ขึ้นต้น `GRAYBOX_`
- หน่วย: 1 unit = 1 m; ผู้เล่นสูง 1.2
- Palette tokens: `DreamRush.Core.Palette` (ตรงกับ GDD §5.3)

## Current audit constraints (2026-10-20)
- User selected detailed stylized Palace interiors/exteriors; read `docs/CURRENT_CAMPAIGN_STATUS.md` and `docs/PALACE_STYLE_REVIEW.md` for remaining work and limits of evidence.
- Galaxy gates Midnight Palace on a saved Dream_World1 clear (`WorldAccess`); preserve editor direct-start QA and never alter the real save to fake unlocks.
- Use `.claude/skills/dream-rush-audit/SKILL.md` for current evidence, scene safety and regression workflow. The milestone number is not proof of human playtest or final art approval.


## Current five-world campaign (2026-10-21)
Read docs/CAMPAIGN_FIVE_REPORT.md and docs/CURRENT_CAMPAIGN_STATUS.md before continuing. The user explicitly authorized building through the five current Galaxy worlds, retaining M21 Palace art. CampaignRoute lists the actual five scene targets. WorldAccess requires persisted predecessor clears.

Only test actual used campaign scenes/BedroomHub/Galaxy for this task. Do not run legacy Dream_Openworld, Test_MovementGym, Test_BossMidnight or map-inventory suites. Gameplay bots and staged encounter tests are not human playthroughs. Keep real-save isolation/hash guards. World 1 title starts at 1; every dream wakes at zero stability; credits/music are original.

The Windows development build contains only the seven active scenes; existing editor build settings retain legacy QA scenes for historical compatibility. Art is still in review, and a passing build is not proof of subjective fun or standalone performance.
