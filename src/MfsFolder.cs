using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Kubo.Models;
using OwlCore.Storage;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OwlCore.Kubo
{
    /// <summary>
    /// A folder that resides in Kubo's Mutable Filesystem.
    /// </summary>
    public partial class MfsFolder : IFolder, IChildFolder, IFastGetItem, IFastGetItemRecursive, IFastGetFirstByName, IFastGetRoot
    {
        private readonly IpfsClient _client;

        /// <summary>
        /// Creates a new instance of <see cref="MfsFolder"/>.
        /// </summary>
        /// <param name="client">The IPFS Client to use for retrieving the content.</param>
        /// <param name="path">The MFS path to the folder.</param>
        public MfsFolder(string path, IpfsClient client)
        {
            Guard.IsNotNullOrWhiteSpace(path);

            // Add trailing slash if missing.
            if (!path.EndsWith("/"))
                path += "/";

            Path = path;
            Id = path;
            _client = client;
            Name = GetFolderItemName(path);
        }

        /// <inheritdoc/>
        public virtual string Id { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// The MFS path to the file. Relative to the root of MFS.
        /// </summary>
        public string Path { get; }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var serialized = await _client.DoCommandAsync("files/ls", cancellationToken, Path, "long=true");
            var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(MfsFileContentsBody), ModelSerializer.Default, cancellationToken);

            Guard.IsNotNull(result);

            var data = (MfsFileContentsBody)result;

            foreach (var link in data.Entries ?? Enumerable.Empty<MfsFileData>())
            {
                Guard.IsNotNullOrWhiteSpace(link.Hash);
                var linkedItemInfo = await _client.FileSystem.ListFileAsync(link.Hash, cancellationToken);

                if (linkedItemInfo.IsDirectory)
                {
                    if (type.HasFlag(StorableType.Folder))
                        yield return new MfsFolder($"{Path}{link.Name}", _client);
                }
                else
                {
                    if (type.HasFlag(StorableType.File))
                        yield return new MfsFile($"{Path}{link.Name}", _client);
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

                var serialized = await _client.DoCommandAsync("files/stat", cancellationToken, mfsPath, "long=true");
                var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(MfsFileStatData), ModelSerializer.Default, cancellationToken);

                Guard.IsNotNull(result);

                var data = (MfsFileStatData)result;
                Guard.IsNotNullOrWhiteSpace(data.Type);

                return data.Type == "directory" ? new MfsFolder(mfsPath, _client) : new MfsFile(mfsPath, _client);
            }
            catch (HttpRequestException httpRequestException) when (httpRequestException.Message.Contains("file does not exist"))
            {
                throw new FileNotFoundException();
            }
        }

        /// <inheritdoc/>
        public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(new MfsFolder(GetParentPath(Path), _client));

        /// <inheritdoc/>
        public Task<IFolder?> GetRootAsync() => Task.FromResult<IFolder?>(new MfsFolder("/", _client));

        /// <inheritdoc/>
        public virtual async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                Guard.IsNotNullOrWhiteSpace(id);

                var serialized = await _client.DoCommandAsync("files/stat", cancellationToken, id, "long=true");
                var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(MfsFileStatData), ModelSerializer.Default, cancellationToken);

                Guard.IsNotNull(result);

                var data = (MfsFileStatData)result;
                Guard.IsNotNullOrWhiteSpace(data.Type);

                return data.Type == "directory" ? new MfsFolder(id, _client) : new MfsFile(id, _client);
            }
            catch (HttpRequestException httpRequestException) when (httpRequestException.Message.Contains("file does not exist"))
            {
                throw new FileNotFoundException();
            }
        }

        /// <inheritdoc/>
        public virtual Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default) => GetItemAsync(id, cancellationToken);

        /// <summary>
        /// Flushes the folder contents to disk and returns the CID of the folder contents.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation. Value is the CID of the folder that was flushed to disk.</returns>
        public virtual async Task<Cid?> FlushAsync(CancellationToken cancellationToken = default)
        {
            var serialized = await _client.DoCommandAsync("files/flush", cancellationToken, Path);
            Guard.IsNotNullOrWhiteSpace(serialized);

            var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(FilesFlushResponse), ModelSerializer.Default, cancellationToken);
            Guard.IsNotNull(result);

            var response = (FilesFlushResponse)result;
            if (response.Cid is null)
                return null;

            return response.Cid;
        }

        internal static string GetFolderItemName(string path)
        {
            var parts = path.Trim('/').Split('/').ToArray();
            return parts[^1];
        }

        internal static string GetParentPath(string relativePath)
        {
            // If the provided path is the root.
            if (relativePath.Trim('/').Split('/').Count() == 1)
                return "/";

            var directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

            // Path.GetDirectoryName() treats strings that end with a directory separator as a directory. If there's no trailing slash, it's treated as a file.
            var isFolder = relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString());

            // Run it twice for folders. The first time only shaves off the trailing directory separator.
            var parentDirectoryName = isFolder ? System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(relativePath)) : System.IO.Path.GetDirectoryName(relativePath);

            // It also doesn't return a string that has a path separator at the end.
            return parentDirectoryName?.Replace('\\', '/') + (isFolder ? directorySeparatorChar : string.Empty) ?? string.Empty;
        }

        internal static string GetParentDirectoryName(string relativePath)
        {
            // If the provided path is the root.
            if (System.IO.Path.GetPathRoot(relativePath)?.Replace('\\', '/') == relativePath)
                return relativePath;

            var directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

            var parentPath = GetParentPath(relativePath);
            var parentParentPath = GetParentPath(parentPath);

            return parentPath.Replace(parentParentPath, "").TrimEnd(directorySeparatorChar);
        }
    }
}
