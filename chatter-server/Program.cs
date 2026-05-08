using System.Collections.Concurrent;
using System.Net;
using chatter_new.Messaging;
using chatter_new.Messaging.Messages;
using chatter_new.Messaging.Session;

var ep = new IPEndPoint(IPAddress.Any, 16777);
Console.WriteLine($"Server is running on {ep}");
var coutner = 0;
var tokenHolder = new CancellationTokenSource();
var sessionMaker = SocketConnection.ListenAndAwaitClients(ep).ConfigureAwait(false);
var sessions = new ConcurrentDictionary<int, EncryptedSession>();
_ = Task.Run(async () =>
{
    await foreach (var client in sessionMaker) {
        var newSess = await EncryptedSession.Create(client);
        Console.WriteLine("Got new client");
        var id = Interlocked.Increment(ref coutner);
        
        newSess.OnReceive += (s, msg) =>
        {
            if (msg is TextMessage tmsg)
                Console.WriteLine($"{id}: {tmsg.Text}");
            else if (msg is SystemMessage { Type: SystemMessage.SysMsgType.Left } sysmsg)
            {
                Console.WriteLine($"{id} sent {sysmsg.Type}");
                if (!sessions.Remove(id, out _)) {
                    Console.WriteLine($"[WARN]: {id} not found in sessions");
                }
            }
            else {
                Console.WriteLine($"{id}: <unsupported message>");
                // TODO: add msg ignore mechanism
            }
        };
        sessions[id] = newSess;
        Console.WriteLine($"Added client with id {id}");
    }
}, tokenHolder.Token);

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Shutting down...");
    foreach (var session in sessions.Values)
        session.SendMessage(new SystemMessage(SystemMessage.SysMsgType.Left));
    tokenHolder.Cancel();
};
while (true)
{
    foreach (var session in sessions.Values) session.CheckForIncoming();
    await Task.Delay(10, tokenHolder.Token);
    // add sending to clients   
}