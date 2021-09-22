using UnityEngine.Rendering.HighDefinition.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class Water : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            WaterStandard = 1 << 0,
            WaterCinematic = 1 << 1
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1600)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 lowFrequencyNormalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal WS", "Geometric Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 phaseNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Foam Color")]
            public Vector3 foamColor;

            [SurfaceDataAttributes("Specular Self Occlusion")]
            public float specularSelfOcclusion;

            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy;

            [SurfaceDataAttributes("Anisotropy IOR")]
            public float anisotropyIOR;

            [SurfaceDataAttributes("Anisotropy Weight")]
            public float anisotropyWeight;

            // SSS
            [SurfaceDataAttributes("Wrap Diffuse Lighting")]
            public float wrapDiffuseLighting;

            [SurfaceDataAttributes("Scattering Lambert Lighting")]
            public float scatteringLambertLighting;

            [SurfaceDataAttributes("Custom Refraction Color")]
            public Vector3 customRefractionColor;
        }

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1650)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;

            public Vector3 fresnel0;

            public float specularSelfOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true)]
            public Vector3 lowFrequencyNormalWS;
            [SurfaceDataAttributes(new string[] { "Geometric Normal WS", "Geometric Normal View Space" }, true)]
            public Vector3 phaseNormalWS;

            public float perceptualRoughness;
            public float roughness;

            public Vector3 foamColor;
            public Vector3 customRefractionColor;

            // SSS
            public float wrapDiffuseLighting;
            public float scatteringLambertLighting;

            // Anisotropy
            public float anisotropy;
            public float anisotropyIOR;
            public float anisotropyWeight;
        }

        public Water() {}
    }
}
