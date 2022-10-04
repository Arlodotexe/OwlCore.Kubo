using Ipfs;
using Ipfs.CoreApi;
using System.Collections.ObjectModel;
using OwlCore.Extensions;
using Timer = System.Timers.Timer;

namespace OwlCore.Kubo;

/// <summary>
/// A "room" of peers listening on a common topic name.
/// Broadcasts a heartbeat to peers on the same topic name, and listens for the heartbeats of other peers.
/// </summary>
/// <remarks>
/// Unless you provide an encrypted <see cref="IPubSubApi"/>, the room should be considered publicly joinable. 
/// </remarks>
public class PeerRoom : IDisposable
{
    private readonly IPubSubApi _pubSubApi;
    private readonly Timer? _timer;
    private readonly int _heartbeatExpirationTimeSeconds;
    private readonly CancellationTokenSource _disconnectTokenSource = new();
    private readonly Dictionary<Peer, DateTime> _lastSeenData = new();

    /// <summary>
    /// Creates a new instance of <see cref="PeerRoom"/>.
    /// </summary>
    /// <param name="thisPeer">The peer information about the current node.</param>
    /// <param name="pubSubApi">An existing, functional <see cref="IPubSubApi"/> instance to use for sending and receiving messages.</param>
    /// <param name="topicName">The PubSub topic (or "channel") name to use for communicating with other peers.</param>
    /// <param name="heartbeatIntervalMilliseconds">How often this peer will broadcast a heartbeat.</param>
    /// <param name="heartbeatExpirationTimeMilliseconds">When another peer hasn't broadcast a heartbeat for this many milliseconds, they will be removed from the list of connected peers.</param>
    public PeerRoom(Peer thisPeer, IPubSubApi pubSubApi, string topicName, int heartbeatIntervalMilliseconds = 2000, int heartbeatExpirationTimeMilliseconds = 5000)
    {
        _pubSubApi = pubSubApi;
        _heartbeatExpirationTimeSeconds = heartbeatExpirationTimeMilliseconds;

        ThisPeer = thisPeer;
        TopicName = topicName;

        if (heartbeatIntervalMilliseconds > 0)
        {
            _timer = new Timer(heartbeatIntervalMilliseconds);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        _ = pubSubApi.SubscribeAsync(topicName, ReceiveMessage, _disconnectTokenSource.Token);
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _ = BroadcastHeartbeat();
        PruneStalePeers();
    }

    /// <summary>
    /// The peers which have successfully authenticated and joined the room.
    /// </summary>
    public ObservableCollection<Peer> ConnectedPeers { get; } = new();

    /// <summary>
    /// Raised when a new message is received.
    /// </summary>
    public EventHandler<IPublishedMessage>? MessageReceived;

    /// <summary>
    /// The peer which represents this device.
    /// </summary>
    public Peer ThisPeer { get; }

    /// <summary>
    /// The topic being used for communication.
    /// </summary>
    public string TopicName { get; }

    /// <summary>
    /// Broadcasts a heartbeat to listeners on the topic.
    /// </summary>
    /// <returns></returns>
    public async Task BroadcastHeartbeat()
    {
        await _pubSubApi.PublishAsync(TopicName, "KuboPeerRoomHeartbeat");
    }

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task BroadcastMessageAsync(string message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task BroadcastMessageAsync(byte[] message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task BroadcastMessageAsync(Stream message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    private void ReceiveMessage(IPublishedMessage publishedMessage)
    {
        if (System.Text.Encoding.UTF8.GetString(publishedMessage.DataBytes) == "KuboPeerRoomHeartbeat")
        {
            _lastSeenData[publishedMessage.Sender] = DateTime.Now;
            ConnectedPeers.Add(publishedMessage.Sender);
        }
        else if (ConnectedPeers.Any(x => x.Id == publishedMessage.Sender.Id))
        {
            MessageReceived?.Invoke(this, publishedMessage);
        }
    }

    private void PruneStalePeers()
    {
        foreach (var peer in ConnectedPeers)
        {
            if (DateTime.Now - _lastSeenData[peer] > TimeSpan.FromMilliseconds(_heartbeatExpirationTimeSeconds))
            {
                ConnectedPeers.Remove(peer);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_timer is not null)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
        }

        _disconnectTokenSource.Cancel();
    }
}
