using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Http;
using Newtonsoft.Json.Linq;
using OwlCore.Diagnostics;
using OwlCore.Extensions;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OwlCore.Kubo;

/// <summary>
/// An easy bootstrapper for the Kubo binary.
/// </summary>
public class KuboBootstrapper : IDisposable
{
    private IpfsClient? _client;
    private readonly Func<CancellationToken, Task<IFile>> _getKuboBinaryFile;
    private SystemFile? _kuboBinaryFile;
    private SystemFolder? _repoFolder;

    /// <summary>
    /// Create a new instance of <see cref="KuboBootstrapper"/>.
    /// </summary>
    /// <param name="getKuboBinaryFile">A kubo binary that is compatible with the currently running OS and Architecture.</param>
    /// <param name="repoPath">The path to the kubo repository folder. Provided to Kubo.</param>
    public KuboBootstrapper(string repoPath, Func<CancellationToken, Task<IFile>> getKuboBinaryFile)
    {
        RepoPath = repoPath;
        _getKuboBinaryFile = getKuboBinaryFile;
    }

    /// <summary>
    /// Create a new instance of <see cref="KuboBootstrapper"/>.
    /// </summary>
    /// <param name="repoPath">The path to the kubo repository folder. Provided to Kubo.</param>
    /// <param name="kuboVersion">The version of Kubo to download to <see cref="BinaryWorkingFolder"/>.</param>
    public KuboBootstrapper(string repoPath, Version kuboVersion)
        : this(repoPath, canceltok => KuboDownloader.GetBinaryVersionAsync(kuboVersion, canceltok))
    {
    }

    /// <summary>
    /// Create a new instance of <see cref="KuboBootstrapper"/>.
    /// </summary>
    /// <param name="repoPath">The path to the kubo repository folder. Provided to Kubo.</param>
    public KuboBootstrapper(string repoPath)
        : this(repoPath, KuboDownloader.GetLatestBinaryAsync)
    {
    }

    /// <summary>
    /// The process being used to run Kubo.
    /// </summary>
    public Process? Process { get; private set; }

    /// <summary>
    /// The environment variables to set for the Kubo process.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// The path to the kubo repository folder. Provided to Kubo.
    /// </summary>
    public string RepoPath { get; set; }

    /// <summary>
    /// The folder containing the kubo repository.
    /// </summary>
    public SystemFolder RepoFolder => _repoFolder ??= new SystemFolder(RepoPath);

    /// <summary>
    /// The Kubo binary being bootstrapped, compatible with the currently running OS and Architecture.
    /// </summary>
    public SystemFile? KuboBinaryFile => _kuboBinaryFile;

    /// <summary>
    /// Gets or sets the folder where the Kubo binary will be copied to and run from via a new <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    /// <remarks>
    /// This location must be one where the current environment can execute a binary. For both Linux and Windows, one common location for this is the Temp folder.
    /// </remarks>
    public SystemFolder BinaryWorkingFolder { get; set; } = new(Path.GetTempPath());

    /// <summary>
    /// The address where the API should be hosted.
    /// </summary>
    public Uri ApiUri { get; set; } = new("http://127.0.0.1:5001");

    /// <summary>
    /// The address where the gateway should be hosted.
    /// </summary>
    public Uri GatewayUri { get; set; } = new("http://127.0.0.1:8080");

    /// <summary>
    /// Gets or sets an enum that determines how to use the supplied <see cref="ApiUri"/>.
    /// </summary>
    public ConfigMode ApiUriMode { get; set; } = ConfigMode.OverwriteExisting;

    /// <summary>
    /// Gets or sets an enum that determines how to use the supplied <see cref="ApiUri"/>.
    /// </summary>
    public ConfigMode GatewayUriMode { get; set; } = ConfigMode.OverwriteExisting;

    /// <summary>
    /// The behavior when a node is already running (when the repo is locked).
    /// </summary>
    public BootstrapLaunchConflictMode LaunchConflictMode { get; set; } = BootstrapLaunchConflictMode.Throw;

