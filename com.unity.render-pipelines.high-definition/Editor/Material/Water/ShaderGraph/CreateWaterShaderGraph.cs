using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateWaterShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Water Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        public static void CreateWaterGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterSubTarget));

            var blockDescriptors = new[]
            {
                // Vertex shader
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.UV0,

                // Fragment shader
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalWS,
                WaterSubTarget.WaterBlocks.LowFrequencyNormalWS,
                WaterSubTarget.WaterBlocks.PhaseNormalWS,
                BlockFields.SurfaceDescription.Smoothness,
                WaterSubTarget.WaterBlocks.FoamColor,
                WaterSubTarget.WaterBlocks.SpecularSelfOcclusion,
                WaterSubTarget.WaterBlocks.Anisotropy,
                WaterSubTarget.WaterBlocks.AnisotropyIOR,
                WaterSubTarget.WaterBlocks.AnisotropyWeight,
                WaterSubTarget.WaterBlocks.WrapDiffuseLighting,
                WaterSubTarget.WaterBlocks.ScatteringLambertLighting,
                WaterSubTarget.WaterBlocks.CustomRefractionColor,
                BlockFields.SurfaceDescription.Alpha,

            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
