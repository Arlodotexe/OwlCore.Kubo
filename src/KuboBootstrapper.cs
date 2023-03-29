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
    private readonly IFile _kuboBinary;
    private SystemFile? _executableBinary;

    /// <summary>
    /// Create a new instance of <see cref="KuboBootstrapper"/>.
    /// </summary>
    /// <param name="kuboBinary">A kubo binary that is compatible with the currently running OS and Architecture.</param>
    /// <param name="repoPath">The path to the kubo repository folder. Provided to Kubo.</param>
    public KuboBootstrapper(IFile kuboBinary, string repoPath)
    {
        RepoPath = repoPath;
        _kuboBinary = kuboBinary;
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
    /// The folder where Kubo will be copied to and run from via a new <see cref="Process"/>.
    /// </summary>
    public SystemFolder BinaryWorkingFolder { get; set; } = new(Path.GetTempPath());

    /// <summary>
    /// Loads the binary and starts it in a new process.
    /// </summary>
    /// <param name="cancellationToken">Cancels the startup process.</param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var bootstrapperBinaryFolder = (SystemFolder)await BinaryWorkingFolder.CreateFolderAsync(name: nameof(KuboBootstrapper), overwrite: false, cancellationToken);
        _executableBinary = (SystemFile)await bootstrapperBinaryFolder.CreateCopyOfAsync(_kuboBinary, overwrite: false, cancellationToken);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await SetExecutablePermissionsForBinary(_executableBinary);

        await ApplySettingsAsync(cancellationToken);

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
            if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("[WARN]"))
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
        if (Process is not null && !Process.HasExited)
            Process.Kill();

        Process = null;
    }

    /// <summary>
    /// Initializes the local node with the provided settings.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task ApplySettingsAsync(CancellationToken cancellationToken = default)
    {
        Guard.IsNotNullOrWhiteSpace(_executableBinary?.Path);

        try
        {
            await RunAsync(_executableBinary, $"init --repo-dir {RepoPath} --empty-repo", throwOnError: false, cancellationToken);
        }
        catch
        {
            // ignored
        }

        await RunAsync(_executableBinary, $"config --repo-dir {RepoPath} Routing.Type {RoutingMode.ToString().ToLowerInvariant()}", throwOnError: true, cancellationToken);
        await RunAsync(_executableBinary, $"config --repo-dir {RepoPath} Addresses.API /ip4/{ApiUri.Host}/tcp/{ApiUri.Port}", throwOnError: true, cancellationToken);
        await RunAsync(_executableBinary, $"config --repo-dir {RepoPath} Addresses.Gateway /ip4/{GatewayUri.Host}/tcp/{GatewayUri.Port}", throwOnError: true, cancellationToken);

        foreach (var profile in StartupProfiles)
            await RunAsync(_executableBinary, $"config --repo-dir {RepoPath} profile apply {profile}", throwOnError: true, cancellationToken);
    }

    private Task SetExecutablePermissionsForBinary(SystemFile file)
    {
        // This should only be used on linux.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Task.CompletedTask;

        return RunAsync(new SystemFile("/bin/bash"), $"-c \"chmod 777 {file}\"", throwOnError: true);
    }

    private async Task RunAsync(SystemFile file, string arguments, bool throwOnError, CancellationToken cancellationToken = default)
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

        using var cancellationCleanup = cancellationToken.Register(() => proc.Dispose());

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        proc.OutputDataReceived += Process_OutputDataReceived;
        proc.ErrorDataReceived += ProcOnErrorDataReceived;
        cancellationToken.ThrowIfCancellationRequested();

#if NET5_0_OR_GREATER
        await proc.WaitForExitAsync();
#elif NETSTANDARD2_0_OR_GREATER
        proc.WaitForExit();
#endif

        cancellationToken.ThrowIfCancellationRequested();
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
        if (Process is not null && !Process.HasExited)
            Process.Kill();

        Process?.Dispose();
    }
}