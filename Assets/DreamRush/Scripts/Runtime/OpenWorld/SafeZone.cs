using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>Pillow-fire light: THE ALARM never enters (docs/OPENWORLD_DESIGN.md).</summary>
    public class SafeZone : MonoBehaviour
    {
        public float radius = 9f;
        static readonly List<SafeZone> All = new List<SafeZone>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static bool Contains(Vector3 p)
        {
            foreach (var z in All) { Vector3 d = p - z.transform.position; d.y = 0; if (d.magnitude < z.radius) return true; }
            return false;
        }
    }
}
