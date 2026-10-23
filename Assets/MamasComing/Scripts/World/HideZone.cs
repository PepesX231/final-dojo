using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>Volume inside a cabinet / under the sofa. Things inside are "hidden" while the door is closed.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class HideZone : MonoBehaviour
    {
        public static readonly List<HideZone> All = new List<HideZone>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public Cabinet cabinet;
        [Range(0, 1)] public float momCheckChance = 0.3f;
        [HideInInspector] public bool checkedByMom;

        BoxCollider box;

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool Contains(Vector3 p)
        {
            if (!box) box = GetComponent<BoxCollider>();
            Vector3 l = transform.InverseTransformPoint(p) - box.center;
            Vector3 h = box.size * 0.5f + Vector3.one * 0.05f;
            return Mathf.Abs(l.x) <= h.x && Mathf.Abs(l.y) <= h.y && Mathf.Abs(l.z) <= h.z;
        }

        public bool Concealing => cabinet == null || !cabinet.hasDoor || !cabinet.IsOpen;

        public static HideZone ZoneAt(Vector3 p)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] && All[i].Contains(p)) return All[i];
            return null;
        }

        public static bool IsHidden(Vector3 p)
        {
            var z = ZoneAt(p);
            return z != null && z.Concealing;
        }
    }
}
