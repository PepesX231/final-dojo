using UnityEngine;

namespace MamasComing
{
    /// A real 3D sign: a wooden plank (child "Plank") with raised 3D lettering (TextMesh + darker layers behind
    /// it for thickness). Edit the words / fonts / colours here; the lettering re-fits the plank automatically.
    [ExecuteAlways, DisallowMultipleComponent]
    public class MamaSign3D : MonoBehaviour
    {
        [TextArea] public string en = "PLAY";
        [TextArea] public string th = "เล่น";
        public Font latinFont, thaiFont;
        public Color face = new Color(0.97f, 0.92f, 0.80f);
        public Color depth = new Color(0.23f, 0.14f, 0.09f);
        [Tooltip("Plank size in metres (x = width, y = height, z = thickness).")]
        public Vector3 plank = new Vector3(0.8f, 0.24f, 0.06f);
        [Range(0.3f, 1f)] public float fill = 0.8f;
        [Range(0, 12)] public int layers = 8;
        public float layerStep = 0.005f;

        public TextMesh Face => face3D;
        [SerializeField, HideInInspector] TextMesh face3D;
        [SerializeField, HideInInspector] TextMesh[] back = new TextMesh[0];
        [SerializeField, HideInInspector] Transform textRoot;
        string shown; bool shownThai; int fitFrames;

        public void Build()
        {
            var p = transform.Find("Plank");
            if (!p) { var pg = GameObject.CreatePrimitive(PrimitiveType.Cube); pg.name = "Plank"; pg.transform.SetParent(transform, false); Kill(pg.GetComponent<Collider>()); p = pg.transform; }
            p.localScale = plank; p.localPosition = Vector3.zero;
            if (textRoot) Kill(textRoot.gameObject);
            textRoot = new GameObject("Lettering").transform; textRoot.SetParent(transform, false);
            textRoot.localPosition = new Vector3(0, 0, -plank.z * 0.5f - 0.003f);
            back = new TextMesh[layers];
            for (int i = layers - 1; i >= 0; i--) back[i] = Layer("Depth" + i, (i + 1) * layerStep, depth, 0);
            face3D = Layer("Face", 0f, face, 1);
            shown = null; Refresh();
        }

        TextMesh Layer(string n, float z, Color c, int order)
        {
            var go = new GameObject(n, typeof(MeshRenderer), typeof(TextMesh));
            go.transform.SetParent(textRoot, false); go.transform.localPosition = new Vector3(0, 0, z);
            var tm = go.GetComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.fontSize = 96; tm.characterSize = 0.01f; tm.color = c;
            go.GetComponent<MeshRenderer>().sortingOrder = order;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tm;
        }

        static void Kill(Object o) { if (!o) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        void OnEnable() { shown = null; }
        void Update() { if (Application.isPlaying || fitFrames > 0 || shown == null) Refresh(); }
        void OnValidate() { shown = null; }

        public void Refresh()
        {
            if (!face3D) return;
            bool thai = Loc.Thai && !string.IsNullOrEmpty(th);
            string s = thai ? th : en;
            var f = thai ? (thaiFont ? thaiFont : MamaTheme.Bold) : (latinFont ? latinFont : MamaTheme.Display);
            if (s != shown || thai != shownThai)
            {
                shown = s; shownThai = thai;
                foreach (var tm in All())
                {
                    if (!tm) continue;
                    tm.text = s; tm.font = f;
                    tm.GetComponent<MeshRenderer>().sharedMaterial = f.material;
                }
                fitFrames = 3;
            }
            if (face3D.color != face) face3D.color = face;
            foreach (var b in back) if (b && b.color != depth) b.color = depth;
            if (fitFrames > 0) { fitFrames--; Fit(); }
        }

        System.Collections.Generic.IEnumerable<TextMesh> All() { yield return face3D; foreach (var b in back) yield return b; }

        /// Scales the lettering so it fills `fill` of the plank face.
        void Fit()
        {
            var r = face3D.GetComponent<MeshRenderer>();
            var b = r.localBounds; if (b.size.x < 1e-4f || b.size.y < 1e-4f) { fitFrames = Mathf.Max(fitFrames, 1); return; }
            float sx = plank.x * fill / b.size.x, sy = plank.y * fill / b.size.y;
            float s = Mathf.Min(sx, sy);
            textRoot.localScale = new Vector3(s, s, 1f);
            textRoot.localPosition = new Vector3(-b.center.x * s, -b.center.y * s, -plank.z * 0.5f - 0.003f);
        }
    }
}
