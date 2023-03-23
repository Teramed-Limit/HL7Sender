using System.Text.Json;
using System.Text.Json.Serialization;
using SendingPDFHL7.DataStructure;
using SendingPDFHL7.HL7Structure;
using SendingPDFHL7.JsonConverter;

namespace HL7Sender.DataStructure
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

        public Field FindFirst(string name)
        {
            return Flatten().First(x =>
            {
                if (x.Value is not string) return false;
                return x.Name == name;
            });
        }
    }
}