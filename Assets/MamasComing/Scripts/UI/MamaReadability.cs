using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MamasComing
{
    /// <summary>Observation first. Context for what you hold; optional ideas, never an ordered quest route.</summary>
    [DefaultExecutionOrder(100)]
    public class MamaReadability : MonoBehaviour
    {
        GameManager gm;
        bool overview;
        bool hintVisible;
        public bool NotesVisible => overview && !hintVisible;
        MessyProp ghostItem;
        GameObject homeGhost;
        Material ghostMaterial;
        float thoughtAt;

        public static string Text(string english, string thai) => Loc.Thai ? thai : english;

        public static int RoomIndex(Vector3 p)
        {
            if (p.x >= 22f && p.z < 6f) return 7;
            if (p.x >= 22f && p.z >= 8f) return 8;
            if (p.z >= 8f) return p.x < 7f ? 4 : p.x < 12f ? 5 : 6;
            if (p.z >= 6f) return 3;
            return p.x < 13f ? 0 : p.x < 17f ? 1 : 2;
        }

        public static string Room(Vector3 p)
        {
            string[] en = { "Living room", "Laundry", "Dining room", "Hallway", "Bedroom", "Bathroom", "Kitchen", "Garage", "Game room" };
            string[] th = { "ห้องนั่งเล่น", "ห้องซักล้าง", "ห้องกินข้าว", "ทางเดิน", "ห้องนอน", "ห้องน้ำ", "ห้องครัว", "โรงรถ", "ห้องเล่นเกม" };
            return Loc.Thai ? th[RoomIndex(p)] : en[RoomIndex(p)];
        }

        public static bool HasHomeCue(MessyProp p) => p && p.homeSet && (p.tool == ToolType.Prop || p.tool == ToolType.Chair);

        void Start()
        {
            gm = GetComponent<GameManager>();
            if (!GetComponent<MamaPresentation>()) gameObject.AddComponent<MamaPresentation>();
            // This shared material is cloned: existing scene/material edits remain untouched.
            if (gm.player.ghostMaterial)
            {
                ghostMaterial = new Material(gm.player.ghostMaterial);
                ghostMaterial.SetColor("_BaseColor", new Color(0.1f, 0.85f, 0.72f, 0.26f));
            }
        }

        void LateUpdate()
        {
            if (!gm || !gm.player || !gm.ui) return;
            bool active = gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome;
            if (!active) { if (homeGhost) homeGhost.SetActive(false); return; }
            var k = Keyboard.current; var pad = Gamepad.current;
            bool help = !MamaSettings.Open && ((k != null && k.hKey.isPressed) || (pad != null && pad.dpad.up.isPressed));
            if (!MamaSettings.Open && ((k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame))) overview = !overview;
            bool mini = MiniGame.I && MiniGame.I.Active;
            if (mini) help = false;
            hintVisible = help;
            for (int i = 0; i < 6; i++) gm.ui.SetGuide(i, false, Vector3.zero, null, null, Color.white);

            var p = gm.player;
            var held = p.Held;
            string title, body;
            if (held)
            {
                title = Loc.T(held.displayName);
                body = ItemMeaning(held);
            }
            else if (p.Target)
            {
                title = Loc.T(p.Target.displayName);
                body = TargetMeaning(p.Target);
            }
            else if (NearOilFire())
            {
                title = Text("OIL FIRE", "ไฟน้ำมัน");
                body = Text("Water makes it flare up. Look for a lid or a fire extinguisher.", "โดนน้ำแล้วไฟจะปะทุ ลองมองหาฝาหม้อ หรือถังดับเพลิง");
            }
            else
            {
                title = Text("Take a look around", "ลองสังเกตรอบตัว");
                body = Text("Water, smoke, things out of place. What will Mom notice?", "น้ำ ควัน ของผิดที่... แม่จะเห็นอะไรบ้าง?");
            }
            string keys = held ? Text("Q place  •  RMB throw  •  hold H: idea", "Q วาง  •  คลิกขวา ขว้าง  •  H ค้าง: คำใบ้") : Text("E interact  •  TAB notes  •  hold H: idea", "E ใช้ / หยิบ  •  TAB ภาพรวม  •  H ค้าง: คำใบ้");
            if (pad != null && k == null) keys = held ? "B place  •  Y throw  •  D-pad up: idea" : "A interact  •  View notes  •  D-pad up: idea";
            gm.ui.SetContext(title, body, keys, Room(p.transform.position));
            Idea(held, p.Target, out string ideaTitle, out string ideaBody);
            gm.ui.SetReadabilityPanels(overview && !MamaSettings.Open && !mini, help, ideaTitle, ideaBody);
            gm.ui.SetContextVisible(!MamaSettings.Open && !mini && !help && !overview && (held || p.Target || NearOilFire()));
            UpdateHome(held as MessyProp, help);
            UpdateFocus(p.Target, help);
            if (!mini && !held && !p.Target && Time.time >= thoughtAt && Time.timeSinceLevelLoad < 18f)
            {
                thoughtAt = Time.time + 60f;
                gm.ui.KidSay(Text("Wet socks... burnt dinner... Mom can't see this!", "ถุงเท้าเปียก... กลิ่นไหม้... แม่เห็นไม่ได้!"), 4f);
            }
        }

        public static string ItemMeaning(Pickup item)
        {
            if (item is CookPot pot) return pot.burn > 0.2f ? Text("Burnt evidence. A sink could clean it; a cupboard could conceal it.", "หม้อไหม้เป็นหลักฐาน อ่างล้างช่วยขัดได้ ตู้ก็อาจช่วยซ่อน") : Text("Clean again. It belongs on the kitchen stove.", "สะอาดแล้ว หม้อใบนี้อยู่บนเตาในครัว");
            if (item is Extinguisher ex) return Text("Hold LMB to spray. Leaves powder to clean. Charge: ", "คลิกซ้ายค้างเพื่อฉีด ทิ้งผงให้เก็บกวาด เหลือ: ") + Mathf.RoundToInt(ex.charge * 100) + "%";
            if (item is Lid) return Text("A lid cuts off air. Can it cover the pot before the flames grow?", "ฝาหม้อปิดกั้นอากาศ จะครอบหม้อทันก่อนไฟแรงไหม?");
            if (item is Mop) return Text("Hold LMB and walk to soak up floor water and scrub powder.", "คลิกซ้ายค้างแล้วเดิน ถูน้ำบนพื้นและเก็บผงสีขาวได้");
            if (item is Squeegee) return Text("Hold LMB to push water. It still needs somewhere to drain.", "คลิกซ้ายค้างเพื่อรีดน้ำ ต้องหาทางให้น้ำไหลลงท่อด้วย");
            if (item is Towel towel) return towel.IsWet ? Text("Wet cloth absorbs water. Small flames need air to keep burning.", "ผ้าเปียกช่วยซับน้ำ เปลวไฟเล็ก ๆ ต้องใช้อากาศในการเผาไหม้") : Text("Dry cloth can soak up puddles. Keep it away from flames.", "ผ้าแห้งซับน้ำได้ แต่ติดไฟได้เมื่อเข้าใกล้เปลวไฟ");
            if (item is Bucket bucket) return Text("Water: ", "น้ำ: ") + bucket.liters.ToString("0") + Text(" L. Q sets it down. Water makes oil fires flare up.", " ลิตร Q วางเบา ๆ น้ำทำให้ไฟน้ำมันปะทุ");
            if (item is Rug) return Text("Can cover a wet floor. Mom may feel the squish underfoot.", "บังพื้นเปียกได้ แต่แม่อาจเหยียบแล้วรู้สึกแฉะ");
            if (item is MessyProp teddy && teddy.displayName == "Teddy bear")
                return Text("Take it to the BEDROOM, follow HOME, then press Q by the pillow to return it.", "นำไปห้องนอน ตามหมุดปลายทาง แล้วกด Q วางบนเตียง ข้างหมอน");
            if (item is MessyProp mp && HasHomeCue(mp))
                return Text("Return to ", "นำกลับไป") + Room(mp.homePosition) + Text(". Follow HOME, stand by the green outline, then Q to finish.", " ตามหมุดปลายทาง เข้าใกล้เงาสีเขียว แล้วกด Q วางให้จบงาน");
            return Text("Move it, place it, or find another use for it.", "ย้าย วาง หรือลองใช้แก้ปัญหารอบตัว");
        }

        public static string TargetMeaning(Interactable target)
        {
            if (target is Pickup pick) return ItemMeaning(pick);
            if (target is Stove) return Text("The gas is feeding the heat. Turning it off won't remove smoke or burn marks.", "แก๊สเพิ่มความร้อน ปิดแล้วควันกับรอยไหม้ยังต้องจัดการต่อ");
            if (target is WaterTap) return Text("The tub is full. Stop the incoming water before cleaning the floor.", "อ่างเต็มแล้ว ถ้าน้ำยังไหล พื้นที่เช็ดก็จะเปียกอีก");
            if (target is MainValve) return Text("One valve controls every tap. The kitchen sink needs water too.", "วาล์วเดียวคุมน้ำทั้งบ้าน อ่างล้างหม้อก็ต้องใช้น้ำเหมือนกัน");
            if (target is Drain) return Text("Hair is blocking the drain. Water needs a clear way out.", "เศษผมขวางทางระบาย น้ำต้องมีทางไหลออก");
            if (target is Window) return Text("Fresh air in, smoky air out.", "เปิดทางให้อากาศใหม่เข้า และควันออก");
            if (target is Fan) return Text("Moves smoky air, and loose papers too.", "พัดควันได้ กระดาษที่วางอยู่ก็ปลิวได้เช่นกัน");
            if (target is SmokeAlarm) return Text("A warning, not the source. Something long could reach it.", "มันเตือนว่ามีควัน ของด้ามยาวน่าจะช่วยเอื้อมถึง");
            if (target is Cabinet) return Text("Out of sight isn't gone. Mom sometimes checks inside.", "ซ่อนได้ แต่ของยังอยู่ แม่อาจเปิดตรวจข้างใน");
            if (target is WaterDump) return Text("A place to fill, rinse or empty things, depending on what you carry.", "ใช้เติมน้ำ ล้าง หรือเทน้ำทิ้งได้ ขึ้นกับของในมือ");
            return Text("Move closer and read the action below.", "เข้าใกล้แล้วดูสิ่งที่ทำได้ด้านล่าง");
        }

        void Idea(Pickup held, Interactable target, out string title, out string body)
        {
            title = Text("A THOUGHT, NOT A TO-DO LIST", "ลองคิดดู... เลือกวิธีเองได้");
            if (held) { body = ItemMeaning(held); return; }
            bool fire = false; foreach (var source in FireSource.All) if (source && source.burning) { fire = true; break; }
            if (target is Stove || target is CookPot || fire)
            {
                body = Text("Oil fire: water makes it flare up. Cutting off air or using the extinguisher are different options. Look around the kitchen.", "ไฟน้ำมันโดนน้ำแล้วปะทุ ลองหาทางปิดกั้นอากาศ หรือใช้อุปกรณ์ดับเพลิง สำรวจของในครัวดู");
                return;
            }
            if (target) { body = TargetMeaning(target); return; }
            body = Text("Stop things getting worse, then deal with the traces. You can clean or conceal. Hold an object to learn about it; TAB shows the house's problems.", "ลองหยุดต้นเหตุ แล้วค่อยจัดการร่องรอย จะเก็บจริงหรือซ่อนก็ได้ หยิบของเพื่อดูว่ามันใช้ทำอะไร TAB ดูปัญหาในบ้าน");
        }

        void UpdateHome(MessyProp item, bool help)
        {
            bool valid = HasHomeCue(item);
            if (item != ghostItem)
            {
                if (homeGhost) Destroy(homeGhost);
                ghostItem = item;
                if (valid && ghostMaterial) CreateHomeGhost(item);
            }
            if (!valid) { if (homeGhost) homeGhost.SetActive(false); return; }
            bool sameRoom = RoomIndex(item.homePosition) == RoomIndex(gm.player.transform.position);
            if (homeGhost) homeGhost.SetActive(sameRoom || help);
            float distance = Vector3.Distance(gm.player.transform.position, item.homePosition);
            Vector3 where = item.homePosition + Vector3.up * 0.5f;
            // Keep a destination at the screen edge even before the player finds the room.
            gm.ui.SetGuide(5, true, where, "Q", (sameRoom ? Text("RETURN HERE", "เก็บที่นี่") : Room(item.homePosition)) + " · " + Mathf.CeilToInt(distance) + Text(" m", " ม."), MamaUI.Teal);
            if (TrashBin.For(gm.player)) gm.ui.SetPrompt(Text("[Q] Throw ", "[Q] ทิ้ง") + Loc.T(item.displayName) + Text(" in the bin", "ลงถังขยะ"));
            else if (item.NearHome(gm.player)) gm.ui.SetPrompt(Text("[Q] Return ", "[Q] วาง ") + Loc.T(item.displayName) + Text(" to its spot", " กลับเข้าที่"));
        }

        void CreateHomeGhost(MessyProp item)
        {
            homeGhost = new GameObject("Home outline - " + item.displayName);
            homeGhost.transform.SetPositionAndRotation(item.homePosition, item.homeRotation);
            homeGhost.transform.localScale = item.transform.lossyScale;
            foreach (var source in item.GetComponentsInChildren<MeshFilter>())
            {
                if (!source.sharedMesh) continue;
                var visible = source.GetComponent<Renderer>();
                if (visible && !visible.enabled) continue;
                var part = new GameObject("Outline"); part.transform.SetParent(homeGhost.transform, false);
                part.transform.localPosition = item.transform.InverseTransformPoint(source.transform.position);
                part.transform.localRotation = Quaternion.Inverse(item.transform.rotation) * source.transform.rotation;
                Vector3 scale = source.transform.lossyScale; Vector3 rootScale = item.transform.lossyScale;
                part.transform.localScale = new Vector3(scale.x / rootScale.x, scale.y / rootScale.y, scale.z / rootScale.z);
                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                var materials = new Material[source.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = ghostMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }

        void UpdateFocus(Interactable target, bool help)
        {
            if (MiniGame.I && MiniGame.I.Active) return;
            if (target && !gm.player.Held)
                gm.ui.SetGuide(2, true, target.InteractPoint + Vector3.up * 0.45f, "E", Loc.T(target.displayName), MamaUI.Teal);
            if (help && !gm.player.Held && ((gm.stove && gm.stove.on) || NearOilFire()))
            {
                var lid = FindFirstObjectByType<Lid>();
                var extinguisher = FindFirstObjectByType<Extinguisher>();
                if (lid && !lid.IsHeld) gm.ui.SetGuide(3, true, lid.transform.position + Vector3.up * 0.4f, "+", Text("Cuts off air", "ปิดกั้นอากาศ"), MamaUI.Teal);
                if (extinguisher && extinguisher.charge > 0f && !extinguisher.IsHeld) gm.ui.SetGuide(4, true, extinguisher.transform.position + Vector3.up * 0.7f, "+", Text("Fire extinguisher", "ถังดับเพลิง"), MamaUI.Teal);
            }
        }

        bool NearOilFire() => gm.pot && gm.pot.Fire && gm.pot.Fire.burning && Vector3.Distance(gm.player.transform.position, gm.pot.transform.position) < 4.5f;

        void OnDestroy()
        {
            if (homeGhost) Destroy(homeGhost);
            if (ghostMaterial) Destroy(ghostMaterial);
        }
    }
}
