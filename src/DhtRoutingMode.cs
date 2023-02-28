namespace OwlCore.Kubo;

/// <summary>
/// Specified a routing mode for the Kubo DHT.
/// </summary>
public enum DhtRoutingMode
{
    /// <summary>
    /// This is the default routing mode. In the normal, DHT mode IPFS can retrieve content from any peer and seed content to other peers outside your local network.
    /// </summary>
    /// <remarks>
    /// This is the recommended choice for most devices.
    /// If you're working with constrained resources, use <see cref="DhtClient"/>.
    /// The accelerated DHT is available for devices with extra resources (such as a desktop), trading these for faster and more reliable content routing. 
    /// </remarks>
    Dht,

    /// <summary>
    /// Ideal for devices with constrained resources. In the "dhtclient" mode, IPFS can ask other peers for content, but it will not seed content to peers outside of your local network.
    /// </summary>
    /// <remarks>
    /// To further optimize IPFS for devices with constrained resources, try setting the 'lowpower' config profile.
    /// </remarks>
    DhtClient,
}