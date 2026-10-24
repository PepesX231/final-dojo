using UnityEngine;

namespace MamasComing
{
    /// Chaotic co-op slapstick: left click (empty hands) / G / RB = punch the kid in front of you: they fly back,
    /// curl up into a plain fruit and drop what they carry. Two kids sprinting into each other bounce apart.
    /// Online: the swing animation, the comic word and the sounds play on EVERY screen; the knock-back is applied
    /// by the victim's own machine (which also decides whether they had already dodged).
    [RequireComponent(typeof(PlayerController))]
    public class KidRoughhouse : MonoBehaviour
    {
        public float shoveRange = 1.5f;
        public float shoveForce = 7.5f;
        public float shoveStun = 1.4f;
        public float shoveCooldown = 0.6f;
        const float HitDelay = 0.12f;   // the fist lands mid-swing, not on the click
        public float bumpSpeed = 4.0f;

        PlayerController me;
        float shoveCd, bumpCd;
        // counters / grabbing
        float lastSwingTime = -9f, graceUntil, grabT; bool wasStunned;
        PlayerController grabTarget, carrying; float carryTimer;
        public PlayerController Carrying => carrying;

        static PlayerController NearestTo(Vector3 from, PlayerController not)
        {
            PlayerController best = null; float bd = 3f;
            foreach (var p in PlayerController.AllPlayers) { if (!p || p == not) continue; Vector3 d = p.transform.position - from; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = p; } }
            return best;
        }

