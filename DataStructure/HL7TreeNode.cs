using System.Text.Json;
using System.Text.Json.Serialization;
using SendingPDFHL7.HL7Structure;
using SendingPDFHL7.JsonConverter;

namespace SendingPDFHL7.DataStructure
{
    [JsonConverter(typeof(TreeNodeConverter))]
    public class HL7TreeNode : TreeNode<Field>
    {
        public HL7TreeNode(Field value) : base(value)
        {
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}