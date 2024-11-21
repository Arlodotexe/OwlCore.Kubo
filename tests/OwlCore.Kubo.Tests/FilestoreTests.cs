using OwlCore.Storage.System.IO;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Diagnostics;
using OwlCore.Storage;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class FilestoreTests
    {
        [TestMethod]
        public async Task TestFilestoreAsync()
        {
            Guard.IsNotNull(TestFixture.Bootstrapper);
            var client = TestFixture.Client;

            var workingFolder = (IModifiableFolder?)await TestFixture.Bootstrapper.RepoFolder.GetParentAsync();
            Guard.IsNotNull(workingFolder);
            
            var testFolder = (SystemFolder)await workingFolder.CreateFolderAsync("in", overwrite: true);

            // Create random data in working folder
            await foreach (var file in testFolder.CreateFilesAsync(2, i => $"{i}.bin", CancellationToken.None))
                await file.WriteRandomBytesAsync(512, 512, CancellationToken.None);

            // Add the same data to filestore
            var filestoreFolderCid = await testFolder.GetCidAsync(client, new Ipfs.CoreApi.AddFileOptions { NoCopy = true, CidVersion = 1, }, default);

            // List the filestore
            var items = await client.Filestore.ListAsync().ToListAsync();
            Assert.AreEqual(2, items.Count);

            // Verify duplicate don't exist yet
            var duplicates = await client.Filestore.DupsAsync().ToListAsync();
            Assert.AreEqual(0, duplicates.Count);

            // Add to ipfs (without filestore)
            var folderCid = await testFolder.GetCidAsync(client, new Ipfs.CoreApi.AddFileOptions { NoCopy = false, RawLeaves = true, CidVersion = 1, }, default);
            Assert.AreEqual(folderCid, filestoreFolderCid);

            // Verify the filestore
            items = await client.Filestore.VerifyObjectsAsync().ToListAsync();
            Assert.AreEqual(2, items.Count);

            // Verify duplicates now exist
            duplicates = await client.Filestore.DupsAsync().ToListAsync();
            Assert.AreEqual(2, duplicates.Count);
        }
    }
}
