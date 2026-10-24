using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing.Editor
{
    /// Reproducible authoring pass for the single-floor, original-fruit iteration.
    public static class MamaFastIteration
    {
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("MamasKitchen.unity")) throw new InvalidOperationException("Open MamasKitchen first.");
            var catalog = Resources.Load<KidCatalog>("MamaKidCatalog");
            string root = "Assets/MamasComing/Prefabs/Characters/";
            string[] names = { "PF_AppleKid", "PF_Kid_Banana", "PF_Kid_Orange", "PF_Kid_Watermelon", "PF_Kid_WaxApple" };
            var originals = names.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(root + n + ".prefab")).ToArray();
            if (originals.Any(g => !g)) throw new InvalidOperationException("Original fruit prefab missing.");
            catalog.kids = originals; EditorUtility.SetDirty(catalog);
            foreach (var material in originals.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct())
            {
                if (material.HasProperty("_ShadowColor")) material.SetColor("_ShadowColor", new Color(.72f, .68f, .88f));
                if (material.HasProperty("_AmbientStrength")) material.SetFloat("_AmbientStrength", .7f);
                EditorUtility.SetDirty(material);
            }

            var bedroom = GameObject.Find("Rooms/Bedroom");
            Vector3 move = new Vector3(0f, -HouseLayout.UpperY, 16f);
            bool needsMove = bedroom && bedroom.transform.position.y > -1f;
            if (needsMove)
            {
                // The former staircase has the same 14 x 12 footprint as the bedroom.
                foreach (var prop in UnityEngine.Object.FindObjectsByType<MessyProp>(FindObjectsSortMode.None))
                    if (prop.homePosition.y > HouseLayout.UpperThreshold && prop.homePosition.x < 14f && prop.homePosition.z < 12f)
                    { prop.homePosition += move; if (!prop.transform.IsChildOf(bedroom.transform)) prop.transform.position += move; }
                foreach (var pickup in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                    if (!(pickup is MessyProp) && pickup.transform.position.y > HouseLayout.UpperThreshold && pickup.transform.position.x < 14f && pickup.transform.position.z < 12f && !pickup.transform.IsChildOf(bedroom.transform)) pickup.transform.position += move;
                bedroom.transform.position += move;
                var lamp = GameObject.Find("Rooms/BedroomLight"); if (lamp) lamp.transform.position += move;
            }
            string[] remove = { "House/UpperFloor", "House/Roof", "Rooms/Staircase", "Rooms/StairLight", "Rooms/CrashMat", "Rooms/UpstairsHall", "Rooms/UpHallLight", "Rooms/UpHallLight2", "Rooms/LandingLight", "Rooms/MomsRoom", "Rooms/MomsRoomLight", "Rooms/PartyLoft", "Rooms/LoftLight" };
            foreach (string path in remove) { var go = GameObject.Find(path); if (go) go.SetActive(false); }
            int retired = 0;
            foreach (var h in UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
                if (h.InteractPoint.y > HouseLayout.UpperThreshold) { h.gameObject.SetActive(false); retired++; }
            foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                if (p.transform.position.y > HouseLayout.UpperThreshold) p.gameObject.SetActive(false);
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (!(r is ParticleSystemRenderer) && r.bounds.min.y >= HouseLayout.UpperY && !r.GetComponentInParent<PlayerController>()) r.gameObject.SetActive(false);
            var mom = UnityEngine.Object.FindFirstObjectByType<MomController>();
            if (mom && needsMove)
            {
                foreach (var point in mom.route)
                    if (point.point && point.inspect && point.point.position.y > HouseLayout.UpperThreshold && point.point.position.x < 14f && point.point.position.z < 12f) point.point.position += move;
                mom.route = mom.route.Where(p => p.point && p.point.position.y < HouseLayout.UpperThreshold && !p.name.ToLowerInvariant().Contains("stair")).ToArray();
            }
            // A high warm key and soft blue fill retain shape without black interiors.
            var sun = GameObject.Find("Sun")?.GetComponent<Light>();
            if (sun) { sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f); sun.color = new Color(1f, .94f, .8f); sun.intensity = 1.35f; sun.shadowStrength = .5f; }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.78f, .85f, 1f);
            RenderSettings.ambientEquatorColor = new Color(.68f, .73f, .83f);
            RenderSettings.ambientGroundColor = new Color(.52f, .48f, .64f);
            RenderSettings.fog = false;
            // Match the restored chair's facing to the table instead of applying a global rotation.
            int chairs = 0;
            foreach (var p in UnityEngine.Object.FindObjectsByType<MessyProp>(FindObjectsSortMode.None))
            {
                if (p.tool != ToolType.Chair || p.homePosition.x < 30f) continue;
                Vector3 table = p.homePosition.z < 12f ? new Vector3(39f, 0, 5.6f) : new Vector3(34.6f, 0, 20.8f);
                Vector3 facing = table - p.homePosition; facing.y = 0f;
                if (facing.sqrMagnitude < .01f) continue;
                p.homeRotation = Quaternion.LookRotation(facing);
                p.transform.rotation = p.homeRotation; chairs++;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return "Saved single-floor house; restored 5 original fruits; retired upstairs hazards=" + retired + "; corrected chair homes=" + chairs;
        }
    }
}
