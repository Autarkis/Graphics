using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/Ocean", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Ocean")]
    public sealed partial class Ocean : VolumeComponent
    {
        public enum OceanGridResolution
        {
            VeryLow128 = 128,
            Low256 = 256,
            Medium512 = 512,
            High1024 = 1024,
            Ultra2048 = 2048,
        }
        [Serializable]
        public sealed class OceanGridResolutionParameter : VolumeParameter<OceanGridResolution>
        {
            public OceanGridResolutionParameter(OceanGridResolution value, bool overrideState = false) : base(value, overrideState) { }
        }

        public BoolParameter enable = new BoolParameter(false);
        public OceanGridResolutionParameter gridResolution = new OceanGridResolutionParameter(OceanGridResolution.Medium512);
        public MinFloatParameter gridSize = new MinFloatParameter(1000.0f, 100.0f);
        public Vector4Parameter waveAmpltiude = new Vector4Parameter(new Vector4(2.0f, 2.0f, 2.0f, 2.0f));
        public Vector4Parameter choppiness = new Vector4Parameter(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
        public MinIntParameter numLevelOfDetais = new MinIntParameter(4, 0);
        public MaterialParameter material = new MaterialParameter(null);

        Ocean()
        {
            displayName = "Ocean";
        }
    }
}
