using chatter_crypto;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;

namespace chatter_new_tests.ProtocolUsage;

public class HandshakeExample
{
    [Fact]
    public async Task PerformHandshake()
    {
        var (aliceConn, bobConn) = InMemoryConnection.CreatePair();
        var aliceProto = new Protocol(aliceConn);
        var bobProto = new Protocol(bobConn);

        var alice = new DHHandshake(aliceProto);
        var bob = new DHHandshake(bobProto);

        var ct = TestContext.Current.CancellationToken;
        var timed = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;

        _ = Task.Run(async () =>
        {
            while (!timed.IsCancellationRequested)
            {
                await aliceProto.Receive(timed);
            }
        }, TestContext.Current.CancellationToken);
        _ = Task.Run(async () =>
        {
            while (!timed.IsCancellationRequested)
            {
                await bobProto.Receive(timed);
            }
        }, TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(alice.Perform(ct), bob.Perform(ct));

        var msg = "hello handshake"u8.ToArray();
        var encrypted = results[0].Encrypt(msg);
        var decrypted = results[1].Decrypt(encrypted);

        Assert.Equal(msg, decrypted);
    }
}