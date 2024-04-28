using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;
using System.Runtime.CompilerServices;

namespace OwlCore.Kubo;

/// <summary>
/// A folder that resides on IPFS.
/// </summary>
public class IpfsFolder : IFolder, IChildFolder, IGetCid
{

    /// <summary>
    /// Creates a new instance of <see cref="IpfsFolder"/>.
    /// </summary>
    /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpfsFolder(Cid cid, ICoreApi client)
    {
        Id = cid;
        Name = cid;
        Client = client;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IpfsFolder"/>.
    /// </summary>
    /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="name">The name of the folder.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpfsFolder(Cid cid, string name, ICoreApi client)
    {
        Id = cid;
        Name = !string.IsNullOrWhiteSpace(name) ? name : cid;
        Client = client;
    }


    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    public ICoreApi Client { get; }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// The parent directory, if any.
    /// </summary>
    internal IpfsFolder? Parent { get; init; } = null;

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

    /// <inheritdoc/>
    public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var itemInfo = await Client.FileSystem.ListAsync(Id, cancellationToken);
        Guard.IsTrue(itemInfo.IsDirectory);

        foreach (var link in itemInfo.Links)
        {
            Guard.IsNotNullOrWhiteSpace(link.Id);
            Guard.IsNotNull(link.Name);

            var linkedItemInfo = await Client.Mfs.StatAsync($"/ipfs/{link.Id}", cancellationToken);
            if (linkedItemInfo.IsDirectory && type.HasFlag(StorableType.Folder))
            {
                yield return new IpfsFolder(link.Id, link.Name, Client)
                {
                    Parent = this,
                };
            }
            else if (type.HasFlag(StorableType.File))
            {
                yield return new IpfsFile(link.Id, link.Name, Client)
                {
                    Parent = this,
                };
            }
        }
    }

    /// <inheritdoc/>
    public Task<Cid> GetCidAsync(CancellationToken cancellationToken) => Task.FromResult<Cid>(Id);
}