using Ipfs.CoreApi;
using OwlCore.Storage;
using OwlCore.Storage.CommonTests;
using System.Diagnostics;

namespace OwlCore.Kubo.Tests;

[TestClass]
public class IpfsFolderTests : CommonIFolderTests
{
    private static int _testCounter;

    /// <inheritdoc/>
    /// <remarks>IPFS uses content-addressed CIDs which serve as both unique identifiers and valid names.</remarks>
    public override bool AllowsIdEqualToName => true;

    /// <inheritdoc/>
    /// <remarks>IPFS supports mtime via UnixFS 1.5.</remarks>
    public override PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Maybe;

    private readonly List<string> _createdMfsPaths = new();

    /// <inheritdoc/>
    public override async Task<IFolder> CreateFolderAsync()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var mfsPath = $"/ipfs_test_folder_{testId}_{Guid.NewGuid():N}";

        // Create folder in MFS with a child file
        await TestFixture.Client.Mfs.MakeDirectoryAsync(mfsPath, parents: true);
        await TestFixture.Client.Mfs.WriteAsync($"{mfsPath}/child.txt", "child content", new MfsWriteOptions { Create = true });

        // Set mtime so LastModifiedAt tests can verify it
        var mtime = DateTimeOffset.UtcNow;
        await TestFixture.Client.Mfs.TouchAsync(mfsPath, mtime);

        // Flush to get the CID
        var cid = await TestFixture.Client.Mfs.FlushAsync(mfsPath);

        _createdMfsPaths.Add(mfsPath);

        return new IpfsFolder(cid, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override async Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var mfsPath = $"/ipfs_test_folder_items_{testId}_{Guid.NewGuid():N}";

        await TestFixture.Client.Mfs.MakeDirectoryAsync(mfsPath, parents: true);

        for (int i = 0; i < fileCount; i++)
            await TestFixture.Client.Mfs.WriteAsync($"{mfsPath}/file_{i}.txt", $"content_{i}", new MfsWriteOptions { Create = true });

        for (int i = 0; i < folderCount; i++)
            await TestFixture.Client.Mfs.MakeDirectoryAsync($"{mfsPath}/folder_{i}", parents: true);

        var mtime = DateTimeOffset.UtcNow;
        await TestFixture.Client.Mfs.TouchAsync(mfsPath, mtime);

        var cid = await TestFixture.Client.Mfs.FlushAsync(mfsPath);

        _createdMfsPaths.Add(mfsPath);

        return new IpfsFolder(cid, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt) =>
        // IPFS doesn't support CreatedAt
        Task.FromResult<IFolder?>(null);

    /// <inheritdoc/>
    public override async Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var mfsPath = $"/ipfs_test_folder_{testId}_{Guid.NewGuid():N}";

        // Create folder in MFS with a child file
        await TestFixture.Client.Mfs.MakeDirectoryAsync(mfsPath, parents: true);
        await TestFixture.Client.Mfs.WriteAsync($"{mfsPath}/child.txt", "child content", new MfsWriteOptions { Create = true });

        // Set specific mtime
        await TestFixture.Client.Mfs.TouchAsync(mfsPath, new DateTimeOffset(lastModifiedAt));

        // Flush to get the CID
        var cid = await TestFixture.Client.Mfs.FlushAsync(mfsPath);

        _createdMfsPaths.Add(mfsPath);

        return new IpfsFolder(cid, TestFixture.Client);
    }

    /// <inheritdoc/>
    public override Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt) =>
        // IPFS doesn't support LastAccessedAt
        Task.FromResult<IFolder?>(null);

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var path in _createdMfsPaths)
        {
            try { await TestFixture.Client.Mfs.RemoveAsync(path, recursive: true, force: true); } catch { }
        }
        _createdMfsPaths.Clear();
    }

    [TestMethod]
    public async Task GetFilesAsync()
    {
        var folder = new IpfsFolder("QmSnuWmxptJZdLJpKRarxBMS2Ju2oANVrgbr2xWbie9b2D", TestFixture.Client);
        var files = await folder.GetFilesAsync().ToListAsync();

        foreach(var item in files)
            Debug.WriteLine($"{item.Name}, {item.Id}");

        Assert.IsNotNull(files);
        Assert.IsTrue(files.Count > 2);
    }

    [TestMethod]
    public async Task GetFoldersAsync()
    {
        var folder = new IpfsFolder("QmSnuWmxptJZdLJpKRarxBMS2Ju2oANVrgbr2xWbie9b2D", TestFixture.Client);
        var folders = await folder.GetFoldersAsync().ToListAsync();

        foreach(var item in folders)
            Debug.WriteLine($"{item.Name}, {item.Id}");

        Assert.IsNotNull(folders);
        Assert.AreEqual(2, folders.Count);
    }
}
