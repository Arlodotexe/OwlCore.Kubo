using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Kubo;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// An implementation of <see cref="SystemFolder"/> with added support for <see cref="IGetCid"/>.
/// </summary>
public class ContentAddressedSystemFolder : SystemFolder, IAddFileToGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="client"></param>
    public ContentAddressedSystemFolder(string path, ICoreApi client)
        : base(path)
    {
        Client = client;
    }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    public ICoreApi Client { get; }

    /// <inheritdoc/>
    public async Task<Cid> GetCidAsync(AddFileOptions addFileOptions, CancellationToken cancellationToken)
    {
        FilePart[] fileParts = [];
        FolderPart[] folderParts = [new FolderPart { Name = Name }];

        // Get child file and folder parts recursively
        var q = new Queue<SystemFolder>([this]);
        while (q.Count > 0)
        {
            await foreach (var item in q.Dequeue().GetItemsAsync(StorableType.All, cancellationToken))
            {
                var relativePath = await this.GetRelativePathToAsync(item, cancellationToken);
                var adjustedRelativePath = $"{Name}{relativePath}";
                
                if (item is SystemFile file)
                {
                    fileParts = [.. fileParts, new FilePart { Name = adjustedRelativePath, Data = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous), AbsolutePath = file.Path }];
                }
                else if (item is SystemFolder folder)
                {
                    folderParts = [.. folderParts, new FolderPart { Name = adjustedRelativePath }];
                    q.Enqueue(folder);
                }
            }
        }

        Cid? rootCid = null;

        await foreach (var node in Client.FileSystem.AddAsync(fileParts, folderParts, addFileOptions, cancellationToken))
        {
            var filePart = fileParts.FirstOrDefault(x => x.Name == node.Name);
            filePart?.Data?.Dispose();

            if (node.Name == Name)
                rootCid = node.Id;
        }
        
        Guard.IsNotNull(rootCid);
        return rootCid;
    }
}
