using System;
using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Assembles the player hierarchy from code so the editor prefab builder and PlayMode tests use the
    /// exact same composition. The visual is a GRAYBOX apple proxy until the Blender model passes review.
    /// </summary>
    public static class PlayerRigBuilder
    {
        public const float Height = 1.2f;
        public const float Radius = 0.42f;

        public delegate Material MaterialProvider(string name, Color color);

        static readonly Dictionary<string, Material> RuntimeCache = new Dictionary<string, Material>();

        public static Material RuntimeMaterial(string name, Color color)
        {
            if (RuntimeCache.TryGetValue(name, out var m) && m != null) return m;
            m = RuntimeMaterials.CreateLit(color, 0.2f, name);
            RuntimeCache[name] = m;
            return m;
        }

        /// <param name="visualFactory">Creates the character model under the given parent (Blender apple). Null = graybox proxy.</param>
        public static GameObject BuildPlayer(PlayerMotorConfig config, bool withInput, MaterialProvider materials = null,
            Material shadowMaterial = null, Func<Transform, GameObject> visualFactory = null)
        {
            materials ??= RuntimeMaterial;
            var root = new GameObject("Player");
            var cc = root.AddComponent<CharacterController>();
            cc.height = Height;
            cc.radius = Radius;
            cc.center = new Vector3(0f, Height * 0.5f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f; // motor's maxWalkableSlope (45) is the gameplay rule; CC limit slightly above
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;

            if (withInput) root.AddComponent<PlayerInputReader>();
            var motor = root.AddComponent<PlayerMotor3D>();
            motor.Config = config;
            if (withInput)
            {
                root.AddComponent<Interaction.InteractionSystem>();
                root.AddComponent<UI.PlayerUIBridge>();
            }

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject model = visualFactory != null ? visualFactory(visual.transform) : null;
            if (model != null)
            {
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                var anim = model.AddComponent<AppleProceduralAnimator>();
                anim.motor = motor;
                model.AddComponent<AppleFace>().animator = anim;
                Look.DreamLook.AddOutline(model, 2.6f); // selective ink outline: the player (GDD §5.6)
            }
            else
            {
                BuildGrayboxApple(visual.transform, materials);
            }
            var ss = visual.AddComponent<PlayerSquashStretch>();
            ss.motor = motor;
            if (withInput) root.AddComponent<Presentation.PlayerFeelFX>(); // dust, landing puff, footsteps (after the animator exists)

            var shadowGo = new GameObject("BlobShadow");
            shadowGo.transform.SetParent(root.transform, false);
            var shadow = shadowGo.AddComponent<BlobShadow>();
            shadow.target = root.transform;
            shadow.material = shadowMaterial;

            return root;
        }

        /// <summary>Primitive apple: reads as an apple with a face direction. GRAYBOX only.</summary>
        public static void BuildGrayboxApple(Transform parent, MaterialProvider materials)
        {
            var red = materials("GRAYBOX_AppleSkin", Palette.AppleRed);
            var cream = materials("GRAYBOX_Cream", Palette.WarmCream);
            var ink = materials("GRAYBOX_Ink", Palette.DarkInk);
            var leaf = materials("GRAYBOX_Leaf", Palette.LeafGreen);
            var stem = materials("GRAYBOX_Stem", Color.Lerp(Palette.ApricotOrange, Palette.MidnightPlum, 0.55f));

            // Body: slightly wide apple mass, lifted for legs
            Part(PrimitiveType.Sphere, "Body", parent, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.9f, 0.8f, 0.78f), red);
            Part(PrimitiveType.Sphere, "BodyTopLobe_L", parent, new Vector3(-0.17f, 0.95f, 0f), Vector3.zero, new Vector3(0.45f, 0.35f, 0.5f), red);
            Part(PrimitiveType.Sphere, "BodyTopLobe_R", parent, new Vector3(0.17f, 0.95f, 0f), Vector3.zero, new Vector3(0.45f, 0.35f, 0.5f), red);
            Part(PrimitiveType.Cylinder, "Stem", parent, new Vector3(0.03f, 1.12f, 0f), new Vector3(0f, 0f, -14f), new Vector3(0.05f, 0.09f, 0.05f), stem);
            Part(PrimitiveType.Sphere, "Leaf", parent, new Vector3(0.14f, 1.17f, -0.02f), new Vector3(0f, 20f, -25f), new Vector3(0.2f, 0.035f, 0.1f), leaf);

            // Face (front = +Z)
            Part(PrimitiveType.Sphere, "Eye_L", parent, new Vector3(-0.15f, 0.78f, 0.35f), Vector3.zero, new Vector3(0.17f, 0.12f, 0.06f), cream);
            Part(PrimitiveType.Sphere, "Eye_R", parent, new Vector3(0.15f, 0.78f, 0.35f), Vector3.zero, new Vector3(0.17f, 0.12f, 0.06f), cream);
            Part(PrimitiveType.Sphere, "Pupil_L", parent, new Vector3(-0.13f, 0.77f, 0.375f), Vector3.zero, new Vector3(0.07f, 0.07f, 0.03f), ink);
            Part(PrimitiveType.Sphere, "Pupil_R", parent, new Vector3(0.13f, 0.77f, 0.375f), Vector3.zero, new Vector3(0.07f, 0.07f, 0.03f), ink);
            Part(PrimitiveType.Cube, "Mouth", parent, new Vector3(0f, 0.6f, 0.385f), new Vector3(0f, 0f, 4f), new Vector3(0.2f, 0.018f, 0.02f), ink);

            // Limbs
            Part(PrimitiveType.Capsule, "Leg_L", parent, new Vector3(-0.16f, 0.17f, 0f), Vector3.zero, new Vector3(0.11f, 0.12f, 0.11f), cream);
            Part(PrimitiveType.Capsule, "Leg_R", parent, new Vector3(0.16f, 0.17f, 0f), Vector3.zero, new Vector3(0.11f, 0.12f, 0.11f), cream);
            Part(PrimitiveType.Sphere, "Foot_L", parent, new Vector3(-0.17f, 0.05f, 0.06f), Vector3.zero, new Vector3(0.2f, 0.1f, 0.28f), cream);
            Part(PrimitiveType.Sphere, "Foot_R", parent, new Vector3(0.17f, 0.05f, 0.06f), Vector3.zero, new Vector3(0.2f, 0.1f, 0.28f), cream);
            Part(PrimitiveType.Sphere, "Hand_L", parent, new Vector3(-0.47f, 0.55f, 0.1f), Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), cream);
            Part(PrimitiveType.Sphere, "Hand_R", parent, new Vector3(0.47f, 0.55f, 0.1f), Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), cream);
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Creates a third-person camera following <paramref name="player"/>.</summary>
        public static ThirdPersonCameraController BuildHubCamera(GameObject player)
        {
            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            var tp = camGo.AddComponent<ThirdPersonCameraController>();
            tp.target = player.transform;
            tp.input = player.GetComponent<PlayerInputReader>();
            var motor = player.GetComponent<PlayerMotor3D>();
            if (motor != null) motor.CameraTransform = camGo.transform;
            return tp;
        }
    }
}
