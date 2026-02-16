using System.Net;
using System.Net.Sockets;

namespace chatter_new.Messaging;

public interface IConnection
{
    public void Send(byte[] data);
    public byte[] Receive();
}

public class SocketConnection(Socket sock) : IConnection, IDisposable
{
    public const int KiB = 1024;
    public const int MiB = KiB * KiB;
    private readonly byte[] buffer = new byte[2 * MiB];
    public void Send(byte[] data) => sock.Send(data);

    public byte[] Receive()
    {
        if (sock.Available > 0)
        {
            var size = sock.Receive(buffer);
            return buffer[..size];
        }
        return Array.Empty<byte>();
    }

    public static SocketConnection ConnectTo(IPEndPoint address)
    {
        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Connect(address);
        return new SocketConnection(sock);
    }

    public static SocketConnection ListenAndAwaitClient(IPEndPoint address)
    {
        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Bind(address);
        sock.Listen(1);
        return new SocketConnection(sock.Accept());
    }
    public static IEnumerable<SocketConnection> ListenAndAwaitClients(IPEndPoint address, TimeSpan timeout)
    {
        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Bind(address);
        sock.Listen();
        var connections = new List<SocketConnection>();
        var waitUntil = DateTime.Now.Add(timeout);
        while (DateTime.Now < waitUntil) {
            if (sock.Poll(waitUntil - DateTime.Now, SelectMode.SelectRead)) {
                connections.Add(new SocketConnection(sock.Accept()));
            }    
        }
        return connections;
    }

    public static SocketConnection TryAwaitClient(IPEndPoint address)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        sock.Dispose();
    }
}
