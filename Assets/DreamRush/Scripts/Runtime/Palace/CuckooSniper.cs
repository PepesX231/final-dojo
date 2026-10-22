using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// CUCKOO SNIPER — a winged cuckoo-clock bird that hovers 4 m up and keeps ~11 m away, circling.
    /// Every ~2.4 s: beak glows (0.55 s) → a fan of 3 slow feather-orbs. Counter-play: roll through them, or SLASH one back
    /// (reflected orbs one-shot it), or jump + AIR SPIN to reach it. Hit it and it drops to the ground dazed for 1.5 s.
    /// </summary>
    public class CuckooSniper : EnemyBase
    {
        public enum State { Perch, Hover, Aim, Dazed }
        public State Current { get; private set; } = State.Perch;
        public float hoverHeight = 4f, keepAway = 11f, fireEvery = 2.4f, orbSpeed = 8.5f;
        float _fire, _orbit, _groundY;

        protected override void Awake()
        {
            displayName = "CUCKOO SNIPER"; maxHp = 55f; height = 1.4f; radius = 0.7f; notice = 24f;
            base.Awake();
        }

        protected override void Start() { base.Start(); _groundY = transform.position.y; _orbit = Random.value * 6f; }

        protected override float GravityScale => 0.7f;

        protected override void OnHurt(HitInfo hit)
        {
            if (Current != State.Dazed) { Current = State.Dazed; t = 0f; Tint(Color.black); ClearTint(); }
        }

        protected override void OnLanded() { Current = State.Hover; t = 0f; _groundY = transform.position.y; _fire = 1.5f; }

        protected override void Think(float dt, Vector3 to, float d)
        {
            Vector3 pos = transform.position;
            switch (Current)
            {
                case State.Perch:
                    model.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 1.3f) * 20f, 0);
                    if (d < notice) { Current = State.Hover; _fire = 1.2f; Audio.AudioManager.Play(Audio.Sfx.SleepChime, pos, 0.6f); VFX.Play(VFXKind.StarBurst, pos + Vector3.up * 2f, 0.6f, VFX.Danger); }
                    break;
                case State.Hover:
                case State.Aim:
                {
                    // circle the player at keepAway, bobbing at hover height (clamped to the leash)
                    _orbit += dt * 0.35f;
                    Vector3 around = player.position + new Vector3(Mathf.Sin(_orbit), 0, Mathf.Cos(_orbit)) * keepAway;
                    if (Vector3.Distance(new Vector3(around.x, home.y, around.z), home) > leash) around = home + (around - home).normalized * leash;
                    float y = Mathf.Max(_groundY, player.position.y) + hoverHeight + Mathf.Sin(t * 2.2f) * 0.35f;
                    Vector3 target = new Vector3(around.x, y, around.z);
                    transform.position = Vector3.MoveTowards(pos, target, (Current == State.Aim ? 2f : 6.5f) * dt);
                    Face(to, 300f, dt);
                    model.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 18f) * 6f);   // wing flutter
                    _fire -= dt;
                    if (Current == State.Hover && _fire <= 0f && d < notice + 6f) { Current = State.Aim; t = 0f; Audio.AudioManager.Play(Audio.Sfx.ChairYelp, pos, 0.5f); }
                    if (Current == State.Aim)
                    {
                        Tint(Color.Lerp(Color.black, new Color(1.6f, 0.4f, 1.3f), Mathf.PingPong(t * 6f, 1f)));
                        if (t > 0.55f)
                        {
                            ClearTint();
                            Vector3 from = transform.position + transform.forward * 0.8f + Vector3.up * 0.7f;
                            Vector3 aim = (player.position + Vector3.up * 0.7f - from).normalized;
                            for (int k = -1; k <= 1; k++) EnemyBullet.Fire(from, Quaternion.AngleAxis(k * 16f, Vector3.up) * aim * orbSpeed, this, player, recv);
                            Audio.AudioManager.Play(Audio.Sfx.Whoosh, from, 0.9f);
                            Current = State.Hover; _fire = fireEvery;
                        }
                    }
                    break;
                }
                case State.Dazed:
                    // flutters down to the ground, dizzy: the moment to finish it
                    if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                        transform.position = Vector3.MoveTowards(pos, hit.point, 9f * dt);
                    model.localRotation = Quaternion.Euler(0, t * 500f, 20f);
                    if (Mathf.Repeat(t, 0.3f) < dt) VFX.Play(VFXKind.StarBurst, pos + Vector3.up * 1.6f, 0.3f, VFX.Gold);
                    if (t > 1.6f) { Current = State.Hover; model.localRotation = Quaternion.identity; _fire = 1.4f; }
                    break;
            }
        }
    }
}
