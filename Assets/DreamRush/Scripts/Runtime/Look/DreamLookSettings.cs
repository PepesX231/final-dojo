using UnityEngine;

namespace DreamRush.Look
{
    /// <summary>Shader references so runtime-made materials survive builds (Resources/DreamRush/DreamLook).</summary>
    [CreateAssetMenu(menuName = "DreamRush/Dream Look Settings", fileName = "DreamLook")]
    public class DreamLookSettings : ScriptableObject
    {
        public const string ResourcePath = "DreamRush/DreamLook";
        public Shader toon;
        public Shader outline;
        public Material outlineMaterial;
        [Header("Defaults (Art Bible §5)")]
        public Color shadowTint = new Color(0.55f, 0.47f, 0.72f);
        public Color ink = new Color(0.188f, 0.165f, 0.239f);

        static DreamLookSettings _cached;
        public static DreamLookSettings Instance => _cached != null ? _cached : (_cached = Resources.Load<DreamLookSettings>(ResourcePath));
    }
}
