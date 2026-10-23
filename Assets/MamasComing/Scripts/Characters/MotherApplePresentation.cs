using System.Collections.Generic;
using UnityEngine;
namespace MamasComing
{
    [DefaultExecutionOrder(600)]
    public class MotherApplePresentation : MonoBehaviour
    {
        public GameObject kind, angry;
        public bool IsAngry;
        MomController mom;
        Vector3 previous;
        float phase;
        readonly Dictionary<Transform,Quaternion> rest = new Dictionary<Transform,Quaternion>();
        void Awake()
        {
            mom=GetComponent<MomController>(); previous=transform.position;
            foreach(var root in new[]{kind,angry}) if(root) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.name.Contains("arm.") || t.name.Contains("thigh.") || t.name.Contains("shin.") || t.name.Contains("foot.")) rest[t]=t.localRotation;
        }
        void LateUpdate()
        {
            if(kind)kind.SetActive(!IsAngry);if(angry)angry.SetActive(IsAngry);
            float speed=Time.deltaTime>0 ? Vector3.Distance(previous,transform.position)/Time.deltaTime : 0;
            previous=transform.position;
            if(speed>.05f)phase+=Time.deltaTime*8f;
            float walk=Mathf.Clamp01(speed/1.6f);
            foreach(var kv in rest)
            {
                if(!kv.Key)continue;
                float side=kv.Key.name.EndsWith(".L") ? 1f : -1f;
                float swing=Mathf.Sin(phase)*side*walk;
                float angle=kv.Key.name.Contains("thigh") ? swing*25f : kv.Key.name.Contains("shin") ? Mathf.Max(0,-swing)*22f : kv.Key.name.Contains("upper_arm") ? -swing*18f : 0;
                Vector3 axis=kv.Key.parent?kv.Key.parent.InverseTransformDirection(transform.right):Vector3.right;
                kv.Key.localRotation=Quaternion.AngleAxis(angle,axis)*kv.Value;
            }
        }
    }
}
