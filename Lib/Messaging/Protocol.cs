using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using chatter_new.Messaging.Connection;

namespace chatter_new.Messaging;

public class Protocol(IConnectionAsync connection) : IAsyncDisposable
{
    private const int ReadChunkSize = 4096;

    private readonly SemaphoreSlim sendGate = new(1, 1);
    private readonly SemaphoreSlim receiveGate = new(1, 1);
    private readonly SemaphoreSlim readGate = new(1, 1);
    private readonly Pipe recvPipe = new(new PipeOptions(
        pauseWriterThreshold: 1 << 20,      // 1 MiB
        resumeWriterThreshold: 1 << 19));   // 0.5 MiB

    private bool endOfStream;

    public async Task Send(byte[] data, CancellationToken cancellationToken = default)
    {
        await sendGate.WaitAsync(cancellationToken);
        try
        {
            await SendAllAsync(data.Length.Encode(), cancellationToken);
            await SendAllAsync(data, cancellationToken);
        }
        finally
        {
            sendGate.Release();
        }
    }

    private async Task SendAllAsync(byte[] data, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < data.Length)
        {
            var sent = await connection.SendAsync(data, offset, data.Length - offset, cancellationToken);
            if (sent <= 0)
                throw new IOException("Connection closed before the frame was fully sent.");
            offset += sent;
        }
    }

    public async Task<int> Receive(CancellationToken ct = default)
    {
        if (endOfStream) return 0;

        await receiveGate.WaitAsync(ct);
        try
        {
            if (endOfStream) return 0;

            var writer = recvPipe.Writer;
            var received = await connection.ReceiveAsync(writer.GetMemory(ReadChunkSize), ct);

            if (received == 0)
            {
                endOfStream = true;
                await writer.CompleteAsync();
                return 0;
            }

            writer.Advance(received);
            await writer.FlushAsync(ct);
            return received;
        }
        finally
        {
            receiveGate.Release();
        }
    }

    public async Task<byte[]?> ReadNextFrameAsync(CancellationToken ct = default)
    {
        await readGate.WaitAsync(ct);
        try
        {
            var reader = recvPipe.Reader;

            while (true)
            {
                if (reader.TryRead(out var r))
                {
                    var buffer = r.Buffer;

                    if (FrameDecoder.TryReadFrame(
                            buffer,
                            out var frame,
                            out var consumed,
                            out var examined))
                    {
                        var result = frame.ToArray();
                        reader.AdvanceTo(consumed, examined);
                        return result;
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (r.IsCompleted || endOfStream)
                        return null;
                }
                else if (endOfStream)
                {
                    return null;
                }

                await Receive(ct);
            }
        }
        finally
        {
            readGate.Release();
        }
    }

    public bool TryReadNextFrame(out byte[]? frame)
    {
        var reader = recvPipe.Reader;

        if (!reader.TryRead(out var r) || r.Buffer.IsEmpty)
        {
            frame = null;
            return false;
        }

        var buffer = r.Buffer;
        if (FrameDecoder.TryReadFrame(buffer, out var slice, out var consumed, out var examined))
        {
            reader.AdvanceTo(consumed, examined);
            frame = slice.ToArray();
            return true;
        }

        reader.AdvanceTo(buffer.Start, buffer.End);
        frame = null;
        return false;
    }

    public async IAsyncEnumerable<byte[]> ReadFramesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (true)
        {
            var frame = await ReadNextFrameAsync(ct);
            if (frame is null)
                yield break;

            yield return frame;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await recvPipe.Writer.CompleteAsync();
        recvPipe.Reader.Complete();
        sendGate.Dispose();
        receiveGate.Dispose();
        readGate.Dispose();
    }
}