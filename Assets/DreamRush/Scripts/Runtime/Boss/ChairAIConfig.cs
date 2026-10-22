using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>Chair boss tuning (GDD §15.4). Prototype targets — tune with bot runs and playtests.</summary>
    [CreateAssetMenu(menuName = "DreamRush/Chair AI Config", fileName = "ChairAIConfig")]
    public class ChairAIConfig : ScriptableObject
    {
        [Header("Perception")]
        public float awarenessRadius = 7f;
        public float escapeTriggerRadius = 4f;
        public float panicRadius = 2.5f;
        public float panicAfterSeconds = 1f;
        public float alertDuration = 0.4f;
        public float forgetAfterSeconds = 2f;

        [Header("Locomotion")]
        public float normalSpeed = 6.1f;
        public float panicSpeed = 7.2f;
        public float acceleration = 12f;
        public float turnRate = 280f;
        public float repositionInterval = 1.2f;

        [Header("Escape destinations")]
        public float minEscapeDistance = 5f;
        public float maxEscapeDistance = 11f;
        public float minDistanceFromPlayer = 4f;
        public float pathClearanceFromPlayer = 2f;
        public float candidateSpacing = 2.5f;
        public int topChoices = 3;
        public float recentMemorySeconds = 8f;

        [Header("Fatigue")]
        [Tooltip("GDD §15.4 starts at 18; raised to 36 after bot runs (perfect chaser won in ~14 s).")]
        public float exhaustionThreshold = 36f;
        public float fatigueRunRate = 1f;
        public float fatiguePanicRate = 1.5f;
        public float fatigueRestRate = 1.5f;
        public float exhaustionDuration = 3f;
        [Range(0, 1)] public float fatigueAfterExhaustion = 0.25f;

        [Header("Openings")]
        public float stumbleDuration = 1.5f;
        public float sharpTurnAngle = 110f;
        [Range(0, 1)] public float stumbleChanceOnSharpTurn = 0.35f;
        public float hesitationMin = 0.5f;
        public float hesitationMax = 0.9f;
        public float shortcutLandingRadius = 3f;
        public float shortcutMinFall = 1f;

        [Header("Seat (GDD §15.9)")]
        public float sitRange = 1.7f;
        public float sitDuration = 2f;

        [Header("Anti-softlock")]
        public float stuckSeconds = 2f;
        public float minY = -2f;

        public static ChairAIConfig CreateDefault()
        {
            var c = CreateInstance<ChairAIConfig>();
            c.name = "ChairAIConfig (runtime default)";
            return c;
        }
    }
}
