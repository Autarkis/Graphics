//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef OCEANRENDERERDEF_CS_HLSL
#define OCEANRENDERERDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesOcean
// PackingRules = Exact
CBUFFER_START(ShaderVariablesOcean)
    uint _BandResolution;
    float _WindSpeed;
    float _DirectionDampener;
    float _DispersionTime;
    float _PatchSizeScaleRatio;
    int _GridResolution;
    float _GridSize;
    float _MaxWaveHeight;
    float _WaveTipsScatteringOffset;
    float _SSSMaskCoefficient;
    float _Padding1;
    float _Padding2;
    float4 _BandPatchSize;
    float4 _BandPatchUVScale;
    float4 _WaveAmplitude;
    float4 _Choppiness;
    float4 _JacobianLambda;
    float4 _FoamFadeIn;
    float4 _FoamFadeOut;
    float4 _FoamJacobianOffset;
    float4 _FoamFromHeightWeights;
    float4 _FoamFromHeightFalloff;
    float2 _WindDirection;
    float2 _WindCurrent;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float2 _CameraOffset;
    float2 _PatchOffset;
    uint _GridRenderingResolution;
CBUFFER_END


#endif
