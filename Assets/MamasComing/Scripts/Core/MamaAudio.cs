using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Procedural music/ambience with licensed material foley overrides from Resources/MamaFoley.
    public class MamaAudio : MonoBehaviour
    {
        public static MamaAudio I;
        const int SR = 22050;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource music, waterLoop, fireLoop, hissLoop, drainLoop;
        public float musicVolume = 0.42f;
        // recorded foley: every file "name~N" is one more variation of "name" (picked at random so nothing repeats)
        readonly Dictionary<string, List<AudioClip>> variants = new Dictionary<string, List<AudioClip>>();
        // the score (Resources/MamaMusic): menu, house_calm, house_tense, mom_home, ranking - crossfaded by game state
        readonly Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        AudioSource musicB, musicCur, musicPrev; string musicTrack; float stingDuck, wantT; string wantCache; bool rankingScene;
        float hissT, drainT; Drain[] drains;
        System.Random rng = new System.Random(7);

        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("MamaAudio");
            go.AddComponent<MamaAudio>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
            BuildClips();
            foreach (var clip in Resources.LoadAll<AudioClip>("MamaFoley"))
            {
                string b = clip.name.Split('~')[0];
                if (!variants.TryGetValue(b, out var l)) variants[b] = l = new List<AudioClip>();
                l.Add(clip);
                if (!clip.name.Contains("~") || !clips.ContainsKey(b)) clips[b] = clip;
            }
            foreach (var clip in Resources.LoadAll<AudioClip>("MamaMusic")) tracks[clip.name] = clip;
            music = MakeSrc("Music", false); musicB = MakeSrc("Music B", false); musicCur = music;
            foreach (var m in new[] { music, musicB }) { m.loop = true; m.volume = 0f; m.ignoreListenerPause = true; }
            if (tracks.Count == 0) { music.clip = clips["music"]; music.volume = musicVolume; music.Play(); }   // fallback: the old synth loop
            rankingScene = FindFirstObjectByType<MamaRankingScreen>() != null;
            waterLoop = MakeLoop("Water", "water");
            fireLoop = MakeLoop("Fire", "fire");
            hissLoop = MakeLoop("Hiss", "hiss");
            drainLoop = MakeLoop("Gurgle", "gurgleLoop");
            for (int i = 0; i < 16; i++) pool.Add(MakeSrc("SFX" + i, true));
        }

        AudioSource MakeSrc(string n, bool spatial)
        {
            var go = new GameObject(n); go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.spatialBlend = spatial ? 0.75f : 0f;
            s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2f; s.maxDistance = 22f; s.dopplerLevel = 0;
            return s;
        }
        AudioSource MakeLoop(string n, string clip)
        {
            var s = MakeSrc(n, true); s.clip = clips[clip]; s.loop = true; s.volume = 0; s.Play(); return s;
        }

        public static void Play(string name, Vector3? pos = null, float vol = 1f, float pitch = 1f)
        {
            NetGame.SoundHook?.Invoke(name, pos, vol, pitch);
            PlayLocal(name, pos, vol, pitch);
        }

        public static void PlayLocal(string name, Vector3? pos = null, float vol = 1f, float pitch = 1f)
        {
            if (pos.HasValue) MamaVfx.ForSound(name, pos.Value, vol);   // every positioned sound gets its matching burst
            if (I == null) Ensure();
            if (I == null || !I.clips.TryGetValue(name, out var c)) return;
            AudioSource s = null;
            foreach (var p in I.pool) if (!p.isPlaying) { s = p; break; }
            if (s == null) s = I.pool[0];
            if (I.variants.TryGetValue(name, out var vs) && vs.Count > 1) c = vs[I.rng.Next(vs.Count)];
            if (name == "win" || name == "lose") I.stingDuck = c.length;   // the jingle gets the stage
            s.clip = c; s.volume = Mathf.Clamp01(vol * MamaSettings.SfxVol); s.pitch = pitch * (0.96f + (float)I.rng.NextDouble() * 0.08f);
            if (pos.HasValue) { s.spatialBlend = 0.75f; s.transform.position = pos.Value; }
            else { s.spatialBlend = 0f; }
            s.Play();
        }

        public static AudioClip Clip(string name) { if (I == null) Ensure(); return I != null && I.clips.TryGetValue(name, out var c) ? c : null; }

        /// Keep the extinguisher hiss alive (call every frame while spraying).
        public static void Hiss(Vector3 p) { if (I) { I.hissT = 0.12f; I.hissLoop.transform.position = p; } }
        public static void Gurgle(Vector3 p) { if (I) { I.drainT = 0.25f; I.drainLoop.transform.position = p; } }

        void Update()
        {
            // water: loudest running tap / overflow
            float wv = 0f; Vector3 wp = transform.position;
            foreach (var t in FindObjectsByType<WaterTap>(FindObjectsSortMode.None))
                if (t.Flowing) { wv = 0.22f; wp = t.transform.position; }
            var fs = FloodSim.I;
            if (fs != null && wv == 0f && fs.TotalLiters > 30f) { wv = 0.05f; }
            waterLoop.transform.position = wp;
            waterLoop.volume = Mathf.MoveTowards(waterLoop.volume, wv * MamaSettings.SfxVol, Time.unscaledDeltaTime * 0.8f);

            // fire: strongest fire source
            float fv = 0f; Vector3 fp = transform.position; float best = 0f;
            foreach (var f in FireSource.All)
                if (f.burning && f.intensity > best) { best = f.intensity; fp = f.transform.position; }
            if (best > 0.02f) fv = Mathf.Clamp(0.15f + best * 0.3f, 0f, 0.45f);
            fireLoop.transform.position = fp;
            fireLoop.volume = Mathf.MoveTowards(fireLoop.volume, fv * MamaSettings.SfxVol, Time.unscaledDeltaTime * 1.2f);
            fireLoop.pitch = 0.95f + best * 0.1f;

            if (drains == null) drains = FindObjectsByType<Drain>(FindObjectsSortMode.None);
            if (fs != null) foreach (var dr in drains) if (dr && !dr.clogged && fs.DepthAround(dr.transform.position, 0.8f) > 0.0008f) Gurgle(dr.transform.position);
            hissT -= Time.deltaTime; drainT -= Time.deltaTime;
            hissLoop.volume = Mathf.MoveTowards(hissLoop.volume, hissT > 0 ? 0.35f * MamaSettings.SfxVol : 0f, Time.unscaledDeltaTime * 6f);
            drainLoop.volume = Mathf.MoveTowards(drainLoop.volume, drainT > 0 ? 0.4f * MamaSettings.SfxVol : 0f, Time.unscaledDeltaTime * 2f);
            foreach (var source in pool) source.mute = MamaSettings.SfxVol <= 0f;
            bool focus=GameManager.I && GameManager.I.ui && GameManager.I.ui.ImportantMessage;
            float udt = Time.unscaledDeltaTime;
            stingDuck -= udt;
            float targetMusic=musicVolume*MamaSettings.MusicVol*(focus?.48f:.85f)*(stingDuck>0f?.2f:1f);
            if (tracks.Count == 0) { music.volume=Mathf.Lerp(music.volume,targetMusic,udt*(focus?12f:2f)); return; }
            string want = WantedTrack();
            if (want != musicTrack && tracks.TryGetValue(want, out var tc))
            {
                var next = musicCur == music ? musicB : music;
                if (musicPrev && musicPrev != musicCur) musicPrev.Stop();
                next.clip = tc; next.volume = 0f; next.time = 0f; next.Play();
                musicPrev = musicCur; musicCur = next; musicTrack = want;
            }
            musicCur.volume = Mathf.MoveTowards(musicCur.volume, targetMusic, udt * (focus || stingDuck > 0f ? 1.5f : .35f));
            if (musicPrev && musicPrev != musicCur) { musicPrev.volume = Mathf.MoveTowards(musicPrev.volume, 0f, udt * .45f); if (musicPrev.volume <= .001f) { musicPrev.Stop(); musicPrev = null; } }
        }

        /// Which piece of the score fits right now.
        string WantedTrack()
        {
            wantT -= Time.unscaledDeltaTime;
            if (wantT > 0f && wantCache != null) return wantCache;
            wantT = .25f;
            string w;
            var gm = GameManager.I;
            if (rankingScene) w = "ranking";
            else if (MamaFrontEnd.Active) w = "menu";
            else if (gm)
            {
                switch (gm.state)
                {
                    case GameManager.State.Playing: w = gm.TimeLeft <= 90f ? "house_tense" : "house_calm"; break;   // Mom is close: same theme, double time
                    case GameManager.State.MomHome: w = "mom_home"; break;
                    case GameManager.State.Ending: w = MamaRound.Data != null && MamaRound.Data.won ? "ranking" : "mom_home"; break;
                    default: w = "house_calm"; break;
                }
            }
            else w = musicTrack ?? "menu";
            return wantCache = w;
        }

        // ================================================================== synthesis
        float R() => (float)rng.NextDouble() * 2f - 1f;
        AudioClip Make(string n, float[] d) { var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); clips[n] = c; return c; }
        static float Env(float t, float a, float dur) => t < a ? t / a : Mathf.Exp(-(t - a) / Mathf.Max(0.001f, dur));

        void BuildClips()
        {
            BuildMusic();
            // ---- loops
            clips["water"] = Make("water", SoftWater(4f));
            clips["gurgleLoop"] = Make("gurgleLoop", Gurgle(2f, true));
            clips["hiss"] = Make("hiss", NoiseLoop(1.5f, 0.12f, 0.25f, 0f, 0.6f));
            clips["fire"] = Make("fire", Fire(3f));
            // ---- one-shots
            Make("swish", Swish(0.32f, 0.35f, 0.75f));
            Make("squish", Squish());
            Make("splash", Splash(0.5f));
            Make("step", Splash(0.12f, 0.35f));
            Make("thud", Tone(0.25f, 70f, 40f, 0.02f, 0.08f, 0.8f, 0.3f));
            Make("pop", Tone(0.12f, 520f, 900f, 0.002f, 0.04f, 0.6f, 0f));
            Make("place", Tone(0.16f, 180f, 120f, 0.002f, 0.04f, 0.6f, 0.25f));
            Make("squeak", Tone(0.28f, 1200f, 1650f, 0.01f, 0.18f, 0.35f, 0.02f, 0.5f));
            Make("creak", Creak());
            Make("click", Tone(0.05f, 2200f, 1500f, 0.001f, 0.012f, 0.6f, 0.4f));
            Make("good", Chime(new[] { 72, 76, 79 }, 0.09f, 0.6f));
            Make("bad", Tone(0.35f, 220f, 140f, 0.01f, 0.2f, 0.45f, 0.05f, 0f, true));
            Make("win", Chime(new[] { 67, 72, 76, 79, 84 }, 0.13f, 1.4f));
            Make("lose", Chime(new[] { 72, 68, 65, 60 }, 0.22f, 1.2f));
            Make("beep", Tone(0.22f, 3100f, 3100f, 0.002f, 0.4f, 0.35f, 0f, 0f, true));
            Make("ring", Ring());
            Make("gurgle", Gurgle(0.9f, false));
            Make("whoosh", Swish(0.6f, 0.15f, 0.55f));
            Make("door", Tone(0.4f, 110f, 80f, 0.005f, 0.12f, 0.7f, 0.35f));
            Make("slam", Tone(0.35f, 90f, 50f, 0.001f, 0.09f, 1f, 0.55f));
            Make("car", Car());
            Make("flare", Swish(0.9f, 0.03f, 0.14f));
            Make("wring", Splash(0.7f, 0.25f));
            // ---- party / co-op
            Make("zap", Zap());
            Make("slap", Splash(0.14f, 0.5f));
            Make("steam", Swish(1.6f, 0.1f, 0.3f));
            Make("squeakhi", Tone(0.12f, 2600f, 3200f, 0.005f, 0.05f, 0.25f, 0.02f, 1f));
            Make("thwop", Tone(0.22f, 160f, 60f, 0.003f, 0.08f, 0.8f, 0.3f));
            Make("ratchet", Ratchet());
            Make("crack", Tone(0.15f, 900f, 300f, 0.001f, 0.03f, 0.6f, 0.7f));
            Make("rip", Swish(0.28f, 0.2f, 0.6f));
            clips["party"] = Make("party", PartyLoop());
            clips["buzz"] = Make("buzz", Buzz());
            clips["sizzle"] = Make("sizzle", Fire(2f));
            clips["whir"] = Make("whir", NoiseLoop(2f, 0.04f, 0.09f, 3f, 0.7f));
            clips["sparkle"] = Make("sparkle", Sparkle());
            clips["hum"] = Make("hum", Hum());
        }

        // Original 108 bpm sneak/comedy loop: sparse plucks, short bass, quiet brushed pulse.
        void BuildMusic()
        {
            float beat = 60f / 108f; int bars = 8; float len = bars * 4 * beat;
            int n = (int)(len * SR); var d = new float[n];
            int[][] chords = { new[] { 50, 57, 60, 65 }, new[] { 48, 55, 58, 64 }, new[] { 46, 53, 57, 62 }, new[] { 45, 52, 55, 61 } };
            int[] mel = { 74, -1, 77, -1, 76, 74, -1, 69, 72, -1, 76, -1, 74, -1, 67, -1, 74, -1, 77, 76, -1, 74, -1, 69, 73, -1, 76, -1, 79, 76, -1, -1 };
            float F(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
            for (int b = 0; b < bars; b++)
            {
                var ch = chords[b % 4]; float t0 = b * 4 * beat;
                foreach (var m in ch)
                {
                    float f = F(m); int s0 = (int)(t0 * SR), s1 = Mathf.Min(n, (int)((t0 + 4 * beat + 0.6f) * SR));
                    for (int s = s0; s < s1; s++)
                    {
                        float t = (s - s0) / (float)SR; float env = Mathf.Min(1f, t / 0.8f) * Mathf.Min(1f, (s1 - s) / (0.8f * SR));
                        float ph = 2 * Mathf.PI * f * t;
                        d[s % n] += (Mathf.Sin(ph) + 0.25f * Mathf.Sin(ph * 2.003f) + 0.1f * Mathf.Sin(ph * 0.5f)) * env * 0.022f;
                    }
                }
                // bass on beats 1 and 3
                for (int k = 0; k < 4; k++) Pluck(d, t0 + (k + (k%2==1?.08f:0))*beat, F(ch[0] - 12 + (k==3?7:0)), .6f, .12f, .3f);
                // melody (half-beat notes, 8 per bar, sparse)
                for (int k = 0; k < 8; k++)
                {
                    int m = mel[(b * 8 + k) % mel.Length]; if (m < 0) continue;
                    Pluck(d, t0 + k * beat * 0.5f + (k%2==1?beat*.08f:0), F(m), .45f, .075f, .5f);
                }
                for(int k=0;k<4;k++)
                {
                    int s0=(int)((t0+k*beat)*SR);
                    for(int s=0;s<(int)(.12f*SR)&&s0+s<n;s++)
                    {
                        float t=s/(float)SR;
                        d[s0+s] += k%2==0 ? Mathf.Sin(2*Mathf.PI*(65*t-18*t*t))*Mathf.Exp(-t*35)*.065f : R()*Mathf.Exp(-t*65)*.018f;
                    }
                }
                // soft shaker on off-beats
                for (int k = 0; k < 8; k++)
                {
                    int s0 = (int)((t0 + k * beat * 0.5f + beat * 0.25f) * SR);
                    for (int s = 0; s < 1800 && s0 + s < n; s++) d[s0 + s] += R() * Mathf.Exp(-s / 300f) * 0.025f;
                }
            }
            float lp = 0; for (int i = 0; i < n; i++) { lp += (d[i] - lp) * 0.35f; d[i] = Mathf.Clamp(lp, -1f, 1f); }
            Make("music", d);
        }

        void Pluck(float[] d, float t0, float f, float dur, float amp, float bright)
        {
            int s0 = (int)(t0 * SR), len = (int)(dur * SR);
            for (int s = 0; s < len; s++)
            {
                float t = s / (float)SR; float e = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * 3.2f);
                float ph = 2 * Mathf.PI * f * t;
                d[(s0 + s) % d.Length] += (Mathf.Sin(ph) + bright * 0.3f * Mathf.Sin(ph * 2f) * Mathf.Exp(-t * 6f) + 0.12f * Mathf.Sin(ph * 3f) * Mathf.Exp(-t * 9f)) * e * amp;
            }
        }

        float[] NoiseLoop(float dur, float lpA, float lpB, float modHz, float amp)
        {
            int n = (int)(dur * SR); var d = new float[n]; float a = 0, b = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float k = Mathf.Lerp(lpA, lpB, 0.5f + 0.5f * Mathf.Sin(t * modHz * 2 * Mathf.PI / Mathf.Max(1f, modHz) * Mathf.Round(modHz) + Mathf.Sin(t * 7.3f)));
                a += (R() - a) * k; b += (a - b) * 0.5f;
                float bubbles = modHz > 0 && rng.NextDouble() < 0.0009 ? 1f : 0f;
                d[i] = (a - b * 0.6f) * amp + bubbles * 0.2f;
            }
            return CrossFade(d, 0.1f);
        }

        // warm, low running water: brown-ish noise through two low-pass stages + soft low bubbles
        float[] SoftWater(float dur)
        {
            // Physical tap model: a stream hitting water = thousands of tiny bubbles (Minnaert resonances,
            // pitch gliding up as they collapse) over a soft low-passed splash bed.
            int n = (int)(dur * SR); var d = new float[n]; float a = 0, b = 0;
            for (int i = 0; i < n; i++) { a += (R() - a) * 0.12f; b += (a - b) * 0.2f; d[i] = b * 0.45f; }
            int bub = (int)(dur * 420);
            for (int k = 0; k < bub; k++)
            {
                int s0 = rng.Next(n);
                float r = (float)rng.NextDouble();
                float f0 = Mathf.Lerp(380f, 1500f, r * r);            // most bubbles small-ish & mid pitched
                float amp = Mathf.Lerp(0.05f, 0.012f, r) * (0.5f + (float)rng.NextDouble());
                float decay = 0.004f + (1f - r) * 0.012f;
                int len = (int)(decay * 5f * SR); float ph = 0;
                for (int s = 0; s < len; s++)
                {
                    float t = s / (float)SR; float f = f0 * (1f + 3.5f * t / decay * 0.05f);
                    ph += 2 * Mathf.PI * f / SR;
                    d[(s0 + s) % n] += Mathf.Sin(ph) * amp * Mathf.Exp(-t / decay);
                }
            }
            float lp = 0; for (int i = 0; i < n; i++) { lp += (d[i] - lp) * 0.55f; d[i] = lp * 0.9f; }
            return CrossFade(d, 0.3f);
        }

        // cozy campfire: low rumble + soft, dull pops (no sharp crackle)
        float[] Fire(float dur)
        {
            int n = (int)(dur * SR); var d = new float[n]; float a = 0, b = 0, rum = 0;
            for (int i = 0; i < n; i++)
            {
                a += (R() - a) * 0.03f; b += (a - b) * 0.08f; rum += (R() - rum) * 0.006f;
                d[i] = b * 0.9f + rum * 2.2f;
            }
            int pops = (int)(dur * 7);
            for (int k = 0; k < pops; k++)
            {
                int s0 = rng.Next(n); float amp = 0.06f + (float)rng.NextDouble() * 0.12f; int len = 300 + rng.Next(700); float f0 = 120f + (float)rng.NextDouble() * 180f;
                for (int s = 0; s < len; s++) d[(s0 + s) % n] += Mathf.Sin(2 * Mathf.PI * f0 * s / SR) * amp * Mathf.Exp(-s / (len * 0.25f));
            }
            float lp = 0; for (int i = 0; i < n; i++) { lp += (d[i] - lp) * 0.25f; d[i] = lp; }
            return CrossFade(d, 0.25f);
        }

        float[] FireOld(float dur)
        {
            int n = (int)(dur * SR); var d = new float[n]; float a = 0, rum = 0;
            for (int i = 0; i < n; i++)
            {
                a += (R() - a) * 0.08f; rum += (R() - rum) * 0.01f;
                d[i] = a * 0.35f + rum * 1.2f;
            }
            int cracks = (int)(dur * 26);
            for (int c = 0; c < cracks; c++)
            {
                int s0 = rng.Next(n); float amp = 0.2f + (float)rng.NextDouble() * 0.6f; int len = 80 + rng.Next(500);
                for (int s = 0; s < len; s++) d[(s0 + s) % n] += R() * amp * Mathf.Exp(-s / (len * 0.2f));
            }
            return CrossFade(d, 0.1f);
        }

        float[] Gurgle(float dur, bool loop)
        {
            int n = (int)(dur * SR); var d = new float[n];
            int bub = (int)(dur * (loop ? 14 : 22));
            for (int c = 0; c < bub; c++)
            {
                int s0 = rng.Next(n); float f0 = 180f + (float)rng.NextDouble() * 500f; int len = 900 + rng.Next(1500); float amp = 0.15f + (float)rng.NextDouble() * 0.3f;
                for (int s = 0; s < len; s++) { float t = s / (float)SR; float f = f0 * (1f + t * 6f); d[(s0 + s) % n] += Mathf.Sin(2 * Mathf.PI * f * t) * amp * Mathf.Sin(Mathf.PI * s / len); }
            }
            float a = 0; for (int i = 0; i < n; i++) { a += (R() - a) * 0.05f; d[i] += a * 0.25f; }
            if (loop) return CrossFade(d, 0.1f);
            FadeOut(d, 0.2f); return d;
        }

        float[] Swish(float dur, float lp0, float lp1)
        {
            int n = (int)(dur * SR); var d = new float[n]; float a = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n; float k = Mathf.Lerp(lp0, lp1, Mathf.Sin(t * Mathf.PI));
                a += (R() - a) * k; d[i] = a * Mathf.Sin(t * Mathf.PI) * 0.7f;
            }
            return d;
        }

        float[] Squish()
        {
            int n = (int)(0.38f * SR); var d = new float[n]; float a = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; a += (R() - a) * 0.25f;
                float wob = Mathf.Sin(2 * Mathf.PI * (90f + 60f * Mathf.Sin(t * 30f)) * t);
                d[i] = (a * 0.5f + wob * 0.25f) * Env(t, 0.03f, 0.1f);
            }
            for (int c = 0; c < 6; c++) { int s0 = rng.Next(n / 2); for (int s = 0; s < 600 && s0 + s < n; s++) d[s0 + s] += Mathf.Sin(s * 0.18f * (1 + s * 0.002f)) * 0.15f * Mathf.Exp(-s / 160f); }
            return d;
        }

        float[] Splash(float dur, float amp = 0.7f)
        {
            int n = (int)(dur * SR); var d = new float[n]; float a = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)SR; a += (R() - a) * 0.4f; d[i] = a * Env(t, 0.005f, dur * 0.3f) * amp; }
            for (int c = 0; c < (int)(dur * 40); c++)
            {
                int s0 = rng.Next(n); float f0 = 600f + (float)rng.NextDouble() * 1400f; int len = 300 + rng.Next(700);
                for (int s = 0; s < len && s0 + s < n; s++) { float t = s / (float)SR; d[s0 + s] += Mathf.Sin(2 * Mathf.PI * f0 * (1 + t * 8f) * t) * 0.12f * amp * (1f - s / (float)len); }
            }
            return d;
        }

        float[] Tone(float dur, float f0, float f1, float att, float dec, float amp, float noise, float vib = 0f, bool square = false)
        {
            int n = (int)(dur * SR); var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; float f = Mathf.Lerp(f0, f1, t / dur) * (1f + vib * 0.03f * Mathf.Sin(t * 40f));
                ph += 2 * Mathf.PI * f / SR;
                float w = square ? Mathf.Sign(Mathf.Sin(ph)) * 0.5f : Mathf.Sin(ph);
                d[i] = (w * (1 - noise) + R() * noise) * Env(t, att, dec) * amp;
            }
            FadeOut(d, 0.02f); return d;
        }

        float[] Chime(int[] notes, float gap, float tail)
        {
            int n = (int)((notes.Length * gap + tail) * SR); var d = new float[n];
            for (int k = 0; k < notes.Length; k++) Pluck(d, k * gap, 440f * Mathf.Pow(2f, (notes[k] - 69) / 12f), tail, 0.28f, 0.9f);
            FadeOut(d, 0.1f); return d;
        }

        float[] Creak()
        {
            int n = (int)(0.45f * SR); var d = new float[n];
            int period = 0; for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; float f = 55f + 30f * Mathf.Sin(t * 9f);
                if (--period <= 0) { period = (int)(SR / f); d[i] = 0.7f; }
                d[i] += R() * 0.05f;
            }
            float a = 0; for (int i = 0; i < n; i++) { a += (d[i] - a) * 0.3f; d[i] = a * Mathf.Sin(Mathf.PI * i / n) * 1.2f; }
            return d;
        }

        float[] Ring()
        {
            int n = (int)(1.6f * SR); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; bool on = (t % 0.8f) < 0.55f;
                float trem = 0.5f + 0.5f * Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * 20f));
                d[i] = on ? (Mathf.Sin(2 * Mathf.PI * 1300f * t) + Mathf.Sin(2 * Mathf.PI * 1560f * t)) * 0.18f * trem : 0f;
            }
            return d;
        }

        float[] Zap()
        {
            int n = (int)(0.35f * SR); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = (Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 120f * t)) * 0.3f + R() * 0.5f) * Env(t, 0.002f, 0.08f) * (0.6f + 0.4f * Mathf.Sign(Mathf.Sin(t * 180f))); }
            return d;
        }
        float[] Ratchet()
        {
            int n = (int)(0.25f * SR); var d = new float[n];
            for (int k = 0; k < 4; k++) { int s0 = (int)(k * 0.05f * SR); for (int s = 0; s < 500 && s0 + s < n; s++) d[s0 + s] += R() * Mathf.Exp(-s / 70f) * 0.7f; }
            return d;
        }
        // muffled party music heard through the walls: kick, bass line, hats, 120 bpm
        float[] PartyLoop()
        {
            float beat = 0.5f; int n = (int)(beat * 16 * SR); var d = new float[n];
            int[] bass = { 36, 36, 43, 41, 36, 36, 46, 43 };
            for (int b = 0; b < 16; b++)
            {
                int s0 = (int)(b * beat * SR);
                for (int s = 0; s < (int)(0.25f * SR); s++) { float t = s / (float)SR; d[(s0 + s) % n] += Mathf.Sin(2 * Mathf.PI * (50f + 90f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 9f) * 0.75f; }
                float f = 440f * Mathf.Pow(2f, (bass[(b / 2) % 8] - 69) / 12f);
                int h0 = (int)((b + 0.5f) * beat * SR);
                for (int s = 0; s < (int)(beat * 0.45f * SR); s++) { float t = s / (float)SR; d[(h0 + s) % n] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t)) * Mathf.Exp(-t * 4f) * 0.35f; }
                for (int s = 0; s < 400; s++) d[(h0 + s) % n] += R() * Mathf.Exp(-s / 60f) * 0.06f;
            }
            float lp = 0; for (int i = 0; i < n; i++) { lp += (d[i] - lp) * 0.12f; d[i] = lp * 1.4f; }
            return d;
        }
        float[] Buzz()
        {
            int n = SR; var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = (Mathf.Sin(2 * Mathf.PI * 100f * t) * 0.3f + Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 100f * t)) * 0.08f + R() * 0.03f); }
            return d;
        }
        float[] Hum()
        {
            int n = SR * 2; var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = Mathf.Sin(2 * Mathf.PI * 60f * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * 120f * t) * 0.08f; }
            return d;
        }
        float[] Sparkle()
        {
            int n = SR * 2; var d = new float[n]; float a = 0;
            for (int i = 0; i < n; i++) { a += (R() - a) * 0.5f; d[i] = a * 0.05f; }
            for (int k = 0; k < 160; k++) { int s0 = rng.Next(n); for (int s = 0; s < 120; s++) d[(s0 + s) % n] += R() * Mathf.Exp(-s / 25f) * 0.25f; }
            return CrossFade(d, 0.1f);
        }

        float[] Car()
        {
            int n = (int)(2.5f * SR); var d = new float[n]; float a = 0, ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; float f = 38f + 8f * Mathf.Sin(t * 1.5f);
                ph += 2 * Mathf.PI * f / SR; a += (R() - a) * 0.05f;
                float vol = Mathf.Sin(Mathf.PI * t / 2.5f);
                d[i] = (Mathf.Sin(ph) * 0.4f + Mathf.Sin(ph * 2f) * 0.2f + a * 0.6f) * vol * 0.6f;
            }
            return d;
        }

        static float[] CrossFade(float[] d, float secs)
        {
            int m = Mathf.Min(d.Length / 2, (int)(secs * SR));
            for (int i = 0; i < m; i++) { float k = i / (float)m; d[i] = d[i] * k + d[d.Length - m + i] * (1 - k); }
            var o = new float[d.Length - m]; System.Array.Copy(d, o, o.Length); return o;
        }
        static void FadeOut(float[] d, float secs)
        {
            int m = Mathf.Min(d.Length, (int)(secs * SR));
            for (int i = 0; i < m; i++) d[d.Length - 1 - i] *= i / (float)m;
        }
    }
}
