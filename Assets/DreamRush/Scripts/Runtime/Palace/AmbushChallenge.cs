using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// SHADOW AMBUSH (west plaza): step in → "AMBUSH!" → waves drop from the sky; clear them all → the stolen hour appears.
    /// Waves are written as letters (M14 — every wave mixes enemies so the fight changes shape):
    ///   T = Tick-Tock shadow · B = Bell Brute · G = Gear Roller · C = Cuckoo Sniper
    /// (legacy: <see cref="waves"/> counts of Tick-Tocks when <see cref="waveMix"/> is empty).
    /// </summary>
    public class AmbushChallenge : MonoBehaviour
    {
        public GameObject minionTemplate, bruteTemplate, rollerTemplate, cuckooTemplate;
        public StolenHour reward;
        public float radius = 14f;
        public int[] waves = { 3, 4 };
        public string[] waveMix = { };
        public PlateGroup requires;              // M17: the fountain seal must be broken first (crates on plates)
        public bool Started { get; private set; }
        public bool Cleared { get; private set; }
        public int Wave { get; private set; }
        public int WaveCount => waveMix != null && waveMix.Length > 0 ? waveMix.Length : waves.Length;
        readonly HashSet<EnemyBase> _alive = new HashSet<EnemyBase>();
        Transform _player;

        void Start() { var m = FindFirstObjectByType<Player.PlayerMotor3D>(); _player = m != null ? m.transform : null; }

        void Update()
        {
            if (Started || _player == null) return;
            if (requires != null && !requires.Solved) return;
            if (Vector3.Distance(_player.position, transform.position) < radius) Begin();
        }

        public void Begin() { if (Started) return; Started = true; EnemyBase.AnyDied += OnDied; StartCoroutine(Run()); }

        string Spec(int w) => waveMix != null && waveMix.Length > 0 ? waveMix[w] : new string('T', waves[w]);

        GameObject TemplateFor(char c)
        {
            GameObject t = c == 'B' ? bruteTemplate : c == 'G' ? rollerTemplate : c == 'C' ? cuckooTemplate : minionTemplate;
            return t != null ? t : minionTemplate;
        }

        IEnumerator Run()
        {
            for (Wave = 0; Wave < WaveCount; Wave++)
            {
                string spec = Spec(Wave);
                UI.BossIntroCard.Show(Wave == 0 ? "AMBUSH!" : Wave == WaveCount - 1 ? "FINAL WAVE!" : "MORE SHADOWS!", $"— WAVE {Wave + 1} / {WaveCount} —", chapterCard: true);
                Presentation.CameraShake.Add(0.5f);
                WonderDirector.Novelty("ambush wave");
                for (int i = 0; i < spec.Length; i++)
                {
                    float ring = spec[i] == 'B' ? 7f : spec[i] == 'C' ? 9f : 6f;
                    Vector3 p = transform.position + Quaternion.Euler(0, i * 360f / spec.Length + Wave * 30f, 0) * Vector3.forward * ring;
                    var g = Instantiate(TemplateFor(spec[i]), p + Vector3.up * 12f, Quaternion.LookRotation(transform.position - p)); g.SetActive(true);
                    var e = g.GetComponent<EnemyBase>();
                    if (e != null) { _alive.Add(e); e.DropIn(p); }
                    yield return new WaitForSeconds(0.25f);
                }
                while (_alive.Count > 0) { _alive.RemoveWhere(x => x == null || x.Dead); yield return null; }
                yield return new WaitForSeconds(0.8f);
            }
            EnemyBase.AnyDied -= OnDied;
            Cleared = true;
            if (reward != null) reward.Reveal();
            UI.UIRoot.Get().Toast("ปราบเงาหมดแล้ว! STOLEN HOUR ปรากฏตัว", 2.5f);
        }

        void OnDied(EnemyBase e) => _alive.Remove(e);
        void OnDestroy() => EnemyBase.AnyDied -= OnDied;
    }
}
