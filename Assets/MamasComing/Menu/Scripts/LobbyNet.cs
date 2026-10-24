using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MamasComing.Menu
{
    /// Lobby traffic on the same NetworkManager / room that NetSession opened (Relay code or LAN), using its own "mcl/*" named messages.
    /// The host owns the room settings, the roster (with everyone's look), the beach ball and the start signal.
    /// Works offline too (solo lobby): everything just runs locally.
    public class LobbyNet : MonoBehaviour
    {
        public static LobbyNet I { get; private set; }
        public class Peer { public ulong id; public Cosmetic look; public int number; }

        public Lobby lobby;
        NetworkManager nm;
        bool online, hooked;
        public bool Online => online && nm && nm.IsListening;
        public bool IsHost => !Online || nm.IsServer;
        public ulong LocalId => Online ? nm.LocalClientId : 0UL;
        public int Level { get; private set; }
        public int Max { get; private set; } = 3;
        public bool Starting { get; private set; }
        public readonly List<Peer> Roster = new List<Peer>();
        float poseT, ballT, roomT;
        static readonly string[] Names = { "mcl/hi", "mcl/room", "mcl/pose", "mcl/ball", "mcl/kick", "mcl/go" };

        void Awake() { I = this; }

        void Start()
        {
            nm = NetworkManager.Singleton;
            online = nm && nm.IsListening;
            // NetSession keeps the connection; its per-frame world sync waits until a level with kids is loaded
            if (NetSession.I) NetSession.I.enabled = false;
            Level = MenuFlow.Level; Max = Mathf.Clamp(MenuFlow.MaxPlayers, MenuFlow.MinPlayers, MenuFlow.MaxAllowed);
            if (Online)
            {
                var cm = nm.CustomMessagingManager;
                cm.RegisterNamedMessageHandler("mcl/hi", OnHello);
                cm.RegisterNamedMessageHandler("mcl/room", OnRoom);
                cm.RegisterNamedMessageHandler("mcl/pose", OnPose);
                cm.RegisterNamedMessageHandler("mcl/ball", OnBall);
                cm.RegisterNamedMessageHandler("mcl/kick", OnKick);
                cm.RegisterNamedMessageHandler("mcl/go", OnGo);
                nm.OnClientConnectedCallback += OnConnected;
                nm.OnClientDisconnectCallback += OnDisconnected;
                hooked = true;
            }
            if (IsHost) { Upsert(LocalId, MenuFlow.Mine); BroadcastRoom(); }
            else SendHello();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            if (!hooked || !nm) return;
            if (nm.CustomMessagingManager != null) foreach (var n in Names) nm.CustomMessagingManager.UnregisterNamedMessageHandler(n);
            nm.OnClientConnectedCallback -= OnConnected;
            nm.OnClientDisconnectCallback -= OnDisconnected;
        }

        void Update()
        {
            if (Starting) return;
            if (online && (!nm || !nm.IsListening)) { lobby.Kicked(MenuFlow.Txt("Lost the room connection.", "หลุดจากห้องแล้ว")); online = false; return; }
            if (!Online) return;
            float dt = Time.unscaledDeltaTime;
            poseT -= dt; if (poseT <= 0f) { poseT = 1f / 15f; SendMyPose(); }
            if (IsHost)
            {
                ballT -= dt; if (ballT <= 0f) { ballT = 1f / 12f; SendBall(); }
                roomT -= dt; if (roomT <= 0f) { roomT = 2f; BroadcastRoom(); }
            }
        }

        // ------------------------------------------------------------ roster
        Peer Upsert(ulong id, Cosmetic look)
        {
            var p = Roster.Find(x => x.id == id);
            if (p == null) { p = new Peer { id = id }; Roster.Add(p); }
            p.look = look; Renumber(); return p;
        }
        void Renumber() { for (int i = 0; i < Roster.Count; i++) Roster[i].number = i + 1; }

        public Peer PeerOf(ulong id) => Roster.Find(x => x.id == id);

        /// Local player changed clothes.
        public void SetMyLook(Cosmetic c)
        {
            MenuFlow.Mine = c;
            if (IsHost) { Upsert(LocalId, c); BroadcastRoom(); }
            else SendHello();
            lobby.OnRosterChanged();
        }

        /// Host only: change the level / room size from the lobby board.
        public void SetRoom(int level, int max)
        {
            if (!IsHost) return;
            Level = (level + MenuFlow.Levels.Length) % MenuFlow.Levels.Length; MenuFlow.Level = Level;
            Max = Mathf.Clamp(max, Mathf.Max(MenuFlow.MinPlayers, Roster.Count), MenuFlow.MaxAllowed); MenuFlow.MaxPlayers = Max;
            BroadcastRoom(); lobby.OnRosterChanged();
        }

        void OnConnected(ulong id)
        {
            if (!nm.IsServer || id == nm.LocalClientId) return;
            int count = 0; foreach (var _ in nm.ConnectedClientsIds) count++;
            if (count > Max || Starting) { nm.DisconnectClient(id); return; }   // room full (host's chosen size) or already leaving for the level
            Upsert(id, new Cosmetic { fruit = (byte)(Roster.Count % MenuFlow.FruitEn.Length), hat = 1, color = (byte)(Roster.Count * 3 % MenuFlow.Colors.Length) });
            BroadcastRoom(); lobby.OnRosterChanged();
            MenuSfx.Play(MenuSfx.Kind.Pop);
        }

        void OnDisconnected(ulong id)
        {
            if (!nm.IsServer) return;   // a client losing the host is handled in Update
            Roster.RemoveAll(p => p.id == id); Renumber();
            BroadcastRoom(); lobby.OnRosterChanged();
        }

        void SendHello()
        {
            if (!Online) return;
            using var w = new FastBufferWriter(16, Allocator.Temp);
            WriteLook(w, MenuFlow.Mine);
            nm.CustomMessagingManager.SendNamedMessage("mcl/hi", NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }

        void OnHello(ulong sender, FastBufferReader r)
        {
            if (!IsHost) return;
            Upsert(sender, ReadLook(ref r));
            BroadcastRoom(); lobby.OnRosterChanged();
        }

        void BroadcastRoom()
        {
            if (!Online || !nm.IsServer) return;
            using var w = new FastBufferWriter(64 + Roster.Count * 16, Allocator.Temp);
            w.WriteValueSafe((byte)Level); w.WriteValueSafe((byte)Max); w.WriteValueSafe((byte)Roster.Count);
            foreach (var p in Roster) { w.WriteValueSafe(p.id); WriteLook(w, p.look); }
            SendAll("mcl/room", w, NetworkDelivery.ReliableSequenced, ulong.MaxValue);
        }

        void OnRoom(ulong sender, FastBufferReader r)
        {
            if (IsHost) return;
            r.ReadValueSafe(out byte lvl); r.ReadValueSafe(out byte max); r.ReadValueSafe(out byte n);
            Level = Mathf.Clamp(lvl, 0, MenuFlow.Levels.Length - 1); Max = max; MenuFlow.Level = Level; MenuFlow.MaxPlayers = Max;
            Roster.Clear();
            for (int i = 0; i < n; i++) { r.ReadValueSafe(out ulong id); Roster.Add(new Peer { id = id, look = ReadLook(ref r) }); }
            Renumber();
            lobby.OnRosterChanged();
        }

        // ------------------------------------------------------------ poses (15 Hz, host relays)
        void SendMyPose()
        {
            var me = lobby.LocalAvatar; if (!me) return;
            using var w = new FastBufferWriter(48, Allocator.Temp);
            WritePose(w, LocalId, me);
            if (IsHost) SendAll("mcl/pose", w, NetworkDelivery.UnreliableSequenced, ulong.MaxValue);
            else nm.CustomMessagingManager.SendNamedMessage("mcl/pose", NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
        }

        static void WritePose(FastBufferWriter w, ulong id, LobbyAvatar a)
        {
            var p = a.transform.position;
            w.WriteValueSafe(id); w.WriteValueSafe(p.x); w.WriteValueSafe(p.y); w.WriteValueSafe(p.z);
            w.WriteValueSafe(a.transform.eulerAngles.y); w.WriteValueSafe(a.PlanarSpeed);
            byte f = 0; if (a.Dancing) f |= 1; if (a.Airborne) f |= 2; w.WriteValueSafe(f);
        }

        void OnPose(ulong sender, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong id); r.ReadValueSafe(out float x); r.ReadValueSafe(out float y); r.ReadValueSafe(out float z);
            r.ReadValueSafe(out float yaw); r.ReadValueSafe(out float speed); r.ReadValueSafe(out byte f);
            if (IsHost) id = sender;   // never trust the claimed id
            if (id == LocalId) return;
            lobby.OnPose(id, new Vector3(x, y, z), yaw, speed, (f & 1) != 0, (f & 2) != 0);
            if (IsHost)
            {
                using var w = new FastBufferWriter(48, Allocator.Temp);
                w.WriteValueSafe(id); w.WriteValueSafe(x); w.WriteValueSafe(y); w.WriteValueSafe(z); w.WriteValueSafe(yaw); w.WriteValueSafe(speed); w.WriteValueSafe(f);
                SendAll("mcl/pose", w, NetworkDelivery.UnreliableSequenced, sender);
            }
        }

        // ------------------------------------------------------------ beach ball (host physics, clients ask to kick)
        void SendBall()
        {
            var b = lobby.Ball; if (!b) return;
            using var w = new FastBufferWriter(64, Allocator.Temp);
            var p = b.position; var q = b.rotation; var v = b.linearVelocity;
            w.WriteValueSafe(p.x); w.WriteValueSafe(p.y); w.WriteValueSafe(p.z);
            w.WriteValueSafe(q.x); w.WriteValueSafe(q.y); w.WriteValueSafe(q.z); w.WriteValueSafe(q.w);
            w.WriteValueSafe(v.x); w.WriteValueSafe(v.y); w.WriteValueSafe(v.z);
            w.WriteValueSafe((byte)lobby.GoalsPink); w.WriteValueSafe((byte)lobby.GoalsTeal);
            SendAll("mcl/ball", w, NetworkDelivery.UnreliableSequenced, ulong.MaxValue);
        }

        void OnBall(ulong sender, FastBufferReader r)
        {
            if (IsHost) return;
            r.ReadValueSafe(out float px); r.ReadValueSafe(out float py); r.ReadValueSafe(out float pz);
            r.ReadValueSafe(out float qx); r.ReadValueSafe(out float qy); r.ReadValueSafe(out float qz); r.ReadValueSafe(out float qw);
            r.ReadValueSafe(out float vx); r.ReadValueSafe(out float vy); r.ReadValueSafe(out float vz);
            r.ReadValueSafe(out byte gp); r.ReadValueSafe(out byte gt);
            lobby.OnBallState(new Vector3(px, py, pz), new Quaternion(qx, qy, qz, qw), new Vector3(vx, vy, vz), gp, gt);
        }

        /// Local player bumped the ball.
        public void Kick(Vector3 impulse)
        {
            if (IsHost) { lobby.ApplyKick(impulse); return; }
            using var w = new FastBufferWriter(16, Allocator.Temp);
            w.WriteValueSafe(impulse.x); w.WriteValueSafe(impulse.y); w.WriteValueSafe(impulse.z);
            nm.CustomMessagingManager.SendNamedMessage("mcl/kick", NetworkManager.ServerClientId, w, NetworkDelivery.Unreliable);
            lobby.PredictKick(impulse);
        }

        void OnKick(ulong sender, FastBufferReader r)
        {
            if (!IsHost) return;
            r.ReadValueSafe(out float x); r.ReadValueSafe(out float y); r.ReadValueSafe(out float z);
            lobby.ApplyKick(Vector3.ClampMagnitude(new Vector3(x, y, z), 9f));
        }

        // ------------------------------------------------------------ start
        public void StartGame()
        {
            if (!IsHost || Starting) return;
            Starting = true;
            if (Online)
            {
                using var w = new FastBufferWriter(4, Allocator.Temp);
                w.WriteValueSafe((byte)Level);
                SendAll("mcl/go", w, NetworkDelivery.ReliableSequenced, ulong.MaxValue);
            }
            lobby.BeginCountdown(Level);
        }

        void OnGo(ulong sender, FastBufferReader r)
        {
            if (IsHost) return;
            r.ReadValueSafe(out byte lvl);
            Starting = true; Level = lvl;
            lobby.BeginCountdown(lvl);
        }

        // ------------------------------------------------------------ helpers
        void SendAll(string name, FastBufferWriter w, NetworkDelivery d, ulong except)
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId && id != except) nm.CustomMessagingManager.SendNamedMessage(name, id, w, d);
        }
        static void WriteLook(FastBufferWriter w, Cosmetic c) { w.WriteValueSafe(c.fruit); w.WriteValueSafe(c.hat); w.WriteValueSafe(c.face); w.WriteValueSafe(c.color); }
        static Cosmetic ReadLook(ref FastBufferReader r)
        {
            r.ReadValueSafe(out byte a); r.ReadValueSafe(out byte b); r.ReadValueSafe(out byte c); r.ReadValueSafe(out byte d);
            return new Cosmetic { fruit = (byte)(a % MenuFlow.FruitEn.Length), hat = (byte)(b % MenuFlow.HatEn.Length), face = (byte)(c % MenuFlow.FaceEn.Length), color = (byte)(d % MenuFlow.Colors.Length) };
        }
    }
}
