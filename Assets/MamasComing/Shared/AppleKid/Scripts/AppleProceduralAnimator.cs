using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Procedural animation for the rigid-part apple (Body, Leaf, Arm_L/R, Leg_L/R — see blender-asset-pipeline).
    /// v2 "goofy" set (user request 2026-10-17, docs/RESEARCH_V2.md §6): waddle walk with wide arm swings,
    /// cartoon wheel-run with windmilling arms and a big forward lean, skids, flapping/panicking in the air,
    /// idle fidgets, and a cheeky starfish sleep with a snot bubble. Reads motor state only; never delays gameplay.
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class AppleProceduralAnimator : MonoBehaviour
    {
        public PlayerMotor3D motor;

        [Header("Walk (waddle)")]
        [Tooltip("Metres travelled per full step cycle at walk speed.")]
        public float strideLength = 0.95f;
        public float legSwingWalk = 30f;          // M18 natural walk (was a 40° goofy waddle)
        public float bodyBob = 0.035f;
        public float bodyRoll = 5f;
        public float bodyTwist = 5f;
        public float armSwing = 34f;
        [Tooltip("Arms trail the legs by this much of a cycle (overlapping action).")]
        public float armLag = 0.35f;
        [Tooltip("Degrees of forward lean per m/s² of acceleration (back when braking).")]
        public float accelLean = 1.4f;
        public float walkFootLift = 0.045f;
        public float armOpenMoving = 34f;

        [Header("Sprint (cartoon wheel run)")]
        public float sprintStrideLength = 0.8f;
        public float legSwingSprint = 78f;
        public float sprintLean = 24f;
        public float windmillSpeed = 2.2f;   // arm turns per step cycle

        [Header("Air")]
        public float armOpenAir = 95f;
        public float airLegTuck = 28f;

        [Header("Idle")]
        public float breathRate = 0.45f;
        public float breathAmount = 0.014f;
        public float fidgetAfter = 6f;

        [Header("Leaf follow-through")]
        public float leafLag = 0.014f;
        public float leafStiffness = 80f;
        public float leafDamping = 7f;
        public float leafMaxAngle = 45f;

        [Header("States set by cutscenes")]
        public bool sleeping;
        [Tooltip("Sitting on the chair: legs stick out forward.")]
        public bool sitting;
        /// <summary>Forward body pitch in degrees (grandpa transformation).</summary>
        public float hunch;
        public float sleepArmOpen = 100f;

        [Header("Blending")]
        public float blendSpeed = 10f;

        Transform _body, _leaf, _armL, _armR, _legL, _legR;
        float _accelS, _lastSpeed, _leanVel;
        Vector3 _bodyPos, _bodyScale = Vector3.one, _leafPos, _armLPos, _armRPos, _legLPos, _legRPos;
        Quaternion _bodyRot, _leafRot, _armLRot, _armRRot, _legLRot, _legRRot;
        // v4 two-segment limbs (elbows / knees); null on older models → rigid limbs as before
        Transform _armLLo, _armRLo, _legLLo, _legRLo;
        Vector3 _armLLoPos, _armRLoPos, _legLLoPos, _legRLoPos;
        Quaternion _armLLoRot, _armRLoRot, _legLLoRot, _legRLoRot;
        public bool HasJoints => _legLLo != null;
        float _kneeL, _kneeR, _elbowL, _elbowR;
        float _phase, _move01, _air01, _sprint01, _skid01, _panic01, _flap;
        float _idleTime, _fallTime, _hunchNow, _lean, _bank, _lastYaw;
        Vector2 _leafAngle, _leafVel;
        Vector3 _lastVel;

        // sleep
        Transform _bubble;
        float _sleepT, _twitchTimer = 3f, _scratchTimer = 5f;

        public bool HasRig => _body != null;
        public float Phase => _phase;
        public float Move01 => _move01;
        public float Air01 => _air01;
        public float Sprint01 => _sprint01;
        public bool IsSkidding => _skid01 > 0.5f;

        void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor3D>();
            Bind();
        }

        public void Bind()
        {
            _body = FindDeep(transform, "Body");
            _leaf = FindDeep(transform, "Leaf");
            _armL = FindDeep(transform, "Arm_L");
            _armR = FindDeep(transform, "Arm_R");
            _legL = FindDeep(transform, "Leg_L");
            _legR = FindDeep(transform, "Leg_R");
            if (_body != null) { _bodyPos = _body.localPosition; _bodyRot = _body.localRotation; _bodyScale = _body.localScale; }
            if (_leaf != null) { _leafRot = _leaf.localRotation; _leafPos = _leaf.localPosition; }
            if (_armL != null) { _armLRot = _armL.localRotation; _armLPos = _armL.localPosition; }
            if (_armR != null) { _armRRot = _armR.localRotation; _armRPos = _armR.localPosition; }
            if (_legL != null) { _legLRot = _legL.localRotation; _legLPos = _legL.localPosition; }
            if (_legR != null) { _legRRot = _legR.localRotation; _legRPos = _legR.localPosition; }
            _armLLo = FindDeep(transform, "Arm_L_Lower"); _armRLo = FindDeep(transform, "Arm_R_Lower");
            _legLLo = FindDeep(transform, "Leg_L_Lower"); _legRLo = FindDeep(transform, "Leg_R_Lower");
            if (_armLLo != null) { _armLLoPos = _armLLo.localPosition; _armLLoRot = _armLLo.localRotation; }
            if (_armRLo != null) { _armRLoPos = _armRLo.localPosition; _armRLoRot = _armRLo.localRotation; }
            if (_legLLo != null) { _legLLoPos = _legLLo.localPosition; _legLLoRot = _legLLo.localRotation; }
            if (_legRLo != null) { _legRLoPos = _legRLo.localPosition; _legRLoRot = _legRLo.localRotation; }
        }

        /// <summary>Lower limb follows its upper limb (flat FBX), bent at the joint by <paramref name="bendX"/> degrees.</summary>
        static void Follow(Transform lower, Vector3 lowerRestPos, Quaternion lowerRestRot, Transform upper, Vector3 upperRestPos, Quaternion upperRestRot, float bendX)
        {
            if (lower == null || upper == null) return;
            Quaternion d = upper.localRotation * Quaternion.Inverse(upperRestRot);
            lower.localPosition = upper.localPosition + d * (lowerRestPos - upperRestPos);
            lower.localRotation = d * lowerRestRot * Quaternion.Euler(bendX, 0f, 0f);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        void LateUpdate()
        {
            if (motor == null || _body == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float t = Time.time;

            var cfg = motor.Config;
            Vector3 planar = motor.PlanarVelocity;
            float speed = planar.magnitude;
            bool grounded = motor.IsGrounded;
            float vy = motor.VerticalVelocity;

            float k = 1f - Mathf.Exp(-blendSpeed * dt);
            _move01 = Mathf.Lerp(_move01, grounded ? Mathf.Clamp01(speed / Mathf.Max(0.1f, cfg.walkSpeed)) : _move01 * 0.5f, k);
            _air01 = Mathf.Lerp(_air01, grounded ? 0f : 1f, 1f - Mathf.Exp(-blendSpeed * 1.5f * dt));
            float run01Target = Mathf.Clamp01((speed - cfg.walkSpeed * 0.9f) / Mathf.Max(0.1f, cfg.sprintSpeed - cfg.walkSpeed * 0.9f));
            _sprint01 = Mathf.Lerp(_sprint01, grounded ? run01Target : _sprint01, k);

            // skid: moving fast while the stick points the other way (or hard braking) → plant feet, lean back
            Vector3 intent = motor.MoveIntent;
            bool skid = grounded && speed > cfg.walkSpeed * 0.8f &&
                        ((intent.sqrMagnitude > 0.2f && Vector3.Dot(intent.normalized, planar / Mathf.Max(0.01f, speed)) < -0.3f) ||
                         (intent.sqrMagnitude < 0.01f && speed > cfg.walkSpeed * 1.2f));
            _skid01 = Mathf.MoveTowards(_skid01, skid ? 1f : 0f, dt * (skid ? 10f : 4f));

            // long fall → panic flail
            _fallTime = (!grounded && vy < -4f) ? _fallTime + dt : 0f;
            _panic01 = Mathf.MoveTowards(_panic01, _fallTime > 0.35f ? 1f : 0f, dt * 5f);

            float stride = Mathf.Lerp(strideLength, sprintStrideLength, _sprint01);
            if (grounded) _phase += speed / stride * Mathf.PI * 2f * dt;
            else _phase += dt * Mathf.PI * 2f * (0.8f + _panic01 * 3f); // keep pedalling in the air
            _phase %= Mathf.PI * 2000f;
            float s = Mathf.Sin(_phase), c = Mathf.Cos(_phase);

            // turn banking (lean into turns)
            float yaw = motor.transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
            _lastYaw = yaw;
            _bank = Mathf.Lerp(_bank, Mathf.Clamp(-yawRate * 0.05f, -16f, 16f) * Mathf.Clamp01(speed / cfg.walkSpeed), 1f - Mathf.Exp(-8f * dt));

            // idle fidget clock
            _idleTime = (grounded && speed < 0.2f && !sleeping && !sitting) ? _idleTime + dt : 0f;

            if (sleeping) { SleepPose(dt, t); return; }
            if (_bubble != null) PopBubble();

            // ---------------------------------------------------------------- legs
            float legAmp = Mathf.Lerp(legSwingWalk, legSwingSprint, _sprint01) * _move01 * (1f - _air01);
            // wheel run: legs swing AND lift on the back-swing so the feet draw circles (cartoon blur)
            float liftK = Mathf.Lerp(walkFootLift * _move01, 0.09f, _sprint01) * (1f - _air01);
            float liftL = Mathf.Max(0f, c) * liftK, liftR = Mathf.Max(0f, -c) * liftK;   // the swing foot clears the ground
            float tuck = airLegTuck * _air01;
            float bicycle = Mathf.Sin(_phase * 2f) * 45f * _panic01;
            float legL = -s * legAmp - tuck - bicycle;
            float legR = s * legAmp - tuck * 0.4f + bicycle;
            float splay = 0f;
            if (_skid01 > 0f) { legL = Mathf.Lerp(legL, -35f, _skid01); legR = Mathf.Lerp(legR, -28f, _skid01); }
            if (sitting) { float kick = Mathf.Sin(t * 9f) * 10f; legL = -80f + kick; legR = -80f - kick; }
            if (_idleTime > 0f) splay = Mathf.Sin(t * 0.7f) * 2f;
            // knees (v4): fold while the leg swings forward (recovery), tuck in the air, deep crouch on skids
            float kneeAmp = Mathf.Lerp(38f, 95f, _sprint01) * _move01 * (1f - _air01);
            float kneeL = Mathf.Max(0f, c) * kneeAmp + 6f + _air01 * (vy > 0f ? 70f : 25f) + _skid01 * 35f;
            float kneeR = Mathf.Max(0f, -c) * kneeAmp + 6f + _air01 * (vy > 0f ? 45f : 15f) + _skid01 * 30f;
            if (_panic01 > 0f) { kneeL += Mathf.Max(0f, Mathf.Sin(_phase * 2f)) * 70f * _panic01; kneeR += Mathf.Max(0f, -Mathf.Sin(_phase * 2f)) * 70f * _panic01; }
            if (sitting) { kneeL = 75f; kneeR = 75f; }
            if (HasJoints)
            {
                // with a real knee the thigh can swing further forward and the lift comes from the bend
                legL -= Mathf.Max(0f, c) * 12f * _sprint01; legR -= Mathf.Max(0f, -c) * 12f * _sprint01;
                liftL *= 0.4f; liftR *= 0.4f;
            }
            _kneeL = Mathf.Lerp(_kneeL, kneeL, 1f - Mathf.Exp(-20f * dt)); _kneeR = Mathf.Lerp(_kneeR, kneeR, 1f - Mathf.Exp(-20f * dt));
            SetLeg(_legL, _legLPos, _legLRot, legL, liftL, splay);
            SetLeg(_legR, _legRPos, _legRRot, legR, liftR, -splay);
            Follow(_legLLo, _legLLoPos, _legLLoRot, _legL, _legLPos, _legLRot, _kneeL);
            Follow(_legRLo, _legRLoPos, _legRLoRot, _legR, _legRPos, _legRRot, _kneeR);

            // ---------------------------------------------------------------- body
            float breath = Mathf.Sin(t * breathRate * Mathf.PI * 2f) * breathAmount * (1f - _move01);
            // M18: the body is HIGHEST at the passing pose and dips just after each heel contact (it used to peak at contact,
            // which read as a hop). Two dips per cycle; a small delay gives the "down" pose weight.
            float bob = (0.5f + 0.5f * Mathf.Cos(2f * _phase - 0.5f)) * Mathf.Lerp(bodyBob, bodyBob * 1.7f, _sprint01) * _move01 * (1f - _air01);
            // acceleration lean (spring-smoothed): tip into a start, rock back on a stop
            _accelS = Mathf.Lerp(_accelS, (speed - _lastSpeed) / dt, 1f - Mathf.Exp(-6f * dt)); _lastSpeed = speed;
            float accLean = Mathf.Clamp(_accelS * accelLean, -10f, 12f) * (1f - _air01);
            float lean = Mathf.Lerp(0f, sprintLean, _sprint01) * _move01 - _skid01 * 22f + _air01 * (vy > 0 ? -6f : 8f) + accLean;
            // critically-damped-ish spring so stops get a tiny settle instead of a snap
            float leanForce = (lean - _lean) * 160f - _leanVel * 18f; _leanVel += leanForce * dt; _lean += _leanVel * dt;
            _hunchNow = Mathf.Lerp(_hunchNow, hunch, k);
            float roll = c * Mathf.Lerp(bodyRoll, bodyRoll * 0.6f, _sprint01) * _move01 * (1f - _air01) + _bank;
            float twist = s * bodyTwist * _move01 * (1f - _sprint01 * 0.5f);
            float fidgetYaw = 0f, fidgetRoll = 0f, yawn = 0f;
            if (_idleTime > fidgetAfter)
            {
                float ft = _idleTime - fidgetAfter;
                float cycle = ft % 9f;
                if (cycle < 3f) fidgetYaw = Mathf.Sin(cycle / 3f * Mathf.PI * 2f) * 28f;                      // look around
                else if (cycle < 4.6f) yawn = Mathf.Sin((cycle - 3f) / 1.6f * Mathf.PI);                         // big yawn
                else if (cycle < 6f) fidgetRoll = Mathf.Sin((cycle - 4.6f) * 14f) * 4f;                          // wiggle
            }
            Vector3 bodyOffset = Vector3.up * (bob + breath + yawn * 0.03f);
            Quaternion bodyDelta = Quaternion.Euler(_lean + _hunchNow - yawn * 12f, twist + fidgetYaw, roll + fidgetRoll);
            _body.localPosition = _bodyPos + bodyOffset;
            _body.localRotation = bodyDelta * _bodyRot;
            _body.localScale = _bodyScale;

            // ---------------------------------------------------------------- arms
            float open = Mathf.Max((HasJoints ? 12f : armOpenMoving) * _move01, armOpenAir * _air01);   // v4: arms swing down by the body, not T-posed
            float swing = armSwing * _move01 * (1f - _air01) * (1f - _sprint01);
            float lA, rA;
            // walk: big opposite swings; sprint: windmills (continuous circles); air: flap / panic flail
            float wind = _phase * windmillSpeed * Mathf.Rad2Deg;
            if (HasJoints)
            {
                // v4: athletic run — shoulders pump opposite the legs, elbows bent ~90° (anime sprint), loose at walk
                float pump = Mathf.Lerp(swing, 62f, _sprint01) * _move01 * (1f - _air01);
                float sa = Mathf.Sin(_phase - armLag * (1f - _sprint01));   // relaxed arms trail; sprinting arms drive in sync
                lA = sa * pump; rA = -sa * pump;
            }
            else
            {
                lA = Mathf.Lerp(s * swing, -wind, _sprint01 * (1f - _air01));
                rA = Mathf.Lerp(-s * swing, -wind - 180f, _sprint01 * (1f - _air01));
            }
            if (_air01 > 0.01f)
            {
                bool nearPeak = Mathf.Abs(vy) < 3.5f;
                _flap = Mathf.Lerp(_flap, nearPeak ? 1f : 0f, 1f - Mathf.Exp(-12f * dt));
                float flap = Mathf.Sin(t * 26f) * 38f * _flap;
                float up = -60f * _air01;
                float panic = Mathf.Sin(t * 30f) * 70f * _panic01;
                lA = Mathf.Lerp(lA, up + flap + panic, _air01);
                rA = Mathf.Lerp(rA, up + flap - panic, _air01);
            }
            if (_skid01 > 0f) { lA = Mathf.Lerp(lA, -130f, _skid01); rA = Mathf.Lerp(rA, -130f, _skid01); open = Mathf.Lerp(open, 60f, _skid01); }
            if (yawn > 0f) { lA = Mathf.Lerp(lA, -150f, yawn); rA = Mathf.Lerp(rA, -150f, yawn); open = Mathf.Lerp(open, 40f, yawn); }
            if (sitting) open = 25f;
            float flailOpen = open + Mathf.Sin(_phase * 0.5f) * 12f * _sprint01;
            if (HasJoints) flailOpen = Mathf.Lerp(open, 14f, _sprint01 * (1f - _air01));   // pumping arms stay close to the body
            if (_armL != null) Attach(_armL, _armLPos, _armLRot, bodyOffset, bodyDelta, Quaternion.Euler(lA, 0f, Side(_armLPos) * flailOpen));
            if (_armR != null) Attach(_armR, _armRPos, _armRRot, bodyOffset, bodyDelta, Quaternion.Euler(rA, 0f, Side(_armRPos) * flailOpen));
            // elbows: relaxed 18° at idle, swinging with the stride when walking, 95° pumping when sprinting, flung open in the air
            float elbowBase = Mathf.Lerp(18f + 14f * _move01, 95f, _sprint01 * (1f - _air01));
            float elbL = elbowBase + Mathf.Max(0f, s) * 25f * _move01 * (1f - _sprint01) - _air01 * 10f;
            float elbR = elbowBase + Mathf.Max(0f, -s) * 25f * _move01 * (1f - _sprint01) - _air01 * 10f;
            if (_idleTime > 0f) { elbL += Mathf.Sin(t * 1.3f) * 4f; elbR -= Mathf.Sin(t * 1.1f) * 4f; }
            _elbowL = Mathf.Lerp(_elbowL, elbL, 1f - Mathf.Exp(-18f * dt)); _elbowR = Mathf.Lerp(_elbowR, elbR, 1f - Mathf.Exp(-18f * dt));
            Follow(_armLLo, _armLLoPos, _armLLoRot, _armL, _armLPos, _armLRot, -_elbowL);
            Follow(_armRLo, _armRLoPos, _armRLoRot, _armR, _armRPos, _armRRot, -_elbowR);

            Leaf(dt, bodyOffset, bodyDelta, speed);
        }

        void SetLeg(Transform leg, Vector3 restPos, Quaternion restRot, float swingX, float lift, float splayZ)
        {
            if (leg == null) return;
            leg.localPosition = restPos + Vector3.up * lift;
            leg.localRotation = restRot * Quaternion.Euler(swingX, 0f, splayZ);
        }

        void Leaf(float dt, Vector3 bodyOffset, Quaternion bodyDelta, float speed)
        {
            if (_leaf == null) return;
            Vector3 vel = motor.Velocity;
            Vector3 accel = (vel - _lastVel) / dt;
            _lastVel = vel;
            Vector3 local = motor.transform.InverseTransformDirection(accel);
            Vector2 target = new Vector2(-local.z, local.x) * leafLag * 60f;
            target += new Vector2(-speed * 2.2f, 0f); // trails while running
            Vector2 force = (target - _leafAngle) * leafStiffness - _leafVel * leafDamping;
            _leafVel += force * dt;
            _leafAngle += _leafVel * dt;
            _leafAngle = Vector2.ClampMagnitude(_leafAngle, leafMaxAngle);
            Attach(_leaf, _leafPos, _leafRot, bodyOffset, bodyDelta, Quaternion.Euler(_leafAngle.x, 0f, _leafAngle.y));
        }

        // ---------------------------------------------------------------- cheeky sleep
        void SleepPose(float dt, float t)
        {
            _sleepT += dt;
            float breathe = Mathf.Sin(t * 1.4f) * 0.5f + 0.5f;                   // slow snore (< 60 bpm, soothing)
            // starfish: arms and legs flung wide
            _twitchTimer -= dt;
            if (_twitchTimer < -1.2f) _twitchTimer = 3.5f + Mathf.PerlinNoise(t, 0.4f) * 3f;
            float twitch = _twitchTimer < 0f ? Mathf.Sin(t * 22f) * 30f : 0f;      // dream-running legs
            SetLeg(_legL, _legLPos, _legLRot, -10f + twitch, 0f, 32f);
            SetLeg(_legR, _legRPos, _legRRot, -10f - twitch, 0f, -32f);

            Vector3 bodyOffset = Vector3.up * (breathe * 0.02f);
            Quaternion bodyDelta = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 3f);
            _body.localPosition = _bodyPos + bodyOffset;
            _body.localRotation = bodyDelta * _bodyRot;
            _body.localScale = Vector3.Scale(_bodyScale, new Vector3(1f + breathe * 0.025f, 1f, 1f + breathe * 0.025f));

            // right arm scratches the belly now and then; left arm flops wide
            _scratchTimer -= dt;
            if (_scratchTimer < -2f) _scratchTimer = 4f + Mathf.PerlinNoise(0.2f, t) * 4f;
            bool scratching = _scratchTimer < 0f;
            float rArm = scratching ? -40f + Mathf.Sin(t * 18f) * 12f : 0f;
            float rOpen = scratching ? 20f : sleepArmOpen;
            if (_armL != null) Attach(_armL, _armLPos, _armLRot, bodyOffset, bodyDelta, Quaternion.Euler(-20f, 0f, Side(_armLPos) * sleepArmOpen));
            if (_armR != null) Attach(_armR, _armRPos, _armRRot, bodyOffset, bodyDelta, Quaternion.Euler(rArm, 0f, Side(_armRPos) * rOpen));
            Follow(_legLLo, _legLLoPos, _legLLoRot, _legL, _legLPos, _legLRot, 10f + Mathf.Max(0f, twitch) * 0.8f);
            Follow(_legRLo, _legRLoPos, _legRLoRot, _legR, _legRPos, _legRRot, 10f + Mathf.Max(0f, -twitch) * 0.8f);
            Follow(_armLLo, _armLLoPos, _armLLoRot, _armL, _armLPos, _armLRot, -15f);
            Follow(_armRLo, _armRLoPos, _armRLoRot, _armR, _armRPos, _armRRot, scratching ? -90f : -15f);
            Leaf(dt, bodyOffset, bodyDelta, 0f);

            // the snot bubble: grows with each snore, wobbles, and shrinks on the out-breath
            if (_bubble == null) CreateBubble();
            if (_bubble != null)
            {
                float size = Mathf.Lerp(0.04f, 0.22f, breathe) * Mathf.Clamp01(_sleepT / 1.5f);
                float wob = 1f + Mathf.Sin(t * 9f) * 0.05f;
                _bubble.localScale = new Vector3(size * wob, size / wob, size);
            }
        }

        void CreateBubble()
        {
            var mf = _body.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var b = mf.sharedMesh.bounds;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SnotBubble";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_body, false);
            go.transform.localPosition = new Vector3(b.center.x + b.size.x * 0.06f, b.center.y - b.size.y * 0.02f, b.max.z + 0.03f);
            go.transform.localScale = Vector3.zero;
            go.GetComponent<MeshRenderer>().sharedMaterial = Core.RuntimeMaterials.CreateUnlitTransparent(new Color(0.75f, 0.95f, 1f, 0.55f), null, "M_SnotBubble");
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bubble = go.transform;
        }

        /// <summary>Pops the sleep bubble (called when the dream transition starts, or on waking).</summary>
        public void PopBubble()
        {
            if (_bubble == null) return;
            Destroy(_bubble.gameObject);
            _bubble = null;
            _sleepT = 0f;
            if (_body != null) _body.localScale = _bodyScale;
        }

        static float Side(Vector3 restPos) => restPos.x >= 0f ? 1f : -1f;

        /// <summary>
        /// Virtual parenting: the FBX hierarchy is flat (see blender-asset-pipeline), so parts that belong to the
        /// body follow the body's animated offset/rotation around the body pivot here.
        /// </summary>
        void Attach(Transform part, Vector3 restPos, Quaternion restRot, Vector3 bodyOffset, Quaternion bodyDelta, Quaternion anim)
        {
            part.localPosition = _bodyPos + bodyDelta * (restPos - _bodyPos) + bodyOffset;
            part.localRotation = bodyDelta * restRot * anim;
        }
    }
}
