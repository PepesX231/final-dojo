using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// Ch4 Phantom Shelf-tops chase (docs/LEVEL1_DESIGN.md): THE CHAIR runs a waypoint path ahead of Pip.
    /// Rubber-banded so a sprinting player keeps up (sprint 8 m/s &gt; chair cruise), it throws rolling books back
    /// (jump them), and if it stays more than <see cref="maxLead"/> m ahead for <see cref="failSeconds"/> s:
    /// "IT GOT AWAY!" and the chase restarts. Reaching the end it crashes through the stained glass and opens
    /// the portal to the Cathedral.
    /// </summary>
    public class ChaseRunner : MonoBehaviour
    {
        public List<Transform> waypoints = new List<Transform>();
        public Transform playerRestart;
        public PagePortal endPortal;
        public float startRadius = 7f;
        public float cruiseSpeed = 6.6f, minSpeed = 4.2f, maxSpeed = 9.2f;
        public float idealLead = 9f, maxLead = 18f, failSeconds = 5f;
        public float bookInterval = 3.2f;
        public Transform visual;

        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        public int Fails { get; private set; }
        public float Lead { get; private set; }

        PlayerMotor3D _player;
        float _s, _farTime, _bookTimer, _total;
        readonly List<float> _cum = new List<float>();
        readonly List<RollingBook> _books = new List<RollingBook>();

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _cum.Clear(); _total = 0f; _cum.Add(0f);
            for (int i = 1; i < waypoints.Count; i++) { _total += Vector3.Distance(waypoints[i - 1].position, waypoints[i].position); _cum.Add(_total); }
            ResetChase();
            if (endPortal != null) endPortal.gameObject.SetActive(false);
        }

        public void ResetChase()
        {
            Running = false; _s = 0f; _farTime = 0f; _bookTimer = 1.5f;
            foreach (var b in _books) if (b != null) Destroy(b.gameObject);
            _books.Clear();
            Place(_s);
        }

        Vector3 PointAt(float s, out Vector3 dir)
        {
            dir = Vector3.forward;
            if (waypoints.Count < 2) return transform.position;
            s = Mathf.Clamp(s, 0f, _total);
            for (int i = 1; i < _cum.Count; i++)
            {
                if (s <= _cum[i] || i == _cum.Count - 1)
                {
                    float seg = Mathf.Max(0.001f, _cum[i] - _cum[i - 1]);
                    float k = Mathf.Clamp01((s - _cum[i - 1]) / seg);
                    Vector3 a = waypoints[i - 1].position, b = waypoints[i].position;
                    dir = (b - a).normalized;
                    return Vector3.Lerp(a, b, k);
                }
            }
            return waypoints[waypoints.Count - 1].position;
        }

        /// <summary>Path distance of the closest point to <paramref name="p"/>.</summary>
        public float Project(Vector3 p)
        {
            float best = 0f, bestD = float.MaxValue;
            for (int i = 1; i < waypoints.Count; i++)
            {
                Vector3 a = waypoints[i - 1].position, b = waypoints[i].position;
                Vector3 ab = b - a; float len = ab.magnitude;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, len * len));
                float d = (a + ab * t - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = _cum[i - 1] + len * t; }
            }
            return best;
        }

        void Place(float s)
        {
            Vector3 p = PointAt(s, out var dir);
            transform.position = p;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void Update()
        {
            if (_player == null || Finished || waypoints.Count < 2) return;
            float dt = Time.deltaTime;
            if (!Running)
            {
                if ((_player.transform.position - transform.position).magnitude < startRadius)
                {
                    Running = true;
                    BossIntroCard.Show("CHASE!", "ห้ามให้มันหนีไปได้!", chapterCard: true);
                    Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 1f);
                    Audio.AudioManager.PlayMusic(Audio.Music.Boss, 0.4f);
                }
                if (visual != null) visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.1f, 0);
                return;
            }
            float ps = Project(_player.transform.position);
            Lead = _s - ps;
            // rubber band: slower when far ahead, faster when the apple is close (it panics)
            float speed = Lead > idealLead ? Mathf.Lerp(cruiseSpeed, minSpeed, Mathf.InverseLerp(idealLead, maxLead, Lead))
                                           : Mathf.Lerp(maxSpeed, cruiseSpeed, Mathf.InverseLerp(2f, idealLead, Lead));
            _s += speed * dt;
            Place(_s);
            if (visual != null)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * speed * 2.1f));
                visual.localPosition = new Vector3(0, hop * 0.22f, 0);
                visual.localRotation = Quaternion.Euler(-8f, 0, Mathf.Sin(Time.time * speed * 2.1f) * 7f);
            }
            if (Lead > maxLead) { _farTime += dt; if (_farTime > failSeconds) StartCoroutine(GotAway()); }
            else _farTime = Mathf.Max(0f, _farTime - dt * 2f);

            _bookTimer -= dt;
            if (_bookTimer <= 0f && Lead > 4f)
            {
                _bookTimer = bookInterval * Random.Range(0.8f, 1.2f);
                ThrowBook();
            }
            if (_s >= _total - 0.2f) StartCoroutine(Finish());
        }

        void ThrowBook()
        {
            Vector3 p = PointAt(_s - 1.5f, out var dir);
            var go = new GameObject("RollingBook");
            go.transform.position = p + Vector3.up * 0.5f;
            var b = go.AddComponent<RollingBook>();
            b.Init(this, _s - 1.5f, _player);
            _books.Add(b);
            Audio.AudioManager.Play(Audio.Sfx.Whoosh, p, 0.7f);
        }

        public Vector3 SamplePath(float s, out Vector3 dir) => PointAt(s, out dir);

        IEnumerator GotAway()
        {
            if (!Running) yield break;
            Running = false; Fails++;
            BossIntroCard.Show("IT GOT AWAY!", "ลองใหม่ — วิ่ง (SHIFT) ตามให้ทัน", chapterCard: true);
            yield return new WaitForSeconds(1.6f);
            yield return TransitionOverlay.Get().Close(0.5f, "");
            ResetChase();
            if (playerRestart != null) _player.Teleport(playerRestart.position, playerRestart.rotation);
            yield return TransitionOverlay.Get().Open(0.5f);
        }

        IEnumerator Finish()
        {
            Finished = true; Running = false;
            VFX.Play(VFXKind.Confetti, transform.position + Vector3.up, 1.2f);
            VFX.Play(VFXKind.ImpactRing, transform.position, 1.4f, VFX.Gold);
            CameraShake.Add(0.6f);
            UIRoot.Get().Toast("CRASH!! มันพุ่งทะลุกระจกสีเข้าไปในวิหาร!", 2.4f);
            Audio.AudioManager.Play(Audio.Sfx.ChairStumble, transform.position, 1f);
            foreach (var b in _books) if (b != null) Destroy(b.gameObject);
            if (endPortal != null) endPortal.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            if (visual != null) visual.gameObject.SetActive(false);
        }

        /// <summary>Called by a book that hit the apple: the chair gains distance (the pitch).</summary>
        public void OnBookHit() { _s += 2.5f; }
    }

    /// <summary>A giant book thrown back down the chase path: rolls toward Pip; jump it or get bonked back.</summary>
    public class RollingBook : MonoBehaviour
    {
        ChaseRunner _runner;
        PlayerMotor3D _player;
        float _s, _life;
        Transform _vis;
        bool _hit;
        static Material _cover, _pages;

        public void Init(ChaseRunner runner, float s, PlayerMotor3D player)
        {
            _runner = runner; _s = s; _player = player;
            _cover ??= Look.DreamLook.Toon(new Color(0.9f, 0.18f, 0.25f), null, 0.3f, name: "M_RollingBookCover");
            _pages ??= Look.DreamLook.Toon(new Color(0.98f, 0.94f, 0.86f), null, 0.2f, name: "M_RollingBookPages");
            _vis = new GameObject("Vis").transform; _vis.SetParent(transform, false);
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(_vis, false); c.transform.localScale = new Vector3(1.6f, 1.1f, 0.4f);
            c.GetComponent<Renderer>().sharedMaterial = _cover;
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(_vis, false); p.transform.localScale = new Vector3(1.5f, 1.0f, 0.42f); p.transform.localPosition = new Vector3(0.06f, 0, 0);
            p.GetComponent<Renderer>().sharedMaterial = _pages;
        }

        void Update()
        {
            if (_runner == null || _player == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _life += dt;
            _s -= 7.5f * dt;
            Vector3 p = _runner.SamplePath(_s, out var dir);
            transform.position = p + Vector3.up * 0.55f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            _vis.localRotation = Quaternion.Euler(-_life * 540f, 0, 0) * Quaternion.Euler(0, 90, 0);
            if (_life > 7f || _s < 0f) { Destroy(gameObject); return; }
            if (_hit) return;
            Vector3 d = _player.transform.position - transform.position;
            float h = _player.transform.position.y - p.y;
            if (new Vector2(d.x, d.z).magnitude < 0.95f && h < 0.7f)
            {
                _hit = true;
                _player.Launch(-dir * 7f + Vector3.up * 5.5f);
                VFX.Play(VFXKind.StarBurst, _player.transform.position + Vector3.up, 1f, VFX.Danger);
                Audio.AudioManager.Play(Audio.Sfx.Kick, p, 0.8f);
                CameraShake.Add(0.3f);
                UIRoot.Get().Toast("BONK! มันหนีห่างออกไปอีก", 1.2f);
                _runner.OnBookHit();
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
