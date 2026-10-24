using DreamRush.Player;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Gameplay layer on top of the DreamRush PF_Player (PlayerMotor3D + CharacterController).
    /// Interacting (tap / hold), carrying with real grab & place animations, a placement ghost,
    /// wet-floor grip, slipping & falling, shoving props when running.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor3D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Rig")]
        public Transform fallPivot;
        public Transform holdPoint;
        public Transform dragPoint;
        public AppleProceduralAnimator anim;
        public KidActionAnimator actions;
        public Material ghostMaterial;

        [Header("Wet floor")]
        public float wetGrip = 0.12f;
        public float wetRunFallChance = 0.85f;
        public float wetWalkFallChance = 0.05f;

        public bool controlsEnabled;

        public PlayerMotor3D Motor { get; private set; }
        public Pickup Held { get; private set; }
        public Interactable Target { get; private set; }
        public bool Scrubbing { get; set; }
        public float WaterDepth { get; private set; }
        public bool OnWet => WaterDepth > 0.0009f;
        public bool Stunned => stun > 0f;
        /// Flat on the floor as a plain fruit (slip / punch / knock-back). The last RecoverTime of the
        /// stun is the limbs-pop-out-and-hop-up beat played by KidBody.
        public bool KnockedDown => knocked && stun > RecoverTime;
        public const float RecoverTime = 0.5f;
        public KidBody Body { get; private set; }
        public int Falls { get; private set; }
        public float HoldProgress { get; private set; }
        public float PlanarSpeed => Motor ? Motor.PlanarVelocity.magnitude : 0f;
        public bool Sprinting => Motor && Motor.IsSprinting;
        public bool PlaceValid { get; private set; }
        public Vector3 PlacePos { get; private set; }
        public bool PlaceAtHome { get; private set; }

        float stun, fallAnim, splashCd, crashCd, turnCheck, actionLock, throwT = -1f;
        bool knocked;
        Interactable holdTarget;
        Vector3 lastVel, faceDir;
        float faceT;
        bool scrubbedLastFrame;
        Transform ghost;
        Renderer ghostR;
        MaterialPropertyBlock mpb;

        public static readonly System.Collections.Generic.List<PlayerController> AllPlayers = new System.Collections.Generic.List<PlayerController>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetPlayers() { AllPlayers.Clear(); }
        /// False for other players' avatars in online co-op (they are driven by the network).
        public bool isLocal = true;
        void OnEnable() { if (!AllPlayers.Contains(this)) AllPlayers.Add(this); }
        void OnDisable() { AllPlayers.Remove(this); }

        void Awake()
        {
            Motor = GetComponent<PlayerMotor3D>();
            if (!anim) anim = GetComponentInChildren<AppleProceduralAnimator>();
            if (!actions) actions = GetComponent<KidActionAnimator>();
            mpb = new MaterialPropertyBlock();
            if (!GetComponent<KidSolidBody>()) gameObject.AddComponent<KidSolidBody>();
            if (!GetComponent<KidRoughhouse>()) gameObject.AddComponent<KidRoughhouse>();
            Body = GetComponent<KidBody>(); if (!Body && fallPivot) Body = gameObject.AddComponent<KidBody>();
            if (isLocal) KidLook.RememberApple(this);
        }

        void KnockDown(float time)
        {
            stun = Mathf.Max(stun, time);
            knocked = true;
        }

        void Start()
        {
            if (Camera.main) Motor.CameraTransform = Camera.main.transform;
            // wear the fruit picked in the menu / lobby (remote avatars get theirs from the network)
            if (isLocal && GameManager.I && GameManager.I.player == this && KidLook.Of(this) != KidLook.Selected) KidLook.Apply(this, KidLook.Selected);
            if (ghostMaterial)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(g.GetComponent<Collider>());
                g.name = "PlaceGhost";
                ghost = g.transform;
                ghost.localScale = new Vector3(0.5f, 0.004f, 0.5f);
                ghostR = g.GetComponent<Renderer>();
                ghostR.sharedMaterial = ghostMaterial;
                ghostR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.SetActive(false);
            }
        }

        public void SetSleeping()
        {
            if (anim) anim.sleeping = true;
            if (Motor) Motor.InputEnabled = false;
        }

        public void WakeUp() { if (anim) anim.sleeping = false; }

        /// Remote player's avatar (online co-op): pose comes from the network; just keep the held item in the hands.
        public void RemoteTick(float speed, bool sprint, bool stunned) => RemoteTick(speed, sprint, stunned, false);
        public void RemoteTick(float speed, bool sprint, bool stunned, bool knockedDown)
        {
            // the owner decides when it is down; KidBody plays the curl / pop-up from this flag
            if (knockedDown) { knocked = true; stun = Mathf.Max(stun, RecoverTime + 0.15f); }
            else if (knocked && stun > RecoverTime) stun = RecoverTime;
            remoteSpeed = speed;
            // feed the procedural animator: velocity from the network pose
            float dt0 = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 v = (transform.position - remoteLast) / dt0; v.y = 0f; remoteLast = transform.position;
            remoteVel = Vector3.Lerp(remoteVel, v, 1f - Mathf.Exp(-10f * dt0));
            if (Motor)
            {
                if (planarField == null) planarField = typeof(PlayerMotor3D).GetField("_planar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                planarField?.SetValue(Motor, remoteVel);
                typeof(PlayerMotor3D).GetProperty("IsGrounded").SetValue(Motor, true); typeof(PlayerMotor3D).GetProperty("IsSprinting").SetValue(Motor, sprint);
            }
            if (Held != null) Held.TickHeldPose(Time.deltaTime);
            stun = stunned ? Mathf.Max(stun, 0.1f) : stun;
            AnimateFall(Time.deltaTime);
        }
        public float remoteSpeed;
        Vector3 remoteLast, remoteVel; static System.Reflection.FieldInfo planarField;

        void Update()
        {
            if (!isLocal) { if (stun > 0f) stun -= Time.deltaTime; if (stun <= 0f) knocked = false; return; }
            float dt = Time.deltaTime;
            scrubbedLastFrame = Scrubbing;
            Scrubbing = false;
            WaterDepth = FloodSim.I ? FloodSim.I.DepthAt(transform.position) : 0f;
            if (stun > 0f) stun -= dt;
            if (stun <= 0f) knocked = false;
            if (crashCd > 0f) crashCd -= dt;
            if (splashCd > 0f) splashCd -= dt;
            if (actionLock > 0f) actionLock -= dt;

            bool inMini = MiniGame.I && MiniGame.I.Active;
            bool canMove = controlsEnabled && !Stunned && !inMini && !MamaSettings.Open && !MamaHowToPlay.Open;
            bool holdingAction = HoldProgress > 0f;
            Motor.InputEnabled = canMove;
            Motor.AllowJump = canMove && Held == null;
            Motor.GroundGripScale = OnWet ? Mathf.Lerp(1f, wetGrip, Mathf.InverseLerp(0.0009f, 0.003f, WaterDepth)) : 1f;
            float mult = Held ? Held.SpeedMultiplier : 1f;
            if (scrubbedLastFrame) mult *= Held && Held.tool == ToolType.Mop ? 0.75f : 0.6f;
            if (actionLock > 0f) mult *= 0.25f;
            mult *= SplatMess.StickyAt(transform.position);
            if (holdingAction) mult = 0f;
            Motor.MoveSpeedScale = mult;
            Motor.AllowSprint = !(Held && (Held.dragOnFloor || Held.tool == ToolType.Bucket));

            UpdateTarget();
            if (inMini && MiniGame.I.Target) Face(MiniGame.I.Target.InteractPoint);
            if (canMove)
            {
                HandleInteract(dt);
                if (MamaInput.DropDown && Held != null) Drop(false);
                if (MamaInput.ThrowDown && Held != null && throwT < 0f) { throwT = 0.16f; if (actions) actions.Play(KidActionAnimator.Clip.Throw, 0.5f); }
                if (Held != null && MamaInput.UseHeld && throwT < 0f) Held.UseHeld(this, dt);
                if (Held != null) { Held.TickHeldPose(dt); Held.HeldUpdate(this); }
                WetFloorRisk(dt);
            }
            else HoldProgress = 0f;

            if (throwT >= 0f) { throwT -= dt; if (throwT < 0f && Held != null) Drop(true); }

            FaceTowards(dt);
            UpdateGhost();

            if (OnWet && PlanarSpeed > 1.5f && splashCd <= 0f && FloodSim.I)
            {
                splashCd = Sprinting ? 0.18f : 0.32f;
                FloodSim.I.Splash(transform.position + Vector3.up * 0.05f, Sprinting ? 1f : 0.5f);
                MamaAudio.Play("step", transform.position, Sprinting ? 0.5f : 0.3f, Random.Range(0.9f, 1.2f));
            }

            AnimateFall(dt);
            lastVel = Motor.PlanarVelocity;
        }

        // ------------------------------------------------------------------ interaction
        void HandleInteract(float dt)
        {
            if (Target == null) { HoldProgress = 0f; holdTarget = null; return; }
            float need = Target.HoldTime(this);
            if (need <= 0f)
            {
                HoldProgress = 0f;
                if (MamaInput.InteractDown && actionLock <= 0f)
                {
                    var t = Target;
                    Face(t.InteractPoint);
                    var mg = t.MiniGameKind(this);
                    if (mg != MiniGame.Kind.None && MiniGame.I) { MiniGame.I.Begin(mg, t, this, t.MiniGameTitle); return; }
                    // fish back into the patched tank: a local drop, so the normal pickup sync carries it
                    if (t is AquariumHazard aqh && aqh.Patched && Held is Fish) { Drop(false); return; }
                    PlayClipFor(t);
                    NetGame.Interact(t, this);
                }
                return;
            }
            if (MamaInput.InteractHeld)
            {
                if (holdTarget != Target) { holdTarget = Target; HoldProgress = 0f; }
                Face(Target.InteractPoint);
                if (actions) actions.Hold(HoldClipFor(Target));
                HoldProgress += dt / need;
                Target.HoldTick(this, Mathf.Clamp01(HoldProgress));
                if (HoldProgress >= 1f)
                {
                    HoldProgress = 0f;
                    holdTarget = null;
                    NetGame.Interact(Target, this);
                }
            }
            else { HoldProgress = 0f; holdTarget = null; }
        }

        public KidActionAnimator.Clip HoldClipFor(Interactable t)
        {
            if (t is MainValve) return KidActionAnimator.Clip.Crank;
            if (t is Drain) return KidActionAnimator.Clip.Dig;
            if (t is SmokeAlarm) return KidActionAnimator.Clip.PokeUp;
            if (t is WaterTap) return KidActionAnimator.Clip.Scoop;
            if (t is Stove) return KidActionAnimator.Clip.Twist;
            if (t is WaterDump) return KidActionAnimator.Clip.Scoop;
            return KidActionAnimator.Clip.Press;
        }

        void PlayClipFor(Interactable t)
        {
            if (!actions) return;
            var C = KidActionAnimator.Clip.None;
            float dur = 0.5f;
            if (t is Pickup pk && !(t is Bucket b0 && Held is Mop) && !(t is CookPot && Held != null) && !(t is Fan))
            {
                if (pk is CookPot cp && cp.Fire && cp.Fire.burning) { C = KidActionAnimator.Clip.Slam; }
                else
                {
                    C = pk.transform.position.y - transform.position.y < 0.45f ? KidActionAnimator.Clip.PickLow : KidActionAnimator.Clip.PickHigh;
                    dur = 0.42f; actionLock = 0.22f;
                }
            }
            else if (t is CookPot) C = KidActionAnimator.Clip.Slam;
            else if (t is Window) { C = KidActionAnimator.Clip.PushUp; dur = 0.7f; }
            else if (t is WaterTap) C = KidActionAnimator.Clip.Twist;
            else if (t is Fan) C = KidActionAnimator.Clip.Press;
            else if (t is Cabinet cab) C = Held != null && !Held.dragOnFloor ? KidActionAnimator.Clip.ReachIn : KidActionAnimator.Clip.Pull;
            else if (t is Bucket) { C = KidActionAnimator.Clip.Wring; dur = 0.7f; }
            else if (t is WaterDump)
            {
                if (Held is Mop || Held is Towel) { C = KidActionAnimator.Clip.Wring; dur = 0.75f; }
                else if (Held is Bucket) { C = KidActionAnimator.Clip.Pour; dur = 0.7f; }
                else C = KidActionAnimator.Clip.ReachIn;
            }
            else C = KidActionAnimator.Clip.Press;
            if (C == KidActionAnimator.Clip.Wring || C == KidActionAnimator.Clip.Pour) actionLock = dur * 0.8f;
            actions.Play(C, dur);
        }

        void Face(Vector3 p)
        {
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            faceDir = d.normalized; faceT = 0.35f;
        }

        void FaceTowards(float dt)
        {
            if (faceT <= 0f) return;
            faceT -= dt;
            if (MamaInput.Move.sqrMagnitude > 0.1f && HoldProgress <= 0f) { faceT = 0f; return; }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(faceDir), 720f * dt);
        }

        void WetFloorRisk(float dt)
        {
            if (!OnWet || !Motor.IsGrounded) return;
            float spd = PlanarSpeed;
            float chance = 0f;
            if (Sprinting && spd > 3.5f) chance = wetRunFallChance;
            else if (spd > 2.2f) chance = wetWalkFallChance;
            if (Held && Held.tool == ToolType.Bucket) chance += 0.25f;
            chance *= Mathf.InverseLerp(0.0009f, 0.0025f, WaterDepth);
            if (Random.value < chance * dt) { Slip(null); return; }

            turnCheck -= dt;
            if (turnCheck <= 0f)
            {
                turnCheck = 0.1f;
                Vector3 v = Motor.PlanarVelocity;
                if (lastVel.magnitude > 4f && v.magnitude > 1f && Vector3.Angle(lastVel, v) > 75f)
                    Slip("Sharp turn on a wet floor... WHOA!");
            }
        }

        void UpdateTarget()
        {
            Target = null;
            if (!controlsEnabled || Stunned || (MiniGame.I && MiniGame.I.Active)) return;
            // keep the current hold target while E is held so turning a valve isn't interrupted
            if (holdTarget && MamaInput.InteractHeld && holdTarget.CanInteract(this) && HeldInteraction.Allows(this, holdTarget)) { Target = holdTarget; return; }
            float best = float.MaxValue;
            Vector3 pos = transform.position, fwd = transform.forward;
            foreach (var it in Interactable.All)
            {
                if (!it || !it.isActiveAndEnabled) continue;
                if (Held != null && ReferenceEquals(it, Held)) continue;
                if (!it.CanInteract(this) || !HeldInteraction.Allows(this, it)) continue;
                Vector3 d = it.InteractPoint - pos; d.y = 0f;
                float dist = d.magnitude;
                if (dist > it.interactRadius) continue;
                float dot = dist > 0.01f ? Vector3.Dot(fwd, d / dist) : 1f;
                if (dot < -0.2f) continue;
                float score = dist * 0.85f - dot * 1.3f - it.Priority(this);
                // A held fire tool should select the burning pot, rather than the closer
                // stove knob underneath it. Otherwise E misleadingly turns the gas back on.
                if (it is CookPot cp && cp.Fire && cp.Fire.burning && (Held is Lid || Held is Towel)) score -= 1.25f;
                if (score < best) { best = score; Target = it; }
            }
        }

        // ------------------------------------------------------------------ placement ghost
        void UpdateGhost()
        {
            PlaceValid = false; PlaceAtHome = false;
            if (Held == null || Held.dragOnFloor || !controlsEnabled)
            {
                if (ghost && ghost.gameObject.activeSelf) ghost.gameObject.SetActive(false);
                return;
            }
            PlaceValid = FindPlaceSpot(out Vector3 pos, out bool home);
            PlacePos = pos; PlaceAtHome = home;
            if (!ghost) return;
            ghost.gameObject.SetActive(PlaceValid);
            if (!PlaceValid) return;
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            float size = home ? 0.62f : 0.48f;
            ghost.position = pos + Vector3.up * 0.006f;
            ghost.localScale = new Vector3(size * pulse, 0.004f, size * pulse);
            mpb.SetColor("_BaseColor", home ? new Color(0.35f, 1f, 0.45f, 0.6f) : new Color(0.3f, 0.85f, 0.95f, 0.45f));
            ghostR.SetPropertyBlock(mpb);
        }

        /// <summary>Where the held item will land when placed: the surface right in front (table, counter, floor).</summary>
        public bool FindPlaceSpot(out Vector3 pos, out bool home)
        {
            home = false;
            if (Held is MessyProp mp && mp.NearHome(this))
            {
                pos = mp.homePosition; home = true;
                return true;
            }
            Vector3 origin = transform.position + transform.forward * 0.75f + Vector3.up * 1.4f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 2.2f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; pos = transform.position + transform.forward * 0.75f;
            bool ok = false;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (Held && h.collider.transform.IsChildOf(Held.transform)) continue;
                if (h.collider.attachedRigidbody && !h.collider.attachedRigidbody.isKinematic) continue;
                if (h.normal.y < 0.7f) continue;
                if (h.distance < best) { best = h.distance; pos = h.point; ok = true; }
            }
            return ok;
        }

        // ------------------------------------------------------------------ slipping
        public void Slip(string reason)
        {
            if (Stunned) return;
            KnockDown(1.8f);
            fallAnim = 1f;
            Falls++;
            HoldProgress = 0f;
            Vector3 fwd = Motor.PlanarVelocity.sqrMagnitude > 0.1f ? Motor.PlanarVelocity.normalized : transform.forward;
            if (Held != null)
            {
                var h = Held;
                ReleaseHeld();
                h.OnDropped(this, fwd * 1.5f + Vector3.up * 2.2f);
                if (h is Bucket b) b.Spill(1f);
            }
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb.transform.IsChildOf(transform)) continue;
                rb.AddExplosionForce(3f, transform.position, 2f, 0.8f, ForceMode.VelocityChange);
            }
            // Keep momentum: a running slip sends the whole curled fruit skidding.
            Motor.Launch(fwd * Mathf.Clamp(PlanarSpeed * 1.15f, 4f, 7f) + Vector3.up * 1.4f);
            if (FloodSim.I) FloodSim.I.Splash(transform.position, 2.5f);
            MamaAudio.Play("splash", transform.position, 1f, 0.85f); MamaAudio.Play("thud", transform.position, 0.9f);
            if (GameManager.I) GameManager.I.OnPlayerSlipped(reason);
        }

        /// Shoved / bumped by another kid: fly back a bit, maybe drop what you hold.
        public void Bonk(Vector3 dir, float force, float stunTime, bool dropHeld)
        {
            dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = -transform.forward; dir.Normalize();
            stun = Mathf.Max(stun, stunTime);
            if (stunTime > RecoverTime) { KnockDown(stunTime); fallAnim = Mathf.Max(fallAnim, 0.6f); }
            HoldProgress = 0f;
            Motor.Launch(dir * force + Vector3.up * (1.2f + force * 0.25f));
            if (dropHeld && Held != null) { var h = ReleaseHeld(); h.OnDropped(this, dir * 2f + Vector3.up * 2.2f); if (h is Bucket b) b.Spill(0.6f); }
            MamaAudio.Play("thud", transform.position, 0.8f, Random.Range(1.1f, 1.4f));
            if (isLocal && GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.25f);
        }

        public void KnockBack(Vector3 from, float force)
        {
            Vector3 d = transform.position - from; d.y = 0f;
            KnockDown(1.2f);
            Motor.Launch(d.normalized * force + Vector3.up * 2f);
            if (Held != null) { var h = ReleaseHeld(); h.OnDropped(this, Vector3.up); }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.rigidbody;
            if (rb == null || rb.isKinematic) return;
            if (Held && rb == Held.Body) return;
            float spd = PlanarSpeed;
            Vector3 dir = hit.moveDirection; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            dir.Normalize();
            rb.AddForce(dir * Mathf.Clamp(spd, 0.5f, 6f) * 0.25f, ForceMode.VelocityChange);
            if (spd > 4f && crashCd <= 0f)
            {
                crashCd = 0.6f;
                rb.AddForce((dir + Vector3.up * 0.6f) * 3.5f, ForceMode.VelocityChange);
                MamaAudio.Play("thud", transform.position, 0.8f, 1.2f);
                if (GameManager.I) GameManager.I.Toast("CRASH! Watch where you're running!");
            }
        }

        // ------------------------------------------------------------------ carrying
        public void PickUp(Pickup p)
        {
            if (p == null) return;
            if (Held != null) Drop(false);
            Held = p;
            p.OnPickedUp(this);
            string pickupSound=p is Hamster?"hamster_pick":p.displayName=="Teddy bear"?"plush_pick":p.displayName=="Trash bag"?"bag_drop":"pop";
            MamaAudio.Play(pickupSound, transform.position, 0.5f, p.dragOnFloor ? 0.7f : 1.1f);
            if (!p.FollowsOnItsOwn)
            {
                Transform anchor = p.dragOnFloor ? dragPoint : holdPoint;
                p.transform.SetParent(anchor, true);   // keep world pose: TickHeldPose eases it into the hands
            }
            if (GameManager.I) GameManager.I.OnPickedUp(p);
        }

        public Pickup ReleaseHeld()
        {
            var h = Held;
            Held = null;
            if (h) h.transform.SetParent(null, true);
            return h;
        }

        /// <summary>Q = place it gently on the surface in front (or back home). Throwing goes through RMB.</summary>
        public void Drop(bool throwIt)
        {
            if (Held == null) return;
            var h = Held;
            if (throwIt)
            {
                Held = null;
                Vector3 vel = Motor.PlanarVelocity + transform.forward * 6.5f + Vector3.up * 2.8f;
                h.transform.SetParent(null, true);
                h.OnDropped(this, vel);
                MamaAudio.Play("whoosh", transform.position, 0.5f, 1.4f);
                if (h is Bucket b) b.OnThrown(this);
                return;
            }

            if (h.dragOnFloor || h.FollowsOnItsOwn)
            {
                Held = null;
                h.transform.SetParent(null, true);
                h.OnDropped(this, Vector3.zero);
                return;
            }

            // trash near a bin: lid pops open and it goes straight in
            var bin = TrashBin.For(this);
            if (bin) { if (actions) actions.Play(KidActionAnimator.Clip.PlaceDown, 0.4f); actionLock = 0.2f; NetGame.Interact(bin, this); return; }

            bool ok = FindPlaceSpot(out Vector3 spot, out bool home);
            Held = null;
            h.transform.SetParent(null, true);
            if (actions) actions.Play(KidActionAnimator.Clip.PlaceDown, 0.4f);
            actionLock = 0.2f;
            if (ok)
            {
                if (home && h is MessyProp mp)
                {
                    h.transform.SetPositionAndRotation(mp.homePosition, mp.homeRotation);
                    MamaAudio.Play("good", null, 0.45f, 1.2f);
                }
                else
                {
                    float bottom = h.BottomOffset();
                    h.transform.SetPositionAndRotation(spot + Vector3.up * (bottom + 0.01f), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                }
            }
            MamaAudio.Play("place", h.transform.position, 0.7f);
            h.OnDropped(this, Vector3.zero);
        }

        void AnimateFall(float dt)
        {
            if (!fallPivot || (Body && Body.enabled)) return;   // KidBody owns the pivot (curl into a fruit / pop up)
            float target = Stunned ? 1f : 0f;
            fallAnim = Mathf.MoveTowards(fallAnim, target, dt * (Stunned ? 8f : 2.2f));
            float k = fallAnim;
            fallPivot.localRotation = Quaternion.Euler(-85f * k, 0f, 0f);
            fallPivot.localPosition = new Vector3(0f, 0.2f * k, -0.25f * k);
        }
    }
}
