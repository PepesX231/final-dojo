using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Import rules for the game's audio: the score streams from disk (small memory), short foley is decompressed
    /// for instant playback, loops stay compressed in memory.
    public class MamaAudioImport : AssetPostprocessor
    {
        [MenuItem("Mama's Coming/Reimport game audio")]
        public static void Reimport()
        {
            foreach (var dir in new[] { "Assets/MamasComing/Resources/MamaMusic", "Assets/MamasComing/Resources/MamaFoley" })
                foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { dir })) AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
            Debug.Log("[Audio] reimported");
        }

        void OnPreprocessAudio()
        {
            bool music = assetPath.Contains("/Resources/MamaMusic/");
            bool foley = assetPath.Contains("/Resources/MamaFoley/");
            if (!music && !foley) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            if (music) { s.loadType = AudioClipLoadType.Streaming; s.quality = .7f; ai.forceToMono = false; ai.loadInBackground = true; }
            else
            {
                string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                bool loop = n == "water" || n == "fire" || n == "hiss" || n == "gurgleLoop" || n == "hum" || n == "whir" || n == "sizzle" || n == "sparkle" || n == "party";
                s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.quality = .6f; ai.forceToMono = true;
            }
            s.preloadAudioData = true;
            ai.defaultSampleSettings = s;
        }
    }
}
