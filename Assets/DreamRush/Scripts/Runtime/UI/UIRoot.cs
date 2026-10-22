using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Persistent UI root: canvas layers (HUD / Panels / Overlay), EventSystem, interaction prompt, toasts and
    /// a panel stack. Created on demand (<see cref="Get"/>), survives scene loads.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        static UIRoot _instance;
        public static bool Exists => _instance != null;

        public static UIRoot Get()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("UIRoot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UIRoot>();
            _instance.Build();
            return _instance;
        }

        public Canvas Canvas { get; private set; }
        public RectTransform Hud { get; private set; }
        public RectTransform Panels { get; private set; }
        public RectTransform Overlay { get; private set; }

        readonly List<ModalPanel> _stack = new List<ModalPanel>();
        public bool AnyPanelOpen => _stack.Count > 0;
        public ModalPanel TopPanel => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public event Action<bool> MenuStateChanged;

        RectTransform _prompt;
        Text _promptText;
        RectTransform _toast;
        Text _toastText;
        float _toastUntil;

        void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Hud = UIFactory.Stretch(UIFactory.Rect("HUD", canvasGo.transform));
            Panels = UIFactory.Stretch(UIFactory.Rect("Panels", canvasGo.transform));
            Overlay = UIFactory.Stretch(UIFactory.Rect("Overlay", canvasGo.transform));

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform, false);
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            BuildPrompt();
            BuildToast();
        }

        void BuildPrompt()
        {
            var img = UIFactory.Panel("InteractionPrompt", Hud, UIStyle.Plum, UIStyle.Slab);
            _prompt = img.rectTransform;
            _prompt.anchorMin = _prompt.anchorMax = new Vector2(0.5f, 0f);
            _prompt.pivot = new Vector2(0.5f, 0f);
            _prompt.anchoredPosition = new Vector2(0, 90);
            _prompt.sizeDelta = new Vector2(420, 72);
            var o = img.gameObject.AddComponent<Shadow>();
            o.effectColor = UIStyle.Red;
            o.effectDistance = new Vector2(6, -6);
            _promptText = UIStyle.Hud("Text", _prompt, "", 30, UIStyle.Cream, TextAnchor.MiddleCenter);
            UIFactory.Stretch(_promptText.rectTransform, 8);
            _promptText.rectTransform.offsetMin = new Vector2(36, 4); _promptText.rectTransform.offsetMax = new Vector2(-36, -4);
            _prompt.gameObject.SetActive(false);
        }

        void BuildToast()
        {
            var img = UIFactory.Panel("Toast", Hud, UIStyle.Red, UIStyle.Slab);
            var tsh = img.gameObject.AddComponent<Shadow>(); tsh.effectColor = UIStyle.Plum; tsh.effectDistance = new Vector2(7, -7);
            _toast = img.rectTransform;
            _toast.anchorMin = _toast.anchorMax = new Vector2(0.5f, 1f);
            _toast.pivot = new Vector2(0.5f, 1f);
            _toast.anchoredPosition = new Vector2(0, -132); // below the boss name tag, clear of the objective panel
            _toast.sizeDelta = new Vector2(760, 90);
            _toastText = UIStyle.Title("Text", _toast, "", 34, Color.white, TextAnchor.MiddleCenter, redDrop: false);
            UIFactory.Stretch(_toastText.rectTransform, 14);
            _toastText.rectTransform.offsetMin = new Vector2(50, 10); _toastText.rectTransform.offsetMax = new Vector2(-50, -10);
            _toast.gameObject.SetActive(false);
        }

        CanvasScaler _scaler;

        /// <summary>Accessibility text/UI scale (GDD §23.6). 1 = default.</summary>
        public void ApplyUiScale(float scale)
        {
            _scaler ??= Canvas.GetComponent<CanvasScaler>();
            _scaler.referenceResolution = new Vector2(1920, 1080) / Mathf.Clamp(scale, 0.8f, 1.6f);
        }

        void Update()
        {
            if (_toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil) _toast.gameObject.SetActive(false);
            if (_flash != null && _flash.gameObject.activeSelf)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - _flashStart) / _flashDuration);
                var c = _flashColor; c.a *= (1f - k) * (1f - k);
                _flash.color = c;
                if (k >= 1f) _flash.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------ screen flash (camera flash card, big hits)
        UnityEngine.UI.Image _flash;
        float _flashStart, _flashDuration = 0.3f;
        Color _flashColor;

        /// <summary>Full-screen colour flash that fades out (reduced when Settings → Reduce Flashing is on).</summary>
        public void ScreenFlash(Color color, float seconds)
        {
            if (_flash == null)
            {
                _flash = UIFactory.Panel("ScreenFlash", Overlay, Color.clear);
                UIFactory.Stretch(_flash.rectTransform);
                _flash.raycastTarget = false;
            }
            if (ReduceFlashing) { color.a *= 0.35f; seconds *= 1.5f; }
            _flash.transform.SetAsLastSibling();
            _flashColor = color; _flashStart = Time.unscaledTime; _flashDuration = Mathf.Max(0.05f, seconds);
            _flash.color = color;
            _flash.gameObject.SetActive(true);
        }

        /// <summary>Accessibility (GDD §24): softens full-screen flashes.</summary>
        public static bool ReduceFlashing;

        // ------------------------------------------------------------ prompt / toast
        public void ShowPrompt(string text)
        {
            if (string.IsNullOrEmpty(text) || AnyPanelOpen) { HidePrompt(); return; }
            _promptText.text = text;
            _prompt.sizeDelta = new Vector2(Mathf.Max(320, 19 * text.Length + 130), 72);
            _prompt.gameObject.SetActive(true);
        }

        public void HidePrompt() { if (_prompt != null) _prompt.gameObject.SetActive(false); }
        public bool PromptVisible => _prompt != null && _prompt.gameObject.activeSelf;
        public string PromptText => _promptText != null ? _promptText.text : null;

        public void Toast(string text, float seconds = 3f)
        {
            _toastText.text = text;
            _toast.sizeDelta = new Vector2(Mathf.Clamp(text.Length * 22 + 160, 420, 1300), 96);
            bool was = _toast.gameObject.activeSelf;
            _toast.gameObject.SetActive(true);
            if (!was) { var e = _toast.GetComponent<UIEnter>(); if (e != null) Destroy(e); UIEnter.Add(_toast, new Vector2(0, 160), 0f, 0.3f); }
            _toastUntil = Time.unscaledTime + seconds;
        }

        // ------------------------------------------------------------ panels
        public void Push(ModalPanel panel)
        {
            if (panel == null || _stack.Contains(panel)) return;
            if (TopPanel != null) TopPanel.gameObject.SetActive(false);
            _stack.Add(panel);
            panel.transform.SetParent(Panels, false);
            panel.gameObject.SetActive(true);
            panel.OnShown();
            HidePrompt();
            if (_stack.Count == 1) MenuStateChanged?.Invoke(true);
        }

        public void Pop(ModalPanel panel)
        {
            if (!_stack.Remove(panel)) return;
            panel.OnHidden();
            Destroy(panel.gameObject);
            if (TopPanel != null) { TopPanel.gameObject.SetActive(true); TopPanel.OnShown(); }
            if (_stack.Count == 0) MenuStateChanged?.Invoke(false);
        }

        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) Pop(_stack[i]);
        }
    }
}
