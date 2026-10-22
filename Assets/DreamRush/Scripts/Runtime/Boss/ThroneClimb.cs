using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>
    /// World 1 boss P3 — THRONE CLIMB (docs/LEVEL1_DESIGN.md, Ch5). When THE CHAIR reaches phase 3 it pulls back a
    /// leg (0.5 s telegraph), leaps onto the top of the Throne Tower and kicks the old throne away. The tower's books
    /// slide out of the core as a spiral staircase that keeps rotating (each book is a carrying MovingPlatform).
    /// From the top the chair lobs pillows at you: a red circle marks the landing spot 1.1 s ahead, a hit knocks you
    /// off and costs a moon. Reach the top → the chair is spent (Exhausted) → sit 2 s → win. Fall off → it recovers.
    /// Runs before MovingPlatform (-60) so the step rotation is carried in the same frame.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class ThroneClimb : MonoBehaviour
    {
        public BossChairController chair;
        public ChairSkills skills;
        public BossEncounter encounter;
        [Tooltip("Rotating root of the book steps (pivot = tower axis).")]
        public Transform stepsRoot;
        public List<Transform> steps = new List<Transform>();
        public GameObject throneDecor;
        [Tooltip("World height of the tower top (relative to this transform, the tower base).")]
        public float topHeight = 9f;
        public float topRadius = 3.2f;
        public float idleSpin = 6f, climbSpin = 14f;
        public float stepOutRadius = 4.1f, stepInRadius = 1.4f;
        public float throwInterval = 2.6f, throwWarn = 1.1f, firstThrowDelay = 2.2f, pillowHitRadius = 1.35f;
        public bool AutoStart = true;
        public bool AutoThrow = true;

        public bool Started { get; private set; }
        public bool Climbing { get; private set; }      // chair is up, stairs are out
        public bool PlayerAtTop { get; private set; }
        public int Throws { get; private set; }
        public float StepsOut { get; private set; }     // 0 tucked .. 1 out

        PlayerMotor3D _player;
        float _spin, _throwT;
        Vector3 Top => transform.position + Vector3.up * topHeight;

        void Start()
        {
            if (chair == null) chair = FindFirstObjectByType<BossChairController>();
            if (skills == null && chair != null) skills = chair.GetComponent<ChairSkills>();
            if (encounter == null) encounter = FindFirstObjectByType<BossEncounter>();
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _spin = idleSpin;
            SetSteps(0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (stepsRoot != null) stepsRoot.Rotate(0f, _spin * dt, 0f, Space.World);
            if (chair == null || _player == null) return;
            bool fight = encounter == null || encounter.CurrentPhase == BossEncounter.Phase.Fight;

            if (!Started && AutoStart && fight && skills != null && skills.Phase >= 3 && !skills.Busy && CanLeapNow())
                StartCoroutine(Run());

            if (!Climbing || !fight) return;
            // reached the top?
            Vector3 d = _player.transform.position - Top; float flat = new Vector2(d.x, d.z).magnitude;
            bool atTop = _player.IsGrounded && d.y > -0.6f && d.y < 1.5f && flat < topRadius;
            if (atTop && !PlayerAtTop && chair.State == ChairState.Perched)
            {
                chair.PerchExhausted("ThroneClimb");
                UI.UIRoot.Get().Toast("มันหมดแรงแล้ว! นั่งทับเลย! [E]", 2.5f);
                VFX.Play(VFXKind.StarBurst, Top + Vector3.up * 1.2f, 1.2f, VFX.Gold);
                Audio.AudioManager.Play(Audio.Sfx.ChairTired, chair.transform.position, 1f);
            }
            if (PlayerAtTop && _player.transform.position.y < Top.y - 1.6f) chair.PerchRecover();   // fell off: it gets its breath back
            PlayerAtTop = atTop;

            if (AutoThrow && chair.State == ChairState.Perched)
            {
                _throwT -= dt;
                if (_throwT <= 0f) { _throwT = throwInterval; StartCoroutine(ThrowPillow()); }
            }
        }

        bool CanLeapNow()
        {
            var s = chair.State;
            return s == ChairState.Flee || s == ChairState.Panic || s == ChairState.Hesitate || s == ChairState.Alert || s == ChairState.Idle;
        }

        /// <summary>The leap + stairs. Public so tests/dev commands can trigger P3 directly.</summary>
        public IEnumerator Run()
        {
            if (Started) yield break;
            Started = true;
            chair.SkillBusyUntil = Time.time + 3f;
            UI.UIRoot.Get().Toast("THRONE CLIMB! — ปีนหอหนังสือขึ้นไปจับมัน!", 3f);
            CameraShake.Add(0.3f);
            Audio.AudioManager.Play(Audio.Sfx.ChairGasp, chair.transform.position, 1f);
            var vis = chair.transform.Find("Visual");
            // telegraph: crouch + pull back a leg (0.5 s)
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float k = t / 0.5f;
                if (vis != null) { vis.localScale = new Vector3(1f + 0.18f * k, 1f - 0.22f * k, 1f + 0.18f * k); vis.localRotation = Quaternion.Euler(-14f * k, 0, 0); }
                yield return null;
            }
            if (vis != null) { vis.localScale = Vector3.one; vis.localRotation = Quaternion.identity; }
            chair.BeginPerch();
            Vector3 from = chair.transform.position, to = Top;
            VFX.Play(VFXKind.DustPuff, from, 1.8f);
            Audio.AudioManager.Play(Audio.Sfx.Jump, from, 1f);
            const float flight = 1.1f;
            for (float t = 0; t < flight; t += Time.deltaTime)
            {
                float k = t / flight;
                chair.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 5f;
                Vector3 look = _player.transform.position - chair.transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) chair.transform.rotation = Quaternion.Slerp(chair.transform.rotation, Quaternion.LookRotation(look), 6f * Time.deltaTime);
                yield return null;
            }
            chair.transform.position = to;
            // kick the old throne away
            if (throneDecor != null)
            {
                VFX.Play(VFXKind.Poof, throneDecor.transform.position + Vector3.up * 0.8f, 2.4f, VFX.Gold);
                throneDecor.SetActive(false);
            }
            VFX.Play(VFXKind.ImpactRing, to, 1.5f, VFX.Gold);
            VFX.Play(VFXKind.StarBurst, to + Vector3.up, 1.2f, VFX.Gold);
            CameraShake.Add(0.6f);
            Audio.AudioManager.Play(Audio.Sfx.Land, to, 1f);
            // the books slide out into a spiral staircase, and the stack spins faster
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                SetSteps(t / 1.2f);
                _spin = Mathf.Lerp(idleSpin, climbSpin, t / 1.2f);
                yield return null;
            }
            SetSteps(1f); _spin = climbSpin;
            Climbing = true;
            _throwT = firstThrowDelay;
        }

        void SetSteps(float k)
        {
            StepsOut = k;
            float e = 1f - (1f - k) * (1f - k) * (1f - k);   // out-cubic, with a little overshoot below
            float r = Mathf.LerpUnclamped(stepInRadius, stepOutRadius, e + Mathf.Sin(k * Mathf.PI) * 0.06f);
            foreach (var s in steps)
            {
                if (s == null) continue;
                Vector3 lp = s.localPosition; Vector3 dir = new Vector3(lp.x, 0, lp.z);
                if (dir.sqrMagnitude < 1e-4f) continue;
                dir.Normalize();
                s.localPosition = dir * r + Vector3.up * lp.y;
                foreach (var c in s.GetComponentsInChildren<Collider>()) c.enabled = k >= 0.999f;
            }
        }

        /// <summary>One pillow lobbed at where you will be. Red circle 1.1 s ahead; hit = knock-back + a moon.</summary>
        public IEnumerator ThrowPillow()
        {
            Throws++;
            Vector3 lead = _player.Velocity; lead.y = 0f;
            Vector3 aim = _player.transform.position + lead * 0.6f;
            Vector3 land = aim;
            RaycastHit hit = default; float best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(aim + Vector3.up * 1.2f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
                if (h.distance < best && !h.collider.transform.IsChildOf(_player.transform)) { best = h.distance; hit = h; }   // skip Pip's own capsule
            if (hit.collider != null) land = hit.point;
            var mark = ChairSkills.Disc("ThronePillowMark", land + Vector3.up * 0.04f, pillowHitRadius * 2f, ChairSkills.TelegraphMat, new Color(1f, 0.25f, 0.3f, 0.9f));
            var follow = hit.collider != null ? hit.collider.transform : null;   // marks ride the rotating steps
            Vector3 localLand = follow != null ? follow.InverseTransformPoint(land) : land;
            var vis = chair.transform.Find("Visual");
            Audio.AudioManager.Play(Audio.Sfx.ChairYelp, chair.transform.position, 0.6f);
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ball.GetComponent<Collider>());
            ball.name = "ThronePillow";
            ball.transform.localScale = new Vector3(1.0f, 0.55f, 1.0f);
            ball.GetComponent<Renderer>().sharedMaterial = ChairSkills.CushionMat;
            ball.SetActive(false);
            Vector3 from = chair.transform.position + Vector3.up * 1.4f;
            for (float t = 0; t < throwWarn; t += Time.deltaTime)
            {
                float k = t / throwWarn;
                Vector3 l = follow != null ? follow.TransformPoint(localLand) : land;
                mark.transform.position = l + Vector3.up * 0.07f;
                mark.transform.localScale = Vector3.one * pillowHitRadius * 2f * (0.75f + 0.25f * Mathf.PingPong(t * 6f, 1f));
                if (k < 0.35f) { if (vis != null) vis.localRotation = Quaternion.Euler(-18f * (k / 0.35f), 0, 0); }   // wind-up
                else
                {
                    if (vis != null) vis.localRotation = Quaternion.identity;
                    ball.SetActive(true);
                    float f = (k - 0.35f) / 0.65f;
                    ball.transform.position = Vector3.Lerp(from, l, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * 3.5f;
                    ball.transform.Rotate(0, 0, 420f * Time.deltaTime);
                }
                yield return null;
            }
            Vector3 at = follow != null ? follow.TransformPoint(localLand) : land;
            VFX.Play(VFXKind.DustPuff, at, 1.4f);
            VFX.Play(VFXKind.LandingRing, at, 1.2f, VFX.Danger);
            Audio.AudioManager.Play(Audio.Sfx.Land, at, 0.7f);
            Vector3 d = _player.transform.position - at;
            Vector3 flat = new Vector3(d.x, 0, d.z);
            if (flat.magnitude < pillowHitRadius && d.y > -0.6f && d.y < 1.4f && skills != null)
                skills.Hurt(flat.sqrMagnitude > 0.01f ? flat.normalized : (at - transform.position).normalized, "throne pillow");
            Destroy(mark);
            Destroy(ball, 0.1f);
        }
    }
}
