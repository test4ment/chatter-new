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

    public void Send(byte[] data)
    {
        another.FillData(data);
    }

    public void Send(byte[] data, int offset, int length) 
        => another.FillData(data[offset..(offset + length)]);
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
    public Task SendAsync(byte[] data)
    {
        try
        {
            Send(data);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public Task SendAsync(byte[] data, int offset, int length)
    {
        try
        {
            Send(data, offset, length);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public Task<int> ReceiveAsync(byte[] buffer)
    {
        try
        {
            return Task.FromResult(Receive(buffer));
        }
        catch (Exception exception)
        {
            return Task.FromException<int>(exception);
        }
    }
}