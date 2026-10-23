using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace MamasComing.Editor
{
    /// <summary>Explicitly staged art views, separate from normal gameplay captures.</summary>
    public static class MamaRecastEvidence
    {
        const string Folder="DevBridge/mama-rebuild/";
        public static void HeldCloseup()
        {
            var p=GameManager.I.player;
            Render("staged-hold-"+p.Held.displayName.Replace(" ","-").ToLowerInvariant(),p.transform.position+p.transform.forward*2.2f+p.transform.right*1.2f+Vector3.up*1.4f,p.transform.position+Vector3.up*.65f);
        }
        public static void MotherPreview()
        {
            var source=GameManager.I.mom.GetComponent<MotherApplePresentation>();
            var stage=new GameObject("Temporary Mother Apple art stage");stage.transform.position=new Vector3(60,0,60);
            try
            {
                foreach(bool angry in new[]{false,true})
                {
                    var actor=UnityEngine.Object.Instantiate(angry?source.angry:source.kind,stage.transform);
                    actor.SetActive(true);
                    Render(angry?"staged-mother-angry":"staged-mother-kind",stage.transform.position+new Vector3(.25f,1f,3.4f),stage.transform.position+Vector3.up*.9f);
                    UnityEngine.Object.DestroyImmediate(actor);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(stage);}
        }
        static void Render(string name,Vector3 eye,Vector3 target)
        {
            Directory.CreateDirectory(Folder);
            var go=new GameObject("Temporary evidence camera");var camera=go.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);camera.targetTexture=null;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.16f,.20f);
            camera.fieldOfView=42;camera.nearClipPlane=.03f;camera.farClipPlane=100;
            go.transform.position=eye;go.transform.LookAt(target);
            var rt=RenderTexture.GetTemporary(1280,1280,24);var previous=RenderTexture.active;
            var image=new Texture2D(1280,1280,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1280,1280),0,0);image.Apply();File.WriteAllBytes(Folder+name+".png",image.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
        }
        public static void ExportMusicPreview()
        {
            var source=MamaAudio.I.GetComponentsInChildren<AudioSource>().First(s=>s.name=="Music");var clip=source.clip;
            var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);
            double energy=0;float peak=0;
            using(var stream=File.Create(Folder+"music-preview.wav"))using(var writer=new BinaryWriter(stream))
            {
                int bytes=samples.Length*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)clip.channels);writer.Write(clip.frequency);writer.Write(clip.frequency*clip.channels*2);writer.Write((short)(clip.channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
                foreach(float sample in samples){if(float.IsNaN(sample)||float.IsInfinity(sample))throw new Exception("Non-finite music sample");peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;writer.Write((short)(Mathf.Clamp(sample,-1,1)*32767));}
            }
            File.WriteAllText(Folder+"music-measurement.txt","Peak="+peak+"\nRMS="+Math.Sqrt(energy/samples.Length)+"\nSeconds="+clip.length+"\nMeasured signal only; not a human listening review.\n");
        }
    }
}
