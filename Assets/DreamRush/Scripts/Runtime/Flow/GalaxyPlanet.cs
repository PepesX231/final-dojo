using UnityEngine;

namespace DreamRush.Flow
{
    /// <summary>One world in the Dream Galaxy (authored by GalaxySelectBuilder).</summary>
    public class GalaxyPlanet : MonoBehaviour
    {
        public string worldId = "W1";
        public string title = "THE DREAM BOOK";
        public string subtitle = "";
        [TextArea] public string chapters = "";
        public string sceneName = GameFlowManager.DreamScene;
        public bool locked;
        public float spinSpeed = 6f;
        public Transform cameraPose;
        public Transform shipPoint;
        Vector3 _home;
        float _phase;

        void Awake() { _home = transform.position; _phase = Random.value * 10f; }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = _home + Vector3.up * Mathf.Sin(Time.time * 0.6f + _phase) * 0.35f;
        }
    }
}
