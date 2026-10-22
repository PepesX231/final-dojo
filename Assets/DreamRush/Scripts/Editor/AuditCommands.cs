using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DreamRush.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamRush.Editor
{
    // Editor assembly only. Invoked through the project's existing DevBridge.
    public static class AuditCommands
    {
        public static string Snapshot()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor3D>();
            var cam = Camera.main;
            return $"scene={SceneManager.GetActiveScene().name} frame={Time.frameCount} scale={Time.timeScale} " +
                $"player={(player != null ? player.transform.position.ToString("F2") : "none")} grounded={player?.IsGrounded} " +
                $"camera={(cam != null ? cam.transform.position.ToString("F2") : "none")} " +
                $"renderers={UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} " +
                $"lights={UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length} " +
                $"stars={World1.DreamStar.CollectedThisRun}/{World1.DreamStar.TotalInLevel} " +
                $"flow={(Flow.GameFlowManager.Exists ? Flow.GameFlowManager.Get().State.ToString() : "none")}";
        }

        public static string Chapter(string index)
        {
            var director = World1.ChapterDirector.Current;
            if (director == null) return "no chapter director";
            director.DevJump(int.Parse(index));
            return Snapshot();
        }

        public static string Measure(string label)
        {
            if (!Application.isPlaying) return "measurement requires play mode";
            var go = new GameObject("AuditFrameSampler");
            go.AddComponent<AuditFrameSampler>().label = label;
            return "sampling 10 seconds after 2 seconds warmup; editor wall frame time, not GPU time";
        }

        public static string PreviewSummary()
        {
            var run = new Flow.RunState(0) { Won = true, LevelScene = "Dream_World1", ElapsedSeconds = 180f,
                DreamStars = 7, NewPersonalBest = true, VictoryId = "Exhaustion", DreamMemoryEarned = 10 };
            UI.RunSummaryPanel.Show(run);
            return "UI preview with sample run data; no victory triggered and no save written";
        }

        public static string PreviewGalaxyRecord()
        {
            if (Flow.GalaxySelect.Current == null) return "not in Galaxy";
            var save = Core.SaveManager.GetOrCreate();
            var original = save.Data.progression.levelRecords;
            try
            {
                save.Data.progression.levelRecords = save.Data.Clone().progression.levelRecords;
                save.Data.progression.RecordClear("Dream_World1", 180f, 7);
                Flow.GalaxySelect.Current.Select(0, true);
            }
            finally { save.Data.progression.levelRecords = original; }
            return "UI preview: sample record rendered, original data restored immediately, no save written";
        }
    }

    public class AuditFrameSampler : MonoBehaviour
    {
        public string label;
        readonly List<float> _frames = new List<float>();
        float _elapsed;
        void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < 2f) return;
            _frames.Add(Time.unscaledDeltaTime * 1000f);
            if (_elapsed < 12f) return;
            _frames.Sort();
            string line = $"{DateTime.UtcNow:O} {label} scene={SceneManager.GetActiveScene().name} " +
                $"resolution={Screen.width}x{Screen.height} n={_frames.Count} meanMs={_frames.Average():F2} " +
                $"p50Ms={_frames[_frames.Count / 2]:F2} p95Ms={_frames[Math.Min(_frames.Count - 1, (int)(_frames.Count * 0.95f))]:F2} " +
                $"managedHeapMB={GC.GetTotalMemory(false) / 1048576f:F1}";
            string path = Path.Combine(Application.dataPath, "../DevBridge/out/audit-performance.txt");
            File.AppendAllText(path, line + Environment.NewLine);
            Debug.Log("[Audit] " + line);
            Destroy(gameObject);
        }
    }
}
