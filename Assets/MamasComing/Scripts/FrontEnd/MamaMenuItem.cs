using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MamasComing
{
    /// One clickable word / button of the title or lobby menu. Restyle freely; the screen script only uses `action`.
    public class MamaMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public enum Action { Play, Coop, HowTo, Options, Quit, Back, Start, Leave, HostOnline, HostLan, Join, PrevKid, NextKid }
        public Action action;
        public Text label;
        [Tooltip("3D sign version of this item (pops out toward the camera when selected).")]
        public MamaSign3D sign;
        public float popOut = 0.12f;
        [Tooltip("Optional brush stroke shown under the selected item.")]
        public Image underline;
        public Color normal = new Color(0.97f, 0.92f, 0.80f);
        public Color selected = new Color(1f, 0.47f, 0.62f);
        public bool interactable = true;
        [Tooltip("Camera shot used while this item is selected (title screen). Empty = the screen's main shot.")]
        public MamaShot focus;

        public System.Action<MamaMenuItem> Hovered, Clicked;
        public bool IsSelected { get; private set; }
        float pop; Vector3 restPos; Quaternion restRot; bool rest;

        public void OnPointerEnter(PointerEventData e) { if (interactable) Hovered?.Invoke(this); }
        public void OnPointerClick(PointerEventData e) { if (interactable) Clicked?.Invoke(this); }

        void UpdateSign()
        {
            if (!rest) { rest = true; restPos = transform.localPosition; restRot = transform.localRotation; }
            pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 3f);
            var cam = Camera.main;
            Vector3 toCam = cam ? (cam.transform.position - transform.position).normalized : Vector3.back;
            float sel = IsSelected && interactable ? 1f : 0f;
            Vector3 targetPos = restPos + (transform.parent ? transform.parent.InverseTransformDirection(toCam) : toCam) * popOut * sel;
            var wob = Quaternion.Euler(Mathf.Sin(Time.unscaledTime * 5f) * 3f * sel + pop * 10f, Mathf.Sin(Time.unscaledTime * 3.1f) * 4f * sel, Mathf.Sin(Time.unscaledTime * 4f) * 2f * sel - pop * 8f);
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, k);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, restRot * wob, k);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (1f + sel * 0.08f + pop * 0.1f), k);
            sign.face = !interactable ? new Color(normal.r, normal.g, normal.b, 0.5f) : IsSelected ? selected : normal;
        }

        public void SetSelected(bool on)
        {
            if (on && !IsSelected) pop = 1f;
            IsSelected = on;
        }

        void Update()
        {
            if (sign) { UpdateSign(); return; }
            pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 4f);
            float wob = IsSelected ? 1.08f + Mathf.Sin(Time.unscaledTime * 6f) * 0.015f + pop * 0.12f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * wob, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            if (label) label.color = !interactable ? new Color(normal.r, normal.g, normal.b, 0.45f) : IsSelected ? selected : normal;
            if (underline) { underline.enabled = IsSelected && interactable; underline.color = selected; }
        }
    }
}
