using System.Buffers;
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
        
        var res = await protob.CreateFrames(TestContext.Current.CancellationToken);
        
        Assert.Single(res);
        Assert.Equal(res[0].ToArray(), data);
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
        
        var res = await protob.CreateFrames(TestContext.Current.CancellationToken);
        
        Assert.Equal(3, res.Count);
        
        foreach (var (first, second) in res.Zip(data))
            Assert.Equal(first.ToArray(), second);
    }
}
