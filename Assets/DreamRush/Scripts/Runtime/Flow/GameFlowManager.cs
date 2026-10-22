using System;

using System.Collections;

using DreamRush.Core;

using DreamRush.Interaction;

using DreamRush.Player;

using DreamRush.UI;

using UnityEngine;

using UnityEngine.SceneManagement;



namespace DreamRush.Flow

{

    public enum FlowState { Boot, Hub, FallingAsleep, LevelSelect, CardDraw, Dream, Victory, WakingUp }



    /// <summary>Per-run state. Created when falling asleep, discarded when back in the bedroom (no leaks).</summary>

    public class RunState

    {

        public int Seed;

        public float StartTime;

        public string ChosenCardId;

        public string VictoryId;

        public bool Won;

        public string EndReason;

        public int DreamMemoryEarned;

        public float ElapsedSeconds;

        public int DreamStars;

        public bool NewPersonalBest;

        /// <summary>Card rerolls left this run (Lucky Pillow).</summary>

        public int RerollsLeft;

        /// <summary>Level chosen in the Dream Galaxy (null = default dream scene).</summary>

        public string LevelScene;

        public RunState(int seed) { Seed = seed; StartTime = Time.unscaledTime; }

    }



    /// <summary>

    /// Owns the Hub → Sleep → (Cards) → Dream → Victory/Wake → Hub loop (GDD §13.2, §22).

    /// Persistent singleton; scenes declare what they are with <see cref="SceneContext"/>.

    /// </summary>

    public class GameFlowManager : MonoBehaviour

    {

        public const string HubScene = "BedroomHub";

        public const string DreamScene = "Dream_Chair";

        public const string GalaxyScene = "DreamGalaxy";



        static GameFlowManager _instance;

        public static bool Exists => _instance != null;



        public static GameFlowManager Get()

        {

            if (_instance != null) return _instance;

            var go = new GameObject("GameFlowManager");

            DontDestroyOnLoad(go);

            _instance = go.AddComponent<GameFlowManager>();

            return _instance;

        }



        public FlowState State { get; private set; } = FlowState.Boot;

        public RunState CurrentRun { get; private set; }

        public RunState LastRun { get; private set; }

        public bool ReturningFromDream { get; private set; }

        public event Action<FlowState> StateChanged;

        /// <summary>Hook for the card draw (M6). Return a coroutine that finishes when a card is chosen.</summary>

        public Func<RunState, IEnumerator> CardDrawStep;

        /// <summary>Hook for the Dream Galaxy level select (sleep → galaxy → cards → level). Null skips it.</summary>

        public Func<RunState, IEnumerator> LevelSelectStep;

        /// <summary>Speeds up sequences for automated tests.</summary>

        public float SequenceSpeed = 1f;



        void Awake()

        {

            CardDrawStep = UI.CardSelectPanel.Run; // GDD §22.1 step 9

            LevelSelectStep = GalaxyStep;

        }



        /// <summary>Loads the Dream Galaxy, waits for a world, covers the screen with its name.</summary>

        IEnumerator GalaxyStep(RunState run)

        {

            if (!Application.CanStreamedLevelBeLoaded(GalaxyScene)) yield break;

            SetState(FlowState.LevelSelect);

            yield return SceneManager.LoadSceneAsync(GalaxyScene);

            yield return null;

            yield return TransitionOverlay.Get().Open(W(0.8f));

            GalaxySelect sel = null;

            while (sel == null) { sel = GalaxySelect.Current; yield return null; }

            while (!sel.Done) yield return null;

            run.LevelScene = sel.ChosenScene;

            string title = sel.Selected != null ? sel.Selected.title : "";

            yield return new WaitForSecondsRealtime(W(0.6f));

            yield return TransitionOverlay.Get().Close(W(0.8f), title);

            yield return new WaitForSecondsRealtime(W(0.5f));

            SetState(FlowState.CardDraw);

        }



        void Update()

        {

            // Real play time, excluding pause. Slow-motion effects do not shorten a record.

            if (State == FlowState.Dream && CurrentRun != null && Time.timeScale > 0f)

                CurrentRun.ElapsedSeconds += Time.unscaledDeltaTime;
            // Every active dream must wake at zero stability, including worlds without BossEncounter.
            if(State==FlowState.Dream&&Time.timeScale>0f&&Dream.SleepStability.Current!=null&&Dream.SleepStability.Current.Depleted)
                EndDream(false,null,"Sleep Stability ran out");

        }



        public void SetState(FlowState s)

        {

            if (State == s) return;

            State = s;

            StateChanged?.Invoke(s);

        }



        /// <summary>Called by <see cref="SceneContext"/> when a scene starts directly (e.g. pressing Play in it).</summary>

        public void NotifySceneStarted(SceneKind kind)

        {

            if (kind == SceneKind.Hub && State != FlowState.FallingAsleep) SetState(FlowState.Hub);

            if (kind == SceneKind.Dream && State != FlowState.CardDraw)

            {

                CurrentRun ??= new RunState(Environment.TickCount);

                SetState(FlowState.Dream);

            }

        }



        float W(float seconds) => seconds / Mathf.Max(0.01f, SequenceSpeed);



        // ------------------------------------------------------------------ sleep

        public bool BeginSleep(BedInteractable bed, PlayerMotor3D player)

        {

            if (State != FlowState.Hub) return false;

            StartCoroutine(SleepRoutine(bed, player));

            return true;

        }



        IEnumerator SleepRoutine(BedInteractable bed, PlayerMotor3D player)

