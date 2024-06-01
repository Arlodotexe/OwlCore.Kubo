using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Extensions;
using OwlCore.Storage;

namespace OwlCore.Kubo.Cache;

/// <summary>
/// A cache layer for <see cref="INameApi"/>. No API calls will be made to Kubo until <see cref="FlushAsync"/> is called.
/// </summary>
/// <remarks>
/// Recommended for code that pushes to ipns in bursts of updates, allowing you to defer the publication of the final ipns value. 
/// </remarks>
public class CachedNameApi : SettingsBase, INameApi, IDelegable<INameApi>, IFlushable
{
    private readonly SemaphoreSlim _cacheUpdateMutex = new(1, 1);

    /// <summary>
    /// The cached record for a published path name in a <see cref="CachedNameApi"/>.
    /// </summary>
    public record PublishedPathName(string path, bool resolve, string key, TimeSpan? lifetime, NamedContent returnValue);

    /// <summary>
    /// The cached record for a published cid name in a <see cref="CachedNameApi"/>.
    /// </summary>
    public record PublishedCidName(Cid id, string key, TimeSpan? lifetime, NamedContent returnValue);

    /// <summary>
    /// The cached record for a resolved name in a <see cref="CachedNameApi"/>.
    /// </summary>
    public record ResolvedName(string name, bool recursive, string returnValue);

    /// <summary>
    /// Creates a new instance of <see cref="CachedNameApi"/>.
    /// </summary>
    /// <param name="folder">The folder to store cached name resolutions.</param>
    public CachedNameApi(IModifiableFolder folder)
        : base(folder, KuboCacheSerializer.Singleton)
    {
        FlushDefaultValues = false;
    }

    /// <summary>
    /// The names that have been resolved.
    /// </summary>
    public List<ResolvedName> ResolvedNames
    {
        get => GetSetting(() => new List<ResolvedName>());
        set => SetSetting(value);
    }

    /// <summary>
    /// The latest named content that has been published via <see cref="INameApi.PublishAsync(Cid,string,TimeSpan?,CancellationToken)"/>.
    /// </summary>
    public List<PublishedCidName> PublishedCidNamedContent
    {
        get => GetSetting(() => new List<PublishedCidName>());
        set => SetSetting(value);
    }

    /// <summary>
    /// The latest named content that has been published via <see cref="INameApi.PublishAsync(string,bool,string,TimeSpan?,CancellationToken)"/>.
    /// </summary>
    public List<PublishedPathName> PublishedStringNamedContent
    {
        get => GetSetting(() => new List<PublishedPathName>());
        set => SetSetting(value);
    }

    /// <inheritdoc />
    public required INameApi Inner { get; init; }

    /// <summary>
    /// The Key API to use for getting existing keys that can be published to.
    /// </summary>
    public required IKeyApi KeyApi { get; init; }

    /// <summary>
    /// Flushes the cached requests to the underlying resource.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        using (await _cacheUpdateMutex.DisposableWaitAsync(cancellationToken))
        {
            foreach (var item in PublishedCidNamedContent)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine($"Flushing key {item.key} with value {item.id}");

                // Publish to ipfs
                var result = await Inner.PublishAsync(item.id, item.key, item.lifetime, cancellationToken);

                // Verify result matches original returned data
                _ = Guard.Equals(result.ContentPath, item.returnValue.ContentPath);
                _ = Guard.Equals(result.NamePath, item.returnValue.NamePath);

                // Update cache
                PublishedCidNamedContent.Remove(item);
                PublishedCidNamedContent.Add(item with { returnValue = result });
            }

            foreach (var item in PublishedStringNamedContent)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine($"Flushing key {item.key} with value {item.path}");

                // Publish to ipfs
                var result = await Inner.PublishAsync(item.path, item.resolve, item.key, item.lifetime, cancellationToken);

