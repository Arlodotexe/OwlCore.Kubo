using System.IO;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboDownloaderTests
    {
        [TestMethod]
        public async Task DownloadLatestAsync()
        {
            await KuboDownloader.GetLatestBinaryAsync();
        }

        [TestMethod]
        [DataRow("0.15.0")]
        public async Task DownloadVersionAsync(string version)
        {
            await KuboDownloader.GetBinaryVersionAsync(Version.Parse(version));
        }

        [TestMethod]
        [DataRow("0.0")]
        public async Task DownloadInvalidVersionAsync(string version)
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => KuboDownloader.GetBinaryVersionAsync(Version.Parse(version)));
        }

        [TestMethod]
        public async Task GetLatestBinaryAsync_FileNameIsIpfsOrKubo()
        {
            // Act
            var binaryFile = await KuboDownloader.GetLatestBinaryAsync();

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}'");
        }

        [TestMethod]
        [DataRow("0.17.0")]
        public async Task GetBinaryVersionAsync_FileNameIsIpfsOrKubo(string versionString)
        {
            // Arrange
            var version = Version.Parse(versionString);

            // Act
            var binaryFile = await KuboDownloader.GetBinaryVersionAsync(version);

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}' for version {versionString}");
        }

        [TestMethod]
        public async Task GetLatestBinaryAsync_WithHttpClient_FileNameIsIpfsOrKubo()
        {
            // Arrange
            using var httpClient = new HttpClient();

            // Act
            var binaryFile = await KuboDownloader.GetLatestBinaryAsync(httpClient);

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}'");
        } 

        [TestMethod]
        [DataRow("0.16.0")]
        public async Task GetBinaryVersionAsync_WithHttpClient_FileNameIsIpfsOrKubo(string versionString)
        {
            // Arrange
            var version = Version.Parse(versionString);
            using var httpClient = new HttpClient();

            // Act
            var binaryFile = await KuboDownloader.GetBinaryVersionAsync(httpClient, version);

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}' for version {versionString}");
        }

        [TestMethod]
        public async Task GetLatestBinaryAsync_WithIpfsClient_FileNameIsIpfsOrKubo()
        {
            // Arrange
            var ipfsClient = TestFixture.Client;

            // Act
            var binaryFile = await KuboDownloader.GetLatestBinaryAsync(ipfsClient);

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}'");
        }

        [TestMethod]
        [DataRow("0.15.0")]
        public async Task GetBinaryVersionAsync_WithIpfsClient_FileNameIsIpfsOrKubo(string versionString)
        {
            // Arrange
            var version = Version.Parse(versionString);
            var ipfsClient = TestFixture.Client;

            // Act
            var binaryFile = await KuboDownloader.GetBinaryVersionAsync(ipfsClient, version);

            // Assert
            Assert.IsNotNull(binaryFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(binaryFile.Name);
            Assert.IsTrue(fileNameWithoutExtension == "ipfs" || fileNameWithoutExtension == "kubo", 
                $"Expected file name without extension to be 'ipfs' or 'kubo', but got '{fileNameWithoutExtension}' for version {versionString}");
        }
    }
}