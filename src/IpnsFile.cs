using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// A file that resides on IPFS behind an IPNS Address.
/// </summary>
public class IpnsFile : IFile, IChildFile, IGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFile(string ipnsAddress, ICoreApi client)
    {
        Id = ipnsAddress;
        // Handle named files in ipns addresses, both in folders and directly published to the root of an IPNS address.
        // An IPNS address usually only has a filename in the path query if the published ipns value is a file and not a folder, otherwise names are assigned via the folder.
        Name = PathHelpers.TryGetFileNameFromPathQuery(ipnsAddress) ?? PathHelpers.GetFolderItemName(ipnsAddress);
        Client = client;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFile"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="name">A custom name to use for the file.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFile(string ipnsAddress, string name, ICoreApi client)
    {
        Id = ipnsAddress;
        Name = name;
        Client = client;
    }

    /// <inheritdoc />
    public string Id { get; protected set; }

    /// <inheritdoc />
    public string Name { get; protected set; }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    public ICoreApi Client { get; }

    /// <summary>
    /// The parent directory, if any.
    /// </summary>
    internal IpnsFolder? Parent { get; init; } = null;

    /// <inheritdoc/>
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

    /// <inheritdoc />
    public virtual async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var cid = await GetCidAsync(cancellationToken);
        var file = new IpfsFile(cid, Client);

        return await file.OpenStreamAsync(accessMode, cancellationToken);
    }

    /// <summary>
    /// Retrieves the current CID of this item from IPNS.
    /// </summary>
    /// <param name="cancellationToken">Used to cancel the ongoing operation.</param>
    /// <returns>The resolved CID.</returns>
    public async Task<Cid> GetCidAsync(CancellationToken cancellationToken)
    {
        var resolvedIpns = await Client.Name.ResolveAsync(Id, recursive: true, cancel: cancellationToken);
        Guard.IsNotNull(resolvedIpns);

        var cidOfResolvedIpfsPath = await Client.Mfs.StatAsync(resolvedIpns, cancel: cancellationToken);

        Guard.IsNotNull(cidOfResolvedIpfsPath.Hash);
        return cidOfResolvedIpfsPath.Hash;
    }
}