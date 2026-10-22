using System.Collections;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.Campaign
{
    public sealed class DreamCinematic : MonoBehaviour
    {
        [System.Serializable] public struct Shot {
            public Vector3 from, to, lookAt;
            public float seconds, fovFrom, fovTo;
            public string caption;
            public bool impact;
        }
        public static DreamCinematic Current { get; private set; }
        public static bool Active => Current != null;
        public static bool SkipForGameplayProbe;
        Camera _camera; PlayerMotor3D _player; PlayerUIBridge _bridge;
        ThirdPersonCameraController[] _follow; StageCameraController[] _stage;
        bool[] _followState, _stageState; bool _inputState, _lockState;
        float _fov; Vector3 _position; Quaternion _rotation; GameObject _overlay;
        bool _captured,_hudState;
        public static IEnumerator Play(params Shot[] shots)
        {
            if (Active || SkipForGameplayProbe || shots == null || shots.Length == 0) yield break;
            var host = new GameObject("DreamCinematic");
            var sequence=host.AddComponent<DreamCinematic>();
            yield return sequence.StartCoroutine(sequence.Run(shots));
            if (host != null) Destroy(host);
        }
        IEnumerator Run(Shot[] shots)
        {
            Current=this; _camera=Camera.main; _player=FindFirstObjectByType<PlayerMotor3D>();
            if(_camera==null)yield break;
            _position=_camera.transform.position;_rotation=_camera.transform.rotation;_fov=_camera.fieldOfView;
            _follow=FindObjectsByType<ThirdPersonCameraController>(FindObjectsSortMode.None);_followState=new bool[_follow.Length];
            _stage=FindObjectsByType<StageCameraController>(FindObjectsSortMode.None);_stageState=new bool[_stage.Length];
            for(int i=0;i<_follow.Length;i++){_followState[i]=_follow[i].enabled;_follow[i].enabled=false;}
            for(int i=0;i<_stage.Length;i++){_stageState[i]=_stage[i].enabled;_stage[i].enabled=false;}
            if(_player!=null){_inputState=_player.InputEnabled;_player.InputEnabled=false;_bridge=_player.GetComponent<PlayerUIBridge>();}
            if(_bridge!=null){_lockState=_bridge.CutsceneLock;_bridge.CutsceneLock=true;}
            _captured=true;_hudState=UIRoot.Get().Hud.gameObject.activeSelf;UIRoot.Get().Hud.gameObject.SetActive(false);
            var layer=UIFactory.Stretch(UIFactory.Rect("CinematicBars",UIRoot.Get().Overlay));_overlay=layer.gameObject;
            var top=UIFactory.Panel("Top",layer,new Color(.015f,.02f,.04f,.98f));UIFactory.Stretch(top.rectTransform);
            top.rectTransform.anchorMin=new Vector2(0,.89f);
            var bottom=UIFactory.Panel("Bottom",layer,new Color(.015f,.02f,.04f,.98f));UIFactory.Stretch(bottom.rectTransform);
            bottom.rectTransform.anchorMax=new Vector2(1,.13f);
            var caption=UIFactory.Label("Caption",bottom.transform,"",31,new Color(.97f,.93f,.84f));UIFactory.Stretch(caption.rectTransform,18);
            var skip=UIFactory.Label("Skip",top.transform,"ENTER / B  ·  ข้ามคัตซีน",21,new Color(.75f,.82f,.88f),TextAnchor.MiddleRight);UIFactory.Stretch(skip.rectTransform,20);
            foreach(var shot in shots)
            {
                caption.text=shot.caption ?? "";
                bool impactPlayed=false;
                float duration=Mathf.Max(.05f,shot.seconds);
                for(float t=0;t<duration;)
                {
                    if(UIRoot.Get().AnyPanelOpen){yield return null;continue;}
                    if(SkipPressed()) { Restore(); yield break; }
                    float p=Mathf.Clamp01(t/duration),e=p*p*(3-2*p);
                    _camera.transform.position=Vector3.Lerp(shot.from,shot.to,e);
                    _camera.transform.rotation=Quaternion.LookRotation(shot.lookAt-_camera.transform.position);
                    _camera.fieldOfView=Mathf.Lerp(shot.fovFrom>0?shot.fovFrom:55,shot.fovTo>0?shot.fovTo:45,e);
                    if(shot.impact&&!impactPlayed&&p>.25f)
                    {
                        impactPlayed=true;Audio.AudioManager.Play(Audio.Sfx.HitHeavy,shot.lookAt,.7f);
                        var settings=SaveManager.GetOrCreate().Data.settings;
                        if(!settings.reduceFlashing)Presentation.VFX.Play(Presentation.VFXKind.ImpactRing,shot.lookAt,2.4f);
                        if(!settings.reduceScreenShake)Presentation.CameraShake.Add(.25f);
                    }
                    t+=Time.unscaledDeltaTime;yield return null;
                }
            }
            Restore();
        }
        static bool SkipPressed() => (Keyboard.current!=null&&Keyboard.current.enterKey.wasPressedThisFrame)
            || (Gamepad.current!=null&&Gamepad.current.buttonEast.wasPressedThisFrame);
        void Restore()
        {
            if(_overlay!=null)Destroy(_overlay);
            if(_captured)
            {
                if(UIRoot.Exists)UIRoot.Get().Hud.gameObject.SetActive(_hudState);
                if(_camera!=null){_camera.fieldOfView=_fov;_camera.transform.SetPositionAndRotation(_position,_rotation);}
                if(_player!=null)_player.InputEnabled=_inputState;
                if(_bridge!=null)_bridge.CutsceneLock=_lockState;
                for(int i=0;i<_follow.Length;i++)if(_follow[i]!=null){_follow[i].enabled=_followState[i];if(_followState[i])_follow[i].SnapToTarget();}
                for(int i=0;i<_stage.Length;i++)if(_stage[i]!=null)_stage[i].enabled=_stageState[i];
            }
            _captured=false;if(Current==this)Current=null;
        }
        void OnDisable()=>Restore();
        void OnDestroy()=>Restore();
    }
}
