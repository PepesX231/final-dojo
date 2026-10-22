using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 (user: "I don't want press-E-and-warp — let me just walk in"): every building's ground floor is a real,
    /// furnished room behind an open doorway and big shop windows. No loading, no teleport. This director only
    /// notices which room Pip is standing in (cheap AABB test), dresses it the first time (resident, minutes),
    /// switches to the cosy interior music and shows the place tag; walking out restores the street music.
    /// </summary>
    public class InteriorDirector : MonoBehaviour
    {
        public static InteriorDirector Current { get; private set; }
        public List<InteriorRoom> rooms = new List<InteriorRoom>();
        public GameObject npcTemplate;
        public bool Inside { get; private set; }
        public InteriorRoom Room { get; private set; }
        public BuildingDoor Door => Room != null ? Room.door : null;
        public int Entered { get; private set; }
        PlayerMotor3D _player; Audio.Music _music = Audio.Music.Palace; float _check;

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }
        void Start() { _player = FindFirstObjectByType<PlayerMotor3D>(); }

        void Update()
        {
            if (_player == null) return;
            _check -= Time.deltaTime; if (_check > 0f) return; _check = 0.1f;
            Vector3 p = _player.transform.position + Vector3.up * 0.5f;
            InteriorRoom found = null;
            if (Room != null && Room.area.Contains(p)) found = Room;
            else foreach (var r in rooms) if (r != null && r.area.Contains(p)) { found = r; break; }
            if (found == Room) return;
            if (found != null) EnterRoom(found); else LeaveRoom();
        }

        void EnterRoom(InteriorRoom r)
        {
            bool wasInside = Inside;
            Room = r; Inside = true; Entered++;
            if (!r.Dressed && r.door != null) r.Setup(r.door);
            if (!wasInside) { _music = Audio.AudioManager.CurrentMusic == Audio.Music.Interior ? Audio.Music.Palace : Audio.AudioManager.CurrentMusic; Audio.AudioManager.PlayMusic(Audio.Music.Interior, 0.8f); }
            WonderDirector.Novelty("walked into " + r.type);
        }

        void LeaveRoom()
        {
            var d = Door; Room = null; Inside = false;
            if (d != null) d.Refresh();
            Audio.AudioManager.PlayMusic(_music == Audio.Music.None ? Audio.Music.Palace : _music, 1.0f);
        }

        /// <summary>Tests / dev: stand just inside a building of this door.</summary>
        public InteriorRoom RoomOf(BuildingDoor door) { foreach (var r in rooms) if (r != null && r.door == door) return r; return null; }
        public void WalkInto(BuildingDoor door) { var r = RoomOf(door); if (r != null && _player != null) _player.Teleport(r.spawn.position, r.spawn.rotation); _check = 0f; }
        public void WalkOut(BuildingDoor door) { if (door != null && door.outside != null && _player != null) _player.Teleport(door.outside.position, door.outside.rotation); _check = 0f; }
    }
}
