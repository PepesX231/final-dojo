using System;
using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>
    /// COUNT MIDNIGHT — "The Twelfth Hour" (docs/LEVEL2_DESIGN.md §4). Level-2 boss: a 6 m clock knight in a night-sky cape,
    /// fighting with the hour hand (heavy) and the minute hand (fast) as twin swords. Elden-Ring grammar, dream rules:
    ///   • every attack has a readable wind-up (blade glows red + sound, ≥0.5 s); some are DELAYED (glow flickers, then a late swing)
    ///   • low sweeps / shockwaves can be JUMPED (platformer skill), thrusts and cleaves are ROLLED
    ///   • damage builds POSTURE; a broken posture makes him kneel → hit the clock face for a CRITICAL
    ///   • at 50 % → STOP THE CLOCK: time freezes (grey world), Undertale-style bullet patterns, then a faster phase 2
    ///   • at 0 HP he kneels one last time: hit his clock face / land on it → SNOOZE → victory (no plain HP win: GDD §1.3 B)
    /// The body is a graybox built in code unless a model is supplied (M13 replaces it with the Blender model).
    ///
    /// M19 (user: "the boss is dumb — just hit and dodge"): he is a WOUND-UP clock now, and the fight is about reading him:
    ///   • WINDING KEY ARMOUR — while wound, punches from the front clang off. Unwind the brass key on his BACK (get behind
    ///     him after a big swing), punch the HOUR HAND while it is stuck in the dial after a cleave, or freeze him with a BELL.
    ///     Fully unwound → he RUNS DOWN (kneels: free hits + a critical on the dial), then winds himself back up.
    ///   • ARENA BELLS — 4 stop-the-clock bells (taught on the east roofs): ring one with him inside its bubble and he freezes
    ///     mid-attack, armour open. Bells recharge, so it's "bait him to a bell".
    ///   • He guards his back: stand behind him too long and he spins with a BACKHAND.
    ///   • STOP THE CLOCK — golden numerals fly at Pip among the bullets: punch them back into his face (3 = he's dazed).
    ///   • Phase 2 MIDNIGHT STRIKES — he calls two hours; the dial floor detonates except beside the lit numerals: run there.
    /// </summary>
    public class CountMidnight : MonoBehaviour, IHittable
    {
        public enum Mode { Waiting, Intro, Fight, Attacking, Staggered, TimeStop, Finisher, Defeated, Frozen }

        [Header("Stats")]
        public float maxHP = 1400f, maxPosture = 300f, postureRegen = 22f, postureRegenDelay = 2.2f;
        public float walkSpeed = 3.4f, turnSpeed = 150f;
        public float staggerSeconds = 4.5f, criticalDamage = 240f;
        [Header("Arena")]
        public Vector3 arenaCenter; public float arenaRadius = 20f, wakeRadius = 17f;
        [Header("Refs (built if empty)")]
        public Transform model, head, armL, armR, bladeHour, bladeMinute, hourHand, minuteHand;
        public MidnightBullets bullets;
        public UnityEngine.Rendering.Volume timeStopVolume;
        public bool AutoFight = true;
        /// <summary>Tests: stand still in Fight mode (no stalking/turning).</summary>
        public bool DevHold;

        [Header("M19 mechanics")]
        public float maxWind = 3f;
        public float unwindBehind = 0.5f, unwindStuck = 0.4f, unwindFrozen = 0.5f;
        public float stuckSeconds = 1.6f, backhandAfter = 0.45f;

        public static CountMidnight Instance { get; private set; }
        public float Wind { get; private set; }
        public bool Wound => Wind > 0.01f;
        public bool HandStuck { get; private set; }
        public int Unwinds { get; private set; }
        public int Clangs { get; private set; }
        public int Freezes { get; private set; }
        public int Reflected { get; private set; }
        public int StrikesSurvived { get; private set; }
        /// <summary>One line for the boss bar: what to do right now.</summary>
        public string StatusLine { get; private set; } = "";
        Transform _key; float _keySpin, _behindFor, _frozenUntil, _clangHintAt = -99f; BossWeakPoint _keyWP;
        int _unwindFrame = -1, _staggerFrame = -1;

        public Mode Current { get; private set; } = Mode.Waiting;
        public int Phase { get; private set; } = 1;
        public float HP { get; private set; }
        public float Posture { get; private set; }
        public string LastAttack { get; private set; }
        public float TimeStopWeight => timeStopVolume != null ? timeStopVolume.weight : 0f;
        public int AttacksDone { get; private set; }
        public int PlayerHits { get; private set; }
        public event Action<Mode> ModeChanged;
        public event Action Victory;

        PlayerMotor3D _player;
        IPlayerDamageReceiver _receiver;
        float _lastHit = -99f, _nextAttack;
        Renderer[] _hourGlow, _minGlow;
        readonly List<Material> _glowMats = new List<Material>();
        Vector3 _modelBase;
        Coroutine _attackCo;
        Transform _clockH, _clockM;

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        void Awake()
        {
            HP = maxHP; Posture = 0f; Wind = maxWind; Instance = this;
            if (model == null) BuildGraybox(); else BindModel();
            _modelBase = model.localPosition;
            if (bullets == null) bullets = new GameObject("MidnightBullets").AddComponent<MidnightBullets>();
            if (GetComponent<LockOnTarget>() == null) { var lt = gameObject.AddComponent<LockOnTarget>(); lt.offset = new Vector3(0, 3.2f, 0); }
        }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _receiver = _player != null ? _player.GetComponent<IPlayerDamageReceiver>() : null;
            if (arenaCenter == Vector3.zero) arenaCenter = transform.position;
            BuildKey();
            if (ArenaBell.All.Count == 0)   // 4 bells around the dial (between the numerals so they don't block cover)
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f) * Mathf.Deg2Rad;
                    ArenaBell.Create(transform.parent, arenaCenter + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (arenaRadius - 8f));
                }
        }

        // ---- tests / dev tools
        public void DevSetHP(float hp) => HP = Mathf.Clamp(hp, 1f, maxHP);
        public void DevSetPosture(float p) => Posture = p;
        public void DevSetWind(float w) => Wind = Mathf.Clamp(w, 0f, maxWind);
        /// <summary>Skip the intro: straight into the fight (AutoFight decides whether he attacks on his own).</summary>
        public void DevBeginFight() { StopAllCoroutines(); _attackCo = null; ResetArms(); SetMode(Mode.Fight); UI.BossBarUI.Show(this); _nextAttack = Time.time + 1f; }

        void SetMode(Mode m) { if (Current == m) return; Current = m; ModeChanged?.Invoke(m); }

        // ================================================================== loop
        void Update()
        {
            float dt = Time.deltaTime;
            AnimateClockFace();
            if (_player == null) return;
            if (Time.time - _lastHit > postureRegenDelay && Current != Mode.Staggered) Posture = Mathf.Max(0f, Posture - postureRegen * dt);

            UpdateKey(dt);
            if (Current == Mode.Frozen && Time.time >= _frozenUntil) Unfreeze();
            switch (Current)
            {
                case Mode.Waiting:
                    if (AutoFight && Flat(_player.transform.position - arenaCenter).magnitude < wakeRadius) StartCoroutine(Intro());
                    break;
                case Mode.Fight:
                    if (!DevHold) Stalk(dt);
                    if (AutoFight && _behindFor > backhandAfter) { _behindFor = 0f; DoAttack("Backhand"); break; }   // reflex: he guards his key
                    if (AutoFight && Time.time >= _nextAttack) ChooseAttack();
                    break;
                case Mode.Staggered:
                case Mode.Finisher:
                    CheckHeadStomp();
                    break;
            }
            Idle(dt);
        }

        // ================================================================== M19: winding key, bells, backhand
        bool PipBehind(float maxDist = 7f)
        {
            if (_player == null) return false;
            Vector3 to = Flat(_player.transform.position - transform.position);
            return to.magnitude < maxDist && Vector3.Dot(transform.forward, to.normalized) < -0.25f;
        }

        void BuildKey()
        {
            if (_key != null) return;
            var gold = Look.DreamLook.Toon(new Color(1f, 0.8f, 0.32f), null, 0.5f, name: "M_CM_Key");
            _bodyMats.Add(gold);
            _key = new GameObject("WindingKey").transform; _key.SetParent(transform, false);
            _key.localPosition = new Vector3(0, 2.5f, -1.35f);
            GameObject P(PrimitiveType t, string n, Vector3 lp, Vector3 sc, Vector3 eu)
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(_key, false);
                g.transform.localPosition = lp; g.transform.localScale = sc; g.transform.localRotation = Quaternion.Euler(eu);
                g.GetComponent<Renderer>().sharedMaterial = gold; return g;
            }
            P(PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0, -0.35f), new Vector3(0.22f, 0.4f, 0.22f), new Vector3(90, 0, 0));
            P(PrimitiveType.Cube, "Bar", new Vector3(0, 0, -0.75f), new Vector3(2.0f, 0.24f, 0.16f), Vector3.zero);
            for (int sgn = -1; sgn <= 1; sgn += 2)
                P(PrimitiveType.Cylinder, "Loop", new Vector3(sgn * 1.05f, 0, -0.75f), new Vector3(0.95f, 0.08f, 0.95f), new Vector3(90, 0, 0));
            Look.DreamLook.AddOutline(_key.gameObject, 1.8f);
            _keyWP = BossWeakPoint.Create("KeyHitbox", _key, new Vector3(0, 0, -0.7f), 1.25f, h => HitKey(h));
        }

        void UpdateKey(float dt)
        {
            if (_key == null) return;
            bool open = Current == Mode.Frozen || PipBehind(4.5f);
            _keySpin += (Wound && Current != Mode.Frozen ? 140f + Wind * 40f : 0f) * dt;
            _key.localRotation = Quaternion.Euler(0, 0, _keySpin);
            _key.localScale = Vector3.one * (Wound ? 1f + (open ? Mathf.Sin(Time.time * 10f) * 0.06f : 0f) : 0.85f);
            _behindFor = PipBehind(6f) && (Current == Mode.Fight) ? _behindFor + dt : 0f;
            StatusLine = !Wound ? (Current == Mode.Staggered ? "หมดลาน! — ต่อยหน้าปัด!" : "")
                       : Current == Mode.Frozen ? "หยุดเวลา! — รัวหมัดเลย!"
                       : HandStuck ? "เข็มปักพื้น! — ต่อยเข็ม!"
                       : $"ลาน {Mathf.CeilToInt(Wind / 0.5f) * 0.5f:0.#}/{maxWind:0} — ต่อยกุญแจข้างหลัง · ระฆัง · เข็มที่ปัก";
        }

        /// <summary>Unwind the key (once per frame: a punch can touch both the key and the body).</summary>
        bool Unwind(float turns, Vector3 at)
        {
            if (!Wound || Time.frameCount == _unwindFrame) return false;
            _unwindFrame = Time.frameCount; Unwinds++;
            Wind = Mathf.Max(0f, Wind - turns); _keySpin -= turns * 360f;
            VFX.Play(VFXKind.HitSpark, at, 1.3f, VFX.Gold); VFX.Play(VFXKind.Embers, at, 0.8f);
            Audio.AudioManager.Play(Audio.Sfx.PunchHeavy, at, 0.9f); Audio.AudioManager.Play(Audio.Sfx.Ratchet, at, 0.9f);
            if (!Wound)
            {
                Presentation.ComicPop.Show(_key.position + Vector3.up, 2.4f, "POP");
                UI.UIRoot.Get().Toast("หมดลาน!  RUN DOWN — ต่อยหน้าปัดตอนเขาคุกเข่า!", 2.2f);
                if (Current == Mode.Frozen) { ClearFreezeLook(); Current = Mode.Fight; }
                Posture = maxPosture;
                if (Current != Mode.Staggered && Current != Mode.Finisher && Current != Mode.TimeStop) StartCoroutine(Stagger());
            }
            return true;
        }

        bool HitKey(HitInfo hit)
        {
            if (!Wound || Current == Mode.Waiting || Current == Mode.Intro || Current == Mode.TimeStop || Current == Mode.Defeated) return false;
            float turns = (Current == Mode.Frozen ? unwindFrozen : unwindBehind) * (hit.heavy ? 2f : 1f);
            bool done = Unwind(turns, _key.position);
            if (done) { _lastHit = Time.time; HP = Mathf.Max(1f, HP - hit.damage * 0.5f); Flash(); }
            return done;
        }

        /// <summary>A bell tolled at <paramref name="at"/>: inside its bubble he freezes mid-attack (armour open).</summary>
        public void BellFreeze(Vector3 at, float radius, float seconds)
        {
            bullets.ClearInRadius(at, radius);
            if (Current != Mode.Fight && Current != Mode.Attacking) return;
            if (Flat(transform.position - at).magnitude > radius + 1.5f) { UI.UIRoot.Get().Toast("ไกลไป — ล่อเขาเข้ามาใกล้ระฆังก่อน!", 1.6f); return; }
            if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
            CleanupStuck();
            Freezes++; _frozenUntil = Time.time + seconds;
            SetMode(Mode.Frozen); SetGlow(0, 0);
            foreach (var m in _bodyMats) if (m != null) m.SetColor("_Emission", new Color(0.15f, 0.3f, 0.6f));
            UI.UIRoot.Get().Toast("FROZEN! — เกราะลานเปิด รัวหมัด!", 1.6f);
            if (timeStopVolume != null) timeStopVolume.weight = 0.35f;
        }

        void ClearFreezeLook()
        {
            foreach (var m in _bodyMats) if (m != null) m.SetColor("_Emission", Color.black);
            if (timeStopVolume != null && Current == Mode.Frozen) timeStopVolume.weight = 0f;
        }

        void CleanupStuck() { HandStuck = false; if (_stuckWP != null) Destroy(_stuckWP.gameObject); _stuckWP = null; }
        BossWeakPoint _stuckWP;

        void Unfreeze()
        {
            ClearFreezeLook();
            ResetArms();
            SetMode(Mode.Fight); _nextAttack = Time.time + 0.5f;
            CameraShake.Add(0.2f);
        }

        IEnumerator Rewind()
        {
            UI.UIRoot.Get().Toast("…กริ๊ก กริ๊ก — เขาไขลานตัวเองกลับแล้ว!", 1.8f);
            Audio.AudioManager.Play(Audio.Sfx.Ratchet, transform.position, 1f);
            float from = Wind;
            for (float t = 0; t < 0.9f; t += Time.deltaTime) { Wind = Mathf.Lerp(from, maxWind, t / 0.9f); _keySpin += 900f * Time.deltaTime; yield return null; }
            Wind = maxWind;
        }

        IEnumerator Intro()
        {
            SetMode(Mode.Intro);
            UI.BossIntroCard.Show("COUNT MIDNIGHT", "— THE TWELFTH HOUR —");
            Audio.AudioManager.PlayMusic(Audio.Music.Midnight, 0.5f);
            Audio.AudioManager.Play(Audio.Sfx.ChairGasp, transform.position, 1f);
            // the clock strikes: raise both blades
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                float k = UI.UIStyle.OutCubic(t / 1.2f);
                armL.localRotation = Quaternion.Euler(-150f * k, 0, 20f);
                armR.localRotation = Quaternion.Euler(-150f * k, 0, -20f);
                yield return null;
            }
            for (int i = 0; i < 3; i++) { CameraShake.Add(0.25f); Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 0.8f); yield return new WaitForSeconds(0.35f); }
            UI.BossBarUI.Show(this);
            SetMode(Mode.Fight);
            _nextAttack = Time.time + 1.2f;
        }

        void Stalk(float dt)
        {
            Vector3 to = Flat(_player.transform.position - transform.position);
            float d = to.magnitude;
            Face(to, dt);
            // keep a duelling distance: approach when far, circle when mid, back off a little when hugged
            Vector3 move = Vector3.zero;
            if (d > 6.5f) move = to.normalized;
            else if (d < 2.2f) move = -to.normalized * 0.5f;
            else move = Vector3.Cross(Vector3.up, to.normalized) * Mathf.Sin(Time.time * 0.6f) * 0.6f;
            Step(move * walkSpeed * (Phase == 2 ? 1.25f : 1f) * dt);
        }

        void Step(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            Vector3 off = Flat(p - arenaCenter);
            if (off.magnitude > arenaRadius - 2.5f) p = arenaCenter + off.normalized * (arenaRadius - 2.5f) + Vector3.up * (p.y - arenaCenter.y);
            transform.position = p;
        }

        void Face(Vector3 dir, float dt, float speed = -1f)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir.normalized), (speed > 0 ? speed : turnSpeed) * dt);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void ChooseAttack()
        {
            float d = Flat(_player.transform.position - transform.position).magnitude;
            if (Phase == 2 && _strikeDue) { _strikeDue = false; DoAttack("Strikes"); return; }
            var options = new List<(string n, float w)>();
            if (d < 5f) { options.Add(("Sweep", 3f)); options.Add(("TickTock", 3f)); options.Add(("Cleave", 1f)); }
            else if (d < 10f) { options.Add(("Cleave", 3f)); options.Add(("TickTock", 2f)); options.Add(("Lunge", 1.5f)); }
            else { options.Add(("Lunge", 4f)); options.Add(("Cleave", 0.8f)); }
            if (Phase == 2) { options.Add(("Gears", 1.5f)); options.Add(("Combo", 2f)); options.Add(("Strikes", 1.2f)); }
            if (options.Exists(o => o.n == LastAttack) && options.Count > 1) options.RemoveAll(o => o.n == LastAttack);   // no repeats
            float sum = 0; foreach (var o in options) sum += o.w;
            float r = UnityEngine.Random.value * sum; string pick = options[0].n;
            foreach (var o in options) { r -= o.w; if (r <= 0) { pick = o.n; break; } }
            DoAttack(pick);
        }

        /// <summary>Starts an attack by name (tests and dev commands call this directly).</summary>
        public Coroutine DoAttack(string name)
        {
            if (_attackCo != null) StopCoroutine(_attackCo);
            IEnumerator co = name switch
            {
                "Cleave" => HourCleave(UnityEngine.Random.value < (Phase == 2 ? 0.45f : 0.25f)),
                "CleaveFast" => HourCleave(false),
                "CleaveDelayed" => HourCleave(true),
                "Sweep" => MinuteSweep(),
                "TickTock" => TickTock(),
                "Lunge" => PendulumLunge(),
                "Gears" => GearBurst(),
                "Combo" => Combo(),
                "StopTheClock" => StopTheClock(),
                "Backhand" => Backhand(),
                "Strikes" => MidnightStrikes(),
                _ => HourCleave(false),
            };
            _attackCo = StartCoroutine(Wrap(name, co));
            return _attackCo;
        }

        IEnumerator Wrap(string name, IEnumerator co)
        {
            SetMode(Mode.Attacking);
            LastAttack = name; AttacksDone++;
            yield return co;
            SetGlow(0f, 0f);
            ResetArms();
            if (Current == Mode.Attacking) SetMode(Mode.Fight);
            _nextAttack = Time.time + (Phase == 2 ? UnityEngine.Random.Range(0.35f, 0.8f) : UnityEngine.Random.Range(0.7f, 1.3f));
            _attackCo = null;
        }

        // ================================================================== attacks
        IEnumerator Windup(float seconds, Transform arm, Quaternion pose, bool hour, bool minute)
        {
            Quaternion from = arm.localRotation;
            Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 0.7f);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = UI.UIStyle.OutCubic(t / seconds);
                arm.localRotation = Quaternion.Slerp(from, pose, k);
                SetGlow(hour ? k : 0f, minute ? k : 0f);
                Face(Flat(_player.transform.position - transform.position), Time.deltaTime, turnSpeed * 0.8f);
                yield return null;
            }
        }

        IEnumerator HourCleave(bool delayed)
        {
            yield return Windup(Phase == 2 ? 0.7f : 0.85f, armR, Quaternion.Euler(-200f, 0, -10f), true, false);
            if (delayed)
            {   // ER-style delay: hold high, glow flickers — the swing comes late
                for (float t = 0; t < 0.5f; t += Time.deltaTime) { SetGlow(0.6f + Mathf.PingPong(t * 10f, 0.4f), 0f); yield return null; }
                SetGlow(1.4f, 0f);
            }
            // SLAM
            yield return Swing(armR, Quaternion.Euler(-20f, 0, -10f), 0.12f);
            Vector3 fwd = transform.forward;
            Vector3 hitC = transform.position + fwd * 4f;
            HitBox(hitC, fwd, new Vector3(1.8f, 2f, 8f), "the hour hand");
            VFX.Play(VFXKind.ImpactRing, transform.position + fwd * 6.5f, 1.6f, VFX.Danger);
            VFX.Play(VFXKind.DustPuff, transform.position + fwd * 6.5f, 2f);
            CameraShake.Add(0.6f);
            Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f);
            // shockwave line that travels on (jump it)
            var wave = StartCoroutine(Shockwave(transform.position + fwd * 7f, fwd, 13f, 15f));
            // M19: the hour hand is STUCK in the dial for a moment — punch it to wind him down
            yield return StuckHand(transform.position + fwd * 6.2f + Vector3.up * 0.8f);
            yield return wave;
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator StuckHand(Vector3 at)
        {
            if (!Wound) { yield return new WaitForSeconds(0.5f); yield break; }
            HandStuck = true; int hits = 0;
            var wp = _stuckWP = BossWeakPoint.Create("StuckHourHand", transform, transform.InverseTransformPoint(at), 1.5f, h =>
            {
                if (!HandStuck) return false;
                hits++; _lastHit = Time.time;
                Unwind(unwindStuck * (h.heavy ? 2f : 1f), at);
                Presentation.ComicPop.Show(at, 1.5f, hits >= 3 ? "WHAM" : null);
                return true;
            });
            wp.transform.position = at;
            float dur = Phase == 2 ? stuckSeconds * 0.7f : stuckSeconds;
            for (float t = 0; t < dur && hits < 3 && HandStuck; t += Time.deltaTime)
            {
                SetGlow(0.5f + Mathf.PingPong(t * 6f, 0.6f), 0f);   // pulsing = "hit me"
                armR.localRotation = Quaternion.Euler(-20f + Mathf.Sin(t * 50f) * 2f, 0, -10f);   // straining to pull it out
                if (Mathf.Repeat(t, 0.25f) < Time.deltaTime) VFX.Play(VFXKind.Flare, at, 0.45f, VFX.Gold);
                yield return null;
            }
            CleanupStuck();
            // yanks it free
            VFX.Play(VFXKind.DustPuff, at, 1.6f); Audio.AudioManager.Play(Audio.Sfx.Whoosh, at, 0.8f);
        }

        IEnumerator Backhand()
        {
            UI.UIRoot.Get().Toast("!", 0.4f);
            yield return Windup(0.45f, armL, Quaternion.Euler(-90f, 120f, 40f), false, true);
            for (float t = 0; t < 0.22f; t += Time.deltaTime) { transform.Rotate(0, 180f * Time.deltaTime / 0.22f, 0); yield return null; }
            Vector3 back = Flat(_player.transform.position - transform.position).normalized;   // the spin covers all round
            Sector(transform.position, back, 5.5f, 160f, 3f, "a backhand (he guards his key!)");
            VFX.Play(VFXKind.ImpactRing, transform.position + back * 2f, 1.4f, VFX.Danger);
            CameraShake.Add(0.3f); Audio.AudioManager.Play(Audio.Sfx.SwingHeavy, transform.position, 0.9f);
            yield return new WaitForSeconds(0.55f);
        }

        // ---- phase 2: MIDNIGHT STRIKES — he calls two hours; only beside those numerals is safe
        bool _strikeDue;
        readonly List<GameObject> _strikePads = new List<GameObject>();
        public IReadOnlyList<GameObject> StrikePads => _strikePads;
        public Vector3 NumeralPos(int i) { float a = i / 12f * Mathf.PI * 2f; return arenaCenter + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (arenaRadius - 6.2f); }

        IEnumerator MidnightStrikes()
        {
            // one hour near Pip (reachable), one across the dial
            Vector3 pp = Flat(_player.transform.position - arenaCenter);
            int near = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(pp.x, pp.z) * Mathf.Rad2Deg, 360f) / 30f + (UnityEngine.Random.value < 0.5f ? 2 : -2)) % 12;
            if (near < 0) near += 12;
            int far = (near + 6) % 12;
            ClearPads();
            var lib = VFXLibrary.Instance;
            foreach (int i in new[] { near, far })
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pad.name = "StrikeSafe_" + (i == 0 ? 12 : i); Destroy(pad.GetComponent<Collider>());
                pad.transform.position = NumeralPos(i) + Vector3.up * 0.06f; pad.transform.localScale = new Vector3(6f, 0.02f, 6f);
                var m = Look.DreamLook.Toon(new Color(1f, 0.85f, 0.35f), null, 0f, name: "M_StrikeSafe"); m.SetColor("_Emission", new Color(1.4f, 1.0f, 0.3f));
                pad.GetComponent<Renderer>().sharedMaterial = m; _strikePads.Add(pad);
                VFX.Play(VFXKind.Rays, pad.transform.position + Vector3.up, 1.2f, VFX.Gold);
            }
            UI.BossIntroCard.Show("MIDNIGHT STRIKES", $"— วิ่งไปยืนข้างเลข {(near == 0 ? 12 : near)} หรือ {(far == 0 ? 12 : far)} ที่เรืองแสง! —", chapterCard: false);
            armL.localRotation = Quaternion.Euler(-178f, 0, 6f); armR.localRotation = Quaternion.Euler(-178f, 0, -6f);
            // whole-dial telegraph that ticks faster and faster
            float warn = 3.2f;
            for (float t = 0; t < warn; t += Time.deltaTime)
            {
                float rate = Mathf.Lerp(0.7f, 0.15f, t / warn);
                if (Mathf.Repeat(t, rate) < Time.deltaTime) { VFX.Play(VFXKind.ImpactRing, arenaCenter, arenaRadius * 0.35f, VFX.Danger); Audio.AudioManager.Play(Audio.Sfx.Tick, arenaCenter, 0.8f); }
                SetGlow(t / warn, t / warn);
                foreach (var pad in _strikePads) pad.transform.localScale = new Vector3(6f, 0.02f, 6f) * (1f + Mathf.Sin(t * 12f) * 0.04f);
                yield return null;
            }
            // STRIKE
            CameraShake.Add(0.9f);
            VFX.Play(VFXKind.Flare, transform.position + Vector3.up * 5f, 2f); VFX.Play(VFXKind.FlareStreak, arenaCenter + Vector3.up * 2f, 2f);
            for (int k = 0; k < 10; k++) { float a = k / 10f * Mathf.PI * 2f; VFX.Play(VFXKind.Rays, arenaCenter + new Vector3(Mathf.Sin(a), 0.5f, Mathf.Cos(a)) * (arenaRadius * 0.55f), 1.4f, VFX.Danger); }
            Audio.AudioManager.Play(Audio.Sfx.Burst, arenaCenter, 1f);
            bool safe = false;
            foreach (var pad in _strikePads) if (Flat(_player.transform.position - pad.transform.position).magnitude < 3.3f) safe = true;
            if (safe) StrikesSurvived++;
            else Hurt("MIDNIGHT STRIKES (stand by a lit numeral!)", _player.transform.position + Vector3.up);
            SetGlow(0, 0);
            yield return new WaitForSeconds(0.6f);
            ClearPads();
            yield return new WaitForSeconds(0.6f);
        }

        void ClearPads() { foreach (var p in _strikePads) if (p != null) Destroy(p); _strikePads.Clear(); }

        IEnumerator MinuteSweep()
        {
            yield return Windup(Phase == 2 ? 0.55f : 0.65f, armL, Quaternion.Euler(-80f, -120f, 60f), false, true);
            model.localPosition = _modelBase + Vector3.down * 0.6f;   // crouch: the sweep is LOW → jump it
            Vector3 c = transform.position;
            bool hit = false;
            Quaternion from = armL.localRotation;
            float dur = 0.42f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                armL.localRotation = from * Quaternion.Euler(0, 360f * k, 0);
                transform.Rotate(0, 360f * Time.deltaTime / dur, 0);
                if (!hit)
                {
                    Vector3 d = _player.transform.position - c; float r = Flat(d).magnitude;
                    if (r < 6.8f && r > 1f && d.y < 0.9f) hit = Hurt("the minute hand (jump it!)", c);
                }
                yield return null;
            }
            VFX.Play(VFXKind.LandingRing, c, 2.6f, VFX.Lavender);
            model.localPosition = _modelBase;
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator TickTock()
        {
            int n = Phase == 2 ? 4 : 3;
            for (int i = 0; i < n; i++)
            {
                bool right = i % 2 == 0;
                var arm = right ? armR : armL;
                yield return Windup(i == 0 ? 0.55f : 0.32f, arm, Quaternion.Euler(-120f, right ? -60f : 60f, right ? -50f : 50f), right, !right);
                Step(transform.forward * 1.6f);
                yield return Swing(arm, Quaternion.Euler(-30f, right ? 70f : -70f, right ? -30f : 30f), 0.1f);
                Sector(transform.position, transform.forward, 5.2f, 110f, 3f, right ? "TICK" : "TOCK");
                Audio.AudioManager.Play(Audio.Sfx.ChairStep, transform.position, 1f);
                CameraShake.Add(0.2f);
            }
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator PendulumLunge()
        {
            yield return Windup(0.7f, armR, Quaternion.Euler(-90f, 0, -90f), false, true);
            model.localRotation = Quaternion.Euler(-12f, 0, 0);
            Vector3 dir = Flat(_player.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(dir);
            float dist = 13f, dur = 0.42f; bool hit = false;
            Vector3 start = transform.position;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = UI.UIStyle.OutCubic(t / dur);
                Vector3 p = start + dir * dist * k; Vector3 prev = transform.position;
                Step(p - prev);
                if (!hit)
                {
                    Vector3 d = Flat(_player.transform.position - transform.position);
                    if (d.magnitude < 2.6f && _player.transform.position.y - transform.position.y < 2.5f) hit = Hurt("the pendulum lunge", transform.position);
                }
                if (Mathf.Repeat(t, 0.05f) < Time.deltaTime) VFX.Play(VFXKind.DustPuff, transform.position, 1f);
                yield return null;
            }
            model.localRotation = Quaternion.identity;
            yield return new WaitForSeconds(0.7f);
        }

        IEnumerator GearBurst()
        {
            yield return Windup(0.6f, armL, Quaternion.Euler(-170f, 0, 30f), true, true);
            yield return bullets.GearSpiral(transform.position, 1.6f, 0.11f, 26f, 8f, 3);
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator Combo()
        {
            yield return TickTock();
            yield return HourCleave(true);
        }

        // ================================================================== STOP THE CLOCK (phase change)
        IEnumerator StopTheClock()
        {
            CleanupStuck(); ClearPads(); ClearFreezeLook();
            SetMode(Mode.TimeStop);
            bullets.ClearAll();
            // leap to the centre of the dial
            Vector3 from = transform.position, to = arenaCenter;
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                float k = t / 0.9f;
                transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 6f;
                yield return null;
            }
            transform.position = to;
            CameraShake.Add(0.8f);
            VFX.Play(VFXKind.ImpactRing, to, 3f, VFX.Lavender);
            armL.localRotation = Quaternion.Euler(-178f, 0, 6f); armR.localRotation = Quaternion.Euler(-178f, 0, -6f);   // both hands at XII
            UI.BossIntroCard.Show("STOP THE CLOCK", "— ทุกอย่างหยุด… ยกเว้นเรา —", chapterCard: true);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, to, 1f);
            yield return FadeTimeStop(1f, 0.6f);
            Reflected = 0;
            UI.UIRoot.Get().Toast("เลขสีทอง = ต่อยสวนกลับใส่หน้าเขา!", 2.4f);
            yield return Golden(to, 0);
            yield return bullets.ClockRing(to, 4, 24, 6.5f, 0.9f);
            if (Reflected < 3) yield return Golden(to, 1);
            if (Reflected < 3) yield return bullets.GearSpiral(to, 3.2f, 0.09f, 21f, 7.5f, 2);
            if (Reflected < 3) yield return Golden(to, 2);
            if (Reflected < 3) yield return bullets.HandRain(to, arenaRadius - 3f, 14, 0.9f, 0.16f);
            if (Reflected < 3) yield return bullets.TickWall(to, arenaRadius - 2f, 3, 6.5f, 3.4f);
            bool dazed = Reflected >= 3;
            bullets.ClearAll();
            yield return new WaitForSeconds(dazed ? 0.4f : 1.2f);
            yield return FadeTimeStop(0f, 0.8f);
            ResetArms();
            Phase = 2; _strikeDue = true; Wind = maxWind;
            UI.BossIntroCard.Show("COUNT MIDNIGHT", "— THE TWELFTH HOUR STRIKES —");
            SetMode(Mode.Fight);
            _nextAttack = Time.time + 1f;
            if (dazed) { UI.UIRoot.Get().Toast("สวนกลับครบ 3! — เขามึน รีบต่อยหน้าปัด!", 2f); Wind = 0f; Posture = maxPosture; StartCoroutine(Stagger()); }
        }

        /// <summary>A golden numeral flies at Pip (reflectable) — punch it back into his face.</summary>
        IEnumerator Golden(Vector3 from, int n)
        {
            Vector3 at = from + Vector3.up * 3.5f;
            Vector3 dir = (_player.transform.position + Vector3.up * 0.5f - at); dir.y = 0f; dir = dir.normalized;
            bullets.FireGolden(at - Vector3.up * 2.8f, dir * 6.5f, this);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, at, 0.6f);
            yield return new WaitForSeconds(1.4f);
        }

        /// <summary>A reflected golden numeral hit him.</summary>
        public void OnReflectedHit(Vector3 at)
        {
            Reflected++;
            HP = Mathf.Max(1f, HP - 45f);
            Flash();
            VFX.Play(VFXKind.StarBurst, at, 1.8f, VFX.Gold); VFX.Play(VFXKind.HitSpark, at, 1.6f);
            Presentation.ComicPop.Show(at, 2.2f, Reflected >= 3 ? "WHAM" : "POW");
            CameraShake.Add(0.4f); Audio.AudioManager.Play(Audio.Sfx.HitHeavy, at, 1f);
            UI.UIRoot.Get().Toast($"สวนกลับ {Reflected}/3", 1f);
        }

        IEnumerator FadeTimeStop(float target, float seconds)
        {
            if (timeStopVolume == null) yield break;
            float from = timeStopVolume.weight;
            for (float t = 0; t < seconds; t += Time.deltaTime) { timeStopVolume.weight = Mathf.Lerp(from, target, t / seconds); yield return null; }
            timeStopVolume.weight = target;
        }

        // ================================================================== hit shapes against Pip
        bool Hurt(string reason, Vector3 from)
        {
            if (_receiver == null) return false;
            bool landed = _receiver.TryHurt(reason, from, 8f);
            if (landed) PlayerHits++;
            return true;   // the swing "connected" (dodged or not) — one check per swing
        }

        void HitBox(Vector3 center, Vector3 fwd, Vector3 size, string reason)
        {
            Vector3 local = Quaternion.Inverse(Quaternion.LookRotation(fwd)) * (_player.transform.position + Vector3.up * 0.6f - center - Vector3.up * size.y * 0.5f);
            if (Mathf.Abs(local.x) < size.x * 0.5f + 0.35f && Mathf.Abs(local.y) < size.y * 0.5f + 0.6f && Mathf.Abs(local.z) < size.z * 0.5f + 0.35f) Hurt(reason, transform.position);
        }

        void Sector(Vector3 c, Vector3 fwd, float radius, float angle, float height, string reason)
        {
            Vector3 d = _player.transform.position - c;
            if (Flat(d).magnitude < radius + 0.35f && d.y < height && Vector3.Angle(fwd, Flat(d)) < angle * 0.5f) Hurt(reason, c);
        }

        IEnumerator Shockwave(Vector3 start, Vector3 dir, float length, float speed)
        {
            var wave = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(wave.GetComponent<Collider>()); wave.name = "Shockwave";
            wave.transform.localScale = new Vector3(3.2f, 0.5f, 0.6f);
            wave.transform.rotation = Quaternion.LookRotation(dir);
            var mat = Look.DreamLook.Toon(new Color(1f, 0.35f, 0.5f), null, 0.6f, name: "M_Shockwave"); mat.SetColor("_Emission", new Color(2f, 0.4f, 0.7f));
            wave.GetComponent<Renderer>().sharedMaterial = mat;
            bool hit = false;
            for (float s = 0; s < length; s += speed * Time.deltaTime)
            {
                Vector3 p = start + dir * s; wave.transform.position = p + Vector3.up * 0.25f;
                Vector3 d = _player.transform.position - p;
                Vector3 loc = Quaternion.Inverse(wave.transform.rotation) * Flat(d);
                if (!hit && Mathf.Abs(loc.x) < 1.9f && Mathf.Abs(loc.z) < 0.7f && d.y < 0.75f) hit = Hurt("the shockwave (jump it!)", p);
                yield return null;
            }
            Destroy(wave);
        }

        IEnumerator Swing(Transform arm, Quaternion to, float seconds)
        {
            Quaternion from = arm.localRotation;
            for (float t = 0; t < seconds; t += Time.deltaTime) { arm.localRotation = Quaternion.Slerp(from, to, t / seconds); yield return null; }
            arm.localRotation = to;
            Audio.AudioManager.Play(Audio.Sfx.Kick, transform.position, 0.8f);
        }

        void ResetArms()
        {
            armL.localRotation = Quaternion.Euler(-20f, 0, 15f);
            armR.localRotation = Quaternion.Euler(-20f, 0, -15f);
            model.localRotation = Quaternion.identity; model.localPosition = _modelBase;
        }

        // ================================================================== being hit
        public bool TakeHit(HitInfo hit)
        {
            if (Current == Mode.Waiting || Current == Mode.Intro || Current == Mode.TimeStop || Current == Mode.Defeated) return false;
            _lastHit = Time.time;
            bool atHead = head != null && _player != null && Flat(_player.transform.position - head.position).magnitude < 2.8f   // kneeling: any blow from the front reaches the dial
                          && Vector3.Dot(transform.forward, Flat(_player.transform.position - transform.position)) > 0f && Time.frameCount != _staggerFrame;
            if ((Current == Mode.Staggered || Current == Mode.Finisher) && atHead) { Critical(); return true; }
            // M19 winding-key armour: front punches clang off while he's wound (unless frozen or kneeling)
            if (Wound && Current != Mode.Staggered && Current != Mode.Finisher)
            {
                bool behind = PipBehind(8f);
                if (Current == Mode.Frozen) Unwind(unwindFrozen * (hit.heavy ? 2f : 1f), _key.position);
                else if (behind) Unwind(unwindBehind * (hit.heavy ? 2f : 1f), _key.position);
                else
                {
                    Clangs++;
                    VFX.Play(VFXKind.ImpactRing, hit.point, 0.6f, VFX.Cream);
                    Audio.AudioManager.Play(Audio.Sfx.Clank, hit.point, 0.8f);
                    if (Time.time - _clangHintAt > 6f) { _clangHintAt = Time.time; UI.UIRoot.Get().Toast("แกร๊ง! เกราะลาน — อ้อมไปต่อยกุญแจข้างหลัง / ต่อยเข็มที่ปักพื้น / ตีระฆัง", 2.2f); }
                    return false;
                }
                if (Current == Mode.Staggered) return true;   // the unwind ran him down this hit
            }
            float dmg = hit.damage * (Current == Mode.Staggered ? 1.5f : Current == Mode.Frozen ? 1.25f : 1f);
            HP = Mathf.Max(0f, HP - dmg);
            if (Current != Mode.Staggered) Posture += hit.posture * (Phase == 2 ? 0.85f : 1f);
            Flash();
            if (HP <= 0f) { StartFinisher(); return true; }
            if (Phase == 1 && HP <= maxHP * 0.5f && Current != Mode.TimeStop)
            {
                if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
                ResetArms(); SetGlow(0, 0);
                _attackCo = StartCoroutine(StopTheClock());
                return true;
            }
            if (Posture >= maxPosture && Current != Mode.Staggered) StartCoroutine(Stagger());
            return true;
        }

        IEnumerator Stagger()
        {
            if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
            CleanupStuck(); ClearFreezeLook(); _staggerFrame = Time.frameCount;
            SetGlow(0, 0); ResetArms();
            SetMode(Mode.Staggered);
            UI.UIRoot.Get().Toast("POSTURE BREAK! — ต่อยหน้าปัดนาฬิกา!", 2f);
            Audio.AudioManager.Play(Audio.Sfx.ChairTired, transform.position, 1f);
            yield return Kneel(true);
            for (float t = 0; t < staggerSeconds && Current == Mode.Staggered; t += Time.deltaTime) yield return null;
            if (Current == Mode.Staggered)
            {
                yield return Kneel(false);
                Posture = 0f;
                SetMode(Mode.Fight);
                if (!Wound) yield return Rewind();
                _nextAttack = Time.time + 0.6f;
            }
        }

        IEnumerator Kneel(bool down)
        {
            Vector3 a = model.localPosition, b = _modelBase + (down ? new Vector3(0, -2.6f, 0.4f) : Vector3.zero);
            Quaternion ra = model.localRotation, rb = down ? Quaternion.Euler(24f, 0, 0) : Quaternion.identity;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                float k = UI.UIStyle.OutCubic(t / 0.45f);
                model.localPosition = Vector3.Lerp(a, b, k); model.localRotation = Quaternion.Slerp(ra, rb, k);
                yield return null;
            }
            if (down) { CameraShake.Add(0.5f); VFX.Play(VFXKind.DustPuff, transform.position, 2.6f); Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f); }
        }

        void Critical()
        {
            VFX.Play(VFXKind.StarBurst, head.position, 2.2f, VFX.Gold);
            VFX.Play(VFXKind.Confetti, head.position, 1.4f);
            CameraShake.Add(0.8f);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, head.position, 1f);
            if (Current == Mode.Finisher) { StartCoroutine(Snooze()); return; }
            HP = Mathf.Max(1f, HP - criticalDamage);
            UI.UIRoot.Get().Toast("CRITICAL!  — DING!", 1.4f);
            Posture = 0f;
            StartCoroutine(EndStaggerSoon());
        }

        IEnumerator EndStaggerSoon()
        {
            SetMode(Mode.Fight);   // blocks the stagger loop
            yield return Kneel(false);
            if (!Wound) StartCoroutine(Rewind());
            if (Phase == 1 && HP <= maxHP * 0.5f) { _attackCo = StartCoroutine(StopTheClock()); yield break; }
            _nextAttack = Time.time + 1f;
        }

        void CheckHeadStomp()
        {
            if (head == null || _player == null) return;
            Vector3 d = _player.transform.position - head.position;
            if (Flat(d).magnitude < 1.5f && d.y > -0.4f && d.y < 1.6f && _player.VerticalVelocity <= 0.1f) Critical();
        }

        void StartFinisher()
        {
            if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
            bullets.ClearAll(); SetGlow(0, 0); ResetArms(); ClearPads(); CleanupStuck();
            SetMode(Mode.Finisher);
            StartCoroutine(Kneel(true));
            UI.BossIntroCard.Show("SNOOZE IT!", "— ต่อยหรือกระโดดลงบนหน้าปัด แล้วกดเลื่อนปลุก! —", chapterCard: true);
        }

        IEnumerator Snooze()
        {
            SetMode(Mode.Defeated);
            UI.UIRoot.Get().Toast("...snooze.   Count Midnight falls asleep.", 2.5f);
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                model.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, t / 1.6f);
                if (Mathf.Repeat(t, 0.4f) < Time.deltaTime) VFX.Play(VFXKind.Zzz, head.position + Vector3.up, 1.4f);
                yield return null;
            }
            Victory?.Invoke();
            UI.BossBarUI.Hide();
            if (Flow.GameFlowManager.Exists && Flow.GameFlowManager.Get().State == Flow.FlowState.Dream) Flow.GameFlowManager.Get().EndDream(true, "Snooze", null);
        }

        void Flash()
        {
            foreach (var m in _bodyMats) if (m != null) m.SetColor("_Emission", new Color(1.2f, 1.2f, 1.2f));
            _flashUntil = Time.time + 0.06f;
        }
        float _flashUntil;
        readonly List<Material> _bodyMats = new List<Material>();

        void Idle(float dt)
        {
            if (_flashUntil > 0f && Time.time > _flashUntil) { foreach (var m in _bodyMats) if (m != null) m.SetColor("_Emission", Color.black); _flashUntil = 0f; }
            // cape/pendulum sway
            var pend = model.Find("Pendulum");
            if (pend != null) pend.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.4f) * 22f);
        }

        void SetGlow(float hour, float minute)
        {
            if (_glowMats.Count < 2) return;
            _glowMats[0].SetColor("_Emission", new Color(2.5f, 0.2f, 0.35f) * hour);
            _glowMats[1].SetColor("_Emission", new Color(2.5f, 0.2f, 0.35f) * minute);
        }

        void AnimateClockFace()
        {
            if (_clockH == null) return;
            var now = DateTime.Now;
            _clockH.localRotation = Quaternion.Euler(0, 0, -((now.Hour % 12) + now.Minute / 60f) * 30f);
            _clockM.localRotation = Quaternion.Euler(0, 0, -(now.Minute + now.Second / 60f) * 6f);
        }

        /// <summary>Blender model (CountMidnightSetup): instance materials for hit flash + blade glow, add the colliders.</summary>
        void BindModel()
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "InkOutline") continue;
                var mats = r.materials;
                foreach (var m in mats) if (m != null && m.HasProperty("_Emission")) _bodyMats.Add(m);
                r.materials = mats;
            }
            foreach (var blade in new[] { bladeHour, bladeMinute })
            {
                Material glow = null;
                if (blade != null)
                    foreach (var r in blade.GetComponentsInChildren<Renderer>())
                        foreach (var m in r.sharedMaterials) if (m != null && m.name.StartsWith("M_CM_Blade")) { glow = m; break; }
                _glowMats.Add(glow != null ? glow : new Material(Shader.Find("DreamRush/DreamToon")));
            }
            _bodyMats.RemoveAll(m => _glowMats.Contains(m));
            _clockH = hourHand; _clockM = minuteHand;
            var col = gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 2.6f, 0); col.radius = 1.1f; col.height = 5.2f;
            if (head != null) { var hcol = head.gameObject.AddComponent<SphereCollider>(); hcol.radius = 1.0f; }
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;
            ResetArms();
        }

        // ================================================================== graybox body (6 m)
        void BuildGraybox()
        {
            Material M(string n, Color c, float rim = 0.3f) { var m = Look.DreamLook.Toon(c, null, rim, name: n); _bodyMats.Add(m); return m; }
            var ink = M("M_CM_Ink", new Color(0.12f, 0.1f, 0.22f));
            var gold = M("M_CM_Gold", new Color(1f, 0.78f, 0.3f), 0.4f);
            var face = M("M_CM_Face", new Color(1f, 0.96f, 0.86f), 0.2f);
            var cape = M("M_CM_Cape", new Color(0.25f, 0.16f, 0.55f), 0.5f);
            var red = M("M_CM_Red", new Color(0.95f, 0.15f, 0.3f), 0.3f);
            model = new GameObject("Model").transform; model.SetParent(transform, false);
            GameObject P(PrimitiveType t, string n, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
                if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
                g.GetComponent<Renderer>().sharedMaterial = m; return g;
            }
            P(PrimitiveType.Cube, "Case", model, new Vector3(0, 2.4f, 0), new Vector3(1.9f, 3.2f, 1.2f), ink);
            P(PrimitiveType.Cube, "CaseTrim", model, new Vector3(0, 2.4f, -0.62f), new Vector3(1.5f, 2.6f, 0.05f), gold);
            var pend = new GameObject("Pendulum").transform; pend.SetParent(model, false); pend.localPosition = new Vector3(0, 3.3f, -0.66f);
            P(PrimitiveType.Cube, "Rod", pend, new Vector3(0, -0.9f, 0), new Vector3(0.08f, 1.8f, 0.04f), gold);
            P(PrimitiveType.Cylinder, "Bob", pend, new Vector3(0, -1.9f, 0), new Vector3(0.6f, 0.04f, 0.6f), gold, new Vector3(90, 0, 0));
            for (int s = -1; s <= 1; s += 2) P(PrimitiveType.Cube, "Leg" + s, model, new Vector3(s * 0.55f, 0.45f, 0), new Vector3(0.5f, 0.9f, 0.6f), ink);
            P(PrimitiveType.Cube, "Shoulders", model, new Vector3(0, 4.05f, 0), new Vector3(2.8f, 0.45f, 1.2f), gold);
            // cape: two tall panels behind
            P(PrimitiveType.Cube, "Cape", model, new Vector3(0, 2.6f, 0.75f), new Vector3(2.9f, 3.7f, 0.12f), cape, new Vector3(-6, 0, 0));
            head = new GameObject("Head").transform; head.SetParent(model, false); head.localPosition = new Vector3(0, 5.0f, 0);
            P(PrimitiveType.Cylinder, "Dial", head, Vector3.zero, new Vector3(1.9f, 0.18f, 1.9f), face, new Vector3(90, 0, 0));
            P(PrimitiveType.Cylinder, "Rim", head, new Vector3(0, 0, 0.05f), new Vector3(2.1f, 0.15f, 2.1f), gold, new Vector3(90, 0, 0));
            P(PrimitiveType.Sphere, "Bell", head, new Vector3(0, 1.05f, 0), new Vector3(0.7f, 0.5f, 0.7f), gold);
            // mask eyes (phantom-thief domino) + real-time hands
            P(PrimitiveType.Cube, "Mask", head, new Vector3(0, 0.25f, -0.2f), new Vector3(1.5f, 0.32f, 0.06f), ink);
            for (int s = -1; s <= 1; s += 2) P(PrimitiveType.Sphere, "Eye" + s, head, new Vector3(s * 0.38f, 0.25f, -0.25f), new Vector3(0.24f, 0.14f, 0.05f), red);
            _clockH = new GameObject("HourHand").transform; _clockH.SetParent(head, false); _clockH.localPosition = new Vector3(0, 0, -0.14f);
            P(PrimitiveType.Cube, "H", _clockH, new Vector3(0, 0.2f, 0), new Vector3(0.08f, 0.42f, 0.02f), ink);
            _clockM = new GameObject("MinuteHand").transform; _clockM.SetParent(head, false); _clockM.localPosition = new Vector3(0, 0, -0.15f);
            P(PrimitiveType.Cube, "M", _clockM, new Vector3(0, 0.32f, 0), new Vector3(0.05f, 0.66f, 0.02f), ink);
            // arms + blades
            armL = new GameObject("Arm_L").transform; armL.SetParent(model, false); armL.localPosition = new Vector3(-1.45f, 4.0f, 0);
            armR = new GameObject("Arm_R").transform; armR.SetParent(model, false); armR.localPosition = new Vector3(1.45f, 4.0f, 0);
            foreach (var arm in new[] { armL, armR }) P(PrimitiveType.Cube, "Upper", arm, new Vector3(0, -0.9f, 0), new Vector3(0.45f, 1.8f, 0.45f), ink);
            var hourMat = new Material(gold) { name = "M_CM_HourBlade" }; var minMat = new Material(gold) { name = "M_CM_MinuteBlade" };
            _glowMats.Add(hourMat); _glowMats.Add(minMat);
            bladeHour = P(PrimitiveType.Cube, "HourBlade", armR, new Vector3(0, -1.9f, -2.0f), new Vector3(0.5f, 0.12f, 4.2f), hourMat).transform;
            P(PrimitiveType.Cube, "HourTip", bladeHour, new Vector3(0, 0, -0.55f), new Vector3(2.2f, 1f, 0.12f), hourMat, new Vector3(0, 45, 0));
            bladeMinute = P(PrimitiveType.Cube, "MinuteBlade", armL, new Vector3(0, -1.9f, -2.7f), new Vector3(0.28f, 0.1f, 5.6f), minMat).transform;
            ResetArms();
            // body collider (Pip can't walk through; leaf hits land on it)
            var col = gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 2.6f, 0); col.radius = 1.1f; col.height = 5.2f;
            var hcol = head.gameObject.AddComponent<SphereCollider>(); hcol.radius = 0.95f;   // stand on the dial when he kneels
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;
            Look.DreamLook.AddOutline(model.gameObject, 2.2f);
        }
    }
}
