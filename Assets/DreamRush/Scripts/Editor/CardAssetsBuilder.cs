using DreamRush.Cards;
using DreamRush.DreamLogic;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>Creates/updates the 6 starter cards + 3 Dream Laws (GDD §17.2) and the Resources CardLibrary.</summary>
    public static class CardAssetsBuilder
    {
        const string CardDir = "Assets/DreamRush/ScriptableObjects/Cards";
        const string IconDir = "Assets/DreamRush/Art/Illustrations/Cards";
        const string LibDir = "Assets/DreamRush/Resources/DreamRush";

        [MenuItem("DreamRush/Build/Card Assets")]
        public static CardLibrary Build()
        {
            GrayboxAssets.EnsureFolder(CardDir);
            GrayboxAssets.EnsureFolder(LibDir);
            var lib = AssetDatabase.LoadAssetAtPath<CardLibrary>(LibDir + "/CardLibrary.asset");
            if (lib == null) { lib = ScriptableObject.CreateInstance<CardLibrary>(); AssetDatabase.CreateAsset(lib, LibDir + "/CardLibrary.asset"); }
            lib.starterPool.Clear(); lib.dreamLaws.Clear();

            lib.starterPool.Add(Card("SuperGlue", "SUPER GLUE", CardCategory.Tool, CardTargetMode.Target, 6f, 70f, 10f, 4.5f, EffectAction.Root,
                "Splat! Sticks a moving thing in place for a moment.", "CARD_SuperGlue"));
            lib.starterPool.Add(Card("CameraFlash", "CAMERA FLASH", CardCategory.Tool, CardTargetMode.Target, 8f, 40f, 12f, 3f, EffectAction.Freeze,
                "Freezes whatever is right in front of the lens.", "CARD_CameraFlash"));
            lib.starterPool.Add(Card("FishingRod", "FISHING ROD", CardCategory.Tool, CardTargetMode.Target, 11f, 50f, 12f, 1.2f, EffectAction.Pull,
                "Reels a far-away thing toward you. It wobbles after.", "CARD_FishingRod"));
            lib.starterPool.Add(Card("BeAGrandpa", "BE A GRANDPA", CardCategory.Transformation, CardTargetMode.Self, 0f, 0f, 22f, 8f, EffectAction.ChangeBehavior,
                "Become an elder apple. Polite things offer you a seat.", "CARD_BeAGrandpa"));
            lib.starterPool.Add(Card("DecoyApple", "DECOY APPLE", CardCategory.Spawn, CardTargetMode.Point, 0f, 0f, 14f, 6f, EffectAction.SpawnDecoy,
                "Drops a cardboard you. Easily fooled things run from it.", "CARD_DecoyApple"));
            lib.starterPool.Add(Card("BouncyCushion", "BOUNCY CUSHION", CardCategory.Spawn, CardTargetMode.Point, 0f, 0f, 10f, 12f, EffectAction.CreatePlatform,
                "A springy cushion. Land on it to fly high.", "CARD_BouncyCushion"));

            lib.dreamLaws.Add(Law("LawNoJump", "NO JUMPING", DreamLawRule.NoJump, "Your legs forgot how to jump.", "LAW_NoJump"));
            lib.dreamLaws.Add(Law("LawNoSprint", "NO RUNNING", DreamLawRule.NoSprint, "Walking only. Very proper.", "LAW_NoSprint"));
            lib.dreamLaws.Add(Law("LawSlippery", "BUTTERED FLOOR", DreamLawRule.Slippery, "Everything is slippery!", "LAW_Slippery"));
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DreamRush] Card library: {lib.starterPool.Count} cards, {lib.dreamLaws.Count} dream laws");
            return lib;
        }

        static CardDefinition Load(string id)
        {
            string path = $"{CardDir}/CARD_{id}.asset";
            var c = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (c == null) { c = ScriptableObject.CreateInstance<CardDefinition>(); AssetDatabase.CreateAsset(c, path); }
            return c;
        }

        static Texture2D Icon(string name)
        {
            string p = $"{IconDir}/{name}.png";
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && !imp.alphaIsTransparency) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        static CardDefinition Card(string id, string name, CardCategory cat, CardTargetMode mode, float range, float cone, float cd, float dur,
            EffectAction action, string desc, string icon)
        {
            var c = Load(id);
            c.cardId = id; c.displayName = name; c.category = cat; c.targetMode = mode; c.range = range; c.coneDegrees = cone;
            c.cooldown = cd; c.duration = dur; c.action = action; c.description = desc; c.icon = Icon(icon); c.lawRule = DreamLawRule.None;
            EditorUtility.SetDirty(c);
            return c;
        }

        static CardDefinition Law(string id, string name, DreamLawRule rule, string desc, string icon)
        {
            var c = Load(id);
            c.cardId = id; c.displayName = name; c.category = CardCategory.DreamLaw; c.targetMode = CardTargetMode.None;
            c.lawRule = rule; c.duration = 15f; c.cooldown = 0f; c.description = desc; c.icon = Icon(icon);
            EditorUtility.SetDirty(c);
            return c;
        }
    }
}
