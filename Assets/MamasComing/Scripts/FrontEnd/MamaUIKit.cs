using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// Small builder used both by the editor (to author the title / lobby canvases into the scene) and at runtime.
    public static class MamaUIKit
    {
        public static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = Vector2.zero, BR = new Vector2(1, 0), CC = new Vector2(.5f, .5f), TC = new Vector2(.5f, 1), BC = new Vector2(.5f, 0), ML = new Vector2(0, .5f), MR = new Vector2(1, .5f);

        public static Canvas Canvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = .5f;
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float rot = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rect(name, parent, CC, CC, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; return rt;
        }

        public static Image Img(RectTransform rt, Sprite s, Color c, float border = 26f, bool raycast = false)
        {
            var im = rt.gameObject.GetComponent<Image>(); if (!im) im = rt.gameObject.AddComponent<Image>();
            im.color = c; im.raycastTarget = raycast;
            if (s) MamaTheme.Skin(im, s, c, border); else im.sprite = null;
            return im;
        }

        public static Text Txt(RectTransform rt, string en, string th, Font f, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, float outline = 0f, Color? outlineCol = null)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = f; t.fontSize = size; t.color = c; t.alignment = a; t.text = en;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            if (outline > 0f)
            {
                var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = outlineCol ?? MamaTheme.WoodInk; o.effectDistance = new Vector2(outline, -outline);
                var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0.12f, 0.06f, 0.03f, 0.75f); sh.effectDistance = new Vector2(outline * 0.8f, -outline * 1.6f);
            }
            if (!string.IsNullOrEmpty(th)) { var l = rt.gameObject.AddComponent<MamaLocText>(); l.en = en; l.th = th; }
            return t;
        }

        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, string en, string th, Font f, int fs, Color c, TextAnchor a = TextAnchor.MiddleCenter, float outline = 0f, float rot = 0f)
            => Txt(Rect(name, parent, anchor, pivot, pos, size, rot), en, th, f, fs, c, a, outline);
    }
}
