using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Http;
using OwlCore.Kubo.Models;
using OwlCore.Storage;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;

namespace OwlCore.Kubo.FolderWatchers;

/// <summary>
/// Checks the provided MfsFolder for changes to the contents at an interval.
/// </summary>
public class TimerBasedMfsWatcher : TimerBasedFolderWatcher
{
    private readonly ICoreApi _client;

    private bool _running;
    private Cid? _lastKnownRootCid;
    private List<IStorableChild> _knownItems = new();

    /// <summary>
    /// Creates a new instance of <see cref="TimerBasedMfsWatcher"/>.
    /// </summary>
    /// <param name="folder">The folder to watch for changes.</param>
    /// <param name="client">The client used to make requests to Kubo.</param>
    /// <param name="interval">How often checks for updates should be made.</param>
    public TimerBasedMfsWatcher(ICoreApi client, MfsFolder folder, TimeSpan interval)
        : base(folder, interval)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public override event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public override async Task ExecuteAsync()
    {
        if (_running)
            return;

        _running = true;

        var folder = (MfsFolder)Folder;

        // This can be a long running operation, so reruns should be prevented for the duration to avoid concurrent requests.
        var data = await _client.Mfs.StatAsync(folder.Path);
        if (data.Hash is not null && _lastKnownRootCid != data.Hash)
        {
            var items = await folder.GetItemsAsync().ToListAsync();

            var addedItems = items.Except(_knownItems).ToList();
            var removedItems = _knownItems.Except(addedItems).ToList();

            if (addedItems.Count >= 1)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems));

            if (removedItems.Count >= 1)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));

            _lastKnownRootCid = data.Hash;
            _knownItems = items;
        }

        _running = false;
    }
}
