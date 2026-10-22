using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>Marks something the lock-on can target. Point = where the camera looks (chest/face).</summary>
    public class LockOnTarget : MonoBehaviour
    {
        public static readonly List<LockOnTarget> All = new List<LockOnTarget>();
        public Vector3 offset = new Vector3(0, 2.5f, 0);
        public bool lockable = true;
        public Vector3 Point => transform.position + offset;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }

}
