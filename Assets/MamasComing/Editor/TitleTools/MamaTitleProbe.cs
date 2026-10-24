using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Read-only dump used while designing the title screen (kid rigs + living-room layout).
    public static class MamaTitleProbe
    {
        static string Out => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DevBridge", "out", "titleprobe.txt");

        public static void Dump()
        {
            var sb = new StringBuilder();
            foreach (var p in new[] { "PF_AppleKid", "PF_Kid_Banana", "PF_Kid_Orange", "PF_Kid_Watermelon", "PF_Kid_WaxApple" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/" + p + ".prefab");
                sb.AppendLine("=== " + p + (go ? "" : " MISSING"));
                if (go) Walk(go.transform, 0, sb);
            }
            var cat = Resources.Load<KidCatalog>("MamaKidCatalog");
            sb.AppendLine("=== catalog: " + (cat ? string.Join(", ", cat.kids.Select(k => k ? k.name : "null")) : "none"));
            sb.AppendLine("=== scene");
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var n = t.name.ToLower();
                var p = t.position;
                bool living = p.x > -2 && p.x < 14 && p.z > -4 && p.z < 7;
                if (!living) continue;
                if (t.parent && t.parent.parent && t.parent.parent.parent) continue; // only top 3 levels
                var r = t.GetComponentsInChildren<Renderer>().Where(x => x.enabled).ToArray();
                Bounds b = r.Length > 0 ? r[0].bounds : new Bounds(p, Vector3.zero);
                foreach (var x in r) b.Encapsulate(x.bounds);
                sb.AppendLine($"{Path(t)}  pos={F(p)} size={F(b.size)} center={F(b.center)} comps={string.Join(",", t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "null").Where(c => c != "Transform"))}");
            }
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm)
            {
                sb.AppendLine("player=" + (gm.player ? F(gm.player.transform.position) + " rot " + gm.player.transform.eulerAngles : "none"));
                if (gm.cam) sb.AppendLine("cam=" + Path(gm.cam.transform) + " " + F(gm.cam.transform.position) + " " + gm.cam.transform.eulerAngles + " camComps=" + string.Join(",", gm.cam.GetComponents<Component>().Select(c => c.GetType().Name)));
            }
            var main = Camera.main; if (main) sb.AppendLine("main=" + Path(main.transform) + " " + F(main.transform.position) + " fov " + main.fieldOfView);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sb.AppendLine("sun " + l.name + " " + l.transform.eulerAngles + " col " + l.color + " int " + l.intensity);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[TitleProbe] wrote " + Out);
        }


        /// arg: "x,y,z|yaw,pitch|fov|file.png"  renders the open scene from that pose (edit mode).
        public static void Shot(string arg)
        {
            var a = arg.Split('|');
            var p = a[0].Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var r = a[1].Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            float fov = float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
            var go = new GameObject("TitleProbeCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                var main = Camera.main;
                if (main) { cam.CopyFrom(main); }
                if (a.Length > 4 && a[4] == "main" && main) { p = new[] { main.transform.position.x, main.transform.position.y, main.transform.position.z }; }
                go.transform.position = new Vector3(p[0], p[1], p[2]);
                go.transform.rotation = Quaternion.Euler(r[1], r[0], 0f);
                cam.fieldOfView = fov;
                foreach (var k in Object.FindObjectsByType<MamaPosedKid>(FindObjectsSortMode.None)) k.ApplyPose(0.6f);
                foreach (var sg in Object.FindObjectsByType<MamaSign3D>(FindObjectsSortMode.None)) for (int i = 0; i < 4; i++) sg.Refresh();
                foreach (var b in Object.FindObjectsByType<MamaBillboard>(FindObjectsSortMode.None)) b.transform.rotation = Quaternion.LookRotation(go.transform.forward, Vector3.up);
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                RenderTexture.active = null; cam.targetTexture = null;
                File.WriteAllBytes(System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DevBridge", "out", a[3]), tex.EncodeToPNG());
                Object.DestroyImmediate(tex); rt.Release();
            }
            finally { Object.DestroyImmediate(go); }
        }

        public static void DumpObject(string name)
        {
            var go = GameObject.Find(name); var sb = new StringBuilder();
            if (!go) sb.AppendLine("not found " + name);
            else foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var mf = t.GetComponent<MeshFilter>(); var smr = t.GetComponent<SkinnedMeshRenderer>();
                var mb = mf && mf.sharedMesh ? mf.sharedMesh.bounds : smr && smr.sharedMesh ? smr.sharedMesh.bounds : new Bounds();
                sb.AppendLine(Path(t) + " lp=" + F(t.localPosition) + " lr=" + t.localEulerAngles + " ls=" + F(t.localScale) + " wp=" + F(t.position) + " meshCenter=" + F(mb.center) + " meshSize=" + F(mb.size));
            }
            File.WriteAllText(System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DevBridge", "out", "dump.txt"), sb.ToString());
        }

        public static void DumpRig(string prefab)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/" + prefab + ".prefab");
            var sb = new StringBuilder();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                sb.AppendLine("SMR " + smr.name + " root=" + (smr.rootBone ? smr.rootBone.name : "-") + " bones=" + string.Join("|", smr.bones.Select(b => b ? b.name : "null")) + " blendshapes=" + (smr.sharedMesh ? smr.sharedMesh.blendShapeCount : -1) + " verts=" + (smr.sharedMesh ? smr.sharedMesh.vertexCount : 0));
            }
            var an = go.GetComponentInChildren<Animator>(true);
            if (an && an.runtimeAnimatorController) foreach (var c in an.runtimeAnimatorController.animationClips)
            {
                sb.AppendLine("clip " + c.name + " len=" + c.length);
                foreach (var b in AnimationUtility.GetCurveBindings(c)) sb.AppendLine("   " + b.path + " : " + b.propertyName);
            }
            File.WriteAllText(System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DevBridge", "out", "rig_" + prefab + ".txt"), sb.ToString());
        }

        static void Walk(Transform t, int d, StringBuilder sb)
        {
            if (d > 7) return;
            sb.AppendLine(new string(' ', d * 2) + t.name + "  lp=" + F(t.localPosition) + " lr=" + t.localEulerAngles + " ls=" + F(t.localScale) + "  [" + string.Join(",", t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "null").Where(c => c != "Transform")) + "]");
            foreach (Transform c in t) Walk(c, d + 1, sb);
        }
        static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
        static string F(Vector3 v) => $"({v.x:0.##},{v.y:0.##},{v.z:0.##})";
    }
}
