using System.Text.Json;
using System.Text.Json.Serialization;

namespace chatter_new.Messaging;

public abstract class BaseMessage
{
    public string Type => GetType().Name;

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, this.GetType(), Default);
    }

    protected static JsonSerializerOptions Default = new JsonSerializerOptions() { IncludeFields = true };
}

[method: JsonConstructor]
public class TextMessage(string text): BaseMessage
{
    public string Text { get; init; } = text;
}

[method: JsonConstructor]
public class UserInfoBaseMessage(string name): BaseMessage
{
    public string Name { get; init; } = name;
}

[method: JsonConstructor]
public class SystemBaseMessage(SystemBaseMessage.SysMsgType type): BaseMessage
{
    public SysMsgType SystemType { get; init; } = type;

    public enum SysMsgType
    {
        Joined,
        Left
    }
}
