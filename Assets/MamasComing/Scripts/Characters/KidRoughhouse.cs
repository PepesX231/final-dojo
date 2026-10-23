using UnityEngine;

namespace MamasComing
{
    /// Chaotic co-op slapstick: G / RB = shove the kid in front of you (they drop what they carry),
    /// and two kids sprinting into each other bounce apart.
    /// Online: the swing animation, the comic word and the sounds play on EVERY screen; the knock-back is applied
    /// by the victim's own machine (which also decides whether they had already dodged).
    [RequireComponent(typeof(PlayerController))]
    public class KidRoughhouse : MonoBehaviour
    {
        public float shoveRange = 1.35f;
        public float shoveForce = 6.5f;
        public float shoveCooldown = 0.75f;
        public float bumpSpeed = 4.0f;

        PlayerController me;
        float shoveCd, bumpCd;

        static readonly string[] PowEn = { "POW!", "BONK!", "OOF!", "WHAM!", "BOOP!" };
        static readonly string[] PowTh = { "ตุ้บ!", "โป๊ก!", "อั่ก!", "ปั้ก!", "ดึ๋ง!" };
        static readonly string[] Lines = { "Hey! I was carrying that!", "Watch it!", "Not now, Mom's coming!" };
        static readonly string[] LinesTh = { "เฮ้ย! ถือของอยู่นะ!", "ระวังหน่อยสิ!", "ไม่ใช่ตอนนี้ แม่จะมาแล้ว!" };

        void Awake() { me = GetComponent<PlayerController>(); }

        void Update()
        {
            if (!me || !me.isLocal) return;
            shoveCd -= Time.deltaTime; bumpCd -= Time.deltaTime;
            if (!me.controlsEnabled || me.Stunned || (MiniGame.I && MiniGame.I.Active) || MamaSettings.Open) return;

            if (MamaInput.ShoveDown && shoveCd <= 0f) Shove();
            if (bumpCd <= 0f && me.PlanarSpeed > bumpSpeed) Bump();
        }

        void Shove()
        {
            shoveCd = shoveCooldown;
            if (me.actions) me.actions.Play(KidActionAnimator.Clip.Throw, 0.32f);   // relayed to every screen
            PlayerController best = null; float bestD = float.MaxValue;
            foreach (var o in PlayerController.AllPlayers)
            {
                if (!o || o == me || !o.isActiveAndEnabled) continue;
                Vector3 d = o.transform.position - transform.position; d.y = 0f;
                float dist = d.magnitude; if (dist > shoveRange) continue;
                float dot = dist > 0.01f ? Vector3.Dot(transform.forward, d / dist) : 1f;
                if (dot < 0.35f) continue;
                if (dist < bestD) { bestD = dist; best = o; }
            }
            if (!best) { NetGame.Pop(transform.position + transform.forward * 0.6f + Vector3.up * 1.0f, null, null, Color.white, "whoosh", 0.45f, 1.6f); return; }
            Vector3 dir = best.transform.position - transform.position;
            NetGame.Hit(best, dir, shoveForce, 0.9f, true, transform.position);
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
                if (o.PlanarSpeed > bumpSpeed * 0.8f && string.CompareOrdinal(NetGame.NameOf(me) ?? "", NetGame.NameOf(o) ?? "") > 0) continue;
                bumpCd = 0.8f;
                NetGame.Hit(o, d, 4.5f, 0.35f, false, transform.position);   // them: boing
                me.Bonk(-d, 3.5f, 0.25f, false);                              // me: bounce back
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
