using System.Globalization;
using System.Text;
using System.Text.Json;
using SendingPDFHL7;

namespace HL7Sender.Function;

public class FunctionFactory
{
    private readonly Dictionary<string, Func<string[], string>> _functionDict = new();

    public FunctionFactory()
    {
        _functionDict.Add("GetJsonProperty", args => GetJsonProperty(args[0]));
        _functionDict.Add("GetCurrentTimeStamp", args => GetCurrentTimeStamp(args[0]));
        _functionDict.Add("GetSequenceNumber", _ => GetSequenceNumber());
        _functionDict.Add("ConvertToBase64", args => ConvertToBase64(args[0]));
        _functionDict.Add("GenerateOBXPDFBase64", args => GenerateOBXPDFBase64(args[0]));
    }

    public string Execute(string functionName, string[] functionParams)
    {
        return _functionDict[functionName](functionParams);
    }

    private string GetJsonProperty(string fieldName)
    {
        var streamReader = new StreamReader(ArgumentCommander.JsonFilePath);
        var jsonStr = streamReader.ReadToEnd();
        var json = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonStr);

        return json?[fieldName].ToString() ?? "";
    }

    private string GetCurrentTimeStamp(string format)
    {
        return DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
    }

    private string GetSequenceNumber()
    {
        const string facilityNumberPrefix = "1234"; // some arbitrary prefix for the facility
        return facilityNumberPrefix + GetCurrentTimeStamp("yyyyMMddHHmmss");
    }

    private string ConvertToBase64(string filePath)
    {
        Base64Helper base64Helper = new();
        var file = new FileInfo(filePath);
        return $"{base64Helper.ConvertToBase64String(file)}";
    }

    private string GenerateOBXPDFBase64(string filePath)
    {
        Base64Helper base64Helper = new();
        var file = new FileInfo(filePath);
        var fileBase64 = $"{base64Helper.ConvertToBase64String(file)}";

        // 超過200個字幅串，必須做切割
        int groupSize = 32000;
        if (fileBase64.Length > groupSize)
        {
            // OBX|1|FT|PDF_PART^Document_Partial_1^L|1|JVBERi0xLjQKJcfs...||||F
            // OBX|2|FT|PDF_PART^Document_Partial_2^L|1|jYhYi0xLjMKJYfs...||||F
            // 計算需要切割成幾組
            int numGroups = (int)Math.Ceiling((double)fileBase64.Length / groupSize);
            // 建立陣列
            string[] partialBase64List = new string[numGroups];
            // 將字串分割成一組一組
            for (int i = 0; i < numGroups; i++)
            {
                int startIndex = i * groupSize;
                int length = Math.Min(groupSize, fileBase64.Length - startIndex);
                partialBase64List[i] = fileBase64.Substring(startIndex, length);
            }

            var sb = new StringBuilder();
            int index = 1;
            foreach (var partialBase64 in partialBase64List)
            {
                var str = $"OBX|{index}|FT|PDF_PART^Document_Partial_{index}^L|1|{partialBase64}||||||F";
                sb.AppendLine(str);
                index++;
            }

            return sb.ToString();
        }

        // OBX|1|ED|PDF^Document^L|1|PDF^Base64^{ConvertToBase64(GetJsonProperty(pdffilePath))}|||N|||F
        return $"{base64Helper.ConvertToBase64String(file)}";
    }
}