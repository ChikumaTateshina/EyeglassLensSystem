#ifndef PRESCRIPTION_LENS_OPTICS
#define PRESCRIPTION_LENS_OPTICS
float4 _OpticalCenter, _LensRight, _LensUp, _LensForward, _SampleScale, _AxisSinCos;
float _SpherePower, _CylinderPower, _CylinderAxis, _UseBakedOptics, _ApparentDepth;
float2 LensSampleScales()
{
    return _UseBakedOptics > .5 ? _SampleScale.xy : max(.01, 1 - float2(_SpherePower, _SpherePower + _CylinderPower) * _ApparentDepth * .001);
}
float2 LensAxis()
{
    float a = radians(_CylinderAxis);
    return _UseBakedOptics > .5 ? _AxisSinCos.xy : float2(sin(a), cos(a));
}
#endif
