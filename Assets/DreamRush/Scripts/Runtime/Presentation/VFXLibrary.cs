using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>Particle sprite materials (Resources/DreamRush/VFXLibrary), built by the editor (DreamLookSetup).</summary>
    [CreateAssetMenu(menuName = "DreamRush/VFX Library", fileName = "VFXLibrary")]
    public class VFXLibrary : ScriptableObject
    {
        public const string ResourcePath = "DreamRush/VFXLibrary";
        public Material puff, sparkleAdd, glowAdd, ring, telegraph, streakAdd, beamAdd, confetti, z;
        static VFXLibrary _cached;
        public static VFXLibrary Instance => _cached != null ? _cached : (_cached = Resources.Load<VFXLibrary>(ResourcePath));
    }
}
