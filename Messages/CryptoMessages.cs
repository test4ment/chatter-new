using MessagePack;

namespace chatter_messages;

[MessagePackObject]
public class DHHandshake(byte[] publicKey): BaseMessage {
    [Key(0)]
    public byte[] publicKey { get; init; } = publicKey;
}
