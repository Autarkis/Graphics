using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "HDRP", "WaterAdditionalData")]
    class WaterAdditionalDataNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }

        public WaterAdditionalDataNode()
        {
            name = "WaterAdditionalData";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision3 {0} = IN.WaterAdditionalData;",
                GetVariableNameForSlot(OutputSlotId)));
        }
    }
}
