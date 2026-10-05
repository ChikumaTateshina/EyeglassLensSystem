using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrescriptionLens.Editor
{
    public static class LensMeshGenerator
    {
        public sealed class Result
        {
            public Mesh mesh;
            public float centerThicknessMm, minimumThicknessMm;
            public int boundaryLoops;
        }

        public static void Basis(LensForwardAxis axis, out Vector3 x, out Vector3 y, out Vector3 z)
        {
            switch (axis)
            {
                case LensForwardAxis.NegativeZ: z = Vector3.back; break;
                case LensForwardAxis.PositiveX: z = Vector3.right; break;
                case LensForwardAxis.NegativeX: z = Vector3.left; break;
                case LensForwardAxis.PositiveY: z = Vector3.up; break;
                case LensForwardAxis.NegativeY: z = Vector3.down; break;
                default: z = Vector3.forward; break;
            }
            y = Mathf.Abs(z.y) > .5f ? Vector3.forward : Vector3.up;
            x = Vector3.Cross(y, z).normalized;
            y = Vector3.Cross(z, x);
        }

        public static void ResolveBasis(PrescriptionLensSettings s, out Vector3 x, out Vector3 y, out Vector3 z)
        {
            LensForwardAxis axis = s.forwardAxis;
            if (s.autoOrientSource && s.sourceMesh && s.sourceMesh.isReadable)
            {
                Vector3[] vertices = s.sourceMesh.vertices;
                Vector3 sum = Vector3.zero;
                int[] triangles = s.sourceMesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                    sum += Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                Vector3 weights = new Vector3(Mathf.Abs(sum.x), Mathf.Abs(sum.y), Mathf.Abs(sum.z));
                if (weights.z >= weights.x && weights.z >= weights.y && weights.z > 0)
                    axis = sum.z >= 0 ? LensForwardAxis.PositiveZ : LensForwardAxis.NegativeZ;
                else if (weights.y >= weights.x && weights.y > 0)
                    axis = sum.y >= 0 ? LensForwardAxis.PositiveY : LensForwardAxis.NegativeY;
                else if (weights.x > 0)
                    axis = sum.x >= 0 ? LensForwardAxis.PositiveX : LensForwardAxis.NegativeX;
            }
            Basis(axis, out x, out y, out z);
        }

        static Vector3[] OpticalCenters(List<Vector3> positions, List<int> triangles, Vector3 bx, Vector3 by, PrescriptionLensSettings s)
        {
            var centers = new Vector3[positions.Count];
            Vector3 shift = bx * s.opticalCenterMm.x * .001f + by * s.opticalCenterMm.y * .001f;
            if (!s.autoOpticalCenter) { for (int i = 0; i < centers.Length; i++) centers[i] = shift; return centers; }
            var neighbors = new List<int>[positions.Count];
            for (int i = 0; i < neighbors.Length; i++) neighbors[i] = new List<int>();
            for (int i = 0; i < triangles.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = triangles[i + k], b = triangles[i + (k + 1) % 3];
                    neighbors[a].Add(b); neighbors[b].Add(a);
                }
            var visited = new bool[positions.Count];
            for (int start = 0; start < positions.Count; start++)
            {
                if (visited[start]) continue;
                var component = new List<int>(); var stack = new Stack<int>();
                stack.Push(start); visited[start] = true;
                float minX = float.PositiveInfinity, minY = minX, maxX = float.NegativeInfinity, maxY = maxX;
                while (stack.Count > 0)
                {
                    int id = stack.Pop(); component.Add(id);
                    float px = Vector3.Dot(positions[id], bx), py = Vector3.Dot(positions[id], by);
                    minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px); minY = Mathf.Min(minY, py); maxY = Mathf.Max(maxY, py);
                    foreach (int next in neighbors[id]) if (!visited[next]) { visited[next] = true; stack.Push(next); }
                }
                Vector3 center = bx * ((minX + maxX) * .5f) + by * ((minY + maxY) * .5f) + shift;
                foreach (int id in component) centers[id] = center;
            }
            return centers;
        }

        public static Result Generate(PrescriptionLensSettings s)
        {
            if (!s.sourceMesh) throw new ArgumentException("Source Mesh is required.");
            if (!s.sourceMesh.isReadable) throw new ArgumentException("Enable Read/Write on the source mesh importer.");
            LensOpticsCalculator.RequireFinite(s.spherePower, s.cylinderPower, s.cylinderAxis, s.refractiveIndex,
                s.baseCurve, s.centerThicknessMm, s.minimumEdgeThicknessMm, s.opticalCenterMm.x, s.opticalCenterMm.y, s.apparentDepthMm);
            if (s.refractiveIndex <= 1 || s.centerThicknessMm <= 0 || s.minimumEdgeThicknessMm <= 0 || s.apparentDepthMm < 0)
                throw new ArgumentException("Index > 1, positive thicknesses and nonnegative apparent depth are required.");
            if (s.subdivision < 0 || s.subdivision > 4 || s.edgeSegments < 1 || s.edgeSegments > 8)
                throw new ArgumentException("Subdivision must be 0..4 and Edge Segments 1..8.");
            ResolveBasis(s, out Vector3 bx, out Vector3 by, out Vector3 bz);
            var positions = new List<Vector3>(); var uv = new List<Vector2>();
            var weld = new Dictionary<Vector3, int>();
            Vector3[] original = s.sourceMesh.vertices; Vector2[] originalUv = s.sourceMesh.uv;
            var remap = new int[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                Vector3 p = original[i]; LensOpticsCalculator.RequireFinite(p.x, p.y, p.z);
                if (!weld.TryGetValue(p, out int id))
                {
                    id = positions.Count; weld.Add(p, id); positions.Add(p);
                    Vector2 sourceUv = originalUv.Length == original.Length ? originalUv[i] : new Vector2(Vector3.Dot(p, bx), Vector3.Dot(p, by));
                    LensOpticsCalculator.RequireFinite(sourceUv.x, sourceUv.y);
                    uv.Add(sourceUv);
                }
                remap[i] = id;
            }
            var triangles = new List<int>();
            for (int sub = 0; sub < s.sourceMesh.subMeshCount; sub++)
            {
                if (s.sourceMesh.GetTopology(sub) != MeshTopology.Triangles) throw new ArgumentException("Source must use triangle topology.");
                foreach (int id in s.sourceMesh.GetTriangles(sub)) triangles.Add(remap[id]);
            }
            if (triangles.Count == 0) throw new ArgumentException("Source has no triangles.");
            float orientation = 0;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 normal = Vector3.Cross(positions[triangles[i + 1]] - positions[triangles[i]], positions[triangles[i + 2]] - positions[triangles[i]]);
                float sign = Vector3.Dot(normal, bz);
                if (normal.sqrMagnitude < 1e-28f) throw new ArgumentException($"元Meshの三角形 {i / 3} の面積がほぼゼロです。重複頂点や一直線の頂点を確認してください。");
                if (Mathf.Abs(sign) < 1e-14f) throw new ArgumentException($"元Meshの三角形 {i / 3} がレンズ面に対して横向きです。前面のみのMeshを指定するか、詳細設定の自動方向判定を確認してください。");
                if (orientation == 0) orientation = Mathf.Sign(sign);
                if (sign * orientation <= 0) throw new ArgumentException("Source must be a consistently oriented surface without folds in the lens plane.");
            }
            if (orientation < 0)
                for (int i = 0; i < triangles.Count; i += 3) { int t = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = t; }
            for (int level = 0; level < s.subdivision; level++)
            {
                var midpoints = new Dictionary<long, int>(); var refined = new List<int>();
                Func<int, int, int> midpoint = (a, b) =>
                {
                    long key = LensBoundaryBuilder.Key(a, b);
                    if (!midpoints.TryGetValue(key, out int id))
                    { id = positions.Count; positions.Add((positions[a] + positions[b]) * .5f); uv.Add((uv[a] + uv[b]) * .5f); midpoints.Add(key, id); }
                    return id;
                };
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    int ab = midpoint(a, b), bc = midpoint(b, c), ca = midpoint(c, a);
                    refined.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
                }
                triangles = refined;
            }
            var boundary = LensBoundaryBuilder.Build(triangles, out int loops);
            int count = positions.Count;
            Vector3[] centers = OpticalCenters(positions, triangles, bx, by, s);
            var front = new Vector3[count]; var back = new Vector3[count]; var thickness = new float[count];
            double d = s.centerThicknessMm * .001, axis = LensOpticsCalculator.NormalizeAxis(s.cylinderAxis) * Math.PI / 180;
            double plane = 0;
            foreach (Vector3 p in positions) plane += Vector3.Dot(p, bz) / count;
            double minimum = 0;
            bool converged = false;
            // Recalculate the thick-lens back curvature when minimum thickness increases the center.
            for (int iteration = 0; iteration < 64; iteration++)
            {
                minimum = double.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    double x = Vector3.Dot(positions[i] - centers[i], bx);
                    double y = Vector3.Dot(positions[i] - centers[i], by);
                    double fs = LensOpticsCalculator.Sag(s.baseCurve / (s.refractiveIndex - 1), Math.Sqrt(x * x + y * y));
                    double bs = LensOpticsCalculator.BackSag(x, y, s.spherePower, s.cylinderPower, axis, s.baseCurve, s.refractiveIndex, d);
                    double fz = s.preserveSourceFrontShape ? Vector3.Dot(positions[i], bz) : plane - fs;
                    double bzDepth = plane - d - bs;
                    double t = fz - bzDepth;
                    minimum = Math.Min(minimum, t); thickness[i] = (float)t;
                    Vector3 lateral = positions[i] - bz * Vector3.Dot(positions[i], bz);
                    front[i] = lateral + bz * (float)fz; back[i] = lateral + bz * (float)bzDepth;
                }
                double required = s.minimumEdgeThicknessMm * .001 - minimum;
                if (required <= 1e-9) { converged = true; break; }
                d += required + 1e-8;
            }
            if (!converged) throw new ArgumentException("Minimum thickness correction did not converge.");
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var outputUv = new List<Vector2>();
            var opticalData = new List<Vector4>();
            for (int surface = 0; surface < 2; surface++)
                for (int i = 0; i < count; i++)
                { vertices.Add(surface == 0 ? front[i] : back[i]); outputUv.Add(uv[i]); colors.Add(new Color(surface * .5f, thickness[i] * 1000f, 0, 1)); opticalData.Add(new Vector4(centers[i].x, centers[i].y, centers[i].z, 1)); }
            var indices = new List<int>(triangles);
            for (int i = 0; i < triangles.Count; i += 3)
                indices.AddRange(new[] { triangles[i] + count, triangles[i + 2] + count, triangles[i + 1] + count });
            // Separate edge vertices keep optical surface normals smooth and the bevel sharp.
            foreach (var e in boundary)
            {
                int start = vertices.Count;
                for (int segment = 0; segment <= s.edgeSegments; segment++)
                {
                    float t = (float)segment / s.edgeSegments;
                    vertices.Add(Vector3.Lerp(front[e.a], back[e.a], t)); vertices.Add(Vector3.Lerp(front[e.b], back[e.b], t));
                    outputUv.Add(new Vector2(0, t)); outputUv.Add(new Vector2(1, t));
                    colors.Add(new Color(1, thickness[e.a] * 1000f, 0, 1)); colors.Add(new Color(1, thickness[e.b] * 1000f, 0, 1));
                    opticalData.Add(new Vector4(centers[e.a].x, centers[e.a].y, centers[e.a].z, 1));
                    opticalData.Add(new Vector4(centers[e.b].x, centers[e.b].y, centers[e.b].z, 1));
                    if (segment < s.edgeSegments)
                    { int a = start + segment * 2; indices.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
                }
            }
            foreach (Vector3 p in vertices) LensOpticsCalculator.RequireFinite(p.x, p.y, p.z);
            for (int i = 0; i < indices.Count; i += 3)
                if (Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]], vertices[indices[i + 2]] - vertices[indices[i]]).sqrMagnitude < 1e-28f)
                    throw new ArgumentException("Generated geometry contains a degenerate triangle.");
            var mesh = new Mesh { name = s.sourceMesh.name + "_PrescriptionLens", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, outputUv); mesh.SetUVs(1, opticalData); mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return new Result { mesh = mesh, centerThicknessMm = (float)(d * 1000), minimumThicknessMm = (float)(minimum * 1000), boundaryLoops = loops };
        }
    }
}
