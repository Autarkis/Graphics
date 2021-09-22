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
    [Title("Utility", "High Definition Render Pipeline", "Ocean", "GetGridPositionWS_Ocean (Preview)")]
    class GetGridPositionWS_Ocean : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireVertexID
    {
        public GetGridPositionWS_Ocean()
        {
            name = "Compute Ocean PositionWS (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("GetGridPositionWS_Ocean");

        const int kPositionWSOutputSlotId = 0;
        const string kPositionWSOutputSlotName = "PositionWS";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionWSOutputSlotId, kPositionWSOutputSlotName, kPositionWSOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision3 {0} = GetVertexPositionFromVertexID(IN.{1}, _GridRenderingResolution, _PatchOffset, _CameraOffset);",
                  GetVariableNameForSlot(kPositionWSOutputSlotId),
                  ShaderGeneratorNames.VertexID
              );
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                 GetVariableNameForSlot(kPositionWSOutputSlotId));
            }
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }
    }
}
