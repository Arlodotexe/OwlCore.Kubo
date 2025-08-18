using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Extensions;
using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;

namespace OwlCore.Kubo;

/// <summary>
/// Watch a pubsub topic for other nodes that join the room.
/// </summary>
/// <remarks>
/// Unless you provide an encrypted <see cref="IPubSubApi"/>, the room should be considered publicly joinable. 
/// </remarks>
public class PeerRoom : IDisposable
{
    private readonly SynchronizationContext _syncContext;
    private readonly IPubSubApi _pubSubApi;
    private readonly Timer? _timer;
    private readonly TimeSpan _heartbeatExpirationTime;
    private readonly CancellationTokenSource _disconnectTokenSource = new();
    private readonly Dictionary<MultiHash, (Peer Peer, DateTime LastSeen, string HeartbeatMessage)> _lastSeenDates = new();
    private readonly SemaphoreSlim _receivedMessageMutex = new(1, 1);

    /// <summary>
    /// Creates a new instance of <see cref="PeerRoom"/>.
    /// </summary>
    /// <param name="thisPeer">The peer information about the current node.</param>
    /// <param name="pubSubApi">An existing, functional <see cref="IPubSubApi"/> instance to use for sending and receiving messages.</param>
    /// <param name="topicName">The PubSub topic (or "channel") name to use for communicating with other peers.</param>
    public PeerRoom(Peer thisPeer, IPubSubApi pubSubApi, string topicName)
        : this(thisPeer, pubSubApi, topicName, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(2000))
    {

    }

    /// <summary>
    /// Creates a new instance of <see cref="PeerRoom"/>.
    /// </summary>
    /// <param name="thisPeer">The peer information about the current node.</param>
    /// <param name="pubSubApi">An existing, functional <see cref="IPubSubApi"/> instance to use for sending and receiving messages.</param>
    /// <param name="topicName">The PubSub topic (or "channel") name to use for communicating with other peers.</param>
    /// <param name="heartbeatInterval">How often this peer will broadcast a heartbeat.</param>
    /// <param name="heartbeatExpirationTime">When another peer hasn't broadcast a heartbeat for this many milliseconds, they will be removed from the list of connected peers.</param>
    public PeerRoom(Peer thisPeer, IPubSubApi pubSubApi, string topicName, TimeSpan heartbeatInterval, TimeSpan heartbeatExpirationTime)
    {
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _pubSubApi = pubSubApi;
        _heartbeatExpirationTime = heartbeatExpirationTime;

        ThisPeer = thisPeer;
        TopicName = topicName;

        _timer = new Timer(heartbeatInterval.TotalMilliseconds);
        _timer.Elapsed += Timer_Elapsed;
        _timer.Start();

        _ = pubSubApi.SubscribeAsync(topicName, ReceiveMessage, _disconnectTokenSource.Token);
    }

    private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await BroadcastHeartbeatAsync();
        await PruneStalePeersAsync(CancellationToken.None);
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
    /// The name of the topic being used for communication.
    /// </summary>
    public string TopicName { get; }

    /// <summary>
    /// The string to publish for the heartbeat.
    /// </summary>
    public string HeartbeatMessage { get; set; } = "KuboPeerRoomHeartbeat";

    /// <summary>
    /// Gets or sets a boolean that indicates whether the heartbeat for this peer is enabled.
    /// </summary>
    /// <remarks>
    /// If disabled, other peers will not see this peer in the peer room because the heartbeat will not be broadcast. This can be useful when building specialized peer rooms.
    /// </remarks>
    public bool HeartbeatEnabled { get; set; } = true;

    /// <summary>
    /// Broadcasts a heartbeat to listeners on the topic.
    /// </summary>
    /// <returns></returns>
    public async Task BroadcastHeartbeatAsync()
    {
        if (!HeartbeatEnabled)
            return;

        try
        {
            await _pubSubApi.PublishAsync(TopicName, HeartbeatMessage, _disconnectTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation during shutdown/dispose to avoid unhandled exceptions from timer thread.
        }
    }

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task PublishAsync(string message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task PublishAsync(byte[] message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    /// <summary>
    /// Broadcasts a message to all other peers in the room.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancel">The token to use for cancellation</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task PublishAsync(Stream message, CancellationToken cancel = default) => _pubSubApi.PublishAsync(TopicName, message, cancel);

    private async void ReceiveMessage(IPublishedMessage publishedMessage)
    {
        if (publishedMessage.Sender.Id is null)
            return;

        if (publishedMessage.Sender.Id == ThisPeer.Id)
            return;

        if (_disconnectTokenSource.Token.IsCancellationRequested)
            return;

        await _receivedMessageMutex.WaitAsync();

        if (System.Text.Encoding.UTF8.GetString(publishedMessage.DataBytes) == HeartbeatMessage)
        {
            if (!_lastSeenDates.ContainsKey(publishedMessage.Sender.Id))
            {
                await _syncContext.PostAsync(() =>
                {
                    ConnectedPeers.Add(publishedMessage.Sender);
                    return Task.CompletedTask;
                });
            }

            _lastSeenDates[publishedMessage.Sender.Id] = (publishedMessage.Sender, DateTime.Now, HeartbeatMessage);
        }
        else if (ConnectedPeers.Any(x => x.Id == publishedMessage.Sender.Id))
        {
            MessageReceived?.Invoke(this, publishedMessage);
        }

        _receivedMessageMutex.Release();
    }

    /// <summary>
    /// Prunes any stale peers from <see cref="ConnectedPeers"/>, including expired or outdated heartbeats.
    /// </summary>
    public async Task PruneStalePeersAsync(CancellationToken cancellationToken)
    {
        await _receivedMessageMutex.WaitAsync(cancellationToken);

        var now = DateTime.Now;

        foreach (var peer in ConnectedPeers.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.IsNotNull(peer.Id);

            var heartbeatExpired = now - _heartbeatExpirationTime > _lastSeenDates[peer.Id].LastSeen;
            var heartbeatOutdated = _lastSeenDates[peer.Id].HeartbeatMessage != HeartbeatMessage;
            
            if (heartbeatExpired || heartbeatOutdated)
            {
                await _syncContext.PostAsync(() => Task.FromResult(ConnectedPeers.Remove(ConnectedPeers.First(x => x.Id == peer.Id))));
                _lastSeenDates.Remove(_lastSeenDates.First(x => x.Key == peer.Id).Key);
            }
        }

        _receivedMessageMutex.Release();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        HeartbeatEnabled = false;
        _disconnectTokenSource.Cancel();

        if (_timer is not null)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
