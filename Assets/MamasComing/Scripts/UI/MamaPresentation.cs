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
            if (!GetComponent<MamaActionVfx>()) gameObject.AddComponent<MamaActionVfx>();
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
                    colors.saturation.Override(14f); colors.contrast.Override(12f); colors.postExposure.Override(.65f);   // was -0.08: this runtime pass was darkening the whole game (owner: brighter)
                }
                if(runtimeProfile.TryGet<Tonemapping>(out var tone))tone.mode.Override(TonemappingMode.Neutral);
                if(runtimeProfile.TryGet<Vignette>(out var vignette))vignette.intensity.Override(.12f);
                SoftEvening(runtimeProfile);
            }
            if (!gm.stove) return;
            oilLabel = new GameObject("Oil warning", typeof(RectTransform), typeof(Canvas));
            var canvas = oilLabel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)oilLabel.transform; rect.sizeDelta = new Vector2(420, 105); rect.localScale = Vector3.one * 0.0021f;
            var backing = oilLabel.AddComponent<Image>(); backing.color = new Color(1f, 0.83f, 0.39f, 0.97f); backing.raycastTarget = false;
            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text)); textGO.transform.SetParent(rect, false);
            var tr = (RectTransform)textGO.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 6); tr.offsetMax = new Vector2(-10, -6);
            oilText = textGO.GetComponent<Text>(); oilText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            oilText.fontSize = 28; oilText.alignment = TextAnchor.MiddleCenter; oilText.fontStyle = FontStyle.Bold;
            oilText.color = new Color(0.2f, 0.18f, 0.14f); oilText.raycastTarget = false;
            oilText.text = MamaReadability.Text("OIL FIRE\nNO WATER", "ไฟน้ำมัน\nห้ามสาดน้ำ");
            oilLabel.transform.position = gm.stove.burner.position + Vector3.up * 0.7f;
        }

        /// Owner: softer, cooler, "early evening" look. Gentle on purpose - the house layout and colours stay recognisable.
        void SoftEvening(VolumeProfile prof)
        {
            if (!prof.TryGet<WhiteBalance>(out var wb)) wb = prof.Add<WhiteBalance>(true);
            wb.active = true; wb.temperature.Override(-16f); wb.tint.Override(5f);              // cool blue with a hint of lilac
            if (!prof.TryGet<SplitToning>(out var split)) split = prof.Add<SplitToning>(true);
            split.active = true; split.shadows.Override(new Color(.42f, .50f, .72f)); split.highlights.Override(new Color(1f, .80f, .66f)); split.balance.Override(-15f);   // blue shadows, peach sunset highlights
            if (prof.TryGet<ColorAdjustments>(out var c)) { c.contrast.Override(4f); c.saturation.Override(6f); c.postExposure.Override(.55f); }   // softer contrast
            if (prof.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.38f); bloom.threshold.Override(.9f); bloom.scatter.Override(.75f); }   // dreamy glow on lights
            if (prof.TryGet<Vignette>(out var v)) { v.intensity.Override(.16f); v.smoothness.Override(.6f); v.color.Override(new Color(.10f, .12f, .25f)); }

            // low, warm evening sun + cool sky fill
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.color = new Color(1f, .78f, .60f); l.intensity *= .82f; l.shadowStrength = Mathf.Min(l.shadowStrength, .65f);
            }
            if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, new Color(.55f, .62f, .85f), .5f);
                RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, new Color(.72f, .66f, .78f), .5f);
                RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, new Color(.35f, .36f, .48f), .5f);
            }
            else RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(.62f, .68f, .90f), .45f);
        }

        void LateUpdate()
        {
            if (!oilLabel || !gm || !gm.player || !Camera.main) return;
            bool gameplay = gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome;
            bool relevant = gm.stove.on || (gm.pot && gm.pot.Fire && gm.pot.Fire.burning);
            bool nearby = Vector3.Distance(gm.player.transform.position, gm.stove.transform.position) < 2.8f;
            oilLabel.SetActive(gameplay && relevant && nearby && !MamaSettings.Open && !(MiniGame.I && MiniGame.I.Active));
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
