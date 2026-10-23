using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing.Editor
{
    /// <summary>Targeted visual authoring. Does not rebuild the house or change interaction/collision.</summary>
    public static class MamaVisualPolish
    {
        const string Root = "Assets/MamasComing/Art/Polish";
        static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        static Texture2D wood, tile, wall, cloth, flame;

        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/MamasComing/Scenes/MamasKitchen.unity")
                throw new InvalidOperationException("Apply only to the authored MamasKitchen scene in Edit mode.");
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Textures");
            AssetDatabase.Refresh(); materials.Clear();
            var before = CollisionSignature();
            wood = Texture("Wood", 0); tile = Texture("Tile", 1); wall = Texture("Wallpaper", 2); cloth = Texture("Fabric", 3); flame = Texture("Flame", 4);
            var hard = Mesh("ENV_CraftedEdge"); var soft = Mesh("ENV_SoftCushion");
            var pot = Mesh("PROP_CraftedPotBody"); var lid = Mesh("PROP_CraftedLid");
            int surfaces = 0, rounded = 0;
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene || renderer.GetComponentInParent<PlayerController>() || renderer.GetComponentInParent<MomController>()) continue;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] && AssetDatabase.GetAssetPath(slots[i]).StartsWith("Assets/MamasComing/Materials/", StringComparison.Ordinal)) { slots[i] = Look(slots[i]); surfaces++; }
                renderer.sharedMaterials = slots;
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || filter.sharedMesh.name != "Cube") continue;
                string n = filter.name.ToLowerInvariant();
                bool cushion = n.Contains("cushion") || n == "pillow" || n == "sofaseat" || n == "sofaarm" || n == "sofaback";
                bool furniture = cushion || n.Contains("counter") || n == "door" || n.Contains("tabletop") || n == "seat" || n.Contains("drawer") || n.Contains("bedframe") || n == "arm" || n == "back" || n == "body" || n == "head";
                if (!furniture || renderer.GetComponentInParent<CutawayWall>()) continue;
                if (renderer.bounds.size.magnitude > 12f) continue;
                filter.sharedMesh = cushion ? soft : hard; rounded++;
            }
            foreach (var item in UnityEngine.Object.FindObjectsByType<CookPot>(FindObjectsSortMode.None))
            {
                var body = item.transform.Find("Body");
                if (body && body.GetComponent<MeshFilter>())
                {
                    body.GetComponent<MeshFilter>().sharedMesh = pot;
                    body.localPosition = Vector3.zero; body.localScale = Vector3.one;
                }
            }
            foreach (var item in UnityEngine.Object.FindObjectsByType<Lid>(FindObjectsSortMode.None))
            {
                var shape = item.transform.Find("Lid");
                if (shape && shape.GetComponent<MeshFilter>())
                {
                    shape.GetComponent<MeshFilter>().sharedMesh = lid;
                    shape.localPosition = Vector3.zero; shape.localScale = Vector3.one;
                    var knob = item.transform.Find("Knob"); if (knob) knob.localPosition = new Vector3(0, .072f, 0);
                }
            }
            foreach (var fire in UnityEngine.Object.FindObjectsByType<FireSource>(FindObjectsSortMode.None))
            {
                fire.maxFlameRate = 30f; fire.maxFlameSize = Mathf.Min(fire.maxFlameSize, .7f); fire.baseLight = Mathf.Min(fire.baseLight, 2.3f);
                if (!fire.flames) continue;
                var main = fire.flames.main; main.maxParticles = 100;
                main.startRotation = new ParticleSystem.MinMaxCurve(-.16f, .16f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .65f);
                main.startColor = Color.white;
                var noise = fire.flames.noise; noise.strength = .12f;
                var r = fire.flames.GetComponent<ParticleSystemRenderer>();
                var mat = new Material(r.sharedMaterial); mat.name = "CraftedFlame";
                mat.SetTexture("_BaseMap", flame); mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Blend", 0f); mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.DisableKeyword("_BLENDMODE_ADD");
                r.sharedMaterial = Store(mat, Root + "/Materials/CraftedFlame.mat");
                r.sortingFudge = 0f;
            }
            var sun = GameObject.Find("Sun")?.GetComponent<Light>();
            if (sun) { sun.color = new Color(1f, .94f, .84f); sun.intensity = 1.55f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .75f; }
            RenderSettings.ambientLight = new Color(.66f, .71f, .76f);
            var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
            if (camera) camera.baseFog = .006f;
            var after = CollisionSignature();
            if (before != after) throw new InvalidOperationException("Collision changed: inspect before saving.");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("DevBridge/visual-polish");
            File.WriteAllText("DevBridge/visual-polish/authoring.txt", "Cloned material slots=" + surfaces + "\nRounded mesh instances=" + rounded + "\nCollider signature unchanged=True\n" + before);
            Debug.Log("[MamaVisualPolish] Applied: " + surfaces + " material slots, " + rounded + " crafted mesh surfaces; collider signature unchanged.");
        }

        static string CollisionSignature()
        {
            return string.Join("\n", UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
                .Where(c => c.gameObject.scene == EditorSceneManager.GetActiveScene())
                .Select(c => c.GetInstanceID() + "|" + c.GetType().Name + "|" + EditorJsonUtility.ToJson(c) + "|" + c.transform.position + "|" + c.transform.lossyScale));
        }

        static Mesh Mesh(string name)
        {
            string path = Root + "/Models/" + name + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Missing model: " + path);
            importer.globalScale = 1f; importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().First();
        }

        static Material Store(Material value, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old) { EditorUtility.CopySerialized(value, old); UnityEngine.Object.DestroyImmediate(value); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(value, path); return value;
        }

        static Color Hex(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }
        static Material Look(Material source)
        {
            if (materials.TryGetValue(source, out var cached)) return cached;
            string n = source.name;
            var m = new Material(source); m.name = "Craft_" + n;
            if (m.HasProperty("_BaseColor") && m.GetTag("RenderType", false) != "Transparent")
            {
                Color c = m.GetColor("_BaseColor");
                Color.RGBToHSV(c, out float h, out float s, out float v);
                c = Color.HSVToRGB(h, s * .65f, v * .92f); c.a = 1f;
                bool floor = n.StartsWith("Floor_", StringComparison.Ordinal);
                bool wallpaper = n.StartsWith("Stripe", StringComparison.Ordinal) || n.StartsWith("WallK_", StringComparison.Ordinal) || n.StartsWith("WallL_", StringComparison.Ordinal);
                if (floor)
                {
                    bool ceramic = n.Contains("Kitchen") || n.Contains("Bath") || n.Contains("Laundry");
                    m.SetTexture("_BaseMap", ceramic ? tile : wood);
                    c = ceramic ? Hex("D4CCB8") : Hex("B48D6A");
                    var t = source.GetTextureScale("_BaseMap"); m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1, t.x * .35f), Mathf.Max(1, t.y * .35f)));
                    m.SetFloat("_Smoothness", ceramic ? .26f : .14f);
                }
                if (wallpaper)
                {
                    m.SetTexture("_BaseMap", wall); c = n.Contains("Mint") || n.StartsWith("WallL_", StringComparison.Ordinal) ? Hex("CDD2BE") : n.Contains("Lilac") ? Hex("D0C7CE") : Hex("E2D5BE");
                    m.SetFloat("_Smoothness", .1f);
                }
                if (new[]{"CounterBody","CabinetDoor","CabinetInset","UpperCab","UpperDoor","StoveBody"}.Contains(n)) c = Hex(n.Contains("Inset") ? "728675" : "8B9F8A");
                if (n == "CounterTop") { c = Hex("E7DFCD"); m.SetFloat("_Smoothness", .3f); }
                if (n == "Sofa" || n == "SofaCushion") c = Hex(n == "Sofa" ? "B97470" : "C98B82");
                if (n == "CarpetField") c = Hex("C8B58E");
                if (n == "CarpetBorder") c = Hex("9D6B57");
                if (n == "CarpetMed") c = Hex("A1AD93");
                if (n == "TVStand" || n == "Bookshelf" || n == "PantryWood" || n == "PantryDoor") c = Hex("A67C58");
                bool fabric = n.Contains("Cushion") || n.Contains("Curtain") || n.Contains("Pillow") || n == "Sofa" || n == "Blanket";
                if (fabric) { m.SetTexture("_BaseMap", cloth); m.SetTextureScale("_BaseMap", new Vector2(3,3)); m.SetFloat("_Smoothness", .08f); }
                if (n.Contains("Wood") || n == "TVStand" || n == "Bookshelf") { m.SetTexture("_BaseMap", wood); m.SetFloat("_Smoothness", .17f); }
                if (n == "PotMetal") { c = Hex("8E999A"); m.SetFloat("_Metallic", .65f); m.SetFloat("_Smoothness", .42f); }
                m.SetColor("_BaseColor", c);
            }
            var result = Store(m, Root + "/Materials/Craft_" + n + ".mat"); materials[source] = result; return result;
        }

        static Texture2D Texture(string name, int kind)
        {
            const int size = 256;
            var tex = new Texture2D(size,size,TextureFormat.RGBA32,true);
            var pixels = new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size, v=(y+.5f)/size;
                float grain = Mathf.PerlinNoise(u*55f,v*3f);
                float value = .96f + .035f*grain;
                if(kind==0)
                {
                    value=.87f+.09f*grain+.025f*Mathf.Sin(v*100f+grain*3f);
                    if(x%64<1 || (y + ((x/64)%2)*64)%128<1) value*=.88f;
                }
                if(kind==1) { value=((x/128+y/128)%2==0) ? .98f : .91f; if(x%128<2 || y%128<2) value=.78f; }
                if(kind==2) value=.94f+.045f*((x/32)%2);
                if(kind==3) value=.92f+.035f*Mathf.Sin(x*2f)*Mathf.Sin(y*2f);
                Color c = new Color(value,value,value,1f);
                if(kind==4)
                {
                    float center=.5f+.08f*Mathf.Sin(v*5f), width=.32f*Mathf.Pow(1f-v,.55f)+.01f;
                    float a=Mathf.Clamp01((1f-Mathf.Abs(u-center)/width)*5f)*Mathf.Clamp01(v*15f)*Mathf.Clamp01((1f-v)*12f);
                    c=new Color(1f,1f,1f,a);
                }
                pixels[y*size+x]=c;
            }
            tex.SetPixels(pixels); tex.Apply(); string path=Root+"/Textures/"+name+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode=kind==4 ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.alphaIsTransparency=kind==4; importer.mipmapEnabled=true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
