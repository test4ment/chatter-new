using System.Net;
using System.Text;
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

sess.OnReceive += (sender, container) 
    => Console.WriteLine(container.text);

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
