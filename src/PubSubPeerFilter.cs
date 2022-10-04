using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;

namespace OwlCore.Kubo;

/// <summary>
/// Wraps an existing <see cref="IPubSubApi"/> and filters out any messages that aren't allowed by the provided filter.
/// </summary>
public class PubSubPeerFilter : IDelegatable<IPubSubApi>, IPubSubApi
{
    private readonly Func<Peer, bool> _shouldFilterPeer;

    /// <summary>
    /// Creates a new instance of <see cref="PubSubPeerFilter"/>.
    /// </summary>
    /// <param name="inner">A wrapped implementation which member access can be delegated to.</param>
    /// <param name="shouldFilterPeer">The filter that decides if messages from a peer should be filtered and excluded.</param>
    public PubSubPeerFilter(IPubSubApi inner, Func<Peer, bool> shouldFilterPeer)
    {
        Inner = inner;
        _shouldFilterPeer = shouldFilterPeer;
    }

    /// <inheritdoc/>
    public IPubSubApi Inner { get; }

    /// <inheritdoc/>
    public Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
    {
        return Inner.SubscribeAsync(topic, msg =>
        {
            if (!_shouldFilterPeer(msg.Sender))
                handler(msg);

        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Peer>> PeersAsync(string? topic = null, CancellationToken cancel = default) => Inner.PeersAsync(topic, cancel);

    /// <inheritdoc/>
    public Task PublishAsync(string topic, string message, CancellationToken cancel = default) => Inner.PublishAsync(topic, message, cancel);

    /// <inheritdoc/>
    public Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default) => Inner.PublishAsync(topic, message, cancel);

    /// <inheritdoc/>
    public Task PublishAsync(string topic, Stream message, CancellationToken cancel = default) => Inner.PublishAsync(topic, message, cancel);

    /// <inheritdoc/>
    public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default) => Inner.SubscribedTopicsAsync(cancel);
}
