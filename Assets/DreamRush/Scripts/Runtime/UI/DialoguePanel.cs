using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Persona-style talk box (M15 UI/UX): a wide slab docked at the bottom, the speaker's name on a tilted red tag,
    /// lines typed out (any key finishes the line, the next press advances), a bobbing ▶. Used by Clockfolk NPCs,
    /// shopkeepers and lore shelves. Blocks gameplay like every ModalPanel.
    /// </summary>
    public class DialoguePanel : ModalPanel
    {
        string[] _lines; int _i; float _shown; Text _body, _next; Action _done;
        const float CharsPerSec = 55f;

        public static DialoguePanel Show(string speaker, string[] lines, Action onDone = null)
        {
            var p = Create<DialoguePanel>(speaker, new Vector2(1500, 300));
            p._lines = lines != null && lines.Length > 0 ? lines : new[] { "..." }; p._done = onDone;
            p.Layout();
            UIRoot.Get().Push(p);
            return p;
        }

        void Layout()
        {
            // dock the card to the bottom; drop the full-screen slash for a lighter dim
            var dim = GetComponent<Image>(); if (dim != null) dim.color = new Color(0, 0, 0, 0.25f);
            var slash = transform.Find("Slash"); if (slash != null) slash.gameObject.SetActive(false);
            Card.anchorMin = Card.anchorMax = new Vector2(0.5f, 0f); Card.pivot = new Vector2(0.5f, 0f);
            Card.anchoredPosition = new Vector2(0, 40);
            var close = Card.Find("CloseButton"); if (close != null) close.gameObject.SetActive(false);
            _body = UIFactory.Label("Line", Content, "", 34, UIFactory.Ink, TextAnchor.UpperLeft);
            _body.font = UIStyle.StoryFont; _body.lineSpacing = 1.1f;
            _next = UIFactory.Label("Next", Card, "▶", 34, UIStyle.Red, TextAnchor.MiddleCenter);
            var nr = _next.rectTransform; nr.anchorMin = nr.anchorMax = new Vector2(1, 0); nr.anchoredPosition = new Vector2(-60, 46); nr.sizeDelta = new Vector2(60, 60);
            _i = 0; _shown = 0f;
        }

        void Update()
        {
            if (_lines == null) return;
            string line = _lines[_i];
            _shown = Mathf.Min(line.Length, _shown + Time.unscaledDeltaTime * CharsPerSec);
            _body.text = line.Substring(0, (int)_shown);
            bool full = _shown >= line.Length;
            _next.gameObject.SetActive(full);
            if (full) _next.rectTransform.anchoredPosition = new Vector2(-60, 46 + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 8f);
            var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
            bool press = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                         || (ms != null && ms.leftButton.wasPressedThisFrame) || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame));
            if (!press || Time.unscaledTime - _openedAt < 0.15f) return;
            if (!full) { _shown = line.Length; return; }
            Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero, 0.5f);
            _i++; _shown = 0f;
            if (_i >= _lines.Length) { var d = _done; _lines = null; Close(); d?.Invoke(); }
        }

        float _openedAt;
        public override void OnShown() { base.OnShown(); _openedAt = Time.unscaledTime; }

        /// <summary>Test/dev: finish every line immediately.</summary>
        public void Skip() { if (_lines == null) return; var d = _done; _lines = null; Close(); d?.Invoke(); }
        public int LineIndex => _i;
    }
}
