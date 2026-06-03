using System.Collections.Concurrent;

namespace chatter_new.Messaging.Connection;

public class InMemoryConnection : IConnection, IConnectionAsync
{
    private readonly ConcurrentQueue<byte> _queue = new();
    
    private readonly SemaphoreSlim _signal = new(0);
    
    private InMemoryConnection another = null!;
    
    private InMemoryConnection() {}

    public static (InMemoryConnection, InMemoryConnection) CreatePair()
    {
        var inmem1 = new InMemoryConnection();
        var inmem2 = new InMemoryConnection();
        inmem1.another = inmem2;
        inmem2.another = inmem1;
        
        return (inmem1, inmem2);
    }

    public int Available => _queue.Count;

    public int Send(byte[] data)
    {
        another.FillData(data);
        return data.Length;
    }

    public int Send(byte[] data, int offset, int length)
    {
        another.FillData(data.AsSpan(offset, length));
        return length;
    }

    public int Receive(byte[] buffer) 
        => Receive(buffer, 0, buffer.Length);

    public int Receive(byte[] buffer, int offset, int count)
    {
        int copied = 0;
        while (copied < count && _queue.TryDequeue(out byte b))
        {
            buffer[offset + copied] = b;
            copied++;
            
            _signal.Wait(0); 
        }
        return copied;
    }

    public byte[] Receive()
    {
        var list = new List<byte>();
        while (_queue.TryDequeue(out byte b))
        {
            list.Add(b);
            _signal.Wait(0);
        }
        return list.ToArray();
    }

    private void FillData(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;

        foreach (var b in data)
        {
            _queue.Enqueue(b);
        }

        _signal.Release(data.Length);
    }

    public Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        Send(data);
        return Task.FromResult(data.Length);
    }

    public Task<int> SendAsync(byte[] data, int offset, int length, CancellationToken cancellationToken = default)
    {
        Send(data, offset, length);
        return Task.FromResult(length);
    }

    public Task<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken = default)
    {
        another.FillData(data.Span);
        return Task.FromResult(data.Length);
    }

    public async Task<int> ReceiveAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        return await ReceiveAsync(buffer.AsMemory(), cancellationToken);
    }

    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty) return 0;

        await _signal.WaitAsync(cancellationToken);

        int copied = 0;
        var span = buffer.Span;

        if (_queue.TryDequeue(out byte firstByte))
        {
            span[copied++] = firstByte;
        }

        while (copied < buffer.Length && _queue.TryDequeue(out byte nextByte)) {
            span[copied++] = nextByte;
        }
        
        var extraTokensToConsume = copied - 1;
        for (var i = 0; i < extraTokensToConsume; i++)
        {
            await _signal.WaitAsync(0, cancellationToken);
        }

        return copied;
    }
}