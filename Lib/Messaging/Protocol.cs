using System.IO.Pipelines;
using chatter_new.Messaging.Connection;

namespace chatter_new.Messaging;

public class Protocol(IConnectionAsync connection)
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private Pipe recvPipe = new();
    private const int bufferSize = 4096;
    
    public async Task Send(byte[] data, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try {
            // await send size
            await connection.SendAsync(data.Length.Encode(), cancellationToken);
            await connection.SendAsync(data, cancellationToken);
        }
        finally {
            semaphore.Release();
        }
    }
    
    public async Task Recv(CancellationToken ct = default)
    {
        var writer = recvPipe.Writer;
        
        var recv = await connection.ReceiveAsync(writer.GetMemory(bufferSize), ct);
        writer.Advance(recv);
        
        await writer.FlushAsync(ct);
    }

    public async Task CreateFrames(CancellationToken ct = default)
    {
        var reader = recvPipe.Reader;
        
        var r = await reader.ReadAsync(ct);
        var b = r.Buffer;

        while (true)
        {
            if (b.Length < sizeof(int)) break;
            var toRead = b.Slice(0, sizeof(int));
            if (b.Length - sizeof(int) < toRead)
        }
    }
}

// public class Frame;

// TODO: Enforce a Maximum Message Size; Define a constant (e.g., MAX_MESSAGE_SIZE = 1024 * 1024 // 1MB).
// TODO: Validate leftToReceive (or messageLength) as soon as it is parsed. If it exceeds the limit or is negative, immediately terminate the connection.
// TODO: Implement Read Timeouts; Introduce a CancellationTokenSource with a timeout (e.g., 5–10 seconds) to your read loops.
// TODO: Disconnect clients that send headers but stall on sending the actual message body (Slowloris defense).
// TODO: Adopt a "Fail-Fast" Disconnect Policy
// TODO: Configure Pipe High/Low Water Marks (If moving to IO.Pipelines); set a PipeOptions.PauseWriterThreshold
//  to limit how many unparsed bytes can sit in memory before the socket stops reading, preventing RAM exhaustion from floods.
