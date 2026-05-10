using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;

namespace chatter_new_tests;

public class ConnectionTest
{
    [Fact]
    public void ClientsTalk()
    {
        var (client, server) = InMemoryConnection.CreatePair();
        
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
        var (client, server) = InMemoryConnection.CreatePair();
        
        var clientname = "Alice";
        var servername = "Bob";
        
        client.Send(clientname.Encode());
        var data = server!.Receive();
        
        var received_client = data.Decode();
        
        server.Send(servername.Encode());
        data = client.Receive();
        
        var received_server = data.Decode();
        
        Assert.Equal(clientname, received_client);
        Assert.Equal(servername, received_server);
    }

    [Fact]
    public void MultipleConnections()
    {
        var (client1, server1) = InMemoryConnection.CreatePair();
        var (client2, server2) = InMemoryConnection.CreatePair();
        var (client3, server3) = InMemoryConnection.CreatePair();
        var serverConnections = new List<IConnection>{ server1, server2, server3 };
        
        
        Assert.Equal(3, serverConnections.Count);
        
        client1.Send("c1".Encode());
        client2.Send("c2".Encode());
        client3.Send("c3".Encode());

        foreach (var client in serverConnections)
        {
            var buf = ArrayPool<byte>.Shared.Rent(client.Available);
            
            var recvbytes = client.Receive(buf); 
            var recv = buf[..recvbytes].Decode();
            client.Send($"Hi {recv}!".Encode());
            
            ArrayPool<byte>.Shared.Return(buf);
        }
        
        Assert.Equal("Hi c1!", client1.Receive().Decode());
        Assert.Equal("Hi c2!", client2.Receive().Decode());
        Assert.Equal("Hi c3!", client3.Receive().Decode());
    }
    
    [Fact]
    public void MultipleConnectionsMethod()
    {
        var (client1, server1) = InMemoryConnection.CreatePair();
        var (client2, server2) = InMemoryConnection.CreatePair();
        var (client3, server3) = InMemoryConnection.CreatePair();
        var serverConnections = new List<IConnection>{ server1, server2, server3 };
        
        Assert.Equal(3, serverConnections.Count);
        
        client1.Send("c1".Encode());
        client2.Send("c2".Encode());
        client3.Send("c3".Encode());

        foreach (var connectedClient in serverConnections)
        {
            var buf = ArrayPool<byte>.Shared.Rent(connectedClient.Available);
            
            var recvbytes = connectedClient.Receive(buf); 
            var recv = buf[..recvbytes].Decode();
            connectedClient.Send($"Hi {recv}!".Encode());
            
            ArrayPool<byte>.Shared.Return(buf);
        }
        
        Assert.Equal("Hi c1!", client1.Receive().Decode());
        Assert.Equal("Hi c2!", client2.Receive().Decode());
        Assert.Equal("Hi c3!", client3.Receive().Decode());
    }
}
