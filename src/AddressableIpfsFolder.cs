using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// An addressable folder that resides on IPFS.
/// </summary>
public class AddressableIpfsFolder : IpfsFolder, IAddressableFolder, IChainedAddressableStorable
{
    /// <summary>
    /// Creates a new instance of <see cref="AddressableIpfsFolder"/>.
    /// </summary>
    /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableIpfsFolder(Cid cid, IpfsClient client, IFolder[] parentChain)
        : base(cid, client)
    {
        Guard.IsGreaterThanOrEqualTo(parentChain.Length, 1);
        Path = $"/ipfs/{cid}/{string.Join("/", parentChain.Select(x => x.Name))}";
        ParentChain = parentChain;
    }

    /// <summary>
    /// Creates a new instance of <see cref="AddressableIpfsFolder"/>.
    /// </summary>
    /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="name">The name of the folder.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableIpfsFolder(Cid cid, string name, IpfsClient client, IFolder[] parentChain)
        : base(cid, name, client)
    {
        Guard.IsGreaterThanOrEqualTo(parentChain.Length, 1);
        Path = $"/ipfs/{cid}/{string.Join("/", parentChain.Select(x => x.Name))}";
        ParentChain = parentChain;
    }

    /// <summary>
    /// A list of parent folders. The first item is the root, the last item is the parent.
    /// </summary>
    public IFolder[] ParentChain { get; set; }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ParentChain.Length == 0 ? null : ParentChain[ParentChain.Length - 1]);
    }

    /// <inheritdoc/>
    public string Path { get; }
}