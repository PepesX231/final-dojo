using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// <summary>
    /// Staged check of punching, the curl-into-a-fruit knock-down, fruit swapping/heights and per-fruit gaits.
    /// Offline puppets beside the scene kid; not a human playthrough and not a two-machine network test.
    /// From Edit mode with MamasKitchen open: exec MamasComing.Editor.MamaFightReview.StartFromEditor
    /// </summary>
    public static class MamaFightReview
    {
        const string Folder = "DevBridge/fight-review/";
        static IEnumerator routine;
        static double next;
        static int passed;
        static readonly List<string> lines = new List<string>();
        static readonly List<string> errors = new List<string>();
        static readonly List<GameObject> spawned = new List<GameObject>();

        /// Moves a remote-style puppet like a friend streaming its pose.
        class Driver : MonoBehaviour
        {
            public PlayerController pc; public Vector3 velocity;
            void Update() { transform.position += velocity * Time.deltaTime; pc.RemoteTick(velocity.magnitude, false, false); }
        }

        /// Safe entry for a shared editor: refuses if any open scene has unsaved edits (someone else's work),
        /// otherwise opens MamasKitchen, runs the review and reopens the previous scene afterwards.
        public static void OpenKitchenAndStart()
        {
            Directory.CreateDirectory(Folder);
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (s.isDirty) { File.WriteAllText(Folder + "refused.txt", "REFUSED: unsaved scene open: " + s.path); return; }
            }
            SessionState.SetString("MamaFightReviewReturn", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity");
            StartFromEditor();
        }

        public static void StartFromEditor()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start the review from Edit mode.");
            SessionState.SetBool("MamaFightReviewRequested", true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    string back = SessionState.GetString("MamaFightReviewReturn", "");
                    SessionState.SetString("MamaFightReviewReturn", "");
                    if (!string.IsNullOrEmpty(back) && back != "Assets/MamasComing/Scenes/MamasKitchen.unity")
                        EditorApplication.delayCall += () => UnityEditor.SceneManagement.EditorSceneManager.OpenScene(back);
                    return;
                }
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MamaFightReviewRequested", false)) return;
                SessionState.SetBool("MamaFightReviewRequested", false);
                EditorApplication.delayCall += Run;
            };
        }

        public static void Run()
        {
            if (!EditorApplication.isPlaying || !GameManager.I) throw new InvalidOperationException("Open MamasKitchen in Play mode first.");
            Directory.CreateDirectory(Folder);
            passed = 0; lines.Clear(); errors.Clear(); spawned.Clear();
            Application.logMessageReceivedThreaded += OnLog;
            routine = Checks(); next = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish("ABORTED: Play mode stopped"); return; }
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (!routine.MoveNext()) { Finish("PASSED"); return; }
                next = EditorApplication.timeSinceStartup + (routine.Current is float wait ? wait : 0.05f);
            }
            catch (Exception ex) { lines.Add(ex.ToString()); Finish("FAILED"); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            passed++; lines.Add("PASS " + message);
        }

        static PlayerController Spawn(PlayerController source, Vector3 at, bool local, string name)
        {
            var go = UnityEngine.Object.Instantiate(source.gameObject, at, Quaternion.identity);
            go.name = name; spawned.Add(go);
            var pc = go.GetComponent<PlayerController>();
            pc.isLocal = local; pc.controlsEnabled = false;
            if (!local) { if (pc.Motor) pc.Motor.enabled = false; var cc = go.GetComponent<CharacterController>(); if (cc) cc.enabled = false; }
            // a cloned motor claims the static Instance; give it back to the real kid
            if (source.Motor) typeof(DreamRush.Player.PlayerMotor3D).GetProperty("Instance")?.SetValue(null, source.Motor);
            pc.WakeUp();
            return pc;
        }

        static float Height(PlayerController pc)
        {
            var rs = pc.fallPivot.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            return rs.Max(r => r.bounds.max.y) - pc.transform.position.y;
        }

        static IEnumerator Checks()
        {
            var gm = GameManager.I; var me = gm.player;
            Assert(me && me.Body, "scene kid has KidBody");
            Vector3 basePos = me.transform.position; basePos.y += 0.03f;

            // 1) every fruit can be worn and stands at the same height
            var lineup = new List<PlayerController>();
            for (int id = 0; id < KidLook.Count; id++)
            {
                var p = Spawn(me, basePos + new Vector3(-2.4f + id * 1.2f, 0f, 2.5f), false, "Lineup " + KidLook.Names[id]);
                p.gameObject.AddComponent<Driver>().pc = p;
                yield return null;
                Assert(KidLook.Apply(p, id) && KidLook.Of(p) == id, KidLook.Names[id] + " model swaps in");
                lineup.Add(p);
            }
            yield return .5f;
            foreach (var p in lineup)
            {
                float h = Height(p);
                lines.Add(p.name + " height=" + h.ToString("F3") + " style=" + p.Body.style);
                Assert(Mathf.Abs(h - KidBody.TargetHeight) < 0.12f, p.name + " is kid height (" + h.ToString("F2") + " m)");
            }
            Vector3 mid = basePos + new Vector3(0f, 0f, 2.5f);
            MamaRecastEvidence.Render("fight-lineup", mid + new Vector3(0f, 1.3f, -4.2f), mid + Vector3.up * 0.6f);

            // 2) gaits: walk every fruit and measure how much its body sways
            var sway = new Dictionary<int, float>();
            foreach (var p in lineup) { p.GetComponent<Driver>().velocity = Vector3.forward * 2.6f; sway[KidLook.Of(p)] = 0f; }
            for (int i = 0; i < 24; i++)
            {
                yield return .04f;
                foreach (var p in lineup) { float z = Mathf.DeltaAngle(0f, p.fallPivot.localEulerAngles.z); sway[KidLook.Of(p)] = Mathf.Max(sway[KidLook.Of(p)], Mathf.Abs(z)); }
                if (i == 12) MamaRecastEvidence.Render("fight-walk", mid + new Vector3(0f, 1.3f, -2.2f), mid + new Vector3(0f, 0.6f, 1.2f));
            }
            foreach (var p in lineup) p.GetComponent<Driver>().velocity = Vector3.zero;
            lines.Add("max walk roll deg: " + string.Join(", ", sway.Select(kv => KidLook.Names[kv.Key] + "=" + kv.Value.ToString("F1"))));
            Assert(sway.Values.Distinct().Count() >= 4, "fruits walk with different body sway");
            Assert(sway[KidLook.Banana] > sway[KidLook.Apple] + 4f && sway[KidLook.Banana] >= sway.Values.Max() - 0.01f, "Banana has the goofiest wobble");
            foreach (var p in lineup) UnityEngine.Object.Destroy(p.gameObject);
            yield return .2f;

            // 3) punch: friend in front holding a teddy flies back, drops it and curls into a fruit
            me.Motor.Teleport(basePos, Quaternion.identity);
            me.controlsEnabled = true;
            var victim = Spawn(me, basePos + Vector3.forward * 0.95f, true, "Punch victim");
            yield return null;
            KidLook.Apply(victim, KidLook.Banana);
            var teddy = UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).FirstOrDefault(x => x.displayName == "Teddy bear")
                     ?? UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).First(x => !x.dragOnFloor);
            foreach (var p in PlayerController.AllPlayers) if (p.Held == teddy) p.Drop(false);
            victim.PickUp(teddy);
            yield return .6f;
            Assert(victim.Held == teddy, "victim carries " + teddy.displayName);
            Vector3 before = victim.transform.position;
            me.SendMessage("Shove");
            yield return .3f;
            Assert(victim.KnockedDown, "punched friend is knocked down");
            Assert(victim.Held == null, "punched friend dropped " + teddy.displayName);
            yield return .15f;
            Vector3 flew = victim.transform.position - before; flew.y = 0f;
            lines.Add("knock-back metres=" + flew.magnitude.ToString("F2") + " limbsOut=" + victim.Body.LimbsOut.ToString("F2") + " pivotTilt=" + Quaternion.Angle(Quaternion.identity, victim.fallPivot.localRotation).ToString("F0"));
            Assert(flew.magnitude > 0.4f && Vector3.Dot(flew, Vector3.forward) > 0f, "punch knocks the friend away from the puncher");
            Assert(victim.Body.LimbsOut < 0.1f, "limbs are pulled in: plain fruit");
            Assert(Quaternion.Angle(Quaternion.identity, victim.fallPivot.localRotation) > 40f, "fruit lies tipped over");
            Vector3 v = victim.transform.position;
            MamaRecastEvidence.Render("fight-knocked", v + new Vector3(1.6f, 1.1f, -1.6f), v + Vector3.up * 0.35f);
            bool sawPop = false, sawOvershoot = false; float t0 = Time.time;
            while (Time.time - t0 < 3f && (victim.Stunned || victim.Body.Recovering))
            {
                if (!victim.KnockedDown && victim.Body.LimbsOut > 0.3f && victim.Body.Recovering && !sawPop) { sawPop = true; v = victim.transform.position; MamaRecastEvidence.Render("fight-popup", v + new Vector3(1.6f, 1.1f, -1.6f), v + Vector3.up * 0.4f); }
                if (victim.Body.LimbsOut > 1.05f) sawOvershoot = true;
                yield return .02f;
            }
            Assert(sawPop, "limbs pop back out before standing");
            Assert(sawOvershoot, "limbs spring out with an overshoot");
            yield return .2f;
            Assert(!victim.Stunned && Mathf.Abs(victim.Body.LimbsOut - 1f) < 0.01f && Quaternion.Angle(Quaternion.identity, victim.fallPivot.localRotation) < 12f, "friend is back up with limbs restored");

            // 4) slipping also curls the kid up
            victim.Slip(null);
            yield return .3f;
            Assert(victim.KnockedDown && victim.Body.LimbsOut < 0.1f, "slipping curls the kid into a fruit too");
            me.controlsEnabled = false;
            Assert(errors.Count == 0, "no exceptions during the fight review");
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("Server forcefully disconnected this plugin")) return;
            lock (errors) errors.Add(message + "\n" + trace);
        }

        static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceivedThreaded -= OnLog;
            foreach (var g in spawned) if (g) UnityEngine.Object.Destroy(g);
            lines.Insert(0, result + " passed=" + passed + " (staged offline puppets, not a human or two-machine playthrough)");
            lock (errors) lines.AddRange(errors);
            File.WriteAllLines(Folder + "review-" + DateTime.Now.ToString("HHmmss") + ".txt", lines);
            Debug.Log("Mama fight review " + result + ", checks=" + passed);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}
