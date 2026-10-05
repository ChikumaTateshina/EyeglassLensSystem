using UnityEngine;

namespace PrescriptionLens
{
    // Asset metadata only: no custom MonoBehaviour is required on a VRChat avatar.
    public sealed class PrescriptionLensMetadata : ScriptableObject
    {
        public Mesh mesh;
        public Material material;
        public float spherePower, cylinderPower, cylinderAxis, refractiveIndex;
        public float requestedCenterThicknessMm, actualCenterThicknessMm, minimumThicknessMm;
        public int vertexCount;
    }
}
