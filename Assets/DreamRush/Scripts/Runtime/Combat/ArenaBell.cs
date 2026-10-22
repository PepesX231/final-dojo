using System.Collections.Generic;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>
    /// M19 boss mechanic: the STOP-THE-CLOCK BELLS around Count Midnight's dial (the same bells Pip learned on the east
    /// roofs). Punch a bell → it tolls and freezes time in a bubble for a few seconds: bullets inside vanish and, if the
    /// Count is inside, he freezes mid-swing with his armour (the winding key) open. Then the bell needs to recharge —
    /// the gold ring around its base refills — so the fight becomes "bait him to a bell, then ring it".
    /// </summary>
    public class ArenaBell : MonoBehaviour, IHittable
    {
        public static readonly List<ArenaBell> All = new List<ArenaBell>();
        public float radius = 7f, freezeSeconds = 3.2f, recharge = 11f;
        public float ReadyAt { get; private set; }
        public bool Ready => Time.time >= ReadyAt;
        public int Rings { get; private set; }
        Transform _bell, _ringFill; Material _glow; float _swing;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static ArenaBell Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("ArenaBell"); go.transform.SetParent(parent, true); go.transform.position = pos;
            var b = go.AddComponent<ArenaBell>(); b.Build(); return b;
        }

        void Build()
        {
            var ink = Look.DreamLook.Toon(new Color(0.18f, 0.14f, 0.32f), null, 0.3f, name: "M_ArenaBell_Post");
            _glow = Look.DreamLook.Toon(new Color(1f, 0.82f, 0.36f), null, 0.5f, name: "M_ArenaBell_Gold");
            GameObject P(PrimitiveType t, string n, Transform par, Vector3 lp, Vector3 s, Material m)
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(par, false);
                g.transform.localPosition = lp; g.transform.localScale = s; g.GetComponent<Renderer>().sharedMaterial = m; return g;
            }
            // gallows post + crossbar, bell hanging at punch height
            P(PrimitiveType.Cube, "Post", transform, new Vector3(-0.9f, 1.4f, 0), new Vector3(0.22f, 2.8f, 0.22f), ink);
            P(PrimitiveType.Cube, "Post2", transform, new Vector3(0.9f, 1.4f, 0), new Vector3(0.22f, 2.8f, 0.22f), ink);
            P(PrimitiveType.Cube, "Bar", transform, new Vector3(0, 2.8f, 0), new Vector3(2.2f, 0.22f, 0.3f), ink);
            _bell = new GameObject("Bell").transform; _bell.SetParent(transform, false); _bell.localPosition = new Vector3(0, 2.7f, 0);
            P(PrimitiveType.Sphere, "Dome", _bell, new Vector3(0, -0.55f, 0), new Vector3(1.1f, 1.05f, 1.1f), _glow);
            P(PrimitiveType.Cylinder, "Lip", _bell, new Vector3(0, -1.05f, 0), new Vector3(1.35f, 0.1f, 1.35f), _glow);
            P(PrimitiveType.Sphere, "Clapper", _bell, new Vector3(0, -1.25f, 0), Vector3.one * 0.3f, ink);
            var col = gameObject.AddComponent<SphereCollider>(); col.center = new Vector3(0, 1.6f, 0); col.radius = 0.9f; col.isTrigger = true;
            // recharge ring on the floor (scales up as the bell recharges)
            var lib = VFXLibrary.Instance;
            var ringMat = lib != null && lib.ring != null ? new Material(lib.ring) : Core.RuntimeMaterials.CreateUnlitTransparent(new Color(1f, 0.85f, 0.4f, 0.7f), null, "M_BellRing");
            ringMat.color = new Color(1f, 0.85f, 0.4f, 0.8f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Charge"; q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0, 0.05f, 0); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.GetComponent<Renderer>().sharedMaterial = ringMat; _ringFill = q.transform;
            Look.DreamLook.AddOutline(gameObject, 1.6f);
        }

        public bool TakeHit(HitInfo hit) { if (!Ready) { Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 0.3f); _swing = 0.3f; return false; } Ring(); return true; }

        public void Ring()
        {
            ReadyAt = Time.time + recharge; Rings++; _swing = 1f;
            Vector3 c = transform.position + Vector3.up * 1.6f;
            VFX.Play(VFXKind.ImpactRing, transform.position, radius * 0.45f, new Color(0.7f, 0.85f, 1f));
            VFX.Play(VFXKind.Rays, c, 1.2f, new Color(0.8f, 0.9f, 1f));
            VFX.Play(VFXKind.Flare, c, 0.8f);
            ComicPop.Show(c + Vector3.up, 1.8f, "BAM");
            Audio.AudioManager.Play(Audio.Sfx.BellToll, c, 1f);
            CameraShake.Add(0.25f);
            var boss = CountMidnight.Instance;
            if (boss != null) boss.BellFreeze(transform.position, radius, freezeSeconds);
        }

        void Update()
        {
            float k = Ready ? 1f : 1f - Mathf.Clamp01((ReadyAt - Time.time) / recharge);
            _ringFill.localScale = Vector3.one * Mathf.Lerp(0.6f, radius * 2f * 0.25f, k) * (Ready ? 1f + Mathf.Sin(Time.time * 4f) * 0.05f : 1f);
            _glow.SetColor("_Emission", Ready ? new Color(1.1f, 0.8f, 0.25f) * (0.6f + 0.4f * Mathf.Sin(Time.time * 3f)) : new Color(0.08f, 0.06f, 0.12f));
            _swing = Mathf.MoveTowards(_swing, 0f, Time.deltaTime * 0.7f);
            _bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 14f) * 28f * _swing, 0, 0);
        }
    }
}
