using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace MamasComing
{
    /// Unity Gaming Services: Relay-backed sessions with a short join code (works across the internet, no port forwarding).
    /// Needs the project linked to a Unity Cloud project (Edit > Project Settings > Services).
    public static class NetServices
    {
        static async Task Init()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public static async Task<object> CreateSession(int maxPlayers)
        {
            await Init();
            var opts = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }.WithRelayNetwork();
            return await MultiplayerService.Instance.CreateSessionAsync(opts);
        }

        public static async Task<object> JoinSession(string code)
        {
            await Init();
            return await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        }

        public static string CodeOf(object s) => (s as ISession)?.Code ?? "";

        public static async void Leave(object s)
        {
            try { if (s is ISession session) await session.LeaveAsync(); } catch { }
        }
    }
}
