using System.Text.Json;
using System.Text.Json.Serialization;

namespace chatter_new.Messaging.Messages;

[JsonDerivedType(typeof(TextMessage), "text")]
[JsonDerivedType(typeof(UserInfoMessage), "userinfo")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(BLOBMessage), "blob")]
public abstract class BaseMessage
{
    public string Serialize()
    {
        return JsonSerializer.Serialize<BaseMessage>(this, Default);
    }

    protected static JsonSerializerOptions Default = new JsonSerializerOptions()
    {
        IncludeFields = true
    };
}

[method: JsonConstructor]
public class TextMessage(string text): BaseMessage
{
    public string Text { get; init; } = text;
}

[method: JsonConstructor]
public class UserInfoMessage(string name): BaseMessage
{
    public string Name { get; init; } = name;
}

[method: JsonConstructor]
public class SystemMessage(SystemMessage.SysMsgType type): BaseMessage
{
    public SysMsgType Type { get; init; } = type;

    public enum SysMsgType
    {
        Joined,
        Left
    }
}

[method: JsonConstructor]
public class BLOBMessage(byte[] data, string filename) : BaseMessage
{
    public byte[] Data { get; init; } = data;
    public string Filename { get; init; } = filename;
}
