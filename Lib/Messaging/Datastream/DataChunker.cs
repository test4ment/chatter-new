using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using chatter_new.Messaging.Messages;

namespace chatter_new.Messaging.Datastream;

[method: JsonConstructor]
public class ChunkedBlob(BLOBMessage Blob, int ChunkIndex, int TotalChunks): BaseMessage
{
    public BLOBMessage Blob { get; init; } = Blob;
    public int ChunkIndex { get; init; } = ChunkIndex;
    public int TotalChunks { get; init; } = TotalChunks;
    public Progress Progress => new() { Current = ChunkIndex + 1, Total = TotalChunks, Num = ChunkIndex };
}

public static class DataChunker
{
    public const int DefaultChunkSize = 4096;

    public static async IAsyncEnumerable<ChunkedBlob> ChunkStreamAsync(
        Stream stream,
        string filename,
        int chunkSize = DefaultChunkSize,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var totalChunks = (int)Math.Ceiling((double)stream.Length / chunkSize);

        if (totalChunks == 0)
            yield break;

        for (var i = 0; i < totalChunks; i++)
        {
            ct.ThrowIfCancellationRequested();

            var remaining = (int)Math.Min(chunkSize, stream.Length - i * (long)chunkSize);
            var buffer = new byte[remaining];
            var read = await stream.ReadAsync(buffer.AsMemory(0, remaining), ct);

            if (read == 0)
                yield break;

            var data = read == remaining ? buffer : buffer[..read];
            yield return new ChunkedBlob(new BLOBMessage(data, filename), i, totalChunks);
        }
    }


    public static async IAsyncEnumerable<ChunkedBlob> ChunkFileAsync(
        string path,
        string? filename = null,
        int chunkSize = DefaultChunkSize,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        filename ??= Path.GetFileName(path);
        await using var stream = File.OpenRead(path);
        await foreach (var chunk in ChunkStreamAsync(stream, filename, chunkSize, ct))
            yield return chunk;
    }
}