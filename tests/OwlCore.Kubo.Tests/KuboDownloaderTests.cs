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
    }
}