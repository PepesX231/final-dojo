using DreamRush.Combat;
using DreamRush.Interaction;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17 STOP-THE-CLOCK bell: ring it (E or punch) and every clock-hand fan nearby freezes for a few seconds.</summary>
    public class FreezeBell : Interactable, IHittable
    {
        public float radius = 40f, seconds = 7f;
        public Transform bell;
        float _swing, _cool;
        protected override void OnEnable() { base.OnEnable(); prompt = "RING THE BELL"; }
        protected override void OnInteract(InteractionContext context) => Ring();
        public bool TakeHit(HitInfo hit) { Ring(); return true; }

        public void Ring()
        {
            if (Time.time < _cool) return;
            _cool = Time.time + 1f; _swing = 1f;
            foreach (var h in SpinHazard.All)
                if (h != null && Vector3.Distance(h.transform.position, transform.position) < radius) h.FrozenUntil = Time.time + seconds;
            Audio.AudioManager.Play(Audio.Sfx.Shock, transform.position, 0.9f);
            Presentation.VFX.Play(Presentation.VFXKind.ImpactRing, transform.position, 2.5f, new Color(0.5f, 0.9f, 1f));
            if (UI.UIRoot.Exists) UI.UIRoot.Get().Toast($"เวลาหยุด {seconds:F0} วินาที!", 1.2f);
        }

        void Update()
        {
            if (bell == null) return;
            _swing = Mathf.MoveTowards(_swing, 0f, Time.deltaTime * 0.6f);
            bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 9f) * 25f * _swing, 0, 0);
        }
    }
}
