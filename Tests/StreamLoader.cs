using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Datastream;
using chatter_new.Messaging.Messages;

namespace chatter_new_tests;

public class StreamLoader
{
    [Fact]
    public async Task LoadAndTransferStreamOfData()
    {
        var (a, b) = InMemoryConnection.CreatePair();
        var sendProto = new Protocol(a);
        var recvProto = new Protocol(b);

        var payload = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        using var ms = new MemoryStream(payload);

        var chunks = new List<ChunkedBlob>();
        await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "test.bin", chunkSize: 64, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal(4, chunks.Count);

        foreach (var chunk in chunks)
            await sendProto.Send(chunk.Serialize().Encode(), TestContext.Current.CancellationToken);

        var reassembled = new byte[payload.Length];
        var offset = 0;
        foreach (var _ in chunks)
        {
            var frame = await recvProto.ReadNextFrameAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(frame);
            var msg = System.Text.Json.JsonSerializer.Deserialize<ChunkedBlob>(frame.Decode())!;
            Buffer.BlockCopy(msg.Blob.Data, 0, reassembled, offset, msg.Blob.Data.Length);
            offset += msg.Blob.Data.Length;
        }

        Assert.Equal(payload, reassembled);
    }

    [Fact]
    public async Task ChunkStream_ProducesCorrectNumberOfChunks()
    {
        var data = new byte[100];
        Random.Shared.NextBytes(data);
        using var ms = new MemoryStream(data);

        var chunks = new List<ChunkedBlob>();
        await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "test.bin", chunkSize: 30, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal(4, chunks.Count);
        Assert.Equal(0, chunks[0].ChunkIndex);
        Assert.Equal(4, chunks[0].TotalChunks);
        Assert.Equal("test.bin", chunks[0].Blob.Filename);
    }

    [Fact]
    public async Task ChunkStream_ReassemblesOriginalData()
    {
        var data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();
        using var ms = new MemoryStream(data);

        var chunks = new List<ChunkedBlob>();
        await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "file.dat", chunkSize: 256, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        var reassembled = new byte[data.Length];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            Buffer.BlockCopy(chunk.Blob.Data, 0, reassembled, offset, chunk.Blob.Data.Length);
            offset += chunk.Blob.Data.Length;
        }

        Assert.Equal(data, reassembled);
    }

    [Fact]
    public async Task ChunkStream_EmptyStreamProducesNoChunks()
    {
        using var ms = new MemoryStream([]);

        var chunks = new List<ChunkedBlob>();
        await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "empty.bin", chunkSize: 64, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkStream_InvalidChunkSizeThrows()
    {
        using var ms = new MemoryStream(new byte[10]);

        var enumerator = DataChunker.ChunkStreamAsync(ms, "f", chunkSize: 0, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => enumerator.MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task ChunkStream_CancellationStopsIteration()
    {
        var data = new byte[1024];
        Random.Shared.NextBytes(data);
        using var ms = new MemoryStream(data);

        using var cts = new CancellationTokenSource();
        var chunks = new List<ChunkedBlob>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "cancel.bin", chunkSize: 64, cts.Token))
            {
                chunks.Add(chunk);
                if (chunks.Count == 2)
                    cts.Cancel();
            }
        });

        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public async Task ChunkStream_ProgressTracking()
    {
        var data = new byte[100];
        using var ms = new MemoryStream(data);

        var chunks = new List<ChunkedBlob>();
        await foreach (var chunk in DataChunker.ChunkStreamAsync(ms, "p.bin", chunkSize: 25, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal(4, chunks.Count);

        Assert.Equal(new Progress { Current = 1, Total = 4, Num = 0 }, chunks[0].Progress);
        Assert.Equal(new Progress { Current = 2, Total = 4, Num = 1 }, chunks[1].Progress);
        Assert.Equal(new Progress { Current = 3, Total = 4, Num = 2 }, chunks[2].Progress);
        Assert.Equal(new Progress { Current = 4, Total = 4, Num = 3 }, chunks[3].Progress);
    }
}