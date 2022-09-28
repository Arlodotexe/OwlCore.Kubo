using Ipfs;

namespace OwlCore.Kubo.Models;

/// <summary>
/// A implementation of <see cref="IPublishedMessage"/> where each property can be a custom value.
/// </summary>
public class PublishedMessage : IPublishedMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="PublishedMessage"/>.
    /// </summary>
    public PublishedMessage(Peer sender, IEnumerable<string> topics, byte[] sequenceNumber, byte[] dataBytes, Stream dataStream, Cid id, long size)
    {
        Sender = sender;
        Topics = topics;
        SequenceNumber = sequenceNumber;
        DataBytes = dataBytes;
        DataStream = dataStream;
        Id = id;
        Size = size;
    }

    /// <inheritdoc/>
    public Peer Sender { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Topics { get; }

    /// <inheritdoc/>
    public byte[] SequenceNumber { get; }

    /// <inheritdoc/>
    public byte[] DataBytes { get; }

    /// <inheritdoc/>
    public Stream DataStream { get; }

    /// <inheritdoc/>
    public Cid Id { get; }

    /// <inheritdoc/>
    public long Size { get; }
}
