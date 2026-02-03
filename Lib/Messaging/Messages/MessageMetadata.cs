using System.Text.Json;

namespace chatter_new.Messaging.Messages;

public record MessageMetadata
{
    public int ContentSize { get; init; }
    public bool TrackProgress { get; init; }
    public int Num { get; init; }

    public string Serialize() 
        => JsonSerializer.Serialize(this);
}

public struct Progress
{
    public int Current { get; init; }
    public int Total { get; init; }
    public int Num { get; init; }
}
