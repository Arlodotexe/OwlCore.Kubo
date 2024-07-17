using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Storage;

namespace OwlCore.Kubo.Cache;

/// <summary>
/// A cached api layer for <see cref="IKeyApi"/>.
/// </summary>
public class CachedKeyApi : SettingsBase, IKeyApi, IDelegable<IKeyApi>, IAsyncInit
{
    /// <summary>
    /// The cached record type for created or resolved <see cref="IKey"/>s.
    /// </summary>
    public record CachedKeyInfo(string Name, string Id);
    
    private record ApiKeyInfo(string Name, Cid Id) : IKey;

    /// <summary>
    /// Creates a new instance of <see cref="CachedKeyApi"/>.
    /// </summary>
    /// <param name="folder">The folder to store cached name resolutions.</param>
    public CachedKeyApi(IModifiableFolder folder)
        : base(folder, KuboCacheSerializer.Singleton)
    {
        FlushDefaultValues = false;
    }

    /// <summary>
    /// The resolved ipns keys.
    /// </summary>
    public List<CachedKeyInfo> Keys
    {
        get => GetSetting(() => new List<CachedKeyInfo>());
        set => SetSetting(value);
    }

    /// <inheritdoc />
    public required IKeyApi Inner { get; init; }

    /// <inheritdoc />
    public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default)
    {
        var res = await Inner.CreateAsync(name, keyType, size, cancel);

        var existing = Keys.FirstOrDefault(x => x.Name == res.Name);
        if (existing is not null)
            Keys.Remove(existing);

        Keys.Add(new CachedKeyInfo(Name: res.Name, Id: res.Id));
        return res;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default) => Task.FromResult<IEnumerable<IKey>>(Keys.Select(x=> new ApiKeyInfo(x.Name, x.Id)));

    /// <inheritdoc />
    public Task<IKey?> RemoveAsync(string name, CancellationToken cancel = default) => Inner.RemoveAsync(name, cancel);

    /// <inheritdoc />
    public Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default) => Inner.RenameAsync(oldName, newName, cancel);

    /// <inheritdoc />
    public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default) => Inner.ExportAsync(name, password, cancel);

    /// <inheritdoc />
    public Task<IKey> ImportAsync(string name, string pem, char[]? password = null, CancellationToken cancel = default) => Inner.ImportAsync(name, pem, password, cancel);

    /// <summary>
    /// Initializes local values with fresh data from the API.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        var res = await Inner.ListAsync(cancellationToken);
        Keys = res.Select(x => new CachedKeyInfo(x.Name, x.Id)).ToList();

        await SaveAsync(cancellationToken);

        // Allow multiple initialization
        IsInitialized = true;
    }

    /// <inheritdoc/>
    public bool IsInitialized { get; private set; }
}