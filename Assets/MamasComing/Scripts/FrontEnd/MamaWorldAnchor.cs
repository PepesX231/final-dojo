using UnityEngine;

namespace MamasComing
{
    /// Keeps a UI element (name tag, speech bubble) over a kid in the 3D shot.
    public class MamaWorldAnchor : MonoBehaviour
    {
        public MamaPosedKid kid;
        public Transform target;
        public Vector3 worldOffset = new Vector3(0f, 0.25f, 0f);
        public Vector2 screenOffset;
        public bool hideWhenKidHidden = true;
        CanvasGroup group;

        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            if (!group) { group = GetComponent<CanvasGroup>(); if (!group) group = gameObject.AddComponent<CanvasGroup>(); }
            bool show = true;
            Vector3 w;
            if (kid) { show = kid.gameObject.activeInHierarchy && kid.Model; w = (kid.Model && kid.Model.gameObject.activeInHierarchy ? kid.HeadPoint : kid.transform.position + Vector3.up * 0.55f) + worldOffset; }
            else if (target) w = target.position + worldOffset;
            else return;
            Vector3 sp = cam.WorldToScreenPoint(w);
            if (sp.z < 0f) show = false;
            group.alpha = show || !hideWhenKidHidden ? 1f : 0f;
            var rt = (RectTransform)transform; var parent = (RectTransform)rt.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, sp, null, out var local))
                rt.anchoredPosition = local - (parent.rect.min + Vector2.Scale(rt.anchorMin, parent.rect.size)) + screenOffset;
        }

    }
}
