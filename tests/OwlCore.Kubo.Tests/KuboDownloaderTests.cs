namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboDownloaderTests
    {
        [TestMethod]
        public async Task DownloadLatestAsync()
        {
            var downloader = new KuboDownloader();

            _ = await downloader.DownloadLatestBinaryAsync();
        }

        [TestMethod]
        [DataRow("0.15.0")]
        public async Task DownloadVersionAsync(string version)
        {
            var downloader = new KuboDownloader();

            _ = await downloader.DownloadBinaryAsync(Version.Parse(version));
        }

        [TestMethod]
        [DataRow("0.0")]
        public async Task DownloadInvalidVersionAsync(string version)
        {
            var downloader = new KuboDownloader();

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => downloader.DownloadBinaryAsync(Version.Parse(version)));
        }
    }
}