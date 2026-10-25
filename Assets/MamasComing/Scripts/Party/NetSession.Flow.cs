using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MamasComing
{
    public partial class NetSession
    {
        const byte FlowLoad = 1, FlowPrepared = 2, FlowSnapshot = 3, FlowRelease = 4, FlowResult = 5, FlowVote = 6, FlowVotes = 7, FlowProgress = 8, FlowError = 9;
        public bool Loading { get; private set; }
        public string LoadStatus { get; private set; } = "";
        /// Loading hit a real problem (e.g. different builds) - the loading screen then offers the way back.
        public bool HasFlowError => !string.IsNullOrEmpty(flowError);
        public int ReadyCount => State == Mode.Client ? receivedReadyCount : readyPeers.Count;
        int receivedReadyCount; float nextProgress;
        int transitionEpoch;
        string destination, flowError;
        bool localSceneReady, localOnly, releaseSent;
        float flowStarted, nextSnapshot;
        Coroutine transitionRoutine;
        readonly HashSet<ulong> readyPeers = new HashSet<ulong>(), snapshotPeers = new HashSet<ulong>();
        public int SlotFor(PlayerController p) { foreach (var a in avatars) if (a.Value == p) return a.Key; return mySlot; }
        public int FruitFor(int slot) => slot == mySlot ? KidLook.Selected : KidOfSlot(slot, 0);
        public void PublishPick() { if (nm && nm.IsListening && (State == Mode.Host || State == Mode.Client && rosterReady)) SendPick(); }
        public void LoadLocal(string scene)
        {
            if (Loading || !MamaLaunch.InBuild(scene)) return;
            destination = scene; localOnly = true; BeginLoading();
        }
        void TransitionAll(string scene)
        {
            if (!NetGame.IsAuthority || Loading || !MamaLaunch.InBuild(scene)) return;
            if (scene == MamaLaunch.GameScene && NetGame.PlayerCount < 2 && !MamaShowcase.Enabled) return;
            destination = scene; transitionEpoch++; localOnly = !NetGame.Online;
            if (State == Mode.Host) SendTransition(null);
            BeginLoading();
        }
        void SendTransition(ulong? target)
        {
            var w = Begin(); w.Write(FlowLoad); w.Write(transitionEpoch); w.Write(destination ?? MamaLaunch.LobbyScene);
            for (int s = 0; s < 3; s++) w.Write((byte)FruitFor(s));
            w.Write(JsonUtility.ToJson(MamaRound.Data));
            if (target.HasValue) Send("mc/flow", target.Value, w, NetworkDelivery.ReliableSequenced);
            else SendAll("mc/flow", w, NetworkDelivery.ReliableSequenced);
        }
        void BeginLoading()
        {
            Loading = true; localSceneReady = false; releaseSent = false; flowError = null;
            readyPeers.Clear(); snapshotPeers.Clear(); receivedReadyCount=0; nextProgress=0; flowStarted = Time.unscaledTime; nextSnapshot = 0;
            LoadStatus = Txt("Loading the house...", "กำลังเตรียมฉาก...");
            MamaLoadingScreen.Open();
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(LoadRoutine());
        }
        IEnumerator LoadRoutine()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            MamaLaunch.FromMenu = destination == MamaLaunch.GameScene;
            if (MamaLaunch.InBuild(MamaLaunch.LoadingScene) && SceneManager.GetActiveScene().name != MamaLaunch.LoadingScene)
                yield return SceneManager.LoadSceneAsync(MamaLaunch.LoadingScene);
            var op = SceneManager.LoadSceneAsync(destination);
            while (!op.isDone) { MamaLoadingScreen.Progress = Mathf.Clamp01(op.progress / .9f); yield return null; }
            // Allow Awake/Start, shader uploads and one rendered frame before declaring scene readiness.
            yield return null; yield return null; yield return new WaitForEndOfFrame();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var args=Environment.GetCommandLineArgs();int delay=Array.IndexOf(args,"-mamaload-delay");
            if(delay>=0 && delay+1<args.Length && float.TryParse(args[delay+1],out float seconds)) yield return new WaitForSecondsRealtime(Mathf.Clamp(seconds,0,15));
#endif
            BindScene(); localSceneReady = true; MamaLoadingScreen.Progress = 1;
            if (localKid) { KidLook.Apply(localKid, KidLook.Selected); localKid.controlsEnabled = false; }
            if (localOnly) { ReleaseLoading(); yield break; }
            if (State == Mode.Host) { readyPeers.Add(nm.LocalClientId); snapshotPeers.Add(nm.LocalClientId); }
            else if (State == Mode.Client)
            {
                var w = Begin(); w.Write(FlowPrepared); w.Write(transitionEpoch); w.Write(SceneSignature());
                SendHost("mc/flow", w, NetworkDelivery.ReliableSequenced);
            }
            transitionRoutine = null;
        }
        string SceneSignature()
        {
            var b = new StringBuilder("MamaPartyProtocol-2|").Append(SceneManager.GetActiveScene().name);
            foreach (var it in reg) if (it) b.Append('|').Append(PathOf(it.transform)).Append(':').Append(it.GetType().FullName);
            foreach (var c in synced) if (c) b.Append('|').Append(PathOf(c.transform)).Append(':').Append(c.GetType().FullName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "MamaRegistry-" + State + ".txt"), b.ToString().Replace("|", "\n"));
#endif
            using (var sha = System.Security.Cryptography.SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())));
        }
        void TickFlow()
        {
            if (!Loading || localOnly) return;
            if (!string.IsNullOrEmpty(flowError)) { LoadStatus = flowError; return; }
            if (Time.unscaledTime - flowStarted > 60f)
                LoadStatus = Txt("Still waiting. Check your friend's connection, or return to the lobby.", "ยังรอเพื่อนอยู่ ตรวจการเชื่อมต่อ หรือกดกลับล็อบบี้");
            else if (localSceneReady) LoadStatus = Txt("Waiting for every player to finish loading", "รอผู้เล่นทุกคนโหลดฉากและรับข้อมูลครบ");
            if (State != Mode.Host || !localSceneReady || releaseSent || slotOf.Count == 0) return;
            if (destination == MamaLaunch.GameScene && slotOf.Count < 2 && !MamaShowcase.Enabled)
            { AbortLoading(); return; }
            if(Time.unscaledTime>=nextProgress) { nextProgress=Time.unscaledTime+.5f;var progress=Begin();progress.Write(FlowProgress);progress.Write(transitionEpoch);progress.Write((byte)readyPeers.Count);SendAll("mc/flow",progress,NetworkDelivery.ReliableSequenced); }
            foreach (ulong id in slotOf.Keys) if (!readyPeers.Contains(id)) return;
            if (destination != MamaLaunch.GameScene) foreach (ulong id in slotOf.Keys) snapshotPeers.Add(id);
            if (Time.unscaledTime >= nextSnapshot && GameManager.I)
            { nextSnapshot = Time.unscaledTime + .4f; SendWorld(); SendField(); }
            foreach (ulong id in slotOf.Keys) if (!snapshotPeers.Contains(id)) return;
            releaseSent = true;
            var w = Begin(); w.Write(FlowRelease); w.Write(transitionEpoch); SendAll("mc/flow", w, NetworkDelivery.ReliableSequenced);
            ReleaseLoading();
        }
        void SnapshotReceived()
        {
            if (!Loading || !localSceneReady || State != Mode.Client) return;
            var w = Begin(); w.Write(FlowSnapshot); w.Write(transitionEpoch); SendHost("mc/flow", w, NetworkDelivery.ReliableSequenced);
        }
        void ReleaseLoading()
        {
            Loading = false; localSceneReady = true; MamaLoadingScreen.Close();
            if (destination == MamaLaunch.RankingScene && localKid) { localKid.WakeUp(); localKid.controlsEnabled = true; }
            Debug.Log("[MamaFlow] Ready epoch=" + transitionEpoch + " scene=" + destination + " slot=" + mySlot + " fruit=" + KidLook.Selected);
        }
        bool recovering;
        public void AbortLoading() { if (recovering) return; recovering = true; StartCoroutine(RecoverLobby()); }
        IEnumerator RecoverLobby()
        {
            LoadStatus = Txt("Returning to the lobby...", "กำลังกลับล็อบบี้...");
            while (!localSceneReady) yield return null;
            if (State == Mode.Client) Leave();
            Loading = false; recovering = false;
            if (State == Mode.Host) TransitionAll(MamaLaunch.LobbyScene); else LoadLocal(MamaLaunch.LobbyScene);
        }
        void OnFlow(ulong sender, FastBufferReader reader)
        {
            var r = Read(reader); if (r == null) return;
            byte kind = r.ReadByte(); int epoch = r.ReadInt32();
            if (State == Mode.Host)
            {
                if (!slotOf.TryGetValue(sender, out int slot) || epoch != transitionEpoch) return;
                if (kind == FlowPrepared && Loading)
                {
                    string signature = r.ReadString();
                    // The host may still be loading. Verify against its registry after it is ready.
                    StartCoroutine(AcceptPrepared(sender, epoch, signature));
                }
                else if (kind == FlowSnapshot && readyPeers.Contains(sender)) snapshotPeers.Add(sender);
                else if (kind == FlowVote && !Loading && MamaRound.Data.ended) { SetVote(slot, r.ReadByte()); }
                return;
            }
            if (State != Mode.Client || sender != NetworkManager.ServerClientId) return;
            if (kind == FlowLoad)
            {
                if (epoch <= transitionEpoch) return;
                string scene = r.ReadString(); if (!MamaLaunch.InBuild(scene)) return;
                transitionEpoch = epoch; destination = scene; localOnly = false;
                for (int s = 0; s < 3; s++) { int fruit = r.ReadByte(); if (s == mySlot) continue; if (!remote.TryGetValue(s, out var state)) remote[s] = state = new RemoteState(); state.kid = fruit; state.shownKid = -1; }
                MamaRound.Data = JsonUtility.FromJson<MamaRoundData>(r.ReadString()); BeginLoading(); return;
            }
            if (epoch != transitionEpoch) return;
            if (kind == FlowError) { flowError = r.ReadString(); LoadStatus = flowError; }
            else if (kind == FlowProgress) receivedReadyCount=r.ReadByte();
            else if (kind == FlowRelease && localSceneReady) ReleaseLoading();
            else if (kind == FlowResult) { MamaRound.Data = JsonUtility.FromJson<MamaRoundData>(r.ReadString()); PresentResults(); }
            else if (kind == FlowVotes) for (int s = 0; s < 3; s++) MamaRound.Data.votes[s] = r.ReadByte();
        }
        IEnumerator AcceptPrepared(ulong sender, int epoch, string signature)
        {
            while (Loading && epoch == transitionEpoch && !localSceneReady) yield return null;
            if (!Loading || epoch != transitionEpoch || !slotOf.ContainsKey(sender)) yield break;
            if (signature != SceneSignature())
            {
                Debug.LogError("[MamaFlow] Scene registry mismatch for player " + sender + ". Use the same build on every computer.");
                flowError = LoadStatus = Txt("Scene data differs. Return to lobby and use the same build.", "ข้อมูลฉากไม่ตรงกัน กลับล็อบบี้แล้วใช้บิลด์ชุดเดียวกัน");
                var error = Begin(); error.Write(FlowError); error.Write(transitionEpoch); error.Write(flowError);
                SendAll("mc/flow", error, NetworkDelivery.ReliableSequenced);
                yield break;
            }
            readyPeers.Add(sender);
        }
        public void PublishResults()
        {
            if (!NetGame.IsAuthority) return;
            if (State == Mode.Host) { var w = Begin(); w.Write(FlowResult); w.Write(transitionEpoch); w.Write(JsonUtility.ToJson(MamaRound.Data)); SendAll("mc/flow", w, NetworkDelivery.ReliableSequenced); }
            PresentResults();
            // owner: always end on the score summary - a loss first shows why (defeat camera) for a few seconds
            StartCoroutine(ShowRankingSoon(MamaRound.Data.won ? .8f : 5.5f));
        }
        IEnumerator ShowRankingSoon(float delay) { int epoch = transitionEpoch; yield return new WaitForSecondsRealtime(delay); if (epoch == transitionEpoch && !Loading) LoadLevelForAll(MamaLaunch.RankingScene); }
        void PresentResults()
        {
            var gm = GameManager.I;
            if (gm) { gm.state = GameManager.State.Ending; gm.player.controlsEnabled = false; gm.ui.HideForResults(); if (gm.cam) gm.cam.inputEnabled = false; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (!MamaRound.Data.won && gm) { if (!gm.GetComponent<MamaDefeatCamera>()) gm.gameObject.AddComponent<MamaDefeatCamera>(); }
        }
        public void Vote(int choice)
        {
            if (!MamaRound.Data.ended || Loading || choice < 1 || choice > 2) return;
            if (State == Mode.Client) { var w = Begin(); w.Write(FlowVote); w.Write(transitionEpoch); w.Write((byte)choice); SendHost("mc/flow", w, NetworkDelivery.ReliableSequenced); }
            else SetVote(mySlot, choice);
        }
        void SetVote(int slot, int choice)
        {
            if (choice < 1 || choice > 2) return;
            MamaRound.Data.votes[slot] = choice;
            if (State == Mode.Host) { var w = Begin(); w.Write(FlowVotes); w.Write(transitionEpoch); foreach (int v in MamaRound.Data.votes) w.Write((byte)v); SendAll("mc/flow", w, NetworkDelivery.ReliableSequenced); }
            ResolveVotes();
        }
        void ResolveVotes()
        {
            if (!NetGame.IsAuthority || !MamaRound.Data.ended || Loading) return;
            var slots = new List<int>(slotOf.Values); if (slots.Count == 0) slots.Add(0);
            int choice = MamaRound.WinningVote(MamaRound.Data.votes, slots);
            if (choice == 0) return;
            if (choice == 2 && (slots.Count >= 2 || MamaShowcase.Enabled)) { MamaRound.Begin(); TransitionAll(MamaLaunch.GameScene); }
            else TransitionAll(MamaLaunch.LobbyScene);
        }
    }
}
