using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// BELL BRUTE — a 2.4 m twin-bell alarm clock with a brass shield face. Teaches "read the guard":
    ///   • GUARD: light hits from the front bounce off (clank). Get behind it, use the charged LEAP SLAM, a launcher or the
    ///     DREAM BURST — or punish its recovery.
    ///   • RING POUND: bells shake (0.9 s, red) → it slams → a SHOCK RING rolls out to 8 m: jump over the ring.
    ///   • BELL CHARGE (6–14 m away): crouch 0.6 s → bulldozes 9 m in a straight line: roll sideways.
    ///   • After either attack it pants for 1.4 s with its guard down (the opening).
    /// </summary>
    public class BellBrute : EnemyBase
    {
        public enum State { Idle, Approach, PoundWindup, Pound, ChargeWindup, Charge, Recover }
        public State Current { get; private set; } = State.Idle;
        public float speed = 2.4f, chargeSpeed = 13f;
        public int GuardBlocks { get; private set; }
        Vector3 _chargeDir; float _chargeLeft; float _cool;

        protected override void Awake()
        {
            displayName = "BELL BRUTE"; maxHp = 190f; height = 2.6f; radius = 1.1f; notice = 18f;
            base.Awake();
        }

        protected override float GravityScale => 1.6f;   // heavy: juggles are short

        protected override bool AcceptHit(ref HitInfo hit)
        {
            if (Current == State.Recover || Current == State.Charge || Stunned || Airborne) return true;
            if (hit.heavy || hit.launch > 0f || hit.critical) return true;
            Vector3 from = -hit.direction; from.y = 0;
            bool front = from.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, from) < 70f;
            if (!front) return true;
            GuardBlocks++;
            Tint(new Color(1.2f, 1f, 0.4f)); Invoke(nameof(ClearTintLater), 0.08f);
            if (GuardBlocks % 3 == 1) UI.UIRoot.Get().Toast("GUARD! — ไปด้านหลัง หรือชาร์จทุบหนัก (กดค้าง)", 1.6f);
            return false;
        }

        void ClearTintLater() => ClearTint();

        protected override void OnHurt(HitInfo hit)
        {
            if (Current == State.Idle) Current = State.Approach;
            if (hit.heavy && Current != State.Charge) { Stun(1.1f); Current = State.Approach; model.localScale = Vector3.one; }
        }

        protected override void OnLanded() { Current = State.Approach; t = 0f; }

        protected override void Think(float dt, Vector3 to, float d)
        {
            _cool -= dt;
            switch (Current)
            {
                case State.Idle:
                    model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(t * 2f)) * 0.04f;
                    if (d < notice) { Current = State.Approach; VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 3f, 0.9f, VFX.Danger); Audio.AudioManager.Play(Audio.Sfx.EnemyAlert, transform.position, 0.7f); }
                    break;
                case State.Approach:
                    Face(to, 140f, dt);
                    Steer(d > 3.2f ? to.normalized * speed : Vector3.zero, 7f, dt);   // heavy: slow to start, slow to stop
                    Gait(dt, 0.1f, 1.4f, 6f, 6f);
                    if (_cool <= 0f && d < 4.2f) { Current = State.PoundWindup; t = 0f; Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 0.8f); }
                    else if (_cool <= 0f && d > 6f && d < 14f && Vector3.Angle(transform.forward, to) < 15f) { Current = State.ChargeWindup; t = 0f; }
                    break;
                case State.PoundWindup:
                    Brake(10f, dt);
                    model.localRotation = Quaternion.Euler(Mathf.Sin(t * 50f) * 3f, 0, Mathf.Sin(t * 46f) * 4f);
                    model.localScale = new Vector3(1f + t * 0.12f, 1f + t * 0.18f, 1f + t * 0.12f);
                    Tint(Color.Lerp(Color.black, new Color(1.6f, 0.15f, 0.2f), Mathf.PingPong(t * 6f, 1f)));
                    if (t > 0.9f) { Current = State.Pound; t = 0f; }
                    break;
                case State.Pound:
                    model.localScale = new Vector3(1.2f, 0.75f, 1.2f);
                    if (t < 0.05f)
                    {
                        ClearTint();
                        ShockRing.Spawn(transform.position + Vector3.up * 0.1f, 8f, 0.75f, "a Bell Brute's shock ring", recv, player); Audio.AudioManager.Play(Audio.Sfx.Shock, transform.position, 0.8f);
                        VFX.Play(VFXKind.ImpactRing, transform.position, 2f, VFX.Danger); VFX.Play(VFXKind.DustPuff, transform.position, 2.2f);
                        CameraShake.Add(0.4f); Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f);
                        HitPlayer("a Bell Brute", 2.2f, 0.9f, 9f);
                        t = 0.05f;
                    }
                    if (t > 0.35f) { model.localScale = Vector3.one; Current = State.Recover; t = 0f; _cool = 2.2f; }
                    break;
                case State.ChargeWindup:
                    Face(to, 260f, dt);
                    model.localRotation = Quaternion.Euler(18f, 0, Mathf.Sin(t * 40f) * 3f);
                    Tint(Color.Lerp(Color.black, new Color(1.4f, 0.5f, 0.1f), Mathf.PingPong(t * 7f, 1f)));
                    if (t > 0.6f) { _chargeDir = transform.forward; _chargeLeft = 9f; Current = State.Charge; t = 0f; ClearTint(); Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 1f); }
                    break;
                case State.Charge:
                {
                    float step = chargeSpeed * dt;
                    if (Mathf.Repeat(t, 0.08f) < dt) VFX.Play(VFXKind.DustPuff, transform.position, 1f);
                    bool wall = Blocked(transform.position + Vector3.up * 1f, _chargeDir, 1.6f);
                    if (wall || !MoveGround(_chargeDir * step, ignoreLeash: true)) { Stun(1.6f); CameraShake.Add(0.3f); VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 2f, 1f, VFX.Gold); Current = State.Recover; t = 0f; _cool = 2f; break; }
                    _chargeLeft -= step;
                    HitPlayer("a Bell Brute's charge", 1.7f, 1.0f, 11f);
                    if (_chargeLeft <= 0f) { Current = State.Recover; t = 0f; _cool = 2f; }
                    break;
                }
                case State.Recover:
                    model.localRotation = Quaternion.Euler(10f + Mathf.Sin(t * 8f) * 4f, 0, 0);
                    if (Mathf.Repeat(t, 0.45f) < dt) VFX.Play(VFXKind.Poof, transform.position + Vector3.up * 2.8f, 0.4f, VFX.Cream);   // panting steam
                    if (t > 1.4f) { model.localRotation = Quaternion.identity; Current = State.Approach; }
                    break;
            }
        }
    }
}
