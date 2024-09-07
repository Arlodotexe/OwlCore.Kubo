using CommunityToolkit.Diagnostics;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;

namespace OwlCore.Kubo;

public partial class MfsFolder : IModifiableFolder, IMoveFrom, ICreateCopyOf
{
    /// <summary>
    /// The interval that MFS should be checked for updates.
    /// </summary>
    public TimeSpan UpdateCheckInterval { get; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.IsNotNullOrWhiteSpace(item.Name);

        await Client.Mfs.RemoveAsync($"{Path}{item.Name}", recursive: true, force: true, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, CancellationToken cancellationToken,
        CreateCopyOfDelegate fallback)
    {
        if (fileToCopy is MfsFile mfsFile)
            return CreateCopyOfAsync(mfsFile, overwrite, cancellationToken);

        if (fileToCopy is IpfsFile ipfsFile)
            return CreateCopyOfAsync(ipfsFile, overwrite, cancellationToken);

        if (fileToCopy is IpnsFile ipnsFile)
            return CreateCopyOfAsync(ipnsFile, overwrite, cancellationToken);

        return fallback(this, fileToCopy, overwrite, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken,
        MoveFromDelegate fallback)
    {
        if (fileToMove is MfsFile mfsFile)
            return MoveFromAsync(mfsFile, source, overwrite, cancellationToken);

        return fallback(this, fileToMove, source, overwrite, cancellationToken);
    }

    /// <inheritdoc cref="CreateCopyOfAsync(IFile,bool,CancellationToken,CreateCopyOfDelegate)"/>
    public virtual async Task<IChildFile> CreateCopyOfAsync(MfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Client.Mfs.CopyAsync(fileToCopy.Path, Path, cancel: cancellationToken);
        return new MfsFile($"{Path}{fileToCopy.Name}", Client);
    }

    /// <inheritdoc cref="CreateCopyOfAsync(IFile,bool,CancellationToken,CreateCopyOfDelegate)"/>
    public virtual async Task<IChildFile> CreateCopyOfAsync(IpfsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Client.Mfs.CopyAsync($"/ipfs/{fileToCopy.Id}", Path, cancel: cancellationToken);
        return new MfsFile($"{Path}{fileToCopy.Name}", Client);
    }

    /// <inheritdoc cref="CreateCopyOfAsync(IFile,bool,CancellationToken,CreateCopyOfDelegate)"/>
    public virtual async Task<IChildFile> CreateCopyOfAsync(IpnsFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cid = await fileToCopy.GetCidAsync(cancellationToken);
        await Client.Mfs.CopyAsync($"/ipfs/{cid}", Path, cancel: cancellationToken);

        return new MfsFile($"{Path}{fileToCopy.Name}", Client);
    }

    /// <inheritdoc cref="MoveFromAsync(IChildFile,IModifiableFolder,bool,CancellationToken,MoveFromDelegate)"/>
    public virtual async Task<IChildFile> MoveFromAsync(MfsFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Client.Mfs.MoveAsync(fileToMove.Path, $"{Path}{fileToMove.Name}", cancellationToken);
        return new MfsFile($"{Path}{fileToMove.Name}", Client);
    }

    /// <inheritdoc cref="MoveFromAsync(IChildFile,IModifiableFolder,bool,CancellationToken,MoveFromDelegate)"/>
    public virtual async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (overwrite)
        {
            await Client.Mfs.RemoveAsync($"{Path}{name}", recursive: true, force: true, cancellationToken);
        }

        try
        {
            await Client.Mfs.MakeDirectoryAsync($"{Path}{name}", cancel: cancellationToken);
        }
        catch (Exception ex) when (ex.Message.ToLower().Contains("file already exists"))
        {
            // Ignored, return existing path if exists
        }

        return new MfsFolder($"{Path}{name}", Client);
    }

    /// <inheritdoc/>
    public virtual async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        await Client.Mfs.WriteAsync($"{Path}{name}", new MemoryStream(), new() { Create = true, Truncate = overwrite }, cancellationToken);

        return new MfsFile($"{Path}{name}", Client);
    }


    /// <inheritdoc/>
    public virtual Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new TimerBasedMfsWatcher(Client, this, UpdateCheckInterval));
    }
}