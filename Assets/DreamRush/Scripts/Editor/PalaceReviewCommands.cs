using System.Linq;
using DreamRush.Palace;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DreamRush.EditorTools
{
    public static class PalaceReviewCommands
    {
        public static string Room(string type)
        {
            var r = Object.FindObjectsByType<InteriorRoom>(FindObjectsSortMode.None)
                .Where(r => r.type.ToString() == type).OrderBy(r => r.door != null ? r.door.id : "").First();
            if (Application.isPlaying) InteriorDirector.Current.WalkInto(r.door);
            var floor = r.transform.Find("Floor"); float d = floor.localScale.z, w = floor.localScale.x;
            var cam = Camera.main;
            foreach (var c in Object.FindObjectsByType<CameraSystem.ThirdPersonCameraController>(FindObjectsSortMode.None)) c.enabled = false;
            foreach (var c in Object.FindObjectsByType<CameraSystem.StageCameraController>(FindObjectsSortMode.None)) c.enabled = false;
            cam.transform.position = r.transform.TransformPoint(new Vector3(-Mathf.Min(2.5f,w*.18f), 2.5f, -d*.38f));
            cam.transform.LookAt(r.transform.TransformPoint(new Vector3(0, 1.3f, d*.15f)));
            return $"room={r.door?.id} type={type} camera={cam.transform.position} target={r.transform.position}";
        }
        public static string Inventory() => $"scene={SceneManager.GetActiveScene().name} rooms={Object.FindObjectsByType<InteriorRoom>(FindObjectsSortMode.None).Length} beacons={Object.FindObjectsByType<HourBeacon>(FindObjectsSortMode.None).Length}";
    }
}

