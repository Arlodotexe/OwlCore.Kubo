using Ipfs.Http;
using OwlCore.Extensions;

namespace OwlCore.Kubo.Tests
{
    internal static class KuboAccess
    {
        private static SemaphoreSlim _setupSemaphore = new SemaphoreSlim(1, 1);

        public static KuboBootstrapper? Bootstrapper { get; private set; }

        public static IpfsClient? Ipfs { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static async Task TryInitAsync()
        {
            if (IsInitialized)
                return;

            using (await _setupSemaphore.DisposableWaitAsync())
            {
                if (IsInitialized)
                    return;

                var kuboBinary = await KuboDownloader.GetLatestBinaryAsync();

                Bootstrapper = new KuboBootstrapper(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}"))
                {
                    ApiUri = new Uri("http://127.0.0.1:5577"),
                    GatewayUri = new Uri("http://127.0.0.1:8077"),
                };

                await Bootstrapper.StartAsync();

                Ipfs = new IpfsClient(Bootstrapper.ApiUri.OriginalString);
                IsInitialized = true;
            }
        }
    }
}
