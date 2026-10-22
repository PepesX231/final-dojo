using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.World1
{
    /// <summary>
    /// Cognitive Problem-Solving Training Director for World 1 ("สร้างเกมที่มีชุดปัญหาให้ผู้เล่นฝึกทักษะแก้ปัญหา").
    /// Tracks interactive Cognitive Trial Nodes across the 5 chapters, displays a non-blocking Persona-styled
    /// Problem-Solving Hypothesis Banner at the bottom-left of the HUD, and rewards players for solving
    /// environmental logic puzzles (Observe → Experiment → Deduce Rule → Execute Solution).
    /// </summary>
    public class ProblemSolvingDirector : MonoBehaviour
    {
        public static ProblemSolvingDirector Current { get; private set; }

        public readonly List<CognitiveTrialNode> Trials = new List<CognitiveTrialNode>();
        public int SolvedCount { get; private set; }

        RectTransform _banner;
        Text _stepTag;
        Text _promptText;
        Image _statusBar;
        float _pulse;
        CognitiveTrialNode _activeNode;

        void Awake()
        {
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            BuildBanner();
            RefreshDefaultPrompt(0);
            if (ChapterDirector.Current != null)
                ChapterDirector.Current.ChapterChanged += OnChapterChanged;
        }

        void OnChapterChanged(int idx)
        {
            RefreshDefaultPrompt(idx);
        }

        void BuildBanner()
        {
            if (!UIRoot.Exists && DreamHUD.Current == null) return;
            var hud = UIRoot.Get().Hud;
            _banner = UIFactory.Rect("CognitiveProblemBanner", hud);
            _banner.anchorMin = _banner.anchorMax = new Vector2(0, 0);
            _banner.pivot = new Vector2(0, 0);
            _banner.anchoredPosition = new Vector2(32, 30);
            _banner.sizeDelta = new Vector2(680, 96);
            _banner.localRotation = Quaternion.Euler(0, 0, 1.2f);

            var shadow = UIStyle.Image("Shadow", _banner, UIStyle.Slab, UIStyle.Red);
            UIFactory.Stretch(shadow.rectTransform, -4);
            shadow.rectTransform.anchoredPosition = new Vector2(7, -7);

            var bg = UIStyle.Image("Bg", _banner, UIStyle.Slab, UIStyle.Obsidian);
            UIFactory.Stretch(bg.rectTransform);

            _statusBar = UIStyle.Image("AccentBar", _banner, UIStyle.Slash, UIStyle.RebelYellow);
            _statusBar.rectTransform.anchorMin = new Vector2(0, 1);
            _statusBar.rectTransform.anchorMax = new Vector2(1, 1);
            _statusBar.rectTransform.pivot = new Vector2(0.5f, 1);
            _statusBar.rectTransform.sizeDelta = new Vector2(-24, 28);
            _statusBar.rectTransform.anchoredPosition = new Vector2(0, -6);

            _stepTag = UIStyle.Hud("StepTag", _statusBar.rectTransform, "PROBLEM SET 01 // OBSERVE & DEDUCE", 15, UIStyle.Obsidian, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_stepTag.rectTransform);
            _stepTag.rectTransform.offsetMin = new Vector2(16, 0);

            _promptText = UIStyle.Body("PromptText", _banner, "", 20, UIStyle.Cream, TextAnchor.MiddleLeft);
            _promptText.rectTransform.anchorMin = new Vector2(0, 0);
            _promptText.rectTransform.anchorMax = new Vector2(1, 1);
            _promptText.rectTransform.offsetMin = new Vector2(22, 10);
            _promptText.rectTransform.offsetMax = new Vector2(-18, -36);
        }

        void RefreshDefaultPrompt(int chapterIdx)
        {
            if (_stepTag == null || _promptText == null) return;
            switch (chapterIdx)
            {
                case 0:
                    _stepTag.text = "PROBLEM SET 01  ◆  SPATIAL OBSERVATION & RULE DEDUCTION";
                    _promptText.text = "โจทย์ที่ 1: สังเกตแถบสีมุมกระดาษ (ฟ้า=บันไดเด้ง / เหลือง=พับกลับใน 3 วิ) หรือแก้สมการสวิตช์ตรรกะเพื่อเปิดรางรถไฟลัด!";
                    break;
                case 1:
                    _stepTag.text = "PROBLEM SET 02  ◆  PERSPECTIVE SHIFT & LATERAL THINKING";
                    _promptText.text = "โจทย์ที่ 2: เมื่อฉากหน้าเป็นทางตัน ลองวิเคราะห์มิติที่ซ่อนอยู่ (2.5D) แล้วเดินอ้อมไปแก้ปัญหาด้านหลังฉากละคร!";
                    break;
                case 2:
                    _stepTag.text = "PROBLEM SET 03  ◆  TRAJECTORY & ORBITAL SYNCHRONIZATION";
                    _promptText.text = "โจทย์ที่ 3: คำนวณวิถีโค้งจากเส้นดาวนำทาง และจับจังหวะคาบการหมุนของเกาะวงโคจร (หรือเปิดสวิตช์ SkyRail)";
                    break;
                case 3:
                    _stepTag.text = "PROBLEM SET 04  ◆  HIGH-SPEED ROUTE OPTIMIZATION";
                    _promptText.text = "โจทย์ที่ 4: วิเคราะห์เส้นทางไล่ล่า — เลือกไถลไปตามรางไฟสีเหลืองขอบชั้นหนังสือเพื่อข้ามหลุมและย่นระยะเวลา!";
                    break;
                default:
                    _stepTag.text = "PROBLEM SET 05  ◆  MULTI-RULE SYNTHESIS (FINAL EXAM)";
                    _promptText.text = "โจทย์ตัดสิน: สังเกตเส้นทางหนี อ่านจังหวะบอส และใช้ทางลัดเข้าประชิดตอนเก้าอี้หยุดพัก แล้วกด [E] นั่งค้าง 2 วินาที!";
                    break;
            }
            if (_statusBar != null) _statusBar.color = UIStyle.RebelYellow;
            _pulse = 1f;
        }

        public void NotifyTrialActivated(CognitiveTrialNode node)
        {
            _activeNode = node;
            if (!node.Solved) SolvedCount++;
            if (_stepTag != null && _promptText != null)
            {
                _stepTag.text = $"SOLUTION VERIFIED  ◆  {node.TrialCode}  ({SolvedCount}/{Mathf.Max(1, Trials.Count)})";
                _promptText.text = node.SolvedMessage;
                if (_statusBar != null) _statusBar.color = UIStyle.Cyan;
                _pulse = 1f;
            }
        }

        void Update()
        {
            if (_banner == null) return;
            if (_pulse > 0f)
            {
                _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * 2.5f);
                _banner.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(_pulse * Mathf.PI));
            }
        }
    }

}
