using System.Collections;
using DreamRush.Player;
using UnityEngine;
namespace DreamRush.Campaign
{
    public sealed class ExistingWorldArrival : MonoBehaviour
    {
        public Transform focus;
        public string title="MIDNIGHT PALACE",caption="สามชั่วโมงที่หายไป กำลังรออยู่ในเมือง";
        IEnumerator Start(){
            yield return new WaitForSeconds(.6f);
            if(DreamCinematic.SkipForGameplayProbe)yield break;
            var player=FindFirstObjectByType<PlayerMotor3D>();if(player==null)yield break;
            var p=focus!=null?focus.position:player.transform.position+Vector3.forward*20+Vector3.up*8;
            if(focus!=null){var renderers=focus.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);p=bounds.center;}}
            var hero=player.transform.position;
            yield return DreamCinematic.Play(
                new DreamCinematic.Shot{from=p+new Vector3(-35,18,-35),to=p+new Vector3(-25,10,-25),lookAt=p,seconds=2.8f,fovFrom=64,fovTo=46,caption=title},
                new DreamCinematic.Shot{from=hero+player.transform.forward*7+Vector3.up*2,to=hero+player.transform.forward*4+player.transform.right*2+Vector3.up*1.5f,lookAt=hero+Vector3.up*.9f,seconds=1.8f,fovFrom=54,fovTo=42,caption=caption});
        }
    }
}
