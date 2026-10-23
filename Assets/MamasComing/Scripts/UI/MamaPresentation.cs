using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MamasComing
{
    /// <summary>Small runtime-only readability pass; does not rebuild the authored house or shared assets.</summary>
    public class MamaPresentation : MonoBehaviour
    {
        GameManager gm;
        GameObject oilLabel;
        Text oilText;
        Volume volume;
        VolumeProfile previousProfile, runtimeProfile;

        void Start()
        {
            gm = GetComponent<GameManager>();
            volume = FindFirstObjectByType<Volume>();
            if (volume && volume.sharedProfile)
            {
                previousProfile = volume.sharedProfile;
                runtimeProfile = Instantiate(previousProfile);
                // VolumeProfile.Clone does not clone its ScriptableObject components.
                runtimeProfile.components.Clear();
                foreach (var component in previousProfile.components) runtimeProfile.components.Add(Instantiate(component));
                volume.sharedProfile = runtimeProfile;
                if (runtimeProfile.TryGet<Bloom>(out var bloom)) bloom.intensity.Override(0.18f);
                if (runtimeProfile.TryGet<ColorAdjustments>(out var colors))
                {
                    colors.saturation.Override(6f); colors.contrast.Override(10f); colors.postExposure.Override(0.02f);
                }
            }
            if (!gm.stove) return;
            oilLabel = new GameObject("Oil warning", typeof(RectTransform), typeof(Canvas));
            var canvas = oilLabel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)oilLabel.transform; rect.sizeDelta = new Vector2(420, 105); rect.localScale = Vector3.one * 0.003f;
            var backing = oilLabel.AddComponent<Image>(); backing.color = new Color(1f, 0.83f, 0.39f, 0.97f); backing.raycastTarget = false;
            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text)); textGO.transform.SetParent(rect, false);
            var tr = (RectTransform)textGO.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 6); tr.offsetMax = new Vector2(-10, -6);
            oilText = textGO.GetComponent<Text>(); oilText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            oilText.fontSize = 28; oilText.alignment = TextAnchor.MiddleCenter; oilText.fontStyle = FontStyle.Bold;
            oilText.color = new Color(0.2f, 0.18f, 0.14f); oilText.raycastTarget = false;
            oilText.text = MamaReadability.Text("OIL FIRE\nNO WATER", "ไฟน้ำมัน\nห้ามสาดน้ำ");
            oilLabel.transform.position = gm.stove.burner.position + Vector3.up * 0.7f;
        }

        void LateUpdate()
        {
            if (!oilLabel || !gm || !gm.player || !Camera.main) return;
            bool gameplay = gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome;
            bool relevant = gm.stove.on || (gm.pot && gm.pot.Fire && gm.pot.Fire.burning);
            bool nearby = Vector3.Distance(gm.player.transform.position, gm.stove.transform.position) < 5f;
            oilLabel.SetActive(gameplay && relevant && nearby);
            oilText.text = MamaReadability.Text("OIL FIRE\nNO WATER", "ไฟน้ำมัน\nห้ามสาดน้ำ");
            oilLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            if (oilLabel) Destroy(oilLabel);
            if (volume && volume.sharedProfile == runtimeProfile) volume.sharedProfile = previousProfile;
            if (runtimeProfile)
            {
                foreach (var component in runtimeProfile.components) if (component) Destroy(component);
                Destroy(runtimeProfile);
            }
        }
    }
}
