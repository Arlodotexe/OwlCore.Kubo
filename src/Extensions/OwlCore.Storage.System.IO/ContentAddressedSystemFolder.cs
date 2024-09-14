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
        var res = await Client.FileSystem.AddDirectoryAsync(Id, recursive: true, addFileOptions, cancellationToken);

        Guard.IsTrue(res.IsDirectory);
        return res.ToLink().Id;
    }
}
