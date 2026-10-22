using System;
using UnityEngine;

namespace DreamRush.Enemies
{
    /// <summary>
    /// LITTLE STOOL (GDD §19.3, §13.5): teaches the chair's core behaviour in a forgiving form.
    /// Runs away along a short, predictable waypoint loop when the player comes close, gets tired after a
    /// burst, and is "caught" by touching it. Slower than the walking player on purpose.
    /// </summary>
    public class LittleStoolController : MonoBehaviour
    {
        public enum StoolState { Idle, Alert, Flee, Tired, Caught }

        public Transform player;
        public Transform[] waypoints;
        public float noticeRadius = 6.5f;
        public float catchRadius = 0.95f;
        public float speed = 4.8f;
        public float burstDuration = 2.6f;
        public float tiredDuration = 1.6f;
        public float alertTime = 0.2f;
        public float hopHeight = 0.12f;

        public StoolState State { get; private set; } = StoolState.Idle;
        public event Action Caught;

        int _target;
        float _stateTime;
        Transform _visual;
        Vector3 _visualRest;

        void Awake()
        {
            _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            _visualRest = _visual.localPosition;
        }

        void Start()
        {
            if (player == null)
            {
                var m = FindFirstObjectByType<Player.PlayerMotor3D>();
                if (m != null) player = m.transform;
            }
        }

        void Update()
        {
            if (player == null || State == StoolState.Caught) return;
            float dt = Time.deltaTime;
            _stateTime += dt;
            Vector3 toPlayer = player.position - transform.position; toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist <= catchRadius) { Catch(); return; }

            switch (State)
            {
                case StoolState.Idle:
                    if (dist < noticeRadius) Set(StoolState.Alert);
                    break;
                case StoolState.Alert:
                    Face(toPlayer);
                    _visual.localPosition = _visualRest + Vector3.up * (Mathf.Abs(Mathf.Sin(_stateTime * 30f)) * 0.06f);
                    if (_stateTime >= alertTime) { PickWaypointAwayFrom(player.position); Set(StoolState.Flee); }
                    break;
                case StoolState.Flee:
                    RunTowardsWaypoint(dt);
                    if (_stateTime >= burstDuration) Set(StoolState.Tired);
                    break;
                case StoolState.Tired:
                    _visual.localPosition = _visualRest + Vector3.down * 0.05f;
                    _visual.localRotation = Quaternion.Euler(Mathf.Sin(_stateTime * 25f) * 3f, 0, 0);
                    if (_stateTime >= tiredDuration) Set(dist < noticeRadius ? StoolState.Alert : StoolState.Idle);
                    break;
            }
        }

        void RunTowardsWaypoint(float dt)
        {
            if (waypoints == null || waypoints.Length == 0) return;
            Vector3 target = waypoints[_target].position; target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.magnitude < 0.3f)
            {
                PickWaypointAwayFrom(player.position);
                return;
            }
            Vector3 step = to.normalized * speed * dt;
            transform.position += Vector3.ClampMagnitude(step, to.magnitude);
            Face(to);
            float hop = Mathf.Abs(Mathf.Sin(_stateTime * 22f)) * hopHeight;
            _visual.localPosition = _visualRest + Vector3.up * hop;
            _visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_stateTime * 22f) * 6f);
        }

        /// <summary>Predictable: choose the neighbouring waypoint (loop order) that is farther from the player.</summary>
        void PickWaypointAwayFrom(Vector3 p)
        {
            int n = waypoints.Length;
            int a = (_target + 1) % n, b = (_target - 1 + n) % n;
            _target = (waypoints[a].position - p).sqrMagnitude >= (waypoints[b].position - p).sqrMagnitude ? a : b;
        }

        void Face(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        }

        void Set(StoolState s)
        {
            State = s;
            _stateTime = 0f;
            _visual.localPosition = _visualRest;
            _visual.localRotation = Quaternion.identity;
        }

        public void Catch()
        {
            if (State == StoolState.Caught) return;
            Set(StoolState.Caught);
            _visual.localPosition = _visualRest + Vector3.down * 0.06f;
            Caught?.Invoke();
            if (UI.UIRoot.Exists) UI.UIRoot.Get().Toast("Caught it! Furniture here really doesn't like being approached...", 3f);
        }
    }
}
