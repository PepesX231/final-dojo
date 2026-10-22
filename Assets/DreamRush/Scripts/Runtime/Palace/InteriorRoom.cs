using System.Collections;
using System.Collections.Generic;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// One furnished room per type, built once far away from the city (they're "pocket rooms": every door of that type
    /// leads into the same room, re-dressed for that building — its dreamer, its minutes, its dialogue).
    /// </summary>
    public class InteriorRoom : MonoBehaviour
    {
        public RoomType type;
        public Transform spawn, npcSpot, dynamicRoot;
        public List<Transform> spots = new List<Transform>();   // where minutes / notes can sit (on furniture)
        public Color ambient = new Color(1f, 0.85f, 0.75f);
        public BuildingDoor door;                 // M17: the room sits right behind this building's doorway
        public Bounds area;                       // world-space walk-in volume
        public BuildingDoor CurrentDoor { get; private set; }
        public bool Dressed { get; private set; }

        public void Setup(BuildingDoor door)
        {
            CurrentDoor = door; Dressed = true;
            if (dynamicRoot == null) { dynamicRoot = new GameObject("Dynamic").transform; dynamicRoot.SetParent(transform, false); }
            for (int i = dynamicRoot.childCount - 1; i >= 0; i--) Destroy(dynamicRoot.GetChild(i).gameObject);
            var rnd = new System.Random(door.seed);
            var pd = PalaceDirector.Current;
            // a few minutes on the furniture (once per building)
            int coins = type == RoomType.Vault ? 0 : type == RoomType.Flat ? 3 : 2;
            var free = new List<Transform>(spots);
            for (int k = 0; k < coins && free.Count > 0; k++)
            {
                int pick = rnd.Next(free.Count); var s = free[pick]; free.RemoveAt(pick);
                LostMinute.Spawn(s.position + Vector3.up * 0.5f, 1, $"min:{door.id}:{k}", dynamicRoot);
            }
            // the room's resident
            var lib = InteriorDirector.Current != null ? InteriorDirector.Current.npcTemplate : null;
            if (lib != null && npcSpot != null)
            {
                var npc = Instantiate(lib, npcSpot.position, npcSpot.rotation, dynamicRoot); npc.SetActive(true);
                var tint = npc.GetComponent<ClockfolkLook>(); if (tint != null) tint.Dress(rnd);
                var talk = npc.GetComponent<NpcTalker>();
                if (talk != null) talk.Configure(type, door, rnd);
            }
            if (type == RoomType.Vault)
            {
                var chest = transform.GetComponentInChildren<VaultChest>(true); if (chest != null) chest.Arm(door);
            }
            var dojo = transform.GetComponentInChildren<DojoTrial>(true); if (dojo != null) dojo.Arm(door);
        }
    }
}
