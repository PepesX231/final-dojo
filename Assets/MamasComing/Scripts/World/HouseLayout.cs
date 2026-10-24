using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// The two-storey house plan shared by the level builder and runtime systems: fruit-scale rooms
    /// (2x the original human-scale 30 x 14 m plan) and an upper floor over the west half.
    /// </summary>
    public static class HouseLayout
    {
        /// Fruit-scale factor applied to the original plan (furniture included: the fruit kids are small).
        public const float Scale = 2f;
        /// Ground-floor wall height.
        public const float WallH = 2.8f * Scale;
        /// Walkable height of the upper floor (top of the floor slab).
        public const float UpperY = WallH + 0.25f;
        /// Anything at least this high is treated as being on the upper floor.
        public const float UpperThreshold = UpperY - 1.4f;
        /// The upper floor covers x 0..UpperMaxX, z 0..UpperMaxZ.
        public const float UpperMaxX = 30f, UpperMaxZ = 28f;

        public static readonly string[] RoomNames =
        {
            "Living room", "Laundry", "Dining room", "Hallway", "Bedroom", "Bathroom", "Kitchen", "Garage", "Game room",
            "Staircase", "Upstairs hall", "Mom's room", "Party loft", "Landing",
        };
        public static readonly Rect[] RoomRects =
        {
            new Rect(0f, 0f, 26f, 12f), new Rect(26f, 0f, 8f, 12f), new Rect(34f, 0f, 10f, 12f), new Rect(0f, 12f, 60f, 4f),
            new Rect(0f, 0f, 14f, 12f), new Rect(14f, 16f, 10f, 12f), new Rect(24f, 16f, 20f, 12f), new Rect(44f, 0f, 16f, 12f),
            new Rect(44f, 16f, 16f, 12f), new Rect(0f, 16f, 14f, 12f), new Rect(0f, 12f, 30f, 4f), new Rect(14f, 0f, 16f, 12f),
            new Rect(14f, 16f, 16f, 12f), new Rect(0f, 16f, 14f, 12f),
        };
        /// 0 = ground floor, 1 = upper floor.
        public static readonly int[] RoomFloor = { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1 };

        public static int FloorOf(Vector3 p) => p.y > UpperThreshold ? 1 : 0;
        public static float FloorY(int floor) => floor == 1 ? UpperY : 0f;
        /// Height of the floor surface under a point that is standing on (or just above) one of the floors.
        public static float FloorYAt(Vector3 p) => FloorY(FloorOf(p));
        public static bool SameFloor(Vector3 a, Vector3 b) => FloorOf(a) == FloorOf(b);

        public static int RoomOf(Vector3 p)
        {
            int f = FloorOf(p);
            for (int i = 0; i < RoomRects.Length; i++)
                if (RoomFloor[i] == f && RoomRects[i].Contains(new Vector2(p.x, p.z))) return i;
            return -1;
        }

        public static Vector3 RoomCenter(int i)
        {
            var r = RoomRects[i];
            return new Vector3(r.center.x, FloorY(RoomFloor[i]) + 2.3f, r.center.y);
        }
    }
}
