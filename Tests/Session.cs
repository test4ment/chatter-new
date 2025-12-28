using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using chatter_new.Messaging;
using chatter_new.Messaging.Session;

namespace chatter_new_tests;

public class UnencryptedSessionTest
{
    [Fact]
    public void TestTalk()
    {
        // var sess1 = UnencryptedSession.CreateSession("a", SocketConnection.ListenAndAwaitClient());
        // var sess2 = UnencryptedSession.CreateSession();
    }

    [Fact]
    public void EncryptedSessionTest()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50001);
        EncryptedSession sess1 = null!;
        var t = new Thread(() => 
            sess1 = EncryptedSession.Create(
                SocketConnection.ListenAndAwaitClient(addr), "foo"));
        t.Start();
        var sess2 = EncryptedSession.Create(SocketConnection.ConnectTo(addr), "bar");
        t.Join();
        
        sess1.CheckForIncoming();
        sess2.CheckForIncoming();
        
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));

        int called = 0;
        sess2.OnReceive += (sender, s) =>
        {
            called++;
            var json = JsonDocument.Parse(s);
            var msg = json.Deserialize<TextMessage>();
            Assert.Equal("text", msg!.Text);
        };
        
        sess2.CheckForIncoming();
        
        Assert.Equal(3, called);
    }
}