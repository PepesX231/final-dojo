using System;
using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;
namespace DreamRush.Palace
{
    /// <summary>Distance culling for decorative interior batches only. Never controls room gameplay or collision.</summary>
    public class PalaceDressingVisibility : MonoBehaviour
    {
        [Serializable] public class Entry { public GameObject dressing; public Bounds area; }
        public List<Entry> rooms = new List<Entry>();
        public float visibleDistance = 32f;
        PlayerMotor3D _player; Camera _camera; float _next;
        void Start() { _player=FindFirstObjectByType<PlayerMotor3D>(); _camera=Camera.main; Refresh(); }
        void Update() { _next-=Time.deltaTime; if(_next<=0) { _next=.25f;Refresh(); } }
        public void Refresh()
        {
            float range2=visibleDistance*visibleDistance;
            foreach(var entry in rooms) {
                if(entry.dressing==null)continue;
                bool show=(_player!=null&&entry.area.SqrDistance(_player.transform.position)<=range2)
                    || (_camera!=null&&entry.area.SqrDistance(_camera.transform.position)<=range2);
                if(entry.dressing.activeSelf!=show)entry.dressing.SetActive(show);
            }
        }
        void OnDisable() { foreach(var e in rooms)if(e.dressing!=null)e.dressing.SetActive(true); }
    }
}