        {

            SetState(FlowState.FallingAsleep);

            var bridge = player != null ? player.GetComponent<PlayerUIBridge>() : null;

            if (bridge != null) bridge.CutsceneLock = true;

            var cam = FindFirstObjectByType<CameraSystem.ThirdPersonCameraController>();



            // 1–4: climb onto the bed and lie down

            if (player != null && bed != null && bed.sleepPose != null)

                yield return PlayerCutscene.HopAndLieDown(player, bed.sleepPose, W(0.55f), W(0.35f));

            // 5–7: eyes close, room quiets, camera to the pillow

            Audio.AudioManager.Play(Audio.Sfx.SleepChime, Vector3.zero);

            Audio.AudioManager.PlayMusic(Audio.Music.None, 2f);

            if (cam != null && bed != null && bed.pillowCamera != null) cam.SetOverride(bed.pillowCamera);

            float vol = AudioListener.volume;

            for (float t = 0; t < W(1.1f); t += Time.unscaledDeltaTime)

            {

                AudioListener.volume = Mathf.Lerp(vol, vol * 0.3f, t / W(1.1f));

                yield return null;

            }

            // 8: the snot bubble pops (comedy beat) and the bedroom peels away like paper pages

            if (player != null) player.GetComponentInChildren<AppleProceduralAnimator>()?.PopBubble();

            yield return TransitionOverlay.Get().Close(W(0.9f), "z z z");

            AudioListener.volume = vol;



            var save = SaveManager.GetOrCreate();

            save.Data.diary.runsStarted++;

            save.Save();

            CurrentRun = new RunState(Environment.TickCount) { RerollsLeft = Progression.BedUpgrades.RerollsPerRun(save.Data.progression) };



            // 8b: Dream Galaxy — pick a world (after sleeping, before the cards)

            if (LevelSelectStep != null) yield return LevelSelectStep(CurrentRun);



            // 9: card selection (M6 hook)

            SetState(FlowState.CardDraw);

            if (CardDrawStep != null) yield return CardDrawStep(CurrentRun);



            // 10: dream level begins

            ReturningFromDream = false;

            yield return SceneManager.LoadSceneAsync(string.IsNullOrEmpty(CurrentRun.LevelScene) ? DreamScene : CurrentRun.LevelScene);

            yield return null;

            SetState(FlowState.Dream);

            yield return TransitionOverlay.Get().Open(W(0.9f));

        }



        // ------------------------------------------------------------------ end of dream

        /// <summary>Victory (sweet dreams) or defeat/give-up (mom wakes you). Safe to call once per run.</summary>

        public bool EndDream(bool victory, string victoryId, string reason)

        {

            if (State != FlowState.Dream) return false;

            StartCoroutine(EndRoutine(victory, victoryId, reason));

            return true;

        }



        IEnumerator EndRoutine(bool victory, string victoryId, string reason)

        {

            SetState(victory ? FlowState.Victory : FlowState.WakingUp);

            var run = CurrentRun ?? new RunState(0);

            run.Won = victory;

            run.VictoryId = victoryId;

            run.EndReason = reason;

            run.LevelScene = SceneManager.GetActiveScene().name;

            run.DreamStars = World1.DreamStar.TotalInLevel > 0 ? World1.DreamStar.CollectedThisRun : 0;



            var save = SaveManager.GetOrCreate();

            if (victory)

            {

                save.Data.diary.runsWon++;

                run.DreamMemoryEarned = 10;

                run.NewPersonalBest = save.Data.progression.RecordClear(run.LevelScene, run.ElapsedSeconds, run.DreamStars);

                if (save.Data.diary.Discover(victoryId)) run.DreamMemoryEarned += 5; // reward creative discovery

            }

            else

            {

                save.Data.diary.runsLost++;

                run.DreamMemoryEarned = 2;

            }

            string bossId = Campaign.CampaignRoute.BossId(run.LevelScene);

            if (!save.Data.diary.bossesMet.Contains(bossId)) save.Data.diary.bossesMet.Add(bossId);

            save.Data.progression.dreamMemory += run.DreamMemoryEarned;

            save.Save();



            var ui = UIRoot.Get();

            ui.CloseAll();
            if(victory&&(run.LevelScene=="Dream_World1"||run.LevelScene=="Dream_MidnightPalace"))yield return Campaign.CampaignChapterCinema.Victory(run.LevelScene);

            if (victory) ui.Toast("SWEET DREAMS", 2.5f);

            else { ui.Toast("\"Pip! Wake up, sweetie — it's morning!\"", 3f); Audio.AudioManager.Play(Audio.Sfx.WakeUp, Vector3.zero); }

            yield return new WaitForSecondsRealtime(W(victory ? 1.4f : 1.2f));

            yield return TransitionOverlay.Get().Close(W(0.9f), victory ? "SWEET DREAMS" : "...!");



            if (victory && run.LevelScene == "Dream_MidnightTeacup") yield return Campaign.DreamEnding.Play();

            Time.timeScale = 1f;

            LastRun = run;

            CurrentRun = null;

            ReturningFromDream = true;

            yield return SceneManager.LoadSceneAsync(HubScene);

            yield return null;

            SetState(FlowState.Hub);

            yield return TransitionOverlay.Get().Open(W(0.9f));

            RunSummaryPanel.Show(run);

        }



        /// <summary>For tests: forget everything and return to boot.</summary>

        public void ResetForTests()

        {

            StopAllCoroutines();

            CurrentRun = null; LastRun = null; ReturningFromDream = false;

            CardDrawStep = null;

            LevelSelectStep = null;

            SetState(FlowState.Boot);

        }

    }

}

