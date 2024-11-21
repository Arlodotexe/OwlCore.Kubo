using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Storage;

namespace OwlCore.Kubo.Cache;

/// <summary>
/// A cached api layer for <see cref="ICoreApi"/>.
/// </summary>
public class CachedCoreApi : ICoreApi, IDelegable<ICoreApi>, IFlushable, IAsyncInit
{
    /// <summary>
    /// Creates a new instance of <see cref="CachedCoreApi"/>.
    /// </summary>
    /// <param name="cacheFolder">The folder to store cached data in.</param>
    /// <param name="inner">The inner <see cref="ICoreApi"/> to wrap around.</param>
    public CachedCoreApi(IModifiableFolder cacheFolder, ICoreApi inner)
    {
        Name = new CachedNameApi(cacheFolder) { Inner = inner.Name, KeyApi = inner.Key };
        Key = new CachedKeyApi(cacheFolder) { Inner = inner.Key };
        Inner = inner;
    }

    /// <summary>
    /// The inner, unwrapped core api to delegate to.
    /// </summary>
    public ICoreApi Inner { get; }

    /// <inheritdoc/>
    public IBitswapApi Bitswap => Inner.Bitswap;

    /// <inheritdoc/>
    public IBlockApi Block => Inner.Block;

    /// <inheritdoc/>
    public IBlockRepositoryApi BlockRepository => Inner.BlockRepository;

    /// <inheritdoc/>
    public IBootstrapApi Bootstrap => Inner.Bootstrap;

    /// <inheritdoc/>
    public IConfigApi Config => Inner.Config;

    /// <inheritdoc/>
    public IDagApi Dag => Inner.Dag;

    /// <inheritdoc/>
    public IDhtApi Dht => Inner.Dht;

    /// <inheritdoc/>
    public IDnsApi Dns => Inner.Dns;

    /// <inheritdoc/>
    public IFileSystemApi FileSystem => Inner.FileSystem;

    /// <inheritdoc/>
    public IMfsApi Mfs => Inner.Mfs;

    /// <inheritdoc/>
    public IGenericApi Generic => Inner.Generic;

    /// <inheritdoc/>
    public IKeyApi Key { get; }

    /// <inheritdoc/>
    public INameApi Name { get; }

    /// <inheritdoc/>
    public IPinApi Pin => Inner.Pin;

    /// <inheritdoc/>
    public IPubSubApi PubSub => Inner.PubSub;

    /// <inheritdoc/>
    public IStatsApi Stats => Inner.Stats;

    /// <inheritdoc/>
    public ISwarmApi Swarm => Inner.Swarm;

    /// <inheritdoc/>
    public bool IsInitialized { get; set; }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        await ((CachedNameApi)Name).FlushAsync(cancellationToken);

        await ((CachedNameApi)Name).SaveAsync(cancellationToken);
        await ((CachedKeyApi)Key).SaveAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try loading data from API
            await ((CachedKeyApi)Key).InitAsync(cancellationToken);
        }
        catch
        {
            // Load data from disk as fallback
            await ((CachedKeyApi)Key).LoadAsync(cancellationToken);
        }

        await ((CachedNameApi)Name).LoadAsync(cancellationToken);

        // Allow multiple initialization
        IsInitialized = true;
    }
}
