using System.Net;
using System.Text;
using System.Text.Json;
using chatter_new.Messaging;
using chatter_new.Messaging.Messages;
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
        if (smsg.Type == SystemMessage.SysMsgType.Left)
        {
            Console.WriteLine($"{nick} has left. Exiting...");
            sess.Close();
            running = false;
        }
    }

    if (msg is BLOBMessage blob)
    {
        Console.WriteLine($"Got {blob.Filename} ({blob.Data.Length} bytes)");
        var path = Path.GetFullPath(blob.Filename);
        while (File.Exists(path))
        {
            var i = 1;
            path = Path.GetFullPath(Path.GetFileNameWithoutExtension(blob.Filename) + $"-{i}" + Path.GetExtension(blob.Filename));
            ++i;
        }
        Console.WriteLine($"Saving to {path} ({blob.Data.Length} bytes)");
        try
        {
            File.WriteAllBytes(path, blob.Data);
            Console.WriteLine($"Saved successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        
    }
};

while (running)
{
    sess.CheckForIncoming();
    if (Console.KeyAvailable)
    {
        var inp = Console.ReadLine();
        if(inp?.StartsWith("/img") ?? false)
        {
            try
            {
                var path = inp.Split()[1];
                var fname = Path.GetFileName(path);
                var bytes = File.ReadAllBytes(path);
                sess.SendMessage(new BLOBMessage(bytes, fname));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
        else if(!string.IsNullOrEmpty(inp))
            sess.SendMessage(new TextMessage(inp));
    }
}

void GetName(object? sender, string json)
{
    var msg = JsonSerializer.Deserialize<BaseMessage>(json);
    if (msg is UserInfoMessage userInfo)
    {
        nick = userInfo!.Name;
    }

    sess.OnReceive -= GetName;
}