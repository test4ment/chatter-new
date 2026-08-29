using System.Buffers;
using System.Text.Json;
using chatter_crypto;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;

namespace chatter_new_tests;

public class SessionTest
{
    [Fact]
    public async Task UnencryptedSessionTest()
    {
        var (client, server) = InMemoryConnection.CreatePair();
        
        var sess1 = new Protocol(client);
        var sess2 = new Protocol(server);

        var payload = new TextMessage("text").Serialize().Encode();
        
        await sess1.Send(payload, TestContext.Current.CancellationToken);
        await sess1.Send(payload, TestContext.Current.CancellationToken);
        await sess1.Send(payload, TestContext.Current.CancellationToken);

        var read = await sess2.Receive(TestContext.Current.CancellationToken);
        
        Assert.Equal(read, (payload.Length + sizeof(int)) * 3);
        
        int called = 0;
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)).Token;
        while (await sess2.ProcessNextFrame((readOnlySequence) =>
               {
                   called++;
                   var s = readOnlySequence.ToArray().Decode();
                   var msg = JsonSerializer.Deserialize<BaseMessage>(s);
                   Assert.True(msg is TextMessage);
                   Assert.Equal("text", ((TextMessage)msg).Text);
               }, cancellationToken)) {}
        
        Assert.Equal(3, called);
    }
    
    [Fact]
    public async Task EncryptedSessionTest()
    {
        var (client, server) = InMemoryConnection.CreatePair();
        
        var sess1 = new Protocol(client);
        var sess2 = new Protocol(server);
        
        var enc = new UniversalEncryption(Array.Empty<byte>(), false);
        var payload = enc.Encrypt(new TextMessage("text").Serialize().Encode());
        await sess1.Send(payload, TestContext.Current.CancellationToken);
        await sess1.Send(payload, TestContext.Current.CancellationToken);
        await sess1.Send(payload, TestContext.Current.CancellationToken);
        
        var read = await sess2.Receive(TestContext.Current.CancellationToken);
        
        Assert.Equal(read, (payload.Length + sizeof(int)) * 3);
        
        int called = 0;
        while (await sess2.ProcessNextFrame((readOnlySequence) =>
               {
                   called++;
                   var s = enc.Decrypt(readOnlySequence.ToArray()).Decode();
                   var msg = JsonSerializer.Deserialize<BaseMessage>(s);
                   Assert.True(msg is TextMessage);
                   Assert.Equal("text", ((TextMessage)msg).Text);
               }, TestContext.Current.CancellationToken)) {}
        
        Assert.Equal(3, called);
    }
}