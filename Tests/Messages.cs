using System.Text.Json;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class MessagesTest
{
    [Fact]
    public void MessageSerialize()
    {
        BaseMessage msg = new TextMessage("Hello world");

        // var ser = msg.Serialize();
        var ser = JsonSerializer.Serialize(msg);
        var deser = JsonSerializer.Deserialize<BaseMessage>(ser);
        
        Assert.Equal(msg.GetType(), deser!.GetType());
        Assert.Equal(msg, deser);
    }

    [Fact]
    public void MessageNaming()
    {
        var msgTypes = new BaseMessage[]
        {
            new TextMessage("test"),
            new SystemMessage(SystemMessage.SysMsgType.Joined),
            new UserInfoBaseMessage("nickname")
        };
        var msgNames = new Type[]
        {
            typeof(TextMessage),
            typeof(SystemMessage),
            typeof(UserInfoBaseMessage),
        };

        foreach (var (msgType, msgName) in msgTypes.Zip(msgNames))
        {
            Assert.Equal(msgType.GetType(), msgName);
        }
    }
}