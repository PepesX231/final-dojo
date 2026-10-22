using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.Boss
{
    /// <summary>
    /// Chooses where the chair flees to (GDD §15.5): reachable candidates only, scored by distance from the
    /// player, "away" direction, detour, recency and corner risk; weighted pick among the best few so the chair
    /// is smart but not optimal (§15.6). Path queries are injectable so the logic is unit-testable.
    /// </summary>
    public sealed class EscapeDestinationSelector
    {
        public struct Candidate
        {
            public Vector3 Position;
            /// <summary>Distance to the nearest NavMesh edge (small = near a wall/corner).</summary>
            public float Openness;
        }

        public delegate bool PathQuery(Vector3 from, Vector3 to, List<Vector3> cornersOut, out float length);

        public struct Result
        {
            public bool Found;
            public Vector3 Destination;
            public float Score;
            public int CandidatesConsidered, CandidatesValid;
        }

        readonly List<Candidate> _candidates;
        readonly PathQuery _path;
        readonly ChairAIConfig _cfg;
        readonly System.Random _rng;
        readonly List<(Vector3 pos, float time)> _recent = new List<(Vector3, float)>();
        readonly List<Vector3> _tmpCorners = new List<Vector3>(16);
        readonly List<(int index, float score, List<Vector3> corners)> _scored = new List<(int, float, List<Vector3>)>();

        public float Weight_DistanceFromPlayer = 1.0f, Weight_Away = 0.8f, Weight_Detour = 0.6f, Weight_Recent = 1.2f, Weight_Corner = 0.5f;

        public IReadOnlyList<Candidate> Candidates => _candidates;
        public List<Vector3> LastPath { get; } = new List<Vector3>(16);

        public EscapeDestinationSelector(List<Candidate> candidates, PathQuery path, ChairAIConfig cfg, int seed)
        {
            _candidates = candidates;
            _path = path;
            _cfg = cfg;
            _rng = new System.Random(seed);
        }

        public void Remember(Vector3 destination, float time) => _recent.Add((destination, time));

        public Result Choose(Vector3 chair, Vector3 player, float time)
        {
            _recent.RemoveAll(r => time - r.time > _cfg.recentMemorySeconds);
            _scored.Clear();
            var res = new Result();
            Vector3 awayDir = Flat(chair - player);
            awayDir = awayDir.sqrMagnitude > 1e-4f ? awayDir.normalized : Vector3.forward;

            for (int i = 0; i < _candidates.Count; i++)
            {
                var c = _candidates[i];
                res.CandidatesConsidered++;
                float dChair = Flat(c.Position - chair).magnitude;
                if (dChair < _cfg.minEscapeDistance || dChair > _cfg.maxEscapeDistance) continue;
                float dPlayer = Flat(c.Position - player).magnitude;
                if (dPlayer < _cfg.minDistanceFromPlayer) continue;

                _tmpCorners.Clear();
                if (!_path(chair, c.Position, _tmpCorners, out float length)) continue; // unreachable
                if (PathPassesNear(_tmpCorners, player, _cfg.pathClearanceFromPlayer)) continue;

                float away = Vector3.Dot(Flat(c.Position - chair).normalized, awayDir);
                float detour = length / Mathf.Max(dChair, 0.1f) - 1f;
                float recency = 0f;
                foreach (var r in _recent)
                    if (Flat(c.Position - r.pos).magnitude < 3f)
                        recency = Mathf.Max(recency, 1f - (time - r.time) / _cfg.recentMemorySeconds);
                float corner = c.Openness < 1.5f ? 1f - c.Openness / 1.5f : 0f;

                float score = Weight_DistanceFromPlayer * Mathf.Clamp01(dPlayer / (_cfg.maxEscapeDistance + 4f))
                              + Weight_Away * away
                              - Weight_Detour * detour
                              - Weight_Recent * recency
                              - Weight_Corner * corner;
                _scored.Add((i, score, new List<Vector3>(_tmpCorners)));
                res.CandidatesValid++;
            }

            if (_scored.Count == 0) return res;
            _scored.Sort((a, b) => b.score.CompareTo(a.score));
            int k = Mathf.Min(_cfg.topChoices, _scored.Count);
            float min = _scored[k - 1].score;
            float total = 0f;
            for (int i = 0; i < k; i++) total += _scored[i].score - min + 0.15f;
            double roll = _rng.NextDouble() * total;
            int pick = 0;
            for (int i = 0; i < k; i++)
            {
                roll -= _scored[i].score - min + 0.15f;
                if (roll <= 0) { pick = i; break; }
            }
            var chosen = _scored[pick];
            res.Found = true;
            res.Destination = _candidates[chosen.index].Position;
            res.Score = chosen.score;
            LastPath.Clear();
            LastPath.AddRange(chosen.corners);
            return res;
        }

        /// <summary>Second attempt with looser limits before the chair counts as cornered.</summary>
        public Result ChooseRelaxed(Vector3 chair, Vector3 player, float time)
        {
            float a = _cfg.minEscapeDistance, b = _cfg.minDistanceFromPlayer, c = _cfg.pathClearanceFromPlayer, d = _cfg.maxEscapeDistance;
            _cfg.minEscapeDistance = 2.5f; _cfg.minDistanceFromPlayer = 2.5f; _cfg.pathClearanceFromPlayer = 1.0f; _cfg.maxEscapeDistance = d * 1.5f;
            try { return Choose(chair, player, time); }
            finally { _cfg.minEscapeDistance = a; _cfg.minDistanceFromPlayer = b; _cfg.pathClearanceFromPlayer = c; _cfg.maxEscapeDistance = d; }
        }

        /// <summary>Last resort when cornered against walls: any reachable spot, even past the player.</summary>
        public Result ChooseSqueeze(Vector3 chair, Vector3 player, float time)
        {
            float a = _cfg.minEscapeDistance, b = _cfg.minDistanceFromPlayer, c = _cfg.pathClearanceFromPlayer, d = _cfg.maxEscapeDistance;
            _cfg.minEscapeDistance = 3f; _cfg.minDistanceFromPlayer = 1.5f; _cfg.pathClearanceFromPlayer = 0.6f; _cfg.maxEscapeDistance = d * 2f;
            try { return Choose(chair, player, time); }
            finally { _cfg.minEscapeDistance = a; _cfg.minDistanceFromPlayer = b; _cfg.pathClearanceFromPlayer = c; _cfg.maxEscapeDistance = d; }
        }

        public float NextFloat() => (float)_rng.NextDouble();

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static bool PathPassesNear(List<Vector3> corners, Vector3 p, float clearance)
        {
            for (int i = 0; i + 1 < corners.Count; i++)
            {
                Vector3 a = Flat(corners[i]), b = Flat(corners[i + 1]), q = Flat(p);
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                // ignore the very start of the path (the chair is already near the player when fleeing)
                if (i == 0 && t * ab.magnitude < 1.0f) continue;
                if ((a + ab * t - q).magnitude < clearance) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- NavMesh helpers
        public static bool NavMeshPath(Vector3 from, Vector3 to, List<Vector3> cornersOut, out float length)
        {
            length = 0f;
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(from, out var hf, 1.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(hf.position, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            var c = path.corners;
            for (int i = 0; i < c.Length; i++)
            {
                cornersOut.Add(c[i]);
                if (i > 0) length += Vector3.Distance(c[i - 1], c[i]);
            }
            return c.Length > 0;
        }

        /// <summary>Grid of NavMesh points inside the bounds, with distance-to-edge (openness).</summary>
        public static List<Candidate> BuildCandidates(Bounds bounds, float spacing)
        {
            var list = new List<Candidate>();
            for (float x = bounds.min.x + spacing * 0.5f; x < bounds.max.x; x += spacing)
            for (float z = bounds.min.z + spacing * 0.5f; z < bounds.max.z; z += spacing)
            {
                var probe = new Vector3(x, bounds.center.y, z);
                if (!NavMesh.SamplePosition(probe, out var hit, spacing * 0.5f, NavMesh.AllAreas)) continue;
                if (!bounds.Contains(new Vector3(hit.position.x, bounds.center.y, hit.position.z))) continue;
                float open = NavMesh.FindClosestEdge(hit.position, out var edge, NavMesh.AllAreas) ? edge.distance : 0f;
                list.Add(new Candidate { Position = hit.position, Openness = open });
            }
            return list;
        }
    }
}
