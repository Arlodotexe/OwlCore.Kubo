namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboDownloaderTests
    {
        [TestMethod]
        public async Task BasicDownloadAsync()
        {
            var downloader = new KuboDownloader();

            _ = await downloader.DownloadBinaryAsync();
        }
    }
}