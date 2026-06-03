using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Simple ear-clipping polygon triangulator for 2D simple polygons (no holes).
    ///
    /// Input: ordered ring of Vector2 points (last != first).
    /// Output: triangle indices (CCW from +Y view) into the input array.
    ///
    /// 1:110m Natural Earth land polygons are simple in 99% of cases, so this is sufficient.
    /// If a polygon has holes (rare in land data — usually lakes), the hole is silently ignored.
    /// </summary>
    public static class EarClippingTriangulator
    {
        public static List<int> Triangulate(Vector2[] points)
        {
            var result = new List<int>();
            int n = points.Length;
            if (n < 3) return result;

            // 1) Build index list in CCW order
            var indices = new List<int>(n);
            if (SignedArea(points) > 0f)
            {
                // CW input → reverse to CCW
                for (int i = n - 1; i >= 0; i--) indices.Add(i);
            }
            else
            {
                for (int i = 0; i < n; i++) indices.Add(i);
            }

            // 2) Ear clipping
            int safety = n * n; // O(n^2) hard cap
            while (indices.Count > 3 && safety-- > 0)
            {
                bool earFound = false;
                int count = indices.Count;
                for (int i = 0; i < count; i++)
                {
                    int pi = indices[(i - 1 + count) % count];
                    int ci = indices[i];
                    int ni = indices[(i + 1) % count];

                    if (IsEar(points, pi, ci, ni, indices))
                    {
                        result.Add(pi);
                        result.Add(ci);
                        result.Add(ni);
                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
                if (!earFound) break; // degenerate — stop early
            }

            if (indices.Count == 3)
            {
                result.Add(indices[0]);
                result.Add(indices[1]);
                result.Add(indices[2]);
            }

            return result;
        }

        // Shoelace; >0 = CW (in screen space where Y is down), <0 = CCW.
        // We treat (lng, lat) like (x, y) math coords, so >0 = CW from above.
        private static float SignedArea(Vector2[] pts)
        {
            float a = 0f;
            int n = pts.Length;
            for (int i = 0; i < n; i++)
            {
                var p = pts[i];
                var q = pts[(i + 1) % n];
                a += (q.x - p.x) * (q.y + p.y);
            }
            return a;
        }

        private static bool IsEar(Vector2[] pts, int pi, int ci, int ni, List<int> ring)
        {
            var a = pts[pi];
            var b = pts[ci];
            var c = pts[ni];

            // Convex? cross product (CCW polygon → ear vertices have positive cross)
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (cross <= 0f) return false;

            // No other vertex inside?
            for (int k = 0; k < ring.Count; k++)
            {
                int idx = ring[k];
                if (idx == pi || idx == ci || idx == ni) continue;
                if (PointInTriangle(pts[idx], a, b, c)) return false;
            }
            return true;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }
    }
}
