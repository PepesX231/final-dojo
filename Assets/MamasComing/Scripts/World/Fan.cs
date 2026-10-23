using UnityEngine;

namespace MamasComing
{
    /// <summary>Pedestal fan: pushes smoke along its direction... and blows light stuff (papers!) everywhere.</summary>
    public class Fan : Interactable
    {
        public bool on;
        public Transform head;
        public Transform blades;
        public float range = 5f;
        public float angle = 32f;
        public float strength = 6f;
        public float oscillate = 25f;

        float spin;
        bool warnedPaper;

        public Vector3 HeadPos => (head ? head : transform).position;
        public Vector3 HeadForward => (head ? head : transform).forward;

        void Update()
        {
            spin = Mathf.MoveTowards(spin, on && PowerGrid.On ? 1440f : 0f, Time.deltaTime * 900f);
            if (blades) blades.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
            if (head && on && PowerGrid.On) head.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.7f) * oscillate, 0f);
        }

        void FixedUpdate()
        {
            if (!on || !PowerGrid.On) return;
            Vector3 o = HeadPos, f = HeadForward;
            foreach (var m in MessyProp.Lights)
            {
                if (!m || m.IsHeld || m.Body.isKinematic) continue;
                Vector3 d = m.transform.position - o;
                float dist = d.magnitude;
                if (dist > range || Vector3.Angle(f, d) > angle) continue;
                float k = 1f - dist / range;
                m.Body.AddForce((f * strength + Vector3.up * strength * 0.35f) * k, ForceMode.Acceleration);
                m.Body.WakeUp();
                if (!warnedPaper && m.IsDisplaced && GameManager.I)
                {
                    warnedPaper = true;
                    GameManager.I.Toast("Wait - the fan is blowing papers everywhere!");
                }
            }
        }

        public override string GetPrompt(PlayerController p) => on ? "Turn off the fan" : "Turn ON the fan";

        public override void Interact(PlayerController p)
        {
            on = !on;
            MamaAudio.Play("click", transform.position, 0.8f);
            if (on && GameManager.I) GameManager.I.Toast("Fan on! It pushes the smoke toward the open window.");
        }
    }
}
