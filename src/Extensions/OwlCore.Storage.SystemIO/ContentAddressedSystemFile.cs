using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Kubo;

namespace OwlCore.Storage.SystemIO;

/// <summary>
/// An implementation of <see cref="OwlCore.Storage.SystemIO.SystemFile"/> with added support for <see cref="IGetCid"/>.
/// </summary>
public class ContentAddressedSystemFile : OwlCore.Storage.SystemIO.SystemFile, IGetCid
{
    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="client"></param>
    public ContentAddressedSystemFile(string path, IpfsClient client)
        : base(path)
    {
        Client = client;
    }

    /// <summary>
    /// The IPFS Client to use for retrieving the content.
    /// </summary>
    public IpfsClient Client { get; }

    /// <inheritdoc/>
    public async Task<Cid> GetCidAsync(CancellationToken cancellationToken)
    {
        var res = await Client.FileSystem.AddFileAsync(Id, new()
        {
            OnlyHash = true,
            Pin = false
        }, cancellationToken);

        Guard.IsFalse(res.IsDirectory);
        return res.ToLink().Id;
    }
}
