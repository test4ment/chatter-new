using MessagePack;

namespace chatter_messages;

[MessagePackObject]
internal record MessageMetadata
{
    [Key(0)]
    public int ContentSize { get; init; }
    [Key(1)]
    public bool TrackProgress { get; init; }
    [Key(2)]
    public int Num { get; init; }

    public byte[] Serialize() 
        => MessagePackSerializer.Serialize(this);
}

public struct Progress
{
    public int Current { get; init; }
    public int Total { get; init; }
    public int Num { get; init; }
}
