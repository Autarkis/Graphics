using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Enum that defines the sets of resolution at which the ocean simulation can be evaluated
    public enum OceanSimulationResolution
    {
        VeryLow32 = 32,
        Low64 = 64,
        Medium128 = 128,
        High256 = 256,
        Ultra512 = 512
    }

    public partial class HDRenderPipeline
    {
        // Various internal constants
        const int k_OceanBandCount = 4;
        const int k_OceanMinGridSize = 2;
        const float k_OceanMinPatchSize = 50.0f;
        const float k_OceanMaxPatchSize = 500.0f;
        const float k_PhillipsPatchScalar = 1000.0f;
        const float k_PhillipsAmplitudeScalar = 15.0f;
        const float k_OceanAmplitudeNormalization = 10.0f;
        const float k_OceanChopinessNormalization = 3.0f;
        const float k_PhillipsGravityConstant = 9.8f;
        const float k_PhillipsWindScalar = 1.0f / k_PhillipsGravityConstant; // Is this a coincidence? Found '0.10146f' by curve fitting
        const float k_PhillipsWindFalloffCoefficient = 0.00034060072f; // PI/(9.8^4);

        // Simulation shader and kernels
        ComputeShader m_OceanSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Intermediate RTHandles used to render the ocean
        RTHandle m_H0s;
        RTHandle m_HtRs;
        RTHandle m_HtIs;
        RTHandle m_FFTRowPassRs;
        RTHandle m_FFTRowPassIs;
        RTHandle m_FFTColPassIs;
        RTHandle m_OceanDisplacementBuffer;
        RTHandle m_OceanNormalBuffer;

        // Other rendering data
        Material m_InternalOceanMaterial;
        MaterialPropertyBlock m_OceanMaterialPropertyBlock;
        float m_DispersionTime;
        OceanSimulationResolution m_OceanBandResolution = OceanSimulationResolution.Medium128;

        void GetFFTKernels(OceanSimulationResolution resolution, out int rowKernel, out int columnKernel)
        {
            switch (resolution)
            {
                case OceanSimulationResolution.Ultra512:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_512");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_512");
                }
                break;
                case OceanSimulationResolution.High256:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_256");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_256");
                }
                break;
                case OceanSimulationResolution.Medium128:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_128");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_128");
                }
                break;
                case OceanSimulationResolution.Low64:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
                case OceanSimulationResolution.VeryLow32:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_32");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_32");
                }
                break;
                default:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
            }
        }

        void InitializeOceanRenderer()
        {
            // If the asset doesn't support oceans, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportOcean)
                return;

            m_OceanBandResolution = m_Asset.currentPlatformRenderPipelineSettings.oceanSimulationResolution;

            // Simulation shader and kernels
            m_OceanSimulationCS = m_Asset.renderPipelineResources.shaders.oceanSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_OceanSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_OceanSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsKernel = m_OceanSimulationCS.FindKernel("EvaluateNormals");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_OceanBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            int textureRes = (int)m_OceanBandResolution;
            // Allocate all the RTHanles required for the ocean rendering
            m_H0s = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTColPassIs = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_OceanDisplacementBuffer = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_OceanNormalBuffer = RTHandles.Alloc(textureRes, textureRes, k_OceanBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);

            // Allocate the additional rendering data
            m_OceanMaterialPropertyBlock = new MaterialPropertyBlock();
            m_InternalOceanMaterial = defaultResources.materials.defaultOceanMaterial;
            m_DispersionTime = 0;
        }

        void ReleaseOceanRenderer()
        {
            // If the asset doesn't support oceans, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportOcean)
                return;

            // Release all the RTHandles
            RTHandles.Release(m_OceanNormalBuffer);
            RTHandles.Release(m_OceanDisplacementBuffer);
            RTHandles.Release(m_FFTColPassIs);
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
            RTHandles.Release(m_H0s);
        }

        void GetOceanDispersionTime(float dispersionTime, ref float oceanTime, ref float waveDispersionTime)
        {
            oceanTime = dispersionTime;
            waveDispersionTime = oceanTime * Mathf.Sqrt((int)m_OceanBandResolution / (float) 32);
        }

        void GetOceanWindDirectionAndCurrent(float oceanTime, ref Vector2 outWindDirection, ref Vector2 outCurrentDirection)
        {
            float windDirection = 0.0f * Mathf.Deg2Rad;
            float windDirectionX = Mathf.Cos(windDirection);
            float windDirectionY = Mathf.Sin(windDirection);
            outWindDirection.Set(windDirectionX, windDirectionY);

            float currentDirection = 0.0f * Mathf.Deg2Rad;
            float currentDirectionX = Mathf.Cos(currentDirection);
            float currentDirectionY = Mathf.Sin(currentDirection);
            float oceanCurrent = oceanTime * 0.0f;
            outCurrentDirection.Set(currentDirectionX * oceanCurrent, currentDirectionY * oceanCurrent);
        }

        static public float MaximumWaveHeightFunction(float windSpeed, float S)
        {
            if (windSpeed < 0) windSpeed = 0;
            return 1.0f - Mathf.Exp(-S * windSpeed * windSpeed);
        }

        static public float MaximumWindForPatch(float patchSize)
        {
            float a = Mathf.Sqrt(-1.0f / Mathf.Log(0.999f * 0.999f));
            float b = (0.001f * Mathf.PI * 2.0f) / patchSize;
            float c = k_PhillipsWindScalar * Mathf.Sqrt((1.0f / k_PhillipsGravityConstant) * (a / b));
            return c;
        }

        public float ComputeMaximumWaveHeight(float oceanWaveAmplitude, Vector4 oceanWaveAmplitudeScalars, float oceanMaxPatchSize, float oceanWindSpeed)
        {
            float maxiumumWaveHeight = 0.01f;
            for (int i = 0; i < 4; ++i)
            {
                float patchAmplitude = oceanWaveAmplitude * oceanWaveAmplitudeScalars[i] * (oceanMaxPatchSize / k_PhillipsPatchScalar);
                float L = MaximumWindForPatch(k_PhillipsPatchScalar) / MaximumWindForPatch(oceanMaxPatchSize);
                float A = k_OceanAmplitudeNormalization * patchAmplitude;
                float normalizedMaximumHeight = MaximumWaveHeightFunction(oceanWindSpeed * L, k_PhillipsWindFalloffCoefficient);
                maxiumumWaveHeight = Mathf.Max(A * normalizedMaximumHeight, maxiumumWaveHeight);
            }
            return maxiumumWaveHeight;
        }

        void UpdateShaderVariablesOcean(Ocean oceanSettings, ref ShaderVariablesOcean cb)
        {
            cb._BandResolution = (uint)m_OceanBandResolution;
            cb._WindSpeed = 30.0f;
            cb._DirectionDampener = 0.5f;

            float oceanTime = 0.0f;
            float dynamicOceanDispersionTime = 0.0f;
            GetOceanDispersionTime((float)m_DispersionTime, ref oceanTime, ref dynamicOceanDispersionTime);
            cb._DispersionTime = dynamicOceanDispersionTime;
            cb._GridResolution = (int)oceanSettings.gridResolution.value;
            cb._GridSize = oceanSettings.gridSize.value;

            float patchSizeScaleFactor = Mathf.Pow(k_OceanMaxPatchSize / k_OceanMinPatchSize, 1.0f / (k_OceanBandCount - 1));
            cb._BandPatchUVScale = new Vector4(1.0f, patchSizeScaleFactor, (patchSizeScaleFactor * patchSizeScaleFactor), (patchSizeScaleFactor * patchSizeScaleFactor * patchSizeScaleFactor));
            cb._BandPatchSize = new Vector4(k_OceanMaxPatchSize, k_OceanMaxPatchSize / cb._BandPatchUVScale.y, k_OceanMaxPatchSize / cb._BandPatchUVScale.z, k_OceanMaxPatchSize / cb._BandPatchUVScale.w);
            cb._PatchSizeScaleRatio = Mathf.Lerp(1.0f, 0.5f, k_OceanMinPatchSize / k_OceanMaxPatchSize);
            cb._WaveAmplitude = oceanSettings.waveAmpltiude.value;
            cb._Choppiness = oceanSettings.choppiness.value;

            GetOceanWindDirectionAndCurrent(oceanTime, ref cb._WindDirection, ref cb._WindCurrent);

            // Foam Data
            Vector4 jacobianNormalizer = new Vector4(cb._BandPatchSize.x * cb._BandPatchSize.x,
                                                    cb._BandPatchSize.y * cb._BandPatchSize.y,
                                                    cb._BandPatchSize.z * cb._BandPatchSize.z,
                                                    cb._BandPatchSize.w * cb._BandPatchSize.w) * 0.00000001f;
            cb._JacobianLambda = new Vector4(1 / jacobianNormalizer.x, 1 / jacobianNormalizer.y, 1 / jacobianNormalizer.z, 1 / jacobianNormalizer.w);
            cb._FoamFadeIn = new Vector4(0.01f, 0.01f, 0.01f, 0.01f);
            cb._FoamFadeOut = new Vector4(0.99f, 0.99f, 0.99f, 0.99f);
            cb._FoamJacobianOffset = new Vector4(1, 1, 1, 1);
        }

        struct OceanRenderingParameters
        {
            // Camera parameters
            public uint width;
            public uint height;

            public int gridResolution;
            public float currentTime;
            public int numLODs;
            public Vector3 cameraPosition;
            public float gridSize;
            public Frustum cameraFrustum;
            public int bandResolution;

            public ComputeShader oceanSimulationCS;
            public int initializePhillipsSpectrumKernel;
            public int evaluateDispersionKernel;
            public int evaluateNormalKernel;

            public ComputeShader fourierTransformCS;
            public int iFFTRowPassKernel;
            public int iFFTColPassKernel;

            public Material oceanMaterial;
            public MaterialPropertyBlock mbp;

            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesOcean oceanCB;
        }

        OceanRenderingParameters PrepareOceanRenderingParameters(HDCamera camera, Ocean settings)
        {
            OceanRenderingParameters parameters = new OceanRenderingParameters();

            parameters.gridResolution = (int)settings.gridResolution.value;
            parameters.currentTime = m_DispersionTime;
            parameters.numLODs = settings.numLevelOfDetais.value;
            parameters.cameraPosition = camera.camera.transform.position;
            parameters.gridSize = settings.gridSize.value;
            parameters.cameraFrustum = camera.frustum;
            parameters.bandResolution = (int)m_OceanBandResolution;

            parameters.oceanSimulationCS = m_OceanSimulationCS;
            parameters.initializePhillipsSpectrumKernel = m_InitializePhillipsSpectrumKernel;
            parameters.evaluateDispersionKernel = m_EvaluateDispersionKernel;
            parameters.evaluateNormalKernel = m_EvaluateNormalsKernel;

            parameters.fourierTransformCS = m_FourierTransformCS;
            parameters.iFFTRowPassKernel = m_RowPassTi_Kernel;
            parameters.iFFTColPassKernel = m_ColPassTi_Kernel;

            parameters.oceanMaterial = settings.material.value != null? settings.material.value : m_InternalOceanMaterial;
            parameters.mbp = m_OceanMaterialPropertyBlock;

            BlueNoise blueNoise = GetBlueNoiseManager();
            parameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            UpdateShaderVariablesOcean(settings, ref parameters.oceanCB);
            return parameters;
        }

        class OceanRenderingData
        {
            // All the parameters required to simulate and render the ocean
            public OceanRenderingParameters parameters;

            // Simulation buffers
            public TextureHandle h0Buffer;
            public TextureHandle htRealBuffer;
            public TextureHandle htImaginaryBuffer;
            public TextureHandle fftRowPassRs;
            public TextureHandle fftRowPassIs;
            public TextureHandle fftColPassIs;
            public TextureHandle displacementBuffer;
            public TextureHandle normalBuffer;

            // Ocean rendered to this buffer
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderOcean(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            // If the ocean is disabled, no need to render or simulate
            Ocean settings = hdCamera.volumeStack.GetComponent<Ocean>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Ocean))
                return;

            using (var builder = renderGraph.AddRenderPass<OceanRenderingData>("Render Ocean", out var passData, ProfilingSampler.Get(HDProfileId.OceanRendering)))
            {
                builder.EnableAsyncCompute(false);

                // Prepare all the internal parameters
                passData.parameters = PrepareOceanRenderingParameters(hdCamera, settings);

                // Import all the textures into the system
                passData.h0Buffer = renderGraph.ImportTexture(m_H0s);
                passData.htRealBuffer = renderGraph.ImportTexture(m_HtRs);
                passData.htImaginaryBuffer = renderGraph.ImportTexture(m_HtIs);
                passData.fftRowPassRs = renderGraph.ImportTexture(m_FFTRowPassRs);
                passData.fftRowPassIs = renderGraph.ImportTexture(m_FFTRowPassIs);
                passData.fftColPassIs = renderGraph.ImportTexture(m_FFTColPassIs);
                passData.displacementBuffer = renderGraph.ImportTexture(m_OceanDisplacementBuffer);
                passData.normalBuffer = renderGraph.ImportTexture(m_OceanNormalBuffer);

                // Request the output textures
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                m_DispersionTime = hdCamera.time * 0.1f;

                builder.SetRenderFunc(
                    (OceanRenderingData data, RenderGraphContext ctx) =>
                    {
                        // Bind the sampling textures
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.parameters.ditheredTextureSet);

                        // Bind the constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.parameters.oceanCB, data.parameters.oceanSimulationCS, HDShaderIDs._ShaderVariablesOcean);

                        // Number of tiles we will need to dispatch
                        int tileCount = data.parameters.bandResolution / 8;

                        // Initialize if needed
                        if (data.parameters.currentTime == 0)
                        {
                            // Convert the spectrum noise to the Phillips spectrum
                            ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.initializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, data.h0Buffer);
                            ctx.cmd.DispatchCompute(data.parameters.oceanSimulationCS, data.parameters.initializePhillipsSpectrumKernel, tileCount, tileCount, k_OceanBandCount);
                        }

                        // Execute the dispersion
                        ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.evaluateDispersionKernel, HDShaderIDs._H0Buffer, data.h0Buffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.evaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, data.htRealBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.evaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, data.htImaginaryBuffer);
                        ctx.cmd.DispatchCompute(data.parameters.oceanSimulationCS, data.parameters.evaluateDispersionKernel, tileCount, tileCount, k_OceanBandCount);

                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTRowPassKernel, HDShaderIDs._FFTRealBuffer, data.htRealBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTRowPassKernel, HDShaderIDs._FFTImaginaryBuffer, data.htImaginaryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTRowPassKernel, HDShaderIDs._FFTRealBufferRW, data.fftRowPassRs);
                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTRowPassKernel, HDShaderIDs._FFTImaginaryBufferRW, data.fftRowPassIs);
                        ctx.cmd.DispatchCompute(data.parameters.fourierTransformCS, data.parameters.iFFTRowPassKernel, 1, data.parameters.bandResolution, k_OceanBandCount);

                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTColPassKernel, HDShaderIDs._FFTRealBuffer, data.fftRowPassRs);
                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTColPassKernel, HDShaderIDs._FFTImaginaryBuffer, data.fftRowPassIs);
                        ctx.cmd.SetComputeTextureParam(data.parameters.fourierTransformCS, data.parameters.iFFTColPassKernel, HDShaderIDs._FFTRealBufferRW, data.displacementBuffer);
                        ctx.cmd.DispatchCompute(data.parameters.fourierTransformCS, data.parameters.iFFTColPassKernel, 1, data.parameters.bandResolution, k_OceanBandCount);

                        ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.evaluateNormalKernel, HDShaderIDs._DisplacementBuffer, data.displacementBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.oceanSimulationCS, data.parameters.evaluateNormalKernel, HDShaderIDs._NormalBufferRW, data.normalBuffer);
                        ctx.cmd.DispatchCompute(data.parameters.oceanSimulationCS, data.parameters.evaluateNormalKernel, tileCount, tileCount, k_OceanBandCount);

                        // Bind the constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.parameters.oceanCB, data.parameters.oceanMaterial, HDShaderIDs._ShaderVariablesOcean);

                        // Prepare the material property block for the rendering
                        data.parameters.mbp.SetTexture(HDShaderIDs._DisplacementBuffer, data.displacementBuffer);
                        data.parameters.mbp.SetTexture(HDShaderIDs._NormalBuffer, data.normalBuffer);

                        // Make sure the mip-maps are generated
                        RTHandle normalBuffer = data.normalBuffer;
                        normalBuffer.rt.GenerateMips();

                        // Bind the render targets and render the ocean
                        CoreUtils.SetRenderTarget(ctx.cmd, data.colorBuffer, data.depthBuffer);

                        // Prepare the oobb for the patches
                        OrientedBBox bbox = new OrientedBBox();
                        bbox.right = Vector3.right;
                        bbox.up = Vector3.forward;
                        bbox.extentX = data.parameters.gridSize;
                        bbox.extentY = data.parameters.gridSize;
                        bbox.extentZ = k_OceanAmplitudeNormalization * 2.0f;

                        // Loop through the patches
                        for (int y = -data.parameters.numLODs; y <= data.parameters.numLODs; ++y)
                        {
                            for (int x = -data.parameters.numLODs; x <= data.parameters.numLODs; ++x)
                            {
                                // Compute the center of the patch
                                bbox.center = new Vector3(x * data.parameters.gridSize, -data.parameters.cameraPosition.y, y * data.parameters.gridSize);

                                // is this patch visible by the camera?
                                if (GeometryUtils.Overlap(bbox, data.parameters.cameraFrustum, 6, 8))
                                {
                                    data.parameters.mbp.SetVector(HDShaderIDs._PatchOffset, new Vector2(x, y));
                                    int pachResolution = Mathf.Max(data.parameters.gridResolution >> (Mathf.Abs(x) + Mathf.Abs(y)), k_OceanMinGridSize);
                                    data.parameters.mbp.SetInt(HDShaderIDs._GridRenderingResolution, pachResolution);
                                    data.parameters.mbp.SetVector(HDShaderIDs._CameraOffset, new Vector2(data.parameters.cameraPosition.x, data.parameters.cameraPosition.z));
                                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.oceanMaterial, 0, MeshTopology.Triangles, 6 * pachResolution * pachResolution, 0, data.parameters.mbp);
                                }
                            }
                        }
                    });
                PushFullScreenDebugTexture(m_RenderGraph, passData.displacementBuffer, FullScreenDebugMode.Ocean, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, xrTexture: false);
            }
        }
    }
}
