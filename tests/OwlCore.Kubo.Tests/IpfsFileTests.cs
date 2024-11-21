using CommunityToolkit.Diagnostics;
using Ipfs.CoreApi;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using OwlCore.Storage.System.Net.Http;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpfsFileTests
    {
        [TestMethod]
        public async Task BasicFileRead_RpcApi_Test()
        {
            var file = new IpfsFile("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", TestFixture.Client);
            using var stream = await file.OpenStreamAsync();

            using StreamReader text = new StreamReader(stream);
            var txt = await text.ReadToEndAsync();

            Assert.AreEqual("hello world", txt);
        }

        [TestMethod]
        public async Task BasicFileRead_Gateway_Test()
        {
            Guard.IsNotNull(TestFixture.Bootstrapper);

            var file = new HttpFile($"{TestFixture.Bootstrapper.GatewayUri}/ipfs/Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
            var txt = await file.ReadTextAsync();

            Assert.AreEqual("hello world", txt);
        }

        [TestMethod]
        public async Task BasicFileRead_RpcApi_LargeFile_Test()
        {
            var bufferSize = 4096;
            var totalFileSize = (long)int.MaxValue + 1024;

            // Generate a large file
            var workingFile = new SystemFile(Path.GetTempFileName());
            await workingFile.WriteRandomBytesAsync(totalFileSize, bufferSize, CancellationToken.None);

            // Upload large file via RPC API
            using var srcFileStream = await workingFile.OpenReadAsync();
            var added = await TestFixture.Client.FileSystem.AddAsync([new FilePart { AbsolutePath = workingFile.Path, Data = srcFileStream, Name = workingFile.Name }], []).FirstAsync();
            srcFileStream.Position = 0;

            // Test with IpfsFile.
            var file = new IpfsFile(added.Id, TestFixture.Client);
            using var destFileStream = await file.OpenStreamAsync();

            Assert.AreEqual(srcFileStream.Length, totalFileSize);
            Assert.AreEqual(destFileStream.Length, totalFileSize);

            await srcFileStream.AssertStreamEqualAsync(destFileStream, bufferSize, CancellationToken.None);
        }

        [TestMethod]
        public async Task BasicFileRead_Gateway_LargeFile_Test()
        {
            Guard.IsNotNull(TestFixture.Bootstrapper);
            var bufferSize = 4096;
            var totalFileSize = (long)int.MaxValue + 1024;
            
            // Generate a large file
            var sourceFile = new SystemFile(Path.GetTempFileName());
            await sourceFile.WriteRandomBytesAsync(totalFileSize, bufferSize, CancellationToken.None);

            // Upload large file via RPC API
            var srcFileStream = await sourceFile.OpenReadAsync();
            var added = await TestFixture.Client.FileSystem.AddAsync([new FilePart { AbsolutePath = sourceFile.Path, Data = srcFileStream, Name = sourceFile.Name }], []).FirstAsync();
            srcFileStream.Position = 0;

            // Open large file via http gateway
            var destFile = new HttpFile($"{TestFixture.Bootstrapper.GatewayUri}/ipfs/{added.Id}");
            using var destFileStream = await destFile.OpenReadAsync();

            Assert.AreEqual(srcFileStream.Length, totalFileSize);
            Assert.AreEqual(destFileStream.Length, totalFileSize);

            await srcFileStream.AssertStreamEqualAsync(destFileStream, bufferSize, CancellationToken.None);
        }
    }
}