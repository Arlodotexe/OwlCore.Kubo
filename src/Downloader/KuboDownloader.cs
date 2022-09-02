using CommunityToolkit.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace OwlCore.Kubo
{
    /// <summary>
    /// Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture.
    /// </summary>
    public class KuboDownloader
    {
        private HttpClient? _client;

        /// <summary>
        /// The message handler used for downloading the binary.
        /// </summary>
        public HttpMessageHandler HttpMessageHandler { get; set; } = new HttpClientHandler();

        /// <summary>
        /// Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture, returning the downloaded binary.
        /// </summary>
        public async Task<IFile> DownloadLatestBinaryAsync(CancellationToken cancellationToken = default)
        {
            _client ??= new HttpClient(HttpMessageHandler);

            var latestVersion = await FindLatestVersion(_client);
            var downloadLink = await GetDownloadLink(_client, rootUrl: $"https://dist.ipfs.tech/kubo/{latestVersion}");
            using var downloadStream = await _client.GetStreamAsync(downloadLink);
            var file = await ArchiveCrawlForKuboBinary(downloadStream, cancellationToken);

            Guard.IsNotNull(file);
            return file;
        }

        /// <summary>
        /// Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture, returning the downloaded binary.
        /// </summary>
        public async Task<IFile> DownloadBinaryAsync(Version version, CancellationToken cancellationToken = default)
        {
            _client ??= new HttpClient(HttpMessageHandler);
            Guard.IsGreaterThan(version.Major, -1);
            Guard.IsGreaterThan(version.Minor, -1);
            Guard.IsGreaterThan(version.Build, -1);

            var downloadLink = await GetDownloadLink(_client, rootUrl: $"https://dist.ipfs.tech/kubo/v{version.Major}.{version.Minor}.{version.Build}");
            using var downloadStream = await _client.GetStreamAsync(downloadLink);
            var file = await ArchiveCrawlForKuboBinary(downloadStream, cancellationToken);

            Guard.IsNotNull(file);
            return file;
        }

        private async Task<IFile?> ArchiveCrawlForKuboBinary(Stream archiveStream, CancellationToken cancellationToken = default)
        {
            var folder = ExtractArchive(archiveStream);

            await foreach (var item in DepthFirstSearch(folder))
            {
                var noExtName = Path.GetFileNameWithoutExtension(item.Name);
                if (noExtName == "ipfs" || noExtName == "kubo")
                    return item;

                if (IsArchive(item))
                {
                    using var nestedArchiveStream = await item.OpenStreamAsync(FileAccess.Read, cancellationToken);
                    var foundBinary = await ArchiveCrawlForKuboBinary(nestedArchiveStream, cancellationToken);
                    if (foundBinary is not null)
                        return foundBinary;
                }
            }

            return null;
            static bool IsArchive(IFile file) => Path.GetExtension(file.Name) is string extension && (extension.Trim('.') == "zip" || extension.Trim('.') == "tar" || extension.Trim('.') == "gz");
        }

        private static async IAsyncEnumerable<IFile> DepthFirstSearch(IFolder folder)
        {
            await foreach (var file in folder.GetFilesAsync())
                yield return file;

            await foreach (var subfolder in folder.GetFoldersAsync())
                await foreach (var subfile in DepthFirstSearch(subfolder))
                    yield return subfile;
        }

        private IFolder ExtractArchive(Stream stream)
        {
            var tempLocation = Path.GetTempPath();
            var tempArchiveFolder = Directory.CreateDirectory(Path.Combine(tempLocation, $"{typeof(KuboDownloader).FullName}.{Guid.NewGuid()}"));

            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(tempArchiveFolder.FullName, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

            return new SystemFolder(tempArchiveFolder.FullName);
        }

        private static async Task<string> GetDownloadLink(HttpClient client, string rootUrl)
        {
            var distJsonUrl = $"{rootUrl}/dist.json";

            using var distJsonStream = await client.GetStreamAsync(distJsonUrl);
            Guard.IsNotNull(distJsonStream);

            var distData = await JsonSerializer.DeserializeAsync(distJsonStream, KuboDistributionJsonContext.Default.KuboDistributionData);
            Guard.IsNotNull(distData);
            Guard.IsNotNull(distData.Platforms);

            var shipyardCanonicalPlatform = GetCurrentIpfsShipyardCompatibleOperatingSystemId();
            Guard.IsNotNullOrWhiteSpace(shipyardCanonicalPlatform);

            var platform = distData.Platforms[shipyardCanonicalPlatform];
            Guard.IsNotNull(platform?.Archs);

            var shipyardCanonicalArchitecture = GetCurrentIpfsShipyardCompatibleArchitectureId();
            var relativeFilePath = platform.Archs[shipyardCanonicalArchitecture].Link;

            return $"{rootUrl}{relativeFilePath}";
        }

        private static async Task<string> FindLatestVersion(HttpClient client, string url = "https://dist.ipfs.tech/kubo/versions")
        {
            var versionInformationFromServer = await client.GetStringAsync(url);
            Guard.IsNotNullOrWhiteSpace(versionInformationFromServer);

            var versions = versionInformationFromServer.Split('\n').Where(x => !x.Contains("-rc") && x.Length > 0).Select(GetVersionFromRawCanonicalString);

            return versions.OrderByDescending(x => x.Version).First().RawVersion;

            (Version Version, string RawVersion) GetVersionFromRawCanonicalString(string rawVersionString)
            {
                var cleanVersionString = rawVersionString.Trim().TrimStart('v');
                return (Version.Parse(cleanVersionString), rawVersionString);
            }
        }

        private static string GetCurrentIpfsShipyardCompatibleOperatingSystemId()
        {
            var osId = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                osId = "windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                osId = "linux";

#if NET5_0_OR_GREATER
            if (OperatingSystem.IsFreeBSD())
                osId = "freebsd";
#endif

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                osId = "darwin";

            if (string.IsNullOrWhiteSpace(osId))
                throw new PlatformNotSupportedException($"Platform not supported.");

            return osId;
        }

        private static string GetCurrentIpfsShipyardCompatibleArchitectureId()
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                Architecture.X64 => "amd64",
                Architecture.X86 => "386",
                _ => throw new PlatformNotSupportedException($"Architecture {RuntimeInformation.ProcessArchitecture} not supported.")
            };
        }
    }
}
