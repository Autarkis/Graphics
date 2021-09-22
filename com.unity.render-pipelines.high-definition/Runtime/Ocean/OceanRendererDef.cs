using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesOcean
    {
        // Resolution at which the signal is evaluated
        public uint _BandResolution;
        // Speed of the wind in km/h (or is it m/s?)
        public float _WindSpeed;
        // Factor that attenuates the direction of the ocean
        public float _DirectionDampener;
        // Dispersion time
        public float _DispersionTime;

        public float _PatchSizeScaleRatio;
        public int _GridResolution;
        public float _GridSize;
        public float _MaxWaveHeight;

        public float _WaveTipsScatteringOffset;
        public float _SSSMaskCoefficient;
        public float _Padding1;
        public float _Padding2;

        // Patch size for a given band
        public Vector4 _BandPatchSize;
        public Vector4 _BandPatchUVScale;
        public Vector4 _WaveAmplitude;
        public Vector4 _Choppiness;

        // Jacobian parameters
        public Vector4 _JacobianLambda;

        // Foam parameters
        public Vector4 _FoamFadeIn;
        public Vector4 _FoamFadeOut;
        public Vector4 _FoamJacobianOffset;
        public Vector4 _FoamFromHeightWeights;
        public Vector4 _FoamFromHeightFalloff;

        // Two dimensional vector that describes the wind direction
        public Vector2 _WindDirection;
        // Two dimensional vector that describes the wind current
        public Vector2 _WindCurrent;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterRendering
    {
        public Vector2 _CameraOffset;
        public Vector2 _PatchOffset;
        public uint _GridRenderingResolution;
    }
}
