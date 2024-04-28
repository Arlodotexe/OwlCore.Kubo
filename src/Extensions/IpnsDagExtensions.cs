using Ipfs;
using Ipfs.CoreApi;

namespace OwlCore.Kubo.Extensions;

/// <summary>
/// Extensions for using the Dag with Ipns.
/// </summary>
public static partial class IpnsDagExtensions
{
    /// <summary>
    /// Retrieve and transform the data from the provided IPNS CID, then update the IPNS record.
    /// </summary>
    /// <typeparam name="TTransformType">The type of data to deserialize, transform and re-serialize.</typeparam>
    /// <param name="sourceDagCid">The ipns key to use when resolving the data</param>
    /// <param name="destinationKeyName">The name of the key to publish the transformed data to.</param>
    /// <param name="transform">The transformation to apply over the data before publishing.</param>
    /// <param name="nocache">Whether to use the cache when resolving ipns links.</param>
    /// <param name="ipnsLifetime">The lifetime of the published ipns entry. Other nodes will drop the record after this amount of time. Your node should be online to rebroadcast ipns at least once every iteration of this lifetime.</param>
    /// <param name="client">The client to use for calls to Kubo.</param>
    /// <param name="progress">Defines a provider for reporting progress.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Raised when the provided CID fails to resolve and execution cannot continue.</exception>
    public static async Task TransformIpnsDagAsync<TTransformType>(this ICoreApi client, Cid sourceDagCid, string destinationKeyName, Action<TTransformType> transform, bool nocache, TimeSpan ipnsLifetime, IProgress<IpnsUpdateState>? progress = null, CancellationToken cancellationToken = default)
    {
        Cid cid = sourceDagCid;

        // Resolve ipns
        if (cid.ContentType == "libp2p-key")
        {
            progress?.Report(IpnsUpdateState.ResolvingIpns);
            var ipnsResResult = await client.Name.ResolveAsync($"/ipns/{cid}", recursive: true, nocache: nocache, cancel: cancellationToken);

            cid = Cid.Decode(ipnsResResult.Replace("/ipfs/", ""));
        }

        // Resolve data
        progress?.Report(IpnsUpdateState.ResolvingDag);
        var data = await client.Dag.GetAsync<TTransformType>(cid, cancellationToken);
        if (data is null)
            throw new InvalidOperationException("Failed to resolve data from the provided CID.");
        
        // Update data
        progress?.Report(IpnsUpdateState.TransformingData);
        transform(data);

        // Save data
        progress?.Report(IpnsUpdateState.AddingToDag);
        var newCid = await client.Dag.PutAsync(data, cancel: cancellationToken);

        // Publish new cid to ipns
        progress?.Report(IpnsUpdateState.PublishingToIpns);
        await client.Name.PublishAsync(newCid, destinationKeyName, ipnsLifetime, cancel: cancellationToken);
    }

    /// <summary>
    /// Describes an update state for the IPNS update process.
    /// </summary>
    public enum IpnsUpdateState
    {
        /// <summary>
        /// No update state or not started.
        /// </summary>
        None,

        /// <summary>
        /// Data is being resolved by the DAG Api.
        /// </summary>
        ResolvingIpns,

        /// <summary>
        /// Data is being resolved by the DAG Api.
        /// </summary>
        ResolvingDag,

        /// <summary>
        /// Data is being transformed by the consumer.
        /// </summary>
        TransformingData,

        /// <summary>
        /// Data is being stored in the Ipfs DAG.
        /// </summary>
        AddingToDag,

        /// <summary>
        /// The new data is being published to IPNS.
        /// </summary>
        PublishingToIpns,
    }
}