using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;
using System.Runtime.CompilerServices;

namespace OwlCore.Kubo;

/// <summary>
/// A folder that resides on IPFS behind an IPNS Address.
/// </summary>
public class IpnsFolder : IMutableFolder, IChildFolder, IFastGetRoot, IFastGetItem, IFastGetItemRecursive
{
    private readonly IpfsClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "/ipns/ipfs.tech" or "/ipns/k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFolder(string ipnsAddress, IpfsClient client)
    {
        Guard.IsTrue(ipnsAddress.StartsWith("/ipns/"), nameof(ipnsAddress), "Value must start with /ipns/");

        _client = client;
        Id = ipnsAddress;
        Name = ipnsAddress == "/" ? "Mfs Root" : MfsFolder.GetFolderItemName(ipnsAddress);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// The parent directory, if any.
    /// </summary>
    internal IpnsFolder? Parent { get; init; } = null;

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        if (Parent is not null)
            return Task.FromResult<IFolder?>(Parent);

        return Task.FromResult<IFolder?>(Id == "/" ? null : new IpnsFolder(MfsFolder.GetParentPath(Id), _client));
    }

    /// <inheritdoc />
    public Task<IFolder?> GetRootAsync()
    {
        if (Id == "/")
            return Task.FromResult<IFolder?>(null);

        var parts = Id.Split('/').ToList();
        Guard.IsGreaterThanOrEqualTo(parts.Count, 2);

        return Task.FromResult<IFolder?>(new IpnsFolder(string.Join("/", parts.GetRange(0, 2)), _client));
    }

    /// <summary>
    /// The interval that IPNS should be checked for updates.
    /// </summary>
    public TimeSpan UpdateCheckInterval { get; } = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var itemInfo = await _client.FileSystem.ListFileAsync(Id, cancellationToken);
        Guard.IsTrue(itemInfo.IsDirectory);

        foreach (var link in itemInfo.Links)
        {
            Guard.IsNotNullOrWhiteSpace(link.Id);
            var item = await GetFileOrFolderFromId($"{Id}/{link.Name}", cancellationToken);

            if (item is IFolder && type.HasFlag(StorableType.Folder))
                yield return item;

            if (item is IFile && type.HasFlag(StorableType.File))
                yield return item;
        }
    }

    /// <inheritdoc />
    public Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default) => GetFileOrFolderFromId(id, cancellationToken);

    /// <inheritdoc />
    public Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default) => GetFileOrFolderFromId(id, cancellationToken);

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new TimerBasedIpnsWatcher(_client, this, UpdateCheckInterval));
    }

    private async Task<IStorableChild> GetFileOrFolderFromId(string path, CancellationToken cancellationToken = default)
    {
        var linkedItemInfo = await _client.FileSystem.ListFileAsync(path, cancellationToken);

        if (linkedItemInfo.IsDirectory)
            return new IpnsFolder(path, _client) { Parent = this, };
        else
            return new IpnsFile(path, _client) { Parent = this, };
    }
}