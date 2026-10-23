using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DreamRush.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MamasComing.WitchTest.EditorTools
{
    /// <summary>Builds only the isolated scene from authored FBX and generated raster assets.</summary>
    public static class WitchSceneBuilder
    {
        public const string Root = "Assets/MamasComing/WitchTest";
        public const string ScenePath = Root + "/Scenes/AppleWitch_Test.unity";
        static Dictionary<string, Material> materials;
        static Sprite[] skin;

        [MenuItem("Mama's Coming/Witch Test/Build separate test scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building the test scene.");
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            // Preserve existing unsaved authoring rather than unloading it into the new scene.
            if (current.isDirty && !string.IsNullOrEmpty(current.path)) EditorSceneManager.SaveScene(current);
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/UI/Sprites");
            AssetDatabase.Refresh();
            PrepareTextures(); skin = MakeSkin(); materials = new Dictionary<string, Material>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = Model("ENV_WitchKitchen", Vector3.zero, 1);
            var top = environment.GetComponentsInChildren<Renderer>().First(x => x.name.StartsWith("Countertop_Stone"));
            float surface = top.bounds.max.y;
            // FBX conversion is checked from the actual imported countertop, not guessed.
            float backZ = top.bounds.center.z;
            if (backZ < 0)
            {
                environment.transform.rotation = Quaternion.Euler(0, 180, 0);
                top = environment.GetComponentsInChildren<Renderer>().First(x => x.name.StartsWith("Countertop_Stone"));
                backZ = top.bounds.center.z;
            }
            foreach (var r in environment.GetComponentsInChildren<MeshRenderer>())
            {
                string n = r.name;
                if (n.StartsWith("Floor_") || n.StartsWith("Wall_") || n.StartsWith("Countertop_") || n.StartsWith("Cabinet_Carcass") || n.StartsWith("Stool_Seat"))
                {
                    var mesh = r.GetComponent<MeshFilter>(); if (mesh) r.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                }
                r.gameObject.isStatic = true;
            }
            var systems = new GameObject("AppleWitch_TestSystems");
            var level = systems.AddComponent<WitchLevel>();
            var hud = systems.AddComponent<WitchHUD>(); level.hud = hud; hud.skin = skin;
            hud.spellPages = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/SpellPages.png");

            // Borrow the existing character art, keep a dedicated tiny-world motor config.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/PF_AppleKid.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            player.name = "Tiny Apple - 12cm";
            foreach (var behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!behaviour) continue;
                if (behaviour is PlayerMotor3D || behaviour is PlayerInputReader || behaviour is AppleProceduralAnimator || behaviour is AppleFace || behaviour is PlayerSquashStretch) continue;
                Object.DestroyImmediate(behaviour);
            }
            // Keep the motor root at unit scale: its ground probes use world metres.
            foreach (Transform child in player.transform) { child.localPosition *= .1f; child.localScale *= .1f; }
            player.transform.localScale = Vector3.one;
            player.transform.SetPositionAndRotation(new Vector3(-.55f, surface + .012f, backZ - .23f), Quaternion.Euler(0, 75, 0));
            var motor = player.GetComponent<PlayerMotor3D>(); var reader = player.GetComponent<PlayerInputReader>();
            var config = AssetDatabase.LoadAssetAtPath<PlayerMotorConfig>(Root + "/TinyAppleMotor.asset");
            if (!config) { config = ScriptableObject.CreateInstance<PlayerMotorConfig>(); AssetDatabase.CreateAsset(config, Root + "/TinyAppleMotor.asset"); }
            config.walkSpeed = .28f; config.sprintSpeed = .44f; config.groundAcceleration = 2.5f; config.groundDeceleration = 3.5f; config.turnaroundDeceleration = 4f;
            config.airAcceleration = 1.3f; config.airDeceleration = .3f; config.jumpHeight = .085f; config.gravity = 3.2f; config.maxFallSpeed = 3.5f;
            config.groundSnapDistance = .018f; config.groundStickVelocity = .3f; config.rotationSpeedDegrees = 540;
            EditorUtility.SetDirty(config); motor.Config = config;
            var controller = player.GetComponent<CharacterController>(); controller.height = .12f; controller.radius = .042f; controller.center = new Vector3(0, .06f, 0); controller.skinWidth = .0012f; controller.stepOffset = .014f; controller.minMoveDistance = 0;
            foreach (var animator in player.GetComponentsInChildren<AppleProceduralAnimator>()) { animator.strideLength = .085f; animator.sprintStrideLength = .07f; animator.sleeping = false; }
            level.motor = motor; level.input = reader;

            var wandPivot = new GameObject("WandPose").transform; wandPivot.SetParent(player.transform, false);
            wandPivot.localPosition = new Vector3(.035f, .058f, .027f); wandPivot.localScale = Vector3.one;
            var wand = Model("PROP_AppleWand", Vector3.zero, 1);
            wand.transform.SetParent(wandPivot, false);
            var tip = new GameObject("WandTip").transform; tip.SetParent(wand.transform, false); tip.localPosition = new Vector3(0, .101f, 0);
            level.wandPivot = wandPivot; level.wandTip = tip;

            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGO.tag = "MainCamera";
            var camera = camGO.GetComponent<Camera>(); camera.fieldOfView = 55; camera.nearClipPlane = .012f; camera.farClipPlane = 25;
            camera.backgroundColor = new Color(.31f, .37f, .42f); camera.clearFlags = CameraClearFlags.SolidColor;
            var data = camGO.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var orbit = camGO.AddComponent<TinyWitchCamera>(); orbit.target = player.transform; orbit.yaw = 75; orbit.pitch = 18;
            motor.CameraTransform = camGO.transform; level.orbit = orbit;

            // Physical destinations are authored shapes, not glowing UI arrows.
            var saucer = Model("PROP_CupSaucer", new Vector3(.95f, surface + .001f, backZ + .01f), 1);
            StaticColliders(saucer);
            var stack = Model("PROP_BookStack", new Vector3(-1.12f, surface + .001f, backZ + .01f), 1);
            StaticColliders(stack);
            var rack = Model("PROP_PotionRack", new Vector3(.34f, surface + .001f, backZ + .13f), 1);
            StaticColliders(rack);
            var cupHome = Home("cup", "จานรอง", new Vector3(.95f, surface + .012f, backZ + .01f), new Vector2(.07f, .07f));
            var bookHome = Home("book", "กองหนังสือ", new Vector3(-1.12f, surface + .068f, backZ + .01f), new Vector2(.12f, .13f));
            var bottleHome = Home("bottle", "ชั้นขวด", new Vector3(.34f, surface + .027f, backZ + .13f), new Vector2(.07f, .065f));
            var cup = Prop("PROP_MoonCup", "cup", "ถ้วยชา", new Vector3(-.18f, surface + .004f, backZ - .11f), cupHome, .18f);
            var book = Prop("PROP_Grimoire", "book", "ตำราเวท", new Vector3(.57f, surface + .004f, backZ - .13f), bookHome, .65f);
            var bottle = Prop("PROP_MoonPotion", "bottle", "ขวดน้ำยา", new Vector3(-.68f, surface + .004f, backZ + .11f), bottleHome, .26f);
            level.props = new[] { cup, book, bottle }; level.homes = new[] { cupHome, bookHome, bottleHome };
            var spell = Model("PROP_OpenSpellbook", new Vector3(-1.18f, surface + .004f, backZ - .24f), .72f); StaticColliders(spell);
            // Two physical low book steps are optional exploration, not a new required puzzle rule.
            StaticColliders(Model("PROP_BookStack", new Vector3(1.30f, surface + .002f, backZ + .10f), .7f));
            var mother = Model("CHR_MotherWitch", new Vector3(-.25f, 0, -.96f), 1);
            mother.transform.rotation = Quaternion.identity; level.mother = mother.transform;

            level.magicMaterial = Material("MagicRibbon", new Color(1f, .64f, .37f), .2f, 0, null);
            var ribbon = level.magicMaterial; ribbon.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            ribbon.SetFloat("_Surface", 1); ribbon.SetFloat("_Blend", 2); ribbon.SetFloat("_SrcBlend", 5); ribbon.SetFloat("_DstBlend", 1); ribbon.SetFloat("_ZWrite", 0); ribbon.renderQueue = 3000;
            ribbon.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); ribbon.SetColor("_BaseColor", new Color(1.4f, .7f, .3f, .6f));

            if (!Object.FindFirstObjectByType<EventSystem>()) new GameObject("Witch EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Lighting();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory("DevBridge/witch-test");
            File.WriteAllText("DevBridge/witch-test/build.txt", "Scene=" + ScenePath + "\nCounter top=" + surface + "\nCounter Z=" + backZ + "\nPlayer controller height=" + controller.height + "m\nGenerated UI sprites=" + skin.Length + "\n" + string.Join("\n", level.props.Select(p => p.id + " bounds=" + Combined(p.gameObject).size)));
            Debug.Log("Witch test scene built and saved: " + ScenePath);
        }

        static GameObject Model(string name, Vector3 position, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx");
            if (!asset) throw new Exception("Missing authored model " + name);
            var wrapper = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset); model.transform.SetParent(wrapper.transform, false);
            foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => ImportedMaterial(m ? m.name : "SURF_Cream")).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            }
            wrapper.transform.localScale = Vector3.one * scale; wrapper.transform.position = position;
            return wrapper;
        }

        static Material ImportedMaterial(string name)
        {
            name = name.Replace(" (Instance)", "");
            if (materials.TryGetValue(name, out var known)) return known;
            Texture2D texture = null; Color color = Color.white; float rough = .6f, metal = 0;
            Vector2? offset = null;
            if (name.StartsWith("SURF_"))
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/WitchSurfaces.png");
                offset = name.Contains("Wood") ? new Vector2(0, .5f) : name.Contains("Sage") ? new Vector2(.5f, .5f) : name.Contains("Plum") ? new Vector2(.5f, 0) : Vector2.zero;
            }
            else if (name.Contains("SpellPages")) texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/SpellPages.png");
            else if (name.Contains("Brass")) { color = new Color(.66f, .43f, .16f); metal = .7f; rough = .28f; }
            else if (name.Contains("Peach") || name.Contains("Glow")) { color = new Color(.95f, .48f, .25f); rough = .24f; }
            else if (name.Contains("Leaf")) color = new Color(.25f, .43f, .23f);
            else if (name.Contains("Glass")) { color = new Color(.35f, .64f, .55f); rough = .2f; metal = .2f; }
            else if (name.Contains("Rose")) color = new Color(.66f, .25f, .29f);
            else if (name.Contains("Skin")) color = new Color(.73f, .49f, .34f);
            else if (name.Contains("Hair") || name.Contains("Cork")) color = new Color(.30f, .15f, .10f);
            else if (name.Contains("Eye")) color = new Color(.95f, .91f, .79f);
            else color = new Color(.055f, .045f, .08f);
            var result = Material(name, color, rough, metal, texture);
            if (offset.HasValue) { result.mainTextureScale = Vector2.one * .5f; result.mainTextureOffset = offset.Value; }
            if (name.Contains("Glow") || name.Contains("Peach")) { result.EnableKeyword("_EMISSION"); result.SetColor("_EmissionColor", color * (name.Contains("Glow") ? 2 : .4f)); }
            materials[name] = result; return result;
        }

        static Material Material(string name, Color color, float rough, float metal, Texture2D texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 1 - rough); material.SetFloat("_Metallic", metal);
            if (texture) material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material); return material;
        }

        static Bounds Combined(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
        }

        static WitchProp Prop(string model, string id, string label, Vector3 at, WitchHome home, float mass)
        {
            var go = Model(model, at, 1); var bounds = Combined(go);
            var collider = go.AddComponent<BoxCollider>(); collider.center = go.transform.InverseTransformPoint(bounds.center); collider.size = bounds.size;
            var body = go.AddComponent<Rigidbody>(); body.mass = mass; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.linearDamping = .6f; body.angularDamping = 2;
            var prop = go.AddComponent<WitchProp>(); prop.id = id; prop.label = label; prop.body = body; prop.shape = collider; prop.home = home;
            return prop;
        }

        static WitchHome Home(string id, string label, Vector3 at, Vector2 area)
        {
            var go = new GameObject("Home_" + id); go.transform.position = at;
            var home = go.AddComponent<WitchHome>(); home.acceptsId = id; home.label = label; home.halfArea = area;
            var cue = new GameObject("Optional landing cue"); cue.transform.SetParent(go.transform, false); cue.transform.localPosition = Vector3.up * .004f; cue.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var renderer = cue.AddComponent<SpriteRenderer>(); renderer.sprite = skin[7]; renderer.sortingOrder = 0; renderer.color = new Color(1, .8f, .6f, .35f);
            float width = renderer.sprite.bounds.size.x; cue.transform.localScale = Vector3.one * (.09f / width); home.cue = renderer;
            return home;
        }

        static void StaticColliders(GameObject go)
        {
            foreach (var mesh in go.GetComponentsInChildren<MeshFilter>()) mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
        }

        static void PrepareTextures()
        {
            foreach (string path in new[] { Root + "/UI/AppleWitchUI.png", Root + "/Textures/WitchSurfaces.png", Root + "/Textures/SpellPages.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); if (!importer) throw new Exception("Missing generated image " + path);
                importer.textureType = TextureImporterType.Default; importer.maxTextureSize = 2048; importer.sRGBTexture = true; importer.isReadable = path.Contains("UI.png");
                importer.alphaIsTransparency = path.Contains("UI.png"); importer.mipmapEnabled = !path.Contains("UI.png"); importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            }
        }

        static Sprite[] MakeSkin()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/UI/AppleWitchUI.png");
            // Generated artwork uses unequal visual footprints; crop the actual artwork without redrawing it.
            var regions = new[] { new Rect(0, .575f, .31f, .265f), new Rect(.312f, .575f, .255f, .265f), new Rect(.58f, .575f, .19f, .265f), new Rect(.78f, .575f, .22f, .265f), new Rect(.03f, .04f, .25f, .46f), new Rect(.30f, .04f, .22f, .46f), new Rect(.55f, .02f, .23f, .48f), new Rect(.79f, .04f, .21f, .46f) };
            var pixels = texture.GetPixels32(); var sprites = new Sprite[8];
            for (int i = 0; i < regions.Length; i++)
            {
                var r = regions[i]; int x0 = Mathf.RoundToInt(r.x * texture.width), x1 = Mathf.Min(texture.width, Mathf.RoundToInt(r.xMax * texture.width));
                int y0 = Mathf.RoundToInt(r.y * texture.height), y1 = Mathf.Min(texture.height, Mathf.RoundToInt(r.yMax * texture.height));
                int minX = x1, maxX = x0, minY = y1, maxY = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) if (pixels[y * texture.width + x].a > 60) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                var rect = new Rect(minX, minY, Mathf.Max(1, maxX - minX + 1), Mathf.Max(1, maxY - minY + 1));
                var sprite = Sprite.Create(texture, rect, Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, i < 4 ? new Vector4(32, 32, 32, 32) : Vector4.zero);
                sprite.name = "AppleWitchSkin_" + i;
                string path = Root + "/UI/Sprites/" + sprite.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing) { EditorUtility.CopySerialized(sprite, existing); Object.DestroyImmediate(sprite); sprites[i] = existing; }
                else { AssetDatabase.CreateAsset(sprite, path); sprites[i] = sprite; }
            }
            return sprites;
        }

        static void Lighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.50f, .57f, .66f); RenderSettings.ambientEquatorColor = new Color(.34f, .31f, .32f); RenderSettings.ambientGroundColor = new Color(.22f, .17f, .21f);
            RenderSettings.fog = false;
            var sun = new GameObject("Warm window key").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1, .84f, .65f); sun.intensity = 1.8f;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0); sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f; sun.shadowBias = .025f; sun.shadowNormalBias = .08f; RenderSettings.sun = sun;
            var fill = new GameObject("Cool room fill").AddComponent<Light>(); fill.type = LightType.Point; fill.transform.position = new Vector3(-.7f, 1.7f, -.7f); fill.range = 4; fill.intensity = .55f; fill.color = new Color(.69f, .78f, 1); fill.shadows = LightShadows.None;
            var warm = new GameObject("Pendant pool").AddComponent<Light>(); warm.type = LightType.Point; warm.transform.position = new Vector3(-.55f, 2.05f, -.3f); warm.range = 2.5f; warm.intensity = .65f; warm.color = new Color(1, .68f, .36f);
            var volumeGO = new GameObject("Witch Global Volume"); var volume = volumeGO.AddComponent<Volume>(); volume.isGlobal = true;
            string path = Root + "/WitchVolume.asset"; var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            var bloom = profile.TryGet<Bloom>(out var existingBloom) ? existingBloom : profile.Add<Bloom>(true); bloom.intensity.Override(.24f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.55f);
            var colors = profile.TryGet<ColorAdjustments>(out var existingColors) ? existingColors : profile.Add<ColorAdjustments>(true); colors.postExposure.Override(.25f); colors.contrast.Override(9); colors.saturation.Override(3);
            var tone = profile.TryGet<Tonemapping>(out var existingTone) ? existingTone : profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var vignette = profile.TryGet<Vignette>(out var existingVignette) ? existingVignette : profile.Add<Vignette>(true); vignette.intensity.Override(.13f); vignette.smoothness.Override(.5f);
            foreach (var component in profile.components) if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
            volume.sharedProfile = profile; EditorUtility.SetDirty(profile);
        }
    }
}
