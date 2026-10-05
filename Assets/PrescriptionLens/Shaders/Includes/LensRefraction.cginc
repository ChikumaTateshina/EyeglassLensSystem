#ifndef PRESCRIPTION_LENS_REFRACTION
#define PRESCRIPTION_LENS_REFRACTION
UNITY_DECLARE_SCREENSPACE_TEXTURE(_PrescriptionLensGrab);
float4 _PrescriptionLensGrab_TexelSize;
float _RefractionStrength, _DistortionStrength, _DispersionStrength;
float4 LensProject(float3 localPosition)
{
    return ComputeGrabScreenPos(UnityObjectToClipPos(float4(localPosition, 1)));
}
float2 LensUV(float4 p) { return p.xy / max(p.w, 1e-5); }
float2 LensClampUV(float2 uv)
{
    float2 lo = 0, hi = 1;
    #if defined(UNITY_SINGLE_PASS_STEREO)
        float4 so = unity_StereoScaleOffset[unity_StereoEyeIndex];
        lo = so.zw; hi = lo + so.xy;
    #endif
    float2 margin = abs(_PrescriptionLensGrab_TexelSize.xy) * 1.5;
    return clamp(uv, lo + margin, hi - margin);
}
float3 LensBackground(LensVaryings i)
{
    float2 screen = LensUV(i.grab);
    float3 delta = i.lensPosition - i.opticalCenter;
    float2 xy = float2(dot(delta, _LensRight.xyz), dot(delta, _LensUp.xyz));
    float2 sc = LensAxis();
    float2 rotated = float2(sc.y * xy.x + sc.x * xy.y, -sc.x * xy.x + sc.y * xy.y);
    rotated *= LensSampleScales();
    float2 warped = float2(sc.y * rotated.x - sc.x * rotated.y, sc.x * rotated.x + sc.y * rotated.y);
    float3 localOffset = _LensRight.xyz * (warped.x - xy.x) + _LensUp.xyz * (warped.y - xy.y);
    float2 opticalOffset = LensUV(LensProject(i.lensPosition + localOffset)) - screen;
    float3 n = normalize(i.normal);
    float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPosition);
    n *= dot(n, v) < 0 ? -1 : 1;
    float3 forwardWorld = normalize(UnityObjectToWorldDir(_LensForward.xyz));
    forwardWorld *= dot(forwardWorld, v) < 0 ? -1 : 1;
    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, n - forwardWorld);
    float2 bend = viewNormal.xy * _RefractionStrength * (1 - 1 / _RefractiveIndex);
    #if UNITY_UV_STARTS_AT_TOP
        bend.y = -bend.y;
    #endif
    #if defined(UNITY_SINGLE_PASS_STEREO)
        bend *= unity_StereoScaleOffset[unity_StereoEyeIndex].xy;
    #endif
    float2 offset = opticalOffset * _DistortionStrength + bend;
    float r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_PrescriptionLensGrab, LensClampUV(screen + offset * (1 + _DispersionStrength))).r;
    float g = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_PrescriptionLensGrab, LensClampUV(screen + offset)).g;
    float b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_PrescriptionLensGrab, LensClampUV(screen + offset * (1 - _DispersionStrength))).b;
    return float3(r, g, b);
}
#endif
