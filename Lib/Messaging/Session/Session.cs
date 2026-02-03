using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Session;

public interface ISession
{
    public void SendMessage(BaseMessage message);
    public void CheckForIncoming();
    public event EventHandler<BaseMessage>? OnSend;
    public event EventHandler<string>? OnReceive;
}
