using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class WaterSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<WaterData>
    {
        public WaterSubTarget() => displayName = "Water";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("7dd29427652f2a348be0e480ab69597c");  // WaterSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Water/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Water;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Water/Water.hlsl";
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Water SubShader", "");
        protected override bool requireSplitLighting => false;
        protected override bool supportRaytracing => false;

        WaterData m_WaterData;

        WaterData IRequiresData<WaterData>.data
        {
            get => m_WaterData;
            set => m_WaterData = value;
        }

        public WaterData waterData
        {
            get => m_WaterData;
            set => m_WaterData = value;
        }

        public static FieldDescriptor Water = new FieldDescriptor(kMaterial, "Water", "_MATERIAL_FEATURE_WATER 1");
        public static FieldDescriptor WaterCinematic = new FieldDescriptor(kMaterial, "WaterCinematic", "_MATERIAL_FEATURE_WATER_CINEMATIC 1");

        [GenerateBlocks]
        public struct WaterBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor LowFrequencyNormalWS = new BlockFieldDescriptor(kMaterial, "LowFrequencyNormalWS", "Low Frequency Normal (World Space)", "SURFACEDESCRIPTION_LOWFREQUENCYNORMALWS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor PhaseNormalWS = new BlockFieldDescriptor(kMaterial, "PhaseNormalWS", "Phase Normal (World Space)", "SURFACEDESCRIPTION_PHASENORMALWS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor FoamColor = new BlockFieldDescriptor(kMaterial, "FoamColor", "Foam Color", "SURFACEDESCRIPTION_FOAMCOLOR", new ColorControl(new Color(0.8f, 0.8f, 0.8f), false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularSelfOcclusion = new BlockFieldDescriptor(kMaterial, "SpecularSelfOcclusion", "Self Specular Occlusion", "SURFACEDESCRIPTION_SPECULARSELFOCCLUSION", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(kMaterial, "Anisotropy", "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AnisotropyIOR = new BlockFieldDescriptor(kMaterial, "AnisotropyIOR", "Anisotropy IOR", "SURFACEDESCRIPTION_ANISOTROPYIOR", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AnisotropyWeight = new BlockFieldDescriptor(kMaterial, "AnisotropyWeight", "Anisotropy Weight", "SURFACEDESCRIPTION_ANISOTROPYWEIGHT", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor WrapDiffuseLighting = new BlockFieldDescriptor(kMaterial, "WrapDiffuseLighting", "Wrap Diffuse Lighting", "SURFACEDESCRIPTION_WRAPDIFFUSELIGHTING", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ScatteringLambertLighting = new BlockFieldDescriptor(kMaterial, "ScatteringLambertLighting", "Scattering Lambert Lighting", "SURFACEDESCRIPTION_SCATTERINGLAMBERTLIGHTING", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomRefractionColor = new BlockFieldDescriptor(kMaterial, "CustomRefractionColor", "Custom Refraction Color", "SURFACEDESCRIPTION_CUSTOMREFRACTIONCOLOR", new ColorControl(new Color(0.0f, 0.0f, 0.0f), false), ShaderStage.Fragment);
        }

        public static RenderStateCollection OceanForward = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Back) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ColorMask("ColorMask 0 1") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = CoreRenderStates.Uniforms.stencilWriteMask,
                Ref = CoreRenderStates.Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static PassDescriptor GenerateOceanForwardPass()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD" ,
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, false),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = OceanForward,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstancedInV2Only, false, false),
                defines = HDShaderPasses.GenerateDefines( CoreDefines.Forward, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassForward, IncludeLocation.Postgraph);

                return includes;
            }
        }

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetOceanPasses()
            };

            PassCollection GetOceanPasses()
            {
                var passes = new PassCollection
                {
                    // Generate the ocean forward pass
                    GenerateOceanForwardPass(),
                };
                return passes;
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Water specific properties
            context.AddField(StructFields.VertexDescriptionInputs.uv0);
            context.AddField(Water, waterData.materialType == WaterData.MaterialType.Water);
            context.AddField(WaterCinematic, waterData.materialType == WaterData.MaterialType.WaterCinematic);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex shader
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.UV0);

            // Fragment shader
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS);
            context.AddBlock(WaterBlocks.LowFrequencyNormalWS);
            context.AddBlock(WaterBlocks.PhaseNormalWS);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(WaterBlocks.FoamColor);
            context.AddBlock(WaterBlocks.SpecularSelfOcclusion);
            context.AddBlock(WaterBlocks.Anisotropy);
            context.AddBlock(WaterBlocks.AnisotropyIOR);
            context.AddBlock(WaterBlocks.AnisotropyWeight);
            context.AddBlock(WaterBlocks.WrapDiffuseLighting);
            context.AddBlock(WaterBlocks.ScatteringLambertLighting);
            context.AddBlock(WaterBlocks.CustomRefractionColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, waterData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }
    }
}
