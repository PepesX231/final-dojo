using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Paper-card panel with title, scrollable-free content column and a close button. Subclasses fill
    /// <see cref="Content"/> in <see cref="BuildContent"/>. Cancel (Esc / B) closes the top panel.
    /// </summary>
    public class ModalPanel : MonoBehaviour
    {
        public RectTransform Card { get; private set; }
        public RectTransform Content { get; private set; }
        public Text Title { get; private set; }
        public event Action Closed;
        public bool CloseOnCancel = true;

        Selectable _firstSelectable;

        public static T Create<T>(string title, Vector2 size) where T : ModalPanel
        {
            var root = UIFactory.Rect(typeof(T).Name, UIRoot.Get().Panels);
            UIFactory.Stretch(root);
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(UIStyle.Plum.r, UIStyle.Plum.g, UIStyle.Plum.b, 0.72f);
            var panel = root.gameObject.AddComponent<T>();
            panel.BuildFrame(title, size);
            panel.BuildContent();
            return panel;
        }

        void BuildFrame(string title, Vector2 size)
        {
            // backdrop: halftone field + one big red diagonal slash (Persona), all behind the window
            if (UIStyle.Halftone != null)
            {
                var ht = UIStyle.Pattern("Halftone", transform, UIStyle.Halftone, new Color(1, 1, 1, 0.05f), 1.5f);
                UIFactory.Stretch(ht.rectTransform);
            }
            var slash = UIStyle.Image("Slash", transform, null, UIStyle.Red);
            slash.sprite = UIFactory.RoundedSmall;
            slash.rectTransform.sizeDelta = new Vector2(3400, size.y * 0.42f);
            slash.rectTransform.localRotation = Quaternion.Euler(0, 0, 14f);
            slash.rectTransform.anchoredPosition = new Vector2(0, -size.y * 0.08f);
            UIEnter.Add(slash, new Vector2(-1800, 0), 0f, 0.32f);

            var card = UIFactory.Panel("Card", transform, Color.white, UIStyle.Window ?? UIFactory.Rounded);
            Card = card.rectTransform;
            Card.sizeDelta = size;
            UIStyle.HardShadow(card, UIFactory.PaperShadow, new Vector2(14, -14));
            UIEnter.Add(card, new Vector2(140, -40), 0.04f, 0.34f, -3f);

            // title on a skewed red slab that overhangs the window's top-left corner
            var tag = UIStyle.Image("TitleSlab", Card, UIStyle.Slab, UIStyle.Red);
            var trt = tag.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0, 1); trt.pivot = new Vector2(0, 0.5f);
            trt.sizeDelta = new Vector2(Mathf.Clamp(title.Length * 34 + 140, 320, size.x * 0.8f), 96);
            trt.anchoredPosition = new Vector2(-36, -8);
            trt.localRotation = Quaternion.Euler(0, 0, 3f);
            UIStyle.HardShadow(tag, UIStyle.Plum, new Vector2(8, -8));
            Title = UIStyle.Title("Title", trt, title, 54, Color.white, TextAnchor.MiddleLeft, redDrop: false);
            UIFactory.Stretch(Title.rectTransform);
            Title.rectTransform.offsetMin = new Vector2(52, 0); Title.rectTransform.offsetMax = new Vector2(-30, 4);
            UIEnter.Add(tag, new Vector2(-260, 0), 0.12f, 0.32f, 6f);

            Content = UIFactory.Rect("Content", Card);
            Content.anchorMin = Vector2.zero; Content.anchorMax = Vector2.one;
            Content.offsetMin = new Vector2(60, 116); Content.offsetMax = new Vector2(-60, -84);
            UIFactory.VerticalLayout(Content.gameObject, 12, 0);

            var close = UIFactory.Button("CloseButton", Card, "CLOSE", Close, 260, 64);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(1, 0);
            crt.anchoredPosition = new Vector2(-48, 32);
            _firstSelectable = close;
        }

        protected virtual void BuildContent() { }

        protected void SetFirstSelectable(Selectable s) => _firstSelectable = s;

        public virtual void OnShown()
        {
            Audio.AudioManager.Play(Audio.Sfx.UiOpen, Vector3.zero);
            if (EventSystem.current != null && _firstSelectable != null)
                EventSystem.current.SetSelectedGameObject(_firstSelectable.gameObject);
        }

        public virtual void OnHidden() { }

        public void Close()
        {
            Audio.AudioManager.Play(Audio.Sfx.UiBack, Vector3.zero, 0.5f);
            Closed?.Invoke();
            Closed = null;
            if (UIRoot.Exists) UIRoot.Get().Pop(this);
        }

        protected Text Paragraph(string text, int size = 26, TextAnchor align = TextAnchor.UpperLeft)
        {
            // Text is an ILayoutElement: the VerticalLayoutGroup sizes it from its wrapped preferred height.
            return UIFactory.Label("Paragraph", Content, text, size, UIFactory.Ink, align);
        }
    }
}
