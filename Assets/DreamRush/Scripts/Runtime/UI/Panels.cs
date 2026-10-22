using System.Text;
using DreamRush.Core;
using DreamRush.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>Applies persistent settings to live systems.</summary>
    public static class SettingsApplier
    {
        public static void Apply(SettingsData s)
        {
            foreach (var cam in Object.FindObjectsByType<ThirdPersonCameraController>(FindObjectsSortMode.None))
            {
                cam.mouseSensitivity = s.mouseSensitivity;
                cam.stickSensitivity = s.stickSensitivity;
                cam.invertY = s.invertY;
            }
            AudioListener.volume = s.masterVolume;
            if (UIRoot.Exists) UIRoot.Get().ApplyUiScale(s.textScale);
            UIRoot.ReduceFlashing = s.reduceFlashing;
            Presentation.CameraShake.Reduced = s.reduceScreenShake;
            Audio.AudioManager.ApplyVolumes(s);
        }
    }

    /// <summary>Lamp → Settings (GDD §7.5 Zone B, §23.6). Changes apply live and save on close.</summary>
    public class SettingsPanel : ModalPanel
    {
        protected override void BuildContent()
        {
            var save = SaveManager.GetOrCreate();
            var s = save.Data.settings;
            Slider first = UIFactory.Slider("MouseSensitivity", Content, "Mouse sensitivity", 0.02f, 0.5f, s.mouseSensitivity,
                v => { s.mouseSensitivity = v; SettingsApplier.Apply(s); });
            UIFactory.Slider("StickSensitivity", Content, "Stick sensitivity", 30f, 400f, s.stickSensitivity,
                v => { s.stickSensitivity = v; SettingsApplier.Apply(s); }, v => v.ToString("0"));
            UIFactory.Toggle("InvertY", Content, "Invert camera Y", s.invertY, v => { s.invertY = v; SettingsApplier.Apply(s); });
            UIFactory.Slider("MasterVolume", Content, "Master volume", 0f, 1f, s.masterVolume,
                v => { s.masterVolume = v; SettingsApplier.Apply(s); }, v => Mathf.RoundToInt(v * 100) + "%");
            UIFactory.Slider("MusicVolume", Content, "Music volume", 0f, 1f, s.musicVolume,
                v => { s.musicVolume = v; SettingsApplier.Apply(s); }, v => Mathf.RoundToInt(v * 100) + "%");
            UIFactory.Slider("SfxVolume", Content, "Effects volume", 0f, 1f, s.sfxVolume,
                v => { s.sfxVolume = v; SettingsApplier.Apply(s); }, v => Mathf.RoundToInt(v * 100) + "%");
            UIFactory.Slider("TextScale", Content, "Text size", 0.8f, 1.6f, s.textScale,
                v => { s.textScale = v; SettingsApplier.Apply(s); }, v => Mathf.RoundToInt(v * 100) + "%");
            UIFactory.Toggle("ReduceShake", Content, "Reduce screen shake", s.reduceScreenShake, v => { s.reduceScreenShake = v; SettingsApplier.Apply(s); });
            UIFactory.Toggle("ReduceFlash", Content, "Reduce flashing", s.reduceFlashing, v => { s.reduceFlashing = v; SettingsApplier.Apply(s); });
            UIFactory.Toggle("Subtitles", Content, "Subtitles", s.subtitles, v => s.subtitles = v);
            SetFirstSelectable(first);
            Closed += () => save.Save();
        }
    }

    /// <summary>Bookshelf → Dream Diary (GDD §7.5 Zone C, §16.9). Undiscovered methods stay hidden as "???".</summary>
    public class DreamDiaryPanel : ModalPanel
    {
        public static readonly (string id, string title, string hint)[] ChairVictories =
        {
            ("Exhaustion", "Tired Out", "It can't run forever."),
            ("Shortcut", "Head 'Em Off", "Take the high road."),
            ("Obstacle", "Cornered", "Block the escape."),
        };

        protected override void BuildContent()
        {
            var d = SaveManager.GetOrCreate().Data;
            Paragraph($"Dreams dreamt: {d.diary.runsStarted}    Sweet dreams: {d.diary.runsWon}    Woken up: {d.diary.runsLost}", 26, TextAnchor.MiddleCenter);
            Paragraph($"Dream Memory: {d.progression.dreamMemory}", 26, TextAnchor.MiddleCenter);
            bool metChair = d.diary.bossesMet.Contains("Chair");
            Paragraph(metChair ? "THE CHAIR THAT REFUSED" : "??? (an empty page)", 32, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            var sb = new StringBuilder();
            int found = 0;
            foreach (var v in ChairVictories)
            {
                bool has = d.diary.discoveredVictories.Contains(v.id);
                if (has) found++;
                sb.AppendLine(has ? $"FOUND   {v.title}" : (metChair ? $"???     ({v.hint})" : "???"));
            }
            Paragraph(sb.ToString(), 26);
            Paragraph($"Ways discovered: {found} / {ChairVictories.Length}", 24, TextAnchor.MiddleCenter);
        }
    }

    /// <summary>Simple message card (window, door, posters).</summary>
    public class MessagePanel : ModalPanel
    {
        public static MessagePanel Show(string title, string body)
        {
            var p = Create<MessagePanel>(title, new Vector2(1000, 520));
            p.Paragraph(body, 30, TextAnchor.MiddleCenter);
            UIRoot.Get().Push(p);
            return p;
        }
    }

    /// <summary>Pause (GDD §8.4 accessibility fallback): settings and diary are always reachable from here.</summary>
    public class PauseMenuPanel : ModalPanel
    {
        public System.Action OnQuitDream;
        public bool InDream;

        public static PauseMenuPanel Show(bool inDream, System.Action onQuitDream)
        {
            var p = Create<PauseMenuPanel>("PAUSED", new Vector2(640, 720));
            p.InDream = inDream;
            p.OnQuitDream = onQuitDream;
            p.BuildButtons();
            UIRoot.Get().Push(p);
            return p;
        }

        void BuildButtons()
        {
            var resume = UIFactory.Button("Resume", Content, "RESUME", Close);
            UIFactory.Button("Settings", Content, "SETTINGS", () => UIRoot.Get().Push(Create<SettingsPanel>("SETTINGS", new Vector2(1100, 960))));
            UIFactory.Button("Diary", Content, "DREAM DIARY", () => UIRoot.Get().Push(Create<DreamDiaryPanel>("DREAM DIARY", new Vector2(1100, 900))));
            if (InDream)
                UIFactory.Button("WakeUp", Content, "WAKE UP", () => { var cb = OnQuitDream; UIRoot.Get().CloseAll(); cb?.Invoke(); });
            UIFactory.Button("Quit", Content, "QUIT GAME", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            SetFirstSelectable(resume);
        }
    }
}
