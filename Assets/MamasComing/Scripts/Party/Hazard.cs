using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ====================================================================== POWER
    /// House electricity. Tripped by water on sockets, overloads, a hamster chewing cables...
    public static class PowerGrid
    {
        public static bool On { get; private set; } = true;
        public static string CauseEn, CauseTh;
        public static float OffSince;
        public static event System.Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { On = true; Changed = null; }

        public static void Trip(string en, string th)
        {
            if (!On) return;
            On = false; CauseEn = en; CauseTh = th; OffSince = Time.time;
            MamaAudio.Play("zap", null, 0.9f); MamaAudio.Play("thud", null, 0.6f, 0.6f);
            Changed?.Invoke(false);
        }

        public static void Restore()
        {
            if (On) return;
            On = true; MamaAudio.Play("click", null, 0.9f, 0.7f);
            Changed?.Invoke(true);
        }

        /// Force state silently (network clients mirror the host).
        public static void Set(bool on) { if (On == on) return; On = on; if (!on) OffSince = Time.time; Changed?.Invoke(on); }
    }


    // ====================================================================== HAZARD (one quest)
    /// A household problem that gets worse over time and spills into other problems when ignored.
    /// Goal (questEn) -> obstacle (minigame / tool / co-op) -> outcome (fixed, or failure consequence).
    public abstract class Hazard : Interactable, IMess
    {
        public static readonly List<Hazard> All = new List<Hazard>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() { All.Clear(); }

        [Header("Quest")]
        public string questEn = "Fix it", questTh = "แก้ไข";
        public string startEn, startTh;     // shown as a phone-style call when it begins
        public string failEn, failTh;       // shown when it escalates into its consequence
        public string doneEn = "Fixed!", doneTh = "เรียบร้อย!";
        public string momEn = "What happened here?!", momTh = "เกิดอะไรขึ้นตรงนี้?!";
        public string momFailEn, momFailTh;
        [Tooltip("Seconds after the round starts that this problem begins (-1 = only through a chain reaction).")]
        public float startAt = -1f;
        [Tooltip("Seconds from 'started' to 'escalates into the consequence'.")]
        public float escalateTime = 90f;
        public bool needsPower;
        public bool coop;
        public int severity = 2;
        public bool startActive;
        public MiniGame.Kind puzzle = MiniGame.Kind.None;
        public string puzzleEn, puzzleTh;
        public GameObject[] showWhenActive, showWhenFixed, showWhenFailed;
        public string loopClip; public float loopVolume = 0.35f;

        public bool Active { get; protected set; }
        public bool Fixed { get; protected set; }
        public bool Failed { get; protected set; }
        public float Progress { get; protected set; }
        public float ActiveTime { get; protected set; }
        public bool Urgent => Active && !Fixed;
        protected AudioSource loop;

        protected static string T(string en, string th) => Loc.Thai ? th : en;
        protected bool Sim => (GameManager.I == null || GameManager.I.SimRunning) && NetGame.IsAuthority;

        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); MessRegistry.Remove(this); }

        protected virtual void Start()
        {
            if (!string.IsNullOrEmpty(loopClip))
            {
                loop = gameObject.AddComponent<AudioSource>();
                loop.clip = MamaAudio.Clip(loopClip); loop.loop = true; loop.spatialBlend = 0.85f; loop.rolloffMode = AudioRolloffMode.Linear;
                loop.minDistance = 2f; loop.maxDistance = 16f; loop.volume = 0f; loop.dopplerLevel = 0f;
                if (loop.clip) loop.Play();
            }
            if (startActive) { Active = true; OnActivated(); }
            RefreshVisuals();
        }

        public string Quest => T(questEn, questTh);
        public virtual bool Powered => !needsPower || PowerGrid.On;

        public void Activate(string whyEn = null, string whyTh = null)
        {
            if (Active || Fixed) return;
            Active = true; Progress = 0f; ActiveTime = 0f;
            OnActivated();
            RefreshVisuals();
            string en = whyEn ?? startEn, th = whyTh ?? startTh;
            if (!string.IsNullOrEmpty(en) && QuestDirector.I) QuestDirector.I.Alert(this, en, th);
        }

        /// For problems that can come back (power outage).
        public void Rearm() { Fixed = false; Failed = false; Active = false; Progress = 0f; RefreshVisuals(); }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            bool running = Urgent && Powered;
            if (loop) loop.volume = Mathf.MoveTowards(loop.volume, running ? loopVolume * MamaSettings.SfxVol : 0f, dt * 1.5f);
            if (!Urgent || !Sim) { VisualTick(dt, running); return; }
            ActiveTime += dt;
            if (Powered)
            {
                float pace = QuestDirector.I ? QuestDirector.I.Pace : 1f;
                Progress = Mathf.Min(1f, Progress + dt / Mathf.Max(1f, escalateTime) * pace * ProgressMul);
                Tick(dt);
            }
            if (Progress >= 1f && !Failed)
            {
                Failed = true;
                OnFailed();
                RefreshVisuals();
                if (!string.IsNullOrEmpty(failEn) && QuestDirector.I) QuestDirector.I.Alarm(this, failEn, failTh);
            }
            VisualTick(dt, running);
        }

        protected virtual float ProgressMul => 1f;
        protected virtual void Tick(float dt) { }
        protected virtual void VisualTick(float dt, bool running) { }
        protected virtual void OnActivated() { }
        protected virtual void OnFailed() { }
        protected virtual void OnFixed(PlayerController p) { }

        public void Fix(PlayerController p)
        {
            if (Fixed || !Active) return;
            Fixed = true;
            OnFixed(p);
            RefreshVisuals();
            if (QuestDirector.I) QuestDirector.I.OnFixed(this, p);
        }

        protected void RefreshVisuals()
        {
            if (showWhenActive != null) foreach (var g in showWhenActive) if (g) g.SetActive(Active && !Fixed);
            if (showWhenFixed != null) foreach (var g in showWhenFixed) if (g) g.SetActive(Fixed);
            if (showWhenFailed != null) foreach (var g in showWhenFailed) if (g) g.SetActive(Failed);
        }

        // ---------------------------------------------------------------- Interactable
        public override bool CanInteract(PlayerController p) => Urgent;
        public override string GetPrompt(PlayerController p) => Quest;
        public override MiniGame.Kind MiniGameKind(PlayerController p) => puzzle;
        public override string MiniGameTitle => T(puzzleEn ?? questEn, puzzleTh ?? questTh);
        public override void Interact(PlayerController p) => Fix(p);

        // ---------------------------------------------------------------- IMess (what Mom can notice)
        public virtual MessKind Kind => MessKind.Hazard;
        public virtual bool IsProblem => Active && (!Fixed || Failed);
        public virtual bool IsHidden => false;
        public virtual int Severity => Failed ? severity + 1 : severity;
        public virtual string MomLine => Failed && !string.IsNullOrEmpty(momFailEn) ? T(momFailEn, momFailTh) : T(momEn, momTh);
        public string Label => Quest;
        public Vector3 Position => InteractPoint;

        // ---------------------------------------------------------------- network state
        public virtual void WriteState(System.IO.BinaryWriter w) { w.Write(Active); w.Write(Fixed); w.Write(Failed); w.Write(Progress); }
        public virtual void ReadState(System.IO.BinaryReader r)
        {
            bool a = r.ReadBoolean(), f = r.ReadBoolean(), fl = r.ReadBoolean(); Progress = r.ReadSingle();
            bool changed = a != Active || f != Fixed || fl != Failed;
            if (a && !Active) OnActivated();
            if (fl && !Failed) OnFailed();
            if (f && !Fixed) OnFixed(null);
            Active = a; Fixed = f; Failed = fl;
            if (changed) RefreshVisuals();
        }
    }

    // ====================================================================== SPLATS (frosting / confetti / soap foam)
    public enum SplatKind { Frosting, Confetti, Foam, Soda }

}