    /// <summary>
    /// Gets or creates an <see cref="IpfsClient"/> to interact with the given <see cref="ApiUri"/>.
    /// </summary>
    public IpfsClient Client => _client ??= new IpfsClient { ApiUri = ApiUri };

    /// <summary>
    /// The routing mode that should be used.
    /// </summary>
    public DhtRoutingMode RoutingMode { get; init; } = DhtRoutingMode.Auto;

    /// <summary>
    /// <para>
    /// This alternative Amino DHT client with a Full-Routing-Table strategy will do a complete scan of the DHT every hour and record all nodes found. Then when a lookup is tried instead of having to go through multiple Kad hops it is able to find the 20 final nodes by looking up the in-memory recorded network table.
    /// </para>
    /// 
    /// <para>
    /// This means sustained higher memory to store the routing table and extra CPU and network bandwidth for each network scan. However the latency of individual read/write operations should be ~10x faster and the provide throughput up to 6 million times faster on larger datasets!
    /// </para>
    /// </summary>
    /// <remarks>
    /// When it is enabled:
    /// <list type="bullet">Client DHT operations (reads and writes) should complete much faster.</list>
    /// <list type="bullet">The provider will now use a keyspace sweeping mode allowing to keep alive CID sets that are multiple orders of magnitude larger.
    /// <list type="bullet">The standard Bucket-Routing-Table DHT will still run for the DHT server (if the DHT server is enabled). This means the classical routing table will still be used to answer other nodes. This is critical to maintain to not harm the network.</list>
    /// </list>
    /// <list type="bullet">The operations 'ipfs stats dht' will default to showing information about the accelerated DHT client.</list>
    ///
    /// Caveats:
    /// <list type="bullet">
    /// Running the accelerated client likely will result in more resource consumption (connections, RAM, CPU, bandwidth)
    ///     <list type="bullet">Users that are limited in the number of parallel connections their machines/networks can perform will likely suffer.</list>
    ///     <list type="bullet">The resource usage is not smooth as the client crawls the network in rounds and reproviding is similarly done in rounds.</list>
    ///     <list type="bullet">Users who previously had a lot of content but were unable to advertise it on the network will see an increase in egress bandwidth as their nodes start to advertise all of their CIDs into the network. If you have lots of data entering your node that you don't want to advertise, then consider using Reprovider Strategies to reduce the number of CIDs that you are reproviding. Similarly, if you are running a node that deals mostly with short-lived temporary data (e.g. you use a separate node for ingesting data then for storing and serving it) then you may benefit from using Strategic Providing to prevent advertising of data that you ultimately will not have.</list>
    /// </list>
    /// <list type="bullet">Currently, the DHT is not usable for queries for the first 5-10 minutes of operation as the routing table is being prepared. This means operations like searching the DHT for particular peers or content will not work initially.</list>
    /// <list type="bullet">You can see if the DHT has been initially populated by running 'ipfs stats dht'.</list>
    /// <list type="bullet">Currently, the accelerated DHT client is not compatible with LAN-based DHTs and will not perform operations against them.</list>
    /// 
    /// </remarks>
    public bool UseAcceleratedDHTClient { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the filestore feature should be enabled. Allows files to be added without duplicating the space they take up on disk.
    /// </summary>
    /// <remarks>
    /// To add files using the filestore, pass the NoCopy option to <see cref="AddFileOptions"/> in the <see cref="IFileSystemApi.AddAsync(FilePart[], FolderPart[], AddFileOptions?, CancellationToken)"/> method.
    /// </remarks>
    public bool EnableFilestore { get; set; }

    /// <summary>
    /// The Kubo profiles that will be applied before starting the daemon.
    /// </summary>
    public List<string> StartupProfiles { get; } = new();

    /// <summary>
    /// Loads the binary and starts it in a new process.
    /// </summary>
    /// <param name="cancellationToken">Cancels the startup process.</param>
    /// <returns></returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Get or download the Kubo binary, if needed.
        _kuboBinaryFile ??= await GetOrDownloadExecutableKuboBinary(cancellationToken);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            SetExecutablePermissionsForBinary(_kuboBinaryFile);

        OwlCore.Diagnostics.Logger.LogTrace($"Checking if repo at {RepoPath} is locked.");
        var repoLocked = await IsRepoLockedAsync(new SystemFolder(RepoPath), cancellationToken);

        // If the repo is locked, check the launch conflict mode
        if (repoLocked)
        {
            switch (LaunchConflictMode)
            {
                case BootstrapLaunchConflictMode.Throw:
                    throw new InvalidOperationException("The repository is locked and the launch conflict mode is set to throw.");

                case BootstrapLaunchConflictMode.Relaunch:
                    // Attach to the existing node and shut it down
                    var apiMultiAddr = await GetApiAsync(cancellationToken);
                    Guard.IsNotNull(apiMultiAddr);
                    await new IpfsClient { ApiUri = TcpIpv4MultiAddressToUri(apiMultiAddr) }.Generic.ShutdownAsync();
                    break;

                case BootstrapLaunchConflictMode.Attach:
                    ApiUriMode = ConfigMode.UseExisting;
                    GatewayUriMode = ConfigMode.UseExisting;
                    break;
            }
        }

        OwlCore.Diagnostics.Logger.LogTrace($"Getting existing ApiUri and GatewayUri for repo at {RepoPath} if they exist.");
        {
            if (ApiUriMode == ConfigMode.UseExisting)
            {
                var apiMultiAddr = await GetApiAsync(cancellationToken);
                if (apiMultiAddr is not null)
                    ApiUri = TcpIpv4MultiAddressToUri(apiMultiAddr);
            }

            if (GatewayUriMode == ConfigMode.UseExisting)
            {
                var gatewayMultiAddr = await GetGatewayAsync(cancellationToken);
                if (gatewayMultiAddr is not null)
                    GatewayUri = TcpIpv4MultiAddressToUri(gatewayMultiAddr);
            }
        }

        if (LaunchConflictMode == BootstrapLaunchConflictMode.Attach && repoLocked)
        {
            // In attach mode, we don't bootstrap the process. 
            // Accessing the Client property will connect to the running Daemon using the Kubo RPC API port we have set.
            return;
        }

        // Settings must be applied before bootstrapping.
        OwlCore.Diagnostics.Logger.LogTrace($"Applying settings to repo at {RepoPath}.");
        await ApplySettingsAsync(cancellationToken);

        // Setup process info
        var processStartInfo = new ProcessStartInfo(_kuboBinaryFile.Path, $"daemon --routing={RoutingMode.ToString().ToLowerInvariant()} --enable-pubsub-experiment --enable-namesys-pubsub --repo-dir \"{RepoPath}\"")
        {
            CreateNoWindow = true,
        };

        // Setup environment variables
        foreach (var item in EnvironmentVariables)
            processStartInfo.EnvironmentVariables.Add(item.Key, item.Value);

        OwlCore.Diagnostics.Logger.LogTrace($"Process info ready, starting process.");
        await StartAsync(processStartInfo, cancellationToken);
    }

