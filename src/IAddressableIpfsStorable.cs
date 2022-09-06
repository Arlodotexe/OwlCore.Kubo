using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// The properties used by all IAddressableStorable implementations for Ipfs.
/// </summary>
public interface IAddressableIpfsStorable : IAddressableStorable
{
    /// <summary>
    /// A list of parent folders. The first item is the root, the last item is the parent.
    /// </summary>
    public IFolder[] ParentChain { get; set; }
}