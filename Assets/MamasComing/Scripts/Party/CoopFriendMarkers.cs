using UnityEngine;

namespace MamasComing
{
    /// Online co-op: always know where your friends are. A friend behind a wall gets a see-through tag,
    /// a friend off-screen gets an arrow at the screen edge with the room they are in.
    public class CoopFriendMarkers : MonoBehaviour
    {
        GUIStyle style, arrow;
        static readonly Color[] Cols = { new Color(1f, 0.62f, 0.2f), new Color(0.25f, 0.85f, 0.75f), new Color(1f, 0.9f, 0.3f) };

        void OnGUI()
        {
            if (!NetGame.Online || Event.current.type != EventType.Repaint) return;
            var gm = GameManager.I; if (!gm || gm.state == GameManager.State.Title) return;
            var cam = Camera.main; if (!cam) return;
            if (style == null)
            {
                var font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Segoe UI Symbol", "Tahoma", "Arial Unicode MS" }, 18);
                style = new GUIStyle(GUI.skin.box) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, font = font };
                style.normal.textColor = Color.white;
                arrow = new GUIStyle(style) { fontSize = 22 };
            }
            float sc = Screen.height / 720f; style.fontSize = Mathf.RoundToInt(17 * sc); arrow.fontSize = Mathf.RoundToInt(22 * sc);
            foreach (var p in PlayerController.AllPlayers)
            {
                if (!p || p.isLocal || !p.isActiveAndEnabled) continue;
                string name = NetGame.NameOf(p) ?? "P?";
                int idx = name.Length > 1 && char.IsDigit(name[1]) ? (name[1] - '1') : 0;
                Color col = Cols[Mathf.Clamp(idx, 0, Cols.Length - 1)];
                Vector3 head = p.transform.position + Vector3.up * 1.35f;
                Vector3 sp = cam.WorldToScreenPoint(head);
                bool behind = sp.z < 0f;
                bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
                if (onScreen)
                {
                    // visible friends already wear a name tag; only mark them when a wall hides them
                    Vector3 from = cam.transform.position, to = head - Vector3.up * 0.5f;
                    bool blocked = false;
                    foreach (var h in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to) - 0.3f, ~0, QueryTriggerInteraction.Ignore))
                        if (!h.collider.GetComponentInParent<PlayerController>()) { blocked = true; break; }
                    if (!blocked) continue;
                    Draw(new Vector2(sp.x, Screen.height - sp.y), "◉ " + name, col, style);
                    continue;
                }
                // off-screen: arrow on the edge, pointing the right way
                Vector2 c = new Vector2(Screen.width, Screen.height) * 0.5f;
                Vector2 d = new Vector2(sp.x, sp.y) - c; if (behind) d = -d;
                if (d.sqrMagnitude < 1f) d = Vector2.down;
                float m = 70f * sc;
                float k = Mathf.Min((c.x - m) / Mathf.Max(0.001f, Mathf.Abs(d.x)), (c.y - m) / Mathf.Max(0.001f, Mathf.Abs(d.y)));
                Vector2 e = c + d * k;
                string dir = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? "▶" : "◀") : (d.y > 0 ? "▲" : "▼");
                int room = GameManager.RoomOf(p.transform.position);
                string where = room >= 0 && room < GameManager.RoomNames.Length ? Loc.T(GameManager.RoomNames[room]) : "";
                Draw(new Vector2(e.x, Screen.height - e.y), dir + " " + name + (where.Length > 0 ? "  " + where : ""), col, arrow);
            }
        }

        static void Draw(Vector2 center, string text, Color col, GUIStyle st)
        {
            var size = st.CalcSize(new GUIContent(text)); size.x += 14; size.y += 6;
            var r = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
            r.x = Mathf.Clamp(r.x, 6f, Screen.width - r.width - 6f); r.y = Mathf.Clamp(r.y, 6f, Screen.height - r.height - 6f);
            var old = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.65f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = col; GUI.DrawTexture(new Rect(r.x, r.yMax - 3, r.width, 3), Texture2D.whiteTexture);
            GUI.color = old; var c0 = st.normal.textColor; st.normal.textColor = col; GUI.Label(r, text, new GUIStyle(st) { normal = { background = null, textColor = col } }); st.normal.textColor = c0;
        }
    }
}
