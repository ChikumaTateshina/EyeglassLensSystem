using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrescriptionLens.Editor
{
    [CustomEditor(typeof(PrescriptionLensSettings))]
    public sealed class PrescriptionLensGeneratorEditor : UnityEditor.Editor
    {
        GameObject preview;
        Mesh previewMesh;
        Material previewMaterial;
        string status;
        bool error;
        bool advanced;
        void OnDisable() { Restore(); }
        void Restore()
        {
            if (preview) DestroyImmediate(preview);
            if (previewMesh) DestroyImmediate(previewMesh);
            if (previewMaterial) DestroyImmediate(previewMaterial);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceMesh"), new GUIContent("レンズの元メッシュ"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spherePower"), new GUIContent("球面度数 SPH (D)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cylinderPower"), new GUIContent("円柱度数 CYL (D)"));
            using (new EditorGUI.DisabledScope(serializedObject.FindProperty("cylinderPower").floatValue == 0))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cylinderAxis"), new GUIContent("軸度 AXIS (°)"));
            EditorGUILayout.HelpBox("度数はこの3項目だけで生成できます。屈折率・厚みなどは表示用の既定値を使用します。", MessageType.Info);
            advanced = EditorGUILayout.Foldout(advanced, "詳細設定（通常は変更不要）", true);
            if (advanced)
            {
                foreach (string field in new[] { "refractiveIndex", "baseCurve", "centerThicknessMm", "minimumEdgeThicknessMm",
                    "autoOrientSource", "forwardAxis", "autoOpticalCenter", "opticalCenterMm", "subdivision", "edgeSegments", "preserveSourceFrontShape", "refractive", "apparentDepthMm", "outputFolder" })
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            }
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("プレビュー")) Run(false);
            if (GUILayout.Button("レンズを生成・保存")) Run(true);
            if (GUILayout.Button("プレビューを閉じる")) Restore();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, error ? MessageType.Error : MessageType.Info);
        }
        void Run(bool save)
        {
            Mesh mesh = null; Material material = null;
            try
            {
                var s = (PrescriptionLensSettings)target;
                string folder = s.outputFolder.Replace('\\', '/').TrimEnd('/');
                if (folder == "Assets/PrescriptionLens/Generated")
                {
                    string settingsFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(s));
                    if (!string.IsNullOrEmpty(settingsFolder)) folder = settingsFolder.Replace('\\', '/') + "/Generated";
                }
                if (save && (!folder.StartsWith("Assets/", StringComparison.Ordinal) || folder.Contains("..") || folder.Contains(":")))
                    throw new ArgumentException("Output Folder must be a folder below Assets/ without '..'.");
                var result = LensMeshGenerator.Generate(s); mesh = result.mesh;
                material = LensMaterialBaker.Bake(s);
                status = $"Vertices: {mesh.vertexCount}, center: {result.centerThicknessMm:F3} mm, minimum: {result.minimumThicknessMm:F3} mm.";
                if (result.boundaryLoops > 1) status += $" Warning: {result.boundaryLoops} boundary loops (holes/disconnected surfaces).";
                if (mesh.vertexCount >= 5000) status += " Warning: per-lens vertex count >= 5000; lower Subdivision.";
                if (save)
                {
                    Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                    string name = string.Join("_", s.name.Split(Path.GetInvalidFileNameChars()));
                    AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + "_Mesh.asset")); mesh = null;
                    Material savedMaterial = material;
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".mat")); material = null;
                    var metadata = CreateInstance<PrescriptionLensMetadata>();
                    metadata.mesh = result.mesh;
                    metadata.material = savedMaterial;
                    metadata.spherePower = s.spherePower; metadata.cylinderPower = s.cylinderPower; metadata.cylinderAxis = s.cylinderAxis;
                    metadata.refractiveIndex = s.refractiveIndex; metadata.requestedCenterThicknessMm = s.centerThicknessMm;
                    metadata.actualCenterThicknessMm = result.centerThicknessMm; metadata.minimumThicknessMm = result.minimumThicknessMm;
                    metadata.vertexCount = result.mesh.vertexCount;
                    AssetDatabase.CreateAsset(metadata, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + "_Metadata.asset"));
                    AssetDatabase.SaveAssets(); EditorGUIUtility.PingObject(metadata);
                }
                else
                {
                    Restore(); previewMesh = mesh; previewMaterial = material; mesh = null; material = null;
                    preview = new GameObject("Prescription Lens Preview") { hideFlags = HideFlags.HideAndDontSave, tag = "EditorOnly" };
                    preview.AddComponent<MeshFilter>().sharedMesh = previewMesh;
                    preview.AddComponent<MeshRenderer>().sharedMaterial = previewMaterial;
                    if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(previewMesh.bounds, false);
                    SceneView.RepaintAll();
                }
                error = false;
            }
            catch (Exception ex) { error = true; status = ex.Message; }
            finally { if (mesh) DestroyImmediate(mesh); if (material) DestroyImmediate(material); }
        }
    }
}
