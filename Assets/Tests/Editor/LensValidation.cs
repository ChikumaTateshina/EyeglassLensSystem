using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PrescriptionLens.Editor
{
    public static class LensValidation
    {
        public static void CompileShadersBatch()
        {
            var report = new List<string>();
            try
            {
                foreach (string name in new[] { "PrescriptionLens/Lens", "PrescriptionLens/Standard" })
                {
                    Shader shader = Shader.Find(name);
                    if (!shader) throw new Exception("Missing shader: " + name);
                    foreach (string keyword in new[] { "", "INSTANCING_ON", "UNITY_SINGLE_PASS_STEREO", "STEREO_INSTANCING_ON" })
                    {
                        var material = new Material(shader);
                        if (keyword.Length > 0) material.EnableKeyword(keyword);
                        if (keyword == "STEREO_INSTANCING_ON") material.EnableKeyword("INSTANCING_ON");
                        ShaderUtil.CompilePass(material, 0, true);
                        if (ShaderUtil.ShaderHasError(shader))
                        {
                            foreach (var message in ShaderUtil.GetShaderMessages(shader)) report.Add(message.message);
                            throw new Exception("Shader failed: " + name + " " + keyword);
                        }
                        report.Add("PASS D3D11 shader variant: " + name + " " + keyword);
                        UnityEngine.Object.DestroyImmediate(material);
                    }
                }
                File.WriteAllLines("shader-validation-results.txt", report);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                report.Add("FAIL " + ex); File.WriteAllLines("shader-validation-results.txt", report);
                Debug.LogException(ex); EditorApplication.Exit(1);
            }
        }

        public static void ValidateBatch()
        {
            var report = new List<string>();
            Mesh source = null;
            try
            {
                foreach (string name in new[] { "PrescriptionLens/Lens", "PrescriptionLens/Standard" })
                {
                    Shader shader = Shader.Find(name);
                    if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Shader compilation failed: " + name);
                    report.Add("PASS shader import: " + name);
                }
                source = LensSamples.Circle(); Vector3[] original = source.vertices;
                float[] powers = { 0, -3, -10, 5, -3, -3 };
                for (int scenario = 0; scenario < powers.Length; scenario++)
                    for (int subdivision = 0; subdivision <= 2; subdivision++)
                    {
                        var s = ScriptableObject.CreateInstance<PrescriptionLensSettings>();
                        s.sourceMesh = source; s.spherePower = powers[scenario]; s.subdivision = subdivision;
                        s.cylinderPower = scenario >= 4 ? -2 : 0; s.cylinderAxis = scenario == 5 ? 45 : 180;
                        var result = LensMeshGenerator.Generate(s);
                        if (result.minimumThicknessMm < s.minimumEdgeThicknessMm - .0001f) throw new Exception("Thickness below minimum.");
                        ValidateClosed(result.mesh);
                        foreach (Vector3 normal in result.mesh.normals)
                            if (normal.sqrMagnitude < .9f) throw new Exception("Invalid generated normal.");
                        Material material = LensMaterialBaker.Bake(s);
                        if (material.GetFloat("_UseBakedOptics") != 1) throw new Exception("Material bake disabled.");
                        UnityEngine.Object.DestroyImmediate(material);
                        report.Add($"PASS mesh case {scenario}, subdivision {subdivision}: {result.mesh.vertexCount} vertices, center {result.centerThicknessMm:F3}mm");
                        UnityEngine.Object.DestroyImmediate(result.mesh); UnityEngine.Object.DestroyImmediate(s);
                    }
                for (int i = 0; i < original.Length; i++) if (source.vertices[i] != original[i]) throw new Exception("Source mesh was modified.");
                report.Add("PASS source mesh preserved");
                if (!AssetDatabase.LoadAssetAtPath<Mesh>("Assets/PrescriptionLens/Samples/CircularFront.asset")) LensSamples.CreateSamples();
                report.Add("PASS sample assets available");
                File.WriteAllLines("validation-results.txt", report);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                report.Add("FAIL " + ex); File.WriteAllLines("validation-results.txt", report);
                Debug.LogException(ex); EditorApplication.Exit(1);
            }
            finally { if (source) UnityEngine.Object.DestroyImmediate(source); }
        }

        public static void ValidateClosed(Mesh mesh)
        {
            var weld = new Dictionary<Vector3, int>(); var mapping = new int[mesh.vertexCount];
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i]; LensOpticsCalculator.RequireFinite(p.x, p.y, p.z);
                if (!weld.TryGetValue(p, out int id)) { id = weld.Count; weld.Add(p, id); }
                mapping[i] = id;
            }
            var count = new Dictionary<long, int>(); var direction = new Dictionary<long, int>();
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = mapping[triangles[i + k]], b = mapping[triangles[i + (k + 1) % 3]];
                    long key = LensBoundaryBuilder.Key(a, b);
                    if (!count.ContainsKey(key)) { count[key] = 0; direction[key] = 0; }
                    count[key]++; direction[key] += a < b ? 1 : -1;
                }
            foreach (long key in count.Keys)
                if (count[key] != 2 || direction[key] != 0) throw new Exception("Mesh has a boundary or inconsistent winding.");
        }
    }
}
