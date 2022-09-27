using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// An addressable file that resides on IPFS.
/// </summary>
public class AddressableIpfsFile : IpfsFile, IAddressableFile, IChainedAddressableStorable
{
    /// <summary>
    /// Creates a new instance of <see cref="IpfsFile"/>.
    /// </summary>
    /// <param name="cid">The CID of the file, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
    /// <param name="name">The name of the file.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableIpfsFile(Cid cid, string name, IpfsClient client, IFolder[] parentChain)
        : base(cid, name, client)
    {
        Guard.IsGreaterThanOrEqualTo(parentChain.Length, 1);

        if (parentChain.Length == 1)
            Path = $"/ipfs/{cid}/{name}";
        else
            Path = $"/ipfs/{cid}/{string.Join("/", parentChain.Select(x => x.Name))}/{name}";

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