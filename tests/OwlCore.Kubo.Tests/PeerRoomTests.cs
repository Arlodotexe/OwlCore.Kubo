using System.Text;
using Ipfs;

namespace OwlCore.Kubo.Tests;

[TestClass]
public class PeerRoomTests
{
    [TestMethod]
    public async Task TestHeartbeat()
    {
        var currentPeer = new Peer
        {
            Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName)),
        };

        var loopback = new LoopbackPubSubApi(currentPeer);
        var peerRoom = new PeerRoom(currentPeer, loopback, "test");

        var messagesReceived = 0;

        await loopback.SubscribeAsync(peerRoom.TopicName, x => messagesReceived++, default);
        await peerRoom.BroadcastHeartbeatAsync();

        Assert.AreEqual(1, messagesReceived);
    }

    [TestMethod]
    public async Task ConnectedPeers()
    {
        var firstPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName)), };
        var secondPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.MachineName}.2")), };
        
        var firstPeerLoopback = new LoopbackPubSubApi(firstPeer);
        var secondPeerLoopback = new LoopbackPubSubApi(secondPeer);

        secondPeerLoopback.AddLoopback(firstPeerLoopback);

        var firstPeerRoom = new PeerRoom(firstPeer, firstPeerLoopback, "test");
        var secondPeerRoom = new PeerRoom(firstPeer, secondPeerLoopback, "test");

        await Task.Delay(5000);
        
        Assert.AreEqual(1, firstPeerRoom.ConnectedPeers.Count);
    }

    [TestMethod]
    public async Task ExchangeMessagesWithPeersInRoom()
    {
        var firstPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName)), };
        var secondPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.MachineName}.2")), };
        
        var firstPeerLoopback = new LoopbackPubSubApi(firstPeer);
        var secondPeerLoopback = new LoopbackPubSubApi(secondPeer);

        secondPeerLoopback.AddLoopback(firstPeerLoopback);

        var firstPeerRoom = new PeerRoom(firstPeer, firstPeerLoopback, "test");
        var secondPeerRoom = new PeerRoom(firstPeer, secondPeerLoopback, "test");

        await Task.Delay(5000);

        Assert.AreEqual(1, firstPeerRoom.ConnectedPeers.Count);

        var messages = 0;

        secondPeerRoom.MessageReceived += (sender, message) =>
        {
            messages++;
        };

        await firstPeerRoom.PublishAsync("test");

        Assert.AreEqual(1, messages);
    }
}