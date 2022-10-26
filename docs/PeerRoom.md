# PeerRoom

Watch a pubsub topic for other nodes that join the room.

> **Warning** Unless you provide an [encrypted `IPubSubApi`](./AesPasswordEncryptedPubSub.md), the room should be considered publicly joinable.

## Basic usage

```cs
// Get an existing instance of IpfsClient.
var ipfsClient = GetIpfsClient();

// Get the current peer
var thisPeer = await ipfsClient.IdAsync();

// Optionally, encrypt the pubsub API
var pubsub = shouldBePublic ? ipfsClient.PubSubApi : new AesPasswordEncryptedPubSub(ipfsClient.PubSubApi, password: "testing", salt: null);

// Create the room
var peerRoom = new PeerRoom(thisPeer, pubsub, topicName: "sample");

// An ObservableCollection<Peer> of the peers participating in this room.
Peers = peerRoom.ConnectedPeers;

// Dispose the peer room when done.
peerRoom.Dispose();
```

## Customize the heartbeat
Depending on your usage scenario, you may need to adjust how often heartbeats are sent out, and how long it takes for them to expire.

To customize these, use the `heartbeatInterval` and `heartbeatExpirationTime` parameters:

```cs
new PeerRoom(thisPeer, pubsub, topicName: "sample", heartbeatInterval: TimeSpan.FromSeconds(3), heartbeatExpirationTime: TimeSpan.FromSeconds(6));
```

## Scoped Pubsub to/from peers in the room.

> **Note** If you're moving large amounts of data, add it to IPFS instead, and use pubsub to transmit the CID.

```cs
// Create the room
var peerRoom = new PeerRoom(thisPeer, pubsub, topicName: "sample");

// Listen for messages from connected peers
peerRoom.MessageReceived += OnMessageReceived

// Send a message to connected peers
// string, byte[], and Stream are supported
await peerRoom.PublishAsync("message");

void OnMessageReceived(object sender, IPublishedMessage e)
{
    // Handle received message
}
```