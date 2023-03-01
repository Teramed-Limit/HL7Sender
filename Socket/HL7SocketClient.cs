using System.Net.Sockets;
using System.Text;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapiTools.Base.Util;

namespace HL7Sender.Socket;

public class HL7SocketClient
{
    private System.Net.Sockets.Socket _socket;
    private readonly string _hostname;
    private readonly int _port;
    private readonly int _receiveTimeout;
    private readonly string _baseWorkDir = Path.Combine(Environment.CurrentDirectory, "HL7Logs");

    public HL7SocketClient(string hostname, int port, int receiveTimeout = 5000)
    {
        _port = port;
        _hostname = hostname;
        _receiveTimeout = receiveTimeout;
    }

    public HL7SocketClient Connect()
    {
        _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        _socket.Connect(_hostname, _port);
        return this;
    }

    public async Task<string> SendHL7Message(IMessage hl7IMsg)
    {
        try
        {
            var pipeParser = new PipeParser();
            var message = pipeParser.Encode(hl7IMsg);
            // 紀錄要求的訊息
            SaveHL7ToFile("request.hl7", message);
            message = MLLP.CreateMLLPMessage(message);

            var messageBytes = Encoding.UTF8.GetBytes(message);
            _ = await _socket.SendAsync(messageBytes, SocketFlags.None);

            var sb = new StringBuilder();
            var flag = false;
            var now = DateTime.Now;
            while (!flag)
            {
                // Receive ack.
                var buffer = new byte[1_024];
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.Peek);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                sb.Append(response);

                flag = MLLP.ValidateMLLPMessage(sb);

                if (response.Length > 0)
                    now = DateTime.Now;
                if (!flag && DateTime.Now.Subtract(now).TotalMilliseconds > _receiveTimeout)
                    throw new TimeoutException(
                        $"Reading the HL7 reply timed out after {_receiveTimeout} milliseconds.");
            }

            _socket.Shutdown(SocketShutdown.Both);

            MLLP.StripMLLPContainer(sb);

            // 紀錄回傳的訊息
            SaveHL7ToFile("response.hl7", sb.ToString());
            return sb.ToString();
        }
        catch (Exception e)
        {
            DisConnect();
            await Console.Out.WriteLineAsync($"SendHL7Message error, {e.Message}");
            throw;
        }
    }

    private void SaveHL7ToFile(string fileName, string hl7Msg)
    {
        if (!Directory.Exists(_baseWorkDir))
            Directory.CreateDirectory(_baseWorkDir);

        // 寫入文字到檔案
        fileName = $"{ArgumentCommander.NameOfHL7}_{DateTime.Now:yyyyMMdd-hhmmss}_{fileName}";
        File.WriteAllText(Path.Combine(_baseWorkDir, fileName), hl7Msg);
    }

    private void DisConnect()
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Dispose();
    }
}