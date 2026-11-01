using System.Net;
using MessagePack;

namespace chatter_messages;

[MessagePackObject]
public class ConnectionRedirect(string newAddress) : BaseMessage {
    [Key(0)]
    public string NewAddress { get; init;} = newAddress;

    public ConnectionRedirect(EndPoint newAddress) : this(newAddress.Serialize().ToString()) { }
}
