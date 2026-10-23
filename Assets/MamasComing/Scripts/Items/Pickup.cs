using UnityEngine;

namespace MamasComing
{
    public enum ToolType { None, Prop, Mop, Rug, Pot, Bucket, Chair, Lid, Towel, Extinguisher, Squeegee }

    [RequireComponent(typeof(Rigidbody))]
    public class Pickup : Interactable
    {
        [Header("Pickup")]
        public ToolType tool = ToolType.Prop;
        [Tooltip("Dragged along the floor in front of the player instead of carried.")]
        public bool dragOnFloor;
        public Vector3 holdOffset;
        public Vector3 holdEuler;
        public float carrySpeedMultiplier = 1f;
        [Tooltip("Stays kinematic (leaning on a wall / hanging) until picked up the first time.")]
        public bool startKinematic;
        public float grabTime = 0.3f;

        public Rigidbody Body { get; private set; }
        public bool IsHeld { get; private set; }
        public bool Stashed { get; private set; }
        /// <summary>Item positions itself while held (rugs trail behind the kid).</summary>
        public virtual bool FollowsOnItsOwn => false;

        protected Collider[] cols;
        float grabT = 1f;
        Vector3 grabStartPos;
        Quaternion grabStartRot;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            cols = GetComponentsInChildren<Collider>(true);
            if (startKinematic) Body.isKinematic = true;
        }

        public virtual float SpeedMultiplier => carrySpeedMultiplier;

        public override bool CanInteract(PlayerController p) =>
            !IsHeld && !(Stashed && HideZone.IsHidden(transform.position));

        public override string GetPrompt(PlayerController p)
        {
            string verb = p.Held != null ? "Swap for the " : (dragOnFloor ? "Grab the " : "Pick up the ");
            return verb + displayName.ToLower();
        }

        public override void Interact(PlayerController p) => p.PickUp(this);

        public virtual void OnPickedUp(PlayerController p)
        {
            IsHeld = true;
            Stashed = false;
            Body.isKinematic = true;
            Body.interpolation = RigidbodyInterpolation.None;
            SetCols(false);
            grabT = 0f;
            grabStartPos = transform.position;
            grabStartRot = transform.rotation;
        }

        /// <summary>Eases the item from where it was grabbed into the hands (called every frame while held).</summary>
        public void TickHeldPose(float dt)
        {
            if (FollowsOnItsOwn || grabT >= 1f || !transform.parent) return;
            grabT = Mathf.Min(1f, grabT + dt / Mathf.Max(0.05f, grabTime));
            float e = grabT * grabT * (3f - 2f * grabT);
            Vector3 startLocal = transform.parent.InverseTransformPoint(grabStartPos);
            Quaternion startLocalRot = Quaternion.Inverse(transform.parent.rotation) * grabStartRot;
            // small arc: lift up while travelling to the hands
            transform.localPosition = Vector3.Lerp(startLocal, holdOffset, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.12f;
            transform.localRotation = Quaternion.Slerp(startLocalRot, Quaternion.Euler(holdEuler), e);
        }

        public bool Grabbing => IsHeld && grabT < 1f;

        public virtual void OnDropped(PlayerController p, Vector3 velocity)
        {
            IsHeld = false;
            grabT = 1f;
            transform.SetParent(null, true);
            SetCols(true);
            Body.isKinematic = false;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.linearVelocity = velocity;
            Body.angularVelocity = Vector3.zero;
            Body.WakeUp();
        }

        /// <summary>Distance from the pivot to the lowest point of the visuals (for clean placement).</summary>
        public float BottomOffset()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0f;
            float minY = float.MaxValue;
            Quaternion keep = transform.rotation;
            transform.rotation = Quaternion.Euler(0f, keep.eulerAngles.y, 0f);
            foreach (var r in rs) if (!(r is ParticleSystemRenderer)) minY = Mathf.Min(minY, r.bounds.min.y);
            transform.rotation = keep;
            return minY == float.MaxValue ? 0f : Mathf.Max(0f, transform.position.y - minY);
        }

        public void Stash(Vector3 pos)
        {
            IsHeld = false;
            grabT = 1f;
            transform.SetParent(null, true);
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            SetCols(true);
            Body.isKinematic = true;
            Stashed = true;
        }

        public void Unstash(Vector3 velocityChange)
        {
            Stashed = false;
            Body.isKinematic = false;
            Body.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        protected void SetCols(bool on)
        {
            if (cols == null) return;
            foreach (var c in cols) if (c) c.enabled = on;
        }

        public virtual void HeldUpdate(PlayerController p) { }
        public virtual bool UseHeld(PlayerController p, float dt) => false;
    }
}
