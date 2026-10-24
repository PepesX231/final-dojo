using UnityEngine;

namespace MamasComing
{
    /// Facade between gameplay and networking. Solo play: everything runs locally.
    /// Online (see NetSession): the host owns the world simulation; clients send their actions to the host.
    public static partial class NetGame
    {
        public enum Msg { Call = 0, Toast = 1, Sound = 2, KidSay = 3, Start = 4, Restart = 5, MomSay = 6, Ending = 7, LoadLevel = 8 }
        public static PlayerController Acting; public static ulong ActingClient;
        public static System.Action<string, Vector3?, float, float> SoundHook;

        /// Host running a remote player's action: their thought bubble goes to their screen.
        public static bool ActingRemote(string s)
        {
            if (!Online || !IsHost || Acting == null || Acting.isLocal || NetSession.I == null) return false;
            NetSession.I.SendKidSay(ActingClient, s); return true;
        }
        public static void BroadcastEnding(string title, string body, string stats, Color c) { if (Online && IsHost && NetSession.I) NetSession.I.SendEnding(title, body, stats, c); }

        public static bool Online;
        public static bool IsHost = true;
        public static bool IsAuthority => !Online || IsHost;
        public static bool IsClientOnly => Online && !IsHost;
        public static int Connected = 1;
        public static int PlayerCount => Online ? Mathf.Max(1, Connected) : 1;

        public static System.Action<Interactable, PlayerController> SendInteract;
        public static System.Action<Msg, string, string, int> SendBroadcast;
        public static System.Func<PlayerController, string> GetName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Online = false; IsHost = true; Connected = 1; }

        /// Run an interaction: directly when we own the world, otherwise ask the host to do it for this player.
        public static void Interact(Interactable t, PlayerController p)
        {
            if (!t) return;
            if (!HeldInteraction.Allows(p, t)) return;
            if (IsAuthority || SendInteract == null) { bool problem=t is IMess before && before.IsProblem; t.Interact(p); if(problem && t is IMess after && !after.IsProblem) MamaRound.Award(p,t,t.displayName,100); }
            else SendInteract(t, p);
        }

        public static void Broadcast(Msg m, string en, string th, int level)
        {
            if (Online && IsHost && SendBroadcast != null) SendBroadcast(m, en, th, level);
        }

        public static string NameOf(PlayerController p) => Online && GetName != null ? GetName(p) : null;
    }
}
