using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PrescriptionLens.Editor
{
    public static class LensRelease
    {
        public static void ExportBatch()
        {
            try
            {
                Directory.CreateDirectory("dist");
                AssetDatabase.ExportPackage("Assets/PrescriptionLens", "dist/EyeglassLensSystem-1.0.0.unitypackage", ExportPackageOptions.Recurse);
                Debug.Log("Exported release unitypackage.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