        /// Victim side: does this incoming punch miss? You can't be juggled: after getting up you have a moment
        /// of grace, and punching back at the right time BLOCKS and counters the attacker.
        public bool Deflects(Vector3 from)
        {
            if (!me) return false;
            var carry = GetComponent<KidCarry>(); if (carry && carry.Carried) return true;
            if (Time.time < graceUntil) { NetGame.Pop(transform.position + Vector3.up * 1.2f, "MISS!", "พลาด!", Color.white, "whoosh", .5f, 1.5f); return true; }
            var attacker = NearestTo(from, me);
            if (attacker && Time.time - lastSwingTime < .3f)
            {
                Vector3 d = attacker.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > .001f && Vector3.Dot(transform.forward, d.normalized) > .2f)
                {
                    NetGame.Pop((transform.position + attacker.transform.position) * .5f + Vector3.up * 1.3f, "COUNTER!", "สวนกลับ!", new Color(1f, .45f, .35f), "slap", 1f, .8f);
                    NetGame.Hit(attacker, d, shoveForce * 1.1f, shoveStun, true, transform.position);
                    return true;
                }
            }
            return false;
        }

        static readonly string[] PowEn = { "POW!", "BONK!", "OOF!", "WHAM!", "BOOP!" };
        static readonly string[] PowTh = { "ตุ้บ!", "โป๊ก!", "อั่ก!", "ปั้ก!", "ดึ๋ง!" };
        static readonly string[] Lines = { "Hey! I was carrying that!", "Watch it!", "Not now, Mom's coming!" };
        static readonly string[] LinesTh = { "เฮ้ย! ถือของอยู่นะ!", "ระวังหน่อยสิ!", "ไม่ใช่ตอนนี้ แม่จะมาแล้ว!" };

        void Awake() { me = GetComponent<PlayerController>(); if (!GetComponent<KidCarry>()) gameObject.AddComponent<KidCarry>(); }

        void Update()
        {
            if (!me || !me.isLocal) return;
            shoveCd -= Time.deltaTime; bumpCd -= Time.deltaTime;
            if (swingT > 0f) { swingT -= Time.deltaTime; if (swingT <= 0f) Land(); }
            if (Debug.isDebugBuild && MamaInput.DevNextKid && GameManager.I && GameManager.I.player == me)
            {
                KidLook.Selected = (KidLook.Selected + 1) % KidLook.Count;
                KidLook.Apply(me, KidLook.Selected);
                if (GameManager.I.ui) GameManager.I.Toast(Loc.Thai ? KidLook.NamesTh[KidLook.Selected] : KidLook.Names[KidLook.Selected]);
            }
            // getting up: a short grace period so nobody can be punch-locked
            if (wasStunned && !me.Stunned) graceUntil = Time.time + 1.1f;
            wasStunned = me.Stunned;
            var carried = GetComponent<KidCarry>();
            if (me.KnockedDown && !(carried && carried.Carried) && (MamaInput.ShoveDown || MamaInput.ClickDown || UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame))
                me.ShortenStun(.16f);   // mash to get up sooner
            if (!me.controlsEnabled || me.Stunned || (MiniGame.I && MiniGame.I.Active) || MamaSettings.Open || MamaHowToPlay.Open) { grabT = 0f; return; }

            if (carrying) { CarryTick(); return; }
            // a friend lying curled up in front of you: hold left click to pick them up (instead of punching)
            var downed = Downed();
            if (MamaInput.ClickDown && me.Held == null && downed) { grabT = .001f; grabTarget = downed; }
            if (grabT > 0f)
            {
                bool held = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
                if (!held || !grabTarget || !grabTarget.Stunned) grabT = 0f;
                else if ((grabT += Time.deltaTime) > .25f) Grab(grabTarget);
                return;
            }
            bool punch = MamaInput.ShoveDown || MamaInput.ClickDown && me.Held == null && me.HoldProgress <= 0f;
            if (punch && shoveCd <= 0f) Shove();
            if (bumpCd <= 0f && me.Sprinting && me.PlanarSpeed > bumpSpeed) Bump();
        }

        float swingT;

        PlayerController Downed()
        {
            PlayerController best = null; float bd = 1.5f;
            foreach (var o in PlayerController.AllPlayers)
            {
                if (!o || o == me || !o.Stunned) continue;
                var c = o.GetComponent<KidCarry>(); if (c && c.Carried) continue;
                Vector3 d = o.transform.position - transform.position; d.y = 0f;
                if (d.magnitude < bd && (d.magnitude < .5f || Vector3.Dot(transform.forward, d.normalized) > .1f)) { bd = d.magnitude; best = o; }
            }
            return best;
        }

        void Grab(PlayerController friend)
        {
            grabT = 0f; carrying = friend; carryTimer = 0f;
            NetGame.Hit(friend, transform.forward, KidCarry.GrabCode, 0f, false, transform.position);
            if (me.actions) me.actions.Play(KidActionAnimator.Clip.PickHigh, .35f);
            if (GameManager.I && GameManager.I.ui) GameManager.I.Toast(Loc.Thai ? "คลิกขวา = ปาเพื่อน  •  Q = วาง" : "Right click = throw your friend  •  Q = put down");
        }

        void CarryTick()
        {
            carryTimer += Time.deltaTime;
            bool lost = !carrying || carryTimer > KidCarry.MaxCarry + .6f;
            if (!lost) { Vector3 d = carrying.transform.position - (transform.position + Vector3.up * 1.25f * transform.lossyScale.y); if (carryTimer > .6f && d.magnitude > 2.2f) lost = true; }   // they wriggled free
            if (lost) { carrying = null; return; }
            if (me.actions) me.actions.Hold(KidActionAnimator.Clip.PushUp);
            if (MamaInput.ThrowDown)
            {
                var v = transform.forward * 6.5f;
                NetGame.Hit(carrying, v, KidCarry.ThrowCode, 0f, false, transform.position);
                if (me.actions) me.actions.Play(KidActionAnimator.Clip.Throw, .4f);
                NetGame.Pop(transform.position + transform.forward + Vector3.up * 1.6f, "YEET!", "ปาาา!", new Color(1f, .85f, .2f), "swish", .9f, 1f);
                carrying = null;
            }
            else if (MamaInput.DropDown) { NetGame.Hit(carrying, transform.forward, KidCarry.DropCode, 0f, false, transform.position); carrying = null; }
        }

        PlayerController Victim(float range, float minDot)
        {
            PlayerController best = null; float bestD = float.MaxValue;
            foreach (var o in PlayerController.AllPlayers)
            {
                if (!o || o == me || !o.isActiveAndEnabled) continue;
                Vector3 d = o.transform.position - transform.position; d.y = 0f;
                float dist = d.magnitude; if (dist > range) continue;
                float dot = dist > 0.01f ? Vector3.Dot(transform.forward, d / dist) : 1f;
                if (dot < minDot) continue;
                if (dist < bestD) { bestD = dist; best = o; }
            }
            return best;
        }

        void Shove()
        {
            shoveCd = shoveCooldown; lastSwingTime = Time.time;
            // a little aim help: turn to face a friend who is roughly in front
            var aim = Victim(shoveRange + 0.4f, -0.1f);
            if (aim) { Vector3 d = aim.transform.position - transform.position; d.y = 0f; if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); }
            if (me.actions) me.actions.Play(KidActionAnimator.Clip.Punch, 0.36f);   // relayed to every screen
            swingT = HitDelay;
        }

        void Land()
        {
            if (!me.controlsEnabled || me.Stunned) return;
            var best = Victim(shoveRange, 0.3f);
            if (!best) { NetGame.Pop(transform.position + transform.forward * 0.6f + Vector3.up * 1.0f, null, null, Color.white, "whoosh", 0.45f, 1.6f); return; }
            Vector3 dir = best.transform.position - transform.position;
            NetGame.Hit(best, dir, shoveForce, shoveStun, true, transform.position);
            if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.15f);
            int i = Random.Range(0, PowEn.Length);
            NetGame.Pop(best.transform.position + Vector3.up * 1.3f, PowEn[i], PowTh[i], new Color(1f, 0.85f, 0.2f), "slap", 0.9f, Random.Range(0.85f, 1.15f));
            if (Random.value < 0.35f && GameManager.I && GameManager.I.ui) { int l = Random.Range(0, Lines.Length); GameManager.I.ui.KidSay(Loc.Thai ? LinesTh[l] : Lines[l]); }
        }

        void Bump()
        {
            Vector3 v = me.Motor ? me.Motor.PlanarVelocity : transform.forward * me.PlanarSpeed;
            foreach (var o in PlayerController.AllPlayers)
            {
                if (!o || o == me || !o.isActiveAndEnabled) continue;
                Vector3 d = o.transform.position - transform.position; d.y = 0f;
                float dist = d.magnitude; if (dist > 0.85f || dist < 0.01f) continue;
                if (Vector3.Dot(v.normalized, d / dist) < 0.5f) continue;
                // head-on, both sprinting: only ONE machine resolves the crash, so it never happens twice
                if (o.Sprinting && o.PlanarSpeed > bumpSpeed * 0.8f && string.CompareOrdinal(NetGame.NameOf(me) ?? "", NetGame.NameOf(o) ?? "") > 0) continue;
                bumpCd = 0.8f;
                NetGame.Hit(o, d, 5.5f, 1.25f, true, transform.position);
                me.Bonk(-d, 4.5f, 1.15f, true);
                NetGame.Pop((transform.position + o.transform.position) * 0.5f + Vector3.up * 1.2f, "BOING!", "ดึ๋ง!", new Color(0.5f, 0.9f, 1f), "thud", 0.8f, 1.3f);
                return;
            }
        }

        /// Comic-book word that pops, floats up and fades.
        public static void Pop(Vector3 at, string word, Color col)
        {
            var go = new GameObject("Pow");
            go.transform.position = at;
            var tm = go.AddComponent<TextMesh>();
            tm.text = word; tm.fontSize = 72; tm.characterSize = 0.035f; tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = col;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            go.AddComponent<PowPop>();
        }
    }
}
