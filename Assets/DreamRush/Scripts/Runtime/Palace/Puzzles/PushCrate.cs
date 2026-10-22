using DreamRush.Combat;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17: a big gift-box crate. Punch it and it slides one cell (cellSize) along the punch's main axis, unless
    /// something solid is in the way. Pressure plates count it.
    /// </summary>
    public class PushCrate : MonoBehaviour, IHittable
    {
        public float cellSize = 3f;
        public int Pushes { get; private set; }
        Vector3 _target; bool _moving; float _cool;

        void Start() { _target = transform.position; }

        public bool TakeHit(HitInfo hit)
        {
            if (_moving || Time.time < _cool) return false;
            Vector3 d = hit.direction; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return false;
            d = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(d.z));
            return Push(d);
        }

        public bool Push(Vector3 dir)
        {
            var half = transform.lossyScale * 0.45f;
            var col = GetComponent<Collider>(); if (col != null) col.enabled = false;
            bool blocked = Physics.BoxCast(transform.position + Vector3.up * 0.1f, new Vector3(half.x * 0.9f, half.y * 0.6f, half.z * 0.9f), dir, transform.rotation, cellSize, ~0, QueryTriggerInteraction.Ignore);
            if (col != null) col.enabled = true;
            _cool = Time.time + 0.25f;
            if (blocked) { Audio.AudioManager.Play(Audio.Sfx.Clank, transform.position, 0.6f); return true; }
            _target = transform.position + dir * cellSize; _moving = true; Pushes++;
            Audio.AudioManager.Play(Audio.Sfx.Whoosh, transform.position, 0.7f);
            Presentation.VFX.Play(Presentation.VFXKind.DustPuff, transform.position - dir, 1.2f);
            return true;
        }

        void Update()
        {
            if (!_moving) return;
            transform.position = Vector3.MoveTowards(transform.position, _target, Time.deltaTime * 9f);
            if ((transform.position - _target).sqrMagnitude < 1e-4f) { transform.position = _target; _moving = false; }
        }
    }
}
