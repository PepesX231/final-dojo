using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Batch entry points for the two-storey house: rebuild the level and capture review images
    /// (exterior, ground plan, upper plan, in-game views). Writes to docs/evidence/mama-two-storey/.
    public static class MamaTwoStoreyReview
    {
        const string Out = "docs/evidence/mama-two-storey";

        static bool Between(CutawayWall w, Vector3 cam, Vector3 target)
        {
            Vector3 o = w.transform.position;
            float a = Vector3.Dot(cam - o, w.normal), b = Vector3.Dot(target - o, w.normal);
            if (a * b >= 0f) return false;
            Vector3 hit = cam + (target - cam) * (a / (a - b));
            Vector3 along = Vector3.Cross(Vector3.up, w.normal).normalized;
            return Mathf.Abs(Vector3.Dot(hit - o, along)) < w.halfExtent.x + 1.4f && Mathf.Abs(hit.y - o.y) < w.halfExtent.y + 1f;
        }

        /// Lists every collider straight above the ground under the chute hole (should be none above the hallway floor).
        public static void ProbeChute()
        {
            EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity"); Physics.SyncTransforms();
            var hits = Physics.RaycastAll(new Vector3(27.5f, 20f, 14f), Vector3.down, 40f, ~0, QueryTriggerInteraction.Collide);
            File.WriteAllText(Path.Combine(Out, "probe-chute.txt"), string.Join("\n", hits.OrderBy(h => -h.point.y).Select(h => h.point.y.ToString("0.00") + "  " + h.collider.name + "  <" + (h.collider.transform.parent ? h.collider.transform.parent.name : "") + ">  trigger=" + h.collider.isTrigger)));
        }

        public static void BuildAndCapture()
        {
            MamaLevelBuilder.Build();
            Capture();
        }

        [MenuItem("Mama's Coming/Review/Capture Two-Storey House")]
        public static void Capture()
        {
            Directory.CreateDirectory(Out);
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/MamasComing/Scenes/MamasKitchen.unity") scene = EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity");
            var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToList();
            var roof = GameObject.Find("House/Roof");
            bool IsRoof(Renderer r) => roof && r.transform.IsChildOf(roof.transform);
            bool IsUpper(Renderer r) { var b = r.bounds; return b.min.y >= HouseLayout.WallH - 0.02f && b.max.x <= HouseLayout.UpperMaxX + 0.4f && b.min.x >= -0.4f && b.min.z >= -0.4f && b.max.z <= HouseLayout.UpperMaxZ + 0.4f; }
            var report = new List<string>();

            void Shot(string file, Vector3 pos, Vector3 look, float fov, bool ortho, float size, System.Func<Renderer, bool> hide)
            {
                // like the in-game cut-away: walls between the camera and what it looks at are hidden
                var cutWalls = ortho ? new List<Renderer>() : Object.FindObjectsByType<CutawayWall>(FindObjectsSortMode.None).Where(w => Between(w, pos, look)).SelectMany(w => w.GetComponentsInChildren<Renderer>()).ToList();
                var hidden = all.Where(r => r && (hide(r) || cutWalls.Contains(r))).ToList();
                bool fog = RenderSettings.fog; RenderSettings.fog = false;
                foreach (var r in hidden) r.enabled = false;
                var go = new GameObject("ReviewCam"); var cam = go.AddComponent<Camera>();
                go.transform.position = pos; if (ortho) go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); else go.transform.LookAt(look);
                cam.fieldOfView = fov; cam.orthographic = ortho; cam.orthographicSize = size; cam.farClipPlane = 400f; cam.nearClipPlane = 0.1f;
                cam.clearFlags = CameraClearFlags.Skybox;
                var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderPostProcessing = true;
                var rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
                cam.Render(); cam.Render();
                RenderTexture.active = rt; var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(Out, file), tex.EncodeToPNG());
                cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
                foreach (var r in hidden) if (r) r.enabled = true;
                RenderSettings.fog = fog;
                report.Add(file);
            }

            float U = HouseLayout.UpperY;
            Shot("exterior.png", new Vector3(-22f, 34f, -34f), new Vector3(30f, 3f, 14f), 45f, false, 0f, r => false);
            Shot("plan-ground.png", new Vector3(30f, 80f, 14f), new Vector3(30f, 0f, 14f), 30f, true, 19.5f, r => IsRoof(r) || IsUpper(r));
            Shot("plan-upper.png", new Vector3(15f, 80f, 14f), new Vector3(15f, U, 14f), 30f, true, 15f, r => IsRoof(r));
            Shot("view-living.png", new Vector3(9f, 9f, -3f), new Vector3(9f, 0.5f, 6f), 50f, false, 0f, r => IsRoof(r) || IsUpper(r));
            Shot("view-staircase.png", new Vector3(7f, 11f, 12f), new Vector3(7f, 2f, 24f), 50f, false, 0f, r => IsRoof(r) || IsUpper(r));
            Shot("view-bedroom.png", new Vector3(7f, U + 9f, -3f), new Vector3(7f, U + 0.5f, 6f), 50f, false, 0f, r => IsRoof(r));
            Shot("view-loft.png", new Vector3(22f, U + 9f, 13f), new Vector3(22f, U + 0.5f, 22f), 50f, false, 0f, r => IsRoof(r));
            Shot("view-kitchen.png", new Vector3(34f, 10f, 13f), new Vector3(34f, 0.5f, 22f), 50f, false, 0f, r => IsRoof(r) || IsUpper(r));
            Shot("view-garage.png", new Vector3(52f, 10f, -3f), new Vector3(52f, 0.5f, 6f), 50f, false, 0f, r => IsRoof(r));

            // the art thread's layering audit (scene-layering-qa): z-fighting, floating/buried props, strays below the floor
            string audit = MamasComing.Style.Editor.MamaLayerAudit.AuditActiveScene(Out, true);
            int hazards = Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).Length;
            int gadgets = Object.FindObjectsByType<GadgetHazard>(FindObjectsSortMode.None).Length;
            File.WriteAllText(Path.Combine(Out, "capture.txt"), "scene=" + scene.path + "\nhazards=" + hazards + " (gadgets " + gadgets + ")\nimages=" + string.Join(", ", report) + "\nlayer audit: " + audit + "\n");
            Debug.Log("[Mama's Coming] two-storey review captured: " + string.Join(", ", report));
        }
    }
}
