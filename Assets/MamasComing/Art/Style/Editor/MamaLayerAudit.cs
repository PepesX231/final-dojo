using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.Style.Editor
{
    /// <summary>Scene layering QA (.claude/skills/scene-layering-qa): finds z-fighting coplanar faces, floating props,
    /// props buried inside each other and strays outside the playable volume. Report-first; the only automatic fix is a
    /// 3 mm nudge of thin, collider-free decoration that z-fights (so gameplay collision is never touched).</summary>
    public static class MamaLayerAudit
    {
        [MenuItem("MamasComing/Style/Audit scene layering (report)")]
        static void ReportMenu() => Debug.Log(AuditActiveScene("docs/evidence/style", false));

        [MenuItem("MamasComing/Style/Audit scene layering + fix thin z-fighting")]
        static void FixMenu() { Debug.Log(AuditActiveScene("docs/evidence/style", true)); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); }

        struct Item { public Renderer r; public Bounds b; public bool axisAligned; public bool solidCollider; public int depth; }

        public static string AuditActiveScene(string folder, bool fixThinOverlaps)
        {
            var scene = EditorSceneManager.GetActiveScene();
            bool tiny = scene.path.Contains("Witch");
            float unit = tiny ? 0.25f : 1f;              // the witch level is authored at 12 cm-apple scale
            float coplanar = 0.0015f * unit, minArea = 0.01f * unit * unit, floatGap = 0.03f * unit;
            Physics.SyncTransforms();

            var items = new List<Item>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.gameObject.scene != scene || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.sharedMaterials.Any(m => m && m.renderQueue >= 2450)) continue;              // transparent/decal-blended are allowed to overlap
                if (r.GetComponentInParent<Camera>()) continue;
                var e = r.transform.rotation.eulerAngles;
                bool aligned = Mathf.Abs(Mathf.DeltaAngle(e.x, Mathf.Round(e.x / 90f) * 90f)) < 0.5f && Mathf.Abs(Mathf.DeltaAngle(e.y, Mathf.Round(e.y / 90f) * 90f)) < 0.5f && Mathf.Abs(Mathf.DeltaAngle(e.z, Mathf.Round(e.z / 90f) * 90f)) < 0.5f;
                var mf = r.GetComponent<MeshFilter>();
                bool boxy = mf && mf.sharedMesh && (mf.sharedMesh.name == "Cube" || mf.sharedMesh.name == "Quad" || mf.sharedMesh.name == "Plane");
                int depth = 0; for (var t = r.transform; t; t = t.parent) depth++;
                items.Add(new Item { r = r, b = r.bounds, axisAligned = aligned && boxy, solidCollider = r.GetComponentsInParent<Collider>().Any(c => !c.isTrigger), depth = depth });
            }

            // ---- z-fighting: two axis-aligned box faces lying in the same plane and facing the same way, overlapping in area
            var fights = new List<string>(); int nudged = 0;
            var boxes = items.Where(i => i.axisAligned).OrderBy(i => i.b.min.x).ToArray();
            for (int a = 0; a < boxes.Length; a++)
            {
                for (int c = a + 1; c < boxes.Length && boxes[c].b.min.x <= boxes[a].b.max.x + coplanar; c++)
                {
                    Bounds A = boxes[a].b, B = boxes[c].b;
                    if (!Expanded(A, coplanar).Intersects(B)) continue;
                    for (int axis = 0; axis < 3; axis++)
                    {
                        for (int side = 0; side < 2; side++)
                        {
                            float fa = side == 0 ? A.min[axis] : A.max[axis], fb = side == 0 ? B.min[axis] : B.max[axis];
                            if (Mathf.Abs(fa - fb) > coplanar) continue;
                            int u = (axis + 1) % 3, v = (axis + 2) % 3;
                            float ou = Mathf.Min(A.max[u], B.max[u]) - Mathf.Max(A.min[u], B.min[u]);
                            float ov = Mathf.Min(A.max[v], B.max[v]) - Mathf.Max(A.min[v], B.min[v]);
                            if (ou <= 0 || ov <= 0 || ou * ov < minArea) continue;
                            if (SameLook(boxes[a].r, boxes[c].r)) continue;   // identical material in the same plane cannot flicker visibly
                            var thin = Thinner(boxes[a], boxes[c], axis);
                            string line = $"Z-FIGHT {Path(boxes[a].r)} <> {Path(boxes[c].r)} axis={"XYZ"[axis]}{(side == 0 ? "-" : "+")} area={ou * ov:0.0000}m2";
                            if (fixThinOverlaps && !thin.solidCollider && thin.b.size[axis] < 0.03f * unit)
                            {
                                var d = Vector3.zero; d[axis] = (side == 0 ? -1f : 1f) * 0.003f * unit;
                                Undo.RecordObject(thin.r.transform, "Layer audit nudge");
                                thin.r.transform.position += d; nudged++;
                                line += "  -> nudged " + thin.r.name + " 3mm out";
                            }
                            fights.Add(line);
                        }
                    }
                }
            }

            // ---- floating props: nothing under the bottom and nothing touching the sides (wall-mounted is fine)
            var floating = new List<string>();
            foreach (var group in items.Where(i => IsProp(i.r)).GroupBy(i => PropRoot(i.r.transform)))
            {
                var b = group.First().b; foreach (var g in group) b.Encapsulate(g.b);
                var i = group.First(); i.r = group.Key.GetComponentInChildren<Renderer>() ?? i.r;
                if (b.size.y < 0.005f * unit) continue;
                var own = new HashSet<Collider>(group.Key.GetComponentsInChildren<Collider>());
                var origin = new Vector3(b.center.x, b.min.y + 0.01f * unit, b.center.z);
                bool below = Physics.RaycastAll(origin, Vector3.down, floatGap + 0.01f * unit, ~0, QueryTriggerInteraction.Ignore).Any(h => !own.Contains(h.collider));
                if (below) continue;
                bool side = Physics.OverlapBox(b.center, b.extents + Vector3.one * 0.02f * unit, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(c => !own.Contains(c));
                if (side) continue;
                bool hanging = i.r.name.ToLowerInvariant().Contains("lamp") || i.r.name.ToLowerInvariant().Contains("pendant") || i.r.name.ToLowerInvariant().Contains("fan");
                if (!hanging) floating.Add($"FLOATING {group.Key.name} bottomY={b.min.y:0.000}");
            }

            // ---- buried: a prop more than half inside another prop
            var buried = new List<string>();
            var props = items.Where(i => IsProp(i.r)).ToArray();
            for (int a = 0; a < props.Length; a++)
                for (int c = a + 1; c < props.Length; c++)
                {
                    if (props[a].r.transform.root == props[c].r.transform.root) continue;
                    var A = props[a].b; var B = props[c].b;
                    if (!A.Intersects(B)) continue;
                    var ov = Vector3.Max(Vector3.zero, Vector3.Min(A.max, B.max) - Vector3.Max(A.min, B.min));
                    float vol = ov.x * ov.y * ov.z, small = Mathf.Min(Vol(A), Vol(B));
                    if (small > 0 && vol / small > 0.5f) buried.Add($"BURIED {Path(props[a].r)} <> {Path(props[c].r)} overlap={vol / small:P0}");
                }

            // ---- strays: renderers far below the floor or far outside everything else
            var strays = new List<string>();
            if (items.Count > 0)
            {
                var ys = items.Select(i => i.b.center.y).OrderBy(y => y).ToArray();
                float floor = ys[ys.Length / 20];
                foreach (var i in items) if (i.b.max.y < floor - 1.5f * unit) strays.Add($"BELOW-FLOOR {Path(i.r)} y={i.b.max.y:0.00}");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Layer audit {scene.name}: renderers={items.Count} axisBoxes={boxes.Length} zfight={fights.Distinct().Count()} (nudged {nudged}) floating={floating.Count} buried={buried.Count} strays={strays.Count}");
            foreach (var l in fights.Distinct().Concat(floating).Concat(buried).Concat(strays)) sb.AppendLine(l);
            Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder, "layer_audit_" + scene.name + ".txt"), sb.ToString());
            return sb.ToString().Split('\n')[0];
        }

        static Bounds Expanded(Bounds b, float e) { b.Expand(e * 2); return b; }
        static float Vol(Bounds b) => b.size.x * b.size.y * b.size.z;
        static Item Thinner(Item a, Item b, int axis) => a.b.size[axis] <= b.b.size[axis] ? a : b;
        static bool SameLook(Renderer a, Renderer b) => a.sharedMaterials.SequenceEqual(b.sharedMaterials);

        static bool IsProp(Renderer r)
        {
            for (var t = r.transform; t; t = t.parent)
            {
                string n = t.name;
                if (n == "Props" || n.StartsWith("PROP_") || n.StartsWith("Home_")) return true;
                if (t.GetComponent<Rigidbody>()) return true;
            }
            return false;
        }

        // the whole prop: the child of "Props", or the outermost PROP_/Home_/Rigidbody object
        static Transform PropRoot(Transform t)
        {
            Transform best = t;
            for (var p = t; p; p = p.parent)
            {
                if (p.parent && p.parent.name == "Props") return p;
                if (p.name.StartsWith("PROP_") || p.name.StartsWith("Home_") || p.GetComponent<Rigidbody>()) best = p;
            }
            return best;
        }

        static string Path(Renderer r)
        {
            var parts = new List<string>(); for (var t = r.transform; t && parts.Count < 4; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
