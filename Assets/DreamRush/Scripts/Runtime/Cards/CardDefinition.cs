using DreamRush.DreamLogic;
using UnityEngine;

namespace DreamRush.Cards
{
    public enum CardCategory { Tool, Transformation, Spawn, DreamLaw }
    public enum CardTargetMode { None, Target, Self, Point }
    public enum DreamLawRule { None, NoJump, NoSprint, Slippery }

    /// <summary>Authoring data for a dream card (GDD §17.6). Runtime state (cooldowns) lives elsewhere.</summary>
    [CreateAssetMenu(menuName = "DreamRush/Card Definition", fileName = "CARD_")]
    public class CardDefinition : ScriptableObject
    {
        public string cardId = "SuperGlue";
        public string displayName = "SUPER GLUE";
        public CardCategory category = CardCategory.Tool;
        [Range(1, 3)] public int rarity = 1;
        public CardTargetMode targetMode = CardTargetMode.Target;
        public float range = 6f;
        [Tooltip("Aim cone (degrees) in front of the apple for targeted cards.")]
        public float coneDegrees = 60f;
        public float cooldown = 10f;
        public float duration = 4f;
        public EffectAction action = EffectAction.Root;
        [Tooltip("Dream Laws only: which rule is forbidden.")]
        public DreamLawRule lawRule = DreamLawRule.None;
        [TextArea] public string description = "Stops a moving object.";
        public Texture2D icon;
        public string vfxRef;
        public string sfxRef;

        public DreamProperty SupportedProperty => EffectContext.RequiredProperty(action);
    }
}
