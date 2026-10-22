using System.Collections.Generic;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.Flow
{
    /// <summary>
    /// Dream Galaxy level select (Astro Bot reference): after falling asleep Pip rides a pillow between little
    /// storybook planets. ←/→ (A/D, d-pad, click) choose a world, Enter/E/South confirms. Locked worlds show
    /// what they will be. The flow waits on <see cref="Done"/> and reads <see cref="ChosenScene"/>.
    /// </summary>
    public class GalaxySelect : MonoBehaviour
    {
        public static GalaxySelect Current { get; private set; }
        public List<GalaxyPlanet> planets = new List<GalaxyPlanet>();
        public Transform ship;
        public Camera cam;
        public bool Done { get; private set; }
        public string ChosenScene { get; private set; }
        public GalaxyPlanet Selected => planets.Count > 0 ? planets[_index] : null;
        int _index;
        float _changedAt;
        Vector3 _camVel, _shipVel;

        // UI
        RectTransform _panel, _lockStamp;
        Text _world, _title, _sub, _chapters, _enter;
        readonly List<Image> _dots = new List<Image>();

        void Awake() { Current = this; }

        void Start()
        {
            BuildUI();
            Select(0, true);
            if (cam != null && Selected != null && Selected.cameraPose != null)
                cam.transform.SetPositionAndRotation(Selected.cameraPose.position + Selected.cameraPose.forward * -14f, Selected.cameraPose.rotation);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Audio.AudioManager.PlayMusic(Audio.Music.Galaxy, 1f);
        }

        void OnDestroy() { if (Current == this) Current = null; if (_panelRoot != null) Destroy(_panelRoot.gameObject); }

        RectTransform _panelRoot;

        void BuildUI()
        {
            var ui = UIRoot.Get();
            _panelRoot = UIFactory.Stretch(UIFactory.Rect("GalaxyUI", ui.Hud));
            // header: brush + Kanit
            var brush = UIStyle.Image("Header", _panelRoot, UIStyle.Brush, UIStyle.Cream, false);
            var brt = brush.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 1); brt.pivot = new Vector2(0, 1);
            brt.sizeDelta = new Vector2(900, 170); brt.anchoredPosition = new Vector2(30, -18); brt.localRotation = Quaternion.Euler(0, 0, 2f);
            var head = UIStyle.Title("Text", brt, "DREAM GALAXY", 82, UIStyle.Plum, TextAnchor.MiddleLeft);
            Destroy(head.GetComponent<Outline>());
            UIFactory.Stretch(head.rectTransform); head.rectTransform.offsetMin = new Vector2(70, 8);
            UIEnter.Add(brush, new Vector2(-900, 0), 0.1f, 0.4f, 8f);

            // right info panel (FF window + Persona slab title)
            _panel = UIFactory.Rect("Info", _panelRoot);
            _panel.anchorMin = _panel.anchorMax = new Vector2(1, 0.5f); _panel.pivot = new Vector2(1, 0.5f);
            _panel.anchoredPosition = new Vector2(-50, -30); _panel.sizeDelta = new Vector2(640, 600);
            var win = UIStyle.Image("Window", _panel, UIStyle.Window, Color.white);
            UIFactory.Stretch(win.rectTransform);
            UIStyle.HardShadow(win, new Color(0, 0, 0, 0.6f), new Vector2(12, -12));
            var slab = UIStyle.Image("WorldSlab", _panel, UIStyle.Slab, UIStyle.Red);
            slab.rectTransform.anchorMin = slab.rectTransform.anchorMax = new Vector2(0, 1); slab.rectTransform.pivot = new Vector2(0, 0.5f);
            slab.rectTransform.sizeDelta = new Vector2(250, 66); slab.rectTransform.anchoredPosition = new Vector2(-30, 0);
            slab.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f);
            UIStyle.HardShadow(slab, UIStyle.Plum, new Vector2(6, -6));
            _world = UIStyle.Title("World", slab.rectTransform, "", 38, Color.white, TextAnchor.MiddleCenter, redDrop: false);
            UIFactory.Stretch(_world.rectTransform);
            _title = UIStyle.Title("Title", _panel, "", 60, Color.white, TextAnchor.UpperLeft);
            _title.rectTransform.anchorMin = new Vector2(0, 1); _title.rectTransform.anchorMax = new Vector2(1, 1); _title.rectTransform.pivot = new Vector2(0, 1);
            _title.rectTransform.sizeDelta = new Vector2(-80, 150); _title.rectTransform.anchoredPosition = new Vector2(44, -54);
            _sub = UIStyle.Hud("Sub", _panel, "", 26, UIStyle.Lavender, TextAnchor.UpperLeft);
            _sub.rectTransform.anchorMin = new Vector2(0, 1); _sub.rectTransform.anchorMax = new Vector2(1, 1); _sub.rectTransform.pivot = new Vector2(0, 1);
            _sub.rectTransform.sizeDelta = new Vector2(-80, 40); _sub.rectTransform.anchoredPosition = new Vector2(44, -200);
            _chapters = UIStyle.Body("Chapters", _panel, "", 25, UIStyle.Cream, TextAnchor.UpperLeft);
            _chapters.rectTransform.anchorMin = new Vector2(0, 1); _chapters.rectTransform.anchorMax = new Vector2(1, 1); _chapters.rectTransform.pivot = new Vector2(0, 1);
            _chapters.rectTransform.sizeDelta = new Vector2(-80, 250); _chapters.rectTransform.anchoredPosition = new Vector2(44, -252);
            _chapters.supportRichText = true;
            var enter = UIStyle.Image("Enter", _panel, UIStyle.Slab, UIStyle.Cream);
            enter.rectTransform.anchorMin = enter.rectTransform.anchorMax = new Vector2(1, 0); enter.rectTransform.pivot = new Vector2(1, 0);
            enter.rectTransform.sizeDelta = new Vector2(380, 74); enter.rectTransform.anchoredPosition = new Vector2(10, 26);
            UIStyle.HardShadow(enter, UIStyle.Red, new Vector2(7, -7));
            _enter = UIStyle.Title("Text", enter.rectTransform, "", 34, UIStyle.Plum, TextAnchor.MiddleCenter, redDrop: false);
            Destroy(_enter.GetComponent<Outline>());
            UIFactory.Stretch(_enter.rectTransform);
            _lockStamp = UIStyle.Tag("Locked", _panel, "LOCKED", 44, UIStyle.Red, Color.white, new Vector2(330, 90));
            _lockStamp.anchoredPosition = new Vector2(40, 40); _lockStamp.localRotation = Quaternion.Euler(0, 0, -14f);
            _lockStamp.GetComponentInChildren<Text>().font = UIStyle.TitleFont;
            UIEnter.Add(_panel, new Vector2(800, 0), 0.2f, 0.42f);

            // planet dots + hint (bottom-left)
            var dots = UIFactory.Rect("Dots", _panelRoot);
            dots.anchorMin = dots.anchorMax = new Vector2(0, 0); dots.pivot = new Vector2(0, 0); dots.anchoredPosition = new Vector2(60, 50); dots.sizeDelta = new Vector2(600, 60);
            for (int i = 0; i < planets.Count; i++)
            {
                var d = UIStyle.Image("Dot" + i, dots, UIStyle.Diamond, UIStyle.Cream, false);
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0, 0.5f);
                d.rectTransform.anchoredPosition = new Vector2(24 + i * 54, 0); d.rectTransform.sizeDelta = new Vector2(34, 34);
                _dots.Add(d);
            }
            var hint = UIStyle.Hud("Hint", dots, "◆  ← →  เลือกโลก      ENTER  เข้าสู่ฝัน", 26, UIStyle.Cream, TextAnchor.MiddleLeft);
            UIStyle.HardShadow(hint, UIStyle.Plum, new Vector2(3, -3));
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0, 0.5f); hint.rectTransform.pivot = new Vector2(0, 0.5f);
            hint.rectTransform.sizeDelta = new Vector2(900, 50); hint.rectTransform.anchoredPosition = new Vector2(24 + planets.Count * 54 + 20, 0);
        }

        public void Select(int i, bool instant = false)
        {
            if (planets.Count == 0) return;
            i = (i % planets.Count + planets.Count) % planets.Count;
            if (i == _index && !instant) return;
            _index = i; _changedAt = Time.unscaledTime;
            var p = planets[i];
            _world.text = p.worldId.Replace("W", "WORLD ");
            _title.text = p.title;
            _sub.text = p.subtitle;
            var record = Core.SaveManager.GetOrCreate().Data.progression.FindLevelRecord(p.sceneName);
            _chapters.text = p.chapters + (record != null ? $"\n\nBEST  {record.bestSeconds / 60f:0.0} min  |  CLEARS  {record.clears}" + (record.bestStars > 0 ? $"  |  STARS  {record.bestStars}" : "") : "");
            bool locked = !CanEnter(p);
            _lockStamp.gameObject.SetActive(locked);
            _lockStamp.GetComponentInChildren<Text>().text = WorldAccess.LockLabel(p.sceneName,p.locked);
            if (locked) _chapters.text += "\n\n" + WorldAccess.Reason(p.sceneName, p.locked);
            _enter.text = locked ? "ยังเข้าไม่ได้" : "ENTER  ▶";
            for (int k = 0; k < _dots.Count; k++) _dots[k].color = k == i ? UIStyle.Red : (!CanEnter(planets[k]) ? new Color(1, 1, 1, 0.35f) : UIStyle.Cream);
            if (!instant)
            {
                Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero, 0.6f);
                var e = _panel.GetComponent<UIEnter>(); if (e != null) Destroy(e);
                _panel.anchoredPosition = new Vector2(-50, -30);
                UIEnter.Add(_panel, new Vector2(140, 0), 0f, 0.28f, -2f);
            }
        }

        static bool CanEnter(GalaxyPlanet p) => WorldAccess.CanEnter(p.sceneName, p.locked,
            Core.SaveManager.GetOrCreate().Data.progression);

        public bool Confirm()
        {
            var p = Selected;
            if (Done || p == null) return false;
            if (!CanEnter(p))
            {
                Audio.AudioManager.Play(Audio.Sfx.CardFail, Vector3.zero);
                UIRoot.Get().Toast(WorldAccess.Reason(p.sceneName, p.locked), 3f);
                return false;
            }
            Audio.AudioManager.Play(Audio.Sfx.CardPick, Vector3.zero);
            Presentation.VFX.Play(Presentation.VFXKind.StarBurst, p.transform.position, 4f);
            ChosenScene = string.IsNullOrEmpty(p.sceneName) ? GameFlowManager.DreamScene : p.sceneName;
            Done = true;
            return true;
        }

        void Update()
        {
            if (!Done)
            {
                var kb = Keyboard.current; var gp = Gamepad.current;
                if (kb != null)
                {
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Select(_index + 1);
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Select(_index - 1);
                    if (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Confirm();
                }
                if (gp != null)
                {
                    if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame) Select(_index + 1);
                    if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame) Select(_index - 1);
                    if (gp.buttonSouth.wasPressedThisFrame) Confirm();
                }
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && cam != null)
                {
                    var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    for (int k = 0; k < planets.Count; k++)
                    {
                        float r = 7f;
                        var pp = planets[k].transform.position;
                        if (Vector3.Cross(ray.direction, pp - ray.origin).magnitude < r) { if (k == _index) Confirm(); else Select(k); break; }
                    }
                }
            }
            var sel = Selected;
            if (sel == null) return;
            if (cam != null && sel.cameraPose != null)
            {
                cam.transform.position = Vector3.SmoothDamp(cam.transform.position, sel.cameraPose.position, ref _camVel, 0.55f);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, sel.cameraPose.rotation, 1f - Mathf.Exp(-3f * Time.deltaTime));
            }
            if (ship != null && sel.shipPoint != null)
            {
                Vector3 target = sel.shipPoint.position + Vector3.up * Mathf.Sin(Time.time * 2.2f) * 0.25f;
                Vector3 before = ship.position;
                ship.position = Vector3.SmoothDamp(ship.position, target, ref _shipVel, 0.7f);
                Vector3 v = ship.position - before; v.y = 0;
                if (v.sqrMagnitude > 1e-5f) ship.rotation = Quaternion.Slerp(ship.rotation, Quaternion.LookRotation(v) * Quaternion.Euler(0, 0, -Mathf.Clamp(_shipVel.x, -1, 1) * 15f), 1f - Mathf.Exp(-5f * Time.deltaTime));
                else ship.rotation = Quaternion.Slerp(ship.rotation, Quaternion.LookRotation(cam != null ? Vector3.ProjectOnPlane(cam.transform.position - ship.position, Vector3.up) : Vector3.back), 1f - Mathf.Exp(-2f * Time.deltaTime));
            }
            if (Done) { sel.transform.localScale = Vector3.Lerp(sel.transform.localScale, Vector3.one * 1.15f, Time.deltaTime * 4f); }
        }
    }
}
