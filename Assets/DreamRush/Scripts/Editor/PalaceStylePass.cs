using System;
using System.IO;
using System.Linq;
using DreamRush.Palace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DreamRush.EditorTools
{
    /// <summary>Targeted, repeatable visual dressing. Never rebuilds the city or changes quest/collision geometry.</summary>
    public static class PalaceStylePass
    {
        const string TexDir = "Assets/DreamRush/Art/MidnightPalace/Textures";
        static Material wood, paper, ink, cream, teal, gold;
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        [MenuItem("DreamRush/Art/Apply detailed stylized Palace")]
        public static void MenuApply() { Debug.Log(Apply()); }
        public static string Apply() => ApplyCore(true);
        public static string DressBuiltScene() => ApplyCore(false);
        static string ApplyCore(bool save)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || (save && scene.path != MidnightPalaceBuilder.ScenePath))
                throw new InvalidOperationException("Open the saved Midnight Palace scene outside Play Mode first.");
            int collidersBefore = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
            Init();
            int cables = SupportLanterns();
            foreach (var beacon in Object.FindObjectsByType<HourBeacon>(FindObjectsSortMode.None))
            {
                var t = beacon.transform;
                // M17 cylinder's bottom is the actual objective, not the marker centre.
                if (t.localScale.y > 10f) t.position -= Vector3.up * (t.localScale.y - 2f);
                t.localScale = new Vector3(.65f, 2f, .65f);
                var m = t.GetComponent<Renderer>()?.sharedMaterial;
                if (m != null && m.HasProperty("_NearFade")) { m.SetFloat("_NearFade", 1f); EditorUtility.SetDirty(m); }
            }
            int rooms = 0;
            foreach (var room in Object.FindObjectsByType<InteriorRoom>(FindObjectsSortMode.None)) { Dress(room); rooms++; }
            var director = Object.FindFirstObjectByType<InteriorDirector>();
            if (director != null) {
                var visibility = director.GetComponent<PalaceDressingVisibility>();
                if(visibility == null) visibility = director.gameObject.AddComponent<PalaceDressingVisibility>();
                visibility.rooms.Clear();
                foreach(var room in Object.FindObjectsByType<InteriorRoom>(FindObjectsSortMode.None))
                    visibility.rooms.Add(new PalaceDressingVisibility.Entry { dressing=room.transform.Find("StyleDressing").gameObject, area=room.area });
                EditorUtility.SetDirty(visibility);
            }
            Facades();
            foreach (var clock in Object.FindObjectsByType<SkyClock>(FindObjectsSortMode.None))
            {
                for (int i = 0; i < clock.keys.Length; i++)
                {
                    var k = clock.keys[i];
                    // Sonny Boy / Pop-Surrealism palette: stark obsidian-indigo skies, electric cyan/magenta/gold accents
                    k.zenith = i == 0 ? C("070614") : i == 1 ? C("050410") : C("03020A");
                    k.horizon = i == 0 ? C("FF2A6D") : i == 1 ? C("7928CA") : C("00F0FF");
                    k.band = i == 0 ? C("FFE600") : i == 1 ? C("FF1E56") : C("7000FF");
                    k.light = i == 0 ? C("FFF3C4") : i == 1 ? C("FFD6E8") : C("B8F2FF");
                    k.ambSky = i == 0 ? C("2A1B54") : i == 1 ? C("1F1448") : C("120E36");
                    k.ambEq = i == 0 ? C("781D4E") : i == 1 ? C("5A185C") : C("2B1954");
                    k.ambGround = C("090716");
                    clock.keys[i] = k;
                }
                clock.Apply();
                EditorUtility.SetDirty(clock);
            }
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) {
                var profile = volume.sharedProfile; if(profile == null)continue;
                if(profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom)) { bloom.intensity.Override(.72f); bloom.threshold.Override(0.92f); bloom.scatter.Override(0.68f); }
                if(profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var color)) { color.postExposure.Override(0.12f); color.contrast.Override(36f); color.saturation.Override(32f); }
                if (!profile.TryGet<UnityEngine.Rendering.Universal.SplitToning>(out var st)) { st = profile.Add<UnityEngine.Rendering.Universal.SplitToning>(true); AssetDatabase.AddObjectToAsset(st, profile); }
                st.active = true; st.shadows.Override(C("120A38")); st.highlights.Override(C("FFE866")); st.balance.Override(-14f);
                EditorUtility.SetDirty(profile);
            }
            if (Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length != collidersBefore)
                throw new InvalidOperationException("Visual pass unexpectedly changed collider count; inspect before saving.");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); if(save) EditorSceneManager.SaveScene(scene);
            return $"Palace visual pass saved: {rooms} rooms, {cables} supported lantern spans. Gameplay links and colliders preserved.";
        }
        static void Init()
        {
            wood = Surface("M_MP_StyleWood", C("B99168"), Pattern("Wood", 0), 2.8f);
            paper = Surface("M_MP_StylePaper", C("F1E5CE"), Pattern("Paper", 1), 2f);
            ink = Surface("M_MP_StyleInk", C("39374D"));
            cream = Surface("M_MP_StyleCream", C("E9DDBD"));
            teal = Surface("M_MP_StyleTeal", C("538D88"));
            gold = Surface("M_MP_StyleBrass", C("B59A59"));
        }
        static Material Surface(string name, Color color, Texture2D tex = null, float metres = 0)
        {
            var m = ArtKit.Mat(name, color, rim: .03f, spec: .02f, rampSoft: .65f);
            m.SetTexture("_BaseMap", tex); m.SetFloat("_BaseMapStrength", tex != null ? .85f : 0f);
            m.SetFloat("_WorldUV", metres); m.SetColor("_Emission", Color.black);
            m.SetFloat("_EmissionMapStrength", 0); EditorUtility.SetDirty(m); return m;
        }
        // Small, deterministic painted patterns, generated once as project assets. No random grime or imported art.
        static Texture2D Pattern(string name, int kind)
        {
            string path = TexDir + "/T_MP_Style" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null) return existing;
            const int N = 256; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float v;
                if (kind == 0) {
                    int plank = y / 64; float wave = Mathf.Sin(x * .065f + Mathf.Sin(y * .085f) * .7f);
                    v = .87f + wave * .025f + (plank % 2) * .04f;
                    if (y % 64 < 2 || (x + plank * 83) % 256 < 2) v = .64f;
                } else v = .94f + Mathf.Sin(x * .15f) * Mathf.Sin(y * .17f) * .012f;
                tex.SetPixel(x, y, new Color(v,v,v,1));
            }
            tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.anisoLevel = 4; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static GameObject Shape(Transform p, string name, Vector3 at, Vector3 size, Material mat, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(p,false); go.transform.localPosition=at; go.transform.localScale=size;
            var r=go.GetComponent<Renderer>(); r.sharedMaterial=mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }
        static Transform Group(Transform p, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(p,false); return t;
        }
        static void Line(Transform p, string name, Vector3[] points, float width, Material mat)
        {
            var go = new GameObject(name); go.transform.SetParent(p,false);
            var l=go.AddComponent<LineRenderer>(); l.useWorldSpace=false; l.positionCount=points.Length; l.SetPositions(points);
            l.startWidth=l.endWidth=width; l.sharedMaterial=mat; l.numCapVertices=2; l.generateLightingData=true; l.shadowCastingMode=ShadowCastingMode.Off;
        }
        static int SupportLanterns()
        {
            int count=0;
            // Only decorative street spans. Bridge suspension and grind rails are deliberately excluded by hierarchy.
            foreach (var cable in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Cable" && t.parent != null && t.parent.name=="Signatures").ToArray())
            {
                var parent=cable.parent;
                if(parent.Find("SupportedSpan_"+cable.GetSiblingIndex()) != null) continue;
                var g=Group(parent,"SupportedSpan_"+cable.GetSiblingIndex()); g.position=cable.position;
                bool x=cable.localScale.x>cable.localScale.z; Vector3 along=x?cable.right:cable.forward;
                float span=Mathf.Max(cable.lossyScale.x,cable.lossyScale.z), height=cable.position.y;
                // Reject intersection/canal spans: posts must sit on sidewalks, never in a crossing or the water.
                bool safe=true; float[] roads={-120,-70,-20,30,80,130};
                foreach(int side in new[]{-1,1}) {
                    var end=cable.position+along*(span*.5f*side);
                    if(roads.Any(v=>Mathf.Abs((x?end.z:end.x)-v)<8f) || (Mathf.Abs(end.x)<41 && end.z>84 && end.z<151)) safe=false;
                }
                var lamps=parent.Cast<Transform>().Where(t=>t.name=="Lantern" && Mathf.Abs(Vector3.Dot(t.position-cable.position,along))<span*.5f+.1f && Vector3.ProjectOnPlane(t.position-cable.position,along).sqrMagnitude<5f).ToArray();
                cable.gameObject.SetActive(false);
                if(!safe) { foreach(var l in lamps) l.gameObject.SetActive(false); Object.DestroyImmediate(g.gameObject); continue; }
                for(int side=-1;side<=1;side+=2) {
                    Shape(g,"AnchoredPost",along*(side*span*.5f)-Vector3.up*(height*.5f),new Vector3(.18f,height,.18f),ink);
                    Shape(g,"PostFoot",along*(side*span*.5f)-Vector3.up*(height-.12f),new Vector3(.5f,.24f,.5f),gold);
                    Shape(g,"PostCap",along*(side*span*.5f)+Vector3.up*.08f,Vector3.one*.27f,gold);
                }
                var pts=new Vector3[17]; for(int i=0;i<pts.Length;i++) { float t=i/16f-.5f; pts[i]=along*(t*span)-Vector3.up*((.25f-t*t)*2.4f); }
                Line(g,"SaggingWire",pts,.045f,ink);
                foreach(var lamp in lamps) {
                    float u=Vector3.Dot(lamp.position-cable.position,along); float t=u/span;
                    var a=along*u-Vector3.up*((.25f-t*t)*2.4f); var b=g.InverseTransformPoint(lamp.position+Vector3.up*.375f);
                    Line(g,"LanternDrop",new[]{a,b},.028f,ink);
                }
                count++;
            }
            // These old banner cards have no bracket or gameplay purpose. Remove their unsupported presentation only.
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>(t.name=="Banner"||t.name=="BannerBack")&&t.parent!=null&&t.parent.name=="Signatures")) t.gameObject.SetActive(false);
            return count;
        }
        static void Dress(InteriorRoom room)
        {
            var old=room.transform.Find("StyleDressing"); if(old!=null) Object.DestroyImmediate(old.gameObject);
            var floor=room.transform.Find("Floor"); var ceiling=room.transform.Find("Ceiling"); if(floor==null||ceiling==null)return;
            float w=floor.localScale.x,d=floor.localScale.z,h=ceiling.localPosition.y-.2f;
            floor.GetComponent<Renderer>().sharedMaterial=wood;
            var light = room.transform.Find("RoomLight")?.GetComponent<Light>();
            if(light != null) { light.color = C("FFEFD9"); light.intensity = .85f; light.range = Mathf.Min(w,d) * .65f; }

            var wall=Surface("M_MP_StyleWall"+room.type,room.type==RoomType.Dojo?C("7F9399"):room.type==RoomType.Cafe?C("D8C7B0"):room.type==RoomType.Library?C("A1B7B4"):C("B8B2C7"),Pattern("Paper",1),2f);
            foreach(string n in new[]{"WallBack","WallL","WallR","Ceiling"}) {var t=room.transform.Find(n);if(t!=null)t.GetComponent<Renderer>().sharedMaterial=wall;}
            var g=Group(room.transform,"StyleDressing");
            // Shallow architecture sits against the existing solid walls; all entry, puzzle and combat routes stay clear.
            for(int side=-1;side<=1;side+=2) {
                Shape(g,"Skirting",new Vector3(side*(w*.5f-.025f),.16f,0),new Vector3(.08f,.27f,d),wood);
                Shape(g,"Cornice",new Vector3(side*(w*.5f-.04f),h-.28f,0),new Vector3(.15f,.18f,d),cream);
                for(float z=-d*.5f+2.4f;z<d*.5f-1.5f;z+=4f) {
                    Shape(g,"WallPilaster",new Vector3(side*(w*.5f-.07f),1.5f,z),new Vector3(.12f,2.6f,.12f),wood);
                    var frame=Shape(g,"FramedPrint",new Vector3(side*(w*.5f-.12f),2.25f,z+1.3f),new Vector3(.1f,1.25f,1.5f),ink);
                    Shape(frame.transform,"Paper",new Vector3(-side*.55f,0,0),new Vector3(.12f,.86f,.87f),paper);
                    // Abstract clock discs, sized relative to frame; no illegible text or borrowed logos.
                    var disc=Shape(g,"ClockPrint",new Vector3(side*(w*.5f-.19f),2.25f,z+1.3f),new Vector3(.75f,.025f,.75f),gold,PrimitiveType.Cylinder);
                    disc.transform.localRotation=Quaternion.Euler(0,0,90);
                }
            }
            Shape(g,"BackSkirting",new Vector3(0,.16f,d*.5f-.025f),new Vector3(w,.27f,.08f),wood);
            Shape(g,"BackCornice",new Vector3(0,h-.28f,d*.5f-.04f),new Vector3(w,.18f,.15f),cream);
            // M21: pendants now come from the interior kit (INT_Pendant) placed by the room composition.
            // Restyle existing furnishings, retaining their colliders, spots and interactions.
            foreach(var t in room.GetComponentsInChildren<Transform>()) {
                if(t.IsChildOf(g))continue;
                var rr=t.GetComponent<Renderer>();if(rr==null)continue;
                if(t.name=="Table"||t.name=="Bar"||t.name=="MapTable"||t.name=="Arena"||t.name=="Pillar") rr.sharedMaterial=wood;
                if(t.name=="Stool")rr.sharedMaterial=teal;
                if(t.name=="BarTop")rr.sharedMaterial=cream;
                if(t.name=="RingNeon")rr.sharedMaterial=gold;
                if(t.name=="Table") {
                    var p=room.transform.InverseTransformPoint(t.position); p.y+=t.localScale.y;
                    Shape(g,"Coaster",p+new Vector3(.35f,.015f,0),new Vector3(.28f,.02f,.28f),teal,PrimitiveType.Cylinder);
                    Shape(g,"TeaCup",p+new Vector3(.35f,.11f,0),new Vector3(.17f,.1f,.17f),cream,PrimitiveType.Cylinder);
                }
            }
            if(room.type==RoomType.Library) {
                var table=room.transform.Find("MapTable");
                if(table!=null) {var p=table.localPosition+Vector3.up*.56f;
                    for(int i=0;i<4;i++) Shape(g,"ReferenceBook",p+new Vector3(-1.15f,i*.1f,.2f),new Vector3(.55f,.085f,.42f),i%2==0?teal:cream);
                    Shape(g,"MapBorder",p+new Vector3(0,.035f,-.85f),new Vector3(2.9f,.035f,.05f),gold);
                }
            }
            // Rugs define rest/reading zones without filling the central dojo or climbable vault.
            if(room.type==RoomType.Cafe||room.type==RoomType.Library) {
                var rug=Surface("M_MP_StyleRug",C("71918B"),Pattern("Paper",1),2f);
                Shape(g,"WovenRunner",new Vector3(0,.055f,-d*.22f),new Vector3(Mathf.Min(w*.5f,6f),.008f,2.4f),rug);
                for(int side=-1;side<=1;side+=2) Shape(g,"RunnerBorder",new Vector3(0,.061f,-d*.22f+side*1.05f),new Vector3(Mathf.Min(w*.5f,6f)-.2f,.004f,.045f),cream);
            }
            MergeDressing(g, room.door != null ? room.door.id : room.name);
            EditorUtility.SetDirty(room);
        }
        static void MergeDressing(Transform group, string key)
        {
            const string dir = "Assets/DreamRush/Art/MidnightPalace/StyleMeshes";
            GrayboxAssets.EnsureFolder(dir);
            key = string.Concat(key.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            var filters = group.GetComponentsInChildren<MeshFilter>();
            var batches = filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
            var data = batches.Select(b => new { mat = b.Key, combine = b.Select(f => new CombineInstance {
                mesh = f.sharedMesh, transform = group.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray() }).ToArray();
            for(int i=group.childCount-1;i>=0;i--) Object.DestroyImmediate(group.GetChild(i).gameObject);
            foreach(var b in data) {
                string path = dir + "/" + key + "_" + b.mat.name + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh == null) { mesh = new Mesh { name = key + "_" + b.mat.name, indexFormat = IndexFormat.UInt32 }; AssetDatabase.CreateAsset(mesh,path); }
                else mesh.Clear();
                mesh.CombineMeshes(b.combine,true,true); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                var go = new GameObject(b.mat.name); go.transform.SetParent(group,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=b.mat;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
        }
        static void Facades()
        {
            // Keep the established illustrated facade layouts and window masks, soften gritty texture contrast.
            foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{ArtKit.MatDir}).Select(AssetDatabase.GUIDToAssetPath)) {
                var m=AssetDatabase.LoadAssetAtPath<Material>(path); string n=m.name;
                if(!(n.StartsWith("M_MP_")||n.StartsWith("M_Kit_MP_"))||n.StartsWith("M_MP_Style")||n.Contains("Paper_"))continue;
                if(m.HasProperty("_SpecStrength"))m.SetFloat("_SpecStrength",Mathf.Min(m.GetFloat("_SpecStrength"),.12f));
                if(m.HasProperty("_RimStrength"))m.SetFloat("_RimStrength",Mathf.Min(m.GetFloat("_RimStrength"),.12f));
                if(new[]{"M_MP_Concrete","M_MP_Balcony","M_MP_Industrial","M_MP_Glass2"}.Contains(n)||n.StartsWith("M_MP_Panel")) {
                    // M21: v3 facades are already illustrated (flat cel + ink): show them as painted
                    m.SetFloat("_BaseMapStrength",1f); m.SetColor("_BaseColor",n.StartsWith("M_MP_Panel")?m.GetColor("_BaseColor"):Color.white);
                }
                if(m.HasProperty("_Emission")) {
                    var e=m.GetColor("_Emission"); float max=Mathf.Max(e.r,e.g,e.b);
                    // Preserve colour and functional signs; clamp only excessive environmental emission.
                    float cap=n.Contains("Light")||n.Contains("LampGlow")? .8f : 1.1f;
                    if(max>cap)m.SetColor("_Emission",e*(cap/max));
                }
                EditorUtility.SetDirty(m);
            }
        }
    }
}
