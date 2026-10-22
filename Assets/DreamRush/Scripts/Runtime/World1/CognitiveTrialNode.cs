using DreamRush.Player;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// An interactive Cognitive Logic Switch / Analyzer Totem placed in World 1 chapters.
    /// Stepping near the pressure pad verifies the player's observation/hypothesis, turns the holographic crystal
    /// from Crimson/Amber to Electric Cyan, and activates an optional helper bridge or speed boost.
    /// </summary>
    public class CognitiveTrialNode : MonoBehaviour
    {
        public string TrialCode = "LOGIC-01";
        public string HypothesisPrompt = "วิเคราะห์รูปแบบการเคลื่อนที่";
        public string SolvedMessage = "ไขปริศนาสำเร็จ! เปิดเส้นทางเสริมและรับแรงส่งความเร็ว";
        public Transform CrystalVisual;
        public Light BeaconLight;
        public GameObject HelperBridge;
        public float TriggerRadius = 2.6f;
        public bool GrantSpeedBoost = true;
        public bool Solved { get; private set; }

        PlayerMotor3D _player;

        void Start()
        {
            if (ProblemSolvingDirector.Current != null && !ProblemSolvingDirector.Current.Trials.Contains(this))
                ProblemSolvingDirector.Current.Trials.Add(this);
            if (HelperBridge != null) HelperBridge.SetActive(false);
        }

        void Update()
        {
            if (CrystalVisual != null)
            {
                float spd = Solved ? 140f : 48f;
                CrystalVisual.Rotate(Vector3.up, spd * Time.deltaTime, Space.World);
                Vector3 p = CrystalVisual.localPosition;
                p.y = 2.1f + Mathf.Sin(Time.time * 3.2f) * 0.18f;
                CrystalVisual.localPosition = p;
            }
            if (Solved) return;

            if (_player == null) _player = FindFirstObjectByType<PlayerMotor3D>();
            if (_player == null) return;

            Vector3 d = _player.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 3.2f && new Vector2(d.x, d.z).sqrMagnitude <= TriggerRadius * TriggerRadius)
            {
                Solve();
            }
        }

        public void Solve()
        {
            if (Solved) return;
            Solved = true;
            if (HelperBridge != null) HelperBridge.SetActive(true);
            if (BeaconLight != null)
            {
                BeaconLight.color = new Color(0.15f, 0.95f, 1f);
                BeaconLight.intensity = 4.2f;
            }
            if (CrystalVisual != null)
            {
                foreach (var r in CrystalVisual.GetComponentsInChildren<MeshRenderer>())
                {
                    var m = new Material(r.sharedMaterial);
                    m.SetColor("_BaseColor", new Color(0.2f, 0.98f, 1f));
                    r.sharedMaterial = m;
                }
            }
            Audio.AudioManager.Play(Audio.Sfx.Pickup, transform.position, 0.75f);
            if (ProblemSolvingDirector.Current != null)
                ProblemSolvingDirector.Current.NotifyTrialActivated(this);
        }
    }
}
