using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using chatter_new.Messaging;
using chatter_new.Messaging.Session;

Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;


Console.WriteLine("Hello, World!");

var ip = new IPEndPoint(IPAddress.Loopback, 50001);
EncryptedSession sess;

if (Console.ReadKey(true).Key == ConsoleKey.C )
{
    Console.WriteLine("Connect mode");
    sess = EncryptedSession.Create(SocketConnection.ConnectTo(ip), "foo");
    Console.WriteLine("Connected");
}
else
{
    Console.WriteLine("Await mode");
    sess = EncryptedSession.Create(SocketConnection.ListenAndAwaitClient(ip), "bar");
    Console.WriteLine("Client connected");
}

bool running = true;
Console.CancelKeyPress += (_, __) =>
{
    Console.WriteLine("Exiting...");
    sess.Close();
    running = false;
};

string nick = "";

sess.OnReceive += GetName;

sess.OnReceive += (sender, container) =>
{
    var json = JsonDocument.Parse(container);
    if (json.RootElement.GetProperty(nameof(BaseMessage.Type)).GetString() == nameof(TextMessage))
    {
        var msg = json.Deserialize<TextMessage>();
        Console.WriteLine(nick + ": " + msg?.Text);
    }
};

while (running)
{
    sess.CheckForIncoming();
    if (Console.KeyAvailable)
    {
        var inp = Console.ReadLine();
        if(!string.IsNullOrEmpty(inp))
            sess.SendMessage(new TextMessage(inp));
    }
}

void GetName(object? sender, string msg)
{
    var json = JsonDocument.Parse(msg);
    if (json.RootElement.GetProperty(nameof(BaseMessage.Type)).GetString() == nameof(UserInfoBaseMessage))
    {
        nick = json.RootElement.GetProperty(nameof(UserInfoBaseMessage.Name)).GetString()!;
    }

    sess.OnReceive -= GetName;
}