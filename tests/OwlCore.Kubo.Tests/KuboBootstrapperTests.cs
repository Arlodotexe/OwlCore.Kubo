namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class KuboBootstrapperTests
    {
        [TestMethod, Timeout(45_000)]
        public async Task DownloadAndBootstrapAsync()
        {
            if (TestFixture.Bootstrapper is not null)
                return;

            using var bootstrapper = new KuboBootstrapper(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
            {
                ApiUri = new Uri("http://127.0.0.1:7700"),
            };

            await bootstrapper.StartAsync();
        }
    }
}