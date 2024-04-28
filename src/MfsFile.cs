using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// An file that resides in Kubo's Mutable Filesystem.
/// </summary>
public class MfsFile : IFile, IChildFile, IGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="MfsFile"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="client">The client to use for interacting with ipfs.</param>
    public MfsFile(string path, ICoreApi client)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        Path = path;
        Client = client;
        Name = GetFolderItemName(path);

        static string GetFolderItemName(string path)
        {
            var parts = path.Trim('/').Split('/').ToArray();
            return parts[^1];
        }
    }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(new MfsFolder(PathHelpers.GetParentPath(Path), Client));
    }

    /// <summary>
    /// The MFS path to the file. Relative to the root of MFS.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc/>
    public string Id => Path;

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    protected ICoreApi Client { get; }

    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var data = await Client.Mfs.StatAsync(Path, cancellationToken);

        Guard.IsNotNull(data);
        Guard.IsNotNullOrWhiteSpace(data.Hash?.ToString());
        Guard.IsNotNull(data.Size);

        return new MfsStream(Path, (long)data.Size, Client) { InternalCanWrite = accessMode.HasFlag(FileAccess.Write) };
    }

    /// <summary>
    /// Flushes the file contents to disk and returns the CID of the folder contents.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation. Value is the CID of the file that was flushed to disk.</returns>
    public async Task<Cid> FlushAsync(CancellationToken cancellationToken = default)
    {
        var cid = await Client.Mfs.FlushAsync(Path, cancellationToken);
        Guard.IsNotNullOrWhiteSpace(cid);

        return cid;
    }

    /// <inheritdoc/>
    public Task<Cid> GetCidAsync(CancellationToken cancellationToken) => FlushAsync(cancellationToken);
}
