using System.Text.Json;
using chatter_new.Messaging.Messages;

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
}