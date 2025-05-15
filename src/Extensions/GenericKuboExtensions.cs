using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;

namespace OwlCore.Kubo;

/// <summary>
/// Extensions generics and objects with additional Kubo features.
/// </summary>
public static partial class GenericKuboExtensions
{
    /// <summary>
    /// Resolves the provided <paramref name="cid"/> if it is an Ipns address and retrieves the content from the DAG.
    /// </summary>
    /// <param name="cid">The cid of the DAG object to retrieve.</param>
    /// <param name="client">A client that can be used to communicate with Ipfs.</param>
    /// <param name="nocache">Whether to use cached entries if Ipns is resolved.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>The deserialized DAG content, if any.</returns>
    public static async Task<(TResult? Result, Cid ResultCid)> ResolveDagCidAsync<TResult>(this Cid cid, ICoreApi client, bool nocache, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (cid.ContentType == "libp2p-key")
        {
            var ipnsResResult = await client.Name.ResolveAsync($"/ipns/{cid}", recursive: true, nocache: nocache, cancel: cancellationToken);

            cid = Cid.Decode(ipnsResResult.Replace("/ipfs/", ""));
        }

        var res = await client.Dag.GetAsync<TResult>(cid, cancel: cancellationToken);

        Guard.IsNotNull(res);
        return (res, cid);
    }

    /// <summary>
    /// Resolves the provided <paramref name="cid"/> if it is an Ipns address and retrieves the content from the DAG.
    /// </summary>
    /// <param name="cid">The cid of the DAG object to retrieve.</param>
    /// <param name="client">A client that can be used to communicate with Ipfs.</param>
    /// <param name="nocache">Whether to use cached entries if Ipns is resolved.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>The deserialized DAG content, if any.</returns>
    public static async Task<(TResult? Result, Cid ResultCid)> ResolveDagCidAsync<TResult>(this ICoreApi client, Cid cid, bool nocache, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (cid.ContentType == "libp2p-key")
        {
            var ipnsResResult = await client.Name.ResolveAsync($"/ipns/{cid}", recursive: true, nocache: nocache, cancel: cancellationToken);

            cid = Cid.Decode(ipnsResResult.Replace("/ipfs/", ""));
        }

        var res = await client.Dag.GetAsync<TResult>(cid, cancel: cancellationToken);

        return (res, cid);
    }

    /// <summary>
    /// Resolves the provided <paramref name="cids"/> as Ipns addresses and retrieves the content from the DAG.
    /// </summary>
    /// <typeparam name="TResult">The type to deserialize to.</typeparam>
    /// <param name="cids">The IPNS CIDs of the Dag objects to retrieve.</param>
    /// <param name="client">A client that can be used to communicate with Ipfs.</param>
    /// <param name="nocache">Whether to use cached entries if Ipns is resolved.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>An async enumerable that yields the requested data.</returns>
    public static IAsyncEnumerable<(TResult? Result, Cid ResultCid)> ResolveDagCidAsync<TResult>(this IEnumerable<Cid> cids, ICoreApi client, bool nocache, CancellationToken cancellationToken = default)
        => cids
            .ToAsyncEnumerable()
            .SelectAwaitWithCancellation(async (cid, cancel) => await cid.ResolveDagCidAsync<TResult>(client, nocache, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancel).Token));

    /// <summary>
    /// Resolves the provided <paramref name="cids"/> as Ipns addresses and retrieves the content from the DAG.
    /// </summary>
    /// <typeparam name="TResult">The type to deserialize to.</typeparam>
    /// <param name="cids">The IPNS CIDs of the Dag objects to retrieve.</param>
    /// <param name="client">A client that can be used to communicate with Ipfs.</param>
    /// <param name="nocache">Whether to use cached entries if Ipns is resolved.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>An async enumerable that yields the requested data.</returns>
    public static IAsyncEnumerable<(TResult? Result, Cid ResultCid)> ResolveDagCidAsync<TResult>(this ICoreApi client, IEnumerable<Cid> cids, bool nocache, CancellationToken cancellationToken = default)
        => cids
            .ToAsyncEnumerable()
            .SelectAwaitWithCancellation(async (cid, cancel) => await cid.ResolveDagCidAsync<TResult>(client, nocache, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancel).Token));

    /// <summary>
    /// Creates an ipns key using a temporary name, then renames it to match the Id of the key.
    /// </summary>
    /// <remarks>
    /// Enables pushing to ipns without additional API calls to convert between ipns cid and name.
    /// </remarks>
    /// <param name="keyApi">The key api to use for accessing ipfs keys.</param>
    /// <param name="size">The size of the key to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>A task containing the created key.</returns>
    public static async Task<IKey> CreateKeyWithNameOfIdAsync(this IKeyApi keyApi, int size = 4096, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var key = await keyApi.CreateAsync(name: "temp", "ed25519", size, cancellationToken);

        // Rename key name to the key id
        return await keyApi.RenameAsync("temp", $"{key.Id}", cancellationToken);
    }

    /// <summary>
    /// Gets a key by name, or creates it if it does not exist.
    /// </summary>
    /// <param name="keyApi">The API to use for keys.</param>
    /// <param name="keyName">The name of the key to get or create.</param>
    /// <param name="size">The size of the key to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns></returns>
    public static async Task<IKey> GetOrCreateKeyAsync(this IKeyApi keyApi, string keyName, int size = 4096, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Get or create ipns key
        var keys = await keyApi.ListAsync(cancellationToken);
        if (keys.FirstOrDefault(x => x.Name == keyName) is not { } key)
        {
            // Key does not exist, create it.
            key = await keyApi.CreateAsync(keyName, "ed25519", size, cancellationToken);
        }

        return key;
    }

    /// <summary>
    /// Gets a key by name, or creates it if it does not exist.
    /// </summary>
    /// <param name="client">The client to use for communicating with the ipfs network.</param>
    /// <param name="keyName">The name of the key to get or create.</param>
    /// <param name="ipnsLifetime">The lifetime this ipns key should stay alive before needing to be rebroadcast by this node.</param>
    /// <param name="nocache">Whether to use Kubo's cache when resolving ipns keys.</param>
    /// <param name="size">The size of the key to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <param name="getDefaultValue">Given the created ipns key, provides the default value to be published to it.</param>
    public static async Task<(IKey Key, TResult Value)> GetOrCreateKeyAsync<TResult>(this ICoreApi client, string keyName, Func<IKey, TResult> getDefaultValue, TimeSpan ipnsLifetime, bool nocache, int size = 4096, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Get or create ipns key
        var keys = await client.Key.ListAsync(cancellationToken);
        if (keys.FirstOrDefault(x => x.Name == keyName) is not { } key)
        {
            // Key does not exist, create it.
            key = await client.Key.CreateAsync(keyName, "ed25519", size, cancellationToken);
            Guard.IsNotNull(key);

            // Get default value and cid
            var defaultValue = getDefaultValue(key);
            Guard.IsNotNull(defaultValue);

            // Publish default value cid
            var cid = await client.Dag.PutAsync(defaultValue, cancel: cancellationToken, pin: true);
            await client.Name.PublishAsync(cid, key.Name, ipnsLifetime, cancellationToken);

            return (key, defaultValue);
        }
        else
        {
            var (existingValue, _) = await key.Id.ResolveDagCidAsync<TResult>(client, nocache, cancellationToken);
            Guard.IsNotNull(existingValue);
            return (key, existingValue);
        }
    }
}
