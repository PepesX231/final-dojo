using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Bug-hunting hooks for the DevBridge (exec MamasComing.Editor.MamaBugProbe.X arg).
    public static class MamaBugProbe
    {
        static PlayerController Kid() { var gm = GameManager.I; return gm ? gm.player : Object.FindFirstObjectByType<PlayerController>(); }
        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        /// Orthographic top-down picture: "x,z,size,file"
        public static void TopShot(string arg)
        {
            var a = arg.Split(',');
            float x = F(a[0]), z = F(a[1]), size = F(a[2]); string file = a.Length > 3 ? a[3] : "top.png";
            var go = new GameObject("ProbeTopCam"); var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = size; cam.transform.SetPositionAndRotation(new Vector3(x, 2.6f, z), Quaternion.Euler(90, 0, 0));
            cam.nearClipPlane = .05f; cam.farClipPlane = 6f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(1024, 1024, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1024, 1024, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../DevBridge/out/" + file), tex.EncodeToPNG());
            cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
            Debug.Log("[BugProbe] topshot " + file + " centre " + x + "," + z + " half " + size);
        }

        /// Everything with a renderer near a point: "x,z,radius"
        public static void Near(string arg)
        {
            var a = arg.Split(','); var c = new Vector3(F(a[0]), 0, F(a[1])); float r = F(a[2]);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.parent) continue;
                var p = t.position; if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)) > r) continue;
                if (!(t.GetComponent<Interactable>() || t.parent.name == "Props" || t.parent.name.Contains("Coop") || t.parent.name == "Party")) continue;
                Debug.Log("[BugProbe] near " + t.parent.name + "/" + t.name + " " + p.ToString("F2") + (t.gameObject.activeInHierarchy ? "" : " (inactive)"));
            }
        }

        /// State of every problem in the scene.
        public static void Hazards()
        {
            foreach (var h in Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(h => h.name))
                Debug.Log("[BugProbe] hz " + h.GetType().Name + " '" + h.name + "' act=" + h.Active + " fix=" + h.Fixed + " fail=" + h.Failed + " prog=" + h.Progress.ToString("F2") + " pos=" + h.transform.position.ToString("F1") + " puzzle=" + h.puzzle);
            var fr = Object.FindFirstObjectByType<FridgeHazard>();
            if (fr)
            {
                var f = fr.rottenFood;
                Debug.Log("[BugProbe] fridge spoiled=" + fr.Spoiled + " door=" + fr.DoorOpen + " doorT=" + (fr.door ? fr.door.name : "NULL") + " food=" + (f ? f.name + " active=" + f.gameObject.activeInHierarchy + " pos=" + f.transform.position.ToString("F2") + " removed=" + f.Removed + " disposed=" + f.Disposed + " held=" + f.IsHeld + " fridgeRef=" + (f.fridge == fr) : "NULL"));
            }
        }

        /// Teleport the kid in front of an object by name (or type name) and print what E would do.
        public static void Go(string name)
        {
            var pc = Kid(); if (!pc) return;
            Interactable it = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == name || i.GetType().Name == name);
            if (!it) { Debug.Log("[BugProbe] no interactable " + name); return; }
            var ip = it.InteractPoint; float y = pc.transform.position.y; Vector3 d = Vector3.back, to = ip; bool found = false;
            // try spots all around it: free floor space with a clear line to the thing
            foreach (float dist in new[] { .75f, 1f, .55f, 1.25f })
            {
                if (found) break;
                for (int i = 0; i < 16 && !found; i++)
                {
                    float ang = i * 22.5f * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang));
                    var c = new Vector3(ip.x, 0, ip.z) + dir * Mathf.Min(dist, it.interactRadius * .9f); c.y = .05f;
                    if (Physics.CheckCapsule(c + Vector3.up * .35f, c + Vector3.up * 1.0f, .3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!Physics.Raycast(c + Vector3.up * 2f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore)) continue;   // must stand on something
                    d = dir; to = c; to.y = y; found = true;
                }
            }
            if (!found) Debug.Log("[BugProbe] no free spot near " + it.name);
            pc.Motor.Teleport(to, Quaternion.LookRotation(-d.normalized));
            Debug.Log("[BugProbe] went to " + it.name + " at " + ip.ToString("F2"));
        }

        public static void Target() { var pc = Kid(); if (pc) Debug.Log("[BugProbe] held=" + (pc.Held ? pc.Held.name : "-") + " target=" + (pc.Target ? pc.Target.name + " (" + pc.Target.GetType().Name + ") '" + pc.Target.GetPrompt(pc) + "' mg=" + pc.Target.MiniGameKind(pc) : "none")); }

        /// Press E on the current target exactly like the player does (minigames start; "win" finishes it).
        public static void E(string mode)
        {
            var pc = Kid(); var t = pc ? pc.Target : null; if (!t) { Debug.Log("[BugProbe] E: no target"); return; }
            var mg = t.MiniGameKind(pc);
            if (mg != MiniGame.Kind.None && MiniGame.I)
            {
                Debug.Log("[BugProbe] E starts minigame " + mg + " on " + t.name);
                if (mode == "win") NetGame.Interact(t, pc); else MiniGame.I.Begin(mg, t, pc, t.MiniGameTitle);
            }
            else { Debug.Log("[BugProbe] E interacts " + t.name); NetGame.Interact(t, pc); }
        }

        public static void Drop() { var pc = Kid(); if (pc && pc.Held) { pc.GetType().GetMethod("Drop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(bool) }, null)?.Invoke(pc, new object[] { false }); Debug.Log("[BugProbe] dropped"); } }

        public static void Activate(string type) { foreach (var h in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)) if (h.GetType().Name == type || h.name == type) { if (h is CoopPuzzle) CoopPuzzle.AllowSolo = true; h.Activate(); Debug.Log("[BugProbe] activated " + h.name); } }

        public static void Give(string kind)
        {
            var pc = Kid(); if (!pc) return;
            foreach (var t in Object.FindObjectsByType<ToolItem>(FindObjectsSortMode.None)) if (t.kind.ToString() == kind) { pc.PickUp(t); Debug.Log("[BugProbe] gave " + t.name); return; }
            Debug.Log("[BugProbe] no tool " + kind);
        }
            public static void Cancel() { if (MiniGame.I) MiniGame.I.Cancel(); var pc = Kid(); if (pc && pc.Held) Drop(); }
        public static void Skip() { if (GameManager.I) GameManager.I.SkipToResults(); }
        public static void Calm() { foreach (var f in FireSource.All) if (f && f.burning) f.Extinguish(99f); }
        public static void Poufs()
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("Pouf")) Debug.Log("[BugProbe] " + r.name + " top=" + r.bounds.max.y.ToString("F2") + " c=" + r.bounds.center.ToString("F2") + " parent=" + r.transform.parent.name + " ppos=" + r.transform.parent.position.ToString("F2") + " pscale=" + r.transform.lossyScale.ToString("F2"));
            foreach (var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)) if (c.name.StartsWith("Pouf")) Debug.Log("[BugProbe] col " + c.name + " top=" + c.bounds.max.y.ToString("F2"));
            var pc = Kid(); if (pc) { Debug.Log("[BugProbe] kid y=" + pc.transform.position.y.ToString("F2") + " grounded? model feet=" + MamaPosedKid.TightBounds(pc.transform).min.y.ToString("F2")); var cc = pc.GetComponent<CharacterController>(); if (cc) Debug.Log("[BugProbe] cc center=" + cc.center + " h=" + cc.height + " r=" + cc.radius + " skin=" + cc.skinWidth); var caps = pc.GetComponent<CapsuleCollider>(); if (caps) Debug.Log("[BugProbe] caps center=" + caps.center + " h=" + caps.height + " r=" + caps.radius); }
        }
        public static void Feet()
        {
            var pc = Kid(); if (!pc) return; float lo = 99f; string who = "";
            foreach (var r in pc.GetComponentsInChildren<Renderer>()) { if (!r.enabled || r is ParticleSystemRenderer || r.name.Contains("Crown") || r.name.Contains("Blob")) continue; if (r.bounds.min.y < lo) { lo = r.bounds.min.y; who = r.name; } }
            Debug.Log("[BugProbe] kid y=" + pc.transform.position.y.ToString("F3") + " scale=" + pc.transform.lossyScale.x.ToString("F2") + " lowest renderer=" + who + " minY=" + lo.ToString("F3"));
            foreach (Transform c in pc.transform) Debug.Log("[BugProbe] child " + c.name + " lpos=" + c.localPosition.ToString("F3") + " lscale=" + c.localScale.ToString("F2"));
        }
        public static void Scale(string s) { var pc = Kid(); if (pc) pc.transform.localScale = Vector3.one * F(s); }
        public static void Chain(string bone)
        {
            var pc = Kid(); if (!pc) return; Transform t = null;
            foreach (var x in pc.GetComponentsInChildren<Transform>(true)) if (x.name == bone) { t = x; break; }
            for (; t && t != pc.transform.parent; t = t.parent) Debug.Log("[BugProbe] chain " + t.name + " lpos=" + t.localPosition.ToString("F3") + " lscale=" + t.localScale.ToString("F2") + " wy=" + t.position.y.ToString("F3"));
        }
        public static void Tops()
        {
            var pc = Kid(); if (!pc) return;
            foreach (var r in pc.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy && r.bounds.max.y > pc.transform.position.y + 1.2f) Debug.Log("[BugProbe] top " + r.GetType().Name + " " + r.transform.parent.name + "/" + r.name + " maxY=" + r.bounds.max.y.ToString("F2") + " size=" + r.bounds.size.ToString("F2"));
        }
        public static void PuzzleState()
        {
            if (!Puzzles.I) { Debug.Log("[BugProbe] no puzzles"); return; }
            var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var t = typeof(Puzzles);
            Debug.Log("[BugProbe] puzzle kind=" + t.GetField("kind", f).GetValue(Puzzles.I) + " timeLeft=" + t.GetField("timeLeft", f).GetValue(Puzzles.I) + " limit=" + t.GetField("limit", f).GetValue(Puzzles.I) + " step=" + t.GetField("step", f).GetValue(Puzzles.I) + " a=" + t.GetField("a", f).GetValue(Puzzles.I) + " mgActive=" + (MiniGame.I && MiniGame.I.Active));
        }
        public static void PuzzleFreeze(string av) { if (!Puzzles.I) return; var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; typeof(Puzzles).GetField("a", f).SetValue(Puzzles.I, F(av)); typeof(Puzzles).GetField("b", f).SetValue(Puzzles.I, 0f); }
        public static void Uni()
        {
            var u = Object.FindFirstObjectByType<UnicornHazard>(); if (!u) return; var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var who = (PlayerController[])typeof(CoopPuzzle).GetField("who", f).GetValue(u); var seen = (float[])typeof(CoopPuzzle).GetField("seen", f).GetValue(u);
            string w = ""; for (int i = 0; i < who.Length; i++) w += (who[i] ? who[i].name : "-") + "@" + (Time.time - seen[i]).ToString("F2") + " ";
            Debug.Log("[BugProbe] uni act=" + u.Active + " fix=" + u.Fixed + " urgent=" + u.Urgent + " air=" + typeof(UnicornHazard).GetField("air", f).GetValue(u) + " who=" + w + " sim=" + (GameManager.I == null || GameManager.I.SimRunning) + " auth=" + NetGame.IsAuthority);
        }
        public static void Clock() { Debug.Log("[BugProbe] timeScale=" + Time.timeScale + " dt=" + Time.deltaTime + " frame=" + Time.frameCount + " solver=" + (GameObject.Find("AutoSolve") != null) + " gm=" + (GameManager.I ? GameManager.I.state.ToString() : "-") + " settings=" + MamaSettings.Open + " howto=" + MamaHowToPlay.Open); }
        public static void World()
        {
            string st = "";
            foreach (var x in Object.FindObjectsByType<Stove>(FindObjectsSortMode.None)) st += " stove.on=" + x.on;
            foreach (var x in Object.FindObjectsByType<WaterTap>(FindObjectsSortMode.None)) st += " tap[" + x.name + "].running=" + x.running;
            foreach (var x in Object.FindObjectsByType<MainValve>(FindObjectsSortMode.None)) st += " mainValve.open=" + x.open;
            int fires = 0; foreach (var f in FireSource.All) if (f && f.burning) fires++;
            st += " power=" + PowerGrid.On + " fires=" + fires + " water=" + (FloodSim.I ? FloodSim.I.TotalLiters.ToString("F0") : "-") + " smoke=" + (SmokeSim.I ? SmokeSim.I.IsProblem.ToString() : "-");
            Debug.Log("[BugProbe] world" + st);
        }
        public static void Problems()
        {
            int n = 0;
            foreach (var m in new System.Collections.Generic.List<IMess>(MessRegistry.All)) if (MessRegistry.Alive(m) && m.IsProblem) { n++; Debug.Log("[BugProbe] problem " + m.Kind + " '" + m.Label + "' hidden=" + m.IsHidden + " sev=" + m.Severity); }
            Debug.Log("[BugProbe] problems total " + n);
        }
        public static void TimeScale(string x) { Time.timeScale = F(x); }
        public static void Summary()
        {
            string s0 = "";
            foreach (var h in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).OrderBy(h => h.name)) s0 += h.name.Split(' ')[0] + ":" + (h.Fixed ? "F" : h.Failed ? "X" : h.Active ? "A" + Mathf.RoundToInt(h.Progress * 100) : "-") + " ";
            var gm = GameManager.I;
            Debug.Log("[BugProbe] t=" + (gm ? gm.TimeLeft.ToString("F0") : "?") + " state=" + (gm ? gm.state.ToString() : "?") + " | " + s0);
        }
        public static void Admin(string n)
        {
            var m = Object.FindFirstObjectByType<MamaAdminMenu>(); if (!m) { Debug.Log("[BugProbe] no admin menu"); return; }
            var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            if (n == "show") typeof(MamaAdminMenu).GetMethod("Show", f).Invoke(m, null);
            else typeof(MamaAdminMenu).GetMethod("Do", f).Invoke(m, new object[] { int.Parse(n) });
        }
        public static void TripPower() { PowerGrid.Trip("probe", "probe"); }
        /// Edit mode: "name,x,y,z,yaw" moves a scene object and saves the scene.
        public static void Place(string arg)
        {
            var a = arg.Split(','); var go = GameObject.Find(a[0]); if (!go) { foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t.name == a[0]) { go = t.gameObject; break; } }
            if (!go) { Debug.Log("[BugProbe] no object " + a[0]); return; }
            go.transform.SetPositionAndRotation(new Vector3(F(a[1]), F(a[2]), F(a[3])), Quaternion.Euler(0, F(a[4]), 0));
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
            Debug.Log("[BugProbe] placed " + a[0] + " at " + go.transform.position);
        }
    }
}
