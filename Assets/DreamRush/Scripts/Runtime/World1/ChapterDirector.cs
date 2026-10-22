using System;
using System.Collections;
using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>One chapter of World 1: where it starts, how it looks, how the camera behaves.</summary>
    [Serializable]
    public class ChapterInfo
    {
        public string title = "CHAPTER";
        public string tag = "";
        public Transform spawn;
        public GameObject lookRoot;               // lights / volume / sky props enabled only in this chapter
        public Material skybox;
        public Color background = new Color(0.2f, 0.15f, 0.3f);
        public bool fog = true;
        public Color fogColor = new Color(0.35f, 0.25f, 0.45f);
        public float fogStart = 30f, fogEnd = 140f;
        public Color ambientSky = Color.white, ambientEquator = Color.gray, ambientGround = Color.black;
        public bool sideRail;                      // Ch2 Paper Theater: locked side view (2.5D)
        public float railDistance = 15f, railHeight = 3.2f, railFov = 32f, railPitch = 8f;
        public Vector3 bounds = new Vector3(400, 400, 400);
        [Header("Comic style (World 1 = Densha anime; a = 0 keeps the StylePack's own value)")]
        public Color halftoneColor = new Color(0, 0, 0, 0);
        public Color shadowTint = new Color(0, 0, 0, 0);
        public Color speedColor = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// World 1 conductor (docs/LEVEL1_DESIGN.md): every chapter is drawn in a different style, so entering one
    /// swaps sky, fog, ambient, the chapter's light rig/post volume and (Ch2) the camera to a side rail.
    /// Page-portals call <see cref="GoTo"/>; a title card announces each chapter.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ChapterDirector : MonoBehaviour
    {
        public static ChapterDirector Current { get; private set; }
        public List<ChapterInfo> chapters = new List<ChapterInfo>();
        public int startChapter;
        public int Index { get; private set; } = -1;
        public ChapterInfo Chapter => Index >= 0 && Index < chapters.Count ? chapters[Index] : null;
        public event Action<int> ChapterChanged;

        PlayerMotor3D _player;
        ThirdPersonCameraController _cam;
        Transform _railPose;
        float _defaultFov;
        bool _busy;

        void Awake() { Current = this; DreamStar.ResetRun(); }
        void OnDestroy() { if (Current == this) Current = null; }

        IEnumerator Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _cam = FindFirstObjectByType<ThirdPersonCameraController>();
            if (_cam != null) _defaultFov = _cam.fieldOfView;
            _railPose = new GameObject("RailCameraPose").transform;
            Apply(startChapter);
            yield return new WaitForSeconds(0.8f);
            yield return Campaign.CampaignChapterCinema.Enter(Chapter, startChapter);
            ShowCard(startChapter);
        }

        public void GoTo(int index, bool withTransition)
        {
            if (_busy || index < 0 || index >= chapters.Count) return;
            StartCoroutine(GoRoutine(index, withTransition));
        }

        IEnumerator GoRoutine(int index, bool withTransition)
        {
            _busy = true;
            var ch = chapters[index];
            var bridge = _player != null ? _player.GetComponent<PlayerUIBridge>() : null;
            if (bridge != null) bridge.CutsceneLock = true;
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, Vector3.zero, 0.8f);
            if (withTransition) yield return TransitionOverlay.Get().Close(0.7f, ch.title);
            if (_player != null && ch.spawn != null) _player.Teleport(ch.spawn.position + Vector3.up * 0.2f, ch.spawn.rotation);
            Apply(index);
            if (_cam != null && _player != null) { _cam.Yaw = _player.transform.eulerAngles.y; _cam.Pitch = 14f; _cam.SnapToTarget(); }
            yield return new WaitForSecondsRealtime(0.25f);
            if (withTransition) yield return TransitionOverlay.Get().Open(0.7f);
            if (withTransition) yield return Campaign.CampaignChapterCinema.Enter(ch, index);
            if (bridge != null) bridge.CutsceneLock = false;
            ShowCard(index);
            _busy = false;
        }

        void ShowCard(int index)
        {
            if (index < 0 || index >= chapters.Count) return;
            var ch = chapters[index];
            BossIntroCard.Show(ch.title, $"CHAPTER {index+1}  ◆  {ch.tag}", chapterCard: true);
        }

        public void Apply(int index)
        {
            Index = index;
            for (int i = 0; i < chapters.Count; i++)
                if (chapters[i].lookRoot != null) chapters[i].lookRoot.SetActive(i == index);
            var ch = chapters[index];
            if (ch.skybox != null) RenderSettings.skybox = ch.skybox;
            RenderSettings.fog = ch.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ch.fogColor; RenderSettings.fogStartDistance = ch.fogStart; RenderSettings.fogEndDistance = ch.fogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ch.ambientSky; RenderSettings.ambientEquatorColor = ch.ambientEquator; RenderSettings.ambientGroundColor = ch.ambientGround;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = ch.background;
                cam.clearFlags = ch.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            }
            if (_cam != null)
            {
                if (ch.sideRail) { UpdateRail(true); _cam.SetOverride(_railPose); _cam.fieldOfView = ch.railFov; }
                else { if (_cam.HasOverride) _cam.ClearOverride(); _cam.fieldOfView = _defaultFov > 0 ? _defaultFov : 52f; }
            }
            var sp = Look.StylePack.Current;
            if (sp != null)
            {
                if (ch.halftoneColor.a > 0) sp.halftoneColor = ch.halftoneColor;
                if (ch.shadowTint.a > 0) sp.shadowTint = ch.shadowTint;
                if (ch.speedColor.a > 0) sp.speedColor = ch.speedColor;
                sp.Apply();
            }
            ChapterChanged?.Invoke(index);
        }

        void LateUpdate()
        {
            var ch = Chapter;
            if (ch != null && ch.sideRail) UpdateRail(false);
        }

        Vector3 _railVel;
        void UpdateRail(bool snap)
        {
            if (_player == null || _railPose == null) return;
            var ch = Chapter;
            Vector3 p = _player.transform.position;
            // the theater runs along +X; the camera sits in the audience (−Z of the stage line)
            float stageZ = ch.spawn != null ? ch.spawn.position.z : p.z;
            Vector3 want = new Vector3(p.x + 2.5f, p.y + ch.railHeight, stageZ - ch.railDistance);
            _railPose.position = snap ? want : Vector3.SmoothDamp(_railPose.position, want, ref _railVel, 0.18f);
            _railPose.rotation = Quaternion.Euler(ch.railPitch, 0, 0);
        }

        /// <summary>Dev/tests: jump straight to a chapter without the transition.</summary>
        public void DevJump(int index)
        {
            if (index < 0 || index >= chapters.Count) return;
            var ch = chapters[index];
            if (_player != null && ch.spawn != null) _player.Teleport(ch.spawn.position + Vector3.up * 0.2f, ch.spawn.rotation);
            Apply(index);
            if (_cam != null && _player != null) { _cam.Yaw = _player.transform.eulerAngles.y; _cam.SnapToTarget(); }
        }
    }
}
