using DreamRush.CameraSystem;
using DreamRush.Flow;
using DreamRush.Interaction;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.UI
{
    /// <summary>
    /// Connects the player to the UI: interaction prompt, Pause/Cancel, and blocking gameplay input + camera
    /// while any menu is open (GDD §8.3: movement disabled, camera returns on close).
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerUIBridge : MonoBehaviour
    {
        PlayerInputReader _input;
        InteractionSystem _interaction;
        ThirdPersonCameraController _cam;
        StageCameraController _stageCam;
        UIRoot _ui;
        float _timeScaleBeforePause = 1f;
        bool _paused;

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _interaction = GetComponent<InteractionSystem>();
        }

        void Start()
        {
            _ui = UIRoot.Get();
            _cam = FindFirstObjectByType<ThirdPersonCameraController>();
            _stageCam = FindFirstObjectByType<StageCameraController>();
            _ui.MenuStateChanged += OnMenuState;
            if (_interaction != null) _interaction.CurrentChanged += OnCurrentChanged;
            SettingsApplier.Apply(Core.SaveManager.GetOrCreate().Data.settings);
            OnMenuState(_ui.AnyPanelOpen);
        }

        void OnDestroy()
        {
            if (_ui != null) _ui.MenuStateChanged -= OnMenuState;
            if (_interaction != null) _interaction.CurrentChanged -= OnCurrentChanged;
            if (_paused) Time.timeScale = _timeScaleBeforePause;
        }

        void OnCurrentChanged(IInteractable it)
        {
            if (_ui == null) return;
            if (it == null || CutsceneLock) _ui.HidePrompt();
            else _ui.ShowPrompt("[E]  " + it.Prompt);
        }

        /// <summary>Set by flow sequences (sleeping, waking) to keep the player still without a menu.</summary>
        public bool CutsceneLock
        {
            get => _cutscene;
            set { _cutscene = value; Refresh(); }
        }
        bool _cutscene;

        void OnMenuState(bool open)
        {
            if (!open && _paused)
            {
                Time.timeScale = _timeScaleBeforePause;
                _paused = false;
            }
            Refresh();
        }

        void Refresh()
        {
            bool menu = _ui != null && _ui.AnyPanelOpen;
            bool block = menu || _cutscene;
            _input.GameplayBlocked = block;
            if (_interaction != null) _interaction.Active = !block;
            if (_cam != null)
            {
                _cam.InputEnabled = !block;
                if (menu) _cam.ReleaseCursor();
            }
            if (_stageCam != null)
            {
                _stageCam.InputEnabled = !block;
                if (menu) _stageCam.ReleaseCursor();
            }
            if (block && _ui != null) _ui.HidePrompt();
            else if (_interaction != null) OnCurrentChanged(_interaction.Current);
        }

        void Update()
        {
            if (_ui == null || !_input.PausePressed) return;
            if (_ui.AnyPanelOpen)
            {
                if (_ui.TopPanel.CloseOnCancel) _ui.TopPanel.Close();
                return;
            }
            if (_cutscene) return;
            var flow = GameFlowManager.Get();
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            _paused = true;
            PauseMenuPanel.Show(flow.State == FlowState.Dream, () => flow.EndDream(false, null, "gave up"));
        }
    }
}
