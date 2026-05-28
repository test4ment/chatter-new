using chatter_new.Messaging.Connection;

namespace chatter_new.Messaging;

public class Protocol(IConnectionAsync connection)
{
    private SemaphoreSlim semaphore = new(1, 1);
    
    public async Task Send(byte[] data, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try {
            // await send size
            await connection.SendAsync(data, cancellationToken);
        }
        finally {
            semaphore.Release();
        }
    }
    
    public void Recv()
    {
        throw new NotImplementedException();
    }
}

// TODO: Enforce a Maximum Message Size; Define a constant (e.g., MAX_MESSAGE_SIZE = 1024 * 1024 // 1MB).
// TODO: Validate leftToReceive (or messageLength) as soon as it is parsed. If it exceeds the limit or is negative, immediately terminate the connection.
// TODO: Implement Read Timeouts; Introduce a CancellationTokenSource with a timeout (e.g., 5–10 seconds) to your read loops.
// TODO: Disconnect clients that send headers but stall on sending the actual message body (Slowloris defense).
// TODO: Adopt a "Fail-Fast" Disconnect Policy
// TODO: Configure Pipe High/Low Water Marks (If moving to IO.Pipelines); set a PipeOptions.PauseWriterThreshold
//  to limit how many unparsed bytes can sit in memory before the socket stops reading, preventing RAM exhaustion from floods.
