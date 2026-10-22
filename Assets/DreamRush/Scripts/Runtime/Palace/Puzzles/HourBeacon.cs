using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17 wayfinding: a compact light marker above an objective; it goes out when done.</summary>
    public class HourBeacon : MonoBehaviour
    {
        public StolenHour hour;
        public PuzzleReward puzzle;
        float _t;
        Vector3 _baseScale;
        void OnEnable() { _baseScale = transform.localScale; _t = 0; }
        void Update()
        {
            bool done = (hour != null && hour.Collected) || (puzzle != null && puzzle.Solved);
            if (done) { gameObject.SetActive(false); return; }
            _t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(_t * 2f) * 0.08f;
            transform.localScale = new Vector3(_baseScale.x * pulse, _baseScale.y, _baseScale.z * pulse);
        }
    }
}
