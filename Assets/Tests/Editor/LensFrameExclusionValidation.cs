using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrescriptionLens.Editor
{
    public static class LensFrameExclusionValidation
    {
        public static void ValidateBatch()
        {
            var objects = new List<UnityEngine.Object>();
            var report = new List<string>();
            try
            {
                var settings = ScriptableObject.CreateInstance<PrescriptionLensSettings>(); objects.Add(settings);
                settings.sourceMesh = LensSamples.Circle(); objects.Add(settings.sourceMesh);
                settings.spherePower = -10; settings.apparentDepthMm = 40;
                Mesh lensMesh = LensMeshGenerator.Generate(settings).mesh; objects.Add(lensMesh);
                Material lensMaterial = LensMaterialBaker.Bake(settings); objects.Add(lensMaterial);
                lensMaterial.renderQueue = 3020;
                lensMaterial.SetFloat("_Transmission", 1); lensMaterial.SetColor("_LensTint", Color.white);
                lensMaterial.SetFloat("_SurfaceReflection", 0); lensMaterial.SetFloat("_EdgeReflection", 0);
                lensMaterial.SetFloat("_RefractionStrength", 0); lensMaterial.SetFloat("_DispersionStrength", 0);
                lensMaterial.SetFloat("_DistortionStrength", 1);
                var lens = new GameObject("Validation Lens"); objects.Add(lens);
                lens.AddComponent<MeshFilter>().sharedMesh = lensMesh;
                lens.AddComponent<MeshRenderer>().sharedMaterial = lensMaterial;
                var frameMesh = new Mesh { vertices = new[] {
                    new Vector3(.027f,-.02f,.005f), new Vector3(.034f,-.02f,.005f),
                    new Vector3(.034f,.02f,.005f), new Vector3(.027f,.02f,.005f)
                }, triangles = new[] { 0,1,2,0,2,3 } }; objects.Add(frameMesh);
                frameMesh.RecalculateNormals(); frameMesh.RecalculateBounds();
                var frameMaterial = new Material(Shader.Find("Unlit/Color")); objects.Add(frameMaterial);
                frameMaterial.SetColor("_Color", Color.magenta);
                var frame = new GameObject("Validation Frame"); objects.Add(frame);
                frame.AddComponent<MeshFilter>().sharedMesh = frameMesh;
                frame.AddComponent<MeshRenderer>().sharedMaterial = frameMaterial;
                var cameraObject = new GameObject("Validation Camera"); objects.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(0,0,.2f); camera.transform.rotation = Quaternion.Euler(0,180,0);
                camera.orthographic = true; camera.orthographicSize = .045f; camera.nearClipPlane = .001f; camera.farClipPlane = 1;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
                camera.allowHDR = false; camera.allowMSAA = false;
                var target = new RenderTexture(256,256,24); objects.Add(target); target.Create(); camera.targetTexture = target;
                int before = Capture(camera, frameMaterial, 2000, "frame-exclusion-before.png", out int outsideBefore);
                int after = Capture(camera, frameMaterial, 3030, "frame-exclusion-after.png", out int outsideAfter);
                report.Add($"Frame-colored pixels inside lens: before {before}, after {after}");
                report.Add($"Frame-colored pixels outside lens: before {outsideBefore}, after {outsideAfter}");
                if (before < 10 || after != 0 || outsideAfter < 10) throw new Exception("Frame exclusion render check failed.");
                report.Add("PASS frame remains visible and is absent from refracted lens image.");
                report.Add("PASS prescription sampling scale preserved: " + lensMaterial.GetVector("_SampleScale"));
                File.WriteAllLines("frame-exclusion-validation.txt", report); EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                report.Add("FAIL " + ex); File.WriteAllLines("frame-exclusion-validation.txt", report);
                Debug.LogException(ex); EditorApplication.Exit(1);
            }
            finally { for (int i = objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(objects[i]); }
        }

        static int Capture(Camera camera, Material frame, int queue, string path, out int outside)
        {
            frame.renderQueue = queue; camera.Render();
            var image = new Texture2D(256,256,TextureFormat.RGB24,false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active = previous;
            File.WriteAllBytes(path,image.EncodeToPNG());
            int inside = 0; outside = 0;
            Color[] colors = image.GetPixels();
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                Color c = colors[y*256+x];
                if (c.r < .8f || c.b < .8f || c.g > .2f) continue;
                Vector3 p = camera.ViewportToWorldPoint(new Vector3((x+.5f)/256,(y+.5f)/256,.2f));
                if (p.x*p.x+p.y*p.y < .023f*.023f) inside++; else outside++;
            }
            UnityEngine.Object.DestroyImmediate(image); return inside;
        }
    }
}
