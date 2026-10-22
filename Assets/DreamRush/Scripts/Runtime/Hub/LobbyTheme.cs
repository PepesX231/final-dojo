using System;
using UnityEngine;

namespace DreamRush.Hub
{
    /// <summary>
    /// The lobby's look follows the player's real clock (docs/LOBBY_DESIGN.md §3): four "moods" with their own UI palette,
    /// sky, light and wall tint — the menu is never the same red/black twice a day. Pure data, EditMode-testable.
    /// </summary>
    [Serializable]
    public class LobbyTheme
    {
        public string id, name, thai;
        public Color primary, secondary, accent, ink, paper;
        public Color skyTop, skyHorizon, skyBand, skyGround;
        public bool night;
        public Color sunColor; public float sunIntensity; public Vector3 sunEuler;
        public Color ambientSky, ambientEquator, ambientGround;
        public Color wallTint, floorTint, fog;
        public float lampIntensity;

        static Color H(int hex) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);

        public static readonly LobbyTheme Morning = new LobbyTheme
        {
            id = "morning", name = "SUNNY POP", thai = "เช้าสดใส",
            primary = H(0xFFD400), secondary = H(0x1FD8CF), accent = H(0xFF4F9E), ink = H(0x15112B), paper = H(0xFFF8E6),
            skyTop = H(0x45B8FF), skyHorizon = H(0xBFF3FF), skyBand = H(0xFFF1A8), skyGround = H(0x9FD9FF),
            sunColor = H(0xFFF4DC), sunIntensity = 1.35f, sunEuler = new Vector3(48, 140, 0),
            ambientSky = H(0xBFE4FF), ambientEquator = H(0xFFE9C4), ambientGround = H(0xC9B6D9),
            wallTint = H(0xFFF2CF), floorTint = H(0xFFFFFF), fog = H(0xCDEFFF), lampIntensity = 0.4f,
        };

        public static readonly LobbyTheme Afternoon = new LobbyTheme
        {
            id = "afternoon", name = "CANDY RUSH", thai = "บ่ายหวานเจี๊ยบ",
            primary = H(0xFF3D9A), secondary = H(0xB8F23A), accent = H(0x2E4BD8), ink = H(0x1A1036), paper = H(0xFFF0F7),
            skyTop = H(0x3D6BFF), skyHorizon = H(0x9AD8FF), skyBand = H(0xFFC6E8), skyGround = H(0xB5A8FF),
            sunColor = H(0xFFFFFF), sunIntensity = 1.25f, sunEuler = new Vector3(58, 210, 0),
            ambientSky = H(0xC9D3FF), ambientEquator = H(0xFFD3EA), ambientGround = H(0xB9A4D8),
            wallTint = H(0xFFDDEE), floorTint = H(0xFFF2F2), fog = H(0xD8D0FF), lampIntensity = 0.4f,
        };

        public static readonly LobbyTheme Sunset = new LobbyTheme
        {
            id = "sunset", name = "GOLDEN HOUR", thai = "เย็นสีส้ม",
            primary = H(0xFF7417), secondary = H(0xE3197F), accent = H(0x6A2CE0), ink = H(0x1E0B26), paper = H(0xFFE9D6),
            skyTop = H(0x341A6B), skyHorizon = H(0xFF9A52), skyBand = H(0xFF4D7A), skyGround = H(0x5A2A6E),
            sunColor = H(0xFFA35C), sunIntensity = 1.15f, sunEuler = new Vector3(14, 250, 0),
            ambientSky = H(0x8A6AC8), ambientEquator = H(0xFF9E80), ambientGround = H(0x4A2A50),
            wallTint = H(0xFFD4BC), floorTint = H(0xFFE6D6), fog = H(0xE58A8A), lampIntensity = 1.2f,
        };

        public static readonly LobbyTheme Midnight = new LobbyTheme
        {
            id = "midnight", name = "MIDNIGHT MODE", thai = "ดึกแล้ว แต่ยังไม่ง่วง", night = true,
            primary = H(0x00E1FF), secondary = H(0xFF2E88), accent = H(0x8C6BFF), ink = H(0x0A0820), paper = H(0xE9E6FF),
            skyTop = H(0x070A26), skyHorizon = H(0x3A2470), skyBand = H(0xD04BC8), skyGround = H(0x0C0A2A),
            sunColor = H(0x9DB6FF), sunIntensity = 0.75f, sunEuler = new Vector3(38, 160, 0),
            ambientSky = H(0x5A5CB0), ambientEquator = H(0x6A4A92), ambientGround = H(0x221A3E),
            wallTint = H(0xC8C6F2), floorTint = H(0xC9C2EA), fog = H(0x2A1F5A), lampIntensity = 2.4f,
        };

        public static readonly LobbyTheme[] All = { Morning, Afternoon, Sunset, Midnight };

        /// <summary>05–11 morning, 11–17 afternoon, 17–20 sunset, 20–05 midnight.</summary>
        public static LobbyTheme ForHour(int hour)
        {
            hour = ((hour % 24) + 24) % 24;
            if (hour >= 5 && hour < 11) return Morning;
            if (hour >= 11 && hour < 17) return Afternoon;
            if (hour >= 17 && hour < 20) return Sunset;
            return Midnight;
        }

        public static LobbyTheme ById(string id)
        {
            foreach (var t in All) if (t.id == id) return t;
            return null;
        }

        /// <summary>Dev/test override ("" = follow the clock).</summary>
        public static string OverrideId = "";
        public static LobbyTheme Current => ById(OverrideId) ?? ForHour(DateTime.Now.Hour);
    }
}
