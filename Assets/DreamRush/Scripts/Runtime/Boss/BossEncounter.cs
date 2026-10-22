using System;
using System.Collections;
using DreamRush.CameraSystem;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using Unity.AI.Navigation;
using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>
    /// Runs the chair encounter: builds the arena NavMesh, the reveal (pitch: the chair stubs your pinky toe and
    /// bolts), seating, boss HUD/hints, Sleep Stability pushback, and victory/defeat hand-off to the game flow.
    /// </summary>
    public class BossEncounter : MonoBehaviour
    {
        public enum Phase { Waiting, Reveal, Fight, Victory, Lost }

        public BossChairController chair;
        public NavMeshSurface arenaSurface;
        public PlayerMotor3D player;
        [Tooltip("Player z beyond which the reveal starts (arena entrance).")]
        public float revealTriggerZ = 71.5f;
        public bool skipReveal;
        public float firstHintAfter = 60f, secondHintAfter = 110f;

        public Phase CurrentPhase { get; private set; } = Phase.Waiting;
        public string VictoryMethod { get; private set; }
        public event Action<Phase> PhaseChanged;

        float _fightTime, _lastProgressTime;
        int _hintsShown;
        bool _sitAttached;
        PlayerUIBridge _bridge;
        SleepStability _stability;

        IEnumerator Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMotor3D>();
            if (chair == null) chair = FindFirstObjectByType<BossChairController>();
            _bridge = player != null ? player.GetComponent<PlayerUIBridge>() : null;
            if (arenaSurface != null) arenaSurface.BuildNavMesh();
            yield return null;
            int seed = GameFlowManager.Exists && GameFlowManager.Get().CurrentRun != null ? GameFlowManager.Get().CurrentRun.Seed : Environment.TickCount;
            chair.player = player;
            chair.Initialize(seed);
            chair.SetDormant();
            chair.Defeated += OnChairDefeated;
            chair.StateChanged += OnChairState;
            _stability = SleepStability.Current;
            if (_stability != null)
            {
                _stability.Damaged += OnPlayerDamaged;
                _stability.DepletedEvent += OnStabilityDepleted;
            }
            if (skipReveal) StartFight();
        }

        void OnDestroy()
        {
            if (chair != null) { chair.Defeated -= OnChairDefeated; chair.StateChanged -= OnChairState; }
            if (_stability != null) { _stability.Damaged -= OnPlayerDamaged; _stability.DepletedEvent -= OnStabilityDepleted; }
        }

        void SetPhase(Phase p) { CurrentPhase = p; PhaseChanged?.Invoke(p); }

        void Update()
        {
            if (player == null || chair == null) return;
            switch (CurrentPhase)
            {
                case Phase.Waiting:
                    if (player.transform.position.z > revealTriggerZ && player.IsGrounded) StartCoroutine(Reveal());
                    break;
                case Phase.Fight:
                    _fightTime += Time.deltaTime;
                    UpdateHints();
                    UpdateSitting();
                    break;
            }
        }

        // ---------------------------------------------------------------- reveal (§13.7 + pitch)
        IEnumerator Reveal()
        {
            SetPhase(Phase.Reveal);
            var stage = FindFirstObjectByType<StageCameraController>();
            if (_bridge != null) _bridge.CutsceneLock = true;
            player.StopImmediately();
            if (stage != null) stage.secondaryTarget = chair.transform;
            var tpc = FindFirstObjectByType<ThirdPersonCameraController>();
            if (tpc != null) tpc.focusTarget = chair.transform;
            chair.BeginReveal();
            DreamHUD.Current?.SetObjective("...?");

            // the chair turns. eyes widen. a musical pause.
            float t = 0f;
            while (t < 1.0f)
            {
                t += Time.deltaTime;
                Vector3 to = player.transform.position - chair.transform.position; to.y = 0;
                chair.transform.rotation = Quaternion.RotateTowards(chair.transform.rotation, Quaternion.LookRotation(to), 360f * Time.deltaTime);
                yield return null;
            }
            // dash at the player... and stub the pinky toe
            chair.Locomotion.TargetSpeed = 9f;
            chair.Locomotion.SetDirectTarget(player.transform.position);
            t = 0f;
            while (t < 1.6f && Vector3.Distance(Flat(chair.transform.position), Flat(player.transform.position)) > 1.2f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            chair.Locomotion.Stop(true);
            Vector3 away = Flat(player.transform.position - chair.transform.position).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            player.Launch(away * 4.5f + Vector3.up * 6f);
            UIRoot.Get().Toast("OUCH!! My pinky toe!!", 2.2f);
            var fxl = Presentation.FXLibrary.Instance;
            if (fxl != null) Presentation.WorldFX.Spawn(fxl.ouch, player.transform.position + Vector3.up * 1.7f, 1.3f, 1.1f, player.transform);
            Presentation.CameraShake.Add(0.55f);
            player.GetComponentInChildren<AppleFace>()?.Squeeze(0.9f);
            Audio.AudioManager.Play(Audio.Sfx.Kick, player.transform.position);
            Presentation.VFX.Play(Presentation.VFXKind.StarBurst, player.transform.position + Vector3.up * 1.0f, 1.2f, Presentation.VFX.Danger);
            yield return new WaitForSeconds(0.9f);

            BossIntroCard.Show("THE CHAIR", "THRONE OF REFUSAL");
            StartFight();
        }

        void StartFight()
        {
            if (_bridge != null) _bridge.CutsceneLock = false;
            var stage = FindFirstObjectByType<StageCameraController>();
            if (stage != null) stage.secondaryTarget = chair.transform;
            var tpc = FindFirstObjectByType<ThirdPersonCameraController>();
            if (tpc != null) tpc.focusTarget = chair.transform;
            DreamHUD.Current?.SetObjective("SIT ON THE CHAIR");
            DreamHUD.Current?.ShowBoss("THE CHAIR");
            chair.Activate();
            _fightTime = 0f;
            _lastProgressTime = 0f;
            SetPhase(Phase.Fight);
            Audio.AudioManager.PlayMusic(Audio.Music.Boss, 0.6f);
            var save = Core.SaveManager.GetOrCreate();
            if (!save.Data.diary.bossesMet.Contains("Chair")) { save.Data.diary.bossesMet.Add("Chair"); save.Save(); }
        }

        // ---------------------------------------------------------------- seating
        public bool TrySit(PlayerMotor3D p)
        {
            if (CurrentPhase != Phase.Fight) return false;
            if (!chair.BeginSitting(p)) return false;
            AttachToSeat(true);
            return true;
        }

        void AttachToSeat(bool on)
        {
            _sitAttached = on;
            var anim = player.GetComponentInChildren<AppleProceduralAnimator>();
            if (anim != null) anim.sitting = on;
            if (on)
            {
                player.AutoStep = false;
                player.StopImmediately();
                player.Controller.enabled = false;
            }
            else
            {
                player.Controller.enabled = true;
                player.AutoStep = true;
            }
        }

        void UpdateSitting()
        {
            if (_sitAttached)
            {
                if (chair.State == ChairState.Sitting || chair.State == ChairState.Defeated)
                {
                    var seat = chair.SeatPoint;
                    player.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(0, chair.transform.eulerAngles.y, 0)); // face away from the backrest
                    DreamHUD.Current?.SetSitProgress(chair.SitSeconds, chair.config.sitDuration);
                }
                else
                {
                    // bucked off (jumped): hop away from the seat
                    AttachToSeat(false);
                    player.Teleport(chair.SeatPoint.position + chair.transform.forward * 0.9f, player.transform.rotation);
                    player.Launch(chair.transform.forward * 3f + Vector3.up * 7f);
                    DreamHUD.Current?.SetSitProgress(-1f, 1f);
                }
            }
        }

        void OnChairState(ChairState from, ChairState to)
        {
            if (to == ChairState.Exhausted || to == ChairState.Stumble || to == ChairState.Rooted || to == ChairState.Frozen || to == ChairState.OfferSeat)
                _lastProgressTime = _fightTime;
        }

        void OnChairDefeated(string method)
        {
            if (CurrentPhase != Phase.Fight) return; // victory triggers only once
            VictoryMethod = method;
            SetPhase(Phase.Victory);
            Audio.AudioManager.PlayMusic(Audio.Music.None, 0.8f); // the victory sting plays from the chair (Defeated)
            StartCoroutine(VictoryRoutine(method));
        }

        IEnumerator VictoryRoutine(string method)
        {
            DreamHUD.Current?.SetSitProgress(-1f, 1f);
            UIRoot.Get().Toast("...creak.   The chair gives up.", 2f);
            yield return new WaitForSeconds(1.6f);
            if (GameFlowManager.Exists) GameFlowManager.Get().EndDream(true, method, null);
        }

        // ---------------------------------------------------------------- stability & pushback (pitch)
        void OnPlayerDamaged(string reason)
        {
            if (CurrentPhase == Phase.Fight) chair.Fatigue.Refresh(0.3f); // the chair gets its breath back
            UIRoot.Get().Toast("Sleep wobbles... (" + reason + ")", 2f);
        }

        void OnStabilityDepleted()
        {
            if (CurrentPhase == Phase.Victory || CurrentPhase == Phase.Lost) return;
            SetPhase(Phase.Lost);
            if (GameFlowManager.Exists) GameFlowManager.Get().EndDream(false, null, "Sleep Stability ran out");
        }

        // ---------------------------------------------------------------- hints (§23.5)
        void UpdateHints()
        {
            float since = _fightTime - _lastProgressTime;
            if (_hintsShown == 0 && since > firstHintAfter) { _hintsShown = 1; UIRoot.Get().Toast("The chair seems tired after running.", 4f); }
            else if (_hintsShown == 1 && since > secondHintAfter) { _hintsShown = 2; UIRoot.Get().Toast("Maybe you can catch it when it stops.", 4f); }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
