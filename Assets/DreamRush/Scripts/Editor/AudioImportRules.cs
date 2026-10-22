using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M15 audio import rules: MUS_* loops stream (Vorbis q 0.6, no preload spike), SFX_* stay small and decompressed for
    /// zero-latency one-shots (ADPCM, mono).
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/DreamRush/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            string file = System.IO.Path.GetFileName(assetPath);
            if (file.StartsWith("MUS_"))
            {
                s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.6f;
                imp.loadInBackground = true;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad; s.compressionFormat = AudioCompressionFormat.ADPCM;
                imp.forceToMono = true;
            }
            imp.defaultSampleSettings = s;
        }

        [MenuItem("DreamRush/Tools/Reimport Audio")]
        public static void Reimport() => AssetDatabase.ImportAsset("Assets/DreamRush/Resources/DreamRush/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
    }
}
