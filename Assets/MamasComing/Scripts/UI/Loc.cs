using System;
using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// English -> Thai. Exact sentences first, then fragment replacement (longest first, case-insensitive)
    /// so built-up strings like "Put the plate back  (Kitchen)" translate too.
    public static class Loc
    {
        public static bool Thai { get; private set; }
        public static event Action Changed;
        static readonly Dictionary<string, string> memo = new Dictionary<string, string>();
        static List<KeyValuePair<string, string>> frags;
        static Dictionary<string, string> exact;

        public static void Load() { Thai = PlayerPrefs.GetInt("mama_lang", 0) == 1; }
        public static void SetThai(bool on)
        {
            if (Thai == on) return;
            Thai = on; PlayerPrefs.SetInt("mama_lang", on ? 1 : 0); PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string T(string s)
        {
            if (!Thai || string.IsNullOrEmpty(s)) return s;
            if (memo.TryGetValue(s, out var r)) return r;
            Init();
            if (exact.TryGetValue(s.Trim(), out r)) { }
            else
            {
                r = s;
                foreach (var kv in frags)
                    if (r.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0) r = Replace(r, kv.Key, kv.Value);
            }
            if (memo.Count > 4000) memo.Clear();
            memo[s] = r; return r;
        }

        static string Replace(string s, string a, string b)
        {
            var sb = new System.Text.StringBuilder(); int i = 0;
            while (true)
            {
                int j = s.IndexOf(a, i, StringComparison.OrdinalIgnoreCase);
                if (j < 0) { sb.Append(s, i, s.Length - i); break; }
                sb.Append(s, i, j - i).Append(b); i = j + a.Length;
            }
            return sb.ToString();
        }

        static void Init()
        {
            if (exact != null) return;
            exact = new Dictionary<string, string>();
            var all = new List<KeyValuePair<string, string>>();
            for (int i = 0; i + 1 < D.Length; i += 2)
            {
                exact[D[i].Trim()] = D[i + 1];
                if (D[i].Length >= 8) all.Add(new KeyValuePair<string, string>(D[i], D[i + 1]));
            }
            for (int i = 0; i + 1 < F.Length; i += 2) all.Add(new KeyValuePair<string, string>(F[i], F[i + 1]));
            all.Sort((x, y) => y.Key.Length.CompareTo(x.Key.Length));
            frags = all;
        }

        // ---------------------------------------------------------------- full sentences
        static readonly string[] D =
        {
            "MAMA'S COMING!", "แม่กลับมาแล้ว!",
            "You dozed off on the sofa... the BATHTUB is overflowing, the CURRY is about to catch fire\nand smoke is creeping through the house. Fix it - or fake it - before Mom gets home!",
                "เผลอหลับบนโซฟา... อ่างอาบน้ำล้น แกงกำลังจะไหม้\nควันลอยไปทั่วบ้าน! แก้ให้ทัน - หรือแอบซ่อนให้เนียน - ก่อนแม่กลับถึงบ้าน!",
            "Press any key to wake up", "กดปุ่มอะไรก็ได้เพื่อตื่น",
            "Press R to try again", "กด R เพื่อเล่นใหม่",
            "WASD move     SHIFT run (wet floor = you WILL slip)     SPACE jump     MOUSE look\n", "WASD เดิน     SHIFT วิ่ง (พื้นเปียก = ลื่นแน่)     SPACE กระโดด     เมาส์ หันมอง\n",
            "E interact / pick up     HOLD E valves, drains, knobs     Q drop     RMB throw\n", "E ใช้ / หยิบ     กด E ค้าง วาล์ว ท่อ ลูกบิด     Q วาง     คลิกขวา ขว้าง\n",
            "Hold LMB  use item: mop / squeegee / extinguisher / scrub     SCROLL zoom     ESC mouse", "กดคลิกซ้ายค้าง ใช้ของ: ม็อบ / ไม้รีดน้ำ / ถังดับเพลิง / ขัด     ล้อเมาส์ ซูม     ESC ตั้งค่า",
            "HOW BUSTED ARE YOU", "แม่จับได้แค่ไหน",
            "DISASTER REPORT", "รายงานหายนะ",
            "Empty hands", "มือว่าง",
            "MOM IN", "แม่ถึงใน", "MOM IS", "แม่", "HOME", "ถึงบ้าน!",
            "OFF", "ปิด", "LO", "เบา", "HI", "แรง",
            "[A] [D] [A] [D]...  don't let it slip back!", "[A] [D] [A] [D]... สลับกัน อย่าให้มันหมุนกลับ!",
            "[E] right when it points at OFF!", "กด [E] ตอนเข็มชี้ที่ ปิด!",
            "[E] when it's in the green!", "กด [E] ตอนอยู่ในช่องเขียว!",
            "Tap [E] fast!", "กด [E] รัว ๆ!",
            "CLOSE THE TAP! It's stuck!", "ปิดก๊อก! มันฝืดมาก!",
            "TURN OFF THE GAS!", "ปิดแก๊ส!",
            "CRANK THE MAIN VALVE!", "หมุนวาล์วน้ำหลัก!",
            "Looks clean! Check Mom's route", "ดูสะอาดแล้ว! เช็กทางที่แม่จะเดิน",
            "Act natural...", "ทำตัวปกติ...", "Act natural... act natural...", "ทำตัวปกติ... ทำตัวปกติ...",
            "PUT OUT THE FIRE!", "ดับไฟ!",
            "Turn off the stove", "ปิดเตา", "Turn OFF the stove", "ปิดเตา", "Turn the stove back on", "เปิดเตาอีกครั้ง",
            "Stop the bathtub overflowing", "หยุดน้ำล้นอ่าง",
            "Silence the smoke alarm", "ทำให้สัญญาณควันเงียบ",
            "Get rid of the smoke", "ไล่ควันออก",
            "Deal with the burnt pot", "จัดการหม้อไหม้",
            "Clean the white powder", "เช็ดผงสีขาว",
            "Tidy up", "เก็บกวาด",
            "Put it back here", "วางคืนตรงนี้",
            "Put it back", "วางคืนที่",
            // calls
            "Bathroom FLOODING + kitchen SMOKING. Mom is on her way. GO GO GO!", "ห้องน้ำน้ำท่วม + ครัวควันโขมง แม่กำลังกลับ! ไป ไป ไป!",
            "PHONE - Mom: \"Hi sweetie~ I'm leaving the market now!\"", "โทรศัพท์ - แม่: \"จ้ะลูก~ แม่ออกจากตลาดแล้วนะ!\"",
            "PHONE - Mom: \"Traffic's good! Home in 3 minutes. Did you eat the curry?\"", "โทรศัพท์ - แม่: \"รถไม่ติด! อีก 3 นาทีถึง กินแกงหรือยังลูก?\"",
            "*VROOM* ...Mom's car just turned into the street!", "*บรื้นน* ...รถแม่เลี้ยวเข้าซอยแล้ว!",
            "*jingle jingle*  KEYS AT THE FRONT DOOR!!", "*กรุ๊งกริ๊ง* เสียงกุญแจที่ประตูหน้า!!",
            "*CLICK* The door opens... MOM IS HOME! Act natural!", "*แกร๊ก* ประตูเปิด... แม่กลับมาแล้ว! ทำตัวปกติ!",
            "The water reached the LIVING ROOM CARPET!", "น้ำไหลไปถึงพรมห้องนั่งเล่นแล้ว!",
            "BEEP! BEEP! BEEP! The SMOKE ALARM is going off!", "ปี๊บ! ปี๊บ! ปี๊บ! สัญญาณควันดัง!",
            "FWOOOSH!!! Water on a GREASE FIRE = FIREBALL!", "ฟู่มมม!!! ราดน้ำใส่ไฟน้ำมัน = ลูกไฟ!",
            "*cough cough* ...is something BURNING?!", "*แค่ก แค่ก* ...อะไรไหม้หรือเปล่า?!",
            "...and why are my socks WET?!", "...แล้วทำไมถุงเท้าเปียก?!",
            // kid
            "FIRE!! Put it out!", "ไฟไหม้!! ดับเร็ว!",
            "The stove!! Hurry!", "เตา!! เร็วเข้า!",
            "Something's burning... the stove!", "อะไรไหม้... เตาแน่ ๆ!",
            "The bathtub! Stop the water!", "อ่างอาบน้ำ! หยุดน้ำก่อน!",
            "Is the bath still running?", "น้ำในอ่างยังไหลอยู่เหรอ?",
            "So LOUD! Make it stop!", "ดังมาก! ทำให้มันเงียบที!",
            "*cough* I need fresh air in here!", "*แค่ก* ต้องเปิดให้อากาศเข้า!",
            "Why isn't the water draining?", "ทำไมน้ำไม่ลงท่อ?",
            "Mom's pot... she can't see this!", "หม้อของแม่... ห้ามให้แม่เห็น!",
            "This white powder is everywhere...", "ผงขาวเต็มไปหมด...",
            "Looks okay... I think?", "น่าจะโอเคแล้ว... มั้ง?",
            "Mop time!", "ได้เวลาถูพื้น!", "Swoosh swoosh...", "ฟึ่บ ฟึ่บ...", "Nobody will notice... right?", "ไม่มีใครสังเกตหรอก... ใช่ไหม?",
            "Ugh, it's so black...", "อี๋ ดำปี๋เลย...", "Heavy...!", "หนัก...!", "A lid!", "ฝาหม้อ!", "A towel...", "ผ้าเช็ดตัว...",
            "Okay... aim and squeeze!", "โอเค... เล็งแล้วบีบ!", "Oops...", "อุ๊ย...", "Phew! Water's off!", "เฮ้อ! ปิดน้ำแล้ว!",
            "Why did I do that?!", "ทำไปทำไมเนี่ย?!", "Gas off! But it's still burning!", "ปิดแก๊สแล้ว! แต่ไฟยังลุกอยู่!", "Gas off. Phew!", "ปิดแก๊สแล้ว เฮ้อ!",
            "It's so smoky in here!", "ควันเยอะมาก!", "Hmm... why does it smell burnt?", "อืม... ทำไมได้กลิ่นไหม้?",
            // toasts
            "CRASH! Watch where you're running!", "โครม! ระวังหน่อย!",
            "WHOA! *SLIP* *SPLASH* *THUD*", "ว้าย! *ลื่น* *จ๋อม* *ตุ้บ*",
            "Ouch! Running on a wet floor... genius.", "โอ๊ย! วิ่งบนพื้นเปียก... ฉลาดมาก",
            "*SPLAT* ...that's gonna bruise.", "*แปะ* ...ช้ำแน่",
            "Slow down on the water!", "เดินช้า ๆ บนน้ำ!",
            "Sharp turn on a wet floor... WHOA!", "เลี้ยวแรงบนพื้นเปียก... ว้าย!",
            "!! RUNNING ON A WET FLOOR !!", "!! วิ่งบนพื้นเปียก !!",
            "The fire is OUT!", "ไฟดับแล้ว!", "Smothered! No air, no fire.", "ปิดไฟสำเร็จ! ไม่มีอากาศ ไม่มีไฟ",
            "*HSSSSS* Doused it!", "*ฟี่ดดด* ดับแล้ว!",
            "Ew... but it's draining!", "อี๋... แต่น้ำลงแล้ว!",
            "Window open - smoke will drift out. A fan would help...", "เปิดหน้าต่างแล้ว - ควันจะลอยออก ใช้พัดลมช่วยด้วยก็ดี...",
            "Fan on! It pushes the smoke toward the open window.", "เปิดพัดลมแล้ว! เป่าควันไปทางหน้าต่าง",
            "*clunk* Main water OFF. Every tap in the house just went quiet.", "*กึก* ปิดวาล์วน้ำหลักแล้ว ทุกก๊อกในบ้านเงียบ",
            "Water is back on.", "น้ำมาแล้ว",
            "Plug back in. The tub will fill again...", "เสียบจุกคืนแล้ว น้ำจะเต็มอ่างอีก...",
            "*SHLOOORP* Plug pulled! The tub drains... but the tap is STILL running.", "*ซู้ดดด* ดึงจุกแล้ว! น้ำในอ่างลง... แต่ก๊อกยังไหล",
            "*CRACK*  The old handle SNAPPED OFF! Shut the MAIN VALVE (laundry) or pull the plug!", "*แคร็ก* ด้ามก๊อกหัก! ปิดวาล์วหลัก (ห้องซักผ้า) หรือดึงจุกอ่าง!",
            "*pfft* ...it's empty!", "*ฟึ่บ* ...หมดแล้ว!",
            "*clack* Battery's out. It can't snitch now... but Mom might notice.", "*แกร๊ก* ถอดถ่านแล้ว มันฟ้องไม่ได้แล้ว... แต่แม่อาจสังเกต",
            "Hushed for 25 seconds... clear that smoke!", "เงียบไป 25 วินาที... รีบไล่ควัน!",
            "Lid on = no oxygen. Textbook!", "ปิดฝา = ไม่มีออกซิเจน ตามตำราเป๊ะ!",
            "The flames are too big for the lid! Use the extinguisher!", "ไฟใหญ่เกินกว่าจะปิดฝา! ใช้ถังดับเพลิง!",
            "The towel caught fire too!! Bad idea!", "ผ้าติดไฟด้วย!! ไม่น่าเลย!",
            "Towel soaked through. Good for smothering flames!", "ผ้าเปียกชุ่มแล้ว ใช้คลุมดับไฟได้!",
            "The pot looks (almost) brand new!", "หม้อดู (เกือบ) ใหม่เอี่ยม!",
            "Squeeeze. Mop wrung out.", "บิ๊ดดด ม็อบแห้งแล้ว", "Squeeeze! Mop wrung into the bucket.", "บิ๊ดดด! บิดม็อบลงถังแล้ว",
            "Glug glug glug... bucket emptied.", "โครก โครก... เทถังหมดแล้ว",
            "Bucket is FULL - go empty it in the toilet!", "ถังเต็มแล้ว - ไปเทในชักโครก!",
            "Swoosh! Herd the water toward the bathroom drain.", "ฟึ่บ! ไล่น้ำไปที่ท่อในห้องน้ำ",
            "No water!", "ไม่มีน้ำ!", "No water! The main valve is off...", "ไม่มีน้ำ! วาล์วหลักปิดอยู่...", "Nothing comes out... the main valve is off.", "น้ำไม่ไหล... วาล์วหลักปิดอยู่",
            "Wait - the fan is blowing papers everywhere!", "เดี๋ยว - พัดลมเป่ากระดาษปลิวไปทั่ว!",
            "Hmm... throw water on a grease fire? (RMB)", "อืม... ราดน้ำใส่ไฟน้ำมันดีไหม? (คลิกขวา)",
            "Can't reach! Need something long - the mop?", "เอื้อมไม่ถึง! ต้องใช้อะไรยาว ๆ - ม็อบดีไหม?",
            "The smoke alarm is too high... hold a mop or squeegee to reach it", "สัญญาณควันอยู่สูงไป... ถือม็อบหรือไม้รีดน้ำไปจิ้ม",
            "Scrub it at the KITCHEN SINK (hold LMB there).", "เอาไปขัดที่ซิงก์ครัว (กดคลิกซ้ายค้าง)",
            "Fire extinguisher!", "ถังดับเพลิง!",
            "Can fill a bucket / wet a towel here (needs the main valve open).", "เติมน้ำใส่ถัง / ชุบผ้าได้ตรงนี้ (ต้องเปิดวาล์วหลัก)",
            // prompts
            "Close the bathtub tap", "ปิดก๊อกอ่างอาบน้ำ", "Turn off the tap", "ปิดก๊อก", "Turn on the tap", "เปิดก๊อก",
            "Open the window", "เปิดหน้าต่าง", "Close the window", "ปิดหน้าต่าง", "Turn ON the fan", "เปิดพัดลม", "Turn off the fan", "ปิดพัดลม",
            "Hold [E]  Fill the bucket", "กด [E] ค้าง  เติมน้ำใส่ถัง",
            "Hold [E]  Jab the HUSH button with the handle", "กด [E] ค้าง  ใช้ด้ามจิ้มปุ่มปิดเสียง",
            "Hold [E]  Knock the battery out (sneaky...)", "กด [E] ค้าง  เขี่ยถ่านออก (แอบ ๆ...)",
            "Hold [E]  Open the main water valve", "กด [E] ค้าง  เปิดวาล์วน้ำหลัก",
            "Hold [E]  Pull the hair out of the drain (ew!)", "กด [E] ค้าง  ดึงผมออกจากท่อ (อี๋!)",
            "Hold [E]  Reach into the tub and pull the plug", "กด [E] ค้าง  ล้วงไปดึงจุกอ่าง",
            "Hold [E]  Scoop water OUT of the tub", "กด [E] ค้าง  ตักน้ำออกจากอ่าง",
            "Push the plug back in", "เสียบจุกคืน", "Put the lid on the pot", "ปิดฝาหม้อ",
            "SLAM the lid on the burning pot!", "ปิดฝาหม้อที่ไฟลุกเลย!", "Slam the lid on!", "ปิดฝาเลย!",
            "Shut the MAIN water valve", "ปิดวาล์วน้ำหลัก", "Unclog the drain", "ทะลวงท่อ",
            "Grab the fire extinguisher", "หยิบถังดับเพลิง", "Grab the lid!", "หยิบฝาหม้อ!", "Grab the mop", "หยิบม็อบ", "Grab the squeegee", "หยิบไม้รีดน้ำ",
            "Throw the WET towel over the flames", "โยนผ้าเปียกคลุมไฟ", "Throw the dry towel over the flames (risky!)", "โยนผ้าแห้งคลุมไฟ (เสี่ยง!)",
            "Soak the towel", "ชุบผ้า", "Soak the towel (for smothering fires)", "ชุบผ้าให้เปียก (ไว้คลุมดับไฟ)",
            "Wring out the mop here", "บิดม็อบตรงนี้", "Wring the mop", "บิดม็อบ", "Wring the mop into the bucket", "บิดม็อบลงถัง", "Wring out the towel (hold LMB)", "บิดผ้า (กดคลิกซ้ายค้าง)",
            "Fill it at a tap - or scoop out the tub", "เติมน้ำจากก๊อก - หรือตักจากอ่าง",
            "Drop it on the water", "วางทับน้ำ", "Empty it here", "เทตรงนี้", "Push water here", "ไล่น้ำมาตรงนี้", "Hide it?", "ซ่อนไหม?",
            "[Hold LMB]  Mop   (walk slowly!)     [Q]  Place", "[คลิกซ้ายค้าง]  ถูพื้น  (เดินช้า ๆ!)     [Q]  วาง",
            "[Hold LMB]  Push water ahead of you     [Q]  Place", "[คลิกซ้ายค้าง]  ดันน้ำไปข้างหน้า     [Q]  วาง",
            "[Hold LMB]  SPRAY at the base of the fire     [Q]  Place", "[คลิกซ้ายค้าง]  ฉีดที่ฐานไฟ     [Q]  วาง",
            "[Hold LMB at kitchen sink]  Scrub     [Q]  Place     [RMB]  Throw", "[คลิกซ้ายค้างที่ซิงก์]  ขัด     [Q]  วาง     [คลิกขวา]  ขว้าง",
            "[Q]  Place it (circle)      [RMB]  Throw", "[Q]  วาง (ตรงวงกลม)      [คลิกขวา]  ขว้าง",
            "[Q]  Place it on a puddle to soak it up     [RMB]  Throw", "[Q]  วางบนแอ่งน้ำให้ซับ     [คลิกขวา]  ขว้าง",
            "[Q]  Put down      [RMB]  Throw", "[Q]  วาง      [คลิกขวา]  ขว้าง",
            "[RMB]  Throw the water     [Q]  Place", "[คลิกขวา]  สาดน้ำ     [Q]  วาง",
            // mom
            "THE SMOKE ALARM IS GOING OFF?!", "สัญญาณควันดังเหรอ?!",
            "Why is the smoke alarm hanging open? Where's the battery?!", "ทำไมสัญญาณควันเปิดค้าง? ถ่านไปไหน?!",
            "WHY IS MY HOUSE FULL OF SMOKE?!", "ทำไมบ้านควันเต็มไปหมด?!",
            "You left the STOVE ON?! You could've burned the house down!", "เปิดเตาทิ้งไว้เหรอ?! บ้านไหม้ได้เลยนะ!",
            "Is that the BATHTUB running?! Why is it still running?!", "น้ำในอ่างไหลอยู่เหรอ?! ทำไมยังไม่ปิด?!",
            "WHO BROKE THE BATHTUB TAP?!", "ใครทำก๊อกอ่างหัก?!",
            "Why has the fire extinguisher been USED?!", "ทำไมถังดับเพลิงถูกใช้แล้ว?!",
            "Why are you holding the FIRE EXTINGUISHER?!", "ถือถังดับเพลิงทำไม?!",
            "And what is that you're HOLDING?! Is that my POT?!", "แล้วถืออะไรอยู่?! นั่นหม้อแม่ใช่ไหม?!",
            "*BONK* OW!!! ...Is this my POT?! WHY IS IT BLACK?!", "*โป๊ก* โอ๊ย!!! ...หม้อแม่เหรอ?! ทำไมดำแบบนี้?!",
            "Is that my good POT?! Why is it completely BLACK?!", "นั่นหม้อดีของแม่เหรอ?! ทำไมดำไปหมด?!",
            "Why is there no water?! Who turned off the main valve?!", "ทำไมไม่มีน้ำ?! ใครปิดวาล์วหลัก?!",
            "What is all this white POWDER on the floor?!", "ผงขาวเต็มพื้นนี่อะไร?!",
            "Why are there soaking wet towels on the floor?!", "ทำไมผ้าเปียกโชกกองอยู่บนพื้น?!",
            "Why is my good towel BURNT?!", "ทำไมผ้าเช็ดตัวดี ๆ ไหม้?!",
            "Hm? Why is the floor damp...?", "หืม? ทำไมพื้นชื้น ๆ...?",
            "Why is the floor soaking wet in here?!", "ทำไมพื้นตรงนี้เปียกโชก?!",
            "*peeks underneath* ...Just dust bunnies.", "*ก้มดูข้างใต้* ...มีแต่ฝุ่น",
            "*squish?* ...Hm. Must be my imagination.", "*แจ๊ะ?* ...หืม คงคิดไปเอง",
            "Chips for lunch?! Where's the curry I made?", "กินขนมเป็นมื้อเที่ยงเหรอ?! แกงที่แม่ทำไว้ล่ะ?",
            "Is that your HOMEWORK all over the floor?!", "การบ้านกระจายเต็มพื้นเหรอ?!",
            "Were you playing video games ALL afternoon?!", "เล่นเกมทั้งบ่ายเลยใช่ไหม?!",
            "Why is Mr. Fluffy lying in the middle of the floor?", "ทำไมน้องหมีนอนกลางพื้นล่ะ?",
            "Why is the mop bucket out?! What were you DOING?", "ทำไมถังม็อบออกมา?! ทำอะไรอยู่?",
            "Why is the soy sauce on the FLOOR?!", "ทำไมซีอิ๊วมาอยู่บนพื้น?!",
            "Why is this chair all the way over HERE?", "ทำไมเก้าอี้มาอยู่ตรงนี้?",
            // endings
            "FIRE DEPARTMENT!", "รถดับเพลิงมา!",
            "The neighbours saw flames in the kitchen window and called 911.\nThree fire trucks are outside when Mom pulls up. Everyone's okay... except your allowance.",
                "เพื่อนบ้านเห็นไฟที่หน้าต่างครัวเลยโทรเรียก 199\nตอนแม่มาถึงมีรถดับเพลิงจอดอยู่สามคัน ทุกคนปลอดภัย... ยกเว้นค่าขนม",
            "POT-ASTROPHE!", "หม้อถล่ม!",
            "Everything looked fine... until Mom opened the cabinet and the burnt pot fell right on her head.", "ทุกอย่างดูเรียบร้อย... จนแม่เปิดตู้ แล้วหม้อไหม้หล่นใส่หัวแม่พอดี",
            "PERFECT ALIBI", "แผนเนียนสมบูรณ์แบบ",
            "Mom didn't suspect a thing.\n\"Aww, were you a good kid today?\" ...Yes. Yes you were.", "แม่ไม่สงสัยอะไรเลย\n\"วันนี้เป็นเด็กดีหรือเปล่าจ๊ะ?\" ...ใช่ ดีมากเลยแหละ",
            "MOM IS... SUSPICIOUS", "แม่... เริ่มสงสัย",
            "She keeps squinting at you. And sniffing the air.\nYou survived - for now.", "แม่หรี่ตามองแล้วก็ดมกลิ่นไปรอบ ๆ\nรอดไปได้ - แค่ตอนนี้",
            "BUSTED!", "โดนจับได้!",
            "Mom saw EVERYTHING.\nNo games, no phone, no dessert. For a month.", "แม่เห็นทุกอย่าง\nงดเกม งดมือถือ งดขนม หนึ่งเดือน",
            "BURNING", "ไฟลุก", "out", "ดับ", "ALARM BEEPING!", "สัญญาณดัง!", "alarm disabled", "ปิดสัญญาณแล้ว",
            "thick", "หนา", "hazy", "จาง ๆ", "clear", "โปร่ง",
        };

        // ---------------------------------------------------------------- fragments (names, pieces of built-up lines)
        static readonly string[] F =
        {
            "Living room", "ห้องนั่งเล่น", "Laundry", "ห้องซักผ้า", "Dining room", "ห้องกินข้าว", "Hallway", "ทางเดิน", "Bedroom", "ห้องนอน", "Bathroom", "ห้องน้ำ", "Kitchen", "ห้องครัว",
            "Bath towel", "ผ้าเช็ดตัว", "Bathroom sink", "อ่างล้างหน้า", "Bathtub tap", "ก๊อกอ่างอาบน้ำ", "Bathtub", "อ่างอาบน้ำ", "Big towel", "ผ้าผืนใหญ่", "Bucket", "ถังน้ำ",
            "Bath mat", "พรมเช็ดเท้า", "BedroomRug", "พรมห้องนอน", "Desk chair", "เก้าอี้โต๊ะ", "Hallway runner", "พรมทางเดิน", "Living room carpet", "พรมห้องนั่งเล่น",
            "Chair", "เก้าอี้", "Chips bag", "ถุงขนม", "Curry pot", "หม้อแกง", "Burnt pot", "หม้อไหม้", "Drain", "ท่อระบาย", "Fan", "พัดลม", "Fire extinguisher", "ถังดับเพลิง",
            "Fruit bowl", "ชามผลไม้", "Game controller", "จอยเกม", "Homework", "การบ้าน", "Kitchen sink", "ซิงก์ครัว", "Laundry basket", "ตะกร้าผ้า", "Laundry hamper", "ตะกร้าผ้าใช้แล้ว",
            "Main valve", "วาล์วหลัก", "Mop", "ม็อบ", "Pantry", "ตู้กับข้าว", "Plate", "จาน", "Pot lid", "ฝาหม้อ", "Schoolbook", "หนังสือเรียน", "Smoke alarm", "สัญญาณควัน", "Sofa", "โซฟา",
            "Soy sauce", "ซีอิ๊ว", "Squeegee", "ไม้รีดน้ำ", "Stove", "เตา", "Teddy bear", "ตุ๊กตาหมี", "Toilet", "ชักโครก", "Towel", "ผ้า", "Toy box", "กล่องของเล่น", "Under-sink cabinet", "ตู้ใต้ซิงก์",
            "Utility sink", "อ่างซักล้าง", "Wardrobe", "ตู้เสื้อผ้า", "Washing machine", "เครื่องซักผ้า", "Window", "หน้าต่าง", "Bed", "เตียง", "Used extinguisher", "ถังดับเพลิงที่ใช้แล้ว",
            "Bath towel fire", "ผ้าเช็ดตัวไหม้", "Big towel fire", "ผ้าผืนใหญ่ไหม้", "Dish towel", "ผ้าเช็ดจาน", "Kitchen cabinet", "ตู้ครัว", "Pot fire", "ไฟในหม้อ", "Towel fire", "ผ้าไหม้",
            "Extinguisher powder", "ผงดับเพลิง", "Wet floor", "พื้นเปียก",
            // pieces
            "Put the ", "เอา", " back  (", " ไปคืน  (", " back (green circle)", " ไปคืน (วงกลมเขียว)", " back where it belongs.", " ไปคืนที่เดิม",
            " goes back in the ", " ต้องเอาไปคืนที่", "...and ", "...และอีก ", " more things", " ชิ้น", "its spot", "ที่เดิม",
            "So much water in the ", "น้ำท่วมเต็ม", "Clean up the water  (", "เช็ดน้ำ  (", "Empty the bucket (", "เทถังน้ำ (",
            "Pick up the soggy ", "หยิบ", "Pick up the ", "หยิบ", "Grab the soggy ", "หยิบ", "Grab the ", "หยิบ", "Stash the ", "ซ่อน", "Stashed the ", "ซ่อน", " in the cabinet", " ในตู้", ". Out of sight...", " มองไม่เห็นแล้ว...",
            "Swap for the ", "สลับเป็น", "Open the ", "เปิด", "Close the ", "ปิด", "Scooped ", "ตักออก ", " L out of the tub!", " ลิตรจากอ่าง!",
            "WHOOSH! The ", "ฟู่ม! ", " is ON FIRE!", " ไฟลุกแล้ว!", "The fire SPREAD to the ", "ไฟลามไปที่", "FIRE!!! THE ", "ไฟไหม้!!! ", " IS ON FIRE!!!", " ไฟลุก!!!",
            "Why is the ", "ทำไม", " all BLACK and burnt?!", " ดำไหม้หมด?!", "WHY IS THE ", "ทำไม", " FLOODED?!", " น้ำท่วม?!", "And WHAT is the ", "แล้ว", " doing in here?!", " มาอยู่ในนี้ได้ยังไง?!",
            "*opens the ", "*เปิด", "* ...Hmm. All good.", "* ...อืม เรียบร้อยดี", "*SQUISH* ...Why is this ", "*แจ๊ะ* ...ทำไม", " SOAKING WET?!", " เปียกโชก?!", " all over the place?!", " กระจายเต็มไปหมด?!",
            "SPLOOSH! ", "จ๋อม! ", " liters all over the floor!", " ลิตรเต็มพื้น!",
            "FIRE DEPT in ", "รถดับเพลิงใน ", "spreads in ", "ลามใน ", "ignites in ", "ติดไฟใน ", "  RISING", "  เพิ่มขึ้น",
            "Really fixed: ", "แก้จริง: ", "    Faked / hidden: ", "    แอบซ่อน: ", "    Slipped: ", "    ลื่นล้ม: ", "x    Smart moves: ", " ครั้ง    เล่นฉลาด: ",
            "\nWater still on the floor: ", "\nน้ำที่ยังค้างบนพื้น: ", "\n\nMom noticed:", "\n\nแม่สังเกตเห็น:",
            "Hold [E]", "กด [E] ค้าง", "Hold E - ", "กด E ค้าง - ", "Hold LMB - ", "คลิกซ้ายค้าง - ", "hush it", "ปิดเสียง", "main valve", "วาล์วหลัก", "scoop the tub", "ตักน้ำในอ่าง",
            "stove off", "ปิดเตา", "unclog drain", "ทะลวงท่อ", "mop!", "ถู!", "scrub", "ขัด", "spray!", "ฉีด!", "Pick up", "หยิบ", "Put down", "วาง", "Place", "วาง", "Throw", "ขว้าง",
            "or hide it", "หรือซ่อน", "or pull the plug", "หรือดึงจุก",
        };
    }
}
