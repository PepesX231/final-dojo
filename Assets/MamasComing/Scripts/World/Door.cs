using UnityEngine;

namespace MamasComing
{
    public class Door : MonoBehaviour
    {
        public Transform pivot;
        public float openAngle = -100f;
        public bool open;
        float cur;

        public void Open() { open = true; }

        void Update()
        {
            cur = Mathf.MoveTowards(cur, open ? openAngle : 0f, 200f * Time.deltaTime);
            if (pivot) pivot.localRotation = Quaternion.Euler(0f, cur, 0f);
        }
    }
}
