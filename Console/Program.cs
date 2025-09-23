using System.Net;
using System.Text;
using System.Text.Json;
using chatter_new.Messaging;

Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;


Console.WriteLine("Hello, World!");

var ip = new IPEndPoint(IPAddress.Loopback, 50001);
Session sess;

if (Console.ReadKey(true).Key == ConsoleKey.C )
{
    Console.WriteLine("Connect mode");
    sess = Session.CreateSession("foo", SocketConnection.ConnectTo(ip));
    Console.WriteLine("Connected");
}
else
{
    Console.WriteLine("Await mode");
    sess = Session.CreateSession("bar", SocketConnection.ListenAndAwaitClient(ip));
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
    var json = JsonDocument.Parse(container.text);
    if (json.RootElement.GetProperty(nameof(IMessage.Type)).GetString() == nameof(TextMessage))
        Console.WriteLine(nick + ": " + json.RootElement.GetProperty(nameof(TextMessage.text)).GetString());
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

void GetName(object? sender, BytesContainer container)
{
    var json = JsonDocument.Parse(container.text);
    if (json.RootElement.GetProperty(nameof(IMessage.Type)).GetString() == nameof(UserInfoMessage))
    {
        nick = json.RootElement.GetProperty(nameof(UserInfoMessage.name)).GetString()!;
    }

    sess.OnReceive -= GetName;
}