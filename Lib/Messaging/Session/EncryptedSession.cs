using System.Text.Json;
using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Session;

public class EncryptedSession: ISession, IDisposable
{
    private readonly IConnection connection;
    private readonly List<byte> buffer = new List<byte>();
    private DHKeyExchange? keyExchange = null;
    private BytesEncryption? encryption = null;
    private int leftToReceive = 0;
    private MessageMetadata? metadata = null;
    private int Sent = 0;
    
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<BaseMessage>? OnReceive;
    public event EventHandler<Progress>? OnMsgProgress;
    private EncryptedSession(IConnection connection)
    {
        this.connection = connection;
    }

    public static EncryptedSession Create(IConnection connection, string name)
    {
        var session = new EncryptedSession(connection);
        
        session.SendHandshake();
        session.AwaitHandshake();
        
        session.SendMessage(new UserInfoMessage(name));

        return session;
    }
    private void SendHandshake()
    {
        keyExchange = new DHKeyExchange();
        connection.Send(keyExchange.PublicKey);
    }
    private void AwaitHandshake()
    {
        while(buffer.Count < keyExchange!.PublicKey.Length)
        {
            buffer.AddRange(connection.Receive());
        }
        
        var key = keyExchange!.DerivePrivateKey(buffer.ToArray());
        buffer.Clear();
        keyExchange.Dispose();
        
        encryption = new BytesEncryption(key);
    }
    public void SendMessage(BaseMessage message)
    {
        var bytes = message.Serialize().Encode();
        var encryptbytes = encryption!.Encrypt(bytes);
        
        var meta = new MessageMetadata()
        {
            ContentSize = encryptbytes.Length, 
            TrackProgress = false,
            Num = Sent++
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
        buffer.AddRange(connection.Receive());
        while(true)
        {
            if (leftToReceive == 0)
                if (buffer.Count >= 4)
                {
                    leftToReceive = buffer[..sizeof(int)].ToArray().DecodeInt();
                    buffer.RemoveRange(0, 4);
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

    public void UpdateProgress()
    {
        OnMsgProgress?.Invoke(this, new Progress() {
            Num = Sent, Current = buffer.Count, Total = leftToReceive
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
    
    public void Close()
    {
        CheckForIncoming();
        if(connection is IDisposable disposable)
            disposable.Dispose();
    }
    public void Dispose()
    {
        if(connection is IDisposable disposable)
            disposable.Dispose();
        keyExchange?.Dispose();
        buffer.Clear();
    }
}