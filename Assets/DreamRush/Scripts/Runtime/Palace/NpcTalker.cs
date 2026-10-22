using DreamRush.Interaction;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// A Clockfolk resident (the city's sleepers, stuck at 11:59). What they do depends on the room:
    ///   FLAT → gossip + a hint about the level (where the hours are, how to beat an enemy) + 2 minutes, once
    ///   WATCHMAKER → opens the upgrade shop · CAFE → rest (moons back + respawn here) · DOJO → explains the trial
    ///   LIBRARY → draws the hour compass on your HUD (arrows to every remaining stolen hour)
    /// </summary>
    public class NpcTalker : Interactable
    {
        public string npcName = "CLOCKFOLK";
        public string[] lines = { "..." };
        public RoomType role = RoomType.Flat;
        BuildingDoor _door;
        public int Talks { get; private set; }

        static readonly string[] Names = { "TICK-TILLY", "OLD MR. GONG", "LITTLE SECOND", "MADAME CUCKOO", "PENDULUM PETE", "NANA NOON", "SNOOZY SAM", "MISS MINUTE", "BARON BELL", "DOZY DOT" };
        static readonly string[][] Gossip =
        {
            new[] { "ซู่ว… เธอก็ฝันอยู่เหมือนกันเหรอ?", "เมืองนี้หยุดอยู่ที่ 11:59 มาตั้งแต่ COUNT MIDNIGHT ขโมยชั่วโมงไปสามชิ้น", "ชิ้นนึงอยู่บนหลังคาฝั่งตะวันออก ต้องไถรางเหลืองไปเรื่อยๆ ถึงจะถึง" },
            new[] { "ระวังเจ้า BELL BRUTE นะ ต่อยจากข้างหน้ามันกันได้หมด", "อ้อมไปข้างหลัง หรือกดค้างชาร์จฟาดลงไปเลย!", "แล้วตอนมันทุบพื้น ให้กระโดดข้ามวงคลื่นนะ" },
            new[] { "เจ้านกกุ๊กกูบนหลังคายิงขนนกมาใส่ฉันทั้งคืน…", "ได้ยินว่าถ้าต่อยขนนกตอนที่มันบินมา มันจะเด้งกลับไปหาเจ้าของ!" },
            new[] { "เฟืองหนามกลิ้งไล่ฉันชนกำแพงเมื่อกี้", "แต่มันชนเองนะ แล้วก็มึนไปพักนึง ตอนนั้นแหละตีแรงๆ เลย" },
            new[] { "ฉันเก็บนาทีที่ทำหล่นไว้ใต้หมอน… เอาไปใช้สิ", "ร้านช่างนาฬิกา (ประตูสีทอง) แลกนาทีเป็นของดีได้นะ" },
            new[] { "ทางเหนือมีคลองแก้วกับเฟืองหมุนสูงเสียดฟ้า", "ปีนขึ้นไปให้ถึงยอด ชั่วโมงที่หายไปรออยู่บนนั้น" },
            new[] { "ลานน้ำพุฝั่งตะวันตกเงียบผิดปกติ…", "ใครเดินเข้าไปก็โดนเงาซุ่มโจมตี แต่ถ้าชนะ ชั่วโมงจะปรากฏตรงกลางน้ำพุ" },
            new[] { "ห้องสมุดนาฬิกา (ประตูสีฟ้า) มีบรรณารักษ์วาดแผนที่ให้", "แผนที่จะชี้ทางไปหาชั่วโมงที่ยังไม่เจอ" },
            new[] { "เคล็ดลับ: หลบให้พอดีจังหวะ แล้วสวนกลับทันที จะแรงเป็นสองเท่า!", "ถ้าเกจชมพูเต็ม กด V ปล่อย DREAM BURST ได้เลย" },
            new[] { "คาเฟ่แสงจันทร์ (ประตูชมพู) ให้นั่งพักได้ พระจันทร์จะเต็มอีกครั้ง", "แล้วคาเฟ่ก็เป็นจุดเซฟด้วยนะ" },
            new[] { "รู้ไหม หอนาฬิกาตรงกลางเมืองคือที่อยู่ของ COUNT MIDNIGHT", "เขาไม่ได้ร้ายหรอก… แค่ไม่อยากให้คืนนี้จบ" },
            new[] { "ตู้กดน้ำข้างถนน ลองต่อยดูสิ บางทีนาทีที่ติดอยู่ข้างในจะร่วงออกมา" },
        };

        protected override void OnEnable() { base.OnEnable(); prompt = "TALK"; }

        public void Configure(RoomType type, BuildingDoor door, System.Random rnd)
        {
            role = type; _door = door;
            switch (type)
            {
                case RoomType.Watchmaker: npcName = "WATCHMAKER GEARHART"; lines = new[] { "ยินดีต้อนรับสู่ร้านช่างนาฬิกา!", "เอานาทีที่หายไปมาแลกของดีๆ ได้นะ เลือกเอาเลย" }; prompt = "SHOP"; break;
                case RoomType.Cafe: npcName = "BARISTA LUNA"; lines = new[] { "นั่งพักก่อนสิ นมอุ่นๆ สักแก้วไหม", "(พระจันทร์ของเธอเต็มแล้ว — คาเฟ่นี้เป็นจุดเซฟใหม่)" }; prompt = "REST"; break;
                case RoomType.Dojo: npcName = "SENSEI SECOND-HAND"; lines = new[] { "นี่คือโดโจเงา ตีฆ้องเมื่อพร้อม", "ชนะทุกระลอก แล้วฉันจะให้นาทีกองโต", "จำไว้: หลบให้พอดี แล้วสวนกลับ!" }; break;
                case RoomType.Library: npcName = "LIBRARIAN TOMEWICK"; lines = new[] { "ชู่… ห้องสมุดนาฬิกา", "ชั่วโมงที่ถูกขโมย 3 ชิ้น ฉันวาดเข็มทิศให้เธอแล้ว", "ดูลูกศรบนจอ มันจะชี้ไปหาชั่วโมงที่ยังเหลืออยู่" }; break;
                default:
                    npcName = Names[rnd.Next(Names.Length)];
                    lines = Gossip[rnd.Next(Gossip.Length)];
                    break;
            }
        }

        protected override void OnInteract(InteractionContext context)
        {
            Talks++;
            Audio.AudioManager.Play(Audio.Sfx.UiOpen, transform.position, 0.6f);
            var anim = GetComponent<ClockfolkLook>(); if (anim != null) anim.Bounce();
            DialoguePanel.Show(npcName, lines, AfterTalk);
        }

        void AfterTalk()
        {
            var pd = PalaceDirector.Current; string key = _door != null ? "door:" + _door.id : null;
            switch (role)
            {
                case RoomType.Watchmaker: ShopPanel.Open(); if (pd != null) pd.Take(key); break;
                case RoomType.Cafe:
                {
                    var st = Dream.SleepStability.Current; int got = st != null ? st.Heal() : 0;
                    if (pd != null && _door != null && _door.outside != null) pd.SetRespawn(_door.outside.position);
                    UIRoot.Get().Toast(got > 0 ? $"พักผ่อนแล้ว — พระจันทร์ +{got}" : "พักผ่อนแล้ว — จุดเซฟ: คาเฟ่นี้", 1.8f);
                    Audio.AudioManager.Play(Audio.Sfx.SleepChime, transform.position, 0.8f);
                    if (pd != null) pd.Take(key);
                    break;
                }
                case RoomType.Library:
                    if (pd != null) { pd.HourCompass = true; pd.Take(key); }
                    UIRoot.Get().Toast("ได้เข็มทิศชั่วโมงแล้ว! ลูกศรบนจอชี้ไปหาชั่วโมงที่เหลือ", 2.2f);
                    break;
                case RoomType.Flat:
                    if (pd != null && !pd.Taken(key))
                    {
                        pd.Take(key);
                        LostMinute.Burst(transform.position + transform.forward * 0.8f, 2);
                        UIRoot.Get().Toast("ได้รับ LOST MINUTE ×2", 1.4f);
                    }
                    break;
            }
        }
    }
}
