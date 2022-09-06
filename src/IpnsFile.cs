using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// A file that resides on IPFS behind an IPNS Address.
/// </summary>
public class IpnsFile : IFile
{
    private readonly IpfsClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFile(string ipnsAddress, IpfsClient client)
    {
        Id = ipnsAddress;
        Name = ipnsAddress;
        _client = client;
    }

    /// <inheritdoc />
    public string Id { get; protected set; }

    /// <inheritdoc />
    public string Name { get; protected set; }

    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var resolvedIpns = await _client.ResolveAsync(Id, recursive: true, cancel: cancellationToken);
        Guard.IsNotNull(resolvedIpns);

        var cid = resolvedIpns.Split(new[] { "/ipfs/" }, StringSplitOptions.None)[1];

        var file = new IpfsFile(cid, _client);

        return await file.OpenStreamAsync(accessMode, cancellationToken);
    }
}