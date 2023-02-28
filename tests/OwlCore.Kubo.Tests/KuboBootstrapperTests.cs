using System.Text;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboBootstrapperTests
    {
        [TestMethod, Timeout(45_000)]
        public async Task DownloadAndBootstrapAsync()
        {
            if (KuboAccess.Bootstrapper is not null)
                return;

            var downloader = new KuboDownloader();
            var kuboBinary = await downloader.DownloadLatestBinaryAsync();

            using var bootstrapper = new KuboBootstrapper(kuboBinary, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
            {
                ApiUri = new Uri("http://127.0.0.1:7700"),
            };

            await bootstrapper.StartAsync();
        }
    }
}