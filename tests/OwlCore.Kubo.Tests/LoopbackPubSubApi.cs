using System.Text;
using System.Threading.Channels;
using Ipfs;
using Ipfs.CoreApi;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using OwlCore.Extensions;
using OwlCore.Kubo.Models;

namespace OwlCore.Kubo.Tests;

public class LoopbackPubSubApi : IPubSubApi
{
    private readonly Peer _senderPeer;
    private readonly List<IPubSubApi> _loopbackApis = new();

    public LoopbackPubSubApi(Peer senderPeer)
    {
        _senderPeer = senderPeer;
    }

    private readonly Dictionary<string, HashSet<Action<IPublishedMessage>>> _handlers = new();

    public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = new())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = new())
    {
        throw new NotImplementedException();
    }

    public Task PublishAsync(string topic, string message, CancellationToken cancel = new()) => PublishAsync(topic, Encoding.UTF8.GetBytes(message), cancel);

    public Task PublishAsync(string topic, byte[] message, CancellationToken cancel = new()) => PublishAsync(topic, new MemoryStream(message), cancel);

    public Task PublishAsync(string topic, Stream message, CancellationToken cancel = new())
    {
        if (_handlers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(new PublishedMessage(_senderPeer, topic.IntoList(), Array.Empty<byte>(), message.ToBytes(),
                    message, message.Length));
            }
        }

        return _loopbackApis.InParallel(x => x.PublishAsync(topic, message, cancel));
    }

    public Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
    {
        _handlers.TryAdd(topic, new HashSet<Action<IPublishedMessage>>());
        _handlers[topic].Add(handler); 

        return _loopbackApis.InParallel(x => x.SubscribeAsync(topic, handler, cancellationToken));
    }

    public void AddLoopback(IPubSubApi api)
    {
        _loopbackApis.Add(api);
    }
}