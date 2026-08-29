using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using chatter_crypto;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;

Console.OutputEncoding = Encoding.Unicode;

var ep = new IPEndPoint(IPAddress.Any, 16777);
Console.WriteLine($"Server is running on {ep}");

var tokenHolder = new CancellationTokenSource();

byte[] PrepareMsg(UniversalEncryption enc, BaseMessage msg)
{
    return enc.Encrypt(msg.Serialize().Encode());
}

BaseMessage ProcessMsg(UniversalEncryption enc, byte[] msg)
{
    return JsonSerializer.Deserialize<BaseMessage>(enc.Decrypt(msg).Decode())!;
}

var clients = new ConcurrentDictionary<int, (Protocol Session, UniversalEncryption Enc, string Username)>();
var idCounter = 0;

void Broadcast(int senderId, BaseMessage msg)
{
    foreach (var (id, client) in clients)
    {
        if (id == senderId) continue;
        try
        {
            _ = client.Session.Send(PrepareMsg(client.Enc, msg), tokenHolder.Token);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{id}] Failed to deliver: {e.Message}");
        }
    }
}

_ = Task.Run(async () =>
{
    await foreach (var connection in SocketConnection.ListenAndAwaitClients(ep, tokenHolder.Token).ConfigureAwait(false))
    {
        var id = Interlocked.Increment(ref idCounter);
        _ = Task.Run(async () =>
        {
            var sess = new Protocol(connection);

            Console.WriteLine($"[{id}] Client connected, performing handshake");
            var enc = await new DHHandshake(sess).Perform(tokenHolder.Token);
            Console.WriteLine($"[{id}] Handshake complete");

            await sess.Send(PrepareMsg(enc, new UserInfoMessage("server")));
            Console.WriteLine($"[{id}] Sent greeting");

            var msgQueue = new ConcurrentQueue<byte[]>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var frame in sess.ReadFramesAsync(tokenHolder.Token))
                        msgQueue.Enqueue(frame);
                }
                catch (OperationCanceledException) { }
            }, tokenHolder.Token);

            string username = $"client{id}";
            byte[]? result;
            while (!msgQueue.TryDequeue(out result)) ;
            var first = ProcessMsg(enc, result);
            if (first is UserInfoMessage userInfo)
                username = userInfo.Name;

            clients[id] = (sess, enc, username);
            Console.WriteLine($"[{id}] Registered as {username}");

            while (!tokenHolder.IsCancellationRequested)
            {
                if (msgQueue.IsEmpty)
                {
                    await Task.Delay(16, tokenHolder.Token);
                    continue;
                }

                if (!msgQueue.TryDequeue(out var frame)) continue;
                var msg = ProcessMsg(enc, frame);

                switch (msg)
                {
                    case TextMessage tmsg:
                        Console.WriteLine($"{username}: {tmsg.Text}");
                        Broadcast(id, new RetransmittedMessage(username, tmsg));
                        break;
                    case SystemMessage { Type: SystemMessage.SysMsgType.Left }:
                        Console.WriteLine($"{username} has left");
                        clients.TryRemove(id, out _);
                        Broadcast(id, new TextMessage($"{username} has left"));
                        return;
                    default:
                        Console.WriteLine($"[{id}] <unsupported message>");
                        break;
                }
            }
        }, tokenHolder.Token);
    }
}, tokenHolder.Token);

Console.CancelKeyPress += (_, __) =>
{
    Console.WriteLine("Shutting down...");
    foreach (var client in clients.Values)
        client.Session.Send(PrepareMsg(client.Enc, new SystemMessage(SystemMessage.SysMsgType.Left))).Wait();
    tokenHolder.Cancel();
    Environment.Exit(0);
};

while (!tokenHolder.IsCancellationRequested)
{
    await Task.Delay(100, tokenHolder.Token);
}
