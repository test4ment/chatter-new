namespace chatter_new.Messaging.Connection;

public class InMemoryConnection : IConnection, IConnectionAsync
{
    private List<byte> buf = new();
    private InMemoryConnection another = null!;
    private InMemoryConnection(){}

    public static (InMemoryConnection, InMemoryConnection) CreatePair()
    {
        var inmem1 = new InMemoryConnection();
        var inmem2 = new InMemoryConnection();
        inmem1.another = inmem2;
        inmem2.another = inmem1;
        
        return (inmem1, inmem2);
    }

    public int Available => buf.Count;

    public int Send(byte[] data)
    {
        another.FillData(data);
        return data.Length;
    }

    public int Send(byte[] data, int offset, int length)
    {
        another.FillData(data[offset..(offset + length)]);
        return length;
    }

    public int Receive(byte[] buffer) 
        => Receive(buffer, 0, buffer.Length);

    public int Receive(byte[] buffer, int offset, int count)
    {
        var toCopy = Math.Min(count, buf.Count);
        buf.CopyTo(0, buffer, offset, toCopy);
        buf.RemoveRange(0, toCopy);
        return toCopy;
    }

    public byte[] Receive()
    {
        var data = buf.ToArray();
        buf.Clear();
        return data;
    }

    private void FillData(byte[] data) => buf.AddRange(data);
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
        Send(data.ToArray());
        return Task.FromResult(data.Length);
    }

    public Task<int> ReceiveAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Receive(buffer));
    }

    public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Receive(buffer.ToArray()));
    }
}
