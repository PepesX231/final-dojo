using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// THE CHAIR in the open world (docs/OPENWORLD_DESIGN.md): waits at a landmark, bolts when Pip comes close,
    /// running (NavMesh) to the landmark that best keeps it away from you. Its stamina drains while running →
    /// EXHAUSTED for a few seconds (jump on it or press E next to it to sit). Sitting 2 s = it drops a Dream Key and
    /// poofs to another region. A ground pound next to it trips it (Stumble = also sit-able).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class OpenWorldChair : MonoBehaviour
    {
        public enum State { Waiting, Alert, Flee, Exhausted, Stumble, Sitting, Gone }
        public List<Transform> landmarks = new List<Transform>();
        public Transform visual;
        public float noticeRadius = 24f, runSpeed = 7.6f, panicSpeed = 9f;
        public float stamina = 26f, staminaMax = 26f;      // seconds of running
        public float exhaustedSeconds = 4.5f, sitSeconds = 2f;
        public int keysCarried = 3;

        public State Current { get; private set; } = State.Waiting;
        public float StateTime { get; private set; }
        public int KeysDropped { get; private set; }
        public float SitProgress => Current == State.Sitting ? Mathf.Clamp01(StateTime / sitSeconds) : 0f;
        public System.Action<int> KeyDropped;

        NavMeshAgent _agent;
        PlayerMotor3D _player;
        PlayerInputReader _input;
        int _landmark = -1;
        float _replan;
        readonly HashSet<int> _visited = new HashSet<int>();

        void Awake() { _agent = GetComponent<NavMeshAgent>(); }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
            _agent.speed = runSpeed; _agent.acceleration = 30f; _agent.angularSpeed = 540f; _agent.autoBraking = true;
            PlayerAbilities.AnyPound += OnPound;
        }

        void OnDestroy() => PlayerAbilities.AnyPound -= OnPound;

        void OnPound(Vector3 at, float radius)
        {
            if ((Current == State.Flee || Current == State.Alert || Current == State.Waiting) && Vector3.Distance(at, transform.position) < radius)
            {
                Go(State.Stumble);
                UIRoot.Get().Toast("มันสะดุด! กระโดดขึ้นไปนั่งเลย!", 1.6f);
            }
        }

        void Go(State s)
        {
            Current = s; StateTime = 0f;
            if (_agent.isOnNavMesh) _agent.isStopped = s != State.Flee;
            var fx = FXLibrary.Instance;
            Vector3 head = transform.position + Vector3.up * 2f;
            switch (s)
            {
                case State.Alert:
                    Audio.AudioManager.Play(Audio.Sfx.ChairGasp, transform.position, 1f);
                    if (fx != null) WorldFX.Spawn(fx.exclaim, head, 1.1f, 0.8f, transform);
                    break;
                case State.Flee: Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 1f); break;
                case State.Exhausted:
                    Audio.AudioManager.Play(Audio.Sfx.ChairTired, transform.position, 1f);
                    if (fx != null) WorldFX.Spawn(fx.tired, head, 1.2f, exhaustedSeconds, transform);
                    VFX.Play(VFXKind.StarBurst, head, 1.2f, VFX.Gold);
                    UIRoot.Get().Toast("เก้าอี้หมดแรง! (EXHAUSTED) — ไปนั่งเลย!", 2f);
                    break;
                case State.Stumble:
                    Audio.AudioManager.Play(Audio.Sfx.ChairStumble, transform.position, 1f);
                    if (fx != null) WorldFX.Spawn(fx.dizzy, head, 1.1f, 2.5f, transform);
                    break;
            }
        }

        void Update()
        {
            if (_player == null) return;
            float dt = Time.deltaTime;
            StateTime += dt;
            Vector3 to = _player.transform.position - transform.position;
            float dist = new Vector2(to.x, to.z).magnitude;
            switch (Current)
            {
                case State.Waiting:
                    stamina = Mathf.Min(staminaMax, stamina + dt * 4f);
                    if (dist < noticeRadius) Go(State.Alert);
                    break;
                case State.Alert:
                    Face(to);
                    if (StateTime > 0.55f) { PickDestination(); Go(State.Flee); }
                    break;
                case State.Flee:
                    stamina -= dt * (dist < 8f ? 1.6f : 1f);
                    _agent.speed = dist < 8f ? panicSpeed : runSpeed;
                    _replan -= dt;
                    if (stamina <= 0f) { Go(State.Exhausted); break; }
                    if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 1.2f)
                    {
                        if (dist > noticeRadius * 1.4f) Go(State.Waiting);
                        else PickDestination();
                    }
                    else if (_replan <= 0f && dist < 10f) PickDestination();   // you cut it off: it changes its mind
                    break;
                case State.Exhausted:
                case State.Stumble:
                    if (TrySit()) break;
                    float limit = Current == State.Exhausted ? exhaustedSeconds : 2.5f;
                    if (StateTime > limit)
                    {
                        stamina = staminaMax * (Current == State.Exhausted ? 0.55f : 0.9f);
                        PickDestination(); Go(State.Flee);
                    }
                    break;
                case State.Sitting:
                    HoldPlayerOnSeat();
                    DreamHUD.Current?.SetSitProgress(StateTime, sitSeconds);
                    if (StateTime >= sitSeconds) StartCoroutine(DropKey());
                    break;
            }
            Animate(dt);
        }

        bool TrySit()
        {
            Vector3 d = _player.transform.position - transform.position;
            float flat = new Vector2(d.x, d.z).magnitude;
            bool landedOn = flat < 1.25f && d.y > 0.35f && d.y < 1.6f;
            bool pressE = flat < 2.0f && _input != null && _input.InteractPressed;
            if (!landedOn && !pressE) return false;
            Go(State.Sitting);
            _player.StopImmediately();
            _player.AutoStep = false;
            _player.Controller.enabled = false;
            var anim = _player.GetComponentInChildren<AppleProceduralAnimator>();
            if (anim != null) anim.sitting = true;
            Audio.AudioManager.Play(Audio.Sfx.ChairCreak, transform.position, 1f);
            return true;
        }

        void HoldPlayerOnSeat()
        {
            _player.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.72f, Quaternion.Euler(0, transform.eulerAngles.y, 0));
        }

        void ReleasePlayer(Vector3 launch)
        {
            var anim = _player.GetComponentInChildren<AppleProceduralAnimator>();
            if (anim != null) anim.sitting = false;
            _player.Controller.enabled = true;
            _player.AutoStep = true;
            _player.Launch(launch);
            DreamHUD.Current?.SetSitProgress(-1f, 1f);
        }

        IEnumerator DropKey()
        {
            Go(State.Gone);
            KeysDropped++;
            VFX.Play(VFXKind.Confetti, transform.position + Vector3.up, 1f);
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 1.2f, 1.6f, VFX.Gold);
            CameraShake.Add(0.4f);
            Audio.AudioManager.Play(Audio.Sfx.Victory, transform.position, 0.8f);
            ReleasePlayer(transform.forward * 4f + Vector3.up * 8f);
            KeyDropped?.Invoke(KeysDropped);
            yield return new WaitForSeconds(0.6f);
            VFX.Play(VFXKind.Poof, transform.position + Vector3.up * 0.7f, 2.4f);
            if (visual != null) visual.gameObject.SetActive(false);
            if (KeysDropped >= keysCarried) yield break;
            yield return new WaitForSeconds(2.5f);
            // reappear at the landmark farthest from Pip that we have not used yet
            int best = -1; float bestD = -1f;
            for (int i = 0; i < landmarks.Count; i++)
            {
                if (_visited.Contains(i)) continue;
                float d = Vector3.Distance(landmarks[i].position, _player.transform.position);
                if (d > bestD) { bestD = d; best = i; }
            }
            if (best < 0) { _visited.Clear(); best = 0; }
            _visited.Add(best);
            _agent.Warp(landmarks[best].position);
            if (visual != null) visual.gameObject.SetActive(true);
            VFX.Play(VFXKind.Poof, transform.position + Vector3.up * 0.7f, 2.4f);
            stamina = staminaMax * (1f + KeysDropped * 0.25f);       // each key makes it fitter (harder)
            staminaMax = stamina;
            runSpeed += 0.4f;
            UIRoot.Get().Toast("เก้าอี้วาร์ปไปที่อื่น! ดูเข็มทิศด้านบน", 2.4f);
            Go(State.Waiting);
        }

        void PickDestination()
        {
            _replan = 2.5f;
            Vector3 p = _player.transform.position;
            int best = -1; float bestScore = float.MinValue;
            for (int i = 0; i < landmarks.Count; i++)
            {
                if (i == _landmark) continue;
                Vector3 l = landmarks[i].position;
                float away = Vector3.Distance(l, p);
                float travel = Vector3.Distance(l, transform.position);
                // prefer far from Pip, not insanely far to travel, and not through Pip
                Vector3 dirL = (l - transform.position).normalized, dirP = (p - transform.position).normalized;
                float through = Vector3.Dot(dirL, dirP);
                float score = away - travel * 0.35f - through * 25f + Random.value * 8f;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best < 0) return;
            _landmark = best;
            if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.SetDestination(landmarks[best].position); }
        }

        void Face(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        }

        void Animate(float dt)
        {
            if (visual == null) return;
            float t = Time.time;
            switch (Current)
            {
                case State.Flee:
                    float sp = _agent.velocity.magnitude;
                    visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * sp * 1.9f)) * 0.25f, 0);
                    visual.localRotation = Quaternion.Euler(-10f, 0, Mathf.Sin(t * sp * 1.9f) * 8f);
                    if (Mathf.Repeat(t, 0.18f) < dt) VFX.Play(VFXKind.DustPuff, transform.position, 0.6f);
                    break;
                case State.Exhausted:
                    visual.localPosition = new Vector3(0, 0, 0);
                    visual.localRotation = Quaternion.Euler(8f + Mathf.Sin(t * 3f) * 3f, 0, Mathf.Sin(t * 1.3f) * 4f);
                    visual.localScale = new Vector3(1f + Mathf.Sin(t * 5f) * 0.03f, 1f - Mathf.Sin(t * 5f) * 0.04f, 1f);
                    break;
                case State.Stumble:
                    visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 12f) * 14f);
                    break;
                case State.Sitting:
                    visual.localRotation = Quaternion.Euler(Mathf.Sin(t * 20f) * 4f, 0, Mathf.Sin(t * 17f) * 6f);   // it struggles
                    break;
                default:
                    visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 2f)) * 0.06f, 0);
                    visual.localRotation = Quaternion.identity; visual.localScale = Vector3.one;
                    break;
            }
        }

        /// <summary>Tests/dev: force exhausted next to the player.</summary>
        public void DevExhaust() => Go(State.Exhausted);
    }
}
