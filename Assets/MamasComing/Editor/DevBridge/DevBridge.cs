// DREAM RUSH — DevBridge (editor-only development tool, never part of a build)
// Lets an external agent drive the Editor through plain text files so compile results,
// console output and test results can be verified without guessing.
//   DevBridge/cmd/*.txt   one command per file, processed in file-name order, deleted after reading
//   DevBridge/out/        bridge.log, console.log, compile.log, status.txt, tests_<mode>.txt
// Deliberately references no game assembly so it keeps working when game code fails to compile.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DreamRush.DevBridge
{
    [InitializeOnLoad]
    public static class DevBridge
    {
        static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "DevBridge"));
        static readonly string CmdDir = Path.Combine(Root, "cmd");
        static readonly string OutDir = Path.Combine(Root, "out");
        static readonly object LogLock = new object();
        static double _nextPoll;
        static double _nextStatus;
        static TestRunnerApi _api;
        static readonly System.Collections.Generic.List<(double time, string path)> _pendingShots =
            new System.Collections.Generic.List<(double, string)>();

        static DevBridge()
        {
            try
            {
                Directory.CreateDirectory(CmdDir);
                Directory.CreateDirectory(OutDir);
            }
            catch (Exception e) { Debug.LogWarning("[DevBridge] cannot create folders: " + e.Message); return; }

            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            CompilationPipeline.compilationStarted -= OnCompileStarted;
            CompilationPipeline.compilationStarted += OnCompileStarted;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompiled;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;

            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(new TestCallbacks());

            // remote sessions drive an unfocused editor: without this, play mode freezes when Unity loses focus
            if (!PlayerSettings.runInBackground) { PlayerSettings.runInBackground = true; Bridge("PlayerSettings.runInBackground -> true"); }
            Bridge($"domain loaded (playing={EditorApplication.isPlayingOrWillChangePlaymode})");
            WriteStatus();
        }

        // ---------------------------------------------------------------- polling
        static void Poll()
        {
            double now = EditorApplication.timeSinceStartup;
            // an unfocused editor throttles the player loop; keep the game ticking so remote captures/tests see live frames
            if (EditorApplication.isPlaying && !EditorApplication.isPaused) EditorApplication.QueuePlayerLoopUpdate();
            if (now >= _nextStatus) { _nextStatus = now + 2.0; WriteStatus(); }
            for (int i = _pendingShots.Count - 1; i >= 0; i--)
            {
                if (now < _pendingShots[i].time) continue;
                string path = _pendingShots[i].path;
                _pendingShots.RemoveAt(i);
                bool ui = UiShots.Remove(path);
                try { Bridge(CaptureCamera(path, ui)); } catch (Exception e) { Bridge("screenshot failed: " + e.Message); }
            }
            if (now < _nextPoll) return;
            _nextPoll = now + 0.5;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string[] files;
            try { files = Directory.GetFiles(CmdDir, "*.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray(); }
            catch { return; }
            if (files.Length == 0) return;

            string file = files[0];
            string cmd;
            try { cmd = File.ReadAllText(file).Trim(); File.Delete(file); }
            catch (Exception e) { Bridge("cannot read command " + file + ": " + e.Message); return; }

            Bridge($"> {Path.GetFileName(file)}: {cmd}");
            try { Execute(cmd); }
            catch (Exception e) { Bridge("! command failed: " + e); }
        }

        static void Execute(string line)
        {
            int sp = line.IndexOf(' ');
            string verb = (sp < 0 ? line : line.Substring(0, sp)).ToLowerInvariant();
            string arg = sp < 0 ? "" : line.Substring(sp + 1).Trim();

            switch (verb)
            {
                case "refresh":
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    Bridge("refresh done (compile follows if scripts changed)");
                    break;
                case "recompile":
                    CompilationPipeline.RequestScriptCompilation();
                    Bridge("compilation requested");
                    break;
                case "menu":
                    Bridge($"menu '{arg}' -> {EditorApplication.ExecuteMenuItem(arg)}");
                    break;
                case "exec":
                    {
                        int sp2 = arg.IndexOf(' ');
                        ExecStatic(sp2 < 0 ? arg : arg.Substring(0, sp2), sp2 < 0 ? null : arg.Substring(sp2 + 1));
                        break;
                    }
                case "open":
                    if (EditorApplication.isPlaying) { Bridge("open refused: in play mode"); break; }
                    EditorSceneManager.OpenScene(arg, OpenSceneMode.Single);
                    Bridge("opened " + arg);
                    break;
                case "play":
                    EditorApplication.isPlaying = true;
                    Bridge("entering play mode");
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    Bridge("exiting play mode");
                    break;
                case "pause":
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                    Bridge("paused=" + EditorApplication.isPaused);
                    break;
                case "uishot":
                case "screenshot":
                    {
                        bool withUi = verb == "uishot"; // uishot: overlay canvases are rendered through the camera too
                        // screenshot <name.png> [delaySec] [count] [interval]
                        string[] a = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string name = a.Length > 0 ? a[0] : "shot.png";
                        float delay = a.Length > 1 ? float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        int count = a.Length > 2 ? int.Parse(a[2]) : 1;
                        float interval = a.Length > 3 ? float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture) : 0.25f;
                        string baseName = Path.GetFileNameWithoutExtension(name);
                        for (int i = 0; i < count; i++)
                        {
                            string file = count == 1 ? name : $"{baseName}_{i}.png";
                            string full = Path.IsPathRooted(file) ? file : Path.Combine(OutDir, file);
                            if (withUi) UiShots.Add(full);
                            _pendingShots.Add((EditorApplication.timeSinceStartup + delay + i * interval, full));
                        }
                        Bridge($"screenshot scheduled x{count} after {delay}s");
                        break;
                    }
                case "gameshot":
                    {
                        // Full Game-view capture incl. overlay UI. Needs the Game view to repaint (Editor focused).
                        string p = Path.Combine(OutDir, string.IsNullOrEmpty(arg) ? "game.png" : arg);
                        ScreenCapture.CaptureScreenshot(p);
                        Bridge("gameshot requested -> " + p);
                        break;
                    }
                case "tests":
                    RunTests(arg);
                    break;
                case "clearlogs":
                    lock (LogLock)
                    {
                        File.WriteAllText(Path.Combine(OutDir, "console.log"), "");
                        File.WriteAllText(Path.Combine(OutDir, "compile.log"), "");
                    }
                    Bridge("logs cleared");
                    break;
                case "status":
                    WriteStatus();
                    Bridge("status written");
                    break;
                default:
                    Bridge("unknown command: " + verb);
                    break;
            }
        }

        /// <summary>Renders Camera.main into an offscreen texture — works even when the Game view is not repainting.</summary>
        static readonly HashSet<string> UiShots = new HashSet<string>();

        static string CaptureCamera(string path, bool withUi = false, int w = 1280, int h = 720)
        {
            var cam = Camera.main;
            if (cam == null) return "screenshot failed: no Camera.main";
            var switched = new List<Canvas>();
            if (withUi)
            {
                var roots = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    .OrderByDescending(c => c.sortingOrder).ToList();
                for (int i = 0; i < roots.Count; i++)
                {
                    var c = roots[i];
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = cam.nearClipPlane + 0.02f + 0.01f * i; // higher sorting order = nearer
                    switched.Add(c);
                }
                Canvas.ForceUpdateCanvases();
            }
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return "screenshot saved -> " + path;
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                foreach (var c in switched) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        static void ExecStatic(string target, string stringArg)
        {
            // "Namespace.Type, Assembly.Method" or "Namespace.Type.Method" (searches all assemblies)
            int dot = target.LastIndexOf('.');
            string typeName = target.Substring(0, dot);
            string method = target.Substring(dot + 1);
            Type t = Type.GetType(typeName);
            if (t == null)
            {
                string shortName = typeName.Split(',')[0].Trim();
                t = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(shortName, false))
                    .FirstOrDefault(x => x != null);
            }
            if (t == null) { Bridge("exec: type not found " + typeName); return; }
            var mi = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == method && m.GetParameters().Length == (stringArg == null ? 0 : 1));
            if (mi == null) { Bridge("exec: method not found " + method); return; }
            object r = mi.Invoke(null, stringArg == null ? null : new object[] { stringArg });
            Bridge($"exec {t.Name}.{method} -> {(r ?? "void")}");
        }

        // ---------------------------------------------------------------- tests
        static void RunTests(string arg)
        {
            string[] parts = arg.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            TestMode mode = parts.Length > 0 && parts[0].Equals("PlayMode", StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode : TestMode.EditMode;
            var filter = new Filter { testMode = mode };
            if (parts.Length > 1) filter.groupNames = new[] { parts[1] };
            string outFile = Path.Combine(OutDir, $"tests_{mode}.txt");
            File.WriteAllText(outFile, $"RUNNING {mode} {DateTime.Now:HH:mm:ss}\n");
            SessionState.SetString("DreamRush.DevBridge.TestFile", outFile);
            _api.Execute(new ExecutionSettings(filter));
            Bridge($"tests started: {mode} {(parts.Length > 1 ? parts[1] : "")}");
        }

        class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"FINISHED {DateTime.Now:HH:mm:ss}");
                sb.AppendLine($"RESULT {result.TestStatus}  passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount} duration={result.Duration:F1}s");
                AppendLeaves(result, sb);
                string file = SessionState.GetString("DreamRush.DevBridge.TestFile", Path.Combine(OutDir, "tests_unknown.txt"));
                File.AppendAllText(file, sb.ToString());
                Bridge($"tests finished: {result.TestStatus} passed={result.PassCount} failed={result.FailCount} ({Path.GetFileName(file)})");
            }

            static void AppendLeaves(ITestResultAdaptor r, StringBuilder sb)
            {
                if (r.HasChildren) { foreach (var c in r.Children) AppendLeaves(c, sb); return; }
                sb.AppendLine($"  [{r.TestStatus}] {r.Test.FullName} ({r.Duration:F2}s)");
                if (r.TestStatus == TestStatus.Failed)
                {
                    sb.AppendLine("      " + (r.Message ?? "").Replace("\n", "\n      "));
                    if (!string.IsNullOrEmpty(r.StackTrace))
                        sb.AppendLine("      " + r.StackTrace.Split('\n').FirstOrDefault(l => l.Contains(".cs:")) );
                }
                if (!string.IsNullOrEmpty(r.Output))
                    sb.AppendLine("      out: " + r.Output.Trim().Replace("\n", "\n      out: "));
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }

        // ---------------------------------------------------------------- logging
        static void OnCompileStarted(object _)
        {
            Append("compile.log", $"--- compile started {DateTime.Now:HH:mm:ss}");
        }

        static void OnAssemblyCompiled(string assemblyPath, CompilerMessage[] messages)
        {
            int errors = messages.Count(m => m.type == CompilerMessageType.Error);
            int warnings = messages.Count(m => m.type == CompilerMessageType.Warning);
            var sb = new StringBuilder();
            sb.Append($"{Path.GetFileName(assemblyPath)}: errors={errors} warnings={warnings}");
            foreach (var m in messages)
                sb.Append($"\n  {m.type}: {m.file}({m.line},{m.column}) {m.message}");
            Append("compile.log", sb.ToString());
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition != null && condition.StartsWith("[DevBridge]")) return;
            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {type}: {condition}";
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                string firstFrames = string.Join(" | ", (stackTrace ?? "").Split('\n').Where(l => l.Trim().Length > 0).Take(4));
                entry += "\n    at " + firstFrames;
            }
            Append("console.log", entry);
        }

        static void Bridge(string msg) => Append("bridge.log", $"[{DateTime.Now:HH:mm:ss}] {msg}");

        static void Append(string file, string line)
        {
            try { lock (LogLock) File.AppendAllText(Path.Combine(OutDir, file), line + "\n"); }
            catch { /* never let logging throw */ }
        }

        static void WriteStatus()
        {
            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                File.WriteAllText(Path.Combine(OutDir, "status.txt"),
                    $"time={DateTime.Now:HH:mm:ss}\ncompiling={EditorApplication.isCompiling}\nupdating={EditorApplication.isUpdating}\n" +
                    $"playing={EditorApplication.isPlaying}\npaused={EditorApplication.isPaused}\nscene={scene.path}\n" +
                    $"compileErrors={EditorUtility.scriptCompilationFailed}\n");
            }
            catch { }
        }
    }
}
