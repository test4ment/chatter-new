namespace chatter_new.Messaging.Session;

public class UnencryptedSession : ISession, IDisposable
{
    public const byte EOT = 0x03;
    private readonly List<byte> buffer = new List<byte>();
    private readonly IConnection connection;
    private UnencryptedSession(IConnection connection)
    {
        this.connection = connection;
    }
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<string>? OnReceive;
    
    public static UnencryptedSession CreateSession(string name, IConnection connection)
    {
        var session = new UnencryptedSession(connection);
        session.SendMessage(new UserInfoBaseMessage(name));
        
        return session;
    }
    
    public void SendMessage(BaseMessage msg)
    {
        var bytes = new BytesContainer(msg.Serialize());
        connection.Send(bytes.GetBytes().Append<byte>(EOT).ToArray());
        OnSend?.Invoke(this, msg);
    }

    public void CheckForIncoming()
    {
        buffer.AddRange(connection.Receive());

        var eof = buffer.IndexOf(EOT); // TODO: 0x03 may exist in unicode
        while(eof >= 0)
        {
            var msg = new BytesContainer(buffer[..eof].ToArray());
            buffer.RemoveRange(0, eof+1);
            OnReceive?.Invoke(this, msg.text);
            
            eof = buffer.IndexOf(EOT);
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
    }
}
