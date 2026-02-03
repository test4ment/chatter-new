using System.Text.Json;
using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Session;

public class Session : ISession, IDisposable
{
    private readonly List<byte> buffer = new List<byte>();
    private readonly IConnection connection;
    private int leftToReceive = 0;
    
    private Session(IConnection connection)
    {
        this.connection = connection;
    }
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<BaseMessage>? OnReceive;
    
    public static Session CreateSession(string name, IConnection connection)
    {
        var session = new Session(connection);
        session.SendMessage(new UserInfoMessage(name));
        
        return session;
    }
    
    public void SendMessage(BaseMessage msg)
    {
        var bytes = BytesHelper.Encode(msg.Serialize());
        var len = BytesHelper.Encode(bytes.Length);
        connection.Send(len);
        connection.Send(bytes.ToArray());
        OnSend?.Invoke(this, msg);
    }

    public void CheckForIncoming()
    {
        buffer.AddRange(connection.Receive());

        while (true)
        {
            if (leftToReceive == 0)
                if (buffer.Count >= sizeof(int))
                {
                    leftToReceive = BytesHelper.DecodeInt(buffer[..sizeof(int)]);
                    buffer.RemoveRange(0, sizeof(int));
                } else return;

            if (buffer.Count < leftToReceive)
                return;
            
            var msg = BytesHelper.Decode(buffer[..leftToReceive].ToArray());
            buffer.RemoveRange(0, leftToReceive);
            leftToReceive = 0;
            OnReceive?.Invoke(this, JsonSerializer.Deserialize<BaseMessage>(msg)!);
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
