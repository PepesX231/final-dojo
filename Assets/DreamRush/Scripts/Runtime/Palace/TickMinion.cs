using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// TICK-TOCK SHADOW — Count Midnight's masked alarm-clock minions (practice for the boss): notice → waddle-chase →
    /// crouch + red flash (0.55 s) → hop slam (2 m ring, jump/roll it) → dizzy 0.9 s (free hits). 3 leaf hits.
    /// Can be launched and juggled (EnemyBase). Walks on whatever is below it, never leaves its leash.
    /// </summary>
    public class TickMinion : EnemyBase
    {
        public enum State { Idle, Chase, Windup, Slam, Dizzy, Dead, Falling }
        public float speed = 4.6f;
        public State Current { get; private set; } = State.Idle;
        public static int Alive { get; private set; }
        public new static int Defeated { get; private set; }
        public static System.Action<TickMinion> Died;
        Vector3 _tickDrop;

        public static void ResetCounters() { Alive = 0; Defeated = 0; EnemyBase.ResetDefeated(); }

        protected override void Awake()
        {
            displayName = "TICK-TOCK"; if (maxHp <= 0f) maxHp = 60f;
            base.Awake();
        }

        protected override void OnEnable() { base.OnEnable(); Alive++; }
        protected override void OnDisable() { base.OnDisable(); Alive--; }

        /// <summary>Ambush entrance: falls from the sky onto <paramref name="target"/>.</summary>
        public override void DropIn(Vector3 target) { _tickDrop = target; Current = State.Falling; t = 0f; }

        protected override bool AcceptHit(ref HitInfo hit) => Current != State.Falling;

        protected override void OnHurt(HitInfo hit)
        {
            if (Current == State.Idle) Current = State.Chase;
            if (Current == State.Windup) { Current = State.Chase; model.localScale = Vector3.one; ClearTint(); ReleaseToken(); }   // hits interrupt the slam
        }

        protected override void Think(float dt, Vector3 to, float d)
        {
            switch (Current)
            {
                case State.Falling:
                    transform.position = Vector3.MoveTowards(transform.position, _tickDrop, 22f * dt);
                    if ((transform.position - _tickDrop).sqrMagnitude < 0.01f) { VFX.Play(VFXKind.DustPuff, transform.position, 1.2f); CameraShake.Add(0.15f); Current = State.Chase; home = transform.position; }
                    break;
                case State.Idle:
                    model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 3f)) * 0.05f, 0);
                    if (d < notice) { Current = State.Chase; VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 2.2f, 0.6f, VFX.Danger); Audio.AudioManager.Play(Audio.Sfx.EnemyAlert, transform.position, 0.55f); }
                    break;
                case State.Chase:
                {
                    // M18: accelerate/turn like a body, fan out from the pack, and take turns — only MaxAttackers tick-tocks
                    // commit to a slam at once; the rest circle at ~4 m (readable, fair crowds).
                    bool token = d < 6f && RequestToken();
                    Vector3 want = d > 6f || token ? (d > 2.2f ? to.normalized * speed : Vector3.zero) : OrbitDir(to, 4.2f) * speed * 0.55f;
                    Steer(want, 18f, dt);
                    Face(token || d > 6f ? to : Vector3.Lerp(to, Velocity, 0.3f), 420f, dt);
                    Gait(dt, 0.16f, 0.55f, 10f, 12f);
                    if (token && d < 2.6f) { Current = State.Windup; t = 0f; Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 0.5f); }
                    break;
                }
                case State.Windup:
                    Brake(30f, dt); Face(to, 240f, dt);
                    // anticipation: squash down and lean back, trembling, before the hop
                    float a = Mathf.Clamp01(t / 0.3f);
                    model.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.76f, 1.18f), UI.UIStyle.OutBack(a));
                    model.localRotation = Quaternion.Euler(-14f * a, 0, Mathf.Sin(t * 60f) * 3f * a);
                    model.localPosition = Vector3.zero;
                    Tint(Color.Lerp(Color.black, new Color(1.5f, 0.1f, 0.2f), Mathf.PingPong(t * 8f, 1f)));
                    if (t > 0.55f) { Current = State.Slam; t = 0f; }
                    break;
                case State.Slam:
                    // stretch on take-off, squash on landing
                    float u = Mathf.Clamp01(t / 0.35f);
                    model.localScale = u < 0.5f ? new Vector3(0.88f, 1.22f, 0.88f) : Vector3.Lerp(new Vector3(0.88f, 1.22f, 0.88f), new Vector3(1.25f, 0.75f, 1.25f), (u - 0.5f) * 2f);
                    model.localRotation = Quaternion.Euler(12f * Mathf.Sin(u * Mathf.PI), 0, 0);
                    model.localPosition = new Vector3(0, Mathf.Sin(u * Mathf.PI) * 1.2f, 0);
                    if (t > 0.35f)
                    {
                        ClearTint();
                        VFX.Play(VFXKind.ImpactRing, transform.position, 0.7f, VFX.Danger); CameraShake.Add(0.12f);
                        Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 0.6f);
                        HitPlayer("a Tick-Tock shadow", 2.2f, 0.8f, 6f);
                        Current = State.Dizzy; t = 0f; ReleaseToken();
                    }
                    break;
                case State.Dizzy:
                    model.localRotation = Quaternion.Euler(0, t * 360f, Mathf.Sin(t * 9f) * 12f);
                    if (Mathf.Repeat(t, 0.3f) < dt) VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 2f, 0.3f, VFX.Gold);
                    model.localScale = Vector3.Lerp(model.localScale, Vector3.one, 1f - Mathf.Exp(-10f * dt));
                    if (t > 0.9f) { model.localRotation = Quaternion.identity; model.localScale = Vector3.one; Current = State.Chase; }
                    break;
            }
        }

        protected override void Die()
        {
            if (Dead) return;
            Current = State.Dead; Defeated++;
            Died?.Invoke(this);
            base.Die();
        }
    }
}
