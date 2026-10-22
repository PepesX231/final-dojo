using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>An authored set piece: fires once when Pip comes within <see cref="radius"/> (WonderDirector plays it).</summary>
    public class WonderEvent : MonoBehaviour
    {
        public string kind = "PaperPlanes";
        public float radius = 18f;
        public int count = 3;   // minions for "Ambush"
        public bool Fired { get; set; }
    }
}
