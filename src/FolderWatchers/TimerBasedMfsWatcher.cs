using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;
using System.Collections.Specialized;

namespace OwlCore.Kubo.FolderWatchers;

/// <summary>
/// Checks the provided MfsFolder for changes to the contents at an interval.
/// </summary>
public class TimerBasedMfsWatcher : TimerBasedFolderWatcher
{
    private readonly IpfsClient _client;

    private Cid? _lastKnownRootCid;
    private List<IAddressableStorable> _knownItems = new();

    /// <summary>
    /// Creates a new instance of <see cref="TimerBasedMfsWatcher"/>.
    /// </summary>
    /// <param name="folder">The folder to watch for changes.</param>
    /// <param name="client">The client used to make requests to Kubo.</param>
    /// <param name="interval">How often checks for updates should be made.</param>
    public TimerBasedMfsWatcher(IpfsClient client, MfsFolder folder, TimeSpan interval)
        : base(folder, interval)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public override event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public override async Task ExecuteAsync()
    {
        var folder = (MfsFolder)Folder;

        var itemInfo = await _client.FileSystem.ListFileAsync(folder.Path);
        Guard.IsNotNull(itemInfo);

        if (_lastKnownRootCid != itemInfo.Id)
        {
            var items = await folder.GetItemsAsync().ToListAsync();

            var addedItems = items.Except(_knownItems).ToList();
            var removedItems = _knownItems.Except(addedItems).ToList();

            if (addedItems.Count >= 1)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems));

            if (removedItems.Count >= 1)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, _knownItems));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, _knownItems));

            _lastKnownRootCid = itemInfo.Id;
            _knownItems = items;
        }
    }
}
