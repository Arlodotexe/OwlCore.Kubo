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

            await Client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{item.Name}", "recursive=true", "force=true");
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(MfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Client.DoCommandAsync("files/cp", cancellationToken, arg: fileToCopy.Path, $"arg={Path}");
            return new MfsFile($"{Path}{fileToCopy.Name}", Client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(IpfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Client.DoCommandAsync("files/cp", cancellationToken, arg: $"/ipfs/{fileToCopy.Id}", $"arg={Path}");
            return new MfsFile($"{Path}{fileToCopy.Name}", Client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(IpnsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cid = await fileToCopy.GetCidAsync(cancellationToken);
            await Client.DoCommandAsync("files/cp", cancellationToken, arg: $"/ipfs/{cid}", $"arg={Path}");

            return new MfsFile($"{Path}{fileToCopy.Name}", Client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> MoveFromAsync(MfsFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Client.DoCommandAsync("files/mv", cancellationToken, arg: fileToMove.Path, $"arg={Path}{fileToMove.Name}");
            return new MfsFile($"{Path}{fileToMove.Name}", Client);
        }

        /// <inheritdoc/>
        public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (overwrite)
            {
                await Client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{name}", "recursive=true");
            }

            try
            {
                await Client.DoCommandAsync("files/mkdir", cancellationToken, arg: $"{Path}{name}");
            }
            catch (Exception ex) when (ex.Message.ToLower().Contains("file already exists"))
            {
                // Ignored, return existing path if exists
            }

            return new MfsFolder($"{Path}{name}", Client);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            await Client.UploadAsync("files/write", CancellationToken.None, new MemoryStream(), null, $"arg={Path}{name}", $"create=true", overwrite ? $"truncate=true" : string.Empty);

            return new MfsFile($"{Path}{name}", Client);
        }

        /// <inheritdoc/>
        public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFolderWatcher>(new TimerBasedMfsWatcher(Client, this, UpdateCheckInterval));
        }
    }
}
