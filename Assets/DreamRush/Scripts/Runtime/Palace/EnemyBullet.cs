using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// A slow glowing feather-orb (Cuckoo Sniper). Hurts Pip on touch (roll i-frames dodge it). SLASH IT and it flies back
    /// at 2.2× speed as Pip's projectile: it then hits enemies (big damage to its owner) — the Undertale/Zelda ping-pong.
    /// </summary>
    public class EnemyBullet : MonoBehaviour, IHittable
    {
        public Vector3 velocity; public float life = 4.5f, radius = 0.45f, reflectDamage = 70f;
        public EnemyBase owner;
        public bool Reflected { get; private set; }
        public static int ReflectedCount { get; private set; }
        IPlayerDamageReceiver _recv; Transform _player; float _t; Renderer _r; TrailRenderer _trail;

        public static EnemyBullet Fire(Vector3 from, Vector3 velocity, EnemyBase owner, Transform player, IPlayerDamageReceiver recv)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "FeatherOrb";
            Object.Destroy(go.GetComponent<Collider>());
            var sc = go.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.9f;   // generous: easy to slash back
            go.transform.position = from; go.transform.localScale = Vector3.one * 0.55f;
            var b = go.AddComponent<EnemyBullet>(); b.velocity = velocity; b.owner = owner; b._player = player; b._recv = recv;
            b._r = go.GetComponent<Renderer>();
            b._r.sharedMaterial = Look.DreamLook.Toon(new Color(1f, 0.55f, 0.9f), null, 0.6f, name: "M_FeatherOrb");
            b._r.sharedMaterial.SetColor("_Emission", new Color(1.6f, 0.4f, 1.2f));
            b._r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b._trail = go.AddComponent<TrailRenderer>(); b._trail.time = 0.25f; b._trail.widthMultiplier = 0.4f;
            var lib = VFXLibrary.Instance; if (lib != null) b._trail.sharedMaterial = lib.glowAdd;
            b._trail.startColor = new Color(1f, 0.5f, 0.9f, 0.9f); b._trail.endColor = new Color(1f, 0.5f, 0.9f, 0f);
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime; _t += dt;
            transform.position += velocity * dt;
            transform.Rotate(0, 540f * dt, 0);
            if (!Reflected)
            {
                if (_player != null && Vector3.Distance(_player.position + Vector3.up * 0.6f, transform.position) < radius + 0.45f)
                {
                    bool landed = _recv != null && _recv.TryHurt("a cuckoo feather", transform.position, 5f);
                    if (landed) { Pop(); return; }
                }
            }
            else
            {
                if (owner != null && !owner.Dead)   // reflected orbs home in on their sender (it keeps flying around)
                {
                    Vector3 to = owner.transform.position + Vector3.up * owner.height * 0.5f - transform.position;
                    velocity = Vector3.RotateTowards(velocity, to.normalized * velocity.magnitude, 6f * dt, 0f);
                }
                foreach (var e in EnemyBase.All)
                {
                    if (e == null || e.Dead) continue;
                    Vector3 c = e.transform.position + Vector3.up * e.height * 0.5f;
                    if (Vector3.Distance(c, transform.position) < e.radius + 0.6f + (e.height * 0.3f))
                    {
                        e.TakeHit(new HitInfo { damage = reflectDamage, posture = 30f, point = transform.position, direction = velocity.normalized, heavy = true, source = "Reflect" });
                        VFX.Play(VFXKind.StarBurst, transform.position, 1.2f, VFX.Gold); CameraShake.Add(0.2f);
                        Pop(); return;
                    }
                }
            }
            if (_t > life || Physics.CheckSphere(transform.position, 0.2f, ~0, QueryTriggerInteraction.Ignore)) Pop();
        }

        public bool TakeHit(HitInfo hit)
        {
            if (Reflected) return false;
            Reflected = true; ReflectedCount++; Audio.AudioManager.Play(Audio.Sfx.Reflect, transform.position, 0.9f);
            Vector3 dir = owner != null && !owner.Dead ? (owner.transform.position + Vector3.up * owner.height * 0.5f - transform.position).normalized : hit.direction.normalized;
            velocity = dir * Mathf.Max(18f, velocity.magnitude * 2.2f);
            _r.sharedMaterial = Look.DreamLook.Toon(new Color(1f, 0.9f, 0.4f), null, 0.6f, name: "M_FeatherOrbReflected");
            _r.sharedMaterial.SetColor("_Emission", new Color(2f, 1.5f, 0.3f));
            if (_trail != null) { _trail.startColor = new Color(1f, 0.9f, 0.4f, 1f); _trail.endColor = new Color(1f, 0.9f, 0.4f, 0f); }
            _t = 0f; life = 3f;
            Audio.AudioManager.Play(Audio.Sfx.Whoosh, transform.position, 1f);
            UI.UIRoot.Get().Toast("REFLECT!", 0.6f);
            return true;
        }

        void Pop()
        {
            VFX.Play(VFXKind.Poof, transform.position, 0.6f, Reflected ? VFX.Gold : VFX.Lavender);
            Destroy(gameObject);
        }
    }
}
