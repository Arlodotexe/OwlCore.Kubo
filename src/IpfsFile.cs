using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// A file that resides on IPFS.
/// </summary>
public class IpfsFile : IFile
{
    /// <summary>
    /// Creates a new instance of <see cref="IpfsFile"/>.
    /// </summary>
    /// <param name="cid">The CID of the file, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpfsFile(Cid cid, IpfsClient client)
    {
        Name = cid;
        Id = cid;
        Client = client;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IpfsFile"/>.
    /// </summary>
    /// <param name="cid">The CID of the file, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="name">The name of the file.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpfsFile(Cid cid, string name, IpfsClient client)
    {
        Name = !string.IsNullOrWhiteSpace(name) ? name : cid;
        Id = cid;
        Client = client;
    }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    protected IpfsClient Client { get; }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        if (accessMode.HasFlag(FileAccess.Write))
            throw new NotSupportedException("Attempted to write data to an immutable file on IPFS.");

        return Client.FileSystem.ReadFileAsync(Id, cancellationToken);
    }
}

