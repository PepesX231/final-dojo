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
    /// Staged check of the rigged Banana kid prefab (Banana_Rigged.fbx): bones, walk clip, and both gloves
    /// reaching CarryGrip contacts. Spawns an offline puppet beside the scene kid; does not replace the player,
    /// open networking or count as a human playthrough. Run in Play mode: exec MamasComing.Editor.MamaBananaReview.Run
    /// </summary>
    public static class MamaBananaReview
    {
        const string Folder = "DevBridge/mama-rebuild/banana/";
        const string Prefab = "Assets/MamasComing/Prefabs/Characters/PF_Kid_Banana.prefab";
        static IEnumerator routine;
        static double next;
        static int passed;
        static readonly List<string> lines = new List<string>();
        static readonly List<string> errors = new List<string>();
        static GameObject banana;

        /// Keeps the puppet's held item and walk speed ticking like a remote friend (RemoteTick), once per frame.
        class Driver : MonoBehaviour
        {
            public PlayerController pc; public Vector3 velocity;
            void Update() { transform.position += velocity * Time.deltaTime; pc.RemoteTick(velocity.magnitude, false, false); }
        }

        public static void StartFromEditor()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start the review from Edit mode.");
            SessionState.SetBool("MamaBananaReviewRequested", true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MamaBananaReviewRequested", false)) return;
                SessionState.SetBool("MamaBananaReviewRequested", false);
                EditorApplication.delayCall += Run;
            };
        }

        public static void Run()
        {
            if (!EditorApplication.isPlaying || !GameManager.I) throw new InvalidOperationException("Open MamasKitchen in Play mode first.");
            Directory.CreateDirectory(Folder);
            passed = 0; lines.Clear(); errors.Clear();
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
                next = EditorApplication.timeSinceStartup + (routine.Current is float wait ? wait : 0.12f);
            }
            catch (Exception ex) { lines.Add(ex.ToString()); Finish("FAILED"); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            passed++; lines.Add("PASS " + message);
        }

        static Transform Bone(string n) => banana.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);

        static IEnumerator Checks()
        {
            var gm = GameManager.I;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Assert(source, "PF_Kid_Banana prefab exists");
            banana = UnityEngine.Object.Instantiate(source, new Vector3(16f, .03f, 11f), Quaternion.identity);
            banana.name = "Banana review puppet";
            var pc = banana.GetComponent<PlayerController>();
            pc.isLocal = false; pc.controlsEnabled = false;
            if (pc.Motor) pc.Motor.enabled = false;
            var cc = banana.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            var driver = banana.AddComponent<Driver>(); driver.pc = pc;
            yield return .3f;

            var skin = banana.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r => r.bones.Length > 0);
            Assert(skin && skin.bones.Count(b => b) >= 14, "Banana uses one skinned mesh bound to the exported bones");
            foreach (var n in new[] { "Body", "Arm_L", "Arm_L_Lower", "Hand_L", "Arm_R", "Arm_R_Lower", "Hand_R", "Foot_L", "Foot_R" })
                Assert(Bone(n), "bone " + n + " present");
            Assert(Bone("Arm_L").parent == Bone("Arm_L_Lower").parent && Bone("Body").parent == Bone("Arm_L").parent, "limb bones share the body frame like the apple rig");
            Assert(Bone("Hand_L").parent == Bone("Arm_L_Lower"), "glove follows the forearm");
            var act = banana.GetComponent<KidActionAnimator>();
            Assert(act && act.chr && Bone("Body").IsChildOf(act.chr), "KidActionAnimator drives the Banana bones");
            Vector3 right = banana.transform.InverseTransformPoint(Bone("Arm_R").position) - banana.transform.InverseTransformPoint(Bone("Arm_L").position);
            Vector3 face = banana.transform.InverseTransformPoint(Bone("Face").position) - banana.transform.InverseTransformPoint(Bone("Body").position);
            lines.Add("shoulder axis local=" + right.ToString("F3") + " face offset local=" + face.ToString("F3"));
            Assert(right.x > .1f && face.z > 0f, "Banana faces the kid's forward with Arm_R on its right");
            var bounds = skin.bounds;
            lines.Add("standing bounds min=" + bounds.min.ToString("F3") + " max=" + bounds.max.ToString("F3"));
            Assert(Mathf.Abs(bounds.min.y - banana.transform.position.y) < .06f && Mathf.Abs(bounds.size.y - EditorTools.MamaFruitPrefabs.Height) < .12f, "Banana stands on the floor at kid height");

            // walk clip actually moves the limbs
            var anim = banana.GetComponentInChildren<Animator>();
            Vector3 handA = Bone("Hand_L").position - banana.transform.position;
            driver.velocity = banana.transform.forward * 2f; yield return .55f;
            Assert(anim.GetCurrentAnimatorStateInfo(0).IsName("Walk") || anim.GetNextAnimatorStateInfo(0).IsName("Walk"), "moving puppet switches the Banana to its walk");
            float swing = 0f;
            for (int i = 0; i < 6; i++) { yield return .08f; swing = Mathf.Max(swing, Vector3.Distance(Bone("Hand_L").position - banana.transform.position, handA)); }
            lines.Add("walking glove travel metres=" + swing.ToString("F3"));
            Assert(swing > .03f, "walk swings the glove through the bones");
            driver.velocity = Vector3.zero; yield return .5f;

            foreach (string propName in new[] { "Teddy bear", "Hamster" })
            {
                var item = UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).First(x => x.displayName == propName);
                var grip = item.GetComponent<CarryGrip>();
                Assert(grip, propName + " has CarryGrip contacts");
                if (item.IsHeld) foreach (var p in PlayerController.AllPlayers) if (p.Held == item) p.Drop(false);
                Vector3 at = item.transform.position - Vector3.forward * .6f; at.y = .03f;
                banana.transform.SetPositionAndRotation(at, Quaternion.identity); yield return .2f;
                pc.PickUp(item);
                Assert(pc.Held == item && item.GrabProgress < .34f, propName + " has a reach phase before lifting");
                yield return .9f;
                float left = Vector3.Distance(act.LeftGripWorld, grip.LeftWorld), rightErr = Vector3.Distance(act.RightGripWorld, grip.RightWorld);
                float gloveL = Vector3.Distance(Bone("Hand_L").position, grip.LeftWorld), gloveR = Vector3.Distance(Bone("Hand_R").position, grip.RightWorld);
                lines.Add(propName + " wrist target error metres L=" + left.ToString("F3") + " R=" + rightErr.ToString("F3") + "; glove bone to contact L=" + gloveL.ToString("F3") + " R=" + gloveR.ToString("F3"));
                Assert(left < .055f && rightErr < .055f, propName + " Banana wrists reach both authored contacts");
                Assert(gloveL < .07f && gloveR < .07f, propName + " Banana glove bones sit at the contacts");
                var held = item.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                float clear = banana.transform.InverseTransformPoint(held.center).z - banana.transform.InverseTransformPoint(Bone("Body").position).z;
                lines.Add(propName + " held centre ahead of Body bone metres=" + clear.ToString("F3"));
                Assert(clear > .1f, propName + " is carried in front of the Banana");
                MamaRecastEvidence.Render("banana-hold-" + propName.Replace(" ", "-").ToLowerInvariant(), at + new Vector3(1.4f, 1.2f, 2.0f), at + Vector3.up * .6f);
                driver.velocity = Vector3.right * 1.5f; yield return .6f; driver.velocity = Vector3.zero;
                left = Vector3.Distance(act.LeftGripWorld, grip.LeftWorld); rightErr = Vector3.Distance(act.RightGripWorld, grip.RightWorld);
                lines.Add(propName + " after walking wrist error L=" + left.ToString("F3") + " R=" + rightErr.ToString("F3"));
                Assert(left < .055f && rightErr < .055f, propName + " grip holds while the Banana walks");
                var h = pc.ReleaseHeld(); h.OnDropped(pc, Vector3.zero); yield return .4f;
            }
            MamaRecastEvidence.Render("banana-standing", banana.transform.position + banana.transform.forward * 2.2f + Vector3.up * .9f, banana.transform.position + Vector3.up * .55f);
            Assert(errors.Count == 0, "no exceptions during the Banana review");
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("Server forcefully disconnected this plugin") && message.Contains("Authorization failed")) return;
            lock (errors) errors.Add(message + "\n" + trace);
        }

        static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceivedThreaded -= OnLog;
            if (banana) UnityEngine.Object.Destroy(banana);
            lines.Insert(0, result + " passed=" + passed + " (staged offline puppet, not a human playthrough)");
            lock (errors) lines.AddRange(errors);
            File.WriteAllLines(Folder + "review-" + DateTime.Now.ToString("HHmmss") + ".txt", lines);
            Debug.Log("Mama Banana review " + result + ", checks=" + passed);
        }
    }
}
