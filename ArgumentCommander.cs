using Serilog;

namespace HL7Sender;

public static class ArgumentCommander
{
    public static string IP;
    public static int Port;
    public static string HL7FilePath;
    public static string JsonFilePath;
    public static string NameOfHL7;

    // 檢查接收的參數
    // 0: IP
    // 1: Port
    // 2: HL7 template file
    // 3: Json file
    public static (bool success, string message) Validate()
    {
        var arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
        IP = arguments[0];
        Port = Convert.ToInt32(arguments[1]);
        HL7FilePath = arguments[2];
        JsonFilePath = arguments[3];
        NameOfHL7 = arguments[4];

        if (string.IsNullOrWhiteSpace(IP) ||
            string.IsNullOrWhiteSpace(arguments[1]) ||
            string.IsNullOrWhiteSpace(HL7FilePath) ||
            string.IsNullOrWhiteSpace(JsonFilePath) ||
            string.IsNullOrWhiteSpace(NameOfHL7))
        {
            var errorStr =
                $"Missing arguments, IP: {IP}, Port: {Port}, HL7FilePath: {HL7FilePath}, JsonFilePath: {JsonFilePath}, NameOfHL7: {NameOfHL7}";
            Log.Error(errorStr);
            return (false, errorStr);
        }

        if (!File.Exists(HL7FilePath) && !File.Exists(JsonFilePath))
        {
            var errorStr = $"Missing files, HL7FilePath: {HL7FilePath}, JsonFilePath: {JsonFilePath}";
            Log.Error(errorStr);
            return (false, errorStr);
        }

        return (true, "");
    }
}