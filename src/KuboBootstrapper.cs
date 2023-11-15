using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

namespace OwlCore.Kubo;

/// <summary>
/// An easy bootstrapper for the Kubo binary.
/// </summary>
public class KuboBootstrapper : IDisposable
{
    private readonly Func<CancellationToken, Task<IFile>> _getKuboBinaryFile;
    private SystemFile? _executableBinary;

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
        : this(repoPath, canceltok => KuboDownloader.GetLatestBinaryAsync(canceltok))
    {
    }

    /// <summary>
    /// The process being used to run Kubo.
    /// </summary>
    public Process? Process { get; private set; }

    /// <summary>
    /// The path to the kubo repository folder. Provided to Kubo.
    /// </summary>
    public string RepoPath { get; set; }

    /// <summary>
    /// The address where the API should be hosted.
    /// </summary>
    public Uri ApiUri { get; set; } = new("http://127.0.0.1:5001");

    /// <summary>
    /// The address where the gateway should be hosted.
    /// </summary>
    public Uri GatewayUri { get; set; } = new("http://127.0.0.1:8080");

    /// <summary>
    /// The routing mode that should be used.
    /// </summary>
    public DhtRoutingMode RoutingMode { get; init; }

    /// <summary>
    /// The Kubo profiles that will be applied before starting the daemon.
    /// </summary>
    public List<string> StartupProfiles { get; } = new();

    /// <summary>
    /// Gets or sets the folder where the Kubo binary will be copied to and run from via a new <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    /// <remarks>
    /// This location must be one where the current environment can execute a binary. For both Linux and Windows, one common location for this is the Temp folder.
    /// </remarks>
    public SystemFolder BinaryWorkingFolder { get; set; } = new(Path.GetTempPath());

    /// <summary>
    /// Loads the binary and starts it in a new process.
    /// </summary>
    /// <param name="cancellationToken">Cancels the startup process.</param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        IFile? kuboBinary = await BinaryWorkingFolder
            .GetFilesAsync(cancellationToken)
            .FirstOrDefaultAsync(x => x.Name.StartsWith("ipfs") || x.Name.StartsWith("kubo"));

        // Retrieve the kubo binary if we don't have it
        kuboBinary ??= await _getKuboBinaryFile(cancellationToken);

        _executableBinary ??= (SystemFile)await BinaryWorkingFolder.CreateCopyOfAsync(kuboBinary, overwrite: false, cancellationToken);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            SetExecutablePermissionsForBinary(_executableBinary);

        ApplySettings();

        var processStartInfo = new ProcessStartInfo(_executableBinary.Path, $"daemon --routing={RoutingMode.ToString().ToLowerInvariant()} --enable-pubsub-experiment --enable-namesys-pubsub --repo-dir {RepoPath}")
        {
            CreateNoWindow = true
        };

        Process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        Process.StartInfo.RedirectStandardOutput = true;
        Process.StartInfo.RedirectStandardInput = true;
        Process.StartInfo.RedirectStandardError = true;
        Process.StartInfo.UseShellExecute = false;

        Process.Start();

        var cancellationCleanup = cancellationToken.Register(() => Process.Dispose());

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();

        var startupCompletion = new TaskCompletionSource<object?>();
        Process.OutputDataReceived += Process_OutputDataReceived;
        Process.ErrorDataReceived += ProcessOnErrorDataReceived;

        cancellationToken.ThrowIfCancellationRequested();

        await startupCompletion.Task;

        cancellationToken.ThrowIfCancellationRequested();

        Process.OutputDataReceived -= Process_OutputDataReceived;
        Process.ErrorDataReceived -= ProcessOnErrorDataReceived;

        cancellationCleanup.Dispose();

        void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is not null)
                OwlCore.Diagnostics.Logger.LogInformation(e.Data);

            if (e.Data?.Contains("Daemon is ready") ?? false)
                startupCompletion.SetResult(null);
        }

        void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data) && e.Data.Contains("Error: "))
            {
                throw new InvalidOperationException($"Error received while starting daemon: {e.Data}");
            }
        }
    }

    /// <summary>
    /// Stops the bootstrapped process.
    /// </summary>
    public void Stop()
    {
        Guard.IsNotNullOrWhiteSpace(_executableBinary?.Path);

        if (Process is not null && !Process.HasExited)
        {
            // Gracefully shutdown the running Kubo Daemon
            RunExecutable(_executableBinary, $"shutdown --repo-dir {RepoPath}", throwOnError: false);
            Process.Close();
        }

        Process = null;
    }

    /// <summary>
    /// Initializes the local node with the provided settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public void ApplySettings()
    {
        Guard.IsNotNullOrWhiteSpace(_executableBinary?.Path);

        try
        {
            RunExecutable(_executableBinary, $"init --repo-dir {RepoPath}", throwOnError: false);
        }
        catch
        {
            // ignored
        }

        RunExecutable(_executableBinary, $"config --repo-dir {RepoPath} Routing.Type {RoutingMode.ToString().ToLowerInvariant()}", throwOnError: true);
        RunExecutable(_executableBinary, $"config --repo-dir {RepoPath} Addresses.API /ip4/{ApiUri.Host}/tcp/{ApiUri.Port}", throwOnError: true);
        RunExecutable(_executableBinary, $"config --repo-dir {RepoPath} Addresses.Gateway /ip4/{GatewayUri.Host}/tcp/{GatewayUri.Port}", throwOnError: true);

        foreach (var profile in StartupProfiles)
            RunExecutable(_executableBinary, $"config --repo-dir {RepoPath} profile apply {profile}", throwOnError: true);
    }

    private void SetExecutablePermissionsForBinary(SystemFile file)
    {
        // This should only be used on linux.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        RunExecutable(new SystemFile("/bin/bash"), $"-c \"chmod 777 {file.Path}\"", throwOnError: true);
    }

    private void RunExecutable(SystemFile file, string arguments, bool throwOnError)
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