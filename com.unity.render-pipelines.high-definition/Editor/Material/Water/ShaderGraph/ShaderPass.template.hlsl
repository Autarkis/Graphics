#pragma enable_d3d11_debug_symbols

void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    $SurfaceDescription.BaseColor:                  surfaceData.baseColor =                 surfaceDescription.BaseColor;
    
    $SurfaceDescription.NormalWS:                   surfaceData.normalWS =                  surfaceDescription.NormalWS;
    $SurfaceDescription.LowFrequencyNormalWS:       surfaceData.lowFrequencyNormalWS =      surfaceDescription.LowFrequencyNormalWS;
    $SurfaceDescription.PhaseNormalWS:              surfaceData.phaseNormalWS =             surfaceDescription.PhaseNormalWS;
    
    $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
    $SurfaceDescription.FoamColor:                  surfaceData.foamColor =                 surfaceDescription.FoamColor;
    $SurfaceDescription.SpecularSelfOcclusion:      surfaceData.specularSelfOcclusion =     surfaceDescription.SpecularSelfOcclusion;

    $SurfaceDescription.Anisotropy:                 surfaceData.anisotropy =                surfaceDescription.Anisotropy;
    $SurfaceDescription.AnisotropyIOR:              surfaceData.anisotropyIOR =             surfaceDescription.AnisotropyIOR;
    $SurfaceDescription.AnisotropyWeight:           surfaceData.anisotropyWeight =          surfaceDescription.AnisotropyWeight;
    $SurfaceDescription.WrapDiffuseLighting:        surfaceData.wrapDiffuseLighting =       surfaceDescription.WrapDiffuseLighting;
    $SurfaceDescription.ScatteringLambertLighting:  surfaceData.scatteringLambertLighting = surfaceDescription.ScatteringLambertLighting;
    $SurfaceDescription.CustomRefractionColor:      surfaceData.customRefractionColor =     surfaceDescription.CustomRefractionColor;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_WATER_STANDARD;
    #ifdef _MATERIAL_FEATURE_WATER_CINEMATIC
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_WATER_CINEMATIC;
    #endif

    bentNormalWS = float3(0, 1, 0);

    #ifdef DEBUG_DISPLAY
        if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        {
            // TODO: need to update mip info
            surfaceData.metallic = 0;
        }

        // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
        // as it can modify attribute use for static lighting
        ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
    #endif
}