    protected virtual async Task StartAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Setup process
        Process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        var process = Process;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        // Create a task completion source to wait for the daemon to be ready.
        var startupCompletion = new TaskCompletionSource<object?>();
        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += ProcessOnErrorDataReceived;

        // Start
        Logger.LogTrace($"Starting process {process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();

        // Close the process if cancellation is called early.
        var cancellationCleanup = cancellationToken.Register(() =>
        {
            startupCompletion.TrySetCanceled();
            process.Dispose();
        });

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        cancellationToken.ThrowIfCancellationRequested();

        // Wait for daemon to be ready
        Logger.LogTrace($"Waiting for daemon to start...");
        await startupCompletion.Task;

        cancellationToken.ThrowIfCancellationRequested();

        process.OutputDataReceived -= Process_OutputDataReceived;
        process.ErrorDataReceived -= ProcessOnErrorDataReceived;

        cancellationCleanup.Dispose();

        void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (e.Data is not null)
                OwlCore.Diagnostics.Logger.LogInformation(e.Data);

            if (e.Data?.Contains("Daemon is ready") ?? false)
            {
                startupCompletion.SetResult(null);
                Logger.LogTrace($"Daemon has started successfully.");
            }
        }

        async void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (e.Data is not null)
                OwlCore.Diagnostics.Logger.LogError(e.Data);

