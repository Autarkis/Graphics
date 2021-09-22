// These values are chosen so that an iFFT patch of 1000km^2 will 
// yield a Phillips spectrum distribution in the [-1, 1] range
#define PHILLIPS_GRAVITY_CONSTANT     9.81f
#define PHILLIPS_PATCH_SCALAR         1000.0f
#define PHILLIPS_AMPLITUDE_SCALAR     15.0f
#define OCEAN_AMPLITUDE_NORMALIZATION  10.0f // The maximum waveheight a hurricane of wind 100km^2 can produce

// Ocean simulation data
Texture2DArray<float4> _DisplacementBuffer;
Texture2DArray<float4> _NormalBuffer;

// This array converts an index to the local coordinate shift of the half resolution texture
static const float2 vertexPostion[4] = {float2(0, 0), float2(0, 1), float2(1, 1), float2(1, 0)};
static const uint triangleIndices[6] = {0, 1, 2, 0, 2, 3};

//http://www.dspguide.com/ch2/6.htm
float GaussianDistribution(float u, float v)
{
    return sqrt(-2.0 * log(max(u, 1e-6f))) * cos(PI * v);
}

float Phillips(float2 k, float2 w, float V, float dirDampener)
{
    float kk = k.x * k.x + k.y * k.y;
    float result = 0.0;
    if (kk != 0.0)
    {
	    float L = (V * V) / PHILLIPS_GRAVITY_CONSTANT;
	    // To avoid _any_ directional bias when there is no wind we lerp towards 0.5f
	    float wk = lerp(dot(normalize(k), w), 0.5, dirDampener);
	    float phillips = (exp(-1.0f / (kk * L * L)) / (kk * kk)) * (wk * wk);
	    result = sqrt(phillips * (wk < 0.0f ? dirDampener : 1.0));
    }
    return result;
}

float2 ComplexExp(float arg)
{
    return float2(cos(arg), sin(arg));
}

float2 ComplexMult(float2 a, float2 b)
{
    return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

struct OceanSimulationCoordinates
{
    float2 uvBand0;
    float2 uvBand1;
    float2 uvBand2;
    float2 uvBand3;
};

void ComputeOceanUVs(float3 positionWS, out OceanSimulationCoordinates oceanCoord)
{
    float2 uv = positionWS.xz + _WindCurrent;
    uv /= _BandPatchSize.x;

    float R0 = _BandPatchUVScale.x;
    float O0 = 0 / R0;
    oceanCoord.uvBand0 = ((uv + O0) * R0);

    float R1 = _BandPatchUVScale.y;
    float O1 = 0.5f / R1;
    oceanCoord.uvBand1 = ((uv + O1) * R1);

    float R2 = _BandPatchUVScale.z;
    float O2 = 0.25 / R2;
    oceanCoord.uvBand2 = ((uv + O2) * R2);

    float R3 = _BandPatchUVScale.w;
    float O3 = 0.125 / R3;
    oceanCoord.uvBand3 = ((uv + O3) * R3);
}

#if !defined(OCEAN_SIMULATION)
float3 GetVertexPositionFromVertexID(uint vertexID, uint gridResolution, float2 patchOffset, float2 cameraOffset)
{
    // Compute the data about the quad of this vertex
    uint quadID = vertexID / 6;
    uint quadX = quadID / gridResolution;
    uint quadZ = quadID & (gridResolution - 1);

    // Evaluate the local position in the quad of this pixel
    int localVertexID = vertexID % 6;
    float2 localPos = vertexPostion[triangleIndices[localVertexID]];

    // Compute the position in the patch
    float3 worldpos = float3(localPos.x + quadX - gridResolution / 2, 0.0, localPos.y + quadZ - gridResolution / 2) / gridResolution * _GridSize;
    
    // Offset the tile and place it under the camera's relative position
    worldpos += float3(patchOffset.x * _GridSize + cameraOffset.x, 0, patchOffset.y * _GridSize + cameraOffset.y);

    // Return the final world space position
    return worldpos;
}

struct OceanDisplacementData
{
    float3 displacement;
    float lowFrequencyHeight;
    float foamFromHeight;
    float sssMask;
};

float EvaluateSSSMask(float3 positionWS, float3 cameraPositionWS)
{
    float3 viewWS = normalize(cameraPositionWS - positionWS);
    float distanceToCamera = distance(cameraPositionWS, positionWS);
    float angleWithOceanPlane = pow(saturate(viewWS.y), .2);
    return (1.f - exp(-distanceToCamera * _SSSMaskCoefficient)) * angleWithOceanPlane;
}

void EvaluateOceanDisplacement(float3 positionAWS, out OceanDisplacementData displacementData)
{
    // Compute the simulation coordinates
    OceanSimulationCoordinates oceanCoord;
    ComputeOceanUVs(positionAWS, oceanCoord);

    // Compute the displacement normalization factor
    float4 patchSizes = _BandPatchSize / PHILLIPS_PATCH_SCALAR;
    float4 patchSizes2 = patchSizes * patchSizes;
    float4 displacementNormalization = _PatchSizeScaleRatio * _WaveAmplitude / patchSizes;
    displacementNormalization *= OCEAN_AMPLITUDE_NORMALIZATION;

    // Accumulate the displacement from the various layers
    float3 totalDisplacement = 0.0;
    float lowFrequencyHeight = 0.0;
    float normalizedDisplacement = 0.0;
    float foamFromHeight = 0.0;

    // First band
    float3 displacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_DisplacementBuffer, s_linear_repeat_sampler, oceanCoord.uvBand0, 0, 0).xyz * displacementNormalization.x;
    displacement.yz *= (_Choppiness.x / max(_WaveAmplitude.x, 0.00001f));
    totalDisplacement += displacement;
    lowFrequencyHeight += displacement.x;
    normalizedDisplacement = displacement.x / patchSizes2.x;
    foamFromHeight += pow(max(0, (1.f + normalizedDisplacement) * 0.5f * _FoamFromHeightWeights.x), _FoamFromHeightFalloff.x);

    // Second band
    displacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_DisplacementBuffer, s_linear_repeat_sampler, oceanCoord.uvBand1, 1, 0).xyz * displacementNormalization.y;
    displacement.yz *= (_Choppiness.y / max(_WaveAmplitude.y, 0.00001f));
    totalDisplacement += displacement;
    lowFrequencyHeight += displacement.x * 0.75;
    normalizedDisplacement = displacement.x / patchSizes2.y;
    foamFromHeight += pow(max(0, (1.f + normalizedDisplacement) * 0.5f * _FoamFromHeightWeights.y), _FoamFromHeightFalloff.y);

    // Third band
    displacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_DisplacementBuffer, s_linear_repeat_sampler, oceanCoord.uvBand2, 2, 0).xyz * displacementNormalization.z;
    displacement.yz *= (_Choppiness.z / max(_WaveAmplitude.z, 0.00001f));
    totalDisplacement += displacement;
    lowFrequencyHeight += displacement.x * 0.5;
    normalizedDisplacement = displacement.x / patchSizes2.z;
    foamFromHeight += pow(max(0, (1.f + normalizedDisplacement) * 0.5f * _FoamFromHeightWeights.z), _FoamFromHeightFalloff.z);

    // Fourth band
    displacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_DisplacementBuffer, s_linear_repeat_sampler, oceanCoord.uvBand3, 3, 0).xyz * displacementNormalization.w;
    displacement.yz *= (_Choppiness.w / max(_WaveAmplitude.w, 0.00001f));
    totalDisplacement += displacement;
    lowFrequencyHeight += displacement.x * 0.25;
    normalizedDisplacement = displacement.x / patchSizes2.w;
    foamFromHeight += pow(max(0, (1.f + normalizedDisplacement) * 0.5f * _FoamFromHeightWeights.w), _FoamFromHeightFalloff.w);

    // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
    displacementData.displacement = float3(-totalDisplacement.y, totalDisplacement.x - positionAWS.y, totalDisplacement.z);
    displacementData.lowFrequencyHeight = (_MaxWaveHeight - lowFrequencyHeight) / _MaxWaveHeight - 0.5f + _WaveTipsScatteringOffset;
    displacementData.foamFromHeight = foamFromHeight;
    displacementData.sssMask = EvaluateSSSMask(positionAWS, _WorldSpaceCameraPos);
}

