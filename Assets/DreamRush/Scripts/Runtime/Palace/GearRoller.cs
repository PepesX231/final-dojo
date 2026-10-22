using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// GEAR ROLLER — a spiked brass gear on its edge. Lines up with Pip, revs (0.7 s, sparks + red) and BLASTS 14 m in a
    /// straight line at 16 m/s: side-step / roll. If it hits a wall, a lamp post or a Bell Brute it CRASHES: dazed for
    /// 1.8 s and takes ×1.6 damage — lure it into walls. While rolling only a jump-over is safe; its side is armoured.
    /// </summary>
    public class GearRoller : EnemyBase
    {
        public enum State { Idle, Stalk, Rev, Roll, Skid }
        public State Current { get; private set; } = State.Idle;
        public float stalkSpeed = 3.2f, rollSpeed = 16f, rollDistance = 14f;
        public int Crashes { get; private set; }
        Vector3 _dir; float _left, _spin;

        protected override void Awake()
        {
            displayName = "GEAR ROLLER"; maxHp = 85f; height = 1.7f; radius = 0.9f; notice = 20f;
            base.Awake();
        }

        protected override bool AcceptHit(ref HitInfo hit)
        {
            if (Stunned) { hit.damage *= 1.6f; return true; }
            if (Current == State.Roll) return false;          // a spinning gear just throws sparks
            return true;
        }

        protected override void OnHurt(HitInfo hit) { if (Current == State.Idle) Current = State.Stalk; }

        protected override void OnLanded() { Current = State.Stalk; t = 0f; }

        protected override void Think(float dt, Vector3 to, float d)
        {
            _spin += (Current == State.Roll ? 900f : Current == State.Rev ? 1400f : Current == State.Stalk ? 160f : 0f) * dt;
            model.localRotation = Quaternion.Euler(_spin, 0, 0);
            switch (Current)
            {
                case State.Idle:
                    if (d < notice) { Current = State.Stalk; t = 0f; VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 2.2f, 0.6f, VFX.Danger); }
                    break;
                case State.Stalk:
                    Face(to, 200f, dt);
                    // M18: keeps its distance and sidles around Pip (lining up a lane) instead of sliding back and forth
                    Vector3 want = d > 7f ? to.normalized * stalkSpeed : d < 4f ? -to.normalized * stalkSpeed * 0.8f : OrbitDir(to, 5.5f) * stalkSpeed * 0.5f;
                    Steer(want, 10f, dt);
                    if (t > 1.2f && d < 16f && Vector3.Angle(transform.forward, to) < 10f) { Current = State.Rev; t = 0f; Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 0.6f); }
                    break;
                case State.Rev:
                    Face(to, 120f, dt);
                    Tint(Color.Lerp(Color.black, new Color(1.6f, 0.3f, 0.1f), Mathf.PingPong(t * 8f, 1f)));
                    if (Mathf.Repeat(t, 0.06f) < dt) VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 0.2f - transform.forward * 0.6f, 0.35f, VFX.Gold);
                    if (t > 0.7f) { ClearTint(); _dir = transform.forward; _left = rollDistance; Current = State.Roll; t = 0f; Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 0.9f); }
                    break;
                case State.Roll:
                {
                    float step = rollSpeed * dt;
                    if (Mathf.Repeat(t, 0.05f) < dt) VFX.Play(VFXKind.DustPuff, transform.position, 0.7f);
                    bool blocked = Blocked(transform.position + Vector3.up * 0.9f, _dir, radius + 0.5f);
                    foreach (var e in All) if (e != this && e != null && e is BellBrute && Vector3.Distance(e.transform.position, transform.position + _dir) < 1.8f) { blocked = true; e.TakeHit(new HitInfo { damage = 40, heavy = true, direction = _dir, point = e.transform.position, source = "Roller" }); }
                    if (blocked || !MoveGround(_dir * step, ignoreLeash: true))
                    {
                        Crashes++; Stun(1.8f); CameraShake.Add(0.3f);
                        VFX.Play(VFXKind.ImpactRing, transform.position + Vector3.up, 1.2f, VFX.Gold); Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f);
                        UI.UIRoot.Get().Toast("CRASH! — ตีได้แรงขึ้น", 1f);
                        Current = State.Stalk; t = 0f; break;
                    }
                    _left -= step;
                    HitPlayer("a Gear Roller", 1.4f, 1.1f, 10f);
                    if (_left <= 0f) { Current = State.Skid; t = 0f; }
                    break;
                }
                case State.Skid:
                    if (t > 0.6f) { Current = State.Stalk; t = 0f; }
                    break;
            }
        }
    }
}
