using CommunityToolkit.Diagnostics;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;

namespace OwlCore.Kubo
{
    public partial class MfsFolder : IModifiableFolder
    {
        /// <summary>
        /// The interval that MFS should be checked for updates.
        /// </summary>
        public TimeSpan UpdateCheckInterval { get; } = TimeSpan.FromSeconds(10);

        /// <inheritdoc/>
        public async Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
        {
            Guard.IsNotNullOrWhiteSpace(item.Name);
            cancellationToken.ThrowIfCancellationRequested();

            await _client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{item.Name}", "recursive=true", "force=true");
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (fileToCopy is MfsFile mfsFile)
            {
                await _client.DoCommandAsync("files/cp", cancellationToken, arg: mfsFile.Path, $"arg={Path}");
            }
            else if (fileToCopy is IpfsFile ipfsFile)
            {
                await _client.DoCommandAsync("files/cp", cancellationToken, arg: ipfsFile.Id, $"arg={Path}");
            }
            else
            {
                // Manual file copy. Slower, but covers all other scenarios.
                using var sourceStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);

                if (sourceStream.CanSeek)
                    sourceStream.Seek(0, SeekOrigin.Begin);

                var file = await CreateFileAsync(fileToCopy.Name, overwrite, cancellationToken);
                using var destinationStream = await file.OpenStreamAsync(FileAccess.ReadWrite);

                await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);
            }

            return new MfsFile($"{Path}{fileToCopy.Name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (source is IAddressableFolder addressableSource)
            {
                Guard.IsTrue(addressableSource.Path.StartsWith(System.IO.Path.GetDirectoryName(fileToMove.Path) ?? string.Empty), nameof(source), $"{fileToMove.Id} does not exist in {source.Id}.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (fileToMove is MfsFile mfsFile)
            {
                await _client.DoCommandAsync("files/mv", cancellationToken, arg: mfsFile.Path, $"arg={Path}{fileToMove.Name}");

                return new MfsFile($"{Path}{fileToMove.Name}", _client);
            }
            else
            {
                // Manual move. Slower, but covers all other scenarios.
                var newFile = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
                await source.DeleteAsync(fileToMove, cancellationToken);

                return newFile;
            }
        }

        /// <inheritdoc/>
        public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (overwrite)
            {
                await _client.DoCommandAsync("files/rm", cancellationToken, $"{Path}{name}", "recursive=true");
            }

            await _client.DoCommandAsync("files/mkdir", cancellationToken, arg: $"{Path}{name}");

            return new MfsFolder($"{Path}{name}", _client);
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
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
