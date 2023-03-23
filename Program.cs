using System.Text.RegularExpressions;
using HL7Sender;
using HL7Sender.Function;
using HL7Sender.Parser;
using HL7Sender.Socket;
using NHapi.Base.Parser;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/HL7Sender_Log-.log",
        rollingInterval: RollingInterval.Day, // 每小時一個檔案
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u5}] {Message:lj}{NewLine}{Exception}",
        flushToDiskInterval: TimeSpan.FromSeconds(5)
    )
    .CreateLogger();


try
{
    // 檢查接收的參數
    var result = ArgumentCommander.Validate();
    if (!result.success)
    {
        Console.Out.WriteLine(result.message);
        return 0;
    }

    var pipeParser = new PipeParser();
    // 產生HL7
    var hl7Message = File.ReadAllText(ArgumentCommander.HL7FilePath);
    var pattern = @"\{(\[.+?\])\((.+?)\)\}";
    hl7Message = Regex.Replace(hl7Message, pattern, match =>
    {
        var functionNameString = match.Groups[1].Value;
        var inputString = match.Groups[2].Value;

        var functionNames = new List<string>(functionNameString.Trim('[', ']').Split(','));
        var composedFunction = FunctionComposer.ComposeFunctions(new FunctionFactory().functions, functionNames);

        return composedFunction(inputString);
    });

    var requestHL7Msg = pipeParser.Parse(hl7Message);
    // 建立並連線HL7 Socket client
    var hl7Socket = new HL7SocketClient(ArgumentCommander.IP, ArgumentCommander.Port).Connect();
    // 發送HL7並接收回傳HL7
    var resposeHL7Msg = await hl7Socket.SendHL7Message(requestHL7Msg);

    // 解析HL7至MSA結構
    var resultTuple = new HL7Parser().ParseMSAMessage(resposeHL7Msg);
    Console.Out.WriteLine(resultTuple.message);
    return resultTuple.isSuccess ? 1 : 0;
}
catch (Exception e)
{
    Log.Logger.Error("Error occured while creating and transmitting HL7 message {Message}", e.Message);
    Console.Out.WriteLine($"Error occured while creating and transmitting HL7 message {e.Message}");
    return 0;
}