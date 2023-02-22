using CommunityToolkit.Diagnostics;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;

namespace OwlCore.Kubo
{
    public partial class MfsFolder : IModifiableFolder, IFastFileMove<MfsFile>, IFastFileCopy<MfsFile>, IFastFileCopy<IpfsFile>, IFastFileCopy<IpnsFile>
    {
        /// <summary>
        /// The interval that MFS should be checked for updates.
        /// </summary>
        public TimeSpan UpdateCheckInterval { get; } = TimeSpan.FromSeconds(10);

        /// <inheritdoc/>
        public async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.IsNotNullOrWhiteSpace(item.Name);

            await _client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{item.Name}", "recursive=true", "force=true");
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(MfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _client.DoCommandAsync("files/cp", cancellationToken, arg: fileToCopy.Path, $"arg={Path}");
            return new MfsFile($"{Path}{fileToCopy.Name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(IpfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _client.DoCommandAsync("files/cp", cancellationToken, arg: $"/ipfs/{fileToCopy.Id}", $"arg={Path}");
            return new MfsFile($"{Path}{fileToCopy.Name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(IpnsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cid = await fileToCopy.ResolveCidAsync(cancellationToken);
            await _client.DoCommandAsync("files/cp", cancellationToken, arg: $"/ipfs/{cid}", $"arg={Path}");

            return new MfsFile($"{Path}{fileToCopy.Name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> MoveFromAsync(MfsFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _client.DoCommandAsync("files/mv", cancellationToken, arg: fileToMove.Path, $"arg={Path}{fileToMove.Name}");
            return new MfsFile($"{Path}{fileToMove.Name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (overwrite)
            {
                await _client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{name}", "recursive=true");
            }

            await _client.DoCommandAsync("files/mkdir", cancellationToken, arg: $"{Path}{name}");

            return new MfsFolder($"{Path}{name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            await _client.UploadAsync("files/write", CancellationToken.None, new MemoryStream(), null, $"arg={Path}{name}", $"create=true", overwrite ? $"truncate=true" : string.Empty);

            return new MfsFile($"{Path}{name}", _client);
        }

        /// <inheritdoc/>
        public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFolderWatcher>(new TimerBasedMfsWatcher(_client, this, UpdateCheckInterval));
        }
    }
}
