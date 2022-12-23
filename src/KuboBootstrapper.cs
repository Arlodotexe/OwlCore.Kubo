using OwlCore.Storage;
using OwlCore.Storage.SystemIO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OwlCore.Kubo;

/// <summary>
/// An easy bootstrapper for the Kubo binary.
/// </summary>
public class KuboBootstrapper : IDisposable
{
    private readonly IFile _kuboBinary;

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
    public Process? Process { get; set; }

    /// <summary>
    /// The path to the kubo repository folder. Provided to Kubo.
    /// </summary>
    public string RepoPath { get; }

    /// <summary>
    /// The address where the API should be hosted. Provided to Kubo.
    /// </summary>
    public Uri ApiUri { get; set; } = new Uri("http://127.0.0.1:5001");

    /// <summary>
    /// Loads the binary and starts it in a new process.
    /// </summary>
    /// <param name="cancellationToken">Cancels the startup process.</param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var executableBinary = await CopyToTempFolder(_kuboBinary, cancellationToken);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await SetExecutablePermissionsForBinary(executableBinary.Path);
        }

        var processStartInfo = new ProcessStartInfo(executableBinary.Path, $"daemon --init --enable-pubsub-experiment --enable-namesys-pubsub --api /ip4/{ApiUri.Host}/tcp/{ApiUri.Port}");

        if (!processStartInfo.EnvironmentVariables.ContainsKey("IPFS_PATH"))
            processStartInfo.EnvironmentVariables.Add("IPFS_PATH", RepoPath);
        
        processStartInfo.CreateNoWindow = true;

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

        cancellationToken.ThrowIfCancellationRequested();

        await startupCompletion.Task;

        cancellationToken.ThrowIfCancellationRequested();

        Process.OutputDataReceived -= Process_OutputDataReceived;

        cancellationCleanup.Dispose();

        void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data?.Contains("Daemon is ready") ?? false)
                startupCompletion.SetResult(null);
        }
    }

    private Task SetExecutablePermissionsForBinary(string filePath)
    {
        // This should only be used on linux.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Task.CompletedTask;

        var proc = Process.Start("/bin/bash", $"-c \"chmod 777 {filePath}\"");

#if NET5_0_OR_GREATER
        return proc.WaitForExitAsync();
#elif NETSTANDARD2_0_OR_GREATER
        proc.WaitForExit();
        return Task.CompletedTask;
#endif
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
    /// Copies the provided <paramref name="file"/> to a temporary, addressable folder in local storage so it can be safely executed by <see cref="ProcessStartInfo"/>.
    /// </summary>
    /// <returns>A file in temporary storage.</returns>
    private async Task<IAddressableFile> CopyToTempFolder(IFile file, CancellationToken cancellationToken)
    {
        var tempFolder = new SystemFolder(Path.GetTempPath());
        var newFolder = await tempFolder.CreateFolderAsync($"KuboBootstrapper-{Guid.NewGuid()}");
        var destination = (IModifiableFolder)newFolder;

        var copiedFile = await destination.CreateCopyOfAsync(file, overwrite: true, cancellationToken);

        return (IAddressableFile)copiedFile;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Process is not null && !Process.HasExited)
            Process.Kill();

        Process?.Dispose();
    }
}