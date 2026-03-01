using Ipfs.CoreApi;
using OwlCore.Storage;
using OwlCore.Storage.CommonTests;

namespace OwlCore.Kubo.Tests;

[TestClass]
public class MfsFileTests : CommonIFileTests
{
    private static int _testCounter;
    private readonly List<string> _createdPaths = new();

    /// <inheritdoc/>
    public override bool SupportsWriting => true;

    /// <inheritdoc/>
    /// <remarks>MFS doesn't automatically track mtime - it must be explicitly set via touch.</remarks>
    public override PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    /// <remarks>MFS doesn't track created time.</remarks>
    public override PropertyValueAvailability CreatedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    /// <remarks>MFS doesn't track last accessed time.</remarks>
    public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;

    /// <inheritdoc/>
    public override async Task<IFile> CreateFileAsync()
    {
        var testId = Interlocked.Increment(ref _testCounter);
        var fileName = $"mfs_test_file_{testId}_{Guid.NewGuid():N}.bin";

        // Create file from MFS root folder
        var rootFolder = new MfsFolder("/", TestFixture.Client);
        var file = await rootFolder.CreateFileAsync(fileName, overwrite: true);

        // Write content using MfsStream
        using (var stream = await file.OpenWriteAsync())
        {
            var data = new byte[256];
            Random.Shared.NextBytes(data);
            await stream.WriteAsync(data, 0, 256);
            // Dispose will flush
        }

        _createdPaths.Add($"/{fileName}");

        return file;
    }

    /// <inheritdoc/>
    public override Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt)
    {
        // MFS doesn't support CreatedAt
        return Task.FromResult<IFile?>(null);
    }

    /// <inheritdoc/>
    public override async Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var file = await CreateFileAsync();
        
        // Set mtime via touch
        if (file is MfsFile mfsFile)
        {
            await TestFixture.Client.Mfs.TouchAsync(mfsFile.Path, new DateTimeOffset(lastModifiedAt));
        }

        return file;
    }

    /// <inheritdoc/>
    public override Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        // MFS doesn't support LastAccessedAt
        return Task.FromResult<IFile?>(null);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var path in _createdPaths)
        {
            try { await TestFixture.Client.Mfs.RemoveAsync(path, force: true); } catch { }
        }
        _createdPaths.Clear();
    }
}
