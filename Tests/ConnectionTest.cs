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
        
        var clientname = "Alice";
        var servername = "Bob";
        
        client.Send(clientname.Encode());
        var data = server!.Receive();
        while (data.Length == 0)
            data = server.Receive();
        var received_client = data.Decode();
        
        server.Send(servername.Encode());
        data = client.Receive();
        while (data.Length == 0)
            data = server.Receive();
        var received_server = data.Decode();
        
        Assert.Equal(clientname, received_client);
        Assert.Equal(servername, received_server);
    }

    [Fact]
    public void MultipleConnections()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50003);
        var serverConnections = new List<SocketConnection>();
        var t = new Thread(o =>
        {
            while (serverConnections.Count < 3)
                serverConnections.Add(SocketConnection.ListenAndAwaitClient(addr));
        });
        t.Start();
        using var client1 = SocketConnection.ConnectTo(addr);
        using var client2 = SocketConnection.ConnectTo(addr);
        using var client3 = SocketConnection.ConnectTo(addr);
        t.Join();
        
        Assert.Equal(3, serverConnections.Count);
        
        client1.Send("c1".Encode());
        client2.Send("c2".Encode());
        client3.Send("c3".Encode());

        foreach (var client in serverConnections)
        {
            var recv = client.Receive().Decode();
            client.Send($"Hi {recv}!".Encode());
        }
        
        Assert.Equal("Hi c1!", client1.Receive().Decode());
        Assert.Equal("Hi c2!", client2.Receive().Decode());
        Assert.Equal("Hi c3!", client3.Receive().Decode());
    }
    
    [Fact]
    public void MultipleConnectionsMethod()
    {
        var addr = new IPEndPoint(IPAddress.Loopback, 50004);
        List<SocketConnection> serverConnections = null!;
        var t = new Thread(o =>
        {
            serverConnections = new List<SocketConnection>(
                SocketConnection.ListenAndAwaitClients(addr, TimeSpan.FromSeconds(5)));
        });
        t.Start();
        using var client1 = SocketConnection.ConnectTo(addr);
        using var client2 = SocketConnection.ConnectTo(addr);
        using var client3 = SocketConnection.ConnectTo(addr);
        t.Join();
        
        Assert.Equal(3, serverConnections.Count);
        
        client1.Send("c1".Encode());
        client2.Send("c2".Encode());
        client3.Send("c3".Encode());

        foreach (var connectedClient in serverConnections)
        {
            var recv = connectedClient.Receive().Decode();
            connectedClient.Send($"Hi {recv}!".Encode());
        }
        
        Assert.Equal("Hi c1!", client1.Receive().Decode());
        Assert.Equal("Hi c2!", client2.Receive().Decode());
        Assert.Equal("Hi c3!", client3.Receive().Decode());
    }
}
