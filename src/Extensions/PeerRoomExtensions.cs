using System.Collections.Specialized;
using System.Timers;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Timer = System.Timers.Timer;

namespace OwlCore.Kubo.Extensions;

/// <summary>
/// A set of extensions and helpers for <see cref="PeerRoom"/>.
/// </summary>
public static class PeerRoomExtensions
{
    /// <summary>
    /// Waits for a specific peer to join the peer room.
    /// </summary>>
    /// <param name="peerRoom">The peer room to observe.</param>
    /// <param name="peer">The peer to wait for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the peer that joined the room.</returns>
    public static async Task WaitForJoinAsync(this PeerRoom peerRoom, Peer peer, CancellationToken cancellationToken)
    {
        var enteredPeerTaskCompletionSource = new TaskCompletionSource();

        void OnConnectedPeersOnCollectionChanged(object? _, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems == null)
                return;

            if (args.NewItems.Count > 1)
                throw new InvalidOperationException("Too many nodes joined the room.");

            if (args.NewItems.Cast<Peer>().All(x => x.Id != peer.Id))
                return;
            
            enteredPeerTaskCompletionSource.SetResult();
        }

        peerRoom.ConnectedPeers.CollectionChanged += OnConnectedPeersOnCollectionChanged;
        
        await enteredPeerTaskCompletionSource.Task.WaitAsync(cancellationToken);
        
        peerRoom.ConnectedPeers.CollectionChanged -= OnConnectedPeersOnCollectionChanged;
    }
    
    /// <summary>
    /// Waits for the first peer to enter the room.
    /// </summary>
    /// <param name="peerRoom">The peer room to observe.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the peer that joined the room.</returns>
    /// <exception cref="InvalidOperationException">Too many peers joined the room at once.</exception>
    public static async Task<Peer> WaitForJoinAsync(this PeerRoom peerRoom, CancellationToken cancellationToken)
    {
        var enteredPeerTaskCompletionSource = new TaskCompletionSource<Peer>();

        void OnConnectedPeersOnCollectionChanged(object? _, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems == null)
                return;

            if (args.NewItems.Count > 1)
                throw new InvalidOperationException("Too many nodes joined the room.");

            var peer = (Peer?)args.NewItems[0];
            Guard.IsNotNull(peer);
            enteredPeerTaskCompletionSource.SetResult(peer);
        }

        peerRoom.ConnectedPeers.CollectionChanged += OnConnectedPeersOnCollectionChanged;
        
        var joinedPeer = await enteredPeerTaskCompletionSource.Task.WaitAsync(cancellationToken);
        
        peerRoom.ConnectedPeers.CollectionChanged -= OnConnectedPeersOnCollectionChanged;
        
        return joinedPeer;
    }
    
    /// <summary>
    /// Waits for a specific message to be sent to the provided <paramref name="room"/>.
    /// </summary>
    /// <param name="room">The room to listen for messages in.</param>
    /// <param name="expectedBytes">The bytes that are expected for a received message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>A task containing the received message.</returns>
    public static async Task<IPublishedMessage> WaitToReceiveMessageAsync(this PeerRoom room, byte[] expectedBytes, CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<IPublishedMessage>();
        
        room.MessageReceived += MessageReceived;

        void MessageReceived(object? sender, IPublishedMessage e)
        {
            if (e.DataBytes.SequenceEqual(expectedBytes))
                taskCompletionSource.SetResult(e);
        }

#if NET5_0_OR_GREATER
        var result = await taskCompletionSource.Task.WaitAsync(cancellationToken);
#elif NETSTANDARD
        var result = await taskCompletionSource.Task;
#endif

        room.MessageReceived -= MessageReceived;
        
        return result;
    }

    /// <summary>
    /// Waits for a specific message to be sent to the provided <paramref name="room"/>.
    /// </summary>
    /// <param name="messageToPublish">The message to publish at the given interval.</param>
    /// <param name="room">The room to listen for messages in.</param>
    /// <param name="publishInterval">The interval at which the message is published to the room.</param>
    /// <param name="expectedResponse">The bytes that are expected for a received message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>A task containing the received message.</returns>
    public static async Task<IPublishedMessage> PublishUntilMessageReceivedAsync(this PeerRoom room, byte[] messageToPublish, byte[] expectedResponse, TimeSpan publishInterval, CancellationToken cancellationToken)
    {
        var timer = new Timer(publishInterval.TotalMilliseconds);
        var taskCompletionSource = new TaskCompletionSource<IPublishedMessage>();

#if NET5_0_OR_GREATER
        var task = taskCompletionSource.Task.WaitAsync(cancellationToken);
#elif NETSTANDARD
        var task = taskCompletionSource.Task;
#endif

        timer.Elapsed += TimerOnElapsed;
        room.MessageReceived += MessageReceived;

        timer.Start();
        
        var result = await task;
        
        timer.Elapsed -= TimerOnElapsed;
        room.MessageReceived -= MessageReceived;
        
        return result;

        void MessageReceived(object? sender, IPublishedMessage e)
        {
            if (e.DataBytes.SequenceEqual(expectedResponse))
                taskCompletionSource.SetResult(e);
        }

        async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await room.PublishAsync(messageToPublish, cancellationToken);
        }
    }
}