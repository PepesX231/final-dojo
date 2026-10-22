using DreamRush.CameraSystem;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Debugging
{
    /// <summary>Static dev helpers callable from DevBridge (`exec DreamRush.Debugging.DevCommands.X`) or hotkeys.</summary>
    public static class DevCommands
    {
        /// <summary>Orbit the hub camera to look at the player's face (art review).</summary>
        public static string FaceCamera() => SetCameraRelativeYaw(180f, 12f, 3.2f);

        public static string BackCamera() => SetCameraRelativeYaw(0f, 18f, 5.5f);

        public static string SideCamera() => SetCameraRelativeYaw(90f, 10f, 3.2f);

        /// <summary>Reports animated part transforms of the player's visual (debug the procedural animator).</summary>
        public static string DescribePlayerRig()
        {
            var anim = Object.FindFirstObjectByType<AppleProceduralAnimator>();
            if (anim == null) return "no AppleProceduralAnimator";
            var m = anim.motor;
            var sb = new System.Text.StringBuilder($"rig={anim.HasRig} phase={anim.Phase:F2} move01={anim.Move01:F3} air01={anim.Air01:F3} " +
                (m != null ? $"grounded={m.IsGrounded} vy={m.VerticalVelocity:F2} speed={m.PlanarVelocity.magnitude:F2} dt={Time.deltaTime:F4} " : "motor=null "));
            foreach (var t in anim.GetComponentsInChildren<Transform>())
                if (t != anim.transform)
                    sb.Append($"[{t.name} lp={t.localPosition:F3} lr={t.localEulerAngles:F1}] ");
            return sb.ToString();
        }

        public static string StartSleep()
        {
            var bed = Object.FindFirstObjectByType<Interaction.BedInteractable>();
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (bed == null) return "no bed";
            return "sleep started=" + Flow.GameFlowManager.Get().BeginSleep(bed, motor);
        }

        public static string PickCard(string index)
        {
            var p = Object.FindFirstObjectByType<UI.CardSelectPanel>();
            if (p == null) return "no card panel";
            p.Pick(int.Parse(index));
            return "picked " + index;
        }

        public static string EquipCard(string id)
        {
            var c = Object.FindFirstObjectByType<Cards.PlayerCardController>();
            var lib = Cards.CardLibrary.Load();
            var def = lib != null ? lib.Find(id) : null;
            if (c == null || def == null) return "no controller/card";
            c.Equip(def); c.ResetCooldown();
            return "equipped " + id;
        }

        /// <summary>Turn the apple toward the chair and use the held card. Optional arg: first stand this many metres from it.</summary>
        public static string UseCardOnChair(string standDistance)
        {
            var c = Object.FindFirstObjectByType<Cards.PlayerCardController>();
            var chair = Object.FindFirstObjectByType<Boss.BossChairController>();
            if (c == null) return "no card controller";
            if (chair != null)
            {
                if (!string.IsNullOrEmpty(standDistance) && float.TryParse(standDistance, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float d))
                {
                    Vector3 center = chair.arenaBounds.center; center.y = chair.transform.position.y;
                    Vector3 away = center - chair.transform.position; away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                    var m = c.GetComponent<PlayerMotor3D>();
                    m.Teleport(chair.transform.position + away.normalized * d + Vector3.up * 0.15f, Quaternion.LookRotation(-away));
                }
                Vector3 to = chair.transform.position - c.transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) c.transform.rotation = Quaternion.LookRotation(to);
            }
            var r = c.TryUse();
            return $"used={r.Used} msg={r.Message} chair={(chair != null ? chair.State.ToString() : "-")}";
        }

        /// <summary>Art review: park the main camera in front of the chair ("off" restores the stage camera). Arg = yaw offset in degrees.</summary>
        public static string FocusChair(string arg)
        {
            var cam = Camera.main;
            var stage = Object.FindFirstObjectByType<StageCameraController>();
            var chair = Object.FindFirstObjectByType<Boss.BossChairController>();
            if (arg == "off") { if (stage != null) stage.enabled = true; if (chair != null) chair.DevHold = false; return "stage camera restored"; }
            if (cam == null || chair == null) return "no camera/chair";
            if (stage != null) stage.enabled = false;
            chair.DevHold = true;
            float yaw = 0f; float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out yaw);
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * chair.transform.forward;
            Vector3 target = chair.transform.position + Vector3.up * 1.0f;
            cam.transform.position = target + fwd * 3.6f + Vector3.up * 0.9f;
            cam.transform.LookAt(target);
            return $"camera at chair, state={chair.State}";
        }

        /// <summary>Art review: force a chair state for a moment (e.g. Panic, Exhausted, Frozen).</summary>
        public static string ForceChairState(string state)
        {
            var chair = Object.FindFirstObjectByType<Boss.BossChairController>();
            if (chair == null) return "no chair";
            var s = (Boss.ChairState)System.Enum.Parse(typeof(Boss.ChairState), state);
            if (chair.ApplyStatus(s, 30f, "Dev")) return "status " + s;
            chair.SetStateForDev(s);
            return "state " + s;
        }

        /// <summary>Art review: park the main camera relative to the player's facing (0 = front). Disables camera controllers until "off".</summary>
        public static string FocusPlayer(string arg)
        {
            var cam = Camera.main;
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            var st = Object.FindFirstObjectByType<StageCameraController>();
            if (arg == "off") { if (tp != null) tp.enabled = true; if (st != null) st.enabled = true; return "cameras restored"; }
            if (cam == null || m == null) return "no camera/player";
            if (tp != null) tp.enabled = false;
            if (st != null) st.enabled = false;
            float yaw = 0f; float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out yaw);
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * m.transform.forward;
            Vector3 target = m.transform.position + Vector3.up * 0.65f;
            cam.transform.position = target + dir * 2.4f + Vector3.up * 0.35f;
            cam.transform.LookAt(target);
            return "camera on player yaw " + yaw;
        }

        /// <summary>VFX review: "Kind" or "all" — spawns in front of the player (presentation only).</summary>
        public static string Vfx(string arg)
        {
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (m == null) return "no player";
            if (Presentation.VFXLibrary.Instance == null) return "no VFXLibrary in Resources";
            var kinds = (Presentation.VFXKind[])System.Enum.GetValues(typeof(Presentation.VFXKind));
            int i = 0;
            foreach (var k in kinds)
            {
                if (arg != "all" && k.ToString() != arg) continue;
                Vector3 p = m.transform.position + m.transform.forward * 3f + m.transform.right * ((i % 4) - 1.5f) * 1.6f + Vector3.up * (i / 4) * 1.2f;
                Presentation.VFX.Play(k, p);
                i++;
            }
            return $"spawned {i} (total {Presentation.VFX.Spawned})";
        }

        public static string TimeScale(string arg)
        {
            Time.timeScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
            return "timeScale=" + Time.timeScale;
        }

        public static string Title(string arg)
        {
            if (arg == "off") { UI.TitleScreen.Enabled = false; UI.TitleScreen.Dismiss(); return "title off"; }
            UI.TitleScreen.ShownThisSession = false;
            UI.TitleScreen.Enabled = true;
            return UI.TitleScreen.ShowIfFirstBoot() != null ? "title shown" : "title not shown";
        }

        public static string ShowCardSelect()
        {
            var lib = Cards.CardLibrary.Load();
            if (lib == null) return "no lib";
            UI.CardSelectPanel.Show(Cards.CardDrawManager.DrawThree(lib.starterPool, 1234), 1);
            return "card select shown";
        }

        public static string BossIntro() { UI.BossIntroCard.Show("THE CHAIR", "THRONE OF REFUSAL"); return "boss intro"; }

        public static string Toast(string arg) { UI.UIRoot.Get().Toast(arg, 4f); return "toast"; }

        public static string Pause() { UI.PauseMenuPanel.Show(true, null); return "pause"; }

        public static string Transition(string arg)
        {
            var o = UI.TransitionOverlay.Get();
            o.StartCoroutine(arg == "open" ? o.Open(0.9f) : o.Close(0.9f, arg));
            return "transition " + arg;
        }

        public static string W1Jump(string arg)
        {
            var d = World1.ChapterDirector.Current;
            if (d == null) return "no chapter director";
            d.DevJump(int.Parse(arg));
            return "chapter " + d.Index + " " + d.Chapter.title;
        }

        /// <summary>World 1 P3 review: "start" (leap now), "step,N" (put Pip on climb step N), "view" (wide shot of the tower).</summary>
        public static string ThroneClimb(string arg)
        {
            var c = Object.FindFirstObjectByType<Boss.ThroneClimb>();
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (c == null || m == null) return "no throne climb";
            var parts = arg.Split(',');
            switch (parts[0])
            {
                case "start": c.StartCoroutine(c.Run()); return "leap";
                case "step":
                    {
                        var st = c.steps[int.Parse(parts[1])];
                        Vector3 p = st.position + Vector3.up * 0.5f;
                        Vector3 look = c.transform.position - p; look.y = 0;
                        m.Teleport(p, Quaternion.LookRotation(Vector3.Cross(Vector3.up, look.normalized)));
                        return "on step " + parts[1];
                    }
                case "view":
                    {
                        var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
                        if (tp != null) tp.enabled = false;
                        var cam = Camera.main;
                        Vector3 target = c.transform.position + Vector3.up * 5.5f;
                        cam.transform.position = c.transform.position + new Vector3(9f, 6.5f, -15f);
                        cam.transform.LookAt(target);
                        return "view";
                    }
            }
            return "?";
        }

        /// <summary>Lobby review: theme id (morning|afternoon|sunset|midnight|clock).</summary>
        public static string LobbyTheme(string arg)
        {
            Hub.LobbyTheme.OverrideId = arg == "clock" ? "" : arg;
            if (Hub.LobbyThemeDirector.Current != null) Hub.LobbyThemeDirector.Current.Apply(Hub.LobbyTheme.Current);
            return "theme " + Hub.LobbyTheme.Current.name;
        }

        /// <summary>Lobby review: select a menu item by key (DREAM, WALK, BED, DIARY, SETTINGS, QUIT).</summary>
        public static string LobbySelect(string arg)
        {
            if (UI.TitleScreen.Current == null) { UI.TitleScreen.Enabled = true; UI.TitleScreen.ShowIfFirstBoot(); }
            UI.TitleScreen.Current?.SelectKey(arg);
            return "selected " + arg;
        }

        /// <summary>Lobby review: activate a menu item by key (as if clicked); "back" returns to the lobby.</summary>
        public static string LobbyActivate(string arg)
        {
            if (UI.TitleScreen.Current == null) { UI.TitleScreen.Enabled = true; UI.TitleScreen.ShowIfFirstBoot(); }
            if (UI.TitleScreen.Current == null) return "no lobby";
            if (arg == "back") { UI.TitleScreen.Current.BackToLobby(); return "back"; }
            UI.TitleScreen.Current.ActivateKey(arg);
            return "activated " + arg;
        }

        /// <summary>Boss review: "begin" | "auto,0/1" | "attack,Name" | "hp,0.5" | "pip,dist" | "lock".</summary>
        public static string Midnight(string arg)
        {
            var b = Object.FindFirstObjectByType<Combat.CountMidnight>();
            if (b == null) return "no boss";
            var a = arg.Split(',');
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            switch (a[0])
            {
                case "begin": b.DevBeginFight(); return "fight";
                case "auto": b.AutoFight = a[1] == "1"; return "auto " + b.AutoFight;
                case "attack": b.DoAttack(a[1]); return "attack " + a[1];
                case "hp": b.DevSetHP(b.maxHP * float.Parse(a[1], inv)); return "hp " + b.HP;
                case "pip":
                {
                    var m = Object.FindFirstObjectByType<PlayerMotor3D>();
                    Vector3 p = b.transform.position + b.transform.forward * float.Parse(a[1], inv); p.y = 0.3f;
                    m.Teleport(p, Quaternion.LookRotation(-b.transform.forward));
                    var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
                    if (tp != null) { tp.Yaw = Quaternion.LookRotation(-b.transform.forward).eulerAngles.y; tp.Pitch = 12f; tp.SnapToTarget(); }
                    return "pip placed";
                }
                case "lock": Combat.PlayerCombat.Current?.ToggleLock(); return "lock";
            }
            return "?";
        }

        /// <summary>Style review: "name,value" sets a ComicPost global (_DR_ + name), e.g. "Saturate,-1", "Glitch,1".</summary>
        public static string Style(string arg)
        {
            var a = arg.Split(',');
            Shader.SetGlobalFloat("_DR_" + a[0], float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture));
            var sp = Look.StylePack.Current;
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            string feats = "";
            if (rp != null)
            {
                var r = rp.GetRenderer(0);
                var prop = r.GetType().GetProperty("rendererFeatures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (prop != null && prop.GetValue(r) is System.Collections.IList list) foreach (var f in list) feats += ((Object)f).name + "(" + ((UnityEngine.Rendering.Universal.ScriptableRendererFeature)f).isActive + ") ";
            }
            return "set _DR_" + a[0] + " (stylepack " + (sp != null ? "present" : "none") + ", ink=" + Shader.GetGlobalFloat("_DR_Ink") + ") rp=" + (rp != null ? rp.name : "null") + " feats=" + feats;
        }

        /// <summary>Combat review: "move,slowmo" e.g. "light,0.08" starts the move then sets timeScale (freeze-frame for screenshots).</summary>
        public static string Move(string arg)
        {
            var a = arg.Split(',');
            var c = Object.FindFirstObjectByType<Combat.PlayerCombat>();
            if (c == null) return "no PlayerCombat";
            string r = c.DevDo(a[0]);
            if (a.Length > 1) Time.timeScale = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
            return r;
        }

        /// <summary>Debug: disable every Behaviour whose type name matches (e.g. "StylePack"); also lists the camera's components.</summary>
        public static string Disable(string typeName)
        {
            int n = 0;
            foreach (var b in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)) if (b.GetType().Name == typeName) { b.enabled = false; n++; }
            string comps = "";
            if (Camera.main != null) foreach (var c in Camera.main.GetComponents<Component>()) comps += c.GetType().Name + " ";
            return $"disabled {n} {typeName}; cam: {comps}";
        }

        /// <summary>Look debugging: "off" disables every Volume, "on" re-enables; "tm,none|neutral|aces" forces tonemapping on the active ones.</summary>
        public static string Post(string arg)
        {
            var a = arg.Split(','); string r = "";
            foreach (var v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (a[0] == "off") v.enabled = false; else if (a[0] == "on") v.enabled = true;
                else if (a[0] == "tm" && v.profile != null && v.profile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tm))
                {
                    tm.mode.Override(a[1] == "none" ? UnityEngine.Rendering.Universal.TonemappingMode.None : a[1] == "aces" ? UnityEngine.Rendering.Universal.TonemappingMode.ACES : UnityEngine.Rendering.Universal.TonemappingMode.Neutral);
                    r += v.profile.name + "=" + tm.mode.value + " ";
                }
            }
            var cam = Camera.main; var data = cam != null ? cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
            return r + " post=" + (data != null && data.renderPostProcessing) + " hdr=" + (cam != null && cam.allowHDR);
        }

        /// <summary>Lists active UI graphics that cover most of the screen (finds stray full-screen tints).</summary>
        public static string Covers()
        {
            string r = "";
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled) continue;
                var rt = g.rectTransform; var c = new Vector3[4]; rt.GetWorldCorners(c);
                var canvas = g.canvas; if (canvas == null) continue;
                float w = Mathf.Abs(c[2].x - c[0].x), h = Mathf.Abs(c[2].y - c[0].y);
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && w > Screen.width * 0.8f && h > Screen.height * 0.8f && g.color.a > 0.01f)
                {
                    var cg = g.GetComponentInParent<CanvasGroup>();
                    r += $"[{Path(g.transform)} a={g.color.a:F2} col={g.color} cg={(cg != null ? cg.alpha : 1):F2}] ";
                }
            }
            foreach (var cam in Camera.allCameras) r += $" cam {cam.name} depth={cam.depth}";
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (cv.isRootCanvas) r += $" canvas {Path(cv.transform)} mode={cv.renderMode} order={cv.sortingOrder} cam={(cv.worldCamera != null ? cv.worldCamera.name : "-")}";
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled || g.color.a < 0.05f) continue;
                var c = new Vector3[4]; g.rectTransform.GetWorldCorners(c);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(g.canvas != null && g.canvas.renderMode != RenderMode.ScreenSpaceOverlay ? g.canvas.worldCamera : null, c[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(g.canvas != null && g.canvas.renderMode != RenderMode.ScreenSpaceOverlay ? g.canvas.worldCamera : null, c[2]);
                if (Mathf.Abs(b.x - a.x) > Screen.width * 0.6f && Mathf.Abs(b.y - a.y) > Screen.height * 0.6f) r += $" BIG[{Path(g.transform)} a={g.color.a:F2} {g.GetType().Name}]";
            }
            return r;
        }

        static string Path(Transform t) { string p = t.name; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; } return p; }

        /// <summary>Lists every post Volume (active, global, priority, weight, profile) — look debugging.</summary>
        public static string Volumes()
        {
            string r = "";
            foreach (var v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                r += $"[{v.name} on={v.isActiveAndEnabled} global={v.isGlobal} pri={v.priority} w={v.weight} prof={(v.sharedProfile != null ? v.sharedProfile.name : "null")}] ";
            var ui = GameObject.Find("TransitionOverlay");
            return r + (ui != null ? " overlay=" + ui.activeInHierarchy : "");
        }

        /// <summary>Review: put Pip at "x,y,z,yaw" with the follow camera behind him.</summary>
        public static string PipTo(string arg)
        {
            var a = arg.Split(','); var inv = System.Globalization.CultureInfo.InvariantCulture;
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            float yaw = a.Length > 3 ? float.Parse(a[3], inv) : 0f;
            m.Teleport(new Vector3(float.Parse(a[0], inv), float.Parse(a[1], inv), float.Parse(a[2], inv)), Quaternion.Euler(0, yaw, 0));
            var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            if (tp != null) { tp.enabled = true; tp.Yaw = yaw; tp.Pitch = a.Length > 4 ? float.Parse(a[4], inv) : 12f; tp.SnapToTarget(); }
            return "pip at " + arg;
        }

        public static string GalaxySelectIndex(string arg)
        {
            var g = Flow.GalaxySelect.Current;
            if (g == null) return "no galaxy";
            g.Select(int.Parse(arg));
            return "galaxy " + g.Selected.title;
        }

        /// <summary>Art review: park the camera in front of a named object ("alarm" | "chair"), distance d.</summary>
        public static string Frame(string arg)
        {
            var parts = arg.Split(',');
            Transform t = parts[0] == "alarm" ? (OpenWorld.NightmareAlarm.Active != null ? OpenWorld.NightmareAlarm.Active.transform : null)
                        : Object.FindFirstObjectByType<OpenWorld.OpenWorldChair>()?.transform;
            if (t == null) return "nothing to frame";
            float d = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 10f;
            var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            if (tp != null) tp.enabled = false;
            var cam = Camera.main;
            Vector3 target = t.position + Vector3.up * (parts[0] == "alarm" ? 3f : 0.8f);
            cam.transform.position = target + t.forward * d + Vector3.up * d * 0.25f + t.right * d * 0.3f;
            cam.transform.LookAt(target);
            return "framed " + parts[0];
        }

        /// <summary>M18 close-up: "front|side|back|q,dist[,height]" — parks the camera around Pip (third-person cam off).</summary>
        public static string PipCam(string arg)
        {
            var parts = arg.Split(','); var ci = System.Globalization.CultureInfo.InvariantCulture;
            var m = Object.FindFirstObjectByType<PlayerMotor3D>(); var cam = Camera.main;
            if (m == null || cam == null) return "no pip/cam";
            foreach (var c in Object.FindObjectsByType<ThirdPersonCameraController>(FindObjectsSortMode.None)) c.enabled = false;
            float d = parts.Length > 1 ? float.Parse(parts[1], ci) : 2.4f, h = parts.Length > 2 ? float.Parse(parts[2], ci) : 0.8f;
            var t = m.transform;
            Vector3 dir = parts[0] == "side" ? t.right : parts[0] == "back" ? -t.forward : parts[0] == "q" ? (t.forward + t.right).normalized : t.forward;
            cam.transform.position = t.position + dir * d + Vector3.up * h;
            cam.transform.LookAt(t.position + Vector3.up * 0.65f);
            return "pipcam " + parts[0];
        }

        /// <summary>Art review: put Pip 12 m in front of THE ALARM, camera behind Pip looking at it.</summary>
        public static string AlarmHunt()
        {
            var a = OpenWorld.NightmareAlarm.Active;
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (a == null || m == null) return "no alarm/player";
            Vector3 p = a.transform.position + a.transform.forward * 12f;
            p.y = OpenWorld.DreamlandTerrain.Height(p.x, p.z) + 0.3f;
            m.Teleport(p, Quaternion.LookRotation(-a.transform.forward));
            var tp = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            if (tp != null) { tp.enabled = true; tp.Yaw = Quaternion.LookRotation(a.transform.position - p).eulerAngles.y; tp.Pitch = 8f; tp.SnapToTarget(); }
            return "hunt";
        }

        public static string SqueezeEyes()
        {
            var f = Object.FindFirstObjectByType<AppleFace>();
            if (f == null || !f.HasFace) return "no face";
            f.Squeeze(5f);
            return "eyes shut for 5 s";
        }

        public static string GiveMemory(string n)
        {
            var save = Core.SaveManager.GetOrCreate();
            save.Data.progression.dreamMemory += int.Parse(n);
            save.Save();
            return "dream memory = " + save.Data.progression.dreamMemory;
        }

        public static string OpenBedUpgrades()
        {
            var p = UI.ModalPanel.Create<UI.BedUpgradePanel>("UPGRADE BED", new Vector2(1180, 820));
            UI.UIRoot.Get().Push(p);
            return "opened";
        }

        public static string BuyUpgrade(string id)
        {
            var p = Object.FindFirstObjectByType<UI.BedUpgradePanel>();
            if (p == null) return "panel not open";
            return "bought=" + p.Buy((Progression.BedUpgrade)System.Enum.Parse(typeof(Progression.BedUpgrade), id));
        }

        /// <summary>Art review: "x,y,z>tx,ty,tz" puts the main camera at a world point looking at another (controllers disabled).</summary>
        public static string LookFrom(string arg)
        {
            var parts = arg.Split('>');
            var cam = Camera.main;
            if (cam == null || parts.Length != 2) return "usage x,y,z>tx,ty,tz";
            foreach (var c in Object.FindObjectsByType<ThirdPersonCameraController>(FindObjectsSortMode.None)) c.enabled = false;
            foreach (var c in Object.FindObjectsByType<StageCameraController>(FindObjectsSortMode.None)) c.enabled = false;
            cam.transform.position = V(parts[0]);
            cam.transform.LookAt(V(parts[1]));
            return "camera placed";
        }

        static Vector3 V(string s)
        {
            var p = s.Split(',');
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            return new Vector3(float.Parse(p[0], ci), float.Parse(p[1], ci), float.Parse(p[2], ci));
        }

        /// <summary>"memory,mattress,pillow,blanket" — restore a save after dev testing.</summary>
        public static string SetProgression(string arg)
        {
            var v = arg.Split(',');
            var p = Core.SaveManager.GetOrCreate();
            p.Data.progression.dreamMemory = int.Parse(v[0]); p.Data.progression.mattressLevel = int.Parse(v[1]);
            p.Data.progression.pillowLevel = int.Parse(v[2]); p.Data.progression.blanketLevel = int.Parse(v[3]);
            p.Save();
            return $"progression = {arg}";
        }

        public static string CloseUI()
        {
            if (UI.UIRoot.Exists) UI.UIRoot.Get().CloseAll();
            UI.TitleScreen.Dismiss();
            foreach (var cs in Object.FindObjectsByType<UI.CardSelectPanel>(FindObjectsSortMode.None)) Object.Destroy(cs.gameObject);
            Time.timeScale = 1f;
            return "ui closed, timeScale=1";
        }

        /// <summary>Art capture: "on" freezes the player motor (ignores stray input), "off" resumes.</summary>
        public static string HoldPlayer(string arg)
        {
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (m == null) return "no player";
            m.AutoStep = arg != "on";
            return "player AutoStep=" + m.AutoStep;
        }

        sealed class ScriptedDrive : IPlayerCommandSource
        {
            public PlayerCommand Cmd;
            public PlayerCommand ReadCommand() { var c = Cmd; Cmd.JumpPressed = false; return c; }
        }
        static ScriptedDrive _drive;

        /// <summary>Animation review: "walk", "sprint", "jump", "sprintjump", "stop" (restores the real input).</summary>
        public static string Drive(string mode)
        {
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (m == null) return "no player";
            m.AutoStep = true;
            if (mode == "stop") { m.CommandSource = m.GetComponent<PlayerInputReader>(); _drive = null; return "input restored"; }
            _drive ??= new ScriptedDrive();
            m.CommandSource = _drive;
            var c = new PlayerCommand { Move = new Vector2(0, 1) };
            if (mode.Contains("sprint")) c.Sprint = true;
            if (mode.Contains("world")) m.CameraTransform = null;   // world-space drive: the camera can orbit for side shots
            if (mode.Contains("jump")) { c.JumpPressed = true; c.JumpHeld = true; }
            if (mode == "idle") c.Move = Vector2.zero;
            if (mode == "turn") c.Move = new Vector2(0, -1);
            _drive.Cmd = c;
            return "driving " + mode;
        }

        public static string Grandpa()
        {
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (m == null) return "no player";
            var t = m.GetComponent<Cards.PlayerTransformation>(); if (t == null) t = m.gameObject.AddComponent<Cards.PlayerTransformation>();
            t.BecomeGrandpa(60f);
            return "grandpa for 60 s";
        }

        public static string ForceVictory() => "victory=" + Flow.GameFlowManager.Get().EndDream(true, "Exhaustion", null);
        public static string ForceDefeat() => "defeat=" + Flow.GameFlowManager.Get().EndDream(false, null, "dev");

        /// <summary>Put the player in front of the bed facing it (prompt check).</summary>
        public static string StandAtBed()
        {
            var bed = Object.FindFirstObjectByType<Interaction.BedInteractable>();
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (bed == null || motor == null) return "missing";
            motor.Teleport(bed.wakePose.position, Quaternion.LookRotation(Vector3.ProjectOnPlane(bed.transform.position - bed.wakePose.position, Vector3.up)));
            var cam = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            if (cam != null) { cam.Yaw = motor.transform.eulerAngles.y + 35f; cam.SnapToTarget(); }
            return "at bed";
        }

        public static string NearCamera(string radius)
        {
            var cam = Camera.main; if (cam == null) return "no cam"; float r = float.Parse(radius, System.Globalization.CultureInfo.InvariantCulture);
            string o = "cam " + cam.transform.position;
            foreach (var rd in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (rd.enabled && rd.bounds.SqrDistance(cam.transform.position) < r * r) o += $" | {rd.name}<{(rd.transform.parent != null ? rd.transform.parent.name : "")}> {rd.GetType().Name} {(rd.sharedMaterial != null ? rd.sharedMaterial.name : "")}";
            return o;
        }

        public static string FxStats()
        {
            var arcs = Object.FindObjectsByType<Presentation.SlashArc>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            string r = $"spawned={Presentation.SlashArc.Spawned} objs={arcs.Length}";
            foreach (var a in arcs) { var mr = a.GetComponent<MeshRenderer>(); var mf = a.GetComponent<MeshFilter>(); r += $" | {a.gameObject.activeSelf} mat={(mr.sharedMaterial != null ? mr.sharedMaterial.shader.name : "null")} b={mf.sharedMesh.bounds} pos={a.transform.position}"; }
            return r;
        }

        /// <summary>M15: "Dojo" etc — go into the first building of that room type (instant).</summary>
        public static string EnterRoom(string type)
        {
            var id = Palace.InteriorDirector.Current; if (id == null) return "no interiors";
            var t = (Palace.RoomType)System.Enum.Parse(typeof(Palace.RoomType), type);
            foreach (var d in Object.FindObjectsByType<Palace.BuildingDoor>(FindObjectsSortMode.InstanceID)) if (d.type == t) { id.WalkInto(d); return "walking into " + d.id; }
            return "no door of " + type;
        }
        public static string Talk()
        {
            var npc = Object.FindFirstObjectByType<Palace.NpcTalker>(); var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (npc == null || m == null) return "no npc";
            npc.Interact(new Interaction.InteractionContext { Player = m.gameObject, Motor = m });
            return "talking to " + npc.npcName;
        }
        public static string Shop(string minutes) { var pd = Palace.PalaceDirector.Current; if (pd != null) pd.AddMinutes(int.Parse(minutes)); Palace.ShopPanel.Open(); return "shop"; }
        public static string Compass() { var pd = Palace.PalaceDirector.Current; if (pd == null) return "no palace"; pd.HourCompass = true; return "compass on"; }
        public static string ExitRoom() { var id = Palace.InteriorDirector.Current; if (id == null) return "no interiors"; id.WalkOut(id.Door); return "exit"; }

        /// <summary>M15: stand in front of the n-th door of a type, back to the street, camera looking at the door.</summary>
        public static string AtDoor(string arg)
        {
            var p = arg.Split(','); var t = (Palace.RoomType)System.Enum.Parse(typeof(Palace.RoomType), p[0]); int n = p.Length > 1 ? int.Parse(p[1]) : 0;
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>(); if (motor == null) return "no player";
            foreach (var d in Object.FindObjectsByType<Palace.BuildingDoor>(FindObjectsSortMode.InstanceID))
                if (d.type == t && n-- == 0)
                {
                    var o = d.outside; motor.Teleport(o.position + o.forward * 1.5f, Quaternion.LookRotation(-o.forward));
                    var cam = Object.FindFirstObjectByType<ThirdPersonCameraController>(); if (cam != null) { cam.Yaw = o.eulerAngles.y + 180f + 20f; cam.Pitch = 12f; cam.SnapToTarget(); }
                    return "at " + d.id + " " + d.sign;
                }
            return "none";
        }

        /// <summary>"x,y,z[,yaw]" — move the player (camera snaps).</summary>
        public static string Teleport(string xyz)
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (motor == null) return "no player";
            var v = System.Array.ConvertAll(xyz.Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture));
            motor.Teleport(new Vector3(v[0], v[1], v[2]), Quaternion.Euler(0, v.Length > 3 ? v[3] : 0, 0));
            var stage = Object.FindFirstObjectByType<StageCameraController>();
            if (stage != null) stage.Snap();
            return "teleported " + motor.transform.position;
        }

        public static string ChairExhaust()
        {
            var c = Object.FindFirstObjectByType<Boss.BossChairController>();
            return c == null ? "no chair" : "exhausted=" + c.ApplyStatus(Boss.ChairState.Stumble, 6f, "Exhaustion") + " state=" + c.State;
        }

        /// <summary>Walk-free shortcut for visual checks: stand next to the (open) chair and sit.</summary>
        public static string SitOnChair()
        {
            var c = Object.FindFirstObjectByType<Boss.BossChairController>();
            var e = Object.FindFirstObjectByType<Boss.BossEncounter>();
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (c == null || e == null || m == null) return "missing";
            Vector3 p = c.transform.position + c.transform.forward * 1.2f + Vector3.up * 0.1f;
            m.Teleport(p, Quaternion.LookRotation(-c.transform.forward));
            for (int i = 0; i < 10; i++) m.Step(1f / 60f, default);
            bool ok = c.CanSit(m, out var why);
            return ok ? "sit=" + e.TrySit(m) : "cannot sit: " + why;
        }

        public static string ChairState()
        {
            var c = Object.FindFirstObjectByType<Boss.BossChairController>();
            return c == null ? "no chair" : $"{c.State} t={c.StateTime:F1} fatigue={c.Fatigue.Value:F1}/{c.Fatigue.Threshold} cause={c.OpeningCause} pos={c.transform.position}";
        }

        public static string FlowState() => Flow.GameFlowManager.Exists ? Flow.GameFlowManager.Get().State.ToString() : "no flow";

        static string SetCameraRelativeYaw(float yawOffset, float pitch, float distance)
        {
            var cam = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            if (cam == null || motor == null) return "no camera/player";
            cam.Yaw = motor.transform.eulerAngles.y + yawOffset;
            cam.Pitch = pitch;
            cam.distance = distance;
            cam.SnapToTarget();
            return $"camera yaw={cam.Yaw:F0} pitch={pitch} dist={distance}";
        }
    }
}
