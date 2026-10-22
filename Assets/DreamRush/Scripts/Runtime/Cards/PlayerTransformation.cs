using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>BE A GRANDPA (transformation card): the apple hunches, greys and walks slower for a while.</summary>
    public class PlayerTransformation : MonoBehaviour
    {
        public bool IsGrandpa => Time.time < _until;
        public float Remaining => Mathf.Max(0f, _until - Time.time);

        float _until;
        PlayerMotor3D _motor;
        PlayerMotorConfig _original, _slow;
        Transform _visual;
        GameObject _cane;
        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor3D>();
            _visual = transform.Find("Visual");
            _mpb = new MaterialPropertyBlock();
        }

        public void BecomeGrandpa(float seconds)
        {
            _until = Time.time + seconds;
            if (_slow != null) return;
            _original = _motor.Config;
            _slow = Instantiate(_original);
            _slow.walkSpeed *= 0.75f; _slow.sprintSpeed *= 0.7f; _slow.jumpHeight *= 0.7f;
            _motor.Config = _slow;
            _cane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cane.name = "GrandpaCane";
            DestroyImmediate(_cane.GetComponent<Collider>());
            _cane.transform.SetParent(_visual != null ? _visual : transform, false);
            _cane.transform.localPosition = new Vector3(0.36f, 0.36f, 0.3f);
            _cane.transform.localRotation = Quaternion.Euler(8f, 0f, -5f);
            _cane.transform.localScale = new Vector3(0.05f, 0.36f, 0.05f);
            _cane.GetComponent<Renderer>().sharedMaterial = Core.RuntimeMaterials.CreateLit(new Color(0.55f, 0.36f, 0.22f), 0.2f, "M_Cane");
            _renderers = (_visual != null ? _visual : transform).GetComponentsInChildren<Renderer>();
            SetTint(new Color(0.78f, 0.76f, 0.8f));
            BuildAccessories();
            _anim = GetComponentInChildren<AppleProceduralAnimator>();
            if (_anim != null) _anim.hunch = 14f; // elder posture: readable from the far arena camera
        }

        readonly System.Collections.Generic.List<GameObject> _accessories = new System.Collections.Generic.List<GameObject>();
        AppleProceduralAnimator _anim;

        /// <summary>Round glasses + bushy white moustache/brows, parented to the animated Body part.</summary>
        void BuildAccessories()
        {
            Transform body = FindDeep(_visual != null ? _visual : transform, "Body");
            var mf = body != null ? body.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return;
            Bounds b = mf.sharedMesh.bounds; // Body local space; front = +Z
            float h = b.size.y, front = b.max.z;
            var white = Core.RuntimeMaterials.CreateLit(new Color(0.96f, 0.95f, 0.92f), 0.1f, "M_GrandpaHair");
            var rim = Core.RuntimeMaterials.CreateLit(new Color(0.2f, 0.17f, 0.24f), 0.5f, "M_GrandpaGlasses");
            float eyeY = b.center.y + h * 0.08f;
            for (int side = -1; side <= 1; side += 2)
            {
                var g = new GameObject("GrandpaGlasses");
                g.AddComponent<MeshFilter>().sharedMesh = Core.ProceduralMeshes.Ring();
                g.AddComponent<MeshRenderer>().sharedMaterial = rim;
                g.transform.SetParent(body, false);
                g.transform.localPosition = new Vector3(side * b.size.x * 0.17f, eyeY, front - b.size.z * 0.04f);
                g.transform.localScale = Vector3.one * h * 0.22f;
                _accessories.Add(g);

                var brow = Accessory(PrimitiveType.Sphere, "GrandpaBrow", body, white,
                    new Vector3(side * b.size.x * 0.17f, eyeY + h * 0.17f, front - b.size.z * 0.07f), new Vector3(h * 0.2f, h * 0.06f, h * 0.08f));
                brow.transform.localRotation = Quaternion.Euler(0, 0, side * -12f);
            }
            Accessory(PrimitiveType.Sphere, "GrandpaMoustache", body, white,
                new Vector3(0f, eyeY - h * 0.16f, front - b.size.z * 0.03f), new Vector3(h * 0.38f, h * 0.09f, h * 0.1f));
        }

        GameObject Accessory(PrimitiveType type, string name, Transform parent, Material m, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = m;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            _accessories.Add(go);
            return go;
        }

        static Transform FindDeep(Transform t, string n)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == n) return c;
            return null;
        }

        void SetTint(Color c)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null || (_cane != null && r.gameObject == _cane)) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", c);
                r.SetPropertyBlock(_mpb);
            }
        }

        void Update()
        {
            if (_slow == null) return;
            if (!IsGrandpa) End();
        }

        void End()
        {
            _motor.Config = _original;
            Destroy(_slow); _slow = null;
            if (_cane != null) Destroy(_cane);
            foreach (var a in _accessories) if (a != null) Destroy(a);
            _accessories.Clear();
            if (_anim != null) _anim.hunch = 0f;
            if (_renderers != null) foreach (var r in _renderers) if (r != null) r.SetPropertyBlock(null);
            if (UI.UIRoot.Exists) UI.UIRoot.Get().Toast("...young again!", 1.5f);
        }
    }
}
