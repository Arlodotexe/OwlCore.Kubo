using Ipfs;
using System.Text;

namespace OwlCore.Kubo.Models;

/// <summary>
/// A implementation of <see cref="IPublishedMessage"/> where each property can be a custom value.
/// </summary>
public class PublishedMessage : IPublishedMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="PublishedMessage"/>.
    /// </summary>
    public PublishedMessage(Peer sender, IEnumerable<string> topics, byte[] sequenceNumber, byte[] dataBytes)
    {
        Sender = sender;
        Topics = topics;
        SequenceNumber = sequenceNumber;
        DataBytes = dataBytes;
    }

    /// <inheritdoc/>
    public Peer Sender { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Topics { get; }

    /// <inheritdoc/>
    public byte[] SequenceNumber { get; }

    /// <inheritdoc/>
    public byte[] DataBytes { get; }
}
