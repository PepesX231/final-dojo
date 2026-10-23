using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MamasComing
{
    /// Online co-op (2-3 players) on top of Netcode for GameObjects using named messages only:
    ///  - the host owns the whole world simulation (water, smoke, fire, problems, Mom);
    ///  - every player moves their own kid locally and streams its pose;
    ///  - clients ask the host to perform interactions for them (the host keeps a "puppet" kid per client);
    ///  - the host streams the world state back (items, devices, problems, water/smoke fields, Mom, timer).
    public class NetSession : MonoBehaviour
    {
        public static NetSession I { get; private set; }
        public enum Mode { Offline, Connecting, Host, Client }
        public Mode State { get; private set; } = Mode.Offline;
        public string Status { get; private set; } = "";
        public string JoinCode { get; private set; } = "";
        public const int MaxPlayers = 3;
        public const ushort Port = 7777;

        NetworkManager nm;
        object session;     // Unity.Services.Multiplayer.ISession (kept untyped so the game still runs without services)

        // slots: 0 = host. clientId -> slot
        readonly Dictionary<ulong, int> slotOf = new Dictionary<ulong, int>();
        readonly Dictionary<int, PlayerController> avatars = new Dictionary<int, PlayerController>();
        readonly Dictionary<int, RemoteState> remote = new Dictionary<int, RemoteState>();
        int mySlot;
        PlayerController localKid;

        class RemoteState { public Vector3 pos; public float yaw, speed; public bool use, sprint, stunned; public int holdTarget = -1; public float t; }

        // registry (built when the scene loads, identical on every machine)
        List<Interactable> reg = new List<Interactable>();
        List<Pickup> pickups = new List<Pickup>();
        List<Component> synced = new List<Component>();
        static readonly Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();
        static readonly Type[] SyncTypes = { typeof(WaterTap), typeof(MainValve), typeof(Stove), typeof(Window), typeof(Fan), typeof(Drain), typeof(SmokeAlarm),
            typeof(Cabinet), typeof(CookPot), typeof(Bucket), typeof(Towel), typeof(Extinguisher), typeof(FireSource), typeof(CoverObject), typeof(Fish), typeof(MessyProp), typeof(HeaterValve), typeof(Door), typeof(MotherApplePresentation) };

        float sendT, worldT, fieldT;
        readonly List<(string n, Vector3 p, float v, float pi)> sounds = new List<(string, Vector3, float, float)>();

        public static void Ensure() { if (!I) { var go = new GameObject("NetSession"); DontDestroyOnLoad(go); go.AddComponent<NetSession>(); } }

        void Awake()
        {
            I = this;
            SceneManager.sceneLoaded += (s, m) => { BindScene(); };
            NetGame.SendInteract = (t, p) => SendAct(0, t, null);
            NetGame.SendHit = SendHitMsg;
            NetGame.SendBroadcast = (m, en, th, lvl) => SendEvent((byte)m, en, th, lvl, null);
            NetGame.GetName = p => { foreach (var kv in avatars) if (kv.Value == p) return "P" + (kv.Key + 1); return null; };
            NetGame.SoundHook = (n, p, v, pi) => { if (State == Mode.Host && p.HasValue) sounds.Add((n, p.Value, v, pi)); };
            BindScene();
            // command line: -mamajoin <ip|code>   -mamahost   (handy for testing two windows)
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-mamajoin" && i + 1 < args.Length) { string a = args[i + 1]; Invoke(nameof(AutoJoinLater), 1.5f); autoJoin = a; }
                if (args[i] == "-mamahost") Invoke(nameof(HostLanLater), 1f);
                if (args[i] == "-mamabot") StartCoroutine(Bot());
            }
        }
        string autoJoin;

        /// Test bot for automated co-op checks (client): close the fridge, carry a party cup, report what it sees.
        System.Collections.IEnumerator Bot()
        {
            while (GameManager.I == null || GameManager.I.state != GameManager.State.Playing) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1f);
            var kid = GameManager.I.player;
            var fr = FindFirstObjectByType<FridgeHazard>();
            kid.Motor.Teleport(fr.InteractPoint + Vector3.back * 0.3f, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            NetGame.Interact(fr, kid);
            Debug.Log("[Bot] asked host to close fridge");
            yield return new WaitForSeconds(1.5f);
            Debug.Log("[Bot] fridge fixed on my side = " + fr.Fixed + " water=" + (FloodSim.I ? FloodSim.I.TotalLiters : -1) + " power=" + PowerGrid.On);
            MessyProp cup = null; foreach (var m in FindObjectsByType<MessyProp>(FindObjectsSortMode.None)) if (m.displayName == "Party cup") { cup = m; break; }
            kid.Motor.Teleport(cup.transform.position + Vector3.back * 0.6f, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            NetGame.Interact(cup, kid);
            yield return new WaitForSeconds(1.5f);
            Debug.Log("[Bot] holding cup = " + (kid.Held == cup));
            kid.Motor.Teleport(new Vector3(27.3f, 0.05f, 1.5f), Quaternion.Euler(0, 180, 0));
            yield return new WaitForSeconds(1.5f);
            kid.Drop(false);
            yield return new WaitForSeconds(1.5f);
            Debug.Log("[Bot] dropped cup at " + cup.transform.position + " settled=" + cup.Settled);
            while (true) { yield return new WaitForSeconds(5f); Debug.Log("[Bot] t=" + GameManager.I.TimeLeft.ToString("0") + " state=" + GameManager.I.state + " hazards active=" + Hazard.All.FindAll(h => h.Urgent).Count + " water=" + FloodSim.I.TotalLiters.ToString("0")); }
        }
        void AutoJoinLater() => Join(autoJoin);
        void HostLanLater() => HostLan();

        // ================================================================== scene binding
        void BindScene()
        {
            var gm = GameManager.I ? GameManager.I : FindFirstObjectByType<GameManager>();
            localKid = gm ? gm.player : null;
            reg = new List<Interactable>(FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            reg.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform) + "|" + a.GetType().Name, PathOf(b.transform) + "|" + b.GetType().Name));
            pickups.Clear(); foreach (var i in reg) if (i is Pickup pk) pickups.Add(pk);
            synced.Clear();
            var comps = new List<Component>();
            foreach (var t in SyncTypes) foreach (var c in FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!comps.Contains((Component)c)) comps.Add((Component)c);
            comps.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform) + "|" + a.GetType().Name, PathOf(b.transform) + "|" + b.GetType().Name));
            synced = comps;
            avatars.Clear();
            if (localKid) { localKid.isLocal = true; avatars[mySlot] = localKid; }
            if (State == Mode.Host || State == Mode.Client) foreach (var kv in slotOf) if (kv.Value != mySlot) SpawnPuppet(kv.Value);
            if (State == Mode.Client) ApplyClientMode();
            if (localKid && State != Mode.Offline) PlaceAtSpawn(localKid, mySlot);
        }

        static string PathOf(Transform t) { var s = t.name + "#" + t.GetSiblingIndex(); while (t.parent) { t = t.parent; s = t.name + "#" + t.GetSiblingIndex() + "/" + s; } return s; }

        void ApplyClientMode()
        {
            var mom = FindFirstObjectByType<MomController>(); if (mom) mom.enabled = false;
            foreach (var p in pickups) if (p && p.Body) { p.Body.isKinematic = true; p.Body.interpolation = RigidbodyInterpolation.None; }
            foreach (var s in FindObjectsByType<SplatMess>(FindObjectsSortMode.None)) Destroy(s.gameObject);
        }

        static readonly Vector3[] Spawns = { new Vector3(4.4f, 0.02f, 3f), new Vector3(6.8f, 0.02f, 4.6f), new Vector3(3.0f, 0.02f, 4.8f) };
        static readonly Color[] Tints = { new Color(1f, 1f, 1f), new Color(0.55f, 1f, 0.55f), new Color(1f, 0.95f, 0.45f) };
        void PlaceAtSpawn(PlayerController k, int slot) { if (k && k.Motor && k.isLocal) k.Motor.Teleport(Spawns[slot % Spawns.Length], Quaternion.Euler(0, -90, 0)); else if (k) k.transform.position = Spawns[slot % Spawns.Length]; }

        PlayerController SpawnPuppet(int slot)
        {
            if (avatars.TryGetValue(slot, out var ex) && ex) return ex;
            if (!localKid) return null;
            // Awake on the clone must cache open-eye materials, even if the live source
            // is currently blinking or squeezing its eyes shut.
            localKid.GetComponentInChildren<DreamRush.Player.AppleFace>(true)?.RestoreOpenEyes();
            var go = Instantiate(localKid.gameObject, Spawns[slot % Spawns.Length], Quaternion.identity);
            go.name = "Kid P" + (slot + 1) + " (remote)";
            var pc = go.GetComponent<PlayerController>();
            pc.isLocal = false; pc.controlsEnabled = false;
            if (pc.Motor) pc.Motor.enabled = false;
            var cc = go.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            // DreamToon exposes _BaseColor without the MainColor attribute. Material.color
            // can therefore return black, and a renderer-wide tint also destroys eye/glove colours.
            // Keep the apple/face intact; identify friends by clothing and shape-coded name tags.
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var material = mats[i];
                    if (!material || !material.HasProperty("_BaseColor")) continue;
                    Color color = material.GetColor("_BaseColor");
                    bool clothing = material.name.Contains("Sleeve") || material.name.Contains("Pack") && !material.name.Contains("Trim") || material.name.Contains("Shoe");
                    if (clothing) color = slot % 3 == 1 ? new Color(.24f,.72f,.67f) : new Color(.94f,.67f,.25f);
                    var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block, i);
                    block.SetColor("_BaseColor", color); r.SetPropertyBlock(block, i);
                }
            }
            pc.WakeUp();
            if (localKid.Motor) typeof(DreamRush.Player.PlayerMotor3D).GetProperty("Instance").SetValue(null, localKid.Motor);
            avatars[slot] = pc;
            if (!remote.ContainsKey(slot)) remote[slot] = new RemoteState { pos = go.transform.position };
            NetGame.Connected = Mathf.Max(NetGame.Connected, avatars.Count);
            return pc;
        }

        void DespawnPuppet(int slot)
        {
            if (avatars.TryGetValue(slot, out var pc) && pc && !pc.isLocal) { if (pc.Held) { var h = pc.ReleaseHeld(); h.OnDropped(pc, Vector3.zero); } Destroy(pc.gameObject); }
            avatars.Remove(slot); remote.Remove(slot);
        }

        // ================================================================== start / stop
        NetworkManager MakeManager()
        {
            if (NetworkManager.Singleton) { nm = NetworkManager.Singleton; return nm; }
            var go = new GameObject("NetworkManager"); DontDestroyOnLoad(go);
            var utp = go.AddComponent<UnityTransport>();
            nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = utp, ConnectionApproval = false, EnableSceneManagement = false, TickRate = 30 };
            return nm;
        }

        void Hook()
        {
            nm.OnClientConnectedCallback += OnConnected;
            nm.OnClientDisconnectCallback += OnDisconnected;
            var cm = nm.CustomMessagingManager;
            cm.RegisterNamedMessageHandler("mc/state", OnState);
            cm.RegisterNamedMessageHandler("mc/act", OnAct);
            cm.RegisterNamedMessageHandler("mc/world", OnWorld);
            cm.RegisterNamedMessageHandler("mc/field", OnField);
            cm.RegisterNamedMessageHandler("mc/evt", OnEvent);
            cm.RegisterNamedMessageHandler("mc/roster", OnRoster);
            cm.RegisterNamedMessageHandler("mc/snd", OnSound);
            cm.RegisterNamedMessageHandler("mc/hit", OnHit);
        }

        public async void HostOnline()
        {
            if (State != Mode.Offline) return;
            State = Mode.Connecting; Status = Txt("Creating room...", "กำลังสร้างห้อง...");
            MakeManager();
            try
            {
                session = await NetServices.CreateSession(MaxPlayers);
                JoinCode = NetServices.CodeOf(session);
                if (!nm.IsServer) throw new Exception("relay host did not start");
                BecomeHost();
                Status = Txt("Room code: ", "รหัสห้อง: ") + JoinCode + Txt("  (friends: ESC > Join)", "  (เพื่อน: ESC > เข้าร่วม)");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Co-op] Online host failed, falling back to LAN: " + e.Message);
                HostLan(Txt("Online services unavailable - hosting on LAN. ", "ใช้บริการออนไลน์ไม่ได้ - เปิดห้องแบบ LAN แทน "));
            }
        }

        public void HostLan(string prefix = "")
        {
            if (State == Mode.Host) return;
            MakeManager();
            var utp = nm.GetComponent<UnityTransport>(); utp.SetConnectionData("0.0.0.0", Port, "0.0.0.0");
            if (!nm.StartHost()) { State = Mode.Offline; Status = Txt("Could not start host", "เปิดห้องไม่ได้"); return; }
            BecomeHost();
            JoinCode = LocalIP();
            Status = prefix + Txt("LAN room: ", "ห้อง LAN: ") + JoinCode;
        }

        void BecomeHost()
        {
            State = Mode.Host; NetGame.Online = true; NetGame.IsHost = true;
            Hook();
            slotOf.Clear(); slotOf[nm.LocalClientId] = 0; mySlot = 0;
            BindScene();
            NetGame.Connected = 1;
        }

        public async void Join(string codeOrIp)
        {
            if (State != Mode.Offline || string.IsNullOrWhiteSpace(codeOrIp)) return;
            codeOrIp = codeOrIp.Trim();
            State = Mode.Connecting; Status = Txt("Joining ", "กำลังเข้า ") + codeOrIp + "...";
            MakeManager();
            bool isIp = codeOrIp.Contains(".") || codeOrIp == "localhost";
            try
            {
                if (isIp)
                {
                    nm.GetComponent<UnityTransport>().SetConnectionData(codeOrIp == "localhost" ? "127.0.0.1" : codeOrIp, Port);
                    if (!nm.StartClient()) throw new Exception("StartClient failed");
                }
                else session = await NetServices.JoinSession(codeOrIp.ToUpperInvariant());
                State = Mode.Client; NetGame.Online = true; NetGame.IsHost = false;
                Hook();
                Status = Txt("Connecting...", "กำลังเชื่อมต่อ...");
            }
            catch (Exception e)
            {
                State = Mode.Offline; NetGame.Online = false; Status = Txt("Could not join: ", "เข้าห้องไม่ได้: ") + e.Message;
                if (nm && nm.IsListening) nm.Shutdown();
            }
        }

        public void Leave()
        {
            if (nm && nm.IsListening) nm.Shutdown();
            NetServices.Leave(session); session = null;
            foreach (var s in new List<int>(avatars.Keys)) if (s != mySlot) DespawnPuppet(s);
            State = Mode.Offline; NetGame.Online = false; NetGame.IsHost = true; NetGame.Connected = 1; slotOf.Clear(); mySlot = 0; JoinCode = "";
            Status = Txt("Playing solo", "เล่นคนเดียว");
            if (GameManager.I && GameManager.I.state != GameManager.State.Title) GameManager.I.Restart();
        }

        void OnConnected(ulong id)
        {
            if (State == Mode.Client && id == nm.LocalClientId) { Status = Txt("Connected! Waiting for the host to start...", "เชื่อมต่อแล้ว! รอเจ้าของห้องเริ่มเกม..."); return; }
            if (State != Mode.Host || slotOf.ContainsKey(id)) return;
            int slot = 1; while (slotOf.ContainsValue(slot)) slot++;
            if (slot >= MaxPlayers) { nm.DisconnectClient(id); return; }
            slotOf[id] = slot;
            SpawnPuppet(slot);
            NetGame.Connected = slotOf.Count;
            SendRoster();
            if (GameManager.I) GameManager.I.Toast(Txt("P" + (slot + 1) + " joined the party!", "P" + (slot + 1) + " เข้าร่วมแล้ว!"));
            Status = Txt("Room: ", "ห้อง: ") + JoinCode + "   " + Txt("Players: ", "ผู้เล่น: ") + slotOf.Count + "/" + MaxPlayers;
        }

        void OnDisconnected(ulong id)
        {
            if (State == Mode.Client && (id == nm.LocalClientId || id == NetworkManager.ServerClientId)) { Status = Txt("Disconnected from host", "หลุดจากห้อง"); Leave(); return; }
            if (State != Mode.Host || !slotOf.TryGetValue(id, out int slot)) return;
            slotOf.Remove(id); DespawnPuppet(slot); NetGame.Connected = slotOf.Count; SendRoster();
            if (GameManager.I) GameManager.I.Toast(Txt("P" + (slot + 1) + " left.", "P" + (slot + 1) + " ออกจากห้อง"));
        }

        static string LocalIP()
        {
            try { foreach (var a in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList) if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return a.ToString(); }
            catch { }
            return "127.0.0.1";
        }

        static string Txt(string en, string th) => Loc.Thai ? th : en;

        // ================================================================== per-frame
        void Update()
        {
            if (State != Mode.Host && State != Mode.Client) return;
            if (!nm || !nm.IsListening) return;
            float dt = Time.deltaTime;

            // remote avatars: smooth to their reported pose; the host also performs their continuous actions
            foreach (var kv in avatars)
            {
                if (kv.Key == mySlot || !kv.Value || !remote.TryGetValue(kv.Key, out var rs)) continue;
                var pc = kv.Value;
                pc.transform.position = Vector3.Lerp(pc.transform.position, rs.pos, 1f - Mathf.Exp(-14f * dt));
                pc.transform.rotation = Quaternion.Slerp(pc.transform.rotation, Quaternion.Euler(0, rs.yaw, 0), 1f - Mathf.Exp(-14f * dt));
                pc.RemoteTick(rs.speed, rs.sprint, rs.stunned);
                if (State == Mode.Host)
                {
                    if (rs.use && pc.Held) pc.Held.UseHeld(pc, dt);
                    if (rs.holdTarget >= 0 && rs.holdTarget < reg.Count && reg[rs.holdTarget]) reg[rs.holdTarget].HoldTick(pc, 0.5f);
                }
            }

            sendT -= dt;
            if (sendT <= 0f && localKid) { sendT = 0.05f; SendMyState(); }

            if (State == Mode.Host)
            {
                worldT -= dt; fieldT -= dt;
                if (worldT <= 0f && slotOf.Count > 1) { worldT = 0.1f; SendWorld(); }
                if (fieldT <= 0f && slotOf.Count > 1) { fieldT = 0.25f; SendField(); }
                if (sounds.Count > 0) { if (slotOf.Count > 1) SendSounds(); sounds.Clear(); }
            }
            else WatchLocalHeld();
            SmoothItems(dt);
        }

        // ================================================================== player state
        void SendMyState()
        {
            var w = Begin();
            w.Write((byte)mySlot);
            var p = localKid.transform.position; w.Write(p.x); w.Write(p.y); w.Write(p.z);
            w.Write(localKid.transform.eulerAngles.y); w.Write(localKid.PlanarSpeed);
            byte f = 0; if (MamaInput.UseHeld && localKid.Held && localKid.controlsEnabled) f |= 1; if (localKid.Sprinting) f |= 2; if (localKid.Stunned) f |= 4;
            w.Write(f);
            w.Write((short)(localKid.HoldProgress > 0f && localKid.Target ? reg.IndexOf(localKid.Target) : -1));
            if (State == Mode.Host) SendAll("mc/state", w, NetworkDelivery.UnreliableSequenced);
            else SendHost("mc/state", w, NetworkDelivery.UnreliableSequenced);
        }

        void OnState(ulong sender, FastBufferReader reader)
        {
            var r = Read(reader); if (r == null) return;
            int slot = r.ReadByte();
            if (State == Mode.Host && slotOf.TryGetValue(sender, out int s)) slot = s;   // never trust the claimed slot
            if (slot == mySlot) return;
            if (!remote.TryGetValue(slot, out var rs)) { rs = new RemoteState(); remote[slot] = rs; }
            rs.pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); rs.yaw = r.ReadSingle(); rs.speed = r.ReadSingle();
            byte f = r.ReadByte(); rs.use = (f & 1) != 0; rs.sprint = (f & 2) != 0; rs.stunned = (f & 4) != 0;
            rs.holdTarget = r.ReadInt16(); rs.t = Time.time;
            if (!avatars.ContainsKey(slot)) SpawnPuppet(slot);
            if (State == Mode.Host)   // relay to the other clients
            {
                var w = Begin(); w.Write((byte)slot); w.Write(rs.pos.x); w.Write(rs.pos.y); w.Write(rs.pos.z); w.Write(rs.yaw); w.Write(rs.speed); w.Write(f); w.Write((short)rs.holdTarget);
                foreach (var kv in slotOf) if (kv.Key != sender && kv.Key != nm.LocalClientId) Send("mc/state", kv.Key, w, NetworkDelivery.UnreliableSequenced);
            }
        }

        // ================================================================== actions (client -> host)
        void SendAct(byte type, Interactable t, Pickup item, Vector3 pos = default, Quaternion rot = default, Vector3 vel = default)
        {
            if (State != Mode.Client) return;
            var w = Begin(); w.Write(type); w.Write((short)(t ? reg.IndexOf(t) : item ? reg.IndexOf(item) : -1));
            w.Write(pos.x); w.Write(pos.y); w.Write(pos.z); w.Write(rot.x); w.Write(rot.y); w.Write(rot.z); w.Write(rot.w); w.Write(vel.x); w.Write(vel.y); w.Write(vel.z);
            SendHost("mc/act", w, NetworkDelivery.ReliableSequenced);
        }

        void OnAct(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Host || !slotOf.TryGetValue(sender, out int slot)) return;
            var r = Read(reader); if (r == null) return;
            byte type = r.ReadByte(); int idx = r.ReadInt16();
            var pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); var rot = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); var vel = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            if (!avatars.TryGetValue(slot, out var pc) || !pc || idx < 0 || idx >= reg.Count || !reg[idx]) return;
            var target = reg[idx];
            NetGame.Acting = pc; NetGame.ActingClient = sender;
            try
            {
                if (type == 0)
                {
                    if (target is Pickup pk && pk.IsHeld && pc.Held != pk) return;
                    target.Interact(pc);
                }
                else if (target is Pickup item && pc.Held == item)
                {
                    pc.ReleaseHeld();
                    item.transform.SetPositionAndRotation(pos, rot);
                    item.OnDropped(pc, vel);
                    if (type == 2 && item is Bucket b) b.OnThrown(pc);
                }
            }
            finally { NetGame.Acting = null; }
        }

        /// Client: the local kid let go of something (Q / throw / slipped) -> tell the host where it went.
        Pickup lastHeld; bool hostRelease;
        void WatchLocalHeld()
        {
            if (!localKid) return;
            if (lastHeld && localKid.Held != lastHeld && !hostRelease)
            {
                var b = lastHeld.Body;
                SendAct((byte)(b && b.linearVelocity.sqrMagnitude > 9f ? 2 : 1), null, lastHeld, lastHeld.transform.position, lastHeld.transform.rotation, b ? b.linearVelocity : Vector3.zero);
                if (b) b.isKinematic = true;
                pending[reg.IndexOf(lastHeld)] = Time.time + 0.6f;   // ignore stale host poses for a moment
            }
            hostRelease = false;
            lastHeld = localKid.Held;
        }
        readonly Dictionary<int, float> pending = new Dictionary<int, float>();

        // ================================================================== world state (host -> clients)
        void SendWorld()
        {
            var w = Begin();
            var gm = GameManager.I;
            w.Write((byte)gm.state); w.Write(gm.TimeLeft); w.Write(PowerGrid.On);
            var qd = QuestDirector.I; w.Write(qd ? qd.Neighbor : 0f); w.Write(qd ? qd.Elapsed : 0f);
            // items
            w.Write((short)pickups.Count);
            foreach (var p in pickups)
            {
                if (!p) { w.Write((byte)254); continue; }
                int holder = 255; foreach (var kv in avatars) if (kv.Value && kv.Value.Held == p) holder = kv.Key;
                w.Write((byte)holder);
                w.Write(p.gameObject.activeSelf);
                var t = p.transform; w.Write(t.position.x); w.Write(t.position.y); w.Write(t.position.z);
                var q = t.rotation; w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w);
            }
            // devices / fires / problems
            w.Write((short)synced.Count);
            foreach (var c in synced) WriteFields(w, c);
            w.Write((short)Hazard.All.Count);
            var hz = new List<Hazard>(FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            hz.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform), PathOf(b.transform)));
            w.Write((short)hz.Count); foreach (var h in hz) h.WriteState(w);
            // splats
            var sp = SplatMess.AllSplats; w.Write((short)sp.Count);
            foreach (var s in sp) { w.Write((byte)s.kind); var p = s.transform.position; w.Write(p.x); w.Write(p.z); w.Write(s.transform.localScale.x); w.Write(s.transform.localScale.z); w.Write(s.amount); }
            // Mom
            var mom = gm.mom; w.Write(mom ? mom.transform.position.x : 0f); w.Write(mom ? mom.transform.position.z : 0f); w.Write(mom ? mom.transform.eulerAngles.y : 0f);
            SendAll("mc/world", w, NetworkDelivery.ReliableFragmentedSequenced);
        }

        readonly Dictionary<int, (Vector3 p, Quaternion q)> itemTarget = new Dictionary<int, (Vector3, Quaternion)>();
        readonly List<SplatMess> clientSplats = new List<SplatMess>();

        void OnWorld(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Client) return;
            var r = Read(reader); if (r == null) return;
            var gm = GameManager.I; if (!gm) return;
            var st = (GameManager.State)r.ReadByte(); float tl = r.ReadSingle(); bool power = r.ReadBoolean();
            float neigh = r.ReadSingle(); r.ReadSingle();
            if (st != gm.state)
            {
                if (gm.state == GameManager.State.Title && st != GameManager.State.Title) gm.StartRound();
                if (st == GameManager.State.Playing && gm.state == GameManager.State.Intro) { }  // intro timer on the client handles it
                if (st == GameManager.State.MomHome && gm.state == GameManager.State.Playing) gm.SetTimeLeft(0.01f);
            }
            if (st == GameManager.State.Playing || st == GameManager.State.MomHome) gm.SetTimeLeft(tl);
            PowerGrid.Set(power);
            if (QuestDirector.I) QuestDirector.I.Neighbor = neigh;

            int n = r.ReadInt16();
            for (int i = 0; i < n; i++)
            {
                byte holder = r.ReadByte(); if (holder == 254) continue;
                bool active = r.ReadBoolean();
                var pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); var q = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                if (i >= pickups.Count || !pickups[i]) continue;
                var p = pickups[i];
                if (p.gameObject.activeSelf != active) p.gameObject.SetActive(active);
                if (holder == mySlot)
                {
                    if (localKid.Held != p) { if (localKid.Held) { hostRelease = true; localKid.ReleaseHeld(); } localKid.PickUp(p); lastHeld = p; }
                    continue;
                }
                if (localKid.Held == p && holder != mySlot && !(pending.TryGetValue(reg.IndexOf(p), out float until) && Time.time < until)) { hostRelease = true; localKid.ReleaseHeld(); p.Body.isKinematic = true; }
                if (pending.TryGetValue(reg.IndexOf(p), out float u2) && Time.time < u2) continue;
                if (p.Body && !p.Body.isKinematic) p.Body.isKinematic = true;
                if (holder != 255 && avatars.TryGetValue(holder, out var pc) && pc && pc.Held != p) { p.transform.SetParent(null, true); }
                itemTarget[i] = (pos, q);
            }
            int ns = r.ReadInt16();
            for (int i = 0; i < ns && i < synced.Count; i++) ReadFields(r, synced[i]);
            r.ReadInt16(); int nh = r.ReadInt16();
            var hz = new List<Hazard>(FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            hz.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform), PathOf(b.transform)));
            for (int i = 0; i < nh; i++) { if (i < hz.Count) hz[i].ReadState(r); else { r.ReadBoolean(); r.ReadBoolean(); r.ReadBoolean(); r.ReadSingle(); } }
            int nsp = r.ReadInt16();
            while (clientSplats.Count < nsp) clientSplats.Add(SplatMess.Spawn(SplatKind.Confetti, Vector3.zero, 1f, null));
            for (int i = 0; i < clientSplats.Count; i++)
            {
                if (i >= nsp) { if (clientSplats[i]) Destroy(clientSplats[i].gameObject); continue; }
                var kind = (SplatKind)r.ReadByte(); float x = r.ReadSingle(), z = r.ReadSingle(), sx = r.ReadSingle(), sz = r.ReadSingle(), amt = r.ReadSingle();
                var s = clientSplats[i]; if (!s) continue;
                if (s.kind != kind || s.GetComponent<Renderer>().sharedMaterial == null) { s.kind = kind; s.sticky = kind == SplatKind.Soda; s.GetComponent<Renderer>().sharedMaterial = SplatMess.MatFor(kind); }
                s.transform.position = new Vector3(x, 0.014f, z); s.transform.localScale = new Vector3(sx, kind == SplatKind.Foam ? 0.05f : 0.003f, sz); s.amount = amt;
            }
            if (clientSplats.Count > nsp) clientSplats.RemoveRange(nsp, clientSplats.Count - nsp);
            var mom = gm.mom;
            float mx = r.ReadSingle(), mz = r.ReadSingle(), my = r.ReadSingle();
            if (mom && gm.state >= GameManager.State.MomHome) { momTarget = new Vector3(mx, 0, mz); momYaw = my; }
        }
        Vector3 momTarget; float momYaw;

        void SmoothItems(float dt)
        {
            if (State != Mode.Client) return;
            float k = 1f - Mathf.Exp(-16f * dt);
            foreach (var kv in itemTarget)
            {
                var p = pickups[kv.Key]; if (!p || p == localKid.Held) continue;
                p.transform.position = Vector3.Lerp(p.transform.position, kv.Value.p, k);
                p.transform.rotation = Quaternion.Slerp(p.transform.rotation, kv.Value.q, k);
            }
            var mom = GameManager.I ? GameManager.I.mom : null;
            if (mom && GameManager.I.state >= GameManager.State.MomHome && momTarget != Vector3.zero)
            {
                mom.transform.position = Vector3.Lerp(mom.transform.position, momTarget, k);
                mom.transform.rotation = Quaternion.Slerp(mom.transform.rotation, Quaternion.Euler(0, momYaw, 0), k);
            }
        }

        // ---------------------------------------------------------------- generic device fields
        static FieldInfo[] FieldsOf(Type t)
        {
            if (fieldCache.TryGetValue(t, out var f)) return f;
            var list = new List<FieldInfo>();
            for (var tt = t; tt != null && tt != typeof(MonoBehaviour); tt = tt.BaseType)
                foreach (var fi in tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if ((fi.FieldType == typeof(bool) || fi.FieldType == typeof(float) || fi.FieldType == typeof(int)) && !fi.IsInitOnly && !fi.Name.Contains("Radius") && !fi.Name.Contains("radius"))
                        list.Add(fi);
            list.Sort((a, b) => string.CompareOrdinal(a.DeclaringType.Name + a.Name, b.DeclaringType.Name + b.Name));
            f = list.ToArray(); fieldCache[t] = f; return f;
        }

        static void WriteFields(BinaryWriter w, Component c)
        {
            var fs = FieldsOf(c.GetType()); w.Write((byte)fs.Length);
            foreach (var f in fs)
            {
                object v = f.GetValue(c);
                if (v is bool b) w.Write(b); else if (v is float fl) w.Write(fl); else w.Write((int)v);
            }
        }

        static void ReadFields(BinaryReader r, Component c)
        {
            var fs = FieldsOf(c.GetType()); int n = r.ReadByte();
            for (int i = 0; i < n; i++)
            {
                var f = i < fs.Length ? fs[i] : null;
                if (f == null) { r.ReadInt32(); continue; }
                if (f.FieldType == typeof(bool)) f.SetValue(c, r.ReadBoolean());
                else if (f.FieldType == typeof(float)) f.SetValue(c, r.ReadSingle());
                else f.SetValue(c, r.ReadInt32());
            }
        }

        // ================================================================== water + smoke fields
        void SendField()
        {
            var w = Begin();
            WriteRle(w, FloodSim.I ? FloodSim.I.PackDepth() : new byte[0]);
            WriteRle(w, SmokeSim.I ? SmokeSim.I.Pack() : new byte[0]);
            SendAll("mc/field", w, NetworkDelivery.ReliableFragmentedSequenced);
        }
        void OnField(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Client) return;
            var r = Read(reader); if (r == null) return;
            var a = ReadRle(r); var b = ReadRle(r);
            if (FloodSim.I) FloodSim.I.UnpackDepth(a);
            if (SmokeSim.I) SmokeSim.I.Unpack(b);
        }
        static void WriteRle(BinaryWriter w, byte[] d)
        {
            w.Write(d.Length); int i = 0;
            while (i < d.Length) { byte v = d[i]; int run = 1; while (i + run < d.Length && d[i + run] == v && run < 255) run++; w.Write((byte)run); w.Write(v); i += run; }
        }
        static byte[] ReadRle(BinaryReader r)
        {
            int len = r.ReadInt32(); var d = new byte[len]; int i = 0;
            while (i < len) { int run = r.ReadByte(); byte v = r.ReadByte(); for (int k = 0; k < run && i < len; k++) d[i++] = v; }
            return d;
        }

        // ================================================================== events / roster / sounds
        void SendEvent(byte kind, string en, string th, int level, ulong? only)
        {
            if (State != Mode.Host) return;
            var w = Begin(); w.Write(kind); w.Write(en ?? ""); w.Write(th ?? ""); w.Write(level);
            if (only.HasValue) Send("mc/evt", only.Value, w, NetworkDelivery.ReliableSequenced);
            else SendAll("mc/evt", w, NetworkDelivery.ReliableSequenced);
        }
        public void SendKidSay(ulong client, string s) => SendEvent((byte)NetGame.Msg.KidSay, s, null, 0, client);
        public void SendEnding(string title, string body, string stats, Color c) => SendEvent((byte)NetGame.Msg.Ending, title + "\u0001" + body + "\u0001" + stats, ColorUtility.ToHtmlStringRGB(c), 0, null);

        void OnEvent(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Client) return;
            var r = Read(reader); if (r == null) return;
            var kind = (NetGame.Msg)r.ReadByte(); string en = r.ReadString(), th = r.ReadString(); int level = r.ReadInt32();
            var gm = GameManager.I; if (!gm) return;
            string txt = Loc.Thai && th.Length > 0 && kind != NetGame.Msg.Ending ? th : en;
            switch (kind)
            {
                case NetGame.Msg.Call: gm.ui.Call(txt, level >= 2 ? MamaUI.Red : MamaUI.Orange); if (level >= 2 && gm.cam) gm.cam.Shake(0.4f); break;
                case NetGame.Msg.Toast: gm.ui.Toast(txt); break;
                case NetGame.Msg.KidSay: gm.ui.KidSay(txt); break;
                case NetGame.Msg.MomSay: gm.ui.MomSay(txt); break;
                case NetGame.Msg.Start: gm.StartRound(); break;
                case NetGame.Msg.Restart: gm.Restart(); break;
                case NetGame.Msg.Ending:
                    var parts = en.Split('\u0001'); ColorUtility.TryParseHtmlString("#" + th, out var col);
                    gm.ShowRemoteEnding(parts[0], parts.Length > 1 ? parts[1] : "", parts.Length > 2 ? parts[2] : "", col);
                    break;
            }
        }

        void SendRoster()
        {
            var w = Begin(); w.Write((byte)slotOf.Count);
            foreach (var kv in slotOf) { w.Write(kv.Key); w.Write((byte)kv.Value); }
            SendAll("mc/roster", w, NetworkDelivery.ReliableSequenced);
        }
        void OnRoster(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Client) return;
            var r = Read(reader); if (r == null) return;
            int n = r.ReadByte(); var seen = new HashSet<int>(); slotOf.Clear();
            for (int i = 0; i < n; i++) { ulong id = r.ReadUInt64(); int slot = r.ReadByte(); slotOf[id] = slot; seen.Add(slot); }
            if (slotOf.TryGetValue(nm.LocalClientId, out int mine) && mine != mySlot)
            {
                avatars.Remove(mySlot); mySlot = mine; avatars[mySlot] = localKid; PlaceAtSpawn(localKid, mySlot);
                ApplyClientMode();
            }
            foreach (var s in seen) if (s != mySlot) SpawnPuppet(s);
            foreach (var s in new List<int>(avatars.Keys)) if (!seen.Contains(s) && s != mySlot) DespawnPuppet(s);
            NetGame.Connected = n;
            Status = Txt("In room ", "อยู่ในห้อง ") + JoinCode + "  P" + (mySlot + 1) + "  (" + n + "/" + MaxPlayers + ")";
        }

        void SendSounds()
        {
            var w = Begin(); w.Write((byte)Mathf.Min(sounds.Count, 40));
            for (int i = 0; i < sounds.Count && i < 40; i++) { var s = sounds[i]; w.Write(s.n); w.Write(s.p.x); w.Write(s.p.y); w.Write(s.p.z); w.Write(s.v); w.Write(s.pi); }
            SendAll("mc/snd", w, NetworkDelivery.UnreliableSequenced);
        }
        void OnSound(ulong sender, FastBufferReader reader)
        {
            if (State != Mode.Client) return;
            var r = Read(reader); if (r == null) return;
            int n = r.ReadByte();
            for (int i = 0; i < n; i++) { string name = r.ReadString(); var p = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); float v = r.ReadSingle(), pi = r.ReadSingle(); MamaAudio.PlayLocal(name, p, v, pi); }
        }

        // ================================================================== transport helpers
        MemoryStream ms; BinaryWriter bw;
        BinaryWriter Begin() { ms = new MemoryStream(); bw = new BinaryWriter(ms); return bw; }

        FastBufferWriter Pack()
        {
            bw.Flush(); var bytes = ms.ToArray();
            var fw = new FastBufferWriter(bytes.Length + 8, Allocator.Temp, 1 << 20);
            fw.WriteValueSafe(bytes.Length); fw.WriteBytesSafe(bytes, bytes.Length);
            return fw;
        }
        static BinaryReader Read(FastBufferReader reader)
        {
            try { reader.ReadValueSafe(out int len); var bytes = new byte[len]; reader.ReadBytesSafe(ref bytes, len); return new BinaryReader(new MemoryStream(bytes)); }
            catch (Exception e) { Debug.LogWarning("[Co-op] bad packet " + e.Message); return null; }
        }
        void Send(string name, ulong client, BinaryWriter w, NetworkDelivery d) { using (var fw = Pack()) nm.CustomMessagingManager.SendNamedMessage(name, client, fw, d); }
        void SendHost(string name, BinaryWriter w, NetworkDelivery d) { using (var fw = Pack()) nm.CustomMessagingManager.SendNamedMessage(name, NetworkManager.ServerClientId, fw, d); }
        void SendAll(string name, BinaryWriter w, NetworkDelivery d)
        {
            using (var fw = Pack())
                foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) nm.CustomMessagingManager.SendNamedMessage(name, id, fw, d);
        }

        // ================================================================== slapstick hits (shove / bump), any peer -> victim's owner
        int SlotOfAvatar(PlayerController pc)
        {
            if (pc == localKid) return mySlot;
            foreach (var kv in avatars) if (kv.Value == pc) return kv.Key;
            return -1;
        }
        void WriteHit(BinaryWriter w, int slot, Vector3 dir, float force, float stun, bool drop)
        { w.Write((byte)slot); w.Write(dir.x); w.Write(dir.z); w.Write(force); w.Write(stun); w.Write(drop); }

        void SendHitMsg(PlayerController victim, Vector3 dir, float force, float stun, bool drop)
        {
            int slot = SlotOfAvatar(victim); if (slot < 0) return;
            var w = Begin(); WriteHit(w, slot, dir, force, stun, drop);
            if (State == Mode.Host) { foreach (var kv in slotOf) if (kv.Value == slot && kv.Key != nm.LocalClientId) Send("mc/hit", kv.Key, w, NetworkDelivery.Reliable); }
            else SendHost("mc/hit", w, NetworkDelivery.Reliable);
        }

        void OnHit(ulong sender, FastBufferReader reader)
        {
            var r = Read(reader); if (r == null) return;
            int slot = r.ReadByte(); var dir = new Vector3(r.ReadSingle(), 0f, r.ReadSingle()); float force = r.ReadSingle(), stun = r.ReadSingle(); bool drop = r.ReadBoolean();
            if (slot == mySlot) { if (localKid) localKid.Bonk(dir, Mathf.Min(force, 8f), Mathf.Min(stun, 1.5f), drop); return; }
            if (State == Mode.Host)   // relay to the victim's owner
            {
                var w = Begin(); WriteHit(w, slot, dir, force, stun, drop);
                foreach (var kv in slotOf) if (kv.Value == slot && kv.Key != nm.LocalClientId && kv.Key != sender) Send("mc/hit", kv.Key, w, NetworkDelivery.Reliable);
            }
        }

        void OnApplicationQuit() { if (nm && nm.IsListening) nm.Shutdown(); }
    }
}
