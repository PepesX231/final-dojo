using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// MIDNIGHT PALACE rules (docs/LEVEL2_DESIGN.md §5): a night city where time stopped at 11:59. Find the 3 STOLEN HOURS
    /// (Rooftop Run east, Gear Works north, Shadow Ambush west) → the Clock Tower gate opens → climb the gear spiral → the
    /// dial in the sky where COUNT MIDNIGHT waits. Each district has its own colour mood (fog, ambient, comic-dot tint);
    /// entering one shows a title card. Falling off the sky dial puts you back on its rim.
    /// </summary>
    public class PalaceDirector : MonoBehaviour
    {
        [System.Serializable]
        public class District
        {
            public string name, thai; public Vector3 center; public float radius = 90f;
            public Color fog = Color.black, ambientSky = Color.gray, ambientEquator = Color.gray, halftone = new Color(0.1f, 0.05f, 0.3f, 0.8f);
        }
        public static PalaceDirector Current { get; private set; }
        public List<District> districts = new List<District>();
        public List<StolenHour> hours = new List<StolenHour>();
        public GameObject towerGate;
        public Transform arenaCenter; public float arenaRadius = 22f;
        public Combat.CountMidnight boss;
        public PuzzleReward blackout, fountain;   // M17 quest beats shown in the objective list
        public int Hours { get; private set; }
        public bool GateOpen { get; private set; }
        public District CurrentDistrict { get; private set; }
        public int AmbushWaves { get; set; }
        // ---- M15: the city economy & exploration state (one dream run)
        public int Minutes { get; private set; }
        public event System.Action<int> MinutesChanged;
        public bool HourCompass { get; set; }          // unlocked at a library: arrows to the remaining stolen hours
        readonly HashSet<string> _taken = new HashSet<string>();
        readonly Dictionary<string, int> _upgrades = new Dictionary<string, int>();
        public bool Taken(string key) => _taken.Contains(key);
        public void Take(string key) { if (!string.IsNullOrEmpty(key)) _taken.Add(key); }
        public int TakenCount => _taken.Count;
        Dream.Checkpoint _restPoint;
        /// <summary>Café rest: the dream's checkpoint moves to the café door.</summary>
        public void SetRespawn(Vector3 at)
        {
            if (_restPoint == null) { var go = new GameObject("RestCheckpoint"); _restPoint = go.AddComponent<Dream.Checkpoint>(); _restPoint.radius = 0f; _restPoint.order = 9999; }
            _restPoint.transform.position = at; _lastSafe = at;
            if (Dream.DreamLevelManager.Current != null) Dream.DreamLevelManager.Current.ForceCheckpoint(_restPoint);
        }
        public void AddMinutes(int n) { Minutes = Mathf.Max(0, Minutes + n); MinutesChanged?.Invoke(Minutes); }
        public bool Spend(int n) { if (Minutes < n) return false; AddMinutes(-n); return true; }
        public int Upgrade(string id) => _upgrades.TryGetValue(id, out var v) ? v : 0;
        public void AddUpgrade(string id) => _upgrades[id] = Upgrade(id) + 1;
        // safe ground for fall rescue (street level, not in a building, not on a moving thing)
        Vector3 _lastSafe; float _safeT;
        public Vector3 LastSafe => _lastSafe;
        static bool SolidStaticBelow(Vector3 p)
        {
            if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var h, 1.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return h.collider.GetComponentInParent<World1.MovingPlatform>() == null && h.collider.GetComponentInParent<GrindRail>() == null;
        }
        PlayerMotor3D _player;
        float _blend = 1f;
        Color _fogFrom, _fogTo, _skyFrom, _skyTo, _eqFrom, _eqTo, _htFrom, _htTo;

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; PuzzleReward.AnySolved -= OnPuzzle; }
        void OnPuzzle(PuzzleReward r) => SetGoal();

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            PuzzleReward.AnySolved += OnPuzzle;
            Invoke(nameof(SetGoal), 0.3f);
            _fogTo = RenderSettings.fogColor; _skyTo = RenderSettings.ambientSkyColor; _eqTo = RenderSettings.ambientEquatorColor;
            _htTo = Shader.GetGlobalColor("_DR_HalftoneColor");
        }

        void SetGoal()
        {
            string txt;
            if (GateOpen) txt = "ประตูหอนาฬิกาเปิดแล้ว — ปีนเฟืองขึ้นไปหา COUNT MIDNIGHT!";
            else
            {
                string Mark(bool done) => done ? "✓" : "◆";
                bool east = hours.Count > 0 && hours[0] != null && hours[0].Collected, north = hours.Count > 1 && hours[1] != null && hours[1].Collected, west = hours.Count > 2 && hours[2] != null && hours[2].Collected;
                txt = $"STOLEN HOURS {Hours}/3 — ตามลำแสงบนฟ้า\n" +
                      $"{Mark(blackout == null || blackout.Solved)} ใต้: ต่อวงจรไฟ (ต่อยแผ่นให้หมุน) → ขึ้นรถไฟลอยฟ้า\n" +
                      $"{Mark(east)} ตะวันออก: วิ่งบนหลังคา · ตีระฆังหยุดใบพัด\n" +
                      $"{Mark(north)} เหนือ: ดึงคันโยกย้อนเวลาบนหอเฟือง แล้วรีบขึ้น\n" +
                      $"{Mark(west)} ตะวันตก: ต่อยกล่องลงแผ่นกด 3 แผ่น แล้วสู้เงา";
            }
            DreamHUD.Current?.SetObjective(txt);
        }

        public void OnHourCollected(StolenHour h)
        {
            Hours++;
            BossIntroCard.Show($"STOLEN HOUR  {Hours} / 3", Hours < 3 ? "เวลากลับมาอีกนิด… ไปต่อ!" : "เที่ยงคืนเรียกหาเราแล้ว — หอนาฬิกาเปิด!", chapterCard: true);
            if (Hours >= 3) OpenGate();
            SetGoal();
        }

        public void OpenGate()
        {
            if (GateOpen) return;
            GateOpen = true;
            if (towerGate != null)
            {
                Presentation.VFX.Play(Presentation.VFXKind.StarBurst, towerGate.transform.position + Vector3.up * 4f, 5f, Presentation.VFX.Gold);
                Presentation.CameraShake.Add(0.6f);
                towerGate.SetActive(false);
            }
            WonderDirector.Novelty("gate open");
            SetGoal();
        }

        void Update()
        {
            if (_player == null) return;
            Vector3 p = _player.transform.position;
            bool inside = InteriorDirector.Current != null && InteriorDirector.Current.Inside;
            // remember solid ground every 0.5 s → a fall puts you back right there, not across town
            if (!inside && _player.IsGrounded && p.y < 60f && SolidStaticBelow(p)) { _safeT += Time.deltaTime; if (_safeT > 0.5f) { _safeT = 0f; _lastSafe = p; } }
            if (!inside && p.y < -8f && (arenaCenter == null || boss == null || boss.Current == Combat.CountMidnight.Mode.Waiting))
            {
                Vector3 back = _lastSafe != Vector3.zero ? _lastSafe : (Dream.DreamLevelManager.Current != null && Dream.DreamLevelManager.Current.LastCheckpoint != null ? Dream.DreamLevelManager.Current.LastCheckpoint.transform.position : new Vector3(0, 0.3f, -150f));
                _player.Teleport(back + Vector3.up * 0.3f, _player.transform.rotation);
                Presentation.VFX.Play(Presentation.VFXKind.Poof, back + Vector3.up, 1.2f, Presentation.VFX.Lavender);
                UIRoot.Get().Toast("หลุดจากฝัน… กลับมาที่เดิม", 1.2f);
            }
            if (inside) return;
            // district moods
            District best = null; float bestD = float.MaxValue;
            foreach (var d in districts)
            {
                float dist = new Vector2(p.x - d.center.x, p.z - d.center.z).magnitude;
                if (dist < d.radius && dist < bestD) { bestD = dist; best = d; }
            }
            if (best != null && best != CurrentDistrict)
            {
                CurrentDistrict = best;
                BossIntroCard.Show(best.name, best.thai, chapterCard: true);
                WonderDirector.Novelty("district " + best.name);
                _fogFrom = RenderSettings.fogColor; _skyFrom = RenderSettings.ambientSkyColor; _eqFrom = RenderSettings.ambientEquatorColor; _htFrom = Shader.GetGlobalColor("_DR_HalftoneColor");
                _fogTo = best.fog; _skyTo = best.ambientSky; _eqTo = best.ambientEquator; _htTo = best.halftone;
                _blend = 0f;
            }
            if (_blend < 1f)
            {
                _blend = Mathf.MoveTowards(_blend, 1f, Time.deltaTime / 2.5f);
                float k = Mathf.SmoothStep(0, 1, _blend);
                // M17: fog / ambient belong to the SkyClock (sunset → midnight); districts keep their comic-dot tint
                Shader.SetGlobalColor("_DR_HalftoneColor", Color.Lerp(_htFrom, _htTo, k));
            }
            // sky-dial rescue: fell off during the fight → back on the rim
            if (arenaCenter != null && boss != null && boss.Current != Combat.CountMidnight.Mode.Waiting && p.y < arenaCenter.position.y - 14f)
            {
                Vector3 back = arenaCenter.position + (new Vector3(p.x, 0, p.z) - new Vector3(arenaCenter.position.x, 0, arenaCenter.position.z)).normalized * (arenaRadius - 3f) + Vector3.up * 1f;
                _player.Teleport(back, Quaternion.LookRotation(arenaCenter.position - back));
                Dream.SleepStability.Current?.Damage("fell off the dial", ignoreInvulnerability: true);
            }
        }
    }
}
