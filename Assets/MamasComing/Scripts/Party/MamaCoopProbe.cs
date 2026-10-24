#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using DreamRush.Player;
namespace MamasComing
{
    /// Opt-in two-process acceptance probe. No PlayerPrefs, automatic joins, or test input in release builds.
    public class MamaCoopProbe : MonoBehaviour
    {
        bool host; int fruit; int passed; string role;
        void Check(bool good,string message){if(!good)throw new Exception(message);passed++;Debug.Log("[PartyQA "+role+"] PASS "+message);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();host=Array.IndexOf(args,"-mamahost")>=0;role=host?"HOST":"CLIENT";fruit=host?1:3;
            KidLook.SelectForSession(fruit);Application.runInBackground=true;Application.targetFrameRate=45;
            var steps=Checks();
            while(true)
            {
                object next=null;bool more=false;string failure=null;
                try{more=steps.MoveNext();if(more)next=steps.Current;}catch(Exception e){failure=e.ToString();}
                if(failure!=null){Debug.LogError("[PartyQA "+role+"] FAILED "+failure);yield break;}
                if(!more)break;yield return next;
            }
            Debug.Log("[PartyQA "+role+"] COMPLETE "+passed+" assertions");
        }
        IEnumerator Wait(Func<bool> condition,float seconds=50)
        {float until=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<until)yield return null;}
        bool Playing()=>GameManager.I&&GameManager.I.state==GameManager.State.Playing&&!NetSession.I.Loading;
        void Fruits(string phase)
        {
            var n=NetSession.I;Check(n.AvatarAt(n.MySlot)&&KidLook.Of(n.AvatarAt(n.MySlot))==fruit,phase+" keeps own fruit");
            int other=host?1:0,expected=host?3:1;var pc=n.AvatarAt(other);
            Check(pc&&KidLook.Of(pc)==expected,phase+" sees other player's correct fruit");
            Check(pc.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),phase+" remote model has visible renderers");
        }
        IEnumerator Checks()
        {
            var n=NetSession.I;
            yield return Wait(()=>NetGame.PlayerCount==2&&n.State!=(NetSession.Mode.Connecting));
            Check(NetGame.PlayerCount==2,"two peers connected");
            n.PublishPick();yield return new WaitForSecondsRealtime(2);
            Check(n.FruitFor(host?1:0)==(host?3:1),"lobby fruit identity replicated");
            if(host){Check(MamaLaunch.CanStart,"two players can start");MamaLaunch.ToGame();}
            yield return Wait(Playing);Check(Playing(),"loading barrier releases into gameplay");
            yield return new WaitForSecondsRealtime(1);Fruits("First round");
            var gm=GameManager.I;var p=gm.player;
            // Distinct props: this checks ownership rather than just whether an RPC arrived.
            var props=UnityEngine.Object.FindObjectsByType<MessyProp>(FindObjectsSortMode.None).Where(x=>x.displayName=="Party cup").OrderBy(x=>x.transform.position.x).ThenBy(x=>x.transform.position.z).ToArray();
            Check(props.Length>=2,"test scene contains two independent props");
            var mine=props[host?0:1];p.Motor.Teleport(mine.transform.position+Vector3.back*.35f,Quaternion.identity);
            yield return new WaitForSecondsRealtime(.5f);NetGame.Interact(mine,p);
            yield return new WaitForSecondsRealtime(1.5f);
            Check(p.Held==mine,"pickup is owned by the requesting player");
            var peer=n.AvatarAt(host?1:0);Check(peer&&peer.Held&&peer.Held!=p.Held,"other player holds a different item");
            p.Drop(false);yield return new WaitForSecondsRealtime(.5f);
            if(host)gm.PresentResult(false);
            yield return Wait(()=>MamaRound.Data.ended,12);Check(MamaRound.Data.ended,"defeat reaches both peers");
            yield return new WaitForSecondsRealtime(host?1:3);n.Vote(2);
            if(host){yield return new WaitForSecondsRealtime(.7f);Check(!n.Loading,"one vote cannot retry a two-player round");}
            yield return Wait(()=>n.Loading,15);Check(n.Loading,"retry displays loading");
            yield return Wait(Playing);Check(Playing(),"retry waits for both scenes and snapshots");
            yield return new WaitForSecondsRealtime(1);Fruits("Retry");
            if(host)GameManager.I.PresentResult(true);
            yield return Wait(()=>SceneManager.GetActiveScene().name==MamaLaunch.RankingScene&&!n.Loading);Check(SceneManager.GetActiveScene().name==MamaLaunch.RankingScene&&!n.Loading,"shared victory ranking loads");
            yield return new WaitForSecondsRealtime(1);Fruits("Ranking");
            p=MamaRankingScreen.LocalPlayer;Check(p&&p.controlsEnabled,"ranking player can move");
            p.Motor.Teleport(MamaRankingScreen.SpawnFor(n.MySlot)+Vector3.forward*.65f,Quaternion.identity);
            var from=p.transform.position;p.Motor.AutoStep=false;
            for(int i=0;i<30;i++){p.Motor.Step(1f/45,new PlayerCommand{Move=Vector2.up});yield return null;}
            p.Motor.AutoStep=true;Check(Vector3.Distance(from,p.transform.position)>.3f,"ranking movement changes world position");
            if(host){yield return new WaitForSecondsRealtime(1);n.Vote(1);}else{yield return new WaitForSecondsRealtime(2);n.Vote(1);}
            yield return Wait(()=>SceneManager.GetActiveScene().name==MamaLaunch.LobbyScene&&!n.Loading);Check(SceneManager.GetActiveScene().name==MamaLaunch.LobbyScene&&!n.Loading,"vote returns everyone to lobby");
            Check(KidLook.Selected==fruit,"lobby keeps selection after complete round trip");
            Check(NetGame.PlayerCount==2,"room connection survives all transitions");
        }
    }
}
#endif
