using System;
using System.IO;
using System.Security.Cryptography;
using DreamRush.Core;
using NUnit.Framework;
using UnityEngine;
namespace DreamRush.Tests
{
    /// <summary>Outer guard for every PlayMode fixture, including legacy tests that never injected a save.</summary>
    [SetUpFixture]
    public class PlayModeSaveIsolation
    {
        SaveManager previous;
        string temporaryPath, realPath, realHash;
        static string Hash(string path) {
            if(!File.Exists(path))return "absent";
            using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
        [OneTimeSetUp] public void UseTemporarySuiteSave() {
            previous=SaveManager.Instance;
            realPath=Path.Combine(Application.persistentDataPath,"dreamrush_save.json");realHash=Hash(realPath);
            temporaryPath=Path.Combine(Path.GetTempPath(),"dreamrush_suite_"+Guid.NewGuid().ToString("N")+".json");
            var save=new SaveManager(temporaryPath);save.Load();SaveManager.SetInstance(save);
            Debug.Log("[Audit] PlayMode suite save isolated: "+temporaryPath);
        }
        [OneTimeTearDown] public void RestorePreviousSaveAndVerifyDisk() {
            SaveManager.SetInstance(previous);
            foreach(var p in new[]{temporaryPath,temporaryPath+".tmp",temporaryPath+".bak"})if(File.Exists(p))File.Delete(p);
            Assert.That(Hash(realPath),Is.EqualTo(realHash),"PlayMode suite changed the real Editor save on disk");
            Debug.Log("[Audit] Real Editor save SHA256 unchanged across PlayMode suite.");
        }
    }
}
