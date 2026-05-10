using System.Net;
using System.Net.Sockets;

namespace chatter_new.Messaging.Connection;

public interface IConnection
{
    public int Available { get; }
    public void Send(byte[] data); // TODO: return sent bytes count
    public void Send(byte[] data, int offset, int length);
    public int Receive(byte[] buffer);
    public int Receive(byte[] buffer, int offset, int count);
}

public interface IConnectionAsync: IConnection
{
    public Task SendAsync(byte[] data);
    public Task SendAsync(byte[] data, int offset, int length);
    public Task<int> ReceiveAsync(byte[] buffer);
}

public class SocketConnection(Socket sock) : IConnectionAsync, IConnection, IDisposable
{
    public int Available => sock.Available;
    public void Send(byte[] data) => sock.Send(data);
    public void Send(byte[] data, int offset, int length) 
        => sock.Send(data, offset, length, SocketFlags.None);
    public int Receive(byte[] buffer) 
        => Available > 0 ? sock.Receive(buffer) : 0;

    public int Receive(byte[] buffer, int offset, int count) 
        => Available > 0 ? sock.Receive(buffer, offset, count, SocketFlags.None) : 0;

    public async Task SendAsync(byte[] data) => await sock.SendAsync(data);
    public async Task SendAsync(byte[] data, int offset, int length) => await sock.SendAsync(data[offset..(offset + length)]);
    public async Task<int> ReceiveAsync(byte[] buffer) => await sock.ReceiveAsync(buffer);

    public static async Task<SocketConnection> ConnectTo(IPEndPoint address)
    {
        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try {
            await sock.ConnectAsync(address);
            return new SocketConnection(sock);
        }
        catch {
            sock.Dispose();
            throw;
        }
    }
    public static async Task<SocketConnection> ListenAndAwaitClient(IPEndPoint address)
    {
        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Bind(address);
        sock.Listen(1);
        return new SocketConnection(await sock.AcceptAsync());
    }
    public static async IAsyncEnumerable<SocketConnection> ListenAndAwaitClients(IPEndPoint address)
    {
        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Bind(address);
        sock.Listen();
        
        while (true) {
            yield return new SocketConnection(await sock.AcceptAsync());
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        sock.Dispose();
    }
}
