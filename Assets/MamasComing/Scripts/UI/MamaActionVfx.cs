using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing
{
    /// <summary>Presentation only: fire-out steam and small, shape-coded co-op identity tags.</summary>
    public class MamaActionVfx : MonoBehaviour
    {
        readonly Dictionary<FireSource, bool> fireState = new Dictionary<FireSource, bool>();
        readonly Dictionary<PlayerController, TextMesh> tags = new Dictionary<PlayerController, TextMesh>();
        readonly List<PlayerController> stale = new List<PlayerController>();
        Material steam;
        Material tagBacking;
        Mesh tagQuad;
        GameManager gm;
        float scanAt;

        void Start()
        {
            gm = GetComponent<GameManager>();
            MamaVfx.Prepare();
            foreach(var fire in FireSource.All)
            {
                if(!fire || !fire.flames)continue;
                var main=fire.flames.main; main.startColor=Color.white;main.maxParticles=180;
                var heat=new Gradient();
                heat.SetKeys(new[]{new GradientColorKey(new Color(1.6f,1.22f,.48f),0),new GradientColorKey(new Color(1.35f,.42f,.045f),.4f),new GradientColorKey(new Color(.65f,.10f,.04f),.8f)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(.6f,.6f),new GradientAlphaKey(0,1)});
                var color=fire.flames.colorOverLifetime;color.enabled=true;color.color=heat;
                if(fire.embers){var sparks=fire.embers.main;sparks.maxParticles=40;}
            }
        }
        void LateUpdate()
        {
            if (!gm) return;
            foreach (var fire in FireSource.All)
            {
                if (!fire) continue;
                if (fireState.TryGetValue(fire, out bool burning) && burning && !fire.burning && gm.Live) Steam(fire.transform.position);
                fireState[fire] = fire.burning;
            }
            bool play = gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome;
            if (Time.unscaledTime >= scanAt)
            {
                scanAt = Time.unscaledTime + .5f;
                foreach (var p in PlayerController.AllPlayers)
                {
                    if (!p || p.isLocal || tags.ContainsKey(p)) continue;
                    var go = new GameObject("Coop identity");
                    var label = go.AddComponent<TextMesh>(); label.fontSize = 48; label.characterSize = .045f;
                    label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                    AddTagBacking(go.transform);
                    tags.Add(p, label);
                }
                stale.Clear();
                foreach (var kv in tags) if (!kv.Key || !PlayerController.AllPlayers.Contains(kv.Key)) stale.Add(kv.Key);
                foreach (var p in stale) { if (tags[p]) Destroy(tags[p].gameObject); tags.Remove(p); }
            }
            foreach (var kv in tags)
            {
                if (!kv.Key || !kv.Value) continue;
                var cam = Camera.main;
                bool visible = play && cam && !MamaSettings.Open;
                if (visible)
                {
                    Vector3 view = cam.WorldToViewportPoint(kv.Key.transform.position + Vector3.up * 1.55f);
                    visible = view.z > 0 && view.x > .04f && view.x < .96f && view.y > .08f && view.y < .92f;
                }
                kv.Value.gameObject.SetActive(visible);
                if (!visible) continue;
                string id = NetGame.NameOf(kv.Key) ?? NetGame.GetName?.Invoke(kv.Key) ?? "Friend";
                bool second = id == "P2";
                kv.Value.text = (second ? "◆ " : "▲ ") + id;
                kv.Value.color = second ? new Color(.35f,.95f,.87f) : new Color(1f,.82f,.35f);
                kv.Value.transform.SetPositionAndRotation(kv.Key.transform.position + Vector3.up * 1.55f, cam.transform.rotation);
            }
        }

        void Steam(Vector3 position)
        {
            if (!steam)
            {
                var source = SmokeSim.I && SmokeSim.I.smokeFx ? SmokeSim.I.smokeFx.GetComponent<ParticleSystemRenderer>().sharedMaterial : null;
                if (!source) return;
                steam = new Material(source); steam.name = "Runtime fire-out steam";
            }
            var go = new GameObject("Fire out - settling steam"); go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.duration = .1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f,.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.18f,.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(.1f,.22f); main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.8f,.89f,.87f,.38f);
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 18f; shape.radius = .12f; shape.rotation = new Vector3(-90,0,0);
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});
            fade.color = gradient;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,.5f,1,1.6f));
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = steam; renderer.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play(); ps.Emit(10); Destroy(go,1.2f);
        }

        void AddTagBacking(Transform parent)
        {
            if (!tagBacking)
            {
                tagBacking = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                tagBacking.name = "Coop tag ink"; tagBacking.SetColor("_BaseColor", new Color(.08f,.12f,.13f));
                tagQuad = new Mesh(); tagQuad.name = "Coop tag backing";
                tagQuad.vertices = new[]{new Vector3(-.4f,-.14f,0),new Vector3(-.4f,.14f,0),new Vector3(.4f,.14f,0),new Vector3(.4f,-.14f,0)};
                tagQuad.triangles = new[]{0,1,2,0,2,3};
                tagQuad.uv = new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}; tagQuad.RecalculateBounds();
            }
            var backing = new GameObject("Ink backing",typeof(MeshFilter),typeof(MeshRenderer));
            backing.transform.SetParent(parent,false); backing.transform.localPosition = new Vector3(0,0,.015f);
            backing.GetComponent<MeshFilter>().sharedMesh = tagQuad;
            var renderer = backing.GetComponent<MeshRenderer>(); renderer.sharedMaterial = tagBacking;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        void OnDestroy()
        {
            foreach (var label in tags.Values) if (label) Destroy(label.gameObject);
            if (steam) Destroy(steam);
            if (tagBacking) Destroy(tagBacking);
            if (tagQuad) Destroy(tagQuad);
        }
    }
}
