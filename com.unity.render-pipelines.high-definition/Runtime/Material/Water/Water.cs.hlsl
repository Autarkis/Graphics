//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATER_CS_HLSL
#define WATER_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Water+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_WATER_STANDARD (1)
#define MATERIALFEATUREFLAGS_WATER_CINEMATIC (2)

//
// UnityEngine.Rendering.HighDefinition.Water+SurfaceData:  static fields
//
#define DEBUGVIEW_WATER_SURFACEDATA_MATERIAL_FEATURES (1600)
#define DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR (1601)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_WS (1602)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE (1603)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_WS (1604)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1605)
#define DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_WS (1606)
#define DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1607)
#define DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS (1608)
#define DEBUGVIEW_WATER_SURFACEDATA_FOAM_COLOR (1609)
#define DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_SELF_OCCLUSION (1610)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY (1611)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_IOR (1612)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_WEIGHT (1613)
#define DEBUGVIEW_WATER_SURFACEDATA_WRAP_DIFFUSE_LIGHTING (1614)
#define DEBUGVIEW_WATER_SURFACEDATA_SCATTERING_LAMBERT_LIGHTING (1615)
#define DEBUGVIEW_WATER_SURFACEDATA_CUSTOM_REFRACTION_COLOR (1616)

//
// UnityEngine.Rendering.HighDefinition.Water+BSDFData:  static fields
//
#define DEBUGVIEW_WATER_BSDFDATA_MATERIAL_FEATURES (1650)
#define DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR (1651)
#define DEBUGVIEW_WATER_BSDFDATA_FRESNEL0 (1652)
#define DEBUGVIEW_WATER_BSDFDATA_SPECULAR_SELF_OCCLUSION (1653)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS (1654)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE (1655)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS (1656)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1657)
#define DEBUGVIEW_WATER_BSDFDATA_GEOMETRIC_NORMAL_WS (1658)
#define DEBUGVIEW_WATER_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1659)
#define DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS (1660)
#define DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS (1661)
#define DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR (1662)
#define DEBUGVIEW_WATER_BSDFDATA_CUSTOM_REFRACTION_COLOR (1663)
#define DEBUGVIEW_WATER_BSDFDATA_WRAP_DIFFUSE_LIGHTING (1664)
#define DEBUGVIEW_WATER_BSDFDATA_SCATTERING_LAMBERT_LIGHTING (1665)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY (1666)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_IOR (1667)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_WEIGHT (1668)

// Generated from UnityEngine.Rendering.HighDefinition.Water+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float3 phaseNormalWS;
    float perceptualSmoothness;
    float3 foamColor;
    float specularSelfOcclusion;
    float anisotropy;
    float anisotropyIOR;
    float anisotropyWeight;
    float wrapDiffuseLighting;
    float scatteringLambertLighting;
    float3 customRefractionColor;
};

// Generated from UnityEngine.Rendering.HighDefinition.Water+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float specularSelfOcclusion;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float3 phaseNormalWS;
    float perceptualRoughness;
    float roughness;
    float3 foamColor;
    float3 customRefractionColor;
    float wrapDiffuseLighting;
    float scatteringLambertLighting;
    float anisotropy;
    float anisotropyIOR;
    float anisotropyWeight;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_WATER_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_WS:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_WS:
            result = IsNormalized(surfacedata.lowFrequencyNormalWS)? surfacedata.lowFrequencyNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.lowFrequencyNormalWS)? surfacedata.lowFrequencyNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_WS:
            result = IsNormalized(surfacedata.phaseNormalWS)? surfacedata.phaseNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.phaseNormalWS)? surfacedata.phaseNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_FOAM_COLOR:
            result = surfacedata.foamColor;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_SELF_OCCLUSION:
            result = surfacedata.specularSelfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_IOR:
            result = surfacedata.anisotropyIOR.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_WEIGHT:
            result = surfacedata.anisotropyWeight.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_WRAP_DIFFUSE_LIGHTING:
            result = surfacedata.wrapDiffuseLighting.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SCATTERING_LAMBERT_LIGHTING:
            result = surfacedata.scatteringLambertLighting.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_CUSTOM_REFRACTION_COLOR:
            result = surfacedata.customRefractionColor;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_WATER_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SPECULAR_SELF_OCCLUSION:
            result = bsdfdata.specularSelfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS:
            result = bsdfdata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE:
            result = bsdfdata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_GEOMETRIC_NORMAL_WS:
            result = bsdfdata.phaseNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = bsdfdata.phaseNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR:
            result = bsdfdata.foamColor;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_CUSTOM_REFRACTION_COLOR:
            result = bsdfdata.customRefractionColor;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_WRAP_DIFFUSE_LIGHTING:
            result = bsdfdata.wrapDiffuseLighting.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SCATTERING_LAMBERT_LIGHTING:
            result = bsdfdata.scatteringLambertLighting.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_IOR:
            result = bsdfdata.anisotropyIOR.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_WEIGHT:
            result = bsdfdata.anisotropyWeight.xxx;
            break;
    }
}


#endif
