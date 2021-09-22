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
    [Title("Utility", "High Definition Render Pipeline", "Ocean", "EvaluateSimulationAdditionalData_Ocean (Preview)")]
    class EvaluateSimulationAdditionalData_Ocean : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireVertexID
    {
        public EvaluateSimulationAdditionalData_Ocean()
        {
            name = "Evaluate Simulation Additional Data Ocean (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationAdditionalData_Ocean");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kInputNormalWSInputSlotId = 1;
        const string kInputNormalWSInputSlotName = "InputNormalWS";

        const int kNormalWSOutputSlotId = 2;
        const string kNormalWSOutputSlotName = "NormalWS";

        const int kLowFrequencyNormalWSOutputSlotId = 3;
        const string kLowFrequencyNormalWSOutputSlotName = "LowFrequencyNormalWS";

        const int kPhaseDetailNormalWSOutputSlotId = 4;
        const string kPhaseDetailNormalWSOutputSlotName = "PhaseDetailNormalWS";

        const int kFoamOutputSlotId = 5;
        const string kFoamOutputSlotName = "Foam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kInputNormalWSInputSlotId, kInputNormalWSInputSlotName, kInputNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            
            AddSlot(new Vector3MaterialSlot(kNormalWSOutputSlotId, kNormalWSOutputSlotName, kNormalWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSOutputSlotId, kLowFrequencyNormalWSOutputSlotName, kLowFrequencyNormalWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kPhaseDetailNormalWSOutputSlotId, kPhaseDetailNormalWSOutputSlotName, kPhaseDetailNormalWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kFoamOutputSlotId, kFoamOutputSlotName, kFoamOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSInputSlotId,
                kInputNormalWSInputSlotId,

                kNormalWSOutputSlotId,
                kLowFrequencyNormalWSOutputSlotId,
                kPhaseDetailNormalWSOutputSlotId,
                kFoamOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                string inputNormalWS = GetSlotValue(kInputNormalWSInputSlotId, generationMode);

                sb.AppendLine("OceanAdditionalData oceanAdditionalData;");
                sb.AppendLine("ZERO_INITIALIZE(OceanAdditionalData, oceanAdditionalData);");
                
                sb.AppendLine("EvaluateOceanAdditionalData({0}, {1}, oceanAdditionalData);",
                    positionWS,
                    inputNormalWS
                );

                sb.AppendLine("$precision3 {0} = oceanAdditionalData.normalWS;",
                    GetVariableNameForSlot(kNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = oceanAdditionalData.lowFrequencyNormalWS;",
                    GetVariableNameForSlot(kLowFrequencyNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = oceanAdditionalData.phaseNormalWS;",
                    GetVariableNameForSlot(kPhaseDetailNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision {0} = oceanAdditionalData.foam;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kLowFrequencyNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kPhaseDetailNormalWSOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );

            }
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }
    }
}
