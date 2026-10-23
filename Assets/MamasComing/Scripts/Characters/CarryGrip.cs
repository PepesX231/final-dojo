using UnityEngine;
namespace MamasComing
{
    public class CarryGrip : MonoBehaviour
    {
        public Vector3 left = new Vector3(-.14f,.15f,0);
        public Vector3 right = new Vector3(.14f,.15f,0);
        public Vector3 carryAnchor = new Vector3(0,.48f,.52f);
        public float shoulderForward = .45f;
        public float shoulderInward = .17f;
        public Vector3 LeftWorld => transform.TransformPoint(left);
        public Vector3 RightWorld => transform.TransformPoint(right);
    }
}
