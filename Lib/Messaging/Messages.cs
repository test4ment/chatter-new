using System.Text.Json;
using System.Text.Json.Serialization;

namespace chatter_new.Messaging;

public interface IMessage
{
    public string Type { get; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, this.GetType(), Default);
    }

    protected static JsonSerializerOptions Default = new JsonSerializerOptions() { IncludeFields = true };
}

[method: JsonConstructor]
public record TextMessage([property: JsonInclude] string text): IMessage
{
    public string Type { get; } = nameof(TextMessage);
}

[method: JsonConstructor]
public record UserInfoMessage(string name): IMessage
{
    public string Type { get; } = nameof(UserInfoMessage);
}

[method: JsonConstructor]
public record SystemMessage(SystemMessage.SysMsgType type): IMessage
{
    public string Type { get; } = nameof(SystemMessage);

    public enum SysMsgType
    {
        Joined,
        Left
    }
}
