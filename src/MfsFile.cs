using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Kubo.Models;
using OwlCore.Storage;
using System.Text;
using System.Text.Json;

namespace OwlCore.Kubo;

/// <summary>
/// An file that resides in Kubo's Mutable Filesystem.
/// </summary>
public class MfsFile : IFile, IAddressableFile
{
    private readonly IpfsClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="MfsFile"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public MfsFile(string path, IpfsClient client)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        Path = path;
        _client = client;
        Name = GetFolderItemName(path);

        static string GetFolderItemName(string path)
        {
            var parts = path.Trim('/').Split('/').ToArray();
            return parts[parts.Length - 1];
        }
    }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(new MfsFolder(MfsFolder.GetParentPath(Path), _client));
    }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public string Id => Path;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var serialized = await _client.DoCommandAsync("files/stat", cancellationToken, Path, "long=true");
        var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(MfsFileStatData), ModelSerializer.Default);

        Guard.IsNotNull(result);

        var data = (MfsFileStatData)result;
        Guard.IsNotNullOrWhiteSpace(data.Hash);
        Guard.IsNotNull(data.Size);

        return new MfsStream(Path, (long)data.Size, _client) { InternalCanWrite = accessMode.HasFlag(FileAccess.Write) };
    }

    /// <summary>
    /// Flushes the file contents to disk and returns the CID of the folder contents.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation. Value is the CID of the file that was flushed to disk.</returns>
    public async Task<Cid> FlushAsync(CancellationToken cancellationToken = default)
    {
        var serialized = await _client.DoCommandAsync("files/flush", cancellationToken, Path);
        Guard.IsNotNullOrWhiteSpace(serialized);

        var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(FilesFlushResponse), ModelSerializer.Default);
        Guard.IsNotNull(result);

        var response = (FilesFlushResponse)result;
        return response.Cid;
    }
}
