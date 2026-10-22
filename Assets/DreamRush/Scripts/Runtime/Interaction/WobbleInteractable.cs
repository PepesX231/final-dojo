using System.Collections;
using DreamRush.Flow;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Interaction
{
    /// <summary>The tiny chair in the bedroom wobbles nervously when poked — foreshadows the boss (GDD §7.8).</summary>
    public class WobbleInteractable : Interactable
    {
        public string toast = "The little chair shivers... was it always facing that way?";
        public float duration = 0.8f;
        public float angle = 9f;
        bool _busy;

        protected override void OnInteract(InteractionContext context)
        {
            if (_busy) return;
            StartCoroutine(Wobble(context.Player != null ? context.Player.transform.position : transform.position));
            UIRoot.Get().Toast(toast, 3f);
        }

        IEnumerator Wobble(Vector3 from)
        {
            _busy = true;
            Quaternion rest = transform.localRotation;
            Vector3 restPos = transform.localPosition;
            Vector3 away = transform.position - from; away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : transform.forward;
            Vector3 axis = Vector3.Cross(Vector3.up, away);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = 1f - t / duration;
                float a = Mathf.Sin(t * 38f) * angle * k;
                transform.localRotation = Quaternion.AngleAxis(a, transform.parent ? transform.parent.InverseTransformDirection(axis) : axis) * rest;
                transform.localPosition = restPos + (transform.parent ? transform.parent.InverseTransformDirection(away) : away) * (0.06f * k);
                yield return null;
            }
            transform.localRotation = rest;
            transform.localPosition = restPos + (transform.parent ? transform.parent.InverseTransformDirection(away) : away) * 0.06f;
            _busy = false;
        }
    }
}
