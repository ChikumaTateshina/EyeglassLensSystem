using UnityEditor;
using UnityEngine;

namespace PrescriptionLens.Editor
{
    public sealed class PrescriptionLensShaderGUI : ShaderGUI
    {
        bool advanced;
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(FindProperty("_SpherePower", properties), "球面度数 SPH (D)");
            var cylinder = FindProperty("_CylinderPower", properties);
            editor.ShaderProperty(cylinder, "円柱度数 CYL (D)");
            using (new EditorGUI.DisabledScope(!cylinder.hasMixedValue && cylinder.floatValue == 0))
                editor.ShaderProperty(FindProperty("_CylinderAxis", properties), "軸度 AXIS (°)");
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Edit lens prescription");
                // Manual prescription edits override coefficients baked by the generator.
                foreach (Object target in editor.targets)
                {
                    var material = (Material)target;
                    material.SetFloat("_UseBakedOptics", 0);
                    EditorUtility.SetDirty(material);
                }
            }
            EditorGUILayout.HelpBox("度数は3項目のみ入力してください。見た目の設定は既定値のまま使えます。Meshの厚みも変更する場合はレンズ設定Assetから再生成します。", MessageType.Info);
            advanced = EditorGUILayout.Foldout(advanced, "見た目・描画の詳細設定（通常は変更不要）", true);
            if (advanced)
            {
                foreach (MaterialProperty p in properties)
                {
                    if ((p.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0) continue;
                    if (p.name == "_SpherePower" || p.name == "_CylinderPower" || p.name == "_CylinderAxis" || p.name == "_UseBakedOptics") continue;
                    editor.ShaderProperty(p, p.displayName);
                }
                editor.RenderQueueField(); editor.EnableInstancingField();
            }
        }
    }
}
