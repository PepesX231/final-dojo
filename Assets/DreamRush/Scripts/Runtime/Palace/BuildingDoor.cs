using System.Collections;
using System.Collections.Generic;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>What a building is for once you step inside (M15: "every building can be entered and has a purpose").</summary>
    public enum RoomType { Flat, Watchmaker, Cafe, Dojo, Library, Vault }

    /// <summary>
    /// A street door. Every building in the city has one (glowing frame coloured by what's inside, a hanging sign):
    /// E = go in. The door dims once you've cleared what's inside, so the city tells you where you haven't been.
    /// </summary>
    public class BuildingDoor : Interactable
    {
        public string id = "B000";
        public RoomType type = RoomType.Flat;
        public int seed;
        public string sign = "";
        public Renderer glow;
        public Transform outside;               // where Pip stands when he comes back out
        public bool Cleared => PalaceDirector.Current != null && PalaceDirector.Current.Taken("door:" + id);

        public override string Prompt => "ENTER — " + (string.IsNullOrEmpty(sign) ? TypeName(type) : sign) + (Cleared ? " ✓" : "");
        public static string TypeName(RoomType t) => t switch
        {
            RoomType.Watchmaker => "WATCHMAKER (SHOP)", RoomType.Cafe => "MOONLIGHT CAFE (REST)", RoomType.Dojo => "SHADOW DOJO (TRIAL)",
            RoomType.Library => "CLOCK LIBRARY (MAP)", RoomType.Vault => "TIME VAULT", _ => "DREAMER'S FLAT"
        };
        public static Color TypeColor(RoomType t) => t switch
        {
            RoomType.Watchmaker => new Color(1f, 0.8f, 0.3f), RoomType.Cafe => new Color(1f, 0.5f, 0.75f), RoomType.Dojo => new Color(1f, 0.25f, 0.3f),
            RoomType.Library => new Color(0.4f, 0.95f, 1f), RoomType.Vault => new Color(0.75f, 0.5f, 1f), _ => new Color(0.95f, 0.9f, 0.75f)
        };

        protected override void OnEnable() { base.OnEnable(); prompt = "ENTER"; interactable = false; Refresh(); }   // M17: walk-in doorway, nothing to press
        protected override void OnInteract(InteractionContext context) { }

        public void Refresh()
        {
            if (glow == null) return;
            var mpb = new MaterialPropertyBlock();
            Color c = TypeColor(type) * (Cleared ? 0.25f : 1.15f);
            mpb.SetColor("_Emission", c); mpb.SetColor("_BaseColor", TypeColor(type));
            glow.SetPropertyBlock(mpb);
        }
    }
}
