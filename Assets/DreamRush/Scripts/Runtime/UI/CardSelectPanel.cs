using System;
using System.Collections;
using System.Collections.Generic;
using DreamRush.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Persona 5 × Cognitive Solution Tarot Three-Card Selection UI (GDD §17.1, §17.4):
    /// Obsidian + Crimson + Rebel Yellow high-contrast Tarot frames, Ransom-Note header,
    /// explicit Problem-Solving Property Tags, and sharp kinetic selection feedback.
    /// </summary>
    public class CardSelectPanel : MonoBehaviour
    {
        public CardDefinition Chosen { get; private set; }
        public bool Done { get; private set; }
        /// <summary>The player asked for a new hand (Lucky Pillow). Panel closes with no choice.</summary>
        public bool Rerolled { get; private set; }
        int _rerolls;
        readonly List<RectTransform> _cards = new List<RectTransform>();
        readonly List<Image> _cardBorders = new List<Image>();
        readonly List<Image> _cardHeaders = new List<Image>();
        List<CardDefinition> _options;
        int _highlight;

        readonly List<RectTransform> _bursts = new List<RectTransform>();
        readonly List<RectTransform> _sheens = new List<RectTransform>();
        readonly List<float> _dealT = new List<float>();
        readonly List<float> _restY = new List<float>();
        RectTransform _chronoGear;
        Text _solutionHint;
        float _openTime;

        public static CardSelectPanel Show(List<CardDefinition> options, int rerolls = 0)
        {
            var root = UIFactory.Stretch(UIFactory.Rect("CardSelect", UIRoot.Get().Overlay));
            root.SetAsLastSibling();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.02f, 0.06f, 0.99f);
            if (TitleScreen.Current != null)
                TitleScreen.Current.gameObject.SetActive(false);
            var p = root.gameObject.AddComponent<CardSelectPanel>();
            p._rerolls = rerolls;
            p.Build(options);
            Audio.AudioManager.Play(Audio.Sfx.CardDeal, Vector3.zero);
            return p;
        }

        static Color CategoryColor(CardDefinition c) => c.category switch
        {
            CardCategory.Tool => UIStyle.Red,
            CardCategory.Transformation => UIStyle.RebelYellow,
            CardCategory.Spawn => UIStyle.Cyan,
            _ => UIStyle.Lavender
        };

        static stringArcana RomanArcana(string id) => id switch
        {
            "SuperGlue" => new stringArcana("ARCANA I  ◆  THE ANCHOR", "LOGIC RULE: MOTION LOCK", "ตรึงวัตถุที่เคลื่อนที่ให้อยู่กับที่ เพื่อเปิดจังหวะแก้ปริศนา"),
            "CameraFlash" => new stringArcana("ARCANA II  ◆  THE SHUTTER", "COUNTER-PLAY: SIGHT FREEZE", "หยุดการรับรู้ของเป้าหมายชั่วขณะด้วยแสงแฟลชความเข้มสูง"),
            "FishingRod" => new stringArcana("ARCANA III  ◆  THE HOOK", "SPATIAL RULE: VECTOR PULL", "ดึงเป้าหมายจากระยะไกลเข้ามาหาตัว ย่นระยะทางแก้ปัญหา"),
            "BeAGrandpa" => new stringArcana("ARCANA IV  ◆  THE ELDER", "SOCIAL RULE: RESPECT CODE", "เปลี่ยนสถานะผู้เล่น ใช้กฎมารยาทสังคมบังคับให้เก้าอี้ยอมจำนน"),
            "DecoyApple" => new stringArcana("ARCANA V  ◆  THE MIRAGE", "DECOY LOGIC: THREAT LURE", "สร้างตัวล่อดึงดูดความสนใจ เพื่อเบี่ยงเส้นทางหนีของเป้าหมาย"),
            "BouncyCushion" => new stringArcana("ARCANA VI  ◆  THE SPRING", "PHYSICS RULE: VERTICAL VECTOR", "สร้างจุดดีดตัวแนวตั้ง เปิดเส้นทางลัดข้ามสิ่งกีดขวางมุมสูง"),
            _ => new stringArcana("ARCANA 0  ◆  THE FOOL", "DREAM LOGIC: ADAPTIVE", "ปรับเปลี่ยนกฎความฝันเพื่อสร้างทางออกใหม่")
        };

        readonly struct stringArcana
        {
            public readonly string Arcana;
            public readonly string PropertyTag;
            public readonly string ThaiTactic;
            public stringArcana(string a, string p, string t) { Arcana = a; PropertyTag = p; ThaiTactic = t; }
        }

        void Build(List<CardDefinition> options)
        {
            _options = options;
            _openTime = Time.unscaledTime;

            // 1. Persona 5 × Chrono-Punk Backdrop: Halftone, Concentric Swirl, Rotating Gear, and Crimson Slash Bands
            if (UIStyle.Halftone != null)
                UIFactory.Stretch(UIStyle.Pattern("Halftone", transform, UIStyle.Halftone, new Color(1f, 1f, 1f, 0.055f), 1.5f).rectTransform);

            var swirl = UIStyle.Image("BackdropSwirl", transform, UIStyle.SwirlRing, new Color(UIStyle.Red.r, UIStyle.Red.g, UIStyle.Red.b, 0.16f), false);
            swirl.rectTransform.sizeDelta = new Vector2(1400, 1400);
            swirl.rectTransform.anchoredPosition = new Vector2(420, 40);

            var gear = UIStyle.Image("ChronoGearBg", transform, UIStyle.ChronoGear, new Color(1f, 0.82f, 0.15f, 0.12f), false);
            _chronoGear = gear.rectTransform;
            _chronoGear.sizeDelta = new Vector2(920, 920);
            _chronoGear.anchoredPosition = new Vector2(-580, 220);

            var bandCrimson = UIStyle.Image("BandCrimson", transform, UIStyle.Slash, UIStyle.Red);
            bandCrimson.rectTransform.sizeDelta = new Vector2(3600, 420);
            bandCrimson.rectTransform.localRotation = Quaternion.Euler(0, 0, -9.5f);
            bandCrimson.rectTransform.anchoredPosition = new Vector2(0, -30);

            var bandObsidian = UIStyle.Image("BandObsidian", transform, UIStyle.Slash, UIStyle.Obsidian);
            bandObsidian.rectTransform.sizeDelta = new Vector2(3600, 95);
            bandObsidian.rectTransform.localRotation = Quaternion.Euler(0, 0, -9.5f);
            bandObsidian.rectTransform.anchoredPosition = new Vector2(0, 145);

            var bandYellow = UIStyle.Image("BandYellow", transform, UIStyle.Slash, UIStyle.RebelYellow);
            bandYellow.rectTransform.sizeDelta = new Vector2(3600, 26);
            bandYellow.rectTransform.localRotation = Quaternion.Euler(0, 0, -9.5f);
            bandYellow.rectTransform.anchoredPosition = new Vector2(0, -210);

            // 2. Top-Left Ransom-Note Header + Cognitive Problem-Solving Banner
            var headerHolder = UIFactory.Rect("HeaderHolder", transform);
            headerHolder.anchorMin = headerHolder.anchorMax = new Vector2(0, 1);
            headerHolder.pivot = new Vector2(0, 1);
            headerHolder.sizeDelta = new Vector2(1280, 180);
            headerHolder.anchoredPosition = new Vector2(48, -22);
            headerHolder.localRotation = Quaternion.Euler(0, 0, 2.2f);

            var headerBack = UIStyle.Image("HeaderBack", headerHolder, UIStyle.Slab, UIStyle.Obsidian);
            headerBack.rectTransform.sizeDelta = new Vector2(960, 118);
            headerBack.rectTransform.anchorMin = headerBack.rectTransform.anchorMax = new Vector2(0, 1);
            headerBack.rectTransform.pivot = new Vector2(0, 1);
            headerBack.rectTransform.anchoredPosition = new Vector2(0, -8);
            UIStyle.HardShadow(headerBack, UIStyle.Red, new Vector2(12, -12));

            var ransomTitle = UIStyle.BuildRansomWord("RansomTitle", headerHolder, "SOLUTION TAROT", 50, UIStyle.Red, true);
            ransomTitle.anchorMin = ransomTitle.anchorMax = new Vector2(0, 1);
            ransomTitle.pivot = new Vector2(0, 0.5f);
            ransomTitle.anchoredPosition = new Vector2(28, -64);

            var subSlab = UIStyle.Image("SubSlab", transform, UIStyle.Slab, UIStyle.Red);
            subSlab.rectTransform.anchorMin = subSlab.rectTransform.anchorMax = new Vector2(0, 1);
            subSlab.rectTransform.pivot = new Vector2(0, 1);
            subSlab.rectTransform.sizeDelta = new Vector2(860, 48);
            subSlab.rectTransform.anchoredPosition = new Vector2(72, -148);
            subSlab.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.2f);
            UIStyle.HardShadow(subSlab, UIStyle.Obsidian, new Vector2(6, -6));

            var sub = UIStyle.Body("Sub", subSlab.rectTransform, "COGNITIVE TOOLSET  ◆  เลือกเครื่องมือแก้ปัญหา 1 ชิ้นเพื่อใช้พลิกสถานการณ์ในฝัน", 21, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(sub.rectTransform);
            sub.rectTransform.offsetMin = new Vector2(24, 0);

            // Top-Right Cognitive Protocol Badge
            var protocolTag = UIStyle.Tag("ProtocolBadge", transform, "STEP 02 // FORMULATE STRATEGY", 20, UIStyle.Obsidian, UIStyle.RebelYellow, new Vector2(420, 48));
            protocolTag.anchorMin = protocolTag.anchorMax = new Vector2(1, 1);
            protocolTag.pivot = new Vector2(1, 1);
            protocolTag.anchoredPosition = new Vector2(-46, -34);
            protocolTag.localRotation = Quaternion.Euler(0, 0, -2.5f);

            // 3. Build the 3 Persona Tarot / Cognitive Solution Cards
            for (int i = 0; i < options.Count; i++)
            {
                int idx = i;
                var c = options[i];
                var col = CategoryColor(c);
                var arcana = RomanArcana(c.cardId);

                var holder = UIFactory.Rect($"Card_{i}", transform);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = new Vector2(440, 650);
                holder.anchoredPosition = new Vector2((i - (options.Count - 1) * 0.5f) * 500f, -28f);

                // Spiky Persona Burst behind active card
                var burst = UIStyle.Image("Burst", holder, UIStyle.Burst, new Color(col.r, col.g, col.b, 0.92f), false);
                burst.rectTransform.sizeDelta = new Vector2(860, 860);
                _bursts.Add(burst.rectTransform);

                // Outer Obsidian Tarot Slab with Crimson Offset Drop Shadow
                var outerShadow = UIStyle.Image("TarotShadow", holder, UIStyle.Slab, UIStyle.Red);
                UIFactory.Stretch(outerShadow.rectTransform, -8);
                outerShadow.rectTransform.anchoredPosition = new Vector2(14, -16);

                var outerFrame = UIStyle.Image("TarotObsidian", holder, UIStyle.Slab, UIStyle.Obsidian);
                UIFactory.Stretch(outerFrame.rectTransform);
                _cardBorders.Add(outerFrame);

                // Inner Cream/White Sharp Graphic Cut Card Face
                var card = UIFactory.Panel("Face", holder, UIStyle.Cream, UIStyle.Cut ?? UIFactory.Rounded);
                var rt = card.rectTransform;
                UIFactory.Stretch(rt, 14);

                // Top Arcana Roman Header Strip
                var arcanaBar = UIStyle.Image("ArcanaBar", rt, UIStyle.Slab, UIStyle.Obsidian);
                arcanaBar.rectTransform.anchorMin = new Vector2(0, 1);
                arcanaBar.rectTransform.anchorMax = new Vector2(1, 1);
                arcanaBar.rectTransform.pivot = new Vector2(0.5f, 1);
                arcanaBar.rectTransform.sizeDelta = new Vector2(-20, 38);
                arcanaBar.rectTransform.anchoredPosition = new Vector2(0, -10);
                _cardHeaders.Add(arcanaBar);

                var arcanaTxt = UIStyle.Hud("ArcanaText", arcanaBar.rectTransform, arcana.Arcana, 16, UIStyle.RebelYellow, TextAnchor.MiddleRight);
                UIFactory.Stretch(arcanaTxt.rectTransform);
                arcanaTxt.rectTransform.offsetMax = new Vector2(-16, 0);

                // Art Window: High-Contrast Graphic Frame + Diagonal Halftone + Swirl + Icon + Glossy Sheen
                var artBorder = UIStyle.Image("ArtBorder", rt, UIStyle.Cut, UIStyle.Obsidian);
                artBorder.rectTransform.anchorMin = new Vector2(0, 1);
                artBorder.rectTransform.anchorMax = new Vector2(1, 1);
                artBorder.rectTransform.pivot = new Vector2(0.5f, 1);
                artBorder.rectTransform.sizeDelta = new Vector2(-34, 272);
                artBorder.rectTransform.anchoredPosition = new Vector2(0, -54);

                var art = UIStyle.Image("ArtBg", artBorder.rectTransform, null, col);
                UIFactory.Stretch(art.rectTransform, 6);
                art.gameObject.AddComponent<RectMask2D>();

                if (UIStyle.Stripes != null)
                    UIFactory.Stretch(UIStyle.Pattern("Stripes", art.rectTransform, UIStyle.Stripes, new Color(0f, 0f, 0f, 0.18f), 0.9f).rectTransform);

                var ring = UIStyle.Image("Ring", art.rectTransform, UIStyle.SwirlRing, new Color(1f, 1f, 1f, 0.24f), false);
                ring.rectTransform.sizeDelta = new Vector2(310, 310);

                if (c.icon != null)
                {
                    var icShadow = UIFactory.Rect("ArtShadow", art.rectTransform);
                    icShadow.sizeDelta = new Vector2(246, 246);
                    icShadow.anchoredPosition = new Vector2(10, -10);
                    var rawSh = icShadow.gameObject.AddComponent<RawImage>();
                    rawSh.texture = c.icon;
                    rawSh.color = new Color(0f, 0f, 0f, 0.65f);
                    rawSh.raycastTarget = false;

                    var ic = UIFactory.Rect("Art", art.rectTransform);
                    ic.sizeDelta = new Vector2(246, 246);
                    var raw = ic.gameObject.AddComponent<RawImage>();
                    raw.texture = c.icon;
                    raw.raycastTarget = false;
                }

                var sheen = UIStyle.Image("Sheen", art.rectTransform, null, new Color(1f, 1f, 1f, 0.42f));
                sheen.sprite = UIFactory.RoundedSmall;
                sheen.rectTransform.sizeDelta = new Vector2(76, 700);
                sheen.rectTransform.localRotation = Quaternion.Euler(0, 0, -25f);
                sheen.rectTransform.anchoredPosition = new Vector2(-600, 0);
                _sheens.Add(sheen.rectTransform);

                // Category Tag Overhanging Top-Left Corner
                string catName = c.category.ToString().ToUpperInvariant();
                Color catBg = c.category == CardCategory.Tool ? UIStyle.Red : UIStyle.Obsidian;
                Color catFg = c.category == CardCategory.Tool ? Color.white : UIStyle.RebelYellow;
                var tag = UIStyle.Tag("Category", holder, catName, 21, catBg, catFg, new Vector2(catName.Length * 15 + 78, 44));
                tag.anchorMin = tag.anchorMax = new Vector2(0, 1);
                tag.pivot = new Vector2(0, 0.5f);
                tag.anchoredPosition = new Vector2(-14, -44);
                tag.localRotation = Quaternion.Euler(0, 0, 5.5f);

                // Problem-Solving Property Sticker (Diagonal Slash across bottom of Art Window)
                var propSticker = UIStyle.Image("PropertySticker", rt, UIStyle.Slab, UIStyle.RebelYellow);
                propSticker.rectTransform.anchorMin = new Vector2(0.04f, 1);
                propSticker.rectTransform.anchorMax = new Vector2(0.96f, 1);
                propSticker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                propSticker.rectTransform.sizeDelta = new Vector2(0, 36);
                propSticker.rectTransform.anchoredPosition = new Vector2(0, -324);
                propSticker.rectTransform.localRotation = Quaternion.Euler(0, 0, -2.2f);
                UIStyle.HardShadow(propSticker, UIStyle.Obsidian, new Vector2(5, -5));

                var propText = UIStyle.Hud("PropertyText", propSticker.rectTransform, arcana.PropertyTag, 17, UIStyle.Obsidian, TextAnchor.MiddleCenter);
                UIFactory.Stretch(propText.rectTransform, 6);

                // Card Title (Bold Italic Persona Slab)
                var name = UIStyle.Title("Name", rt, c.displayName.ToUpperInvariant(), 40, UIStyle.Obsidian, TextAnchor.MiddleCenter, redDrop: false);
                Destroy(name.GetComponent<Outline>());
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 0);
                name.rectTransform.sizeDelta = new Vector2(-36, 52);
                name.rectTransform.anchoredPosition = new Vector2(0, 224);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 24;
                name.resizeTextMaxSize = 40;

                var rule = UIStyle.Image("Rule", rt, UIStyle.Slash, UIStyle.Red);
                rule.rectTransform.anchorMin = new Vector2(0.08f, 0);
                rule.rectTransform.anchorMax = new Vector2(0.92f, 0);
                rule.rectTransform.sizeDelta = new Vector2(0, 6);
                rule.rectTransform.anchoredPosition = new Vector2(0, 194);

                // Primary English Description + Thai Problem-Solving Tactic
                var desc = UIStyle.Body("Desc", rt, c.description, 22, UIStyle.Obsidian, TextAnchor.UpperCenter);
                desc.rectTransform.anchorMin = new Vector2(0, 0);
                desc.rectTransform.anchorMax = new Vector2(1, 0);
                desc.rectTransform.sizeDelta = new Vector2(-48, 66);
                desc.rectTransform.anchoredPosition = new Vector2(0, 148);

                var tacticBox = UIStyle.Image("TacticBox", rt, UIStyle.Cut, new Color(0.08f, 0.06f, 0.12f, 0.96f));
                tacticBox.rectTransform.anchorMin = new Vector2(0.06f, 0);
                tacticBox.rectTransform.anchorMax = new Vector2(0.94f, 0);
                tacticBox.rectTransform.sizeDelta = new Vector2(0, 54);
                tacticBox.rectTransform.anchoredPosition = new Vector2(0, 92);

                var tacticTxt = UIStyle.Body("TacticText", tacticBox.rectTransform, arcana.ThaiTactic, 17, UIStyle.Cream, TextAnchor.MiddleCenter);
                UIFactory.Stretch(tacticTxt.rectTransform, 8);

                // Bottom Hotkey Badge + Recharge Spec
                var key = UIStyle.Key(rt, (i + 1).ToString(), 44);
                key.anchorMin = key.anchorMax = new Vector2(0, 0);
                key.anchoredPosition = new Vector2(54, 36);

                string targetInfo = c.targetMode == CardTargetMode.Target ? $"RANGE {c.range:0}M  ◆  " : "";
                var cd = UIStyle.Hud("Cooldown", rt, $"{targetInfo}CD {c.cooldown:0}S", 20, UIStyle.Red, TextAnchor.MiddleRight);
                cd.rectTransform.anchorMin = new Vector2(0, 0);
                cd.rectTransform.anchorMax = new Vector2(1, 0);
                cd.rectTransform.sizeDelta = new Vector2(-56, 36);
                cd.rectTransform.anchoredPosition = new Vector2(0, 36);

                var hit = holder.gameObject.AddComponent<Image>();
                hit.color = new Color(1, 1, 1, 0);
                var btn = holder.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = hit;
                btn.onClick.AddListener(() => Pick(idx));

                var trigger = holder.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    if (_highlight != idx) Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero, 0.35f);
                    _highlight = idx;
                });
                trigger.triggers.Add(enter);

                _cards.Add(holder);
                _dealT.Add(-0.5f);
                _restY.Add(-28f);
            }

            _highlight = Mathf.Clamp(options.Count / 2, 0, options.Count - 1);

            // Bottom Solution Analysis Bar + Input Prompts
            var bottomBar = UIStyle.Image("BottomAnalysis", transform, UIStyle.Slab, UIStyle.Obsidian);
            bottomBar.rectTransform.anchorMin = bottomBar.rectTransform.anchorMax = new Vector2(0.5f, 0);
            bottomBar.rectTransform.pivot = new Vector2(0.5f, 0);
            bottomBar.rectTransform.sizeDelta = new Vector2(1140, 56);
            bottomBar.rectTransform.anchoredPosition = new Vector2(-130, 26);
            UIStyle.HardShadow(bottomBar, UIStyle.Red, new Vector2(6, -6));

            _solutionHint = UIStyle.Body("SolutionHint", bottomBar.rectTransform, "", 20, UIStyle.RebelYellow, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_solutionHint.rectTransform);
            _solutionHint.rectTransform.offsetMin = new Vector2(28, 0);
            _solutionHint.rectTransform.offsetMax = new Vector2(-24, 0);

            var hint = UIFactory.Rect("Hints", transform);
            hint.anchorMin = hint.anchorMax = new Vector2(1, 0);
            hint.pivot = new Vector2(1, 0);
            hint.anchoredPosition = new Vector2(-46, 30);
            hint.sizeDelta = new Vector2(540, 52);
            var hl = UIStyle.Hud("Text", hint, "LOCK IN CARD   ◆   [1 / 2 / 3]  OR  CLICK", 23, UIStyle.Cream, TextAnchor.MiddleRight);
            UIFactory.Stretch(hl.rectTransform);

            if (_rerolls > 0)
            {
                var rb = UIFactory.Button("Reroll", transform, $"LUCKY PILLOW: REROLL  [R]   ({_rerolls})", Reroll, 480, 52);
                var rrt = rb.GetComponent<RectTransform>();
                rrt.anchorMin = rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(1, 1);
                rrt.anchoredPosition = new Vector2(-46, -92);
            }

            AnimateCards();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Reroll()
        {
            if (Chosen != null || Rerolled || _rerolls <= 0) return;
            Rerolled = true;
            Audio.AudioManager.Play(Audio.Sfx.CardDeal, Vector3.zero);
            Done = true;
            if (TitleScreen.Current != null && !TitleScreen.Current.gameObject.activeSelf)
                TitleScreen.Current.gameObject.SetActive(true);
            Destroy(gameObject);
        }

        void Update()
        {
            if (Done || Chosen != null) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Pick(0);
                else if (kb.digit2Key.wasPressedThisFrame) Pick(1);
                else if (kb.digit3Key.wasPressedThisFrame) Pick(2);
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) _highlight = Mathf.Max(0, _highlight - 1);
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) _highlight = Mathf.Min(_cards.Count - 1, _highlight + 1);
                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Pick(_highlight);
                if (kb.rKey.wasPressedThisFrame) Reroll();
            }
            if (gp != null)
            {
                if (gp.dpad.left.wasPressedThisFrame) _highlight = Mathf.Max(0, _highlight - 1);
                if (gp.dpad.right.wasPressedThisFrame) _highlight = Mathf.Min(_cards.Count - 1, _highlight + 1);
                if (gp.buttonSouth.wasPressedThisFrame) Pick(_highlight);
                if (gp.buttonNorth.wasPressedThisFrame) Reroll();
            }
            AnimateCards();
        }

        void AnimateCards()
        {
            float now = Time.unscaledTime - _openTime;
            float lerp = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            if (_chronoGear != null) _chronoGear.localRotation = Quaternion.Euler(0, 0, -now * 16f);

            if (_solutionHint != null && _highlight >= 0 && _highlight < _options.Count)
            {
                var c = _options[_highlight];
                var arc = RomanArcana(c.cardId);
                _solutionHint.text = $"ACTIVE HYPOTHESIS: [{c.displayName.ToUpperInvariant()}]  ▸  {arc.PropertyTag} — {arc.ThaiTactic}";
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                bool hot = i == _highlight;
                float deal = UIStyle.OutBack((now - _dealT[i]) / 0.42f, 1.3f);
                float baseX = (i - (_cards.Count - 1) * 0.5f) * 500f;
                float targetY = hot ? 16f : -36f;
                _restY[i] = Mathf.Lerp(_restY[i], targetY, lerp);
                _cards[i].anchoredPosition = new Vector2(baseX, Mathf.LerpUnclamped(-1200f, _restY[i], Mathf.Max(-0.2f, deal)));
                float target = hot ? 1.11f : 0.93f;
                float s = Mathf.Lerp(_cards[i].localScale.x, target, lerp);
                _cards[i].localScale = Vector3.one * s;
                float fan = (i - (_cards.Count - 1) * 0.5f) * -4.2f;
                _cards[i].localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(fan + 30f * (i - 1), hot ? fan * 0.15f : fan, Mathf.Clamp01(deal)));
                if (hot) _cards[i].SetAsLastSibling();

                if (i < _cardBorders.Count && _cardBorders[i] != null)
                    _cardBorders[i].color = hot ? UIStyle.RebelYellow : UIStyle.Obsidian;
                if (i < _cardHeaders.Count && _cardHeaders[i] != null)
                    _cardHeaders[i].color = hot ? UIStyle.Red : UIStyle.Obsidian;

                var b = _bursts[i];
                float bk = Mathf.MoveTowards(b.localScale.x, hot ? 1.04f : 0f, Time.unscaledDeltaTime * 6.5f);
                b.localScale = Vector3.one * bk;
                b.localRotation = Quaternion.Euler(0, 0, now * 14f);
                var sh = _sheens[i];
                float cycle = hot ? Mathf.Repeat(now * 0.85f, 1.5f) : 2f;
                sh.anchoredPosition = new Vector2(Mathf.Lerp(-440f, 440f, cycle), 0);
            }
        }

        public void Pick(int index)
        {
            if (Chosen != null || index < 0 || index >= _options.Count) return;
            Chosen = _options[index];
            _highlight = index;
            Audio.AudioManager.Play(Audio.Sfx.CardPick, Vector3.zero);
            UIRoot.Get().ScreenFlash(new Color(0.95f, 0.12f, 0.22f, 0.65f), 0.25f);
            StartCoroutine(PickAnim(index));
        }

        IEnumerator PickAnim(int index)
        {
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.6f;
                for (int i = 0; i < _cards.Count; i++)
                {
                    if (i == index)
                    {
                        _cards[i].localScale = Vector3.one * Mathf.Lerp(1.08f, 1.24f, Mathf.Sin(k * Mathf.PI));
                        _cards[i].anchoredPosition += Vector2.up * (70f * Time.unscaledDeltaTime);
                    }
                    else _cards[i].anchoredPosition += Vector2.down * (1850f * Time.unscaledDeltaTime * k);
                }
                yield return null;
            }
            Done = true;
            if (TitleScreen.Current != null && !TitleScreen.Current.gameObject.activeSelf)
                TitleScreen.Current.gameObject.SetActive(true);
            Destroy(gameObject);
        }

        /// <summary>Flow step: deal three, wait for the choice, store it on the run.</summary>
        public static IEnumerator Run(Flow.RunState run)
        {
            var lib = CardLibrary.Load();
            if (lib == null || lib.starterPool.Count == 0) yield break;
            int draw = 0;
            while (true)
            {
                var three = CardDrawManager.DrawThree(lib.starterPool, run.Seed + draw * 7919);
                var panel = Show(three, run.RerollsLeft);
                // note: the component is destroyed when Done; its managed fields stay readable
                while (!panel.Done) yield return null;
                if (panel.Rerolled) { run.RerollsLeft--; draw++; continue; }
                run.ChosenCardId = panel.Chosen != null ? panel.Chosen.cardId : three[0].cardId;
                yield break;
            }
        }
    }
}
