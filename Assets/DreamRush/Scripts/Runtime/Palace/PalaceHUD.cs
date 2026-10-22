using DreamRush.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.Palace
{
    /// <summary>
    /// Midnight Palace HUD (M15): the LOST MINUTES wallet (top-right, pops when it changes), the HOUR COMPASS (after a
    /// library visit: a diamond arrow on the screen edge per remaining stolen hour, with its distance), and a small
    /// "where am I" tag while inside a building.
    /// </summary>
    public class PalaceHUD : MonoBehaviour
    {
        RectTransform _wallet, _place; Text _walletText, _placeText; float _pop; int _shown = -1;
        readonly RectTransform[] _arrows = new RectTransform[3]; readonly Text[] _arrowText = new Text[3];

        void Start()
        {
            if (!UIRoot.Exists) { enabled = false; return; }
            var hud = UIRoot.Get().Hud;
            var bg = UIStyle.Image("Wallet", hud, UIStyle.Slab, UIStyle.Plum);
            _wallet = bg.rectTransform; _wallet.anchorMin = _wallet.anchorMax = new Vector2(1, 1); _wallet.pivot = new Vector2(1, 1);
            _wallet.anchoredPosition = new Vector2(-36, -150); _wallet.sizeDelta = new Vector2(260, 64); _wallet.localRotation = Quaternion.Euler(0, 0, -3f);
            _walletText = UIFactory.Label("Text", _wallet, "⏱ 0", 34, new Color(1f, 0.82f, 0.3f), TextAnchor.MiddleCenter);
            _walletText.font = UIStyle.TitleFont; UIFactory.Stretch(_walletText.rectTransform);
            UIStyle.HardShadow(_walletText, UIStyle.Plum, new Vector2(3, -3));
            var pl = UIStyle.Image("Place", hud, UIStyle.Slab, UIStyle.Red);
            _place = pl.rectTransform; _place.anchorMin = _place.anchorMax = new Vector2(0.5f, 1); _place.pivot = new Vector2(0.5f, 1);
            _place.anchoredPosition = new Vector2(0, -24); _place.sizeDelta = new Vector2(520, 58);
            _placeText = UIFactory.Label("Text", _place, "", 26, Color.white, TextAnchor.MiddleCenter); _placeText.font = UIStyle.TitleFont; UIFactory.Stretch(_placeText.rectTransform);
            for (int i = 0; i < 3; i++)
            {
                var a = UIStyle.Image("HourArrow" + i, hud, UIStyle.Diamond, new Color(0.5f, 0.95f, 1f)); a.raycastTarget = false;
                _arrows[i] = a.rectTransform; _arrows[i].sizeDelta = new Vector2(46, 46); _arrows[i].anchorMin = _arrows[i].anchorMax = Vector2.zero;
                _arrowText[i] = UIFactory.Label("Dist", _arrows[i], "", 18, Color.white, TextAnchor.MiddleCenter);
                var tr = _arrowText[i].rectTransform; tr.anchoredPosition = new Vector2(0, -38); tr.sizeDelta = new Vector2(120, 30);
                UIStyle.HardShadow(_arrowText[i], UIStyle.Plum, new Vector2(2, -2));
            }
        }

        void Update()
        {
            var pd = PalaceDirector.Current; if (pd == null || _wallet == null) return;
            if (pd.Minutes != _shown) { if (_shown >= 0) _pop = 1f; _shown = pd.Minutes; _walletText.text = $"⏱ {_shown}"; }
            _pop = Mathf.MoveTowards(_pop, 0f, Time.unscaledDeltaTime * 3f);
            _wallet.localScale = Vector3.one * (1f + _pop * 0.18f);
            var id = InteriorDirector.Current;
            bool inside = id != null && id.Inside && id.Door != null;
            _place.gameObject.SetActive(inside);
            if (inside) _placeText.text = string.IsNullOrEmpty(id.Door.sign) ? BuildingDoor.TypeName(id.Door.type) : id.Door.sign;
            // hour compass
            var cam = Camera.main; var canvas = _wallet.GetComponentInParent<Canvas>(); float sf = canvas != null ? canvas.scaleFactor : 1f;
            for (int i = 0; i < 3; i++)
            {
                bool show = pd.HourCompass && !inside && cam != null && i < pd.hours.Count && pd.hours[i] != null && !pd.hours[i].Collected && pd.hours[i].gameObject.activeInHierarchy;
                _arrows[i].gameObject.SetActive(show);
                if (!show) continue;
                Vector3 wp = pd.hours[i].transform.position + Vector3.up * 1.5f;
                Vector3 sp = cam.WorldToScreenPoint(wp);
                bool behind = sp.z < 0f; if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
                Vector2 c = new Vector2(Screen.width, Screen.height) * 0.5f; Vector2 d = (Vector2)sp - c;
                float mx = c.x - 70f, my = c.y - 90f;
                bool off = behind || Mathf.Abs(d.x) > mx || Mathf.Abs(d.y) > my;
                if (off) { float k = Mathf.Min(mx / Mathf.Max(1f, Mathf.Abs(d.x)), my / Mathf.Max(1f, Mathf.Abs(d.y))); d *= k; }
                _arrows[i].anchoredPosition = (c + d) / sf;
                _arrows[i].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f + 45f);
                _arrowText[i].rectTransform.localRotation = Quaternion.Inverse(_arrows[i].localRotation);
                var p = Player.PlayerMotor3D.Instance;
                _arrowText[i].text = p != null ? $"{Vector3.Distance(p.transform.position, wp):F0} m" : "";
                _arrows[i].localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f + i) * 0.08f);
            }
        }

        void OnDestroy()
        {
            if (_wallet != null) Destroy(_wallet.gameObject);
            if (_place != null) Destroy(_place.gameObject);
            foreach (var a in _arrows) if (a != null) Destroy(a.gameObject);
        }
    }
}
