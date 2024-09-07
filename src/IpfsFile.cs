using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// A file that resides on IPFS.
/// </summary>
public class IpfsFile : IFile, IChildFile, IGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="IpfsFile"/>.
    /// </summary>
    /// <param name="cid">The CID of the file, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpfsFile(Cid cid, ICoreApi client)
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
    public IpfsFile(Cid cid, string name, ICoreApi client)
    {
        Name = !string.IsNullOrWhiteSpace(name) ? name : cid;
        Id = cid;
        Client = client;
    }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    public ICoreApi Client { get; }

    /// <inheritdoc/>
    public virtual string Id { get; }

    /// <inheritdoc/>
    public virtual string Name { get; }

    /// <summary>
    /// The parent directory, if any.
    /// </summary>
    public virtual IpfsFolder? Parent { get; init; } = null;

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

    /// <inheritdoc/>
    public virtual async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        if (accessMode.HasFlag(FileAccess.Write))
            throw new NotSupportedException("Attempted to write data to an immutable file on IPFS.");

        // Open and wrap ipfs stream
        var stream = await Client.FileSystem.ReadFileAsync(Id, cancellationToken);
        var fileData = await Client.Mfs.StatAsync($"/ipfs/{Id}", cancellationToken);
        var streamWithLength = new LengthOverrideStream(stream, fileData.Size);

        return new ReadOnlyOverrideStream(streamWithLength);
    }

    /// <inheritdoc/>
    public Task<Cid> GetCidAsync(CancellationToken cancellationToken) => Task.FromResult<Cid>(Id);
}

