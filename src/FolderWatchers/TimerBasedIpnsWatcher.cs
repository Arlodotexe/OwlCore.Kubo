using System.Collections.Specialized;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Extensions;
using OwlCore.Storage;

namespace OwlCore.Kubo.FolderWatchers;

/// <summary>
/// Checks the provided IpnsFolder for changes to the contents at an interval.
/// </summary>
public class TimerBasedIpnsWatcher : TimerBasedFolderWatcher
{
    private readonly IpfsClient _ipfsClient;

    private Cid? _lastKnownRootCid;
    private List<IStorableChild> _knownItems = new();

    /// <summary>
    /// Creates a new instance of <see cref="TimerBasedIpnsWatcher"/>.
    /// </summary>
    /// <param name="ipfsClient">The IpfsClient used to check for changes to the IPNS address.</param>
    /// <param name="folder">The folder being watched for changes.</param>
    /// <param name="interval">The interval that IPNS should be checked for updates.</param>
    public TimerBasedIpnsWatcher(IpfsClient ipfsClient, IpnsFolder folder, TimeSpan interval)
        : base(folder, interval)
    {
        _ipfsClient = ipfsClient;
    }

    /// <inheritdoc/>
    public override event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Executes the folder check.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public override async Task ExecuteAsync()
    {
        var ipnsPath = Folder.Id;

        var resolvedIpnsValue = await _ipfsClient.ResolveAsync(ipnsPath, recursive: true);
        Guard.IsNotNullOrWhiteSpace(resolvedIpnsValue);

        var cid = resolvedIpnsValue.Split(new[] { "/ipfs/" }, StringSplitOptions.None)[1];
        if (cid == _lastKnownRootCid && _lastKnownRootCid != null)
            return;

        _lastKnownRootCid = cid;

        var folder = new IpfsFolder(cid, _ipfsClient);
        var items = await folder.GetItemsAsync().ToListAsync();

        var addedItems = items.Except(_knownItems).ToList();
        var removedItems = _knownItems.Except(addedItems).ToList();

        if (addedItems.Count >= 1)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems));

        if (removedItems.Count >= 1)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));

        _knownItems = items;
    }
}