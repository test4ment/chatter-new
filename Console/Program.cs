using System.Net;
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
    sess.SendMessage(new SystemMessage(SystemMessage.SysMsgType.Left));
    sess.Close();
    running = false;
};

string nick = "";

sess.OnReceive += GetName;

sess.OnReceive += (sender, container) =>
{
    var msg = JsonSerializer.Deserialize<BaseMessage>(container);
    if (msg is TextMessage tmsg)
    {
        Console.WriteLine(nick + ": " + tmsg.Text);
    }

    if (msg is SystemMessage smsg)
    {
        if (smsg.SystemType == SystemMessage.SysMsgType.Left)
        {
            Console.WriteLine($"{nick} has left. Exiting...");
            sess.Close();
            running = false;
        }
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

void GetName(object? sender, string json)
{
    var msg = JsonSerializer.Deserialize<BaseMessage>(json);
    if (msg is UserInfoBaseMessage userInfo)
    {
        nick = userInfo!.Name;
    }

    sess.OnReceive -= GetName;
}