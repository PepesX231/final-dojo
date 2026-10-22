using System;
using System.Collections.Generic;
using DreamRush.Core;
using DreamRush.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// "Upgrade Bed" (GDD §21.4): one row per upgrade with level pips, effect and cost. Buying deducts Dream
    /// Memory once, saves immediately, refreshes the bed's look, and applies from the next dream.
    /// </summary>
    public class BedUpgradePanel : ModalPanel
    {
        public static event Action Purchased;
        Text _memory;
        readonly List<(BedUpgrade id, Text level, Text cost, Button buy)> _rows = new List<(BedUpgrade, Text, Text, Button)>();

        protected override void BuildContent()
        {
            _memory = Paragraph("", 30, TextAnchor.MiddleCenter);
            _memory.fontStyle = FontStyle.Bold;
            Paragraph("Better sleep, steadier dreams. Upgrades work from your next dream.", 24, TextAnchor.MiddleCenter);
            Button first = null;
            foreach (var info in BedUpgrades.All)
            {
                var row = UIFactory.Row("Row_" + info.Id, Content, 120);
                var bg = UIFactory.Panel("Bg", row, UIFactory.PaperShade, UIFactory.RoundedSmall);
                UIFactory.Stretch(bg.rectTransform);
                var name = UIFactory.Label("Name", row, info.Name, 30, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Anchor(name.rectTransform, 0.03f, 0.62f, 0.5f, 0.95f);
                var effect = UIFactory.Label("Effect", row, info.Effect, 22, UIFactory.Ink, TextAnchor.UpperLeft);
                UIFactory.Anchor(effect.rectTransform, 0.03f, 0.62f, 0.08f, 0.5f);
                var level = UIFactory.Label("Level", row, "", 40, UIFactory.Ink, TextAnchor.MiddleCenter);
                UIFactory.Anchor(level.rectTransform, 0.6f, 0.72f, 0.1f, 0.9f);
                var id = info.Id;
                var buy = UIFactory.Button("Buy", row, "", () => Buy(id), 230, 72);
                var brt = buy.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.86f, 0.5f);
                brt.anchoredPosition = Vector2.zero;
                var cost = buy.GetComponentInChildren<Text>();
                _rows.Add((id, level, cost, buy));
                first ??= buy;
            }
            SetFirstSelectable(first);
            Refresh();
        }

        public bool Buy(BedUpgrade id)
        {
            var save = SaveManager.GetOrCreate();
            if (!BedUpgrades.TryBuy(save.Data.progression, id))
            {
                Audio.AudioManager.Play(Audio.Sfx.CardFail, Vector3.zero);
                return false;
            }
            save.Save();
            Audio.AudioManager.Play(Audio.Sfx.CardPick, Vector3.zero);
            UIRoot.Get().Toast(BedUpgrades.Get(id).Name + " upgraded!", 1.8f);
            Purchased?.Invoke();
            Refresh();
            return true;
        }

        void Refresh()
        {
            var p = SaveManager.GetOrCreate().Data.progression;
            _memory.text = $"DREAM MEMORY: {p.dreamMemory}";
            foreach (var (id, level, cost, buy) in _rows)
            {
                var info = BedUpgrades.Get(id);
                int lv = BedUpgrades.Level(p, id);
                string pips = "";
                for (int i = 0; i < info.MaxLevel; i++) pips += i < lv ? "<color=#E5293D>◆</color>" : "<color=#FAF0DC44>◆</color>";
                level.text = pips;
                int c = BedUpgrades.NextCost(p, id);
                cost.text = c < 0 ? "MAXED" : $"BUY  {c}";
                buy.interactable = BedUpgrades.CanBuy(p, id);
            }
        }
    }
}
