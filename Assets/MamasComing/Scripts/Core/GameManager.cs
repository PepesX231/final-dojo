using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MamasComing
{
    /// <summary>
    /// Game flow: title -> wake up -> fight the flood / fire / smoke (fix or fake) -> Mom comes home and
    /// inspects her usual route -> ending. Also runs the "fire department" fail state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public enum State { Title, Intro, Playing, MomHome, Ending }
        public State state = State.Title;

        [Header("Timing")]
        public float totalTime = 300f;
        public float TimeLeft { get; private set; }
        [Tooltip("Seconds a big fire may rage before the neighbours call the fire department.")]
        public float fireDeptAfter = 40f;

        [Header("References")]
        public PlayerController player;
        public MomController mom;
        public WaterTap tap;
        public MainValve mainValve;
        public Stove stove;
        public CookPot pot;
        public SmokeAlarm alarm;
        public Door frontDoor;
        public MamaUI ui;
        public ThirdPersonCamera cam;
        public Transform livingRoomCarpet;

        [Header("Start state")]
        public Vector3[] floodSeeds;
        public float floodSeedRadius = 1.8f;
        public float floodSeedDepth = 0.004f;
        public float startHeat = 15f;
        public float startSmoke = 1.2f;

        /// World simulation ticks (only on the host in online co-op).
        public bool SimRunning => Live && NetGame.IsAuthority;
        /// The round is in progress (UI, input, minigames).
        public bool Live => state == State.Intro || state == State.Playing || state == State.MomHome;

        int suspicion;
        bool major, potAvalanche, smokeChecked, fireDept, carpetWarned;
        int calls;
        float endDelay = -1f, introT, trackT, endShownT, bigFireT;
        readonly HashSet<object> found = new HashSet<object>();
        readonly List<string> foundLines = new List<string>();
        readonly HashSet<IMess> everProblem = new HashSet<IMess>();
        readonly HashSet<CoverObject> squishRolled = new HashSet<CoverObject>();
        readonly HashSet<ToolType> hinted = new HashSet<ToolType>();
        int smartMoves;

        void Awake() { I = this; TimeLeft = totalTime; }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            if (!ui) ui = GetComponent<MamaUI>();
            Loc.Load();
            ui.Build();
            MamaAudio.Ensure();
            MamaSettings.Ensure();
            if (!GetComponent<MamaHowToPlay>()) gameObject.AddComponent<MamaHowToPlay>();
            if (!GetComponent<MamaReadability>()) gameObject.AddComponent<MamaReadability>();
            if (!GetComponent<MiniGame>()) gameObject.AddComponent<MiniGame>().ui = ui;
            if (!GetComponent<QuestDirector>()) gameObject.AddComponent<QuestDirector>();
            Puzzles.Ensure();
            NetSession.Ensure();
            ui.ShowTitle(true);
            player.controlsEnabled = false;
            player.SetSleeping();
            if (cam) cam.inputEnabled = false;
            LockCursor(false);

            if (FloodSim.I && floodSeeds != null)
                foreach (var s in floodSeeds) FloodSim.I.Seed(s, floodSeedRadius, floodSeedDepth);
            if (stove) stove.heat = startHeat;
            if (SmokeSim.I && stove) SmokeSim.I.Add(stove.burner.position, startSmoke);
        }

        void Update()
        {
            switch (state)
            {
                case State.Title:
                    if (MamaInput.EscDown) MamaSettings.Toggle();
                    if (MamaInput.AnyStart && !MamaSettings.BlockStart && !MamaHowToPlay.BlockStart && NetGame.IsAuthority)
                    {
                        StartRound();
                        NetGame.Broadcast(NetGame.Msg.Start, null, null, 0);
                    }
                    break;

                case State.Intro:
                    introT -= Time.deltaTime;
                    if (introT <= 0f)
                    {
                        state = State.Playing;
                        player.controlsEnabled = true;
                        if (cam) cam.inputEnabled = true;
                        ui.Call(MamaReadability.Text("You threw a secret party... and fell asleep. The house is a DISASTER. Work together - fix it before Mom gets back!", "แอบจัดปาร์ตี้... แล้วเผลอหลับ บ้านพังยับ! ช่วยกันแก้ก่อนแม่กลับ!"), MamaUI.Orange);
                    }
                    break;

                case State.Playing:
                    TimeLeft -= Time.deltaTime;
                    if (MamaInput.DevSkip) TimeLeft = Mathf.Min(TimeLeft, 3f);
                    Calls();
                    if (NetGame.IsAuthority) FireDepartmentCheck();
                    FloodWarnings();
                    if (TimeLeft <= 0f) { TimeLeft = 0f; MomArrives(); }
                    break;

                case State.MomHome:
                    if (!NetGame.IsAuthority) break;
                    FireDepartmentCheck();
                    CheckHeldEvidence();
                    if (endDelay > 0f) { endDelay -= Time.deltaTime; if (endDelay <= 0f) ShowEnding(); }
                    break;

                case State.Ending:
                    endShownT += Time.deltaTime;
                    if (MamaInput.RestartDown && endShownT > 0.5f && NetGame.IsAuthority) { NetGame.Broadcast(NetGame.Msg.Restart, null, null, 0); Restart(); }
                    break;
            }

            if (state == State.Playing || state == State.MomHome)
            {
                if (MamaInput.EscDown) MamaSettings.Toggle();
                else if (!MamaSettings.Open && !MamaHowToPlay.Open && !(MiniGame.I && MiniGame.I.Active) && Cursor.lockState != CursorLockMode.Locked && MamaInput.ClickDown && !MamaSettings.BlockStart) LockCursor(true);
            }

            if (Live) RefreshHUD();
            trackT -= Time.deltaTime;
            if (trackT <= 0f) { trackT = 0.5f; TrackProblems(); }
        }

        // ------------------------------------------------------------------ flow
        void Calls()
        {
            float[] at = { totalTime - 60f, 180f, 90f, 30f };
            string[] msg =
            {
                "PHONE - Mom: \"Hi sweetie~ Grandma's fine! I'm coming home EARLY tonight!\"",
                "PHONE - Mom: \"Traffic's good! Home in 3 minutes. Did you eat the curry I left?\"",
                "*VROOM* ...Mom's car just turned into the street!",
                "*jingle jingle*  KEYS AT THE FRONT DOOR!!"
            };
            while (calls < at.Length && TimeLeft <= at[calls]) { ui.Call(msg[calls], calls >= 2 ? MamaUI.Red : MamaUI.Pink); MamaAudio.Play(calls < 2 ? "ring" : (calls == 2 ? "car" : "click"), null, 0.8f); if (cam) cam.Shake(0.4f); calls++; }
        }

        float FireLoad()
        {
            float s = 0f;
            foreach (var f in FireSource.All) if (f && f.burning) s += f.intensity;
            return s;
        }

        void FireDepartmentCheck()
        {
            float load = FireLoad();
            if (load >= 1.6f) bigFireT += Time.deltaTime; else bigFireT = Mathf.Max(0f, bigFireT - Time.deltaTime * 0.5f);
            if (bigFireT >= fireDeptAfter && !fireDept)
            {
                fireDept = true;
                ShowEnding();
            }
        }

        public float FireDeptIn => FireLoad() >= 1.6f ? Mathf.Max(0f, fireDeptAfter - bigFireT) : -1f;

        void FloodWarnings()
        {
            if (carpetWarned || !FloodSim.I || !livingRoomCarpet) return;
            if (FloodSim.I.DepthAround(livingRoomCarpet.position, 1.2f) > 0.0012f)
            {
                carpetWarned = true;
                ui.Call("The water reached the LIVING ROOM CARPET!", MamaUI.Water);
            }
        }

        void MomArrives()
        {
            state = State.MomHome;
            if (frontDoor) frontDoor.Open();
            MamaAudio.Play("door", frontDoor ? frontDoor.transform.position : (Vector3?)null, 1f);
            if (mom) mom.Begin();
            ui.Call("*CLICK* The door opens... MOM IS HOME! Act natural!", MamaUI.Red);
        }

        public void MomSay(string line) { ui.MomSay(line); NetGame.Broadcast(NetGame.Msg.MomSay, line, null, 0); }

        public void MomInspect(MomController.InspectPoint ip, MomController m)
        {
            Vector3 c = ip.point.position;

            if (!smokeChecked && SmokeSim.I)
            {
                smokeChecked = true;
                if (SmokeSim.I.IsProblem) Found(SmokeSim.I, SmokeSim.I.MomLine, SmokeSim.I.Severity, false);
            }

            foreach (var x in new List<IMess>(MessRegistry.All))
            {
                if (!MessRegistry.Alive(x) || x is SmokeSim || found.Contains(x)) continue;
                if (XZ(x.Position, c) > ip.radius) continue;
                if (MessRegistry.Visible(x)) Found(x, x.MomLine, x.Severity, x.Kind == MessKind.Evidence || x.Severity >= 3);
            }

            if (FloodSim.I)
            {
                float l = FloodSim.I.VisibleLitersIn(c, ip.radius, out float area);
                string key = "flood:" + ip.name;
                if (l > 1.5f && !found.Contains(key))
                {
                    int sev = l > 25f ? 3 : (l > 8f ? 2 : 1);
                    Found(key, sev >= 3 ? "WHY IS THE " + ip.name.ToUpper() + " FLOODED?!" : (sev == 2 ? "Why is the floor soaking wet in here?!" : "Hm? Why is the floor damp...?"), sev, sev >= 3);
                }
            }

            if (ip.checksHidingSpots)
            {
                foreach (var z in HideZone.All)
                {
                    if (!z || z.checkedByMom) continue;
                    if (XZ(z.transform.position, c) > ip.radius + 0.5f) continue;
                    z.checkedByMom = true;
                    if (Random.value < z.momCheckChance) MomOpens(z, m);
                }
            }
        }

        void MomOpens(HideZone z, MomController m)
        {
            bool door = z.cabinet && z.cabinet.hasDoor;
            if (door) z.cabinet.SetOpen(true);
            var inside = new List<IMess>();
            foreach (var x in MessRegistry.All)
            {
                var comp = x as Component;
                if (comp == null || found.Contains(x) || !x.IsProblem) continue;
                if (z.Contains(comp.transform.position)) inside.Add(x);
            }
            if (inside.Count == 0) { MomSay(door ? "*opens the " + z.cabinet.displayName.ToLower() + "* ...Hmm. All good." : "*peeks underneath* ...Just dust bunnies."); return; }
            foreach (var x in inside)
            {
                if (x is CookPot bp && door)
                {
                    potAvalanche = true;
                    Vector3 toMom = m.transform.position + Vector3.up * 1.6f - bp.transform.position;
                    bp.Unstash(toMom.normalized * 3.5f + Vector3.up * 2f);
                    m.Bonk();
                    Found(bp, "*BONK* OW!!! ...Is this my POT?! WHY IS IT BLACK?!", 3, true);
                }
                else Found(x, "And WHAT is the " + x.Label.ToLower() + " doing in here?!", x.Severity, true);
            }
        }

        public void MomStep(Vector3 pos)
        {
            foreach (var c in CoverObject.All)
            {
                if (!c || c.soaked < 1f || !c.OnFloor || squishRolled.Contains(c) || found.Contains(c)) continue;
                if (!c.Covers(pos, 0.2f)) continue;
                squishRolled.Add(c);
                if (Random.value < 0.6f) Found(c, "*SQUISH* ...Why is this " + c.name.ToLower() + " SOAKING WET?!", 2, false);
                else MomSay("*squish?* ...Hm. Must be my imagination.");
                break;
            }
        }

        void CheckHeldEvidence()
        {
            if (!mom || !mom.Active || mom.Done || !player) return;
            if (player.Held is CookPot bp && bp.IsProblem && XZ(mom.transform.position, player.transform.position) < 3.2f)
                Found(bp, "And what is that you're HOLDING?! Is that my POT?!", 3, true);
            if (player.Held is Extinguisher ex && ex.used && XZ(mom.transform.position, player.transform.position) < 3.2f)
                Found(ex, "Why are you holding the FIRE EXTINGUISHER?!", 2, true);
        }

        public void MomFinished() { endDelay = 2.5f; }

        void Found(object key, string line, int severity, bool isMajor)
        {
            if (key == null || found.Contains(key)) return;
            found.Add(key);
            foundLines.Add(line);
            suspicion += severity;
            if(mom && mom.TryGetComponent<MotherApplePresentation>(out var expression)) expression.IsAngry=true;
            if (isMajor) major = true;
            MomSay(line);
        }

        public void ShowEnding()
        {
            if (state == State.Ending) return;
            state = State.Ending;
            endShownT = 0f;
            player.controlsEnabled = false;
            if (cam) cam.inputEnabled = false;
            LockCursor(false);
            TrackProblems();

            int fixedCount = 0, faked = 0;
            foreach (var m in everProblem)
            {
                if (!MessRegistry.Alive(m)) { fixedCount++; continue; }
                if (!m.IsProblem) fixedCount++;
                else if (m.IsHidden) faked++;
            }

            string title, body; Color col;
            if (fireDept)
            {
                title = "FIRE DEPARTMENT!";
                body = "The neighbours saw flames in the kitchen window and called 911.\nThree fire trucks are outside when Mom pulls up. Everyone's okay... except your allowance.";
                col = MamaUI.Red;
            }
            else if (potAvalanche)
            {
                title = "POT-ASTROPHE!";
                body = "Everything looked fine... until Mom opened the cabinet and the burnt pot fell right on her head.";
                col = MamaUI.Red;
            }
            else if (found.Count == 0)
            {
                title = "PERFECT ALIBI";
                body = "Mom didn't suspect a thing.\n\"Aww, were you a good kid today?\" ...Yes. Yes you were.";
                col = MamaUI.Teal;
            }
            else if (!major && suspicion <= 3)
            {
                title = "MOM IS... SUSPICIOUS";
                body = "She keeps squinting at you. And sniffing the air.\nYou survived - for now.";
                col = MamaUI.Orange;
            }
            else
            {
                title = "BUSTED!";
                body = "Mom saw EVERYTHING.\nNo games, no phone, no dessert. For a month.";
                col = MamaUI.Red;
            }

            MamaAudio.Play(col == MamaUI.Teal || col == MamaUI.Orange ? "win" : "lose", null, 0.85f);
            if (MamaAudio.I) MamaAudio.I.musicVolume = 0.12f;
            float water = FloodSim.I ? FloodSim.I.TotalLiters : 0f;
            string stats = "Really fixed: " + fixedCount + "    Faked / hidden: " + faked + "    Slipped: " + player.Falls + "x    Smart moves: " + smartMoves +
                           "\nWater still on the floor: " + Mathf.RoundToInt(water) + " L";
            if (foundLines.Count > 0)
            {
                stats += "\n\nMom noticed:";
                for (int i = 0; i < foundLines.Count && i < 7; i++) stats += "\n- " + foundLines[i];
            }
            ui.ShowEnding(title, body, stats, col);
            NetGame.BroadcastEnding(title, body, stats, col);
        }

        /// Title -> intro. Host runs it locally and tells clients to do the same.
        public void StartRound()
        {
            if (state != State.Title) return;
            state = State.Intro;
            MamaAudio.Play("pop", null, 0.7f);
            ui.ShowTitle(false);
            foreach (var pc in PlayerController.AllPlayers) if (pc) pc.WakeUp();
            introT = 2.0f;
            ui.Toast("*cough cough* ...is something BURNING?!");
            ui.Toast("...and why are my socks WET?!");
            LockCursor(true);
        }

        /// Client side: show the ending the host computed.
        public void ShowRemoteEnding(string title, string body, string stats, Color col)
        {
            state = State.Ending; endShownT = 0f;
            player.controlsEnabled = false; if (cam) cam.inputEnabled = false; LockCursor(false);
            ui.ShowEnding(title, body, stats, col);
            MamaAudio.Play(col == MamaUI.Teal || col == MamaUI.Orange ? "win" : "lose", null, 0.85f);
        }

        public void Restart()
        {
            var s = SceneManager.GetActiveScene();
            if (s.buildIndex >= 0) SceneManager.LoadScene(s.buildIndex); else SceneManager.LoadScene(s.name);
        }

        // ------------------------------------------------------------------ HUD
        void RefreshHUD()
        {
            var todo = new List<string>();
            int total = 0;
            bool fireBurning = false;
            foreach (var f in FireSource.All) if (f && f.burning) fireBurning = true;

            total++; if (fireBurning) todo.Add("PUT OUT THE FIRE!");
            total++; if (stove && stove.on) todo.Add("Turn off the stove");
            total++; if (tap && tap.Overflowing) todo.Add("Stop the bathtub overflowing");
            float wet = FloodSim.I ? FloodSim.I.WetArea : 0f;
            total++; if (wet > 0.8f) todo.Add("Clean up the water  (" + wet.ToString("0") + " m²)");
            total++; if (alarm && alarm.Beeping) todo.Add("Silence the smoke alarm");
            total++; if (SmokeSim.I && SmokeSim.I.IsProblem) todo.Add("Get rid of the smoke");
            total++; if (pot && MessRegistry.Visible(pot)) todo.Add("Deal with the burnt pot");
            int powder = 0, clutter = 0;
            foreach (var m in MessRegistry.All)
            {
                if (!MessRegistry.Visible(m)) continue;
                if (m is PowderMess) powder++;
                else if (m is MessyProp && !(m is Bucket)) clutter++;
            }
            total++; if (powder > 0) todo.Add("Clean the white powder");
            int openTasks = todo.Count;
            total++;
            if (clutter > 0)
            {
                openTasks++;
                int shown = 0;
                foreach (var m in MessRegistry.All)
                {
                    if (!MessRegistry.Visible(m) || !(m is MessyProp mp) || m is Bucket) continue;
                    if (shown++ >= 4) break;
                    int home = RoomOf(mp.homePosition);
                    todo.Add("Put the " + mp.displayName.ToLower() + " back  (" + (home >= 0 ? RoomNames[home] : "its spot") + ")");
                }
                if (clutter > 4) todo.Add("...and " + (clutter - 4) + " more things");
            }

            string main = todo.Count > 0 ? todo[0] : (state == State.MomHome ? "Act natural..." : "Looks clean! Check Mom's route");
            ui.SetObjectives(main, (total - openTasks) + "/" + total, todo.Count > 1 ? todo.GetRange(1, todo.Count - 1) : new List<string>());
            ui.SetTimer(TimeLeft, state == State.MomHome);

            // Disaster report
            float fl = FireLoad();
            string fs;
            float spread = -1f;
            foreach (var f in FireSource.All) if (f && f.burning) { float s = f.SpreadIn; if (s >= 0f && (spread < 0f || s < spread)) spread = s; }
            float dept = FireDeptIn;
            if (dept >= 0f) fs = "FIRE DEPT in " + Mathf.CeilToInt(dept) + "s!";
            else if (spread >= 0f) fs = "spreads in " + Mathf.CeilToInt(spread) + "s";
            else if (fireBurning) fs = "BURNING";
            else if (stove && stove.IgnitesIn >= 0f) fs = "ignites in " + Mathf.CeilToInt(stove.IgnitesIn) + "s";
            else fs = "out";
            ui.SetRow(ui.fireRow, fireBurning ? Mathf.Clamp01(fl) : (stove && stove.IgnitesIn >= 0f ? 1f - stove.IgnitesIn / stove.igniteAfter : 0f) * 0.3f, fs, fireBurning || (stove && stove.IgnitesIn >= 0f && stove.IgnitesIn < 15f));

            float liters = FloodSim.I ? FloodSim.I.TotalLiters : 0f;
            string ws = Mathf.RoundToInt(liters) + " L  " + wet.ToString("0") + " m²" + (tap && tap.Overflowing ? "  RISING" : "");
            ui.SetRow(ui.floodRow, liters / 250f, ws, tap && tap.Overflowing);

            float sm = SmokeSim.I ? SmokeSim.I.Total : 0f;
            ui.SetRow(ui.smokeRow, sm / 60f, alarm && alarm.Beeping ? "ALARM BEEPING!" : (alarm && alarm.BatteryOut ? "alarm disabled" : (sm > 8f ? "thick" : sm > 2f ? "hazy" : "clear")), alarm && alarm.Beeping);
            ui.SetAlarm(alarm && alarm.Beeping);

            string heldName = player.Held ? player.Held.displayName : null;
            float meter = -1f;
            if (player.Held is Mop mop) { meter = mop.load / mop.capacity; heldName += "  " + mop.load.ToString("0.0") + "/" + mop.capacity + " L"; }
            else if (player.Held is Bucket b) { meter = b.liters / b.capacity; heldName += "  " + Mathf.RoundToInt(b.liters) + " L"; }
            else if (player.Held is Extinguisher ex) { meter = ex.charge; }
            else if (player.Held is Towel tw) { meter = tw.Soaked / 4f; }
            ui.SetHeld(heldName, meter);

            string pr = null;
            if (player.controlsEnabled && !player.Stunned)
            {
                if (player.Target != null) pr = (player.Target.HoldTime(player) > 0f ? "" : "[E]  ") + player.Target.GetPrompt(player);
                else if (player.Held is MessyProp hp && hp.NearHome(player)) pr = "[Q]  Put the " + hp.displayName.ToLower() + " back (green circle)";
                else if (player.Held is Mop) pr = "[Hold LMB]  Mop   (walk slowly!)     [Q]  Place";
                else if (player.Held is Squeegee) pr = "[Hold LMB]  Push water ahead of you     [Q]  Place";
                else if (player.Held is Extinguisher) pr = "[Hold LMB]  SPRAY at the base of the fire     [Q]  Place";
                else if (player.Held is CookPot) pr = "[Hold LMB at kitchen sink]  Scrub     [Q]  Place     [RMB]  Throw";
                else if (player.Held is Bucket bb) pr = bb.liters > 0.5f ? "[RMB]  Throw the water     [Q]  Place" : "Fill it at a tap - or scoop out the tub";
                else if (player.Held is Towel) pr = "[Q]  Place it on a puddle to soak it up     [RMB]  Throw";
                else if (player.Held != null) pr = player.PlaceValid ? "[Q]  Place it (circle)      [RMB]  Throw" : "[Q]  Put down      [RMB]  Throw";
                if (player.OnWet && pr == null && player.Sprinting) pr = "!! RUNNING ON A WET FLOOR !!";
            }
            ui.SetPrompt(pr);
            ui.SetHold(player.HoldProgress);

            float score = 0f;
            foreach (var m in MessRegistry.All) if (MessRegistry.Visible(m)) score += m.Severity;
            score += Mathf.Clamp(wet / 4f, 0f, 6f);
            ui.SetMess(Mathf.Clamp01(score / 30f));
            // MamaReadability owns contextual hints instead of competing always-on markers.
            ui.TickBubble(player.transform);

        }

        // ------------------------------------------------------------------ room warnings + kid thoughts
        public static readonly string[] RoomNames = { "Living room", "Laundry", "Dining room", "Hallway", "Bedroom", "Bathroom", "Kitchen", "Garage", "Game room" };
        public static readonly Rect[] RoomRects = { new Rect(0, 0, 13, 6), new Rect(13, 0, 4, 6), new Rect(17, 0, 5, 6), new Rect(0, 6, 30, 2), new Rect(0, 8, 7, 6), new Rect(7, 8, 5, 6), new Rect(12, 8, 10, 6), new Rect(22, 0, 8, 6), new Rect(22, 8, 8, 6) };
        string lastThought; float thoughtT;

        public static int RoomOf(Vector3 p)
        {
            for (int i = 0; i < RoomRects.Length; i++) if (RoomRects[i].Contains(new Vector2(p.x, p.z))) return i;
            return -1;
        }

        public static Vector3 RoomCenter(int i) { var r = RoomRects[i]; return new Vector3(r.center.x, 2.3f, r.center.y); }

        public void KidSay(string s) { if (NetGame.ActingRemote(s)) return; if (ui) ui.KidSay(s); }

        /// Neighbour complaint etc: Mom arrives earlier.
        public void CutTime(float seconds) { TimeLeft = Mathf.Max(20f, TimeLeft - seconds); }
        public void SetTimeLeft(float t) { TimeLeft = t; }

        void UpdateRoomWarnings()
        {
            if (state != State.Playing && state != State.MomHome) { for (int g = 0; g < 6; g++) ui.SetGuide(g, false, Vector3.zero, null, null, Color.white); return; }
            CacheGuideRefs();
            Vector3 me = player.transform.position;
            wetScanT -= Time.deltaTime;
            if (wetScanT <= 0f && FloodSim.I) { wetScanT = 0.5f; wetFound = FloodSim.I.NearestWet(me, 0.0012f, true, out wetPos); }

            FireSource big = null; foreach (var f in FireSource.All) if (f && f.burning && (big == null || f.intensity > big.intensity)) big = f;
            int myRoom = RoomOf(me);
            var warn = new List<Warn>();
            System.Action<int, Color, string, string> W = (r, c, t, k) => { if (r >= 0) warn.Add(new Warn { room = r, col = c, thought = t, key = k }); };

            if (FloodSim.I && (roomWater == null || (roomScanT -= Time.deltaTime) <= 0f))
            {
                roomScanT = 0.5f; if (roomWater == null) roomWater = new float[RoomRects.Length];
                for (int r = 0; r < RoomRects.Length; r++) roomWater[r] = FloodSim.I.VisibleLitersInRect(RoomRects[r]);
            }

            string thought = null, key = null;
            if (state == State.MomHome) { thought = "Act natural... act natural..."; key = "mom"; }
            else
            {
                if (big) W(RoomOf(big.transform.position), MamaUI.Red, "FIRE!! Put it out!", "fire");
                if (stove && stove.on) W(RoomOf(stove.transform.position), MamaUI.Orange, stove.IgnitesIn >= 0f && stove.IgnitesIn < 15f ? "The stove!! Hurry!" : "Something's burning... the stove!", "stove");
                if (tap && tap.Flowing) W(RoomOf(tap.transform.position), MamaUI.Water, tap.Overflowing ? "The bathtub! Stop the water!" : "Is the bath still running?", "tap");
                if (alarm && alarm.Beeping) W(RoomOf(alarm.transform.position), MamaUI.Red, "So LOUD! Make it stop!", "alarm");
                if (SmokeSim.I && SmokeSim.I.IsProblem) W(RoomOf(stove ? stove.transform.position : me), MamaUI.SmokeCol, "*cough* I need fresh air in here!", "smoke");
                if (gDrain && gDrain.clogged && FloodSim.I && FloodSim.I.WetArea > 3f) W(RoomOf(gDrain.transform.position), MamaUI.Water, "Why isn't the water draining?", "drain");
                if (roomWater != null)
                {
                    int wr = -1; float wl = 4f;
                    for (int r = 0; r < roomWater.Length; r++) if (roomWater[r] > wl) { wl = roomWater[r]; wr = r; }
                    if (wr >= 0) W(wr, MamaUI.Water, "So much water in the " + RoomNames[wr].ToLower() + "!", "water" + wr);
                }
                if (pot && MessRegistry.Visible(pot)) W(RoomOf(pot.transform.position), MamaUI.Ink, "Mom's pot... she can't see this!", "pot");
                PowderMess pw = null; MessyProp near = null; float nd = float.MaxValue;
                foreach (var m in MessRegistry.All)
                {
                    if (!MessRegistry.Visible(m)) continue;
                    if (m is PowderMess pm) pw = pm;
                    else if (m is MessyProp mp2 && !(m is Bucket) && !mp2.IsHeld) { float d = (mp2.transform.position - me).sqrMagnitude; if (d < nd) { nd = d; near = mp2; } }
                }
                if (pw) W(RoomOf(pw.transform.position), MamaUI.Teal, "This white powder is everywhere...", "powder");
                if (near) W(RoomOf(near.transform.position), MamaUI.Teal, "The " + near.displayName.ToLower() + " goes back in the " + RoomNames[Mathf.Max(0, RoomOf(near.homePosition))].ToLower() + "!", "tidy" + near.GetInstanceID());
                if (warn.Count > 0) { thought = warn[0].thought; key = warn[0].key; }
                else { thought = "Looks okay... I think?"; key = "done"; }
            }

            // up to two warning icons, one per room you need to go to (hidden for the room you're in)
            int slot = 0; var used = new HashSet<int>();
            foreach (var w in warn)
            {
                if (slot >= 2) break;
                if (w.room == myRoom || used.Contains(w.room)) { used.Add(w.room); continue; }
                used.Add(w.room);
                ui.SetGuide(slot, true, RoomCenter(w.room), "!", RoomNames[w.room], w.col);
                slot++;
            }
            for (; slot < 2; slot++) ui.SetGuide(slot, false, Vector3.zero, null, null, Color.white);

            // tidy helpers: things out of place in THIS room, and where the held thing belongs
            int ts = 2;
            if (state == State.Playing)
                foreach (var m in MessRegistry.All)
                {
                    if (ts > 4) break;
                    if (!(m is MessyProp mp3) || m is Bucket || mp3.IsHeld || !MessRegistry.Visible(m)) continue;
                    if (RoomOf(mp3.transform.position) != myRoom && (mp3.transform.position - me).sqrMagnitude > 16f) continue;
                    ui.SetGuide(ts++, true, mp3.transform.position + Vector3.up * 0.55f, "!", mp3.displayName, MamaUI.Teal);
                }
            for (; ts <= 4; ts++) ui.SetGuide(ts, false, Vector3.zero, null, null, Color.white);
            var held = player.Held as MessyProp;
            bool showHome = held && held.homeSet && !(held is Bucket) && state == State.Playing;
            ui.SetGuide(5, showHome, showHome ? held.homePosition + Vector3.up * 0.5f : Vector3.zero, "★", showHome ? "Put it back here" : "", MamaUI.Teal);

            thoughtT -= Time.deltaTime;
            if (thought != null && (key != lastThought || thoughtT <= 0f))
            {
                if (!(MiniGame.I && MiniGame.I.Active)) ui.KidSay(thought, 2.6f);
                lastThought = key; thoughtT = 15f;
            }
        }
        struct Warn { public int room; public Color col; public string thought, key; }
        float[] roomWater; float roomScanT;

        // ------------------------------------------------------------------ guide markers
        Lid gLid; Extinguisher gExt; Mop gMop; Squeegee gSq; Drain gDrain; Fan gFan; Window[] gWins; WaterDump[] gDumps; Bucket gBucket; Cabinet[] gCabs;
        float wetScanT; Vector3 wetPos; bool wetFound;

        struct GuideT { public bool on; public Vector3 pos; public string glyph, label; public Color col; }
        static GuideT GT(Vector3 p, string g, string l, Color c) => new GuideT { on = true, pos = p, glyph = g, label = l, col = c };
        static Vector3 Top(Component c, float h = 1.1f) => c.transform.position + Vector3.up * h;
        static Vector3 At(Interactable i, float h = 1.1f) { Vector3 p = i.InteractPoint; p.y = Mathf.Max(p.y, 0f) + h; return p; }

        void CacheGuideRefs()
        {
            if (gMop) return;
            gLid = FindFirstObjectByType<Lid>(); gExt = FindFirstObjectByType<Extinguisher>(); gMop = FindFirstObjectByType<Mop>();
            gSq = FindFirstObjectByType<Squeegee>(); gDrain = FindFirstObjectByType<Drain>(); gFan = FindFirstObjectByType<Fan>();
            gWins = FindObjectsByType<Window>(FindObjectsSortMode.None); gDumps = FindObjectsByType<WaterDump>(FindObjectsSortMode.None);
            gBucket = FindFirstObjectByType<Bucket>(); gCabs = FindObjectsByType<Cabinet>(FindObjectsSortMode.None);
        }

        T Nearest<T>(T[] arr, Vector3 from, System.Func<T, bool> ok) where T : Interactable
        {
            T best = null; float bd = float.MaxValue;
            if (arr == null) return null;
            foreach (var a in arr) { if (!a || (ok != null && !ok(a))) continue; float d = (a.InteractPoint - from).sqrMagnitude; if (d < bd) { bd = d; best = a; } }
            return best;
        }

        void UpdateGuides()
        {
            if (state != State.Playing && state != State.MomHome) { for (int g = 0; g < 6; g++) ui.SetGuide(g, false, Vector3.zero, null, null, Color.white); return; }
            CacheGuideRefs();
            Vector3 me = player.transform.position;
            wetScanT -= Time.deltaTime;
            if (wetScanT <= 0f && FloodSim.I) { wetScanT = 0.4f; wetFound = FloodSim.I.NearestWet(me, 0.0015f, true, out wetPos); }

            FireSource big = null; foreach (var f in FireSource.All) if (f && f.burning && (big == null || f.intensity > big.intensity)) big = f;
            bool potFire = pot && pot.Fire && pot.Fire.burning;
            var held = player.Held;
            GuideT a = default, b = default;
            Color red = MamaUI.Red, blue = MamaUI.Water, teal = MamaUI.Teal, ink = MamaUI.Ink, orange = MamaUI.Orange;
            var tapDump = gDumps != null ? System.Array.Find(gDumps, d => d && d.tub) : null;

            if (held != null)
            {
                if (held is Lid)
                {
                    if (potFire && pot.Fire.intensity <= 0.85f) a = GT(Top(pot, 0.9f), "E", "Slam the lid on!", red);
                    else if (held is MessyProp lp) a = GT(lp.homePosition + Vector3.up * 0.5f, "Q", "Put it back", teal);
                }
                else if (held is Extinguisher)
                {
                    if (big) a = GT(Top(big, 0.6f), "!", "Hold LMB - spray!", red);
                }
                else if (held is Towel tw)
                {
                    if (big && tw.IsWet) a = GT(Top(big, 0.6f), "E", "Smother it!", red);
                    else if (big) { var d = Nearest(gDumps, me, x => x.hasTap && x.HasWater); if (d) a = GT(At(d), "E", "Soak the towel", blue); }
                    else if (wetFound) a = GT(wetPos + Vector3.up * 0.4f, "Q", "Drop it on the water", blue);
                }
                else if (held is Mop mop)
                {
                    if (alarm && alarm.Beeping) a = GT(Top(alarm, -0.6f), "E", "Hold E - hush it", red);
                    else if (mop.load >= mop.capacity * 0.9f)
                    {
                        Interactable w = Nearest(gDumps, me, null);
                        if (gBucket && !gBucket.IsHeld && (gBucket.InteractPoint - me).sqrMagnitude < (w ? (w.InteractPoint - me).sqrMagnitude : 1e9f)) w = gBucket;
                        if (w) a = GT(At(w), "E", "Wring the mop", blue);
                    }
                    else if (wetFound) a = GT(wetPos + Vector3.up * 0.4f, "≈", "Hold LMB - mop!", blue);
                }
                else if (held is Squeegee)
                {
                    if (alarm && alarm.Beeping) a = GT(Top(alarm, -0.6f), "E", "Hold E - hush it", red);
                    else if (gDrain && gDrain.clogged) a = GT(At(gDrain, 0.6f), "E", "Unclog the drain", orange);
                    else { if (gDrain) a = GT(At(gDrain, 0.6f), "↓", "Push water here", blue); if (wetFound) b = GT(wetPos + Vector3.up * 0.4f, "≈", "", blue); }
                }
                else if (held is Bucket bk)
                {
                    if (bk.liters > 0.5f) { var d = Nearest(gDumps, me, x => !x.tub); if (d) a = GT(At(d), "E", "Empty it here", blue); }
                    else if (tap && tap.Overflowing && tapDump) a = GT(At(tapDump), "E", "Hold E - scoop the tub", blue);
                }
                else if (held is CookPot cp)
                {
                    var sink = KitchenSink.Nearest(me, 999f);
                    if (cp.burn > 0.2f && sink && sink.HasWater) a = GT(At(sink), "≈", "Hold LMB - scrub", blue);
                    var hide = Nearest(gCabs, me, c => c.hasDoor);
                    if (cp.burn > 0.2f && hide) { if (!a.on) a = GT(At(hide), "E", "Hide it?", ink); else b = GT(At(hide), "E", "or hide it", ink); }
                }
                else if (held is Rug)
                {
                    if (wetFound) a = GT(wetPos + Vector3.up * 0.4f, "≈", "Cover the water", blue);
                }
                else if (held is MessyProp mp && mp.IsDisplaced)
                {
                    a = GT(mp.homePosition + Vector3.up * 0.5f, "Q", "Put it back", teal);
                }
            }
            else
            {
                if (big)
                {
                    bool lidFree = gLid && !gLid.IsHeld && pot && !pot.lid;
                    if (potFire && lidFree && pot.Fire.intensity <= 0.85f) { a = GT(Top(gLid, 0.6f), "E", "Grab the lid!", red); b = GT(Top(pot, 0.9f), "!", "", red); }
                    else if (gExt && !gExt.IsHeld) { a = GT(Top(gExt, 0.9f), "E", "Fire extinguisher!", red); b = GT(Top(big, 0.6f), "!", "", red); }
                    else a = GT(Top(big, 0.6f), "!", "FIRE!", red);
                }
                else if (stove && stove.on) a = GT(At(stove), "E", "Hold E - stove off", orange);
                else if (tap && tap.Overflowing)
                {
                    if (!tap.handleBroken) a = GT(At(tap), "E", "Turn off the tap", blue);
                    else if (mainValve && mainValve.open) { a = GT(At(mainValve), "E", "Hold E - main valve", blue); b = GT(At(tap), "E", "or pull the plug", blue); }
                }
                else if (alarm && alarm.Beeping && gMop) { a = GT(Top(gMop, 1.2f), "E", "Grab the mop", red); b = GT(Top(alarm, -0.6f), "!", "", red); }
                else if (SmokeSim.I && SmokeSim.I.IsProblem && gWins != null && System.Array.Exists(gWins, w => w && !w.open))
                {
                    var w = Nearest(gWins, me, x => !x.open);
                    a = GT(At(w, 1.4f), "E", "Open the window", ink);
                    if (gFan && !gFan.on) b = GT(At(gFan), "E", "fan", ink);
                }
                else if (gDrain && gDrain.clogged && FloodSim.I && FloodSim.I.WetArea > 2f) a = GT(At(gDrain, 0.6f), "E", "Hold E - unclog drain", orange);
                else if (FloodSim.I && FloodSim.I.WetArea > 2f)
                {
                    Pickup tool = gSq && !gSq.IsHeld ? (Pickup)gSq : gMop;
                    if (tool) a = GT(Top(tool, 1.3f), "E", tool is Squeegee ? "Grab the squeegee" : "Grab the mop", blue);
                    if (wetFound) b = GT(wetPos + Vector3.up * 0.4f, "≈", "", blue);
                }
                else if (pot && MessRegistry.Visible(pot)) a = GT(Top(pot, 0.7f), "E", "Burnt pot", ink);
                else
                {
                    MessyProp near = null; float nd = float.MaxValue;
                    foreach (var m in MessRegistry.All)
                        if (m is MessyProp mp2 && MessRegistry.Visible(mp2) && !(m is Bucket)) { float d = (mp2.transform.position - me).sqrMagnitude; if (d < nd) { nd = d; near = mp2; } }
                    if (near) a = GT(Top(near, 0.6f), "E", "Tidy up", teal);
                }
            }
            Vector3 mf = me; mf.y = 0f;
            if (a.on) { Vector3 ap = a.pos; ap.y = 0f; if ((ap - mf).magnitude < 1.3f && (a.glyph == "\u2248" || a.glyph == "Q")) a.on = false; }
            if (b.on) { Vector3 bp2 = b.pos; bp2.y = 0f; if ((bp2 - mf).magnitude < 1.3f) b.on = false; }
            ui.SetGuide(0, a.on, a.pos, a.glyph, a.label, a.col);
            ui.SetGuide(1, b.on, b.pos, b.glyph, b.label, b.col);
        }

        void TrackProblems()
        {
            foreach (var m in MessRegistry.All) if (MessRegistry.Alive(m) && m.IsProblem) everProblem.Add(m);
        }

        // ------------------------------------------------------------------ hooks
        public void Toast(string s) { if (ui) ui.Toast(s); NetGame.Broadcast(NetGame.Msg.Toast, s, null, 0); }

        public void OnPlayerSlipped(string reason)
        {
            if (cam) cam.Shake(0.5f);
            if (!string.IsNullOrEmpty(reason)) { Toast(reason); return; }
            string[] lines = { "WHOA! *SLIP* *SPLASH* *THUD*", "Ouch! Running on a wet floor... genius.", "*SPLAT* ...that's gonna bruise.", "Slow down on the water!" };
            Toast(lines[Random.Range(0, lines.Length)]);
        }

        public void OnFireStarted(FireSource f)
        {
            if (!SimRunning) return;
            ui.Call("WHOOSH! The " + f.label.ToLower() + " is ON FIRE!", MamaUI.Red);
            if (cam) cam.Shake(0.6f);
        }

        public void OnFlareUp(FireSource f)
        {
            ui.Call("FWOOOSH!!! Water on a GREASE FIRE = FIREBALL!", MamaUI.Red);
            if (cam) cam.Shake(1.2f);
        }

        public void OnFireOut(FireSource f)
        {
            bool anotherFire = false;
            foreach (var source in FireSource.All) if (source && source.burning) { anotherFire = true; break; }
            ui.Call(anotherFire
                ? MamaReadability.Text("One fire out. There are still flames elsewhere!", "ไฟจุดนี้ดับแล้ว แต่ยังมีไฟที่อื่น!")
                : MamaReadability.Text("Fire out. The smoke and evidence still remain.", "ดับไฟแล้ว ควันและร่องรอยยังต้องจัดการ"), anotherFire ? MamaUI.Orange : MamaUI.Teal);
        }

        public void OnAlarm()
        {
            ui.Call("BEEP! BEEP! BEEP! The SMOKE ALARM is going off!", MamaUI.Red);
        }

        public void OnSmart(string msg) { smartMoves++; Toast(msg); }

        public void OnPickedUp(Pickup p)
        {
            if (hinted.Contains(p.tool)) return;
            hinted.Add(p.tool);
            switch (p.tool)
            {
                case ToolType.Mop: ui.KidSay("Mop time!"); break;
                case ToolType.Squeegee: ui.KidSay("Swoosh swoosh..."); break;
                case ToolType.Rug: ui.KidSay("Nobody will notice... right?"); break;
                case ToolType.Pot: ui.KidSay("Ugh, it's so black..."); break;
                case ToolType.Bucket: ui.KidSay("Heavy...!"); break;
                case ToolType.Lid: ui.KidSay("A lid!"); break;
                case ToolType.Towel: ui.KidSay("A towel..."); break;
                case ToolType.Extinguisher: ui.KidSay("Okay... aim and squeeze!"); break;
            }
        }

        void LockCursor(bool l)
        {
            Cursor.lockState = l ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !l;
        }

        static float XZ(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }
    }
}
