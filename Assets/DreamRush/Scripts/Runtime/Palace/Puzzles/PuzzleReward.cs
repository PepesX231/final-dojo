using System.Collections;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17: what a solved puzzle does to the world — objects pop into existence (stairs, bridges, lift pads), blockers
    /// vanish, minutes rain out, a toast + a chime. Shared by the circuit, crate and rewind puzzles.
    /// </summary>
    public class PuzzleReward : MonoBehaviour
    {
        public string id = "puzzle";
        public string toast = "ไขปริศนาสำเร็จ!";
        public GameObject[] show = { };
        public GameObject[] hide = { };
        public int minutes = 5;
        public bool Solved { get; private set; }
        public static event System.Action<PuzzleReward> AnySolved;

        void Start() { if (!Solved) foreach (var g in show) if (g != null) g.SetActive(false); }

        public void Solve()
        {
            if (Solved) return;
            Solved = true;
            StartCoroutine(Run());
            AnySolved?.Invoke(this);
            WonderDirector.Novelty("puzzle " + id);
            if (PalaceDirector.Current != null) PalaceDirector.Current.Take("puzzle:" + id);
        }

        IEnumerator Run()
        {
            Audio.AudioManager.Play(Audio.Sfx.Victory, transform.position, 0.7f);
            if (UIRoot.Exists) UIRoot.Get().Toast(toast, 2.2f);
            if (minutes > 0) LostMinute.Burst(transform.position + Vector3.up, minutes);
            foreach (var g in hide) if (g != null) { Presentation.VFX.Play(Presentation.VFXKind.Poof, g.transform.position + Vector3.up, 1.5f, Presentation.VFX.Gold); g.SetActive(false); }
            foreach (var g in show)
            {
                if (g == null) continue;
                Vector3 s = g.transform.localScale; g.transform.localScale = s * 0.01f; g.SetActive(true);
                Presentation.VFX.Play(Presentation.VFXKind.StarBurst, g.transform.position, 1.2f, Presentation.VFX.Gold);
                Audio.AudioManager.Play(Audio.Sfx.Pickup, g.transform.position, 0.6f);
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
                {
                    float k = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f; g.transform.localScale = s * Mathf.Lerp(0.01f, 1f, Mathf.SmoothStep(0, 1, t)) * k; yield return null;
                }
                g.transform.localScale = s;
                yield return new WaitForSeconds(0.12f);
            }
        }
    }
}
