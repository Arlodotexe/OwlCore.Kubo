using System.Diagnostics;
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

        firstPeerLoopback.AddLoopback(secondPeerLoopback);
        secondPeerLoopback.AddLoopback(firstPeerLoopback);

        using var firstPeerRoom = new PeerRoom(firstPeer, firstPeerLoopback, "test", TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000));
        using var secondPeerRoom = new PeerRoom(secondPeer, secondPeerLoopback, "test", TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000));

        // PeerRoom cannot wait until someone has joined the room. Must be done manually.
        // Use a delay instead of building that all out.
        await Task.Delay(100);

        // Ensure peers joined each others room
        Assert.AreEqual(1, firstPeerRoom.ConnectedPeers.Count);
        Assert.AreNotEqual(firstPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(secondPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);

        Assert.AreEqual(1, secondPeerRoom.ConnectedPeers.Count);
        Assert.AreNotEqual(secondPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(firstPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);
    }

    [TestMethod]
    public async Task ExchangeMessagesWithPeersInRoom()
    {
        var firstPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName)), };
        var secondPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.MachineName}.2")), };

        var firstPeerLoopback = new LoopbackPubSubApi(firstPeer);
        var secondPeerLoopback = new LoopbackPubSubApi(secondPeer);

        firstPeerLoopback.AddLoopback(secondPeerLoopback);
        secondPeerLoopback.AddLoopback(firstPeerLoopback);

        using var firstPeerRoom = new PeerRoom(firstPeer, firstPeerLoopback, "test", TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000));
        using var secondPeerRoom = new PeerRoom(secondPeer, secondPeerLoopback, "test", TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000));

        // PeerRoom cannot wait until someone has joined the room. Must be done manually.
        // Use a delay instead of building that all out.
        await Task.Delay(100);

        // Ensure peers joined each others room
        Assert.AreEqual(1, firstPeerRoom.ConnectedPeers.Count);
        Assert.AreNotEqual(firstPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(secondPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);

        Assert.AreEqual(1, secondPeerRoom.ConnectedPeers.Count);
        Assert.AreNotEqual(secondPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(firstPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);

        var messages = 0;

        secondPeerRoom.MessageReceived += (sender, message) =>
        {
            // Peer room must not emit messages from the sender peer.
            var room = (PeerRoom)sender;
            Assert.AreNotEqual(room.ThisPeer, message.Sender.Id);

            messages++;
        };

        await firstPeerRoom.PublishAsync("test");

        Assert.AreEqual(1, messages);
    }

    [TestMethod]
    public async Task DisposingPeerRemovesFromRoom()
    {
        var firstPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName)), };
        var secondPeer = new Peer { Id = MultiHash.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.MachineName}.2")), };

        var firstPeerLoopback = new LoopbackPubSubApi(firstPeer);
        var secondPeerLoopback = new LoopbackPubSubApi(secondPeer);

        firstPeerLoopback.AddLoopback(secondPeerLoopback);
        secondPeerLoopback.AddLoopback(firstPeerLoopback);

        using var firstPeerRoom = new PeerRoom(firstPeer, firstPeerLoopback, "test");
        var secondPeerRoom = new PeerRoom(secondPeer, secondPeerLoopback, "test");

        var peersLeft = new List<Peer>();

        firstPeerRoom.ConnectedPeers.CollectionChanged += (sender, args) =>
        {
            foreach (var item in args.OldItems?.Cast<Peer>() ?? Enumerable.Empty<Peer>())
                peersLeft.Add(item);
        };

        // PeerRoom cannot wait until someone has joined the room. Must be done manually.
        // Use a delay instead of building that all out.
        await Task.Delay(1500);

        // Ensure peers joined each others room
        Assert.AreEqual(1, firstPeerRoom.ConnectedPeers.Count);
        Assert.AreEqual(0, peersLeft.Count);
        Assert.AreNotEqual(firstPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(secondPeer.Id, firstPeerRoom.ConnectedPeers[0].Id);

        Assert.AreEqual(1, secondPeerRoom.ConnectedPeers.Count);
        Assert.AreNotEqual(secondPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);

        // Dispose and stop emitting
        secondPeerRoom.Dispose();

        // Simulate a disconnect
        firstPeerLoopback.RemoveLoopback(secondPeerLoopback);
        secondPeerLoopback.RemoveLoopback(firstPeerLoopback);

        // Wait for peers to be disconnected
        await Task.Delay(5000);

        Assert.AreEqual(firstPeer.Id, secondPeerRoom.ConnectedPeers[0].Id);
        Assert.AreEqual(0, firstPeerRoom.ConnectedPeers.Count);
        Assert.AreEqual(1, peersLeft.Count);
        Assert.AreEqual(secondPeer.Id, peersLeft[0].Id);
    }
}