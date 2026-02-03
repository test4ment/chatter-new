namespace chatter_new.Messaging.Messages;

public record ServiceData
{
    public int ContentSize { get; init; }
    public bool TrackProgress { get; init; }
}
