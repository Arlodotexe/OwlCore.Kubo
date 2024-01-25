using OwlCore.Kubo.FolderWatchers;
using OwlCore.Storage;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class MfsFolderTests
    {
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
        {            var mfs = new MfsFolder("/", TestFixture.Client);
            var folder = (MfsFolder)await mfs.CreateFolderAsync("test", overwrite: true);
            var subfolder = (MfsFolder)await folder.CreateFolderAsync("subfolder", overwrite: true);

            try
            {
                var root = await subfolder.GetRootAsync();
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
}