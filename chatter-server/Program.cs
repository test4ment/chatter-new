using System.Collections.Concurrent;
using System.Net;
using chatter_new.Messaging;
using chatter_new.Messaging.Messages;
using chatter_new.Messaging.Session;

var sessionMaker = SocketConnection.ListenAndAwaitClients(new IPEndPoint(IPAddress.Any, 16777)).ConfigureAwait(false);
var sessions = new ConcurrentDictionary<int, EncryptedSession>();
var listenerTask = Task.Run(async () =>
{
    await foreach (var client in sessionMaker)
    {
        var newSess = await EncryptedSession.Create(client);
        var id = sessions.Count;
        newSess.OnReceive += (s, msg) =>
        {
            if (msg is TextMessage tmsg)
            {
                Console.WriteLine($"{id}: {tmsg.Text}");
            }
            else if (msg is SystemMessage sysmsg && sysmsg.Type == SystemMessage.SysMsgType.Left)
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
    }
});

while (true)
{
    
}

listenerTask.Cancel
Console.WriteLine("Hello, World!");