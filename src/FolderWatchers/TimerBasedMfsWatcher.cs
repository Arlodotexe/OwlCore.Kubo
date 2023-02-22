using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using OwlCore.Kubo.Models;

namespace OwlCore.Kubo.FolderWatchers;

/// <summary>
/// Checks the provided MfsFolder for changes to the contents at an interval.
/// </summary>
public class TimerBasedMfsWatcher : TimerBasedFolderWatcher
{
    private readonly IpfsClient _client;

    private bool _running;
    private Cid? _lastKnownRootCid;
    private List<IStorableChild> _knownItems = new();

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
        if (_running)
            return;

        _running = true;

        var folder = (MfsFolder)Folder;

        // This can be a long running operation, so reruns should be prevented for the duration to avoid concurrent requests.
        var serialized = await _client.DoCommandAsync("files/stat", CancellationToken.None, folder.Path, "long=true");
        var result = await JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(serialized)), typeof(MfsFileStatData), ModelSerializer.Default);

        Guard.IsNotNull(result);

        var data = (MfsFileStatData)result;

        if (_lastKnownRootCid != data.Hash)
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
