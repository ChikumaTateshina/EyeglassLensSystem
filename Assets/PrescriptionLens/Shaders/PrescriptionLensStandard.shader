Shader "PrescriptionLens/Standard"
{
    Properties {
        _RefractiveIndex ("Refractive Index", Range(1.01,2.2)) = 1.6
        _SpherePower ("SPH (D)", Float) = -3
        _CylinderPower ("CYL (D)", Float) = 0
        _CylinderAxis ("AXIS (degrees)", Range(0,180)) = 180
        [Toggle] _UseBakedOptics ("Use Generator Baked Optics", Float) = 0
        _ApparentDepth ("Apparent Background Depth (mm)", Float) = 12
        _OpticalCenter ("Optical Center (object meters)", Vector) = (0,0,0,0)
        [HideInInspector] _LensRight ("Lens Right", Vector) = (1,0,0,0)
        [HideInInspector] _LensUp ("Lens Up", Vector) = (0,1,0,0)
        [HideInInspector] _LensForward ("Lens Forward", Vector) = (0,0,1,0)
        [HideInInspector] _AxisSinCos ("Axis sin cos", Vector) = (0,1,0,0)
        [HideInInspector] _SampleScale ("Sampling Scale", Vector) = (1,1,0,0)
        _Transmission ("Transmission", Range(0,1)) = 0.98
        _SurfaceReflection ("Surface Reflection", Range(0,2)) = 1
        _ARStrength ("AR Strength", Range(0,1)) = 0.6
        _ARColor ("AR Reflection Color", Color) = (0.45,0.8,1,1)
        _DispersionStrength ("Dispersion", Range(0,0.1)) = 0.002
        _RefractionStrength ("Normal Refraction (screen UV)", Range(0,0.05)) = 0.002
        _DistortionStrength ("Prescription Distortion", Range(0,2)) = 1
        _EdgeReflection ("Edge Reflection", Range(0,1)) = 0.15
        _LensTint ("Tint per mm", Color) = (0.99,0.995,1,1)
        _Smoothness ("Reflection Smoothness", Range(0,1)) = 0.95
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "LensSurface"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex LensVert
            #pragma fragment LensFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "Includes/LensSurface.cginc"

            float4 LensFrag(LensVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 reflection, transmission;
                float reflectivity;
                LensSurface(i, reflection, reflectivity, transmission);
                float alpha = saturate(1 - dot(transmission, float3(.2126,.7152,.0722)) * (1 - reflectivity));
                float3 tintContribution = _LensTint.rgb * (1 - _Transmission);
                return float4(reflection * reflectivity + tintContribution * (1 - reflectivity), alpha);
            }
            ENDCG
        }
    }
    CustomEditor "PrescriptionLens.Editor.PrescriptionLensShaderGUI"
    FallBack Off
}
