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

        var frame = await protob.ReadNextFrameAsync(TestContext.Current.CancellationToken);
        Assert.Equal(data, frame);

        var tok = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => protob.ReadNextFrameAsync(tok));
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

        var received = new List<byte[]>();
        for (var i = 0; i < data.Length; i++)
        {
            var frame = await protob.ReadNextFrameAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(frame);
            received.Add(frame!);
        }

        foreach (var (first, second) in received.Zip(data))
            Assert.Equal(first, second);
    }

    [Fact]
    public async Task FrameReassembledFromChunks()
    {
        var connection = new StubConnection();
        var proto = new Protocol(connection);

        var payload = Enumerable.Range(1, 64).Select(i => (byte)i).ToArray();
        var frame = payload.Length.Encode().Concat(payload).ToArray();

        foreach (var chunk in frame.Chunk(7))
            connection.Feed(chunk);

        var result = await proto.ReadNextFrameAsync(TestContext.Current.CancellationToken);
        Assert.Equal(payload, result);

        connection.Close();
        Assert.Null(await proto.ReadNextFrameAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LargeMessageDoesNotDeadlock()
    {
        var (a, b) = InMemoryConnection.CreatePair();
        var protoa = new Protocol(a);
        var protob = new Protocol(b);

        var payload = new byte[2 * 1024 * 1024];
        Random.Shared.NextBytes(payload);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var ftask = Task.Run(async () => 
            await protob.ReadNextFrameAsync(TestContext.Current.CancellationToken), timeout.Token);
        
        await protoa.Send(payload, TestContext.Current.CancellationToken);
        
        var frame = await ftask;

        Assert.Equal(payload, frame);
    }

    [Fact]
    public async Task ReadFramesAsyncYieldsBufferedFramesThenEnds()
    {
        var connection = new StubConnection();
        var proto = new Protocol(connection);

        var first = "first"u8.ToArray();
        var second = "second"u8.ToArray();
        connection.Feed(first.Length.Encode().Concat(first).ToArray());
        connection.Feed(second.Length.Encode().Concat(second).ToArray());
        connection.Close();

        var received = new List<byte[]>();
        await foreach (var frame in proto.ReadFramesAsync(TestContext.Current.CancellationToken))
            received.Add(frame);

        Assert.Equal(new byte[][] { first, second }, received);
    }
}

file sealed class StubConnection : IConnectionAsync
{
    private readonly Queue<byte> queue = new();
    private readonly SemaphoreSlim signal = new(0);

    public int Available => queue.Count;
    public bool Closed { get; private set; }

    public void Feed(byte[] data)
    {
        foreach (var b in data)
            queue.Enqueue(b);
        signal.Release(data.Length);
    }

    public void Close() => Closed = true;

    public int Send(byte[] data) => data.Length;
    public int Send(byte[] data, int offset, int length) => length;

    public Task<int> SendAsync(byte[] data, CancellationToken ct = default) => Task.FromResult(data.Length);
    public Task<int> SendAsync(byte[] data, int offset, int length, CancellationToken ct = default) => Task.FromResult(length);
    public Task<int> SendAsync(Memory<byte> data, CancellationToken ct = default) => Task.FromResult(data.Length);

    public int Receive(byte[] buffer) => throw new NotSupportedException();
    public int Receive(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public Task<int> ReceiveAsync(byte[] buffer, CancellationToken ct = default)
        => ReceiveAsync(buffer.AsMemory(), ct);

    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (Closed && queue.Count == 0)
            return 0;

        while (queue.Count == 0)
        {
            if (Closed)
                return 0;
            await signal.WaitAsync(ct);
        }

        var count = Math.Min(buffer.Length, queue.Count);
        for (var i = 0; i < count; i++)
            buffer.Span[i] = queue.Dequeue();
        return count;
    }
}