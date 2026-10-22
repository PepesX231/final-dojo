using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>Material references for 2D world-space effects (GDD §5.7 Type B). Lives in Resources so runtime systems can find it.</summary>
    [CreateAssetMenu(menuName = "DreamRush/FX Library", fileName = "FXLibrary")]
    public class FXLibrary : ScriptableObject
    {
        public const string ResourcePath = "DreamRush/FXLibrary";

        public Material exclaim, sweat, dizzy, poof, tired;
        [Header("M7")]
        public Material flash, glueSplat, dust, heart, confetti, ouch, decoyStandee;

        static FXLibrary _cached;
        public static FXLibrary Instance
        {
            get
            {
                if (_cached == null) _cached = Resources.Load<FXLibrary>(ResourcePath);
                return _cached;
            }
        }
    }
}
