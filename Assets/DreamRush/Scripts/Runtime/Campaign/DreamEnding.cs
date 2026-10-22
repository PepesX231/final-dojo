using System.Collections;
using DreamRush.Core;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.Campaign
{
    /// <summary>Original DREAM RUSH ending text. Presentation reference only; no Minecraft poem/music is reproduced.</summary>
    public sealed class DreamEnding : MonoBehaviour
    {
        public static bool Showing {get;private set;}
        public static bool SkipForGameplayProbe;
        public static int CompletedThisSession {get;private set;}
        public static readonly string Poem =
            "คืนนี้ เธอวิ่งมาไกลแล้ว\n\nข้ามหน้ากระดาษที่ไม่เคยอยู่กับที่\nผ่านเมืองซึ่งนาฬิกาลืมวิธีพัก\nยืมแสงเล็ก ๆ จากดวงจันทร์\nแล้วพาเรือกระดาษกลับบ้าน\n\nแต่ความฝันไม่ได้ถามว่าเธอเร็วแค่ไหน\nมันถามว่า ระหว่างทาง\nเธอยังมองเห็นอะไรอยู่บ้าง\n\nแสงในหน้าต่างที่ไม่มีใครยืนดู\nเสียงระฆังหลังเสียงสุดท้าย\nเก้าอี้ตัวหนึ่งที่อยากมีเพื่อนนั่ง\nและมือเล็ก ๆ ของเธอ\nที่ยังสร้างทางขึ้นมาได้เสมอ\n\nไม่มีถ้วยรางวัลสำหรับการตื่น\nไม่มีคะแนนให้เช้าวันใหม่\nมีเพียงพื้นที่ว่าง\nที่รอให้เธอใส่อะไรบางอย่างลงไป\n\nวางหมัดลงได้แล้ว Pip\nคืนนี้ เธอไม่ต้องชนะอะไรอีก\n\nดื่มชาให้หมด\nเก็บแสงไว้สักดวง\nแล้วค่อย ๆ ลืมตา\n\nโลกข้างนอกยังมีที่ให้เธอ\nและความฝันจะเก็บที่นั่งนี้ไว้\nจนกว่าเราจะพบกันอีกครั้ง";
        GameObject _root;PlayerUIBridge _bridge;PlayerMotor3D _player;
        bool _bridgeEnabled,_lock,_input;bool _cleaned,_hudState,_cameraCaptured,_styleState;
        Camera _camera;int _mask;CameraClearFlags _clear;Color _background;Look.StylePack _style;
        public static IEnumerator Play()
        {
            if(Showing)yield break;
            if(SkipForGameplayProbe){CompletedThisSession++;yield break;}
            var host=new GameObject("DreamEnding");var e=host.AddComponent<DreamEnding>();
            yield return e.StartCoroutine(e.Run());if(host!=null)Destroy(host);
        }
        IEnumerator Run()
        {
            Showing=true;_hudState=UIRoot.Get().Hud.gameObject.activeSelf;UIRoot.Get().Hud.gameObject.SetActive(false);
            _camera=Camera.main;if(_camera!=null){_mask=_camera.cullingMask;_clear=_camera.clearFlags;_background=_camera.backgroundColor;_camera.cullingMask=1<<5;_camera.clearFlags=CameraClearFlags.SolidColor;_camera.backgroundColor=new Color(.018f,.025f,.058f);_cameraCaptured=true;}
            _style=Look.StylePack.Current;if(_style!=null){_styleState=_style.enabled;_style.enabled=false;}
            _player=FindFirstObjectByType<PlayerMotor3D>();
            if(_player!=null){_input=_player.InputEnabled;_player.InputEnabled=false;_bridge=_player.GetComponent<PlayerUIBridge>();}
            if(_bridge!=null){_bridgeEnabled=_bridge.enabled;_lock=_bridge.CutsceneLock;_bridge.CutsceneLock=true;_bridge.enabled=false;}
            Audio.AudioManager.PlayMusic(Audio.Music.Credits,2.5f);
            var rt=UIFactory.Stretch(UIFactory.Rect("DreamEnding",UIRoot.Get().Overlay));_root=rt.gameObject;
            var back=UIFactory.Panel("Night",rt,new Color(.018f,.025f,.058f,1));UIFactory.Stretch(back.rectTransform);
            var rng=new System.Random(51);
            for(int i=0;i<65;i++){
                var star=UIFactory.Panel("Star",rt,new Color(.65f,.78f,.88f,(float)rng.NextDouble()*.55f+.12f));
                star.rectTransform.anchorMin=star.rectTransform.anchorMax=new Vector2((float)rng.NextDouble(),(float)rng.NextDouble());
                star.rectTransform.sizeDelta=Vector2.one*(float)(1.5+rng.NextDouble()*3);star.raycastTarget=false;
            }
            var viewport=UIFactory.Stretch(UIFactory.Rect("Viewport",rt));viewport.offsetMin=new Vector2(90,110);viewport.offsetMax=new Vector2(-90,-100);
            viewport.gameObject.AddComponent<RectMask2D>();
            string credits="\n\n\nDREAM RUSH\nTHE APPLE DREAM\n\nA DREAM IN FIVE PAGES\n\nTHE DREAM BOOK\nMIDNIGHT PALACE\nTOY BOX MOON\nBUBBLE BATH SEA\nMIDNIGHT TEACUP\n\n\nโลกและตัวละคร\nDREAM RUSH PROJECT\n\nเพลงและเสียง\nOriginal DREAM RUSH arrangements\nGeneralUser GS SoundFont — S. Christian Collins\nOriginal compositions · licensed instruments\n\nการทดสอบและการเดินทาง\nถึงทุกคนที่ยอมหลงทางอีกครั้ง\n\n\nขอบคุณที่เล่น\n\nSWEET DREAMS, PIP.\n\n\nความฝันทั้งห้ายังรอให้กลับมา\nเส้นทางอื่น ดาวที่ยังไม่พบ\nและเวลาที่เป็นของเธอเอง";
            var text=UIFactory.Label("PoemAndCredits",viewport,Poem+credits,36,new Color(.87f,.9f,.86f),TextAnchor.UpperCenter);
            text.lineSpacing=1.45f;var tr=text.rectTransform;tr.anchorMin=new Vector2(0,0);tr.anchorMax=new Vector2(1,0);tr.pivot=new Vector2(.5f,1);
            Canvas.ForceUpdateCanvases();float scrollHeight=Mathf.Max(1000,text.preferredHeight+100);
            tr.sizeDelta=new Vector2(0,scrollHeight);tr.anchoredPosition=new Vector2(0,-40);
            var title=UIFactory.Label("Heading",rt,"DREAM RUSH  ·  END OF THE NIGHT",25,new Color(.56f,.79f,.8f));
            title.rectTransform.anchorMin=new Vector2(0,.91f);title.rectTransform.anchorMax=Vector2.one;title.rectTransform.offsetMin=title.rectTransform.offsetMax=Vector2.zero;
            var hint=UIFactory.Label("Hint",rt,"SPACE / A  เร่งข้อความ      ENTER / B  กลับบ้าน",22,new Color(.6f,.68f,.77f));
            hint.rectTransform.anchorMin=Vector2.zero;hint.rectTransform.anchorMax=new Vector2(1,.07f);hint.rectTransform.offsetMin=hint.rectTransform.offsetMax=Vector2.zero;
            float elapsed=0,scroll=0;
            while(scroll<scrollHeight+viewport.rect.height){
                elapsed+=Time.unscaledDeltaTime;
                bool fast=(Keyboard.current!=null&&Keyboard.current.spaceKey.isPressed)||(Gamepad.current!=null&&Gamepad.current.buttonSouth.isPressed);
                bool exit=(Keyboard.current!=null&&Keyboard.current.enterKey.isPressed)||(Gamepad.current!=null&&Gamepad.current.buttonEast.isPressed);
                if(elapsed>3f&&exit)break;
                scroll+=Time.unscaledDeltaTime*(fast?220f:48f);tr.anchoredPosition=new Vector2(0,scroll-40);yield return null;
            }
            CompletedThisSession++;Cleanup();
        }
        void Cleanup(){
            if(_cleaned)return;_cleaned=true;
            if(UIRoot.Exists)UIRoot.Get().Hud.gameObject.SetActive(_hudState);
            if(_cameraCaptured&&_camera!=null){_camera.cullingMask=_mask;_camera.clearFlags=_clear;_camera.backgroundColor=_background;}
            if(_style!=null)_style.enabled=_styleState;
            if(_root!=null)Destroy(_root);if(_player!=null)_player.InputEnabled=_input;
            if(_bridge!=null){_bridge.CutsceneLock=_lock;_bridge.enabled=_bridgeEnabled;}
            Showing=false;
        }
        void OnDisable()=>Cleanup();void OnDestroy()=>Cleanup();
    }
}
