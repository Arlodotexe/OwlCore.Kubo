using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore.Storage;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace OwlCore.Kubo;


/// <summary>
/// Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture.
/// </summary>
public static class KuboDownloader
{
    private const string HttpProtocolPrefix = "https:/";
    private const string IpnsProtocolPrefix = "/ipns";
    private const string DnsDomain = "dist.ipfs.tech";
    private const string DnsDomainKuboPath = "kubo";

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <returns>A file with a stream that contains the latest Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static Task<IFile> GetLatestBinaryAsync(CancellationToken cancellationToken = default)
    {
        return GetLatestBinaryAsync(new HttpClient(new HttpClientHandler()), cancellationToken);
    }

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="client">The client to use for retrieval.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <returns>A file with a stream that contains the latest Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static async Task<IFile> GetLatestBinaryAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var httpKuboSourcePath = $"{HttpProtocolPrefix}/{DnsDomain}/{DnsDomainKuboPath}";

        // Get the list of available versions
        var versionsFile = new HttpFile($"{httpKuboSourcePath}/versions", client);

        // Scan the versions file and return the latest
        var latestVersion = await GetLatestKuboVersionAsync(versionsFile);
        var rawVersion = $"v{latestVersion.Major}.{latestVersion.Minor}.{latestVersion.Build}";

        // Scan the latest dist.json and get the relative path to the binary archive.
        var distJson = new HttpFile($"{httpKuboSourcePath}/{rawVersion}/dist.json", client);
        var binaryArchiveRelativeDownloadLink = await GetDownloadLink(distJson);

        // Set up the archive file and use ArchiveFolder to crawl the contents of the archive for the Kubo binary.
        var binaryArchive = new HttpFile($"{httpKuboSourcePath}/{rawVersion}/{binaryArchiveRelativeDownloadLink}", client);
        var kuboBinary = await SearchArchiveForKuboBinary(binaryArchive, cancellationToken);

        Guard.IsNotNull(kuboBinary);

