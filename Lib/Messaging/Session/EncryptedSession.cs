using System.Buffers.Binary;

namespace chatter_new.Messaging.Session;

public class EncryptedSession: ISession, IDisposable
{
    private readonly IConnection connection;
    private readonly List<byte> buffer = new List<byte>();
    private byte[]? Key = null;
    private DHKeyExchange? keyExchange = null;
    private BytesEncryption? encryption = null;
    private int leftToReceive = 0;
    
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<string>? OnReceive;
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
        
        Key = keyExchange!.DerivePrivateKey(buffer.ToArray());
        buffer.Clear();
        keyExchange.Dispose();
        
        encryption = new BytesEncryption(Key);
    }
    public void SendMessage(BaseMessage message)
    {
        var bytes = new BytesContainer(message.Serialize()).GetBytes();
        var encryptbytes = encryption!.Encrypt(bytes);
        var len = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(len, encryptbytes.Length);
        connection.Send(len.Concat(encryptbytes).ToArray());
        OnSend?.Invoke(this, message);
    }

    public void CheckForIncoming()
    {
        buffer.AddRange(connection.Receive());
        while(true) {
            if (leftToReceive == 0)
                if (buffer.Count >= 4)
                {
                    leftToReceive = BinaryPrimitives.ReadInt32BigEndian(buffer[..4].ToArray());
                    buffer.RemoveRange(0, 4);
                } else return;

            if (buffer.Count >= leftToReceive)
            {
                var msgb = encryption!.Decrypt(buffer[..leftToReceive].ToArray());
                buffer.RemoveRange(0, leftToReceive);
                leftToReceive = 0;

                var msg = new BytesContainer(msgb);
                OnReceive?.Invoke(this, msg.text);
            } else return;
        }
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
        Key = null;
        buffer.Clear();
    }
}