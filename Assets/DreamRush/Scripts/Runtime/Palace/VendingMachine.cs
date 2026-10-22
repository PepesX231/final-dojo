using DreamRush.Combat;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>Street vending machines (M15 "everything should mean something"): slash one and it rattles out 2 minutes, up to 3 times.</summary>
    public class VendingMachine : MonoBehaviour, IHittable
    {
        public string key = "vend";
        public int charges = 3;
        float _cool; Vector3 _base; Transform _model;

        void Start() { _model = transform.childCount > 0 ? transform.GetChild(0) : transform; _base = _model.localPosition; if (PalaceDirector.Current != null && PalaceDirector.Current.Taken(key)) charges = 0; }

        public bool TakeHit(HitInfo hit)
        {
            if (charges <= 0 || Time.time < _cool) return false;
            charges--; _cool = Time.time + 0.5f;
            LostMinute.Burst(transform.position + transform.forward * 0.8f + Vector3.up * 0.4f, 2);
            Audio.AudioManager.Play(Audio.Sfx.Clank, transform.position, 0.8f);
            _shake = 0.35f;
            if (charges == 0 && PalaceDirector.Current != null) PalaceDirector.Current.Take(key);
            return true;
        }

        float _shake;
        void Update()
        {
            if (_shake <= 0f || _model == null) return;
            _shake -= Time.deltaTime;
            _model.localPosition = _base + new Vector3(Mathf.Sin(Time.time * 70f) * 0.05f * _shake / 0.35f, 0, 0);
            if (_shake <= 0f) _model.localPosition = _base;
        }
    }
}
