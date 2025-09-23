namespace chatter_new.Messaging;

public class Session : IDisposable
{
    private List<byte> buffer = new List<byte>();
    private readonly IConnection _connection;
    private Session(IConnection connection)
    {
        _connection = connection;
    }
    public event EventHandler<BytesContainer>? OnSend;
    public event EventHandler<BytesContainer>? OnReceive;
    
    public static Session CreateSession(string name, IConnection connection)
    {
        var session = new Session(connection);
        session.SendMessage(new UserInfoMessage(name));
        
        return session;
    }
    
    public void SendMessage(IMessage message)
    {
        var msg = new BytesContainer(message.Serialize());
        _connection.Send(msg);
        OnSend?.Invoke(this, msg);
    }

    public void CheckForIncoming()
    {
        buffer.AddRange(_connection.Receive());

        var eof = buffer.IndexOf(IConnection.EOT);
        while(eof >= 0)
        {
            var msg = new BytesContainer(buffer[..eof].ToArray());
            buffer.RemoveRange(0, eof+1);
            OnReceive?.Invoke(this, msg);
            
            eof = buffer.IndexOf(0);
        }
    }
    
    public void Close()
    {
        CheckForIncoming();
        _connection.Dispose();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
