#ifndef PRESCRIPTION_LENS_SURFACE
#define PRESCRIPTION_LENS_SURFACE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "LensOptics.cginc"
float _RefractiveIndex, _Transmission, _SurfaceReflection, _ARStrength, _EdgeReflection, _Smoothness;
float4 _ARColor, _LensTint;
struct LensVertex
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float4 optics : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct LensVaryings
{
    float4 position : SV_POSITION;
    float4 grab : TEXCOORD0;
    float3 worldPosition : TEXCOORD1;
    float3 normal : TEXCOORD2;
    float3 lensPosition : TEXCOORD3;
    float3 opticalCenter : TEXCOORD4;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
LensVaryings LensVert(LensVertex v)
{
    LensVaryings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(LensVaryings, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.position = UnityObjectToClipPos(v.vertex);
    o.grab = ComputeGrabScreenPos(o.position);
    o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.lensPosition = v.vertex.xyz;
    o.opticalCenter = v.optics.w > .5 ? v.optics.xyz : _OpticalCenter.xyz;
    o.color = v.color;
    return o;
}
void LensSurface(LensVaryings i, out float3 reflection, out float reflectivity, out float3 transmission)
{
    float3 n = normalize(i.normal), v = normalize(_WorldSpaceCameraPos.xyz - i.worldPosition);
    float ndv = saturate(abs(dot(n, v)));
    n *= dot(n, v) < 0 ? -1 : 1;
    float f0 = pow((_RefractiveIndex - 1) / (_RefractiveIndex + 1), 2);
    float fresnel = f0 + (1 - f0) * pow(1 - ndv, 5);
    float edge = step(.75, i.color.r);
    reflectivity = saturate(fresnel * _SurfaceReflection * lerp(1 - .85 * _ARStrength, 1, pow(1 - ndv, 3)) + edge * _EdgeReflection * (1 - ndv));
    float3 direction = reflect(-v, n);
    float4 env = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, direction, (1 - _Smoothness) * 6);
    reflection = DecodeHDR(env, unity_SpecCube0_HDR);
    float3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPosition));
    float3 halfDirection = normalize(lightDirection + v);
    reflection += _LightColor0.rgb * pow(saturate(dot(n, halfDirection)), lerp(16, 512, _Smoothness)) * saturate(dot(n, lightDirection));
    reflection *= lerp(float3(1, 1, 1), _ARColor.rgb, _ARStrength * (.25 + .75 * (1 - ndv)));
    float path = max(.1, i.color.g) * lerp(1, 1 / max(.15, ndv), edge);
    transmission = _Transmission * pow(max(_LensTint.rgb, .001), path);
}
#endif
