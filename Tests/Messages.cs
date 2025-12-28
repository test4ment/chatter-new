using chatter_new.Messaging;

namespace chatter_new_tests;

public class MessagesTest
{
    [Fact]
    public void MessageSerialize()
    {
        BaseMessage msg = new TextMessage("Hello world");

        var ser = msg.Serialize();
        
        Assert.Equal("{\"Text\":\"Hello world\",\"Type\":\"TextMessage\"}", ser);
    }

    [Fact]
    public void MessageNaming()
    {
        var msgTypes = new BaseMessage[]
        {
            new TextMessage("test"),
            new SystemBaseMessage(SystemBaseMessage.SysMsgType.Joined),
            new UserInfoBaseMessage("nickname")
        };
        var msgNames = new string[]
        {
            nameof(TextMessage),
            nameof(SystemBaseMessage),
            nameof(UserInfoBaseMessage),
        };

        foreach (var (msgType, msgName) in msgTypes.Zip(msgNames))
        {
            Assert.Equal(msgType.Type, msgName);
        }
    }
}