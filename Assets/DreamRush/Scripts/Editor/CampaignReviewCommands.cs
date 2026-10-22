using System;
using System.IO;
using System.Linq;
using DreamRush.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DreamRush.EditorTools
{
    public static class CampaignReviewCommands
    {
        public static string BuildWindows(string directory){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play before building");
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
            var scenes=new[]{"BedroomHub","DreamGalaxy"}.Concat(CampaignRoute.Scenes).Select(n=>"Assets/DreamRush/Scenes/"+n+".unity").ToArray();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(directory,"DREAM_RUSH.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
            var summary=report.summary;
            string result=$"result={summary.result}\nerrors={summary.totalErrors}\nwarnings={summary.totalWarnings}\nbytes={summary.totalSize}\nseconds={summary.totalTime.TotalSeconds:F1}\nexecutable={summary.outputPath}\nscenes={string.Join(",",scenes)}\n";
            File.WriteAllText("DevBridge/out/campaign-windows-build.txt",result);
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException(result);
            return result;
        }
        public static string AttachPalaceArrival(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play first");
            const string path="Assets/DreamRush/Scenes/Dream_MidnightPalace.unity";
            string backup=Path.Combine("DevBridge/out","Palace-before-campaign-arrival.unity");
            if(!File.Exists(backup))File.Copy(path,backup);
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var arrival=Object.FindFirstObjectByType<ExistingWorldArrival>();
            if(arrival==null)arrival=new GameObject("PalaceArrivalCinema").AddComponent<ExistingWorldArrival>();
            arrival.focus=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="ClockTower");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Saved Palace arrival only; existing M21 city/interiors preserved. Backup="+backup;
        }
    }
}
