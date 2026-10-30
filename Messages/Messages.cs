using MessagePack;

namespace chatter_messages;

[MessagePackObject]
[UnionTag<TextMessage>(0)]
[UnionTag<UserInfoMessage>(1)]
[UnionTag<SystemMessage>(2)]
[UnionTag<BLOBMessage>(3)]
[UnionTag<RetransmittedMessage>(4)]
[UnionTag<ChunkedBlob>(5)]
public abstract class BaseMessage
{
    public byte[] Serialize()
        => MessagePackSerializer.Serialize(this);

    public static BaseMessage Deserialize(byte[] data)
        => MessagePackSerializer.Deserialize<BaseMessage>(data);
}

[MessagePackObject]
public class TextMessage(string text): BaseMessage
{
    [Key(0)]
    public string Text { get; init; } = text;
}

[MessagePackObject]
public class UserInfoMessage(string name): BaseMessage
{
    [Key(0)]
    public string Name { get; init; } = name;
}

[MessagePackObject]
public class SystemMessage(SystemMessage.SysMsgType type): BaseMessage
{
    [Key(0)]
    public SystemMessage.SysMsgType Type { get; init; } = type;

    public enum SysMsgType
    {
        Joined,
        Left
    }
}

[MessagePackObject]
public class BLOBMessage(byte[] data, string filename): BaseMessage
{
    [Key(0)]
    public byte[] Data { get; init; } = data;
    [Key(1)]
    public string Filename { get; init; } = filename;
}

[MessagePackObject]
public class RetransmittedMessage(string originalSender, BaseMessage msg): BaseMessage
{
    [Key(0)]
    public string OriginalSender { get; init; } = originalSender;
    [Key(1)]
    public BaseMessage Msg { get; init; } = msg;
}
