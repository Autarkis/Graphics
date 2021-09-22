using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Ocean", "EvaluateSimulationDisplacement_Ocean (Preview)")]
    class EvaluateSimulationDisplacement_Ocean : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireVertexID
    {
        public EvaluateSimulationDisplacement_Ocean()
        {
            name = "Evaluate Simulation Displacement (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationDisplacement_Ocean");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kDisplacementOutputSlotId = 1;
        const string kDisplacementOutputSlotName = "Displacement";

        const int kLowFrequencyHeightOutputSlotId = 2;
        const string kLowFrequencyHeightOutputSlotName = "LowFrequencyHeight";

        const int kFoamFromHeightOutputSlotId = 3;
        const string kFoamFromHeightOutputSlotName = "FoamFromHeight";

        const int kSSSMaskOutputSlotId = 4;
        const string kSSSMaskOutputSlotName = "SSSMask";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kDisplacementOutputSlotId, kDisplacementOutputSlotName, kDisplacementOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightOutputSlotName, kLowFrequencyHeightOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kFoamFromHeightOutputSlotId, kFoamFromHeightOutputSlotName, kFoamFromHeightOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSSSMaskOutputSlotId, kSSSMaskOutputSlotName, kSSSMaskOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSInputSlotId,
                kDisplacementOutputSlotId,
                kLowFrequencyHeightOutputSlotId,
                kFoamFromHeightOutputSlotId,
                kSSSMaskOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("OceanDisplacementData displacementData;");
                sb.AppendLine("ZERO_INITIALIZE(OceanDisplacementData, displacementData);");

                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                sb.AppendLine("EvaluateOceanDisplacement({0}, displacementData);",
                    positionWS
                );

                sb.AppendLine("$precision3 {0} = displacementData.displacement;",
                    GetVariableNameForSlot(kDisplacementOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.lowFrequencyHeight;",
                    GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.foamFromHeight;",
                    GetVariableNameForSlot(kFoamFromHeightOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.sssMask;",
                    GetVariableNameForSlot(kSSSMaskOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kDisplacementOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kFoamFromHeightOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kSSSMaskOutputSlotId)
                );
            }
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }
    }
}