                // Verify result matches original returned data
                _ = Guard.Equals(result.ContentPath, item.returnValue.ContentPath);
                _ = Guard.Equals(result.NamePath, item.returnValue.NamePath);

                // Update cache
                PublishedStringNamedContent.Remove(item);
                PublishedStringNamedContent.Add(item with { returnValue = result });
            }
        }
    }

    /// <inheritdoc />
    public async Task<NamedContent> PublishAsync(string path, bool resolve = true, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default)
    {
        using (await _cacheUpdateMutex.DisposableWaitAsync(cancel))
        {
            if (PublishedStringNamedContent.FirstOrDefault(x => x.key == key) is { } existing)
                PublishedStringNamedContent.Remove(existing);

            var keys = await KeyApi.ListAsync(cancel);
            var existingKey = keys.FirstOrDefault(x => x.Name == key);
            var keyId = existingKey?.Id;

            NamedContent published = new() { ContentPath = path, NamePath = $"/ipns/{keyId}" };

            PublishedStringNamedContent.Add(new(path, resolve, key, lifetime, published));
            return published;
        }
    }

    /// <inheritdoc />
    public async Task<NamedContent> PublishAsync(Cid id, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default)
    {
        using (await _cacheUpdateMutex.DisposableWaitAsync(cancel))
        {
            if (PublishedCidNamedContent.FirstOrDefault(x => x.key == key) is { } existing)
                PublishedCidNamedContent.Remove(existing);

            var keys = await KeyApi.ListAsync(cancel);
            var existingKey = keys.FirstOrDefault(x => x.Name == key);
            var keyId = existingKey?.Id;

            NamedContent published = new() { ContentPath = $"/ipfs/{id}", NamePath = $"/ipns/{keyId}" };
            PublishedCidNamedContent.Add(new(id, key, lifetime, published));

            return published;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Using nocache = true here forces immediate name resolution via API, falling back to cache on failure.
    /// <para/> 
    /// Using nocache = false here checks the cache first, and falls back to the API if not found.
    /// </remarks>
    public async Task<string> ResolveAsync(string name, bool recursive = false, bool nocache = false, CancellationToken cancel = default)
    {
        using (await _cacheUpdateMutex.DisposableWaitAsync(cancel))
        {
            if (nocache)
            {
                try
                {
                    // Don't resolve with cache, but still save resolved data to cache.
                    var resToCache = await Inner.ResolveAsync(name, recursive, nocache, cancel);

                    var existing = ResolvedNames.FirstOrDefault(x => x.name == name);
                    if (existing is not null)
                        ResolvedNames.Remove(existing);

                    ResolvedNames.Add(new(name, recursive, resToCache));

                    return resToCache;
                }
                catch
                {
                    // request failed, continue with cache anyway
                }
            }

            // Check if name is in published cache
            if (PublishedCidNamedContent.FirstOrDefault(x => x.returnValue.NamePath is not null && (name.Contains(x.returnValue.NamePath) || x.returnValue.NamePath.Contains(name))) is { } publishedCidNamedContent)
            {
                if (publishedCidNamedContent.returnValue.ContentPath is not null)
                    return publishedCidNamedContent.returnValue.ContentPath;
            }

            // Check in other published cache
            if (PublishedStringNamedContent.FirstOrDefault(x => x.returnValue.NamePath is not null && (name.Contains(x.returnValue.NamePath) || x.returnValue.NamePath.Contains(name))) is { } publishedStringNamedContent)
            {
                if (publishedStringNamedContent.returnValue.ContentPath is not null)
                    return publishedStringNamedContent.returnValue.ContentPath;
            }

            // Check if previously resolved.
            if (ResolvedNames.FirstOrDefault(x => x.name == name) is { } resolvedName)
                return resolvedName.returnValue;

            // If not, resolve the name and cache.
            var result = await Inner.ResolveAsync(name, recursive, nocache, cancel);
            ResolvedNames.Add(new(name, recursive, result));

            return result;
        }
    }
}
