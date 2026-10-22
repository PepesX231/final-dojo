using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.Boss
{
    /// <summary>
    /// Deterministic NavMesh path follower for the chair (no NavMeshAgent update loop, so the boss can be
    /// stepped with a fixed dt in tests exactly like the player motor). Movement follows path corners;
    /// the body turns toward travel at a limited rate so sharp turns read as momentum (§20.4).
    /// </summary>
    public sealed class ChairLocomotion
    {
        readonly Transform _t;
        readonly List<Vector3> _corners = new List<Vector3>(16);
        int _next;

        public float Speed { get; private set; }
        public float TargetSpeed { get; set; }
        public Vector3 Velocity { get; private set; }
        public bool HasPath => _next < _corners.Count;
        public Vector3 Destination => _corners.Count > 0 ? _corners[_corners.Count - 1] : _t.position;
        public float RemainingDistance
        {
            get
            {
                if (!HasPath) return 0f;
                float d = Vector3.Distance(_t.position, _corners[_next]);
                for (int i = _next; i + 1 < _corners.Count; i++) d += Vector3.Distance(_corners[i], _corners[i + 1]);
                return d;
            }
        }

        public ChairLocomotion(Transform t) { _t = t; }

        public void SetPath(List<Vector3> corners)
        {
            _corners.Clear();
            _corners.AddRange(corners);
            _next = _corners.Count > 1 ? 1 : 0;
        }

        public void Stop(bool hard)
        {
            _corners.Clear();
            _next = 0;
            TargetSpeed = 0f;
            if (hard) { Speed = 0f; Velocity = Vector3.zero; }
        }

        /// <summary>Straight-line dash on the NavMesh (reveal kick). Returns false if blocked.</summary>
        public bool SetDirectTarget(Vector3 target)
        {
            if (NavMesh.Raycast(_t.position, target, out var hit, NavMesh.AllAreas)) target = hit.position;
            SetPath(new List<Vector3> { _t.position, target });
            return true;
        }

        static readonly NavMeshPath s_path = new NavMeshPath();

        /// <summary>Follow the NavMesh around obstacles to <paramref name="target"/> (fishing-rod reel). Falls back to a direct dash.</summary>
        public bool SetPathTo(Vector3 target)
        {
            if (NavMesh.SamplePosition(target, out var th, 1.5f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(_t.position, th.position, NavMesh.AllAreas, s_path) && s_path.status != NavMeshPathStatus.PathInvalid)
            {
                SetPath(new List<Vector3>(s_path.corners));
                return true;
            }
            return SetDirectTarget(target);
        }

        public void Step(float dt, float acceleration, float turnRate)
        {
            Speed = Mathf.MoveTowards(Speed, HasPath ? TargetSpeed : 0f, acceleration * dt);
            Vector3 dir = Vector3.zero;
            if (HasPath)
            {
                Vector3 to = _corners[_next] - _t.position; to.y = 0f;
                float step = Speed * dt;
                while (HasPath && to.magnitude <= Mathf.Max(0.05f, step))
                {
                    _next++;
                    if (!HasPath) break;
                    to = _corners[_next] - _t.position; to.y = 0f;
                }
                if (HasPath && to.sqrMagnitude > 1e-6f) dir = to.normalized;
            }
            Velocity = dir * Speed;
            Vector3 pos = _t.position + Velocity * dt;
            if (NavMesh.SamplePosition(pos, out var hit, 1.0f, NavMesh.AllAreas)) pos = hit.position;
            _t.position = pos;
            if (dir.sqrMagnitude > 1e-6f)
                _t.rotation = Quaternion.RotateTowards(_t.rotation, Quaternion.LookRotation(dir, Vector3.up), turnRate * dt);
        }
    }
}
