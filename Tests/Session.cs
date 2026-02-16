using System.Net;
using System.Text.Json;
using chatter_new.Messaging;
using chatter_new.Messaging.Messages;
using chatter_new.Messaging.Session;

namespace chatter_new_tests;

public class SessionTest
{
    [Fact]
    public void UnencryptedSessionTest()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50000);
        Session sess1 = null!;
        var t = new Thread(() => 
            sess1 = Session.CreateSession(
                "foo", SocketConnection.ListenAndAwaitClient(addr)));
        t.Start();
        using var sess2 = Session.CreateSession("bar", SocketConnection.ConnectTo(addr));
        t.Join();
        
        sess1.CheckForIncoming();
        sess2.CheckForIncoming();
        
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));

        int called = 0;
        sess2.OnReceive += (sender, s) => {
            called++;
            Assert.True(s is TextMessage msg);
            Assert.Equal("text", ((TextMessage)s).Text);
        };
        
        sess2.CheckForIncoming();
        
        Assert.Equal(3, called);
        
        sess1.Dispose();
    }
    
    [Fact]
    public void EncryptedSessionTest()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50010);
        EncryptedSession sess1 = null!;
        var t = new Thread(() => 
            sess1 = EncryptedSession.Create(
                SocketConnection.ListenAndAwaitClient(addr), "foo"));
        t.Start();
        using var sess2 = EncryptedSession.Create(SocketConnection.ConnectTo(addr), "bar");
        t.Join();
        
        sess1.CheckForIncoming();
        sess2.CheckForIncoming();
        
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));
        sess1.SendMessage(new TextMessage("text"));

        int called = 0;
        sess2.OnReceive += (sender, s) => {
            called++;
            Assert.True(s is TextMessage msg);
            Assert.Equal("text", ((TextMessage)s).Text);
        };
        
        sess2.CheckForIncoming();
        
        Assert.Equal(3, called);
        
        sess1.Dispose();
    }
}