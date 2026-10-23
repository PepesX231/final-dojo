using UnityEngine;

namespace DreamRush.Core
{
    /// <summary>Applebook Surrealism palette tokens (GDD §5.3). Use these instead of ad-hoc colours.</summary>
    public static class Palette
    {
        public static readonly Color MidnightPlum = Hex(0x292137);
        public static readonly Color AppleRed = Hex(0xE85D65);
        public static readonly Color WarmCream = Hex(0xF5E7D3);
        public static readonly Color LeafGreen = Hex(0x78B78B);
        public static readonly Color DreamLavender = Hex(0xAAA0DD);
        public static readonly Color ApricotOrange = Hex(0xFFB870);
        public static readonly Color SkyBlue = Hex(0x8FD5ED);
        public static readonly Color DarkInk = Hex(0x302A3D);

        // Graybox functional colours (derived, desaturated so gameplay reads before art exists)
        public static readonly Color GrayboxFloor = Color.Lerp(WarmCream, MidnightPlum, 0.18f);
        public static readonly Color GrayboxWalkable = WarmCream;
        public static readonly Color GrayboxWall = Color.Lerp(DreamLavender, MidnightPlum, 0.35f);
        public static readonly Color GrayboxInteractable = Color.Lerp(LeafGreen, WarmCream, 0.25f);
        public static readonly Color GrayboxHazard = ApricotOrange;
        public static readonly Color GrayboxAccent = Color.Lerp(ApricotOrange, WarmCream, 0.4f);

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
    }
}
