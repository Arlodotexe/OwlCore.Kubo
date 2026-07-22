using Ipfs.CoreApi;
using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;
using OwlCore.Storage.CommonTests;

namespace OwlCore.Kubo.Tests;

[TestClass]
public class MfsFolderTests : CommonIFolderTests
{
    private static int _testCounter;
    private readonly List<string> _createdPaths = new();

    /// <inheritdoc/>
    /// <remarks>MFS doesn't track created time.</remarks>
    public override PropertyValueAvailability CreatedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    /// <remarks>MFS doesn't track last accessed time.</remarks>
    public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    /// <remarks>MFS supports mtime via UnixFS 1.5.</remarks>
    public override PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    public override async Task<IFolder> CreateFolderAsync()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var path = $"/mfs_test_folder_{testId}_{Guid.NewGuid():N}";

        await TestFixture.Client.Mfs.MakeDirectoryAsync(path, parents: true);
        await TestFixture.Client.Mfs.WriteAsync($"{path}/child.txt", "child content", new MfsWriteOptions { Create = true });

        // Set mtime so LastModifiedAt tests can verify it
        var mtime = DateTimeOffset.UtcNow;
        await TestFixture.Client.Mfs.TouchAsync(path, mtime);

        _createdPaths.Add(path);

        return new MfsFolder(path, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override async Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var path = $"/mfs_test_folder_items_{testId}_{Guid.NewGuid():N}";

        await TestFixture.Client.Mfs.MakeDirectoryAsync(path, parents: true);

        for (int i = 0; i < fileCount; i++)
            await TestFixture.Client.Mfs.WriteAsync($"{path}/file_{i}.txt", $"content_{i}", new MfsWriteOptions { Create = true });

        for (int i = 0; i < folderCount; i++)
            await TestFixture.Client.Mfs.MakeDirectoryAsync($"{path}/folder_{i}", parents: true);

        var mtime = DateTimeOffset.UtcNow;
        await TestFixture.Client.Mfs.TouchAsync(path, mtime);

        _createdPaths.Add(path);

        return new MfsFolder(path, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt)
    {
        // MFS doesn't support CreatedAt
        return Task.FromResult<IFolder?>(null);
    }

    /// <inheritdoc/>
    public override async Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var path = $"/mfs_test_folder_{testId}_{Guid.NewGuid():N}";

        await TestFixture.Client.Mfs.MakeDirectoryAsync(path, parents: true);
        await TestFixture.Client.Mfs.WriteAsync($"{path}/child.txt", "child content", new MfsWriteOptions { Create = true });

        // Set specific mtime
        await TestFixture.Client.Mfs.TouchAsync(path, new DateTimeOffset(lastModifiedAt));

        _createdPaths.Add(path);

        return new MfsFolder(path, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        // MFS doesn't support LastAccessedAt
        return Task.FromResult<IFolder?>(null);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var path in _createdPaths)
        {
            try { await TestFixture.Client.Mfs.RemoveAsync(path, recursive: true, force: true); } catch { }
        }
        _createdPaths.Clear();
    }

    [TestMethod]
    public async Task GetItemsAsync()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);

        await mfs.GetItemsAsync().ToListAsync();
    }

    [TestMethod]
    public async Task CreateAndDeleteFolderAsync()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);
        var folder = await mfs.CreateFolderAsync("test", overwrite: true);

        await mfs.DeleteAsync(folder);

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await mfs.GetItemAsync("/test"));
    }

    [TestMethod]
    public async Task CreateAndDeleteFolderAsync_Overwrite()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);
        var folder = await mfs.CreateFolderAsync("test", overwrite: true);

        await ((IModifiableFolder)folder).CreateFileAsync("random", overwrite: true);

        var itemsCount = await GetItemsCount(folder);
        Assert.AreEqual(1, itemsCount);

        var overwrittenFolder = await mfs.CreateFolderAsync("test", overwrite: true);

        var overwrittenFolderItemsCount = await GetItemsCount(overwrittenFolder);
        Assert.AreEqual(0, overwrittenFolderItemsCount);

        await mfs.DeleteAsync(overwrittenFolder);

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await mfs.GetItemAsync("/test"));

        Task<int> GetItemsCount(IFolder folder) => folder.GetItemsAsync().CountAsync().AsTask();
    }

    [TestMethod]
    public async Task CreateAndDeleteFileAsync()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);
        var file = await mfs.CreateFileAsync("test.bin");
        await mfs.DeleteAsync(file);

        var items = await mfs.GetItemsAsync(StorableType.File).ToListAsync();

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await mfs.GetItemAsync("/test.bin"));
    }

    [TestMethod]
    public async Task CreateAndDeleteFileAsync_Overwrite()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);
        var file = await mfs.CreateFileAsync("test.bin", overwrite: true);

        // Write random data
        var buffer = GenerateRandomData(256);
        using var stream = await file.OpenStreamAsync(FileAccess.ReadWrite);
        await stream.WriteAsync(buffer);
        Assert.AreEqual(buffer.Length, stream.Length);

        // Recreate the file
        var overwrittenFile = await mfs.CreateFileAsync("test.bin", overwrite: true);
        using var overwrittenFileStream = await overwrittenFile.OpenStreamAsync(FileAccess.ReadWrite);
        Assert.AreNotEqual(buffer.Length, overwrittenFileStream.Length);

        await mfs.DeleteAsync(overwrittenFile);
    }

    [TestMethod]
    public async Task GetParentAsync()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);

        var folder = await mfs.CreateFolderAsync("test", overwrite: true);

        var parent = await folder.GetParentAsync();

        Assert.AreEqual(mfs.Id, parent?.Id);

        await mfs.DeleteAsync(folder);
    }

    [TestMethod]
    public async Task GetPathFromRootAsync()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);
        var folder = (MfsFolder)await mfs.CreateFolderAsync("test", overwrite: true);
        var subfolder = (MfsFolder)await folder.CreateFolderAsync("subfolder", overwrite: true);

        try
        {
            var root = await subfolder.GetRootAsync();
            Assert.IsNotNull(root);

            var path = await root.GetRelativePathToAsync(subfolder);
            Assert.AreEqual(path, subfolder.Path);
        }
        finally
        {
            await mfs.DeleteAsync(folder);
        }
    }

    [TestMethod]
    public async Task CreateAndRunFolderWatcher()
    {
        var mfs = new MfsFolder("/", TestFixture.Client);

        var watcher = await mfs.GetFolderWatcherAsync();

        await ((TimerBasedFolderWatcher)watcher).ExecuteAsync();
    }

    static byte[] GenerateRandomData(int length)
    {
        var rand = new Random();
        var b = new byte[length];
        rand.NextBytes(b);

        return b;
    }
}
