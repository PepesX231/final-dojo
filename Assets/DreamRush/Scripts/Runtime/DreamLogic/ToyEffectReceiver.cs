using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>Wind-up toy: reacts to shared environmental effects.</summary>
    public class ToyEffectReceiver : MonoBehaviour, IEffectReceiver
    {
        public DreamProperty properties = DreamProperty.Movable | DreamProperty.Rootable | DreamProperty.Freezeable |
                                          DreamProperty.Pullable | DreamProperty.SocialResponsive | DreamProperty.Distractible;
        public Dream.PingPongMover mover;
        float _pausedUntil;
        Vector3 _pullTarget;
        float _pullUntil;

        public DreamProperty Properties => properties;
        public Transform Transform => transform;
        public string LastEffect { get; private set; }

        void Awake() { if (mover == null) mover = GetComponent<Dream.PingPongMover>(); }
        void OnEnable() => EffectReceiverRegistry.Register(this);
        void OnDisable() => EffectReceiverRegistry.Unregister(this);

        public bool CanReceive(EffectContext c)
        {
            var need = EffectContext.RequiredProperty(c.Action);
            return need == DreamProperty.None || (properties & need) != 0;
        }

        public void ApplyEffect(EffectContext c)
        {
            LastEffect = c.Action.ToString();
            _pausedUntil = Time.time + Mathf.Max(1.5f, c.Duration);
            if (c.Action == EffectAction.Pull) { _pullTarget = c.Source.transform.position; _pullUntil = Time.time + 1f; }
            if (UI.UIRoot.Exists)
            {
                string msg = c.Action switch
                {
                    EffectAction.Root => "The toy is stuck fast! (Rooted things can't run.)",
                    EffectAction.Freeze => "Flash! The toy froze mid-step.",
                    EffectAction.Pull => "Reeled in! Pulled things wobble for a moment.",
                    EffectAction.ChangeBehavior => "The toy bows politely to its elder.",
                    EffectAction.SpawnDecoy => "The toy stares at the decoy apple...",
                    _ => c.Action.ToString()
                };
                UI.UIRoot.Get().Toast(msg, 2.5f);
            }
        }

        void Update()
        {
            if (mover != null) mover.paused = Time.time < _pausedUntil;
            if (Time.time < _pullUntil)
            {
                Vector3 to = _pullTarget - transform.position; to.y = 0f;
                if (to.magnitude > 1.3f) transform.position += to.normalized * 5f * Time.deltaTime;
            }
        }
    }
}
