using Ipfs;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// Implementations are capable of providing a CID for their current content.
/// </summary>
public partial interface IGetCid : IStorable
{
    /// <summary>
    /// Gets the CID of the storable item.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns></returns>
    public Task<Cid> GetCidAsync(CancellationToken cancellationToken);
}
