using DreamRush.Player;
using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>Updraft: while gliding inside the column you rise (Crystal Cliffs, island hops). Visual = rising streaks.</summary>
    public class WindColumn : MonoBehaviour
    {
        public float radius = 3.5f, height = 30f, accel = 34f;
        PlayerAbilities _ab; Transform _p;
        void Start() { _ab = FindFirstObjectByType<PlayerAbilities>(); if (_ab != null) _p = _ab.transform; }
        void Update()
        {
            if (_ab == null) return;
            Vector3 d = _p.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude < radius && d.y > -1f && d.y < height) _ab.Updraft = accel;
        }
    }
}
