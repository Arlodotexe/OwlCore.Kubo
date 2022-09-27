using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore.Extensions;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;
using System.Runtime.CompilerServices;

namespace OwlCore.Kubo;

/// <summary>
/// A folder that resides on IPFS behind an IPNS Address.
/// </summary>
public class IpnsFolder : IMutableFolder
{
    private readonly IpfsClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolder"/>.
    /// </summary>
    /// <param name="ipnsAddress">A resolvable IPNS address, such as "ipfs.tech" or "k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj".</param>
    /// <param name="client">The IPFS Client to use for retrieving the content.</param>
    public IpnsFolder(string ipnsAddress, IpfsClient client)
    {
        Id = ipnsAddress;
        Name = ipnsAddress;
        _client = client;
    }

    /// <inheritdoc />
    public string Id { get; protected set; }

    /// <inheritdoc />
    public string Name { get; protected set; }

    /// <summary>
    /// The interval that IPNS should be checked for updates.
    /// </summary>
    public TimeSpan UpdateCheckInterval { get; } = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resolvedIpns = await _client.ResolveAsync(Id, recursive: true, cancel: cancellationToken);
        Guard.IsNotNull(resolvedIpns);

        var cid = resolvedIpns.Split(new[] { "/ipfs/" }, StringSplitOptions.None)[1];

        var folder = new IpfsFolder(cid, _client);

        await foreach (var item in folder.GetItemsAsync(type, cancellationToken))
        {
            if (item is IFolder subFolder)
            {
                if (item is IChainedAddressableStorable addressableSubFolder)
                    yield return new AddressableIpnsFolder(Id, subFolder.Name, _client, addressableSubFolder.ParentChain.Concat(((IFolder)this).IntoList()).ToArray());
                else
                    yield return new AddressableIpnsFolder(Id, subFolder.Name, _client, new IFolder[] { this });
            }

            if (item is IFile file)
            {
                if (item is IChainedAddressableStorable addressableFile)
                    yield return new AddressableIpnsFile(Id, file.Name, _client, addressableFile.ParentChain.Concat(((IFolder)this).IntoList()).ToArray());
                else
                    yield return new AddressableIpnsFile(Id, file.Name, _client, new IFolder[] { this });
            }

        }
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new TimerBasedIpnsWatcher(_client, this, UpdateCheckInterval));
    }
}