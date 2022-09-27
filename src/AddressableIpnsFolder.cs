using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// An addressable folder that resides on IPFS behind an IPNS Address.
/// </summary>
public class AddressableIpnsFolder : IpnsFolder, IAddressableFolder, IChainedAddressableStorable
{
    /// <summary>
    /// Creates a new instance of <see cref="AddressableIpfsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="name">The name of the file, provided by the parent folder.</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableIpnsFolder(string ipnsAddress, string name, IpfsClient client, IFolder[] parentChain)
        : base(ipnsAddress, client)
    {
        Name = name;
        Guard.IsGreaterThanOrEqualTo(parentChain.Length, 1);
        Path = $"{ipnsAddress}/{string.Join("/", parentChain.Select(x => x.Name))}";
        Id = Path;
        Name = name;
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