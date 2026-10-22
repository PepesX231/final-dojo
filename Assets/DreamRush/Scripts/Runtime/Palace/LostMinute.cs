using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// LOST MINUTE — the Midnight Palace currency: little golden clock-coins the shadows stole from the city's dreamers.
    /// Found everywhere (rooftops, alleys, flats, vaults, enemy drops, vending machines) and spent at the Watchmaker's
    /// (upgrades) — so every corner of the city is worth a look. Spins, bobs, gets magnetised to Pip within 3.5 m.
    /// </summary>
    public class LostMinute : MonoBehaviour
    {
        public int value = 1;
        public string saveKey = "";             // non-empty: placed pickup, collected once per run
        public static int Collected { get; private set; }
        Transform _player; Vector3 _base; float _t, _magnet; Vector3 _vel; bool _flying;
        static Material _mat, _rim;

        public static void ResetCount() => Collected = 0;

        public static LostMinute Spawn(Vector3 at, int value = 1, string key = "", Transform parent = null)
        {
            if (!string.IsNullOrEmpty(key) && PalaceDirector.Current != null && PalaceDirector.Current.Taken(key)) return null;
            var go = new GameObject("LostMinute");
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = at;
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(coin.GetComponent<Collider>());
            coin.name = "Coin"; coin.transform.SetParent(go.transform, false); coin.transform.localRotation = Quaternion.Euler(90, 0, 0);
            coin.transform.localScale = new Vector3(0.42f, 0.04f, 0.42f) * (value > 1 ? 1.35f : 1f);
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(face.GetComponent<Collider>());
            face.name = "Hand"; face.transform.SetParent(go.transform, false); face.transform.localPosition = new Vector3(0, 0.05f, -0.03f); face.transform.localScale = new Vector3(0.04f, 0.15f, 0.04f);
            if (_mat == null)
            {
                _mat = Look.DreamLook.Toon(new Color(1f, 0.8f, 0.25f), null, 0.6f, name: "M_LostMinute"); _mat.SetColor("_Emission", new Color(0.9f, 0.55f, 0.05f));
                _rim = Look.DreamLook.Toon(new Color(0.2f, 0.1f, 0.3f), null, 0f, name: "M_LostMinuteHand");
            }
            coin.GetComponent<Renderer>().sharedMaterial = _mat; face.GetComponent<Renderer>().sharedMaterial = _rim;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var m = go.AddComponent<LostMinute>(); m.value = value; m.saveKey = key;
            return m;
        }

        /// <summary>A fountain of minutes (enemy drop, vending machine, chest) that flies out then homes in.</summary>
        public static void Burst(Vector3 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var m = Spawn(at + Vector3.up * 0.8f);
                if (m == null) continue;
                m._flying = true; m._vel = Quaternion.Euler(0, i * 360f / Mathf.Max(1, count) + Random.Range(-20f, 20f), 0) * Vector3.forward * Random.Range(2f, 3.5f) + Vector3.up * Random.Range(4f, 6.5f);
            }
        }

        void Start() { var p = FindFirstObjectByType<Player.PlayerMotor3D>(); _player = p != null ? p.transform : null; _base = transform.position; _t = Random.value * 6f; }

        void Update()
        {
            float dt = Time.deltaTime; _t += dt;
            if (_flying)
            {
                _vel += Vector3.down * 18f * dt; transform.position += _vel * dt;
                if (_vel.y < 0f && Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var h, 0.8f, ~0, QueryTriggerInteraction.Ignore)) { _flying = false; _base = h.point + Vector3.up * 0.5f; transform.position = _base; }
                if (_t > 3f) { _flying = false; _base = transform.position; }
            }
            transform.Rotate(0, 220f * dt, 0, Space.World);
            if (_player == null) return;
            Vector3 target = _player.position + Vector3.up * 0.6f;
            float d = Vector3.Distance(target, transform.position);
            if (!_flying && d < 3.5f) _magnet = Mathf.Min(1f, _magnet + dt * 3f);
            if (_magnet > 0f) transform.position = Vector3.MoveTowards(transform.position, target, (6f + 16f * _magnet) * dt);
            else if (!_flying) transform.position = _base + Vector3.up * Mathf.Sin(_t * 2.6f) * 0.15f;
            if (d < 0.7f) Collect();
        }

        void Collect()
        {
            Collected += value;
            if (PalaceDirector.Current != null) { PalaceDirector.Current.AddMinutes(value); if (!string.IsNullOrEmpty(saveKey)) PalaceDirector.Current.Take(saveKey); }
            VFX.Play(VFXKind.StarBurst, transform.position, value > 1 ? 0.7f : 0.4f, VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.Coin, transform.position, 0.55f);
            Destroy(gameObject);
        }
    }
}
