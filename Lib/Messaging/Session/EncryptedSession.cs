using System.Buffers;
using System.Text.Json;
using chatter_crypto;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Session;

public class EncryptedSession: ISession, IDisposable
{
    private enum ReceiverState { AwaitingHeader, AwaitingMetadata, AwaitingPayload }
    private readonly IConnectionAsync connection;
    private readonly List<byte> buffer = new List<byte>(); // TODO: use IO.Pipelines
    private DHKeyExchange? keyExchange = null; // TODO: key cycling
    private UniversalEncryption? encryption = null; // TODO: encryption dependency
    private ReceiverState _currentState = ReceiverState.AwaitingHeader;
    private int _remainingBytes = 0;
    private MessageMetadata? _pendingMetadata = null;
    private int sent = 0;
    public bool IsDisposed { get; private set; } = false;
    
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<BaseMessage>? OnReceive;
    [Obsolete]
    public event EventHandler<Progress>? OnMsgProgress;
    private EncryptedSession(IConnectionAsync connection)
    {
        this.connection = connection;
    }

    public static async Task<EncryptedSession> Create(IConnectionAsync connection) // TODO: Async
    {
        var session = new EncryptedSession(connection);
        
        await session.SendHandshake();
        await session.AwaitHandshake();
        
        return session;
    }
    private async Task SendHandshake()
    {
        keyExchange = new DHKeyExchange(); // TODO: inject key exchange
        await connection.SendAsync(keyExchange.PublicKey);
    }
    private async Task AwaitHandshake()
    {
        var len = keyExchange!.PublicKey.Length;
        var keyBuf = ArrayPool<byte>.Shared.Rent(len);
        byte[] key;
        try
        {
            var recv = 0;
            while ((recv += await connection.ReceiveAsync(keyBuf)) < len)
            {
                await Task.Delay(1);
            }

            key = keyExchange!.DerivePrivateKey(keyBuf[..len]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(keyBuf, true);
        }
        
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

        bool canContinue = true;
        while (canContinue)
        {
            canContinue = _currentState switch
            {
                ReceiverState.AwaitingHeader   => TryReadHeader(),
                ReceiverState.AwaitingMetadata => TryReadMetadata(),
                ReceiverState.AwaitingPayload  => TryReadPayload(),
                _ => false
            };
        }
    }

    private bool TryReadHeader()
    {
        if (buffer.Count < sizeof(int))
            return false;

        _remainingBytes = buffer[..sizeof(int)].ToArray().DecodeInt();
        buffer.RemoveRange(0, sizeof(int));
        _currentState = ReceiverState.AwaitingMetadata;
        return true;
    }

    private bool TryReadMetadata()
    {
        if (buffer.Count < _remainingBytes)
            return false;

        var encrypted = buffer[.._remainingBytes].ToArray();
        buffer.RemoveRange(0, _remainingBytes);
        var decrypted = encryption!.Decrypt(encrypted).Decode();
        _pendingMetadata = JsonSerializer.Deserialize<MessageMetadata>(decrypted);
        _remainingBytes = _pendingMetadata!.ContentSize;
        _currentState = ReceiverState.AwaitingPayload;
        return true;
    }

    private bool TryReadPayload()
    {
        if (buffer.Count < _remainingBytes)
        {
            if (_pendingMetadata is { TrackProgress: true })
                OnMsgProgress?.Invoke(this, new Progress()
                {
                    Current = buffer.Count, Total = _pendingMetadata.ContentSize
                });
            return false;
        }

        var encrypted = buffer[.._remainingBytes].ToArray();
        buffer.RemoveRange(0, _remainingBytes);
        var decrypted = encryption!.Decrypt(encrypted).Decode();
        OnReceive?.Invoke(this, JsonSerializer.Deserialize<BaseMessage>(decrypted)!); 

        _remainingBytes = 0;
        _pendingMetadata = null;
        _currentState = ReceiverState.AwaitingHeader;
        return true;
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