        // The binary can be extracted from the archive by name, returning the file allows the consumer to copy the Stream directly from the internet to a new location.
        return kuboBinary;
    }

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="client">The client to use for retrieval.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <returns>A file with a stream that contains the latest Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static async Task<IFile> GetLatestBinaryAsync(IpfsClient client, CancellationToken cancellationToken = default)
    {
        var ipfsKuboSourcePath = $"{IpnsProtocolPrefix}/{DnsDomain}/{DnsDomainKuboPath}";

        // Get the list of available versions
        var versionsFile = new IpnsFile($"{ipfsKuboSourcePath}/versions", client);

        // Scan the versions file and return the latest
        var latestVersion = await GetLatestKuboVersionAsync(versionsFile);
        var rawVersion = $"v{latestVersion.Major}.{latestVersion.Minor}.{latestVersion.Build}";

        // Scan the latest dist.json and get the relative path to the binary archive.
        var distJson = new IpnsFile($"{ipfsKuboSourcePath}/{rawVersion}/dist.json", client);
        var binaryArchiveRelativeDownloadLink = await GetDownloadLink(distJson);

        // Set up the archive file and use ArchiveFolder to crawl the contents of the archive for the Kubo binary.
        var binaryArchive = new IpnsFile($"{ipfsKuboSourcePath}/{rawVersion}/{binaryArchiveRelativeDownloadLink}", client);
        var kuboBinary = await SearchArchiveForKuboBinary(binaryArchive, cancellationToken);

        Guard.IsNotNull(kuboBinary);

        // The binary can be extracted from the archive by name, returning the file allows the consumer to copy the Stream directly from the internet to a new location.
        return kuboBinary;
    }

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <param name="version">The version to download.</param>
    /// <returns>A file with a stream that contains the requested Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static Task<IFile> GetBinaryVersionAsync(Version version, CancellationToken cancellationToken = default)
    {
        return GetBinaryVersionAsync(new HttpClient(), version, cancellationToken);
    }

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="client">The client to use for retrieval.</param>
    /// <param name="version">The version to download.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <returns>A file with a stream that contains the requested Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static async Task<IFile> GetBinaryVersionAsync(HttpClient client, Version version, CancellationToken cancellationToken = default)
    {
        var httpKuboSourcePath = $"{HttpProtocolPrefix}/{DnsDomain}/{DnsDomainKuboPath}";
        var rawVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

        Guard.IsGreaterThanOrEqualTo(version.Major, 0);
        Guard.IsGreaterThanOrEqualTo(version.Minor, 0);
        Guard.IsGreaterThanOrEqualTo(version.Build, 0);

        // Scan the dist.json for this version and get the relative path to the binary archive.
        var distJson = new HttpFile($"{httpKuboSourcePath}/{rawVersion}/dist.json", client);
        var binaryArchiveRelativeDownloadLink = await GetDownloadLink(distJson);

        // Set up the archive file and use ArchiveFolder to crawl the contents of the archive for the Kubo binary.
        var binaryArchive = new HttpFile($"{httpKuboSourcePath}/{rawVersion}/{binaryArchiveRelativeDownloadLink}", client);
        var kuboBinary = await SearchArchiveForKuboBinary(binaryArchive, cancellationToken);

        Guard.IsNotNull(kuboBinary);

        // The binary can be extracted from the archive by name, returning the file allows the consumer to copy the Stream directly from the internet to a new location.
        return kuboBinary;
    }

    /// <summary>
    /// Get a stream of the latest binary from dist.ipfs.tech for the current Platform and Architecture without downloading it.
    /// </summary>
    /// <param name="client">The client to use for retrieval.</param>
    /// <param name="version">The version to download.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing task.</param>
    /// <returns>A file with a stream that contains the requested Kubo binary. Seeking this stream will download it directly from source.</returns>
    public static async Task<IFile> GetBinaryVersionAsync(IpfsClient client, Version version, CancellationToken cancellationToken = default)
    {
        var ipfsKuboSourcePath = $"{IpnsProtocolPrefix}/{DnsDomain}/{DnsDomainKuboPath}";
        var rawVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

        Guard.IsGreaterThanOrEqualTo(version.Major, 0);
        Guard.IsGreaterThanOrEqualTo(version.Minor, 0);
        Guard.IsGreaterThanOrEqualTo(version.Build, 0);

        // Scan the dist.json for this version and get the relative path to the binary archive.
        var distJson = new IpnsFile($"{ipfsKuboSourcePath}/{rawVersion}/dist.json", client);
        var binaryArchiveRelativeDownloadLink = await GetDownloadLink(distJson);

        // Set up the archive file and use ArchiveFolder to crawl the contents of the archive for the Kubo binary.
        var binaryArchive = new IpnsFile($"{ipfsKuboSourcePath}/{rawVersion}/{binaryArchiveRelativeDownloadLink}", client);
        var kuboBinary = await SearchArchiveForKuboBinary(binaryArchive, cancellationToken);

        Guard.IsNotNull(kuboBinary);

        // The binary can be extracted from the archive by name, returning the file allows the consumer to copy the Stream directly from the internet to a new location.
        return kuboBinary;
    }

    internal static async Task<IFile?> SearchArchiveForKuboBinary(IFile archiveFile, CancellationToken cancellationToken = default)
    {
        // Open non-seekable archive file stream from HttpFile/IpnsFile
        var archiveStream = await archiveFile.OpenStreamAsync();

        using var folder = new OwlCore.Storage.SharpCompress.ReadOnlyArchiveFolder(archiveFile);

        await foreach (var item in DepthFirstSearch(folder).WithCancellation(cancellationToken))
        {
            var noExtName = Path.GetFileNameWithoutExtension(item.Name);
            if (noExtName == "ipfs" || noExtName == "kubo")
                return item;

            if (IsArchive(item))
            {
                var foundBinary = await SearchArchiveForKuboBinary(item, cancellationToken);
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

    /// <summary>
    /// Reads the provided dist.json for a specific Kubo version and returns the appropriate download link for your current architecture and platform.
    /// </summary>
    /// <param name="kuboVersionDistJson">The dist.json file to scan.</param>
    /// <returns>The relative file path where the binary archive can be found.</returns>
    private static async Task<string> GetDownloadLink(IFile kuboVersionDistJson)
    {
        Guard.IsTrue(kuboVersionDistJson.Name == "dist.json", kuboVersionDistJson.Name);

        using var distJsonStream = await kuboVersionDistJson.OpenStreamAsync();
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
        Guard.IsNotNullOrWhiteSpace(relativeFilePath);

        return relativeFilePath.Trim('/');
    }

    /// <summary>
    /// Retrieves and parses the the latest version of Kubo from the provided url.
    /// </summary>
    /// <param name="versionsFile"></param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public static async Task<Version> GetLatestKuboVersionAsync(IFile versionsFile, CancellationToken cancellationToken = default)
    {
        using var versionsFileStream = await versionsFile.OpenStreamAsync(FileAccess.Read, cancellationToken);

        using var reader = new StreamReader(versionsFileStream);
        var versionInformationFromServer = reader.ReadToEnd();

        Guard.IsNotNullOrWhiteSpace(versionInformationFromServer);

        var versions = versionInformationFromServer
            .Split('\n')
            .Where(x => !x.Contains("-rc") && x.Length > 0)
            .Select(GetVersionFromRawCanonicalString)
            .OrderByDescending(x => x)
            .First();

        return versions;

        Version GetVersionFromRawCanonicalString(string rawVersionString)
        {
            var cleanVersionString = rawVersionString.Trim().TrimStart('v');
            return Version.Parse(cleanVersionString);
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
