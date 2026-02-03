using System.Text.Json;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class MessagesTest
{
    [Fact]
    public void MessageRoundtrip()
    {
        BaseMessage msg = new TextMessage("Hello world");

        var ser = msg.Serialize();
        var deser = JsonSerializer.Deserialize<BaseMessage>(ser);
        
        Assert.Equal(msg.GetType(), deser!.GetType());
        Assert.True(deser is TextMessage);
        Assert.Equal(((TextMessage)msg).Text, ((TextMessage)deser).Text);
    }

    [Fact]
    public void MessageTypePreserve()
    {
        var msgTypes = new BaseMessage[]
        {
            new TextMessage("test"),
            new SystemMessage(SystemMessage.SysMsgType.Joined),
            new UserInfoMessage("nickname")
        };
        var msgNames = new Type[]
        {
            typeof(TextMessage),
            typeof(SystemMessage),
            typeof(UserInfoMessage),
        };

        foreach (var (msgInstance, msgType) in msgTypes.Zip(msgNames))
        {
            Assert.True(msgInstance.GetType() == msgType);
        }
    }
}