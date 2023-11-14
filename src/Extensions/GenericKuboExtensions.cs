using Ipfs;
using Ipfs.Http;

namespace OwlCore.Kubo;

/// <summary>
/// Extensions generics and objects with additional Kubo features.
/// </summary>
public static partial class GenericKuboExtensions
{
    /// <summary>
    /// Gets the CID of the provided <paramref name="serializable"/> object.
    /// </summary>
    /// <param name="serializable">The object to serialize into the Dag.</param>
    /// <param name="client">The Ipfs client to use.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns></returns>
    public static Task<Cid> GetCidAsync(this object serializable, IpfsClient client, CancellationToken cancellationToken)
    {
        return client.Dag.PutAsync(serializable, cancel: cancellationToken, pin: false);
    }
}
