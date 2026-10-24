using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Off-screen line-up renders of the sculpted kid prefabs as Unity imports them (toon materials, rig, clips),
    /// without opening a scene or entering play mode. Writes docs/evidence/sculpted-kids/unity_*.png.
    public static class MamaSculptPreview
    {
        const string Out = "docs/evidence/sculpted-kids";

        [MenuItem("Mama's Coming/Sculpted Kids/Render Unity Preview")]
        public static void Render()
        {
            Directory.CreateDirectory(Out);
            Shot("Idle", 0f, 0f, "warmup");   // the first preview render after a reload comes out empty
            File.Delete(Out + "/unity_lineup_warmup.png");
            foreach (var (clip, t) in new[] { ("Idle", .25f), ("Punch", .4f), ("Jump", .45f), ("Taunt", .3f), ("Walk", .2f) })
                Shot(clip, t, 0f);
            Shot("Idle", .25f, 140f, "back");
            Debug.Log("[Mama's Coming] sculpted kid previews written to " + Out);
        }

        static void Shot(string clip, float t, float yaw, string tag = null)
        {
            var pru = new PreviewRenderUtility();
            try
            {
                pru.camera.fieldOfView = 30; pru.camera.nearClipPlane = .1f; pru.camera.farClipPlane = 50;
                pru.camera.clearFlags = CameraClearFlags.SolidColor; pru.camera.backgroundColor = new Color(1f, .9f, .62f);
                pru.camera.transform.SetPositionAndRotation(new Vector3(0, 1.3f, -6.2f), Quaternion.Euler(6, 0, 0));
                pru.lights[0].intensity = 1.2f; pru.lights[0].transform.rotation = Quaternion.Euler(45, -30, 0); pru.lights[0].color = new Color(1f, .97f, .9f);
                pru.lights[1].intensity = .5f;
                pru.ambientColor = new Color(.6f, .58f, .7f);
                for (int i = 0; i < MamaSculptKids.Names.Length; i++)
                {
                    string n = MamaSculptKids.Names[i];
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Art/Sculpted/" + n + "/" + n + "_Sculpt.fbx");
                    if (!fbx) continue;
                    var go = pru.InstantiatePrefabInScene(fbx);
                    go.transform.position = new Vector3((i - 2) * 1.3f, 0, 0);
                    go.transform.rotation = Quaternion.Euler(0, 180f + yaw + (i - 2) * -10f, 0);
                    var ac = AssetDatabase.LoadAllAssetsAtPath("Assets/MamasComing/Art/Sculpted/" + n + "/" + n + "_Sculpt.fbx")
                        .OfType<AnimationClip>().FirstOrDefault(c => c.name == n + "_" + clip);
                    if (ac) ac.SampleAnimation(go, ac.length * t);
                }
                pru.BeginStaticPreview(new Rect(0, 0, 1600, 600));
                pru.Render(true);
                var tex = pru.EndStaticPreview();
                var rt = RenderTexture.GetTemporary(tex.width, tex.height);
                Graphics.Blit(tex, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var png = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); png.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                File.WriteAllBytes(Out + "/unity_lineup_" + (tag ?? clip) + ".png", png.EncodeToPNG());
                Object.DestroyImmediate(png);
            }
            finally { pru.Cleanup(); }
        }
    }
}