            if (!string.IsNullOrWhiteSpace(e.Data) && e.Data.Contains("Error: "))
            {
                if (e.Data.Contains($"/ip4/{ApiUri.Host}/tcp/{ApiUri.Port}") && e.Data.Contains("bind"))
                {
                    process.OutputDataReceived -= Process_OutputDataReceived;
                    process.ErrorDataReceived -= ProcessOnErrorDataReceived;

                    // Failed to bind to port, process may be orphaned.
                    // Connect and shutdown via api instead.
                    await new IpfsClient { ApiUri = ApiUri }.Generic.ShutdownAsync();
                    await StartAsync(processStartInfo, cancellationToken);
                    startupCompletion.SetResult(null);
                    return;
                }

                throw new InvalidOperationException($"Error received while starting daemon: {e.Data}");
            }
        }
    }

    /// <summary>
    /// Stops the bootstrapped process.
    /// </summary>
    public virtual void Stop()
    {
        if (_kuboBinaryFile is not null && Process is not null && !Process.HasExited)
        {
            // Gracefully shutdown the running Kubo Daemon
            RunExecutable(_kuboBinaryFile, $"shutdown --repo-dir \"{RepoPath}\"", throwOnError: false);
            Process.Close();
        }

        Process = null;
    }

    /// <summary>
    /// Gets or downloads the Kubo binary and sets it up in the <see cref="BinaryWorkingFolder"/>.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    protected virtual async Task<SystemFile> GetOrDownloadExecutableKuboBinary(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IFile? existingBinary = await BinaryWorkingFolder
            .GetFilesAsync(cancellationToken)
            .FirstOrDefaultAsync(x => x.Name.StartsWith("ipfs") || x.Name.StartsWith("kubo"), cancellationToken: cancellationToken);

        if (existingBinary is null)
        {
            OwlCore.Diagnostics.Logger.LogTrace($"Binary not found in working folder at {BinaryWorkingFolder.Path}, retrieving it.");

            // Retrieve the kubo binary if we don't have it
            existingBinary ??= await _getKuboBinaryFile(cancellationToken);

            // Copy it into the binary working folder, store the new file for use.
            OwlCore.Diagnostics.Logger.LogTrace($"Copying binary to working folder at {BinaryWorkingFolder.Path}.");
            return (SystemFile)await BinaryWorkingFolder.CreateCopyOfAsync(existingBinary, overwrite: false, cancellationToken);
        }

        return (SystemFile)existingBinary;
    }

    /// <summary>
    /// Checks if the Kubo repository at the provided <see cref="RepoPath"/> has an active repo.lock.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a boolean value. If true, the repo is locked, otherwise false.</returns>
    public Task<bool> IsRepoLockedAsync(CancellationToken cancellationToken) => IsRepoLockedAsync(new SystemFolder(RepoPath), cancellationToken);

    /// <summary>
    /// Gets the gateway address from the provided <see cref="RepoPath"/>.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The <see cref="MultiAddress"/> formatted gateway address, if found.</returns>
    public Task<MultiAddress?> GetGatewayAsync(CancellationToken cancellationToken) => GetGatewayAsync(new SystemFolder(RepoPath), cancellationToken);

    /// <summary>
    /// Gets the gateway address from the provided <see cref="RepoPath"/>.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The <see cref="MultiAddress"/> formatted api address, if found.</returns>
    public Task<MultiAddress?> GetApiAsync(CancellationToken cancellationToken) => GetApiAsync(new SystemFolder(RepoPath), cancellationToken);

    /// <summary>
    /// Checks of the Kubo repository at the provided <paramref name="kuboRepo"/> has an active repo.lock.
    /// </summary>
    /// <param name="kuboRepo">The Kubo repository to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns></returns>
    public static async Task<bool> IsRepoLockedAsync(IFolder kuboRepo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var target = await kuboRepo.GetFirstByNameAsync("repo.lock", cancellationToken);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a TcpIpv4 <see cref="MultiAddress"/> to a <see cref="Uri"/> with the location and port.
    /// </summary>
    /// <param name="multiAddress">The <see cref="MultiAddress"/> to transform.</param>
    /// <returns>A standard uri containing the location and port provided from the <paramref name="multiAddress"/>.</returns>
    public static Uri TcpIpv4MultiAddressToUri(MultiAddress multiAddress)
    {
        return new Uri($"http://{multiAddress.Protocols[0].Value}:{multiAddress.Protocols[1].Value}");
    }

    /// <summary>
    /// Gets the gateway address from the provided <paramref name="kuboRepo"/>.
    /// </summary>
    /// <param name="kuboRepo">The Kubo repository to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The <see cref="MultiAddress"/> formatted gateway address, if found.</returns>
    public static async Task<MultiAddress?> GetGatewayAsync(IFolder kuboRepo, CancellationToken cancellationToken)
    {
        IFile? file = null;
        try
        {
            file = (IFile)await kuboRepo.GetFirstByNameAsync("config", cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        using var stream = await file.OpenStreamAsync(FileAccess.Read, cancellationToken);
        var bytes = await stream.ToBytesAsync(cancellationToken: cancellationToken);

        var strng = Encoding.UTF8.GetString(bytes).Trim();
        var config = JObject.Parse(strng);

        var addresses = config["Addresses"];
        Guard.IsNotNull(addresses);

        var gateway = addresses["Gateway"]?.Value<string>();
        Guard.IsNotNullOrWhiteSpace(gateway);

        return new MultiAddress(gateway);
    }

    /// <summary>
    /// Gets the API address from the provided <paramref name="kuboRepo"/>.
    /// </summary>
    /// <param name="kuboRepo">The Kubo repository to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The <see cref="MultiAddress"/> formatted api address, if found.</returns>
    public static async Task<MultiAddress?> GetApiAsync(IFolder kuboRepo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IFile? file = null;
        try
        {
            file = (IFile)await kuboRepo.GetFirstByNameAsync("config", cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        using var stream = await file.OpenStreamAsync(FileAccess.Read, cancellationToken);
        var bytes = await stream.ToBytesAsync(cancellationToken: cancellationToken);

        var strng = Encoding.UTF8.GetString(bytes).Trim();
        var config = JObject.Parse(strng);

        var addresses = config["Addresses"];
        Guard.IsNotNull(addresses);

        var api = addresses["API"]?.Value<string>();
        Guard.IsNotNullOrWhiteSpace(api);

        return new MultiAddress(api);
    }

    /// <summary>
    /// Initializes the local node with the provided settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public virtual async Task ApplySettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Guard.IsNotNullOrWhiteSpace(_kuboBinaryFile?.Path);

        // Init if needed
        try
        {
            RunExecutable(_kuboBinaryFile, $"init --repo-dir \"{RepoPath}\"", throwOnError: false);
        }
        catch
        {
            // ignored
        }

        await ApplyExperimentalConfigSettingsAsync(cancellationToken);
        await ApplyRoutingSettingsAsync(cancellationToken);
        await ApplyPortSettingsAsync(cancellationToken);
        await ApplyStartupProfileSettingsAsync(cancellationToken);
    }

    /// <summary>
    /// Initializes the local node with the provided startup profile settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected virtual Task ApplyStartupProfileSettingsAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(_kuboBinaryFile?.Path);

        // Startup profiles
        foreach (var profile in StartupProfiles)
            RunExecutable(_kuboBinaryFile, $"config --repo-dir \"{RepoPath}\" profile apply {profile}", throwOnError: true);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the local node with the provided port settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected virtual async Task ApplyPortSettingsAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(_kuboBinaryFile?.Path);

        // Port options
        if (ApiUriMode == ConfigMode.OverwriteExisting)
            RunExecutable(_kuboBinaryFile, $"config Addresses.API /ip4/{ApiUri.Host}/tcp/{ApiUri.Port} --repo-dir \"{RepoPath}\"", throwOnError: true);

        if (GatewayUriMode == ConfigMode.OverwriteExisting)
            RunExecutable(_kuboBinaryFile, $"config Addresses.Gateway /ip4/{GatewayUri.Host}/tcp/{GatewayUri.Port} --repo-dir \"{RepoPath}\"", throwOnError: true);

        if (GatewayUriMode == ConfigMode.UseExisting)
        {
            var existingGatewayUri = await GetGatewayAsync(cancellationToken);
            if (existingGatewayUri is null)
                RunExecutable(_kuboBinaryFile, $"config Addresses.Gateway /ip4/{GatewayUri.Host}/tcp/{GatewayUri.Port} --repo-dir \"{RepoPath}\"", throwOnError: true);
        }

        if (ApiUriMode == ConfigMode.UseExisting)
        {
            var existingApiUri = await GetApiAsync(cancellationToken);
            if (existingApiUri is null)
                RunExecutable(_kuboBinaryFile, $"config Addresses.API /ip4/{ApiUri.Host}/tcp/{ApiUri.Port} --repo-dir \"{RepoPath}\"", throwOnError: true);
        }
    }

    /// <summary>
    /// Initializes the local node with the provided experimental settings.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected virtual Task ApplyExperimentalConfigSettingsAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(_kuboBinaryFile?.Path);

        RunExecutable(_kuboBinaryFile, $"config Experimental.FilestoreEnabled \"{EnableFilestore.ToString().ToLower()}\" --json --repo-dir \"{RepoPath}\"", throwOnError: true);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the local node with the provided routing settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected virtual Task ApplyRoutingSettingsAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(_kuboBinaryFile?.Path);

        RunExecutable(_kuboBinaryFile, $"config Routing.Type {RoutingMode.ToString().ToLowerInvariant()} --repo-dir \"{RepoPath}\"", throwOnError: true);
        RunExecutable(_kuboBinaryFile, $"config Routing.AcceleratedDHTClient \"{UseAcceleratedDHTClient.ToString().ToLower()}\" --json --repo-dir \"{RepoPath}\"", throwOnError: true);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets executable permissions for the given file.
    /// </summary>
    /// <param name="file">The file to adjust execute permissions for.</param>
    protected virtual void SetExecutablePermissionsForBinary(SystemFile file)
    {
        // This should only be used on linux.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        RunExecutable(new SystemFile("/bin/bash"), $"-c \"chmod 777 '{file.Path}'\"", throwOnError: true);
    }

    /// <summary>
    /// Runs the provided executable with the given arguments.
    /// </summary>
    /// <param name="file">The binary file to execute.</param>
    /// <param name="arguments">The execution arguments to provide.</param>
    /// <param name="throwOnError">Whether to throw when stderr is emitted.</param>
    /// <exception cref="InvalidOperationException"></exception>
    protected void RunExecutable(SystemFile file, string arguments, bool throwOnError)
    {
        var processStartInfo = new ProcessStartInfo(file.Path, arguments)
        {
            CreateNoWindow = true,
        };

        var proc = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardInput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.UseShellExecute = false;

        proc.Start();

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        proc.OutputDataReceived += Process_OutputDataReceived;
        proc.ErrorDataReceived += ProcOnErrorDataReceived;

        proc.WaitForExit();

        proc.OutputDataReceived -= Process_OutputDataReceived;
        proc.ErrorDataReceived -= ProcOnErrorDataReceived;

        void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is not null)
                OwlCore.Diagnostics.Logger.LogInformation(e.Data);
        }

        void ProcOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data) && throwOnError)
            {
                throw new InvalidOperationException($"Error received while running {file.Path} {arguments}: {e.Data}");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        Process?.Dispose();
    }
}