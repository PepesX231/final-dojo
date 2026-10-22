using System.Collections;
using System.Collections.Generic;
using DreamRush.Interaction;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// SHADOW DOJO: strike the gong → 3 short waves in a ring-lit training hall (each building's dojo rolls its own mix
    /// from its seed) → 15 minutes + a toast. Practice room for the move set; cleared dojos stay quiet.
    /// </summary>
    public class DojoTrial : Interactable
    {
        public Transform arenaCenter;
        public GameObject tick, brute, roller, cuckoo;
        public bool Running { get; private set; }
        public bool Won { get; private set; }
        BuildingDoor _door; readonly HashSet<EnemyBase> _alive = new HashSet<EnemyBase>();
        static readonly string[][] Mixes = { new[] { "TT", "TTG", "BT" }, new[] { "TTT", "TC", "GG" }, new[] { "TG", "TTC", "BG" }, new[] { "TTT", "BT", "TCG" } };

        protected override void OnEnable() { base.OnEnable(); prompt = "STRIKE THE GONG"; }

        public void Arm(BuildingDoor door) { _door = door; Running = false; Won = PalaceDirector.Current != null && PalaceDirector.Current.Taken("door:" + door.id); interactable = !Won; prompt = Won ? "THE GONG IS SILENT" : "STRIKE THE GONG"; }

        protected override void OnInteract(InteractionContext context) { if (!Running && !Won) StartCoroutine(Run()); }

        public void StartTrial() { if (!Running && !Won) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            Running = true; interactable = false;
            Audio.AudioManager.Play(Audio.Sfx.Shock, transform.position, 1f);
            Presentation.CameraShake.Add(0.3f);
            var mix = Mixes[(_door != null ? Mathf.Abs(_door.seed) : 0) % Mixes.Length];
            for (int w = 0; w < mix.Length; w++)
            {
                BossIntroCard.Show($"DOJO — WAVE {w + 1}/{mix.Length}", "ชนะให้หมด!", chapterCard: true);
                for (int i = 0; i < mix[w].Length; i++)
                {
                    char c = mix[w][i];
                    var tpl = c == 'B' ? brute : c == 'G' ? roller : c == 'C' ? cuckoo : tick; if (tpl == null) tpl = tick;
                    Vector3 p = arenaCenter.position + Quaternion.Euler(0, i * 360f / mix[w].Length + w * 40f, 0) * Vector3.forward * 5.5f;
                    var g = Instantiate(tpl, p + Vector3.up * 8f, Quaternion.LookRotation(arenaCenter.position - p), transform); g.SetActive(true);
                    var e = g.GetComponent<EnemyBase>(); if (e != null) { e.leash = 9f; _alive.Add(e); e.DropIn(p); }
                    yield return new WaitForSeconds(0.2f);
                }
                while (_alive.Count > 0) { _alive.RemoveWhere(x => x == null || x.Dead); yield return null; }
                yield return new WaitForSeconds(0.6f);
            }
            Won = true; Running = false;
            prompt = "THE GONG IS SILENT";
            if (PalaceDirector.Current != null && _door != null) PalaceDirector.Current.Take("door:" + _door.id);
            LostMinute.Burst(arenaCenter.position, 15);
            UIRoot.Get().Toast("ผ่านโดโจ! ได้ LOST MINUTE ×15", 2f);
            Audio.AudioManager.Play(Audio.Sfx.Victory, transform.position, 0.8f);
        }
    }
}