struct OceanAdditionalData
{
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float3 phaseNormalWS;
    float foam;
};

void EvaluateOceanAdditionalData(float3 positionAWS, float3 inputNormalWS, out OceanAdditionalData oceanAdditionalData)
{
    // Compute the simulation coordinates
    OceanSimulationCoordinates oceanCoord;
    ComputeOceanUVs(positionAWS, oceanCoord);

    // First band
    float4 additionalData = SAMPLE_TEXTURE2D_ARRAY(_NormalBuffer, s_linear_repeat_sampler, oceanCoord.uvBand0, 0);
    float3 surfaceGradients = float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.x;
    float3 lowFrequencySurfaceGradients = surfaceGradients;
    float3 phaseDetailSurfaceGradients = surfaceGradients;
    float foam = additionalData.z;

    // Second band
    additionalData = SAMPLE_TEXTURE2D_ARRAY(_NormalBuffer, s_linear_repeat_sampler, oceanCoord.uvBand1, 1);
    surfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.y;
    lowFrequencySurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.y * 0.75;
    phaseDetailSurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.y * 0.75;
    foam += additionalData.z;

    // Second band
    additionalData = SAMPLE_TEXTURE2D_ARRAY(_NormalBuffer, s_linear_repeat_sampler, oceanCoord.uvBand2, 2);
    surfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.z;
    lowFrequencySurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.z * 0.5;
    phaseDetailSurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.z * 0.5;
    foam += additionalData.z;

    // Second band
    additionalData = SAMPLE_TEXTURE2D_ARRAY(_NormalBuffer, s_linear_repeat_sampler, oceanCoord.uvBand3, 3);
    surfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.w;
    lowFrequencySurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.w * 0.25;
    phaseDetailSurfaceGradients += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.w * 0.25;
    foam += additionalData.z;

    // Blend the various surface gradients
    oceanAdditionalData.normalWS = SurfaceGradientResolveNormal(inputNormalWS, surfaceGradients);
    oceanAdditionalData.lowFrequencyNormalWS = SurfaceGradientResolveNormal(inputNormalWS, lowFrequencySurfaceGradients);
    oceanAdditionalData.phaseNormalWS = SurfaceGradientResolveNormal(inputNormalWS, phaseDetailSurfaceGradients);
    oceanAdditionalData.foam = foam;
}

float3 ComputeDebugNormal(float3 worldPos)
{
    float3 worldPosDdx = normalize(ddx(worldPos));
    float3 worldPosDdy = normalize(ddy(worldPos));
    return normalize(-cross(worldPosDdx, worldPosDdy));
}
#endif
