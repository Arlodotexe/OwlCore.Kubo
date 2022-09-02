using Ipfs.Http;
using OwlCore.Provisos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwlCore.Kubo.Tests
{
    internal static class KuboAccess
    {
        private static SemaphoreSlim _setupSemaphore = new SemaphoreSlim(1, 1);

        public static KuboBootstrapper? Bootstrapper { get; private set; }

        public static IpfsClient Ipfs { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static async Task TryInitAsync()
        {
            if (IsInitialized)
                return;

            using (await Flow.EasySemaphore(_setupSemaphore))
            {
                if (IsInitialized)
                    return;

                try
                {
                    Ipfs = new IpfsClient();
                    var stats = await Ipfs.Stats.RepositoryAsync();

                    Assert.IsTrue(stats.NumObjects > 0);
                }
                catch
                {
                    var downloader = new KuboDownloader();
                    var kuboBinary = await downloader.DownloadLatestBinaryAsync();

                    Bootstrapper = new KuboBootstrapper(kuboBinary, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                    {
                        ApiUri = new Uri("http://127.0.0.1:5577"),
                    };

                    await Bootstrapper.StartAsync();

                    Ipfs = new IpfsClient(Bootstrapper.ApiUri.OriginalString);
                }
                finally
                {
                    IsInitialized = true;
                }
            }
        }
    }
}
