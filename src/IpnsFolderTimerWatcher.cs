using System.Collections.Specialized;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Extensions;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// Watches the provided IpnsFolder for changes to the contents.
/// </summary>
public class IpnsFolderTimerWatcher : IFolderWatcher
{
    private readonly IpfsClient _ipfsClient;
    private readonly Timer _timer;

    private Cid? _lastKnownRootCid;
    private List<IStorable> _knownItems = new();

    /// <summary>
    /// Creates a new instance of <see cref="IpnsFolderTimerWatcher"/>.
    /// </summary>
    /// <param name="ipfsClient">The IpfsClient used to check for changes to the IPNS address.</param>
    /// <param name="folder">The folder being watched for changes.</param>
    /// <param name="interval">The interval that IPNS should be checked for updates.</param>
    public IpnsFolderTimerWatcher(IpfsClient ipfsClient, IpnsFolder folder, TimeSpan interval)
    {
        _ipfsClient = ipfsClient;
        Folder = folder;

        _timer = new Timer(_ => ExecuteAsync().Forget());
        _timer.Change(TimeSpan.MinValue, interval);
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public IMutableFolder Folder { get; }

    private async Task ExecuteAsync()
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

        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, _knownItems));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, _knownItems));

        _knownItems = items;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}