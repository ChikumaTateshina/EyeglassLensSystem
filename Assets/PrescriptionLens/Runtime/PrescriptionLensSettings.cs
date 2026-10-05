using UnityEngine;

namespace PrescriptionLens
{
    public enum LensForwardAxis { PositiveZ, NegativeZ, PositiveX, NegativeX, PositiveY, NegativeY }

    [CreateAssetMenu(menuName = "Prescription Lens/Settings")]
    public sealed class PrescriptionLensSettings : ScriptableObject
    {
        public Mesh sourceMesh;
        public float spherePower = -3f, cylinderPower = 0f;
        [Range(0, 180)] public float cylinderAxis = 180f;
        public float refractiveIndex = 1.6f, baseCurve = 4f;
        public float centerThicknessMm = 1.5f, minimumEdgeThicknessMm = 1f;
        public LensForwardAxis forwardAxis;
        public bool autoOrientSource = true;
        public bool autoOpticalCenter = true;
        public Vector2 opticalCenterMm;
        [Range(0, 4)] public int subdivision = 2;
        [Range(1, 8)] public int edgeSegments = 1;
        public bool preserveSourceFrontShape;
        public bool refractive = true;
        public float apparentDepthMm = 12f;
        public string outputFolder = "Assets/PrescriptionLens/Generated";
    }
}
