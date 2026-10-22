using System.Collections;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// Page-portal: a giant standing page at the end of a chapter. Run into it → page-turn transition with the
    /// next chapter's title → teleport to that chapter's spawn (ChapterDirector applies its look/camera).
    /// </summary>
    public class PagePortal : MonoBehaviour
    {
        public int toChapter = 1;
        public float radius = 2.2f;
        public bool requireGrounded = false;
        PlayerMotor3D _player;
        bool _busy;

        void Start() => _player = FindFirstObjectByType<PlayerMotor3D>();

        void Update()
        {
            transform.GetChild(0).localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 1.5f) * 6f, 0);
            if (_busy || _player == null || ChapterDirector.Current == null) return;
            Vector3 d = _player.transform.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude < radius && Mathf.Abs(d.y) < 3f && (!requireGrounded || _player.IsGrounded))
            {
                _busy = true;
                ChapterDirector.Current.GoTo(toChapter, true);
            }
        }
    }
}
