using System.Net;
using System.Net.Sockets;
using System.Text;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class ConnectionTest
{
    [Fact]
    public void ClientsTalk()
    {
        using var sock_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var sock_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var addr = new IPEndPoint(IPAddress.Loopback, 0);
        
        sock_server.Bind(addr);
        sock_server.Listen(1);
        sock_client.Connect(sock_server.LocalEndPoint!);
        var client = new SocketConnection(sock_client);
        using var client_remote = sock_server.Accept();
        var server = new SocketConnection(client_remote); 

        const string msg = "Hello server!";
        const string msg_response = "Ack! Hello client!";
        
        client.Send(Encoding.ASCII.GetBytes(msg));
        var received_server = server.Receive();
        server.Send(Encoding.ASCII.GetBytes(msg_response));
        var received_client = client.Receive();
        
        Assert.Equal(msg, Encoding.ASCII.GetString(received_server));
        Assert.Equal(msg_response, Encoding.ASCII.GetString(received_client));
    }

    [Fact]
    public void ClientNameExchange()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50002);
        SocketConnection server = null!;
        var t = new Thread(o => server = SocketConnection.ListenAndAwaitClient(addr));
        t.Start();
        using var client = SocketConnection.ConnectTo(addr);
        t.Join();
        
        var clientname = new BytesContainer("Alice");
        var servername = new BytesContainer("Bob");
        
        client.Send(clientname.GetBytes());
        var data = server!.Receive();
        while (data.Length == 0)
            data = server.Receive();
        var received_client = new BytesContainer(data);
        
        server.Send(servername.GetBytes());
        data = client.Receive();
        while (data.Length == 0)
            data = server.Receive();
        var received_server = new BytesContainer(data);
        
        Assert.Equal(clientname.ToString(), received_client.ToString());
        Assert.Equal(servername.ToString(), received_server.ToString());
    }
}
