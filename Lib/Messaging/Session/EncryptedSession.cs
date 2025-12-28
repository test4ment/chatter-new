using System.Net.Sockets;
using System.Security.Cryptography;

namespace chatter_new.Messaging.Session;

public class EncryptedSession: ISession, IDisposable
{
    private readonly IConnection connection;
    private readonly List<byte> buffer = new List<byte>();
    private readonly byte[] enc_buffer = new byte[1024];
    private byte[]? Key = null;
    private DHKeyExchange? keyExchange = null;
    private BytesEncryption? encryption = null;
    private CryptoStream? cryptostream = null;
    private MemoryStream memstream;
    
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<string>? OnReceive;
    private EncryptedSession(IConnection connection)
    {
        this.connection = connection;
        memstream = new MemoryStream();
    }

    public static EncryptedSession Create(IConnection connection, string name)
    {
        var session = new EncryptedSession(connection);
        
        session.SendHandshake();
        session.AwaitHandshake();
        
        session.SendMessage(new UserInfoBaseMessage(name));

        return session;
    }
    private void SendHandshake()
    {
        keyExchange = new DHKeyExchange();
        connection.Send(keyExchange.PublicKey.Append<byte>(UnencryptedSession.EOT).ToArray());
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

        cryptostream = new CryptoStream(memstream, encryption.GetDecryptor(), CryptoStreamMode.Read);
    }
    public void SendMessage(BaseMessage message)
    {
        var bytes = new BytesContainer(message.Serialize());
        var pl = bytes.GetBytes().Append<byte>(UnencryptedSession.EOT).ToArray();
        connection.Send(encryption!.Encrypt(pl));
        OnSend?.Invoke(this, message);
    }

    public void CheckForIncoming()
    {
        var pos = memstream.Position;
        memstream.Write(connection.Receive());
        memstream.Seek(pos, SeekOrigin.Begin);

        int read = 0;
        do
        {
            if(cryptostream!.HasFlushedFinalBlock)
                cryptostream = new CryptoStream(memstream, encryption!.GetDecryptor(), CryptoStreamMode.Read);
            read = cryptostream!.Read(enc_buffer);
            buffer.AddRange(enc_buffer[..read]);
        } while (read > 0);
        
        var eof = buffer.IndexOf(UnencryptedSession.EOT);
        while(eof >= 0)
        {
            var msg = new BytesContainer(buffer[..eof].ToArray());
            buffer.RemoveRange(0, eof+1);
            OnReceive?.Invoke(this, msg.text);
            
            eof = buffer.IndexOf(UnencryptedSession.EOT);
        }
    }
    
    public void Close()
    {
        CheckForIncoming();
        connection.Dispose();
    }
    public void Dispose()
    {
        connection.Dispose();
        memstream.Dispose();
        cryptostream?.Dispose();
        keyExchange?.Dispose();
        Key = null;
        buffer.Clear();
    }
}