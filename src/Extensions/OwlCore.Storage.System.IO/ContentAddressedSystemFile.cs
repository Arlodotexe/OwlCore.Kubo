using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Kubo;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// An implementation of <see cref="SystemFile"/> with added support for <see cref="IGetCid"/>.
/// </summary>
public class ContentAddressedSystemFile : SystemFile, IAddFileToGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="client"></param>
    public ContentAddressedSystemFile(string path, ICoreApi client)
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
        var fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        var res = await Client.FileSystem.AddAsync([new FilePart { Name = Name, Data = fileStream, AbsolutePath = Path }], [], addFileOptions, cancellationToken).FirstAsync();

        Guard.IsFalse(res.IsDirectory);
        return res.ToLink().Id;
    }
}
