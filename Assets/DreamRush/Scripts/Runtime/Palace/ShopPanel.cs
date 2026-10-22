using DreamRush.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.Palace
{
    /// <summary>
    /// The Watchmaker's counter: spend LOST MINUTES on this run's upgrades. Every upgrade changes how you play
    /// (more moons = more mistakes allowed, stamina = longer combos/rolls, Dream Ink = more BURSTs, Sharper Leaf = faster kills).
    /// </summary>
    public class ShopPanel : ModalPanel
    {
        public struct Item { public string id, name, desc; public int cost, max; }
        public static readonly Item[] Items =
        {
            new Item { id = "moon", name = "EXTRA MOON", desc = "+1 พระจันทร์ (พลังชีวิต)", cost = 15, max = 2 },
            new Item { id = "stamina", name = "IRON STAMINA", desc = "+25 สตามินา — คอมโบ/กลิ้งได้นานขึ้น", cost = 10, max = 3 },
            new Item { id = "ink", name = "DREAM INK", desc = "เกจ DREAM ขึ้นเร็วขึ้น 40%", cost = 12, max = 2 },
            new Item { id = "leaf", name = "HEAVIER MITTS", desc = "หมัดแรงขึ้น 20%", cost = 14, max = 3 },
            new Item { id = "milk", name = "WARM MILK", desc = "เติมพระจันทร์ให้เต็ม", cost = 4, max = 99 },
        };

        Text _wallet; readonly System.Collections.Generic.List<(Item it, Text label, Button b)> _rows = new System.Collections.Generic.List<(Item, Text, Button)>();

        public static ShopPanel Open()
        {
            var p = Create<ShopPanel>("WATCHMAKER", new Vector2(1180, 880));
            UIRoot.Get().Push(p);
            return p;
        }

        protected override void BuildContent()
        {
            _wallet = Paragraph("", 30, TextAnchor.MiddleCenter);
            foreach (var it in Items)
            {
                var row = UIFactory.Row("Item_" + it.id, Content, 92);
                var label = UIFactory.Label("Label", row, "", 26, UIFactory.Ink, TextAnchor.MiddleLeft);
                UIFactory.Anchor(label.rectTransform, 0f, 0.68f); label.supportRichText = true;
                var item = it;
                var b = UIFactory.Button("Buy", row, "BUY", () => BuyClick(item), 220, 70);
                UIFactory.Anchor((RectTransform)b.transform, 0.72f, 1f, 0.12f, 0.88f);
                _rows.Add((it, label, b));
            }
            Refresh();
        }

        /// <summary>Applies an upgrade (also used by tests). Returns false when it can't be afforded / is maxed.</summary>
        public static bool Buy(Item it)
        {
            var pd = PalaceDirector.Current; if (pd == null) return false;
            if (pd.Upgrade(it.id) >= it.max || !pd.Spend(it.cost)) { Audio.AudioManager.Play(Audio.Sfx.CardFail, Vector3.zero, 0.6f); return false; }
            pd.AddUpgrade(it.id);
            var st = Dream.SleepStability.Current; var c = Combat.PlayerCombat.Current;
            switch (it.id)
            {
                case "moon": if (st != null) st.AddMax(1); break;
                case "stamina": if (c != null) c.maxStamina += 25f; break;
                case "ink": if (c != null) c.meterGainMul += 0.4f; break;
                case "leaf": if (c != null) c.damageMul += 0.2f; break;
                case "milk": if (st != null) st.Heal(); break;
            }
            Audio.AudioManager.Play(Audio.Sfx.Coin, Vector3.zero, 0.9f);
            Presentation.VFX.Play(Presentation.VFXKind.CardBurst, Player.PlayerMotor3D.Instance != null ? Player.PlayerMotor3D.Instance.transform.position + Vector3.up : Vector3.zero, 0.8f);
            return true;
        }

        void BuyClick(Item it) { Buy(it); Refresh(); }
        public static bool BuyById(string id) { foreach (var it in Items) if (it.id == id) return Buy(it); return false; }

        void Refresh()
        {
            var pd = PalaceDirector.Current; int m = pd != null ? pd.Minutes : 0;
            _wallet.text = $"LOST MINUTES ⏱ {m}";
            foreach (var (it, label, b) in _rows)
            {
                int lv = pd != null ? pd.Upgrade(it.id) : 0;
                bool maxed = lv >= it.max;
                label.text = $"<b>{it.name}</b>  {(it.max < 99 ? $"[{lv}/{it.max}]" : "")}\n<size=20>{it.desc}</size>";
                var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = maxed ? "MAX" : $"⏱ {it.cost}";
                b.interactable = !maxed && m >= it.cost;
            }
        }
    }
}
