using System.Collections;
using UnityEngine;
namespace MamasComing
{
    public class MamaDefeatCamera : MonoBehaviour
    {
        IEnumerator Start()
        {
            var gm=GameManager.I; var cam=Camera.main;
            if(gm && gm.cam) gm.cam.enabled=false;
            var canvas=MamaFlowUI.Canvas("What went wrong",transform,170);
            var top=MamaFlowUI.Panel(canvas.transform,"Failure heading",new Vector2(0,425),new Vector2(950,105),MamaTheme.PlankDarkS,Color.white);
            MamaFlowUI.Text(top,"Title",Vector2.zero,new Vector2(870,85),MamaFlowUI.T("MOM FOUND THE EVIDENCE!", "แม่เจอหลักฐานแล้ว!"),43,MamaTheme.Cream,TextAnchor.MiddleCenter);
            var note=MamaFlowUI.Panel(canvas.transform,"Cause",new Vector2(0,-260),new Vector2(1080,110),MamaTheme.PaperS,Color.white);
            MamaFlowUI.Text(note,"Explanation",Vector2.zero,new Vector2(1000,92),MamaRound.Data.reason,29,null,TextAnchor.MiddleCenter);
            var votes=MamaFlowUI.VotePanel(canvas.transform,new Vector2(0,-430)); votes.gameObject.SetActive(false);
            if(cam)
            {
                Vector3 focus=MamaRound.Data.cause+Vector3.up*.45f;
                Vector3 from=cam.transform.position, to=focus+new Vector3(-2.6f,4.6f,-3.2f); Quaternion rot=cam.transform.rotation;
                for(float t=0;t<2.6f;t+=Time.unscaledDeltaTime)
                { float u=Mathf.SmoothStep(0,1,t/2.6f); cam.transform.SetPositionAndRotation(Vector3.Lerp(from,to,u),Quaternion.Slerp(rot,Quaternion.LookRotation(focus-to),u)); yield return null; }
            }
            votes.gameObject.SetActive(true);
        }
    }
}
