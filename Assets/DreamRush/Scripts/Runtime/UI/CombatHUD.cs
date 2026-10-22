using DreamRush.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Combat-level HUD (M14): a skewed STAMINA bar and the DREAM meter (fills from hits and DREAM DODGES; full = "V ◆ DREAM
    /// BURST" flashes), plus a move-list strip that teaches the kit for the first 40 s (and whenever you press Tab).
    /// Lives on the player; bottom-left, out of the way of the objective (top-left) and the boss bar (bottom-centre).
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        public PlayerCombat combat;
        RectTransform _root, _stam, _dream, _moves, _tab; Image _dreamImg, _stamImg; Text _burst; CanvasGroup _movesCg, _tabCg, _stamCg; float _fullFor;
        float _shownAt, _stamShown = 1f, _dreamShown;
        const float BarW = 330f;

        void Start()
        {
            if (!UIRoot.Exists) { enabled = false; return; }
            var hud = UIRoot.Get().Hud;
            _root = UIFactory.Rect("CombatHUD", hud);
            _root.anchorMin = _root.anchorMax = new Vector2(0, 0); _root.pivot = new Vector2(0, 0);
            _root.anchoredPosition = new Vector2(36, 34); _root.sizeDelta = new Vector2(420, 120);
            _root.localRotation = Quaternion.Euler(0, 0, 4f);
            _stam = Bar("Stamina", new Vector2(0, 62), 18f, new Color(1f, 0.85f, 0.2f), out _stamImg, "STAMINA");
            _dream = Bar("Dream", new Vector2(0, 22), 26f, new Color(1f, 0.35f, 0.75f), out _dreamImg, "DREAM");
            _burst = UIFactory.Label("BurstHint", _root, "V ◆ DREAM BURST!", 26, Color.white, TextAnchor.MiddleLeft);
            _burst.font = UIStyle.TitleFont;
            var br = _burst.rectTransform; br.anchorMin = br.anchorMax = new Vector2(0, 0); br.pivot = new Vector2(0, 0);
            br.anchoredPosition = new Vector2(BarW + 20, 18); br.sizeDelta = new Vector2(320, 36);
            UIStyle.HardShadow(_burst, UIStyle.Plum, new Vector2(3, -3));
            if (GetComponent<SkillBar>() == null) gameObject.AddComponent<SkillBar>().combat = combat;   // M18 skill cards
            _stamCg = _stam.parent.gameObject.AddComponent<CanvasGroup>();
            _shownAt = Time.unscaledTime;
        }

        RectTransform Bar(string name, Vector2 pos, float h, Color fill, out Image fillImg, string label)
        {
            var bg = UIStyle.Image(name + "Bg", _root, UIStyle.Slab, UIStyle.Plum);
            var rt = bg.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(BarW, h + 10);
            var f = UIFactory.Panel(name + "Fill", rt, fill, UIFactory.RoundedSmall);
            var fr = f.rectTransform; fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(1, 1); fr.offsetMin = new Vector2(8, 5); fr.offsetMax = new Vector2(-8, -5);
            fr.pivot = new Vector2(0, 0.5f);
            fillImg = f;
            var l = UIFactory.Label(name + "Label", rt, label, 15, Color.white, TextAnchor.MiddleLeft);
            l.font = UIStyle.HudFont; var lr = l.rectTransform; UIFactory.Stretch(lr); lr.offsetMin = new Vector2(14, 0);
            UIStyle.HardShadow(l, UIStyle.Plum, new Vector2(2, -2));
            return fr;
        }

        void BuildMoves(RectTransform hud)
        {
            _moves = UIFactory.Rect("MoveList", hud);
            _moves.anchorMin = _moves.anchorMax = new Vector2(0, 0); _moves.pivot = new Vector2(0, 0);
            _moves.anchoredPosition = new Vector2(40, 150); _moves.sizeDelta = new Vector2(620, 230);
            _movesCg = _moves.gameObject.AddComponent<CanvasGroup>();
            var bg = UIStyle.Image("Bg", _moves, UIStyle.Slab, new Color(UIStyle.Plum.r, UIStyle.Plum.g, UIStyle.Plum.b, 0.82f)); UIFactory.Stretch(bg.rectTransform);
            string txt =
                "<b>LMB</b>  ต่อยคอมโบ 3 จังหวะ (แย็บ · ฮุก · อัปเปอร์คัต)   ·   <b>กดค้าง</b>  ชาร์จทุบพื้น\n" +
                "<b>กลางอากาศ + LMB</b>  หมัดหมุน AIR SPIN (2 ครั้ง/กระโดด)\n" +
                "<b>ต่อยโดน → SPACE</b>  อัปเปอร์คัตเสยลอยฟ้า\n" +
                "<b>แตะ Shift</b>  กลิ้งหลบ  →  <b>LMB</b>  จรวดหมัด พุ่งทะลุ\n" +
                "<b>หลบพอดี (DREAM DODGE) → LMB</b>  สวนกลับ ×2\n" +
                "<b>V</b>  DREAM BURST เมื่อเพชรชมพูเต็ม   ·   <b>R / คลิกกลาง</b>  ล็อกเป้า   ·   การ์ดสีทอง = ใช้ได้ตอนนี้!";
            var l = UIFactory.Label("Text", _moves, txt, 19, Color.white, TextAnchor.UpperLeft);
            l.supportRichText = true; l.font = UIStyle.BodyFont;
            UIFactory.Stretch(l.rectTransform, 14);
        }

        void Update()
        {
            if (combat == null || _root == null) return;
            float lift = BossBarUI.Current != null ? 130f : 0f;   // M19: clear the boss bar
            _root.anchoredPosition = new Vector2(36, Mathf.Lerp(_root.anchoredPosition.y, 34f + lift, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)));
            if (_moves != null) _moves.anchoredPosition = new Vector2(40, _root.anchoredPosition.y + 116f);
            if (_tab != null) _tab.anchoredPosition = new Vector2(40, _root.anchoredPosition.y + 116f);
            if (DreamHUD.Current != null && DreamHUD.Current.ShowCards) DreamHUD.Current.ShowCards = false;
            float dt = Time.unscaledDeltaTime;
            _stamShown = Mathf.MoveTowards(_stamShown, combat.Stamina / combat.maxStamina, dt * 3f);
            _dreamShown = Mathf.MoveTowards(_dreamShown, combat.Meter / 100f, dt * 1.5f);
            _stam.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, _stamShown), 1f);
            _dream.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, _dreamShown), 1f);
            _stamImg.color = combat.Stamina < 20f ? Color.Lerp(new Color(1f, 0.85f, 0.2f), UIStyle.Red, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : new Color(1f, 0.85f, 0.2f);
            bool ready = combat.BurstReady;
            _burst.gameObject.SetActive(ready);
            if (ready)
            {
                float p = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                _burst.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.85f), p);
                _dreamImg.color = Color.Lerp(new Color(1f, 0.35f, 0.75f), Color.white, p * 0.6f);
                _burst.rectTransform.localScale = Vector3.one * (1f + p * 0.06f);
            }
            else _dreamImg.color = new Color(1f, 0.35f, 0.75f);
            // stamina bar fades back when it's been full for a moment (less HUD noise while exploring)
            _fullFor = combat.Stamina >= combat.maxStamina - 0.01f ? _fullFor + dt : 0f;
            _stamCg.alpha = Mathf.MoveTowards(_stamCg.alpha, _fullFor > 1.5f ? 0.3f : 1f, dt * (_fullFor > 1.5f ? 1.5f : 8f));
        }

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_moves != null) Destroy(_moves.gameObject);
            if (_tab != null) Destroy(_tab.gameObject);
        }
    }
}
