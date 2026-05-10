using System.Net;
using System.Text.Json;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;
using chatter_new.Messaging.Session;

namespace chatter_new_tests;

public class SessionTest
{
    [Fact]
    public void UnencryptedSessionTest()
    {
        var (client, server) = InMemoryConnection.CreatePair();
        
        using var sess1 = Session.CreateSession("foo", client);
        using var sess2 = Session.CreateSession("bar", server);
        
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
    }
    
    [Fact]
    public async Task EncryptedSessionTest()
    {
        var (client, server) = InMemoryConnection.CreatePair();
        
        var sess1t = EncryptedSession.Create(client);
        var sess2t = EncryptedSession.Create(server);
        using var sess1 = await sess1t;
        using var sess2 = await sess2t;
        
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
    }
}