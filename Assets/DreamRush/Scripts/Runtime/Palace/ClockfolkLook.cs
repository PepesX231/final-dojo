using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>Clockfolk resident: random pyjama colour, sleepy idle sway, a happy hop when you talk to them.</summary>
    public class ClockfolkLook : MonoBehaviour
    {
        static readonly Color[] Pyjamas = { new Color(0.45f, 0.6f, 1f), new Color(1f, 0.55f, 0.7f), new Color(0.5f, 0.85f, 0.6f), new Color(1f, 0.75f, 0.3f), new Color(0.7f, 0.5f, 1f), new Color(0.35f, 0.85f, 0.9f) };
        Transform _model; float _t, _hop; Vector3 _base; float _phase;

        public void Dress(System.Random rnd)
        {
            var c = Pyjamas[rnd.Next(Pyjamas.Length)];
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.Contains("Pyjama"))
                    {
                        mpb.Clear(); mpb.SetColor("_BaseColor", c); r.SetPropertyBlock(mpb, i);
                    }
            }
            _phase = (float)rnd.NextDouble() * 6f;
            float s = 0.9f + (float)rnd.NextDouble() * 0.25f; transform.localScale = Vector3.one * s;
        }

        public void Bounce() { _hop = 1f; }

        void Start() { _model = transform.childCount > 0 ? transform.GetChild(0) : transform; _base = _model.localPosition; }

        void Update()
        {
            if (_model == null) return;
            _t += Time.deltaTime; _hop = Mathf.MoveTowards(_hop, 0f, Time.deltaTime * 2.5f);
            float hop = Mathf.Sin((1f - _hop) * Mathf.PI) * 0.25f * (_hop > 0f ? 1f : 0f);
            _model.localPosition = _base + Vector3.up * hop;
            _model.localRotation = Quaternion.Euler(0, Mathf.Sin(_t * 0.6f + _phase) * 8f, Mathf.Sin(_t * 1.1f + _phase) * 4f);
            // face Pip when he's close
            var p = Player.PlayerMotor3D.Instance;
            if (p != null)
            {
                Vector3 to = p.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude < 25f && to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f);
            }
        }
    }
}
