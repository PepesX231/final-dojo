using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// THE CHAIR appears in every chapter (docs/LEVEL1_DESIGN.md): it waits, notices Pip, yelps and runs to
    /// <see cref="runTo"/>, then poofs away. Or, with <see cref="patrol"/>, it just trots back and forth in the
    /// background (Ch2 silhouette). Presentation only.
    /// </summary>
    public class ChairCameo : MonoBehaviour
    {
        public Transform runTo;
        public float noticeRadius = 10f, speed = 7f;
        public string line = "!!";
        public bool patrol;
        public Vector3 patrolOffset = new Vector3(20, 0, 0);
        public float patrolPeriod = 8f;
        public Transform visual;
        PlayerMotor3D _player;
        int _state; float _t; Vector3 _home;

        void Start() { _player = FindFirstObjectByType<PlayerMotor3D>(); _home = transform.position; }

        void Update()
        {
            _t += Time.deltaTime;
            if (patrol)
            {
                float k = Mathf.PingPong(_t / patrolPeriod * 2f, 1f);
                Vector3 prev = transform.position;
                transform.position = _home + patrolOffset * Mathf.SmoothStep(0, 1, k);
                Vector3 v = transform.position - prev; v.y = 0;
                if (v.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(v), 0.2f);
                Hop(8f);
                return;
            }
            if (_player == null) return;
            switch (_state)
            {
                case 0:
                    Hop(2f);
                    if ((_player.transform.position - transform.position).magnitude < noticeRadius)
                    {
                        _state = 1; _t = 0;
                        UI.UIRoot.Get().Toast(line, 2f);
                        Audio.AudioManager.Play(Audio.Sfx.ChairGasp, transform.position, 1f);
                        VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 1.6f, 0.8f, VFX.Gold);
                    }
                    break;
                case 1:
                    Vector3 to = _player.transform.position - transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
                    if (_t > 0.7f) { _state = 2; Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 1f); }
                    break;
                case 2:
                    if (runTo == null) { Vanish(); break; }
                    Vector3 d = runTo.position - transform.position; d.y = 0;
                    if (d.magnitude < 0.5f) { Vanish(); break; }
                    transform.rotation = Quaternion.LookRotation(d);
                    transform.position += d.normalized * speed * Time.deltaTime;
                    Hop(10f);
                    break;
            }
        }

        void Hop(float rate)
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_t * rate)) * 0.2f, 0);
            visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * rate) * 6f);
        }

        void Vanish()
        {
            VFX.Play(VFXKind.Poof, transform.position + Vector3.up * 0.7f, 1.8f);
            gameObject.SetActive(false);
        }
    }
}
