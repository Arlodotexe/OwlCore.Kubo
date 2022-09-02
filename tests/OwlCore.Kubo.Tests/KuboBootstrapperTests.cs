using System.Text;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboBootstrapperTests
    {
        [TestMethod, Timeout(20_000)]
        public async Task DownloadAndBootstrapAsync()
        {
            var downloader = new KuboDownloader();
            var kuboBinary = await downloader.DownloadLatestBinaryAsync();

            using var bootstrapper = new KuboBootstrapper(kuboBinary, Path.GetTempPath())
            {
                ApiUri = new Uri("http://127.0.0.1:7700"),
            };

            await bootstrapper.StartAsync();
            bootstrapper.Stop();
        }
    }
}