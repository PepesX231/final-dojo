using UnityEngine;

namespace MamasComing
{
    /// Floating comic word spawned by KidRoughhouse.
    public class PowPop : MonoBehaviour
    {
        float t; TextMesh tm; Color c0; float spin;
        TextMesh[] ink;

        void Awake()
        {
            tm = GetComponent<TextMesh>(); if (tm) c0 = tm.color; spin = Random.Range(-18f, 18f);
            ComicFx.Pow(transform.position, c0);   // halftone starburst + speed lines behind the word
            if (!tm) return;
            // chunky ink outline: dark copies of the word nudged around it, slightly behind
            var offs = new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(.7f, -.7f), new Vector2(-.7f, -.7f), new Vector2(1.2f, -1.2f) };
            ink = new TextMesh[offs.Length];
            for (int i = 0; i < offs.Length; i++)
            {
                var go = new GameObject("Ink"); go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(offs[i].x, offs[i].y, 0f) * .045f + Vector3.forward * .01f;
                var t = go.AddComponent<TextMesh>();
                t.text = tm.text; t.fontSize = tm.fontSize; t.characterSize = tm.characterSize; t.anchor = tm.anchor;
                t.alignment = tm.alignment; t.fontStyle = tm.fontStyle; t.font = tm.font;
                t.color = new Color(.11f, .09f, .2f);
                go.GetComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
                ink[i] = t;
            }
        }
        void Update()
        {
            t += Time.deltaTime;
            float s = t < 0.12f ? Mathf.Lerp(0.2f, 1.35f, t / 0.12f) : Mathf.Lerp(1.35f, 1f, (t - 0.12f) * 6f);
            transform.localScale = Vector3.one * s;
            transform.position += Vector3.up * Time.deltaTime * 0.6f;
            var cam = Camera.main; if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position) * Quaternion.Euler(0, 0, spin);
            if (tm) { var c = c0; c.a = Mathf.Clamp01(1.6f - t * 1.6f); tm.color = c; }
            if (ink != null) foreach (var i in ink) { var c = i.color; c.a = Mathf.Clamp01(1.6f - t * 1.6f); i.color = c; }
            if (t > 1f) Destroy(gameObject);
        }
    }
}
