using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>A physics prop with a "home" spot. Knocked over / moved away = mess Mom can notice.</summary>
    public class MessyProp : Pickup, IMess
    {
        public static readonly List<MessyProp> Lights = new List<MessyProp>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLights() { Lights.Clear(); }

        [Header("Mess")]
        public Vector3 homePosition;
        public Quaternion homeRotation = Quaternion.identity;
        public bool homeSet;
        public float displaceTolerance = 0.45f;
        public float snapDistance = 1.3f;
        public int severity = 1;
        [Tooltip("Light things (paper) get blown around by the fan.")]
        public bool isLight;
        [TextArea] public string momLine;

        protected override void Awake()
        {
            base.Awake();
            if (!homeSet)
            {
                homePosition = transform.position;
                homeRotation = transform.rotation;
                homeSet = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MessRegistry.Add(this);
            if (isLight && !Lights.Contains(this)) Lights.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MessRegistry.Remove(this);
            Lights.Remove(this);
        }

        public virtual MessKind Kind => MessKind.Clutter;

        public bool IsDisplaced =>
            Vector3.Distance(transform.position, homePosition) > displaceTolerance ||
            Quaternion.Angle(transform.rotation, homeRotation) > 40f;

        public virtual bool IsProblem => !IsHeld && IsDisplaced;
        public virtual bool IsHidden => HideZone.IsHidden(transform.position);
        public virtual int Severity => severity;
        public virtual string MomLine => string.IsNullOrEmpty(momLine)
            ? "Why is the " + displayName.ToLower() + " all over the place?!"
            : momLine;
        public string Label => displayName;
        public Vector3 Position => transform.position;

        public bool NearHome(PlayerController p)
        {
            Vector3 a = p.transform.position, b = homePosition;
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b) < snapDistance;
        }

        public override void OnDropped(PlayerController p, Vector3 velocity)
        {
            if (p != null && velocity.sqrMagnitude < 30f && NearHome(p))
            {
                base.OnDropped(p, Vector3.zero);
                transform.SetPositionAndRotation(homePosition, homeRotation);
                // Commit the restored pose to physics too: the last carried pose must not
                // become a release velocity that throws a correctly returned prop away.
                Body.position = homePosition;
                Body.rotation = homeRotation;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.Sleep(); // remains dynamic; a later collision can still knock it out of place
                if (GameManager.I && GameManager.I.ui) GameManager.I.ui.Confirm(MamaReadability.Text(displayName + " is back where it belongs.", Loc.T(displayName) + " กลับเข้าที่แล้ว"));
                return;
            }
            base.OnDropped(p, velocity);
        }
    }
}
