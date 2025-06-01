using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;

namespace OwlCore.Kubo;

/// <summary>
/// Extends <see cref="IStorable"/> with additional Kubo features.
/// </summary>
public static partial class StorableKuboExtensions
{
    /// <summary>
    /// Gets a CID for the provided <paramref name="item"/>. If possible, a CID will be provided without adding the item to ipfs, otherwise the <paramref name="addFileOptions"/> will be used to add content to ipfs and compute the cid.
    /// </summary>
    /// <param name="item">The storable to get the cid for.</param>
    /// <param name="client">The client to use for communicating with ipfs.</param>
    /// <param name="addFileOptions">The options to use when adding content from the <paramref name="item"/> to ipfs.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the cid of the <paramref name="item"/>.</returns>
    /// <exception cref="NotSupportedException">An unsupported implementation of <see cref="IStorable"/> was provided for <paramref name="item"/>.</exception>
    public static async Task<Cid> GetCidAsync(this IStorable item, ICoreApi client, AddFileOptions addFileOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Wrap with known supported IGetCid implementations.
        // Non-inbox wrappers that implement IGetCid should be applied before being passed to this method.
        // ---
        // Consumers can apply their own implementation wrappers for IGetCid before calling this method.
        // You'd typically only do this if you can interact your Kubo node faster than a manually piped stream,
        // or if your implementation of IGetCid has enhanced capabilities (e.g. folder support).
        // ---
        
        // Get cid without adding to ipfs, if possible
        if (item is IGetCid getCid)
            return await getCid.GetCidAsync(cancellationToken);
        
        // Get cid by adding content to ipfs.
        if (item is not IAddFileToGetCid && item is SystemFile systemFile)
            item = new ContentAddressedSystemFile(systemFile.Path, client);

        if (item is not IAddFileToGetCid && item is SystemFolder systemFolder)
            item = new ContentAddressedSystemFolder(systemFolder.Path, client);

        // If the implementation can handle content addressing directly, use that.
        if (item is IAddFileToGetCid contentAddressedStorable)
            return await contentAddressedStorable.GetCidAsync(addFileOptions, cancellationToken);

        // Otherwise, a fallback approach that manually connects the streams together.
        // The Kubo API doesn't support this scenario for folders, without assuming that the Id is a local path,
        // a scenario that's already handled where supported via the IGetCid interface.
        if (item is IFile file)
        {
            using var stream = await file.OpenStreamAsync(FileAccess.Read, cancellationToken);
            var res = await client.FileSystem.AddAsync(stream, file.Name, addFileOptions, cancellationToken);

            Guard.IsFalse(res.IsDirectory);
            return res.ToLink().Id;
        }

        // Process as folder
        if (item is not IFolder folder)
            throw new NotSupportedException($"Unsupported implementation of {nameof(IStorable)}: {item.GetType().Name}");

        // Get child file and folder parts recursively
        FilePart[] fileParts = [];
        FolderPart[] folderParts = [new FolderPart { Name = folder.Name }];

        var q = new Queue<IFolder>([folder]);
        while (q.Count > 0)
        {
            await foreach (var folderItem in q.Dequeue().GetItemsAsync(StorableType.All, cancellationToken))
            {
                var relativePath = await folder.GetRelativePathToAsync(folderItem, cancellationToken);
                var adjustedRelativePath = $"{folder.Name}{relativePath}";
                
                if (folderItem is IFile innerFile)
                {
                    fileParts = [.. fileParts, new FilePart { Name = adjustedRelativePath, Data = await innerFile.OpenReadAsync(cancellationToken) }];
                }
                else if (folderItem is IFolder innerFolder)
                {
                    folderParts = [.. folderParts, new FolderPart { Name = adjustedRelativePath }];
                    q.Enqueue(innerFolder);
                }
            }
        }

        Cid? rootCid = null;

        // Add to ipfs, dispose of data streams as they're processed
        await foreach (var node in client.FileSystem.AddAsync(fileParts, folderParts, addFileOptions, cancellationToken))
        {
            var filePart = fileParts.FirstOrDefault(x => x.Name == node.Name);
            filePart?.Data?.Dispose();

            if (node.Name == folder.Name)
                rootCid = node.Id;
        }
        
        Guard.IsNotNull(rootCid);
        return rootCid;
    }
}
