﻿using System.Collections.Specialized;
using OwlCore.Extensions;
using OwlCore.Storage;

namespace OwlCore.Kubo.FolderWatchers;

/// <summary>
/// Watches a folder for changes using a timer.
/// </summary>
public abstract class TimerBasedFolderWatcher : IFolderWatcher
{
    private readonly Timer _timer;

    /// <summary>
    /// Creates a new instance of <see cref="TimerBasedIpnsWatcher"/>.
    /// </summary>
    /// <param name="folder">The folder being watched for changes.</param>
    /// <param name="interval">How often checks for updates should be made.</param>
    public TimerBasedFolderWatcher(IMutableFolder folder, TimeSpan interval)
    {
        Folder = folder;

        _timer = new Timer(_ => ExecuteAsync().Forget());
        _timer.Change(TimeSpan.MinValue, interval);
    }

    /// <inheritdoc/>
    public abstract event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public IMutableFolder Folder { get; }

    /// <summary>
    /// Executes the folder check.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public abstract Task ExecuteAsync();

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
