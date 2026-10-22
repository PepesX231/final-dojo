using System.Collections;
using DreamRush.Enemies;
using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// Functional 2D element (GDD §5.7 Type C): a paper plank folded upright that unfolds flat when
    /// <see cref="trigger"/> stool is caught. Its collider follows the paper exactly — it only blocks/supports
    /// when it visibly does.
    /// </summary>
    public class PaperBridge : MonoBehaviour
    {
        public LittleStoolController trigger;
        public float unfoldTime = 0.8f;
        public bool startOpen;
        public bool IsOpen { get; private set; }

        void Start()
        {
            transform.localRotation = startOpen ? Quaternion.identity : Quaternion.Euler(-90f, 0, 0);
            IsOpen = startOpen;
            if (trigger != null) trigger.Caught += Open;
        }

        void OnDestroy() { if (trigger != null) trigger.Caught -= Open; }

        public void Open()
        {
            if (IsOpen) return;
            StartCoroutine(Unfold());
        }

        IEnumerator Unfold()
        {
            Quaternion a = transform.localRotation, b = Quaternion.identity;
            for (float t = 0; t < unfoldTime; t += Time.deltaTime)
            {
                float k = t / unfoldTime;
                k = 1f + 2.2f * Mathf.Pow(k - 1f, 3) + 1.2f * Mathf.Pow(k - 1f, 2); // ease out back
                transform.localRotation = Quaternion.SlerpUnclamped(a, b, k);
                yield return null;
            }
            transform.localRotation = b;
            IsOpen = true;
        }
    }
}
