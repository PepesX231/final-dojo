using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>Clock hands / gears that keep turning around a local axis; <see cref="Speed"/> lets a TIME SKIP spin them all.</summary>
    public class ClockSpin : MonoBehaviour
    {
        public static float Speed = 1f;
        public Vector3 axis = Vector3.forward;
        public float degPerSec = 6f;
        void Update() => transform.Rotate(axis, degPerSec * Speed * Time.deltaTime, Space.Self);
    }
}
