using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// Golden-hour air (FF field spirit): drifting pollen / light motes in a box that follows the camera, plus
    /// slow fireflies near the ground. World-space simulation so they don't swim with the camera.
    /// </summary>
    public class AmbientMotes : MonoBehaviour
    {
        public float boxSize = 60f;
        public Color colorA = new Color(1f, 0.88f, 0.6f, 0.9f), colorB = new Color(0.8f, 0.75f, 1f, 0.8f);
        ParticleSystem _ps;
        Transform _cam;

        void Start()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main; main.loop = true; main.startLifetime = 7f; main.startSpeed = 0.15f; main.maxParticles = 900;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _ps.emission; em.rateOverTime = 110f;
            var sh = _ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(boxSize, boxSize * 0.35f, boxSize);
            var noise = _ps.noise; noise.enabled = true; noise.strength = 0.35f; noise.frequency = 0.25f;
            var col = _ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var lib = VFXLibrary.Instance;
            var r = GetComponent<ParticleSystemRenderer>();
            if (lib != null) r.sharedMaterial = lib.glowAdd;
            _ps.Play();
            var c = Camera.main; _cam = c != null ? c.transform : null;
        }

        void LateUpdate()
        {
            if (_cam == null) { var c = Camera.main; if (c != null) _cam = c.transform; return; }
            transform.position = _cam.position + Vector3.up * 2f + _cam.forward * 12f;
        }
    }
}
