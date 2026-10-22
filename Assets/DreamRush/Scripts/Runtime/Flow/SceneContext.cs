using System.Collections;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Flow
{
    public enum SceneKind { Test, Hub, Dream }

    /// <summary>
    /// Declares what a scene is so the persistent flow can start from any scene, and places the player
    /// correctly when returning from a dream (GDD §30 "Returning from a dream places the player correctly").
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class SceneContext : MonoBehaviour
    {
        public SceneKind kind = SceneKind.Test;
        public Transform defaultSpawn;
        [Tooltip("Level theme (None = the kind's default: Bedroom for the hub, Dream for dreams).")]
        public Audio.Music music = Audio.Music.None;

        public static SceneContext Current { get; private set; }

        void Awake()
        {
            Current = this;
            UIRoot.Get();
            if (kind == SceneKind.Dream) World1.DreamStar.ResetRun();
            if (kind != SceneKind.Test) GameFlowManager.Get().NotifySceneStarted(kind);
            if (music != Audio.Music.None) Audio.AudioManager.PlayMusic(music);
            else if (kind == SceneKind.Hub) Audio.AudioManager.PlayMusic(Audio.Music.Bedroom);
            else if (kind == SceneKind.Dream) Audio.AudioManager.PlayMusic(Audio.Music.Dream);
        }

        IEnumerator Start()
        {
            if (kind != SceneKind.Hub) yield break;
            var flow = GameFlowManager.Get();
            var motor = FindFirstObjectByType<PlayerMotor3D>();
            if (motor == null) yield break;
            if (flow.ReturningFromDream)
            {
                var bed = FindFirstObjectByType<BedInteractable>();
                if (bed != null && bed.wakePose != null)
                    motor.Teleport(bed.wakePose.position, Quaternion.Euler(0, bed.wakePose.eulerAngles.y, 0));
                var cam = FindFirstObjectByType<CameraSystem.ThirdPersonCameraController>();
                // frame the waking apple from the room side (behind it is the bed/headboard)
                if (cam != null) { cam.Yaw = motor.transform.eulerAngles.y + 150f; cam.Pitch = 15f; cam.SnapToTarget(); }
                yield return PlayerCutscene.WakeStretch(motor);
                if (flow.SequenceSpeed <= 1f) TitleScreen.ShowIfFirstBoot();   // the bedroom IS the lobby — back to the menu after every dream
            }
            else
            {
                if (defaultSpawn != null) motor.Teleport(defaultSpawn.position, defaultSpawn.rotation);
                yield return null;
                if (flow.SequenceSpeed <= 1f) TitleScreen.ShowIfFirstBoot();   // boot straight into the lobby (no title screen; tests run faster and skip it)
            }
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
