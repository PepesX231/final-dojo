using System.Collections.Generic;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// Dreamland rules + HUD: 3 Dream Keys from THE CHAIR = victory. Compass strip (top centre) with the chair,
    /// THE ALARM (when near) and pillow fires; key counter; red danger vignette while the alarm hunts you;
    /// a region title card when you enter a new region (FF field spirit).
    /// </summary>
    public class OpenWorldDirector : MonoBehaviour
    {
        [System.Serializable] public class Region { public string name; public string thai; public Vector3 center; public float radius = 120f; }
        public static OpenWorldDirector Current { get; private set; }
        public OpenWorldChair chair;
        public NightmareAlarm alarm;
        public List<Transform> pillowFires = new List<Transform>();
        public List<Region> regions = new List<Region>();
        public int keysNeeded = 3;
        public int Keys { get; private set; }
        public bool Won { get; private set; }

        PlayerMotor3D _player;
        RectTransform _compass, _markerChair, _markerAlarm, _keysRt;
        readonly List<RectTransform> _fireMarks = new List<RectTransform>();
        Text _chairDist, _keysText;
        Image _vignette;
        RectTransform _root;
        int _region = -1;
        const float CompassWidth = 1000f, CompassFov = 180f;

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; if (_root != null) Destroy(_root.gameObject); }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            if (chair != null) chair.KeyDropped += OnKey;
            BuildHud();
            Invoke(nameof(SetGoal), 0.2f);
        }

        void OnKey(int n)
        {
            Keys = n;
            _keysPop = 1f;
            SetGoal();
            BossIntroCard.Show($"DREAM KEY  {Keys} / {keysNeeded}", Keys < keysNeeded ? "เก้าอี้หนีไปอีกแล้ว — ตามต่อ!" : "ประตูความฝันเปิดแล้ว!", chapterCard: true);
            if (Keys >= keysNeeded && !Won)
            {
                Won = true;
                if (GameFlowManager.Exists) Invoke(nameof(Finish), 2.6f);
            }
        }

        void SetGoal() => DreamHUD.Current?.SetObjective(Keys >= keysNeeded ? "ได้ครบแล้ว — ฝันดีนะ!" : $"ล่าเก้าอี้ (◆ แดงบนเข็มทิศ) แล้วนั่งทับมันตอนหมดแรง — กุญแจฝัน {Keys}/{keysNeeded}\nระวัง THE ALARM!");

        void Finish() => GameFlowManager.Get().EndDream(true, "DreamKeys", null);

        float _keysPop;

        void BuildHud()
        {
            var ui = UIRoot.Get();
            _root = UIFactory.Stretch(UIFactory.Rect("OpenWorldHUD", ui.Hud));
            _vignette = UIStyle.Image("DangerVignette", _root, null, new Color(1, 0.1f, 0.2f, 0));
            _vignette.sprite = MakeVignette(); _vignette.type = Image.Type.Simple;
            UIFactory.Stretch(_vignette.rectTransform);

            // compass strip
            var holder = UIFactory.Rect("Compass", _root);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1); holder.pivot = new Vector2(0.5f, 1);
            holder.anchoredPosition = new Vector2(0, -150); holder.sizeDelta = new Vector2(CompassWidth, 54);
            _compass = holder;
            var bar = UIStyle.Image("Bar", holder, UIStyle.Slab, new Color(UIStyle.Plum.r, UIStyle.Plum.g, UIStyle.Plum.b, 0.78f));
            UIFactory.Stretch(bar.rectTransform);
            var mask = UIFactory.Rect("Mask", holder); UIFactory.Stretch(mask, 4); mask.gameObject.AddComponent<RectMask2D>();
            foreach (var (lbl, yaw) in new[] { ("N", 0f), ("E", 90f), ("S", 180f), ("W", 270f) })
            {
                var t = UIStyle.Hud("Cardinal_" + lbl, mask, lbl, 24, UIStyle.Cream, TextAnchor.MiddleCenter);
                t.rectTransform.sizeDelta = new Vector2(40, 40);
                t.gameObject.AddComponent<CompassTag>().yaw = yaw;
            }
            for (int i = 0; i < 24; i++)
            {
                if (i % 6 == 0) continue;
                var tick = UIStyle.Image("Tick", mask, null, new Color(1, 1, 1, 0.35f)); tick.sprite = UIFactory.RoundedSmall;
                tick.rectTransform.sizeDelta = new Vector2(3, 12);
                tick.gameObject.AddComponent<CompassTag>().yaw = i * 15f;
            }
            foreach (var f in pillowFires)
            {
                var m = UIStyle.Image("Fire", mask, UIStyle.Diamond, new Color(1f, 0.7f, 0.35f), false);
                m.rectTransform.sizeDelta = new Vector2(18, 18);
                _fireMarks.Add(m.rectTransform);
            }
            var cm = UIStyle.Image("ChairMark", mask, UIStyle.Diamond, UIStyle.Red, false);
            cm.rectTransform.sizeDelta = new Vector2(34, 34);
            _markerChair = cm.rectTransform;
            _chairDist = UIStyle.Hud("Dist", _markerChair, "", 20, UIStyle.Cream, TextAnchor.MiddleCenter);
            _chairDist.rectTransform.sizeDelta = new Vector2(140, 30); _chairDist.rectTransform.anchoredPosition = new Vector2(0, -34);
            UIStyle.HardShadow(_chairDist, UIStyle.Plum, new Vector2(2, -2));
            var am = UIStyle.Image("AlarmMark", mask, UIStyle.Burst, UIStyle.Lavender, false);
            am.rectTransform.sizeDelta = new Vector2(40, 40);
            _markerAlarm = am.rectTransform;
            var center = UIStyle.Image("Needle", holder, UIStyle.Pointer, UIStyle.Cream, false);
            center.rectTransform.sizeDelta = new Vector2(26, 20); center.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            center.rectTransform.anchorMin = center.rectTransform.anchorMax = new Vector2(0.5f, 0); center.rectTransform.anchoredPosition = new Vector2(0, -6);

            // keys (top-right)
            _keysRt = UIStyle.Tag("Keys", _root, "", 34, UIStyle.Red, Color.white, new Vector2(300, 74));
            _keysRt.anchorMin = _keysRt.anchorMax = new Vector2(1, 1); _keysRt.pivot = new Vector2(1, 1);
            _keysRt.anchoredPosition = new Vector2(-30, -30); _keysRt.localRotation = Quaternion.Euler(0, 0, 3f);
            _keysText = _keysRt.GetComponentInChildren<Text>(); _keysText.font = UIStyle.TitleFont;
        }

        static Sprite MakeVignette()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2f) / (S / 2f), dy = (y - S / 2f) / (S / 2f);
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01((d - 0.65f) * 2.2f));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }

        float YawTo(Vector3 target, Transform cam)
        {
            Vector3 d = target - cam.position; d.y = 0;
            float camYaw = cam.eulerAngles.y;
            return Mathf.DeltaAngle(camYaw, Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : Vector3.forward).eulerAngles.y);
        }

        float X(float delta) => Mathf.Clamp(delta / (CompassFov * 0.5f), -1.1f, 1.1f) * CompassWidth * 0.5f;

        void Update()
        {
            var cam = Camera.main;
            if (cam == null || _player == null || _compass == null) return;
            float camYaw = cam.transform.eulerAngles.y;
            foreach (var tag in _compass.GetComponentsInChildren<CompassTag>(true))
            {
                float dlt = Mathf.DeltaAngle(camYaw, tag.yaw);
                var rt = (RectTransform)tag.transform;
                rt.anchoredPosition = new Vector2(X(dlt), 0);
                rt.gameObject.SetActive(Mathf.Abs(dlt) < CompassFov * 0.5f);
            }
            for (int i = 0; i < _fireMarks.Count; i++)
            {
                float dlt = YawTo(pillowFires[i].position, cam.transform);
                _fireMarks[i].anchoredPosition = new Vector2(X(dlt), -14);
                _fireMarks[i].gameObject.SetActive(Mathf.Abs(dlt) < CompassFov * 0.5f && Vector3.Distance(pillowFires[i].position, _player.transform.position) < 160f);
            }
            if (chair != null)
            {
                bool visible = chair.Current != OpenWorldChair.State.Gone;
                _markerChair.gameObject.SetActive(visible);
                float dlt = YawTo(chair.transform.position, cam.transform);
                _markerChair.anchoredPosition = new Vector2(X(dlt), 2);
                float dist = Vector3.Distance(chair.transform.position, _player.transform.position);
                _chairDist.text = chair.Current == OpenWorldChair.State.Exhausted ? "TIRED!" : $"{dist:0} m";
                float pulse = chair.Current == OpenWorldChair.State.Exhausted ? 1.3f + Mathf.Sin(Time.time * 10f) * 0.2f : 1f;
                _markerChair.localScale = Vector3.one * pulse;
            }
            float danger = 0f;
            if (alarm != null)
            {
                float d = Vector3.Distance(alarm.transform.position, _player.transform.position);
                _markerAlarm.gameObject.SetActive(d < 90f);
                _markerAlarm.anchoredPosition = new Vector2(X(YawTo(alarm.transform.position, cam.transform)), 0);
                _markerAlarm.localRotation = Quaternion.Euler(0, 0, Time.time * (alarm.IsHunting ? 400f : 60f));
                ((Image)_markerAlarm.GetComponent<Image>()).color = alarm.IsHunting ? UIStyle.Red : UIStyle.Lavender;
                if (alarm.IsHunting) danger = Mathf.Clamp01(1.2f - d / 40f) * (0.55f + 0.25f * Mathf.Sin(Time.time * 9f));
            }
            var c = _vignette.color; c.a = Mathf.MoveTowards(c.a, danger, Time.deltaTime * 2f); _vignette.color = c;
            _keysText.text = $"◆  KEYS  {Keys} / {keysNeeded}";
            _keysPop = Mathf.MoveTowards(_keysPop, 0f, Time.deltaTime * 2f);
            _keysRt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(_keysPop * Mathf.PI));

            // Choose one region before comparing it with the previous one. Skipping the
            // current region inside this loop made overlapping regions alternate every frame.
            int nextRegion = -1;
            for (int i = 0; i < regions.Count; i++)
            {
                Vector3 dd = _player.transform.position - regions[i].center; dd.y = 0;
                if (dd.sqrMagnitude < regions[i].radius * regions[i].radius) { nextRegion = i; break; }
            }
            if (nextRegion >= 0 && nextRegion != _region)
            {
                bool first = _region < 0;
                _region = nextRegion;
                if (!first || Time.timeSinceLevelLoad > 1f) BossIntroCard.Show(regions[_region].name, regions[_region].thai, chapterCard: true);
            }
        }
    }

    /// <summary>Compass strip element at a fixed world yaw.</summary>
    public class CompassTag : MonoBehaviour { public float yaw; }
}
