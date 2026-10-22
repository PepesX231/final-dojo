using System.Collections;
using DreamRush.Player;
using DreamRush.World1;
using UnityEngine;
namespace DreamRush.Campaign
{
    public static class CampaignChapterCinema
    {
        public static IEnumerator Enter(ChapterInfo chapter,int index)
        {
            if(chapter==null||chapter.spawn==null||DreamCinematic.SkipForGameplayProbe)yield break;
            var p=chapter.spawn.position;
            Vector3 target=p+(chapter.sideRail?new Vector3(12,1,0):new Vector3(0,2,10));
            yield return DreamCinematic.Play(new DreamCinematic.Shot{
                from=p+(chapter.sideRail?new Vector3(-5,5,-14):new Vector3(-9,7,-7)),
                to=p+(chapter.sideRail?new Vector3(2,3,-11):new Vector3(-5,3,-4)),lookAt=target,
                seconds=index==4?2.5f:1.8f,fovFrom=60,fovTo=44,
                caption="CHAPTER "+(index+1)+"  ·  "+chapter.title,impact=index==4});
        }
        public static IEnumerator Victory(string scene)
        {
            if(DreamCinematic.SkipForGameplayProbe)yield break;
            var player=Object.FindFirstObjectByType<PlayerMotor3D>();if(player==null)yield break;
            var p=player.transform.position;var front=player.transform.forward;var side=player.transform.right;
            Audio.AudioManager.PlayMusic(scene=="Dream_World1"?Audio.Music.World1:Audio.Music.Palace,1f);
            yield return DreamCinematic.Play(new DreamCinematic.Shot{
                from=p+front*7+side*3+Vector3.up*3,to=p+front*4+side*2+Vector3.up*1.8f,
                lookAt=p+Vector3.up*.8f,seconds=2.5f,fovFrom=54,fovTo=36,
                caption=scene=="Dream_World1"?"A CHAIR. A FRIEND. A NEW DREAM.":"TIME CAN MOVE AGAIN.",impact=true});
        }
    }
}
