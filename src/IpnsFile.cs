using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;
using System.Threading;

namespace OwlCore.Kubo;

/// <summary>
/// A file that resides on IPFS behind an IPNS Address.
/// </summary>
public class IpnsFile : IFile, IChildFile
{
    private readonly IpfsClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFile(string ipnsAddress, IpfsClient client)
    {
        Id = ipnsAddress;
        Name = MfsFolder.GetFolderItemName(ipnsAddress);
        _client = client;
    }

    /// <inheritdoc />
    public string Id { get; protected set; }

    /// <inheritdoc />
    public string Name { get; protected set; }

    /// <summary>
    /// Retrieves the current CID of this item from IPNS.
    /// </summary>
    /// <param name="cancellationToken">Used to cancel the ongoing operation.</param>
    /// <returns>The resolved CID.</returns>
    public async Task<Cid> ResolveCidAsync(CancellationToken cancellationToken)
    {
        var resolvedIpns = await _client.ResolveAsync(Id, recursive: true, cancel: cancellationToken);
        Guard.IsNotNull(resolvedIpns);

        Cid cid = resolvedIpns.Split(new[] { "/ipfs/" }, StringSplitOptions.None)[1];
        return cid;
    }

    /// <summary>
    /// The parent directory, if any.
    /// </summary>
    internal IpnsFolder? Parent { get; init; } = null;

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

    /// <inheritdoc />
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var cid = await ResolveCidAsync(cancellationToken);
        var file = new IpfsFile(cid, _client);

        return await file.OpenStreamAsync(accessMode, cancellationToken);
    }
}