using System;
using DreamRush.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    /// <summary>M7: every sound the code can request exists (missing clips would be silent bugs).</summary>
    public class AudioAssetTests
    {
        [Test]
        public void EverySfx_HasAClip()
        {
            foreach (Sfx s in Enum.GetValues(typeof(Sfx)))
            {
                string n = AudioManager.FileName(s);
                bool ok = Resources.Load<AudioClip>("DreamRush/Audio/" + n) != null || Resources.Load<AudioClip>("DreamRush/Audio/" + n + "_0") != null;
                Assert.IsTrue(ok, "missing clip for " + s + " (" + n + ")");
            }
        }

        [Test]
        public void EveryMusic_HasALoop()
        {
            foreach (Music m in Enum.GetValues(typeof(Music)))
                if (m != Music.None) Assert.IsNotNull(Resources.Load<AudioClip>("DreamRush/Audio/MUS_" + m), "missing music " + m);
        }
    }
}
