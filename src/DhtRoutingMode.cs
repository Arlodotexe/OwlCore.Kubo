namespace OwlCore.Kubo;

/// <summary>
/// Specified a routing mode for the Kubo DHT.
/// </summary>
public enum DhtRoutingMode
{
    /// <summary>
    ///  Your node will use NO routing system. You'll have to explicitly connect to peers that have the content you're looking for.
    /// </summary>
    None,

    /// <summary>
    /// Your node will use the public IPFS DHT (aka "Amino") and parallel IPNI routers.
    /// </summary>
    /// <remarks> Will accelerate some types of routing with Delegated Routing V1 HTTP API introduced in IPIP-337 in addition to the Amino DHT. By default, an instance of IPNI at https://cid.contact is used.</remarks>
    Auto,

    /// <summary>
    /// Your node will behave as in "auto" but without running a DHT server.
    /// </summary>
    /// <remarks> Will accelerate some types of routing with Delegated Routing V1 HTTP API introduced in IPIP-337 in addition to the Amino DHT. By default, an instance of IPNI at https://cid.contact is used.</remarks>
    AutoClient,

    /// <summary>
    /// In the normal DHT mode IPFS can retrieve content from any peer and seed content to other peers outside your local network. Your node will ONLY use the Amino DHT (no IPNI routers).
    /// </summary>
    /// <remarks>
    /// If you're working with constrained resources, use <see cref="DhtClient"/>.
    /// The accelerated DHT is available for devices with extra resources (such as a desktop), trading these for faster and more reliable content routing. 
    /// </remarks>
    Dht,

    /// <summary>
    /// In server mode, your node will query other peers for DHT records, and will respond to requests from other peers (both requests to store records and requests to retrieve records).
    /// </summary>
    /// <remarks>
    /// Please do not set this unless you're sure your node is reachable from the public network.
    /// </remarks>
    DhtServer,

    /// <summary>
    /// In client mode, your node will query the DHT as a client but will not respond to requests from other peers. This mode is less resource-intensive than server mode.
    /// </summary>
    /// <remarks>
    /// To further optimize IPFS for devices with constrained resources, try setting the 'lowpower' config profile.
    /// </remarks>
    DhtClient,

    /// <summary>
    /// All default routers are disabled, and only ones defined in Kubo's 'Routing.Routers' config will be used.
    /// </summary>
    Custom,
}