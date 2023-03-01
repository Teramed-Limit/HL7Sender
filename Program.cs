using HL7Sender;
using HL7Sender.Parser;
using HL7Sender.Socket;
using NHapi.Base.Parser;

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
    hl7Message = StringParameterParser.Parse(hl7Message);
    var requestHL7Msg = pipeParser.Parse(hl7Message);
    // 建立並連線HL7 Socket client
    var hl7Socket = new HL7SocketClient(ArgumentCommander.IP, ArgumentCommander.Port).Connect();
    // 發送HL7並接收回傳HL7
    var resposeHL7Msg = await hl7Socket.SendHL7Message(requestHL7Msg);

    Console.Out.WriteLine(resposeHL7Msg);
    // 解析HL7至TreeNode結構
    // var requestTreeNode = new HL7Parser().ParseHL7Message(hl7Message);
    // LogToDebugConsole($"{requestTreeNode.ToJson()}");

    // var responseTreeNode = new HL7Parser().ParseHL7Message(resposeHL7Msg);
    // LogToDebugConsole($"{responseTreeNode.ToJson()}");
}
catch (Exception e)
{
    Console.Out.WriteLine($"Error occured while creating and transmitting HL7 message {e.Message}");
    return 0;
}

return 1;