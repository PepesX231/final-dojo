using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 the city's clock IS the sky: the palace starts at golden sunset (user: "the mood is stifling — make it
    /// beautiful"); every stolen hour you recover pushes it on — dusk → blue hour → midnight for the Count. Drives
    /// the sky material, sun/moon light, fog and ambient. Keys at t = 0, 0.5, 1.
    /// </summary>
    public class SkyClock : MonoBehaviour
    {
        [System.Serializable] public struct Key { public Color zenith, horizon, band, sun, light, fog, ambSky, ambEq, ambGround; public float elevation, lightIntensity, stars, fogEnd; }
        public Material sky;
        public Light sun;
        public float azimuth = 200f;            // where the sun sets (seen from the spawn looking north it backlights the city)
        public Key[] keys = new Key[3];
        public float T { get; private set; }
        public float Target { get; set; }

        void Start() { var pd = PalaceDirector.Current; Target = pd != null ? pd.Hours / 3f : 0f; T = Target; Apply(); }

        void Update()
        {
            var pd = PalaceDirector.Current; if (pd != null) Target = Mathf.Clamp01(pd.Hours / 3f);
            if (Mathf.Abs(T - Target) < 1e-4f) return;
            T = Mathf.MoveTowards(T, Target, Time.deltaTime / 6f);      // a slow, visible six-second sunset step
            Apply();
        }

        Key Sample(float t)
        {
            if (keys == null || keys.Length < 3) return default;
            float k = t < 0.5f ? t / 0.5f : (t - 0.5f) / 0.5f; Key a = t < 0.5f ? keys[0] : keys[1], b = t < 0.5f ? keys[1] : keys[2];
            return new Key
            {
                zenith = Color.Lerp(a.zenith, b.zenith, k), horizon = Color.Lerp(a.horizon, b.horizon, k), band = Color.Lerp(a.band, b.band, k), sun = Color.Lerp(a.sun, b.sun, k),
                light = Color.Lerp(a.light, b.light, k), fog = Color.Lerp(a.fog, b.fog, k), ambSky = Color.Lerp(a.ambSky, b.ambSky, k), ambEq = Color.Lerp(a.ambEq, b.ambEq, k), ambGround = Color.Lerp(a.ambGround, b.ambGround, k),
                elevation = Mathf.Lerp(a.elevation, b.elevation, k), lightIntensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, k), stars = Mathf.Lerp(a.stars, b.stars, k), fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, k)
            };
        }

        public void Apply()
        {
            var k = Sample(T);
            var dir = Quaternion.Euler(-k.elevation, azimuth, 0) * Vector3.forward;   // direction TO the sun
            if (sky != null)
            {
                sky.SetColor("_Zenith", k.zenith); sky.SetColor("_Horizon", k.horizon); sky.SetColor("_Band", k.band); sky.SetColor("_SunColor", k.sun);
                sky.SetFloat("_StarStrength", k.stars); var sd = dir; sd.y = Mathf.Max(sd.y, 0.02f); sky.SetVector("_SunDir", (Vector4)sd.normalized);
            }
            if (sun != null) { sun.transform.rotation = Quaternion.LookRotation(-dir); sun.color = k.light; sun.intensity = k.lightIntensity; }
            RenderSettings.fogColor = k.fog; RenderSettings.fogEndDistance = k.fogEnd;
            RenderSettings.ambientSkyColor = k.ambSky; RenderSettings.ambientEquatorColor = k.ambEq; RenderSettings.ambientGroundColor = k.ambGround;
        }
    }
}
