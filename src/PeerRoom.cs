using Ipfs;
using Ipfs.CoreApi;
using System.Collections.ObjectModel;
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
    private readonly SynchronizationContext _syncContext;
    private readonly IPubSubApi _pubSubApi;
    private readonly Timer? _timer;
    private readonly TimeSpan _heartbeatExpirationTime;
    private readonly CancellationTokenSource _disconnectTokenSource = new();
    private readonly Dictionary<MultiHash, (Peer Peer, DateTime LastSeen)> _lastSeenDates = new();
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

    internal bool HeartbeatEnabled { get; set; } = true;

    /// <summary>
    /// Broadcasts a heartbeat to listeners on the topic.
    /// </summary>
    /// <returns></returns>
    public async Task BroadcastHeartbeatAsync()
    {
        if (HeartbeatEnabled)
            await _pubSubApi.PublishAsync(TopicName, "KuboPeerRoomHeartbeat", _disconnectTokenSource.Token);
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

    private void ReceiveMessage(IPublishedMessage publishedMessage)
    {
        if (publishedMessage.Sender.Id == ThisPeer.Id)
            return;

        if (_disconnectTokenSource.Token.IsCancellationRequested)
            return;

        _receivedMessageMutex.Wait();

        if (System.Text.Encoding.UTF8.GetString(publishedMessage.DataBytes) == "KuboPeerRoomHeartbeat" && HeartbeatEnabled)
        {
            if (!_lastSeenDates.ContainsKey(publishedMessage.Sender.Id))
            {
                _syncContext.Post(_ => ConnectedPeers.Add(publishedMessage.Sender), null);
            }

            _lastSeenDates[publishedMessage.Sender.Id] = (publishedMessage.Sender, DateTime.Now);
        }
        else if (ConnectedPeers.Any(x => x.Id == publishedMessage.Sender.Id))
        {
            MessageReceived?.Invoke(this, publishedMessage);
        }

        _receivedMessageMutex.Release();
    }

    internal void PruneStalePeers()
    {
        _receivedMessageMutex.Wait();

        var now = DateTime.Now;

        foreach (var peer in ConnectedPeers.ToArray())
        {
            if (now - _heartbeatExpirationTime > _lastSeenDates[peer.Id].LastSeen)
            {
                _syncContext.Post(_ => ConnectedPeers.Remove(ConnectedPeers.First(x => x.Id == peer.Id)), null);
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
