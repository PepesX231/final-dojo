using UnityEngine;

namespace MamasComing
{
    /// Floating comic word spawned by KidRoughhouse.
    public class PowPop : MonoBehaviour
    {
        float t; TextMesh tm; Color c0; float spin;
        void Awake() { tm = GetComponent<TextMesh>(); if (tm) c0 = tm.color; spin = Random.Range(-18f, 18f); }
        void Update()
        {
            t += Time.deltaTime;
            float s = t < 0.12f ? Mathf.Lerp(0.2f, 1.35f, t / 0.12f) : Mathf.Lerp(1.35f, 1f, (t - 0.12f) * 6f);
            transform.localScale = Vector3.one * s;
            transform.position += Vector3.up * Time.deltaTime * 0.6f;
            var cam = Camera.main; if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position) * Quaternion.Euler(0, 0, spin);
            if (tm) { var c = c0; c.a = Mathf.Clamp01(1.6f - t * 1.6f); tm.color = c; }
            if (t > 1f) Destroy(gameObject);
        }
    }
}
