using System.Buffers;
using System.IO.Pipelines;
using chatter_new.Messaging.Connection;

namespace chatter_new.Messaging;

public class Protocol(IConnectionAsync connection)
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private Pipe recvPipe = new();
    private const int bufferSize = 4096;
    private int? awaitingPacketSize = null;
    
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
    
    public async Task<int> Receive(CancellationToken ct = default)
    {
        var writer = recvPipe.Writer;
        
        var recv = await connection.ReceiveAsync(writer.GetMemory(bufferSize), ct);
        
        if (recv == 0)
            await writer.CompleteAsync();
        
        writer.Advance(recv);
        
        await writer.FlushAsync(ct);
        return recv;
    }

    public async Task<IList<ReadOnlySequence<byte>>> CreateFrames(CancellationToken ct = default)
    {
        var reader = recvPipe.Reader;
        
        var r = await reader.ReadAsync(ct);
        var b = r.Buffer;

        var res = new List<ReadOnlySequence<byte>>();
        while (true)
        {
            if(ct.IsCancellationRequested) break;
            
            if (!awaitingPacketSize.HasValue) // hasnt gotten len
            {
                if (b.Length < sizeof(int)) break;
                
                awaitingPacketSize = b.Slice(0, sizeof(int)).DecodeInt();
                b = b.Slice(sizeof(int));
            }
            
            if (b.Length < awaitingPacketSize.Value) break;
                
            var packet = b.Slice(0, awaitingPacketSize.Value);
            res.Add(packet);
                
            b = b.Slice(awaitingPacketSize.Value);
            awaitingPacketSize = null;
        }
        
        return res;
    }
}


// TODO: Enforce a Maximum Message Size; Define a constant (e.g., MAX_MESSAGE_SIZE = 1024 * 1024 // 1MB).
// TODO: Validate leftToReceive (or messageLength) as soon as it is parsed. If it exceeds the limit or is negative, immediately terminate the connection.
// TODO: Implement Read Timeouts; Introduce a CancellationTokenSource with a timeout (e.g., 5–10 seconds) to your read loops.
// TODO: Disconnect clients that send headers but stall on sending the actual message body (Slowloris defense).
// TODO: Adopt a "Fail-Fast" Disconnect Policy
// TODO: Configure Pipe High/Low Water Marks (If moving to IO.Pipelines); set a PipeOptions.PauseWriterThreshold
//  to limit how many unparsed bytes can sit in memory before the socket stops reading, preventing RAM exhaustion from floods.
