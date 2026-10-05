using System;
using System.Collections.Generic;

namespace PrescriptionLens.Editor
{
    public static class LensBoundaryBuilder
    {
        public struct Edge
        {
            public int a, b;
            public Edge(int a, int b) { this.a = a; this.b = b; }
        }
        public static long Key(int a, int b) => ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);

        public static List<Edge> Build(List<int> triangles, out int loopCount)
        {
            var edges = new Dictionary<long, Edge>();
            var counts = new Dictionary<long, int>();
            for (int i = 0; i < triangles.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    var e = new Edge(triangles[i + k], triangles[i + (k + 1) % 3]);
                    long key = Key(e.a, e.b);
                    if (counts.ContainsKey(key))
                    {
                        if (++counts[key] > 2) throw new ArgumentException("Non-manifold source edge.");
                        Edge previous = edges[key];
                        if (previous.a == e.a) throw new ArgumentException("Inconsistent triangle winding.");
                    }
                    else { counts[key] = 1; edges[key] = e; }
                }
            var next = new Dictionary<int, Edge>();
            var incoming = new HashSet<int>();
            foreach (var pair in counts)
                if (pair.Value == 1)
                {
                    Edge e = edges[pair.Key];
                    if (next.ContainsKey(e.a) || !incoming.Add(e.b)) throw new ArgumentException("Boundary branches at a vertex.");
                    next.Add(e.a, e);
                }
            if (next.Count == 0) throw new ArgumentException("Boundary not found: supply an open front surface.");
            var result = new List<Edge>();
            var visited = new HashSet<int>();
            loopCount = 0;
            foreach (int start in next.Keys)
            {
                if (visited.Contains(start)) continue;
                loopCount++;
                int current = start;
                do
                {
                    if (!visited.Add(current) || !next.ContainsKey(current)) throw new ArgumentException("Boundary loop is not closed.");
                    Edge e = next[current]; result.Add(e); current = e.b;
                } while (current != start);
            }
            return result;
        }
    }
}
