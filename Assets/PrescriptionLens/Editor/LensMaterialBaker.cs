using System;
using UnityEngine;

namespace PrescriptionLens.Editor
{
    public static class LensMaterialBaker
    {
        public static Material Bake(PrescriptionLensSettings s)
        {
            Shader shader = Shader.Find(s.refractive ? "PrescriptionLens/Lens" : "PrescriptionLens/Standard");
            if (!shader) throw new ArgumentException("Prescription Lens shader is missing.");
            var material = new Material(shader) { name = s.name + "_Lens" };
            double radians = LensOpticsCalculator.NormalizeAxis(s.cylinderAxis) * Math.PI / 180;
            LensMeshGenerator.ResolveBasis(s, out Vector3 x, out Vector3 y, out Vector3 z);
            material.SetFloat("_RefractiveIndex", s.refractiveIndex);
            material.SetFloat("_SpherePower", s.spherePower);
            material.SetFloat("_CylinderPower", s.cylinderPower);
            material.SetFloat("_CylinderAxis", s.cylinderAxis);
            material.SetFloat("_UseBakedOptics", 1);
            material.SetFloat("_ApparentDepth", s.apparentDepthMm);
            material.SetVector("_AxisSinCos", new Vector4((float)Math.Sin(radians), (float)Math.Cos(radians), 0, 0));
            material.SetVector("_SampleScale", new Vector4(
                (float)LensOpticsCalculator.SampleScale(s.spherePower, s.apparentDepthMm * .001),
                (float)LensOpticsCalculator.SampleScale(s.spherePower + s.cylinderPower, s.apparentDepthMm * .001), 0, 0));
            material.SetVector("_LensRight", x); material.SetVector("_LensUp", y); material.SetVector("_LensForward", z);
            material.SetVector("_OpticalCenter", x * s.opticalCenterMm.x * .001f + y * s.opticalCenterMm.y * .001f);
            return material;
        }
    }
}
