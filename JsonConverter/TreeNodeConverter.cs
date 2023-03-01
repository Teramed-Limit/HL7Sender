using System.Text.Json;
using System.Text.Json.Serialization;
using SendingPDFHL7.DataStructure;
using SendingPDFHL7.HL7Structure;

namespace SendingPDFHL7.JsonConverter;

public class TreeNodeConverter : JsonConverter<TreeNode<Field>>
{
    public override bool CanConvert(Type objectType)
    {
        // we can serialize everything that is a TreeNode
        return typeof(TreeNode<Field>).IsAssignableFrom(objectType);
    }

    public override TreeNode<Field> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // we currently support only writing of JSON
        throw new NotImplementedException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        TreeNode<Field> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var dictionaryObj = Traverse(value);
        writer.WritePropertyName(value.Value.Name);
        JsonSerializer.Serialize(writer, dictionaryObj, options);
        writer.WriteEndObject();
    }

    private Dictionary<string, object> Traverse(TreeNode<Field> value)
    {
        Dictionary<string, object> dictionary = new();
        foreach (var child in value.Children)
        {
            if (child.Children.Any())
            {
                dictionary.Add(child.Value.Name, Traverse(child));
            }
            else
            {
                dictionary.Add(child.Value.Name, child.Value.Value);
            }
        }

        return dictionary;
    }
}