using System.Collections.Generic;
using DreamRush.Core;
using DreamRush.DreamLogic;
using UnityEngine;

namespace DreamRush.Audio
{
    public enum Sfx
    {
        Footstep, Jump, Land, Hurt, Kick,
        ChairStep, ChairGasp, ChairYelp, ChairStumble, ChairTired, ChairCreak,
        CardFlash, CardGlue, CardReel, CardGrandpa, CardDecoy, CardBoing, CardFail,
        UiClick, UiOpen, CardDeal, CardPick, SleepChime, WakeUp, LawWarn, Victory,
        // M15 combat / movement / world set
        Whoosh, Swing, SwingHeavy, Hit, HitHeavy, Clank, Burst, Coin, Door, Grind, Reflect, EnemyAlert, Shock, UiHover, UiBack, Pickup,
        // M18/M19 fists + clockwork boss
        Punch, PunchHeavy, BellToll, Ratchet, Tick
    }

    public enum Music { None, Bedroom, Dream, Boss, World1, Palace, Midnight, Galaxy, Interior, ToyBoxMoon, BubbleBathSea, MidnightTeacup, DreamFinale, Credits }

    /// <summary>
    /// Persistent audio hub (GDD §20.7): pooled one-shots with slight pitch variation, crossfaded music loops,
    /// volumes from Settings. Clips load from Resources/DreamRush/Audio (procedural placeholders, CC0 — see
    /// docs/AUDIO_CREDITS.md). Missing clips are silently skipped so audio can never break gameplay.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string ClipRoot = "DreamRush/Audio/";
        static AudioManager _instance;
        public static bool Exists => _instance != null;
        /// <summary>Tests can turn audio off entirely (no GameObject is created).</summary>
        public static bool Muted;

        static float _sfxVolume = 0.9f, _musicVolume = 0.7f;
        public static float MusicTime => _instance != null && _instance._musicA != null ? _instance._musicA.time : 0f;
        public static bool MusicIsPlaying => _instance != null && _instance._musicA != null && _instance._musicA.isPlaying;
        public static Music CurrentMusic { get; private set; }
        public static int PlayedCount { get; private set; }
        public static Sfx? LastPlayed { get; private set; }

        readonly List<AudioSource> _pool = new List<AudioSource>();
        readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<Sfx, float> _lastTime = new Dictionary<Sfx, float>();
        AudioSource _musicA, _musicB;
        float _fade = 1f;

        static AudioManager Get()
        {
            if (_instance != null) return _instance;
            if (Muted) return null;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AudioManager>();
            _instance._musicA = _instance.NewSource("MusicA", true);
            _instance._musicB = _instance.NewSource("MusicB", true);
            var save = SaveManager.Instance;
            if (save != null && save.Data != null) ApplyVolumes(save.Data.settings);
            return _instance;
        }

        AudioSource NewSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
            return s;
        }

        public static void ApplyVolumes(SettingsData s)
        {
            if (s == null) return;
            _sfxVolume = s.sfxVolume; _musicVolume = s.musicVolume;
            if (_instance != null) _instance.UpdateMusicVolumes();
        }

        AudioClip[] Clips(string baseName)
        {
            if (_clips.TryGetValue(baseName, out var c)) return c;
            var list = new List<AudioClip>();
            var single = Resources.Load<AudioClip>(ClipRoot + baseName);
            if (single != null) list.Add(single);
            for (int i = 0; i < 6; i++) // variations: Name_0, Name_1, ...
            {
                var v = Resources.Load<AudioClip>($"{ClipRoot}{baseName}_{i}");
                if (v == null) break;
                list.Add(v);
            }
            c = list.ToArray();
            _clips[baseName] = c;
            return c;
        }

        public static string FileName(Sfx s) => s switch
        {
            Sfx.CardFlash => "SFX_Card_Flash", Sfx.CardGlue => "SFX_Card_Glue", Sfx.CardReel => "SFX_Card_Reel",
            Sfx.CardGrandpa => "SFX_Card_Grandpa", Sfx.CardDecoy => "SFX_Card_Decoy", Sfx.CardBoing => "SFX_Card_Boing",
            Sfx.CardFail => "SFX_Card_Fail", Sfx.CardDeal => "SFX_Card_Deal", Sfx.CardPick => "SFX_Card_Pick",
            Sfx.UiClick => "SFX_UI_Click", Sfx.UiOpen => "SFX_UI_Open",
            _ => "SFX_" + s
        };

        /// <summary>Plays a one-shot. Same sound is rate-limited (35 ms) so stacked events don't get loud.</summary>
        public static void Play(Sfx sfx, Vector3 position, float volume = 1f)
        {
            var m = Get();
            if (m == null) return;
            float now = Time.unscaledTime;
            if (m._lastTime.TryGetValue(sfx, out var last) && now - last < 0.035f) return;
            m._lastTime[sfx] = now;
            var clips = m.Clips(FileName(sfx));
            if (clips.Length == 0) return;
            var src = m.FreeSource();
            src.clip = clips[Random.Range(0, clips.Length)];
            src.pitch = Random.Range(0.94f, 1.06f);
            src.volume = Mathf.Clamp01(volume) * _sfxVolume;
            src.Play();
            PlayedCount++; LastPlayed = sfx;
        }

        public static void PlayCard(EffectAction action, Vector3 position)
        {
            Play(action switch
            {
                EffectAction.Freeze => Sfx.CardFlash,
                EffectAction.Root => Sfx.CardGlue,
                EffectAction.Pull => Sfx.CardReel,
                EffectAction.ChangeBehavior => Sfx.CardGrandpa,
                EffectAction.SpawnDecoy => Sfx.CardDecoy,
                _ => Sfx.CardBoing
            }, position);
        }

        AudioSource FreeSource()
        {
            foreach (var s in _pool) if (!s.isPlaying) return s;
            if (_pool.Count < 16) { var n = NewSource("Sfx" + _pool.Count, false); _pool.Add(n); return n; }
            return _pool[0];
        }

        // ------------------------------------------------------------ music
        public static void PlayMusic(Music music, float fadeSeconds = 1.2f)
        {
            var m = Get();
            if (m == null || CurrentMusic == music) return;
            CurrentMusic = music;
            (m._musicA, m._musicB) = (m._musicB, m._musicA); // A = incoming
            m._musicA.clip = music == Music.None ? null : Resources.Load<AudioClip>(ClipRoot + "MUS_" + music);
            m._musicA.volume = 0f;
            if (m._musicA.clip != null) m._musicA.Play();
            m._fade = 0f;
            m._fadeSpeed = 1f / Mathf.Max(0.05f, fadeSeconds);
        }

        float _fadeSpeed = 1f;

        void Update()
        {
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime * _fadeSpeed);
                UpdateMusicVolumes();
                if (_fade >= 1f) _musicB.Stop();
            }
        }

        void UpdateMusicVolumes()
        {
            if (_musicA != null) _musicA.volume = _fade * _musicVolume * 0.6f;
            if (_musicB != null) _musicB.volume = (1f - _fade) * _musicVolume * 0.6f;
        }

        public static void ResetForTests()
        {
            PlayedCount = 0; LastPlayed = null;
            if (_instance != null) { Destroy(_instance.gameObject); _instance = null; }
            CurrentMusic = Music.None;
        }
    }
}
