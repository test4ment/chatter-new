using System.Buffers;
using System.Text.Json;
using chatter_crypto;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Session;

public class EncryptedSession: ISession, IDisposable
{
    private readonly IConnectionAsync connection;
    private readonly List<byte> buffer = new List<byte>(); // TODO: use IO.Pipelines
    private DHKeyExchange? keyExchange = null; // TODO: key cycling
    private UniversalEncryption? encryption = null;
    private int leftToReceive = 0;
    private MessageMetadata? metadata = null;
    private int sent = 0;
    public bool IsDisposed { get; private set; } = false;
    
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<BaseMessage>? OnReceive;
    public event EventHandler<Progress>? OnMsgProgress;
    private EncryptedSession(IConnectionAsync connection)
    {
        this.connection = connection;
    }

    public static async Task<EncryptedSession> Create(IConnectionAsync connection) // TODO: Async
    {
        var session = new EncryptedSession(connection);
        
        session.SendHandshake();
        await session.AwaitHandshake();
        
        return session;
    }
    private void SendHandshake()
    {
        keyExchange = new DHKeyExchange();
        connection.Send(keyExchange.PublicKey);
    }
    private async Task AwaitHandshake()
    {
        var len = keyExchange!.PublicKey.Length;
        var keyBuf = ArrayPool<byte>.Shared.Rent(len);
        var recv = 0;
        while ((recv += await connection.ReceiveAsync(keyBuf)) < len)
        {
            await Task.Delay(1);
        }
        
        var key = keyExchange!.DerivePrivateKey(keyBuf[..len]);
        ArrayPool<byte>.Shared.Return(keyBuf);
        
        keyExchange.Dispose();
        keyExchange = null;
        
        encryption = new UniversalEncryption(key, false);
    }
    public void SendMessage(BaseMessage message)
    {
        var bytes = message.Serialize().Encode();
        var encryptbytes = encryption!.Encrypt(bytes);
        
        var meta = new MessageMetadata()
        {
            ContentSize = encryptbytes.Length, 
            TrackProgress = encryptbytes.Length >= 128 * 1024, // TODO: big messages or files
            Num = sent++
        }.Serialize();
        var metab = meta.Encode();
        var metaenc = encryption!.Encrypt(metab);
        
        connection.Send(metaenc.Length.Encode());
        connection.Send(metaenc);
        connection.Send(encryptbytes);
        
        OnSend?.Invoke(this, message);
    }

    public void CheckForIncoming()
    {
        var buf = ArrayPool<byte>.Shared.Rent(connection.Available);
        var recv = connection.Receive(buf); // blocks
        buffer.AddRange(buf[..recv]);
        ArrayPool<byte>.Shared.Return(buf);
        
        while(true)
        {
            if (leftToReceive == 0)
                if (buffer.Count >= sizeof(int))
                {
                    leftToReceive = buffer[..sizeof(int)].ToArray().DecodeInt();
                    buffer.RemoveRange(0, sizeof(int));
                } else return;

            if (buffer.Count < leftToReceive)
            {
                if(metadata is { TrackProgress: true }) UpdateProgress();         
                return;
            }
            
            if (metadata == null)
                ProccessMeta();
            else {
                if(metadata is { TrackProgress: true }) UpdateProgress();
                ProccessMessage();
            }
        }
    }

    private void UpdateProgress()
    {
        OnMsgProgress?.Invoke(this, new Progress() {
            Num = sent, Current = buffer.Count, Total = metadata!.ContentSize
        });
    }

    private void ProccessMeta()
    {
        var msg = ReadMessage().Decode();
        metadata = JsonSerializer.Deserialize<MessageMetadata>(msg);
        leftToReceive = metadata!.ContentSize;
    }

    private void ProccessMessage()
    {
        var msg = ReadMessage().Decode();
        OnReceive?.Invoke(this, JsonSerializer.Deserialize<BaseMessage>(msg)!);
        metadata = null;
    }

    private byte[] ReadMessage()
    {
        var msgb = encryption!.Decrypt(buffer[..leftToReceive].ToArray());
        buffer.RemoveRange(0, leftToReceive);
        leftToReceive = 0;
        return msgb;
    }
    
    public void Dispose()
    {
        IsDisposed = true;
        if(connection is IDisposable disposable)
            disposable.Dispose();
        keyExchange?.Dispose();
        buffer.Clear();
    }
}