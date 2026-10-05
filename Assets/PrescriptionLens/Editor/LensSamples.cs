using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrescriptionLens.Editor
{
    public static class LensSamples
    {


        public static Mesh Circle()
        {
            const int segments = 32;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * .025f, Mathf.Sin(a) * .025f, 0);
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            var mesh = new Mesh { name = "CircularFront50mm", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        [MenuItem("Tools/Prescription Lens/Create Sample Assets")]
        public static void CreateSamples()
        {
            const string folder = "Assets/PrescriptionLens/Samples";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var source = Circle();
            AssetDatabase.CreateAsset(source, AssetDatabase.GenerateUniqueAssetPath(folder + "/CircularFront.asset"));
            string[] names = { "Plano", "Myopia", "HighMyopia", "Hyperopia", "Astigmatism", "RotatedAstigmatism" };
            float[] powers = { 0, -3, -10, 5, -3, -3 };
            for (int i = 0; i < names.Length; i++)
            {
                var s = ScriptableObject.CreateInstance<PrescriptionLensSettings>();
                s.name = names[i]; s.sourceMesh = source; s.spherePower = powers[i];
                s.cylinderPower = i >= 4 ? -2 : 0; s.cylinderAxis = i == 5 ? 45 : 180;
                AssetDatabase.CreateAsset(s, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + names[i] + ".asset"));
                var result = LensMeshGenerator.Generate(s);
                AssetDatabase.CreateAsset(result.mesh, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + names[i] + "_Mesh.asset"));
                var material = LensMaterialBaker.Bake(s);
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + names[i] + ".mat"));
            }
            AssetDatabase.SaveAssets();
        }




    }
}
