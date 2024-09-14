using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// Implementations are capable of providing a CID for the current content by adding it ipfs.
/// </summary>
public partial interface IAddFileToGetCid : IStorable
{
    /// <summary>
    /// Gets the CID of the storable item.
    /// </summary>
    /// <param name="addFileOptions">The add file options to use when computing the cid for this storable.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns></returns>
    public Task<Cid> GetCidAsync(AddFileOptions addFileOptions, CancellationToken cancellationToken);
}
