using System.Net;
using System.Net.Sockets;

namespace chatter_new.Messaging;

public interface IConnection
{
    public void Send(byte[] data);
    public byte[] Receive();
    public void Send(BytesContainer bytesContainer) => Send(bytesContainer.GetBytes());
}

public class SocketConnection(Socket sock) : IConnection, IDisposable
{
    private readonly byte[] buffer = new byte[1024 * 4];
    public void Send(byte[] data)
    {
        sock.Send(data);
    }

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

    public static SocketConnection TryAwaitClient(IPEndPoint address)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        sock.Dispose();
    }
}
