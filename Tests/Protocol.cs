using System.Buffers;
using System.Diagnostics;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;

namespace chatter_new_tests;

public class ProtocolTests
{
    [Fact]
    public async Task ProtocolReadWrite()
    {
        var (a, b) = InMemoryConnection.CreatePair();
        var protoa = new Protocol(a);
        var protob = new Protocol(b);
        
        var data = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        
        await protoa.Send(data, TestContext.Current.CancellationToken);
        await protob.Receive(TestContext.Current.CancellationToken);

        var tok = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        await protob.NextFrame(
            frame => Assert.Equal(data, frame.ToArray()), 
            tok
            );
        await protob.NextFrame(
            _ => throw new UnreachableException(),
            tok
        );
    }

    [Fact]
    public async Task ProtocolReadWriteMultiPackets()
    {
        var (a, b) = InMemoryConnection.CreatePair();
        var protoa = new Protocol(a);
        var protob = new Protocol(b);
        
        var data = Enumerable.Range(0, 3)
            .Select(i => Enumerable.Range(16 * i, 16).Select(i1 => (byte)i1).ToArray())
            .ToArray();

        foreach (var d in data)
            await protoa.Send(d, TestContext.Current.CancellationToken);
        
        await protob.Receive(TestContext.Current.CancellationToken);
        
        var res = await protob.GetFrameCopies(TestContext.Current.CancellationToken);

        Assert.NotNull(res);
        Assert.Equal(3, res.Count);
        
        foreach (var (first, second) in res.Zip(data))
            Assert.Equal(first.ToArray(), second);
    }
}
