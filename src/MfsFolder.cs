using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Http;
using OwlCore.Kubo.Models;
using OwlCore.Storage;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OwlCore.Kubo;

/// <summary>
/// A folder that resides in Kubo's Mutable Filesystem.
/// </summary>
public partial class MfsFolder : IFolder, IChildFolder, IGetItem, IGetItemRecursive, IGetFirstByName, IGetRoot, IGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="MfsFolder"/>.
    /// </summary>
    /// <param name="client">The IPFS api to use for retrieving the content.</param>
    /// <param name="path">The MFS path to the folder.</param>
    public MfsFolder(string path, ICoreApi client)
    {
        Guard.IsNotNullOrWhiteSpace(path);

        // Add trailing slash if missing.
        if (!path.EndsWith("/"))
            path += "/";

        Path = path;
        Id = path;
        Client = client;
        Name = PathHelpers.GetFolderItemName(path);
    }

    /// <inheritdoc/>
    public virtual string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// The MFS path to the file. Relative to the root of MFS.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    protected ICoreApi Client { get; }

    /// <inheritdoc/>
    public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await Client.Mfs.ListAsync(Path, cancel: cancellationToken);

        foreach (var link in result ?? Enumerable.Empty<FileSystemNode>())
        {
            Guard.IsNotNullOrWhiteSpace(link.Id);
            var linkedItemInfo = await Client.Mfs.StatAsync($"/ipfs/{link.Id}", cancellationToken);

            if (linkedItemInfo.IsDirectory)
            {
                if (type.HasFlag(StorableType.Folder))
                    yield return new MfsFolder($"{Path}{link.Name}", Client);
            }
            else
            {
                if (type.HasFlag(StorableType.File))
                    yield return new MfsFile($"{Path}{link.Name}", Client);
            }
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IStorableChild> GetFirstByNameAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        var mfsPath = $"{Id}{name}";

        try
        {
            Guard.IsNotNullOrWhiteSpace(name);

            var data = await Client.Mfs.StatAsync(mfsPath, cancellationToken);

            return data.IsDirectory ? new MfsFolder(mfsPath, Client) : new MfsFile(mfsPath, Client);
        }
        catch (HttpRequestException httpRequestException) when (httpRequestException.Message.Contains("file does not exist"))
        {
            throw new FileNotFoundException();
        }
    }

    /// <inheritdoc/>
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(new MfsFolder(PathHelpers.GetParentPath(Path), Client));

    /// <inheritdoc/>
    public virtual Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(new MfsFolder("/", Client));

    /// <inheritdoc/>
    public virtual async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            Guard.IsNotNullOrWhiteSpace(id);

            var data = await Client.Mfs.StatAsync(id, cancellationToken);

            return data.IsDirectory ? new MfsFolder(id, Client) : new MfsFile(id, Client);
        }
        catch (HttpRequestException httpRequestException) when (httpRequestException.Message.Contains("file does not exist"))
        {
            throw new FileNotFoundException();
        }
    }

    /// <inheritdoc/>
    public virtual Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default) => GetItemAsync(id, cancellationToken);

    /// <summary>
    /// Flushes the file contents to disk and returns the CID of the folder contents.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation. Value is the CID of the file that was flushed to disk.</returns>
    public virtual async Task<Cid> FlushAsync(CancellationToken cancellationToken = default)
    {
        var serialized = await Client.Mfs.FlushAsync(Path, cancellationToken);
        Guard.IsNotNullOrWhiteSpace(serialized);

        var result = (FilesFlushResponse?)await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(FilesFlushResponse), ModelSerializer.Default, cancellationToken);

        // This field is always present if the operation was successful.
        Guard.IsNotNullOrWhiteSpace(result?.Cid);

        return result.Cid;
    }

    /// <inheritdoc/>
    public Task<Cid> GetCidAsync(CancellationToken cancellationToken) => FlushAsync(cancellationToken);
}