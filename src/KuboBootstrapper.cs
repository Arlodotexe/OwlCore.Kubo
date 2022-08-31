using System.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

namespace OwlCore.Kubo;

/// <summary>
/// An easy bootstrapper for the Kubo binary.
/// </summary>
public class KuboBootstrapper
{
    private readonly IFile _kuboBinary;
    private readonly Uri _apiUri;

    public KuboBootstrapper(IFile kuboBinary, string repoPath, Uri apiUri)
    {
        RepoPath = repoPath;
        _kuboBinary = kuboBinary;
        _apiUri = apiUri;
    }

    /// <summary>
    /// The process being used to run Kubo.
    /// </summary>
    public Process? Process { get; set; }

    /// <summary>
    /// The path to the kubo repository folder.
    /// </summary>
    public string RepoPath { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var executableBinary = await CopyToTempFolder(_kuboBinary, cancellationToken);

        var processStartInfo = new ProcessStartInfo(executableBinary.Path, $"daemon --init --enable-pubsub-experiment --enable-namesys-pubsub --api /ip4/{_apiUri.Host}/tcp/{_apiUri.Port}");
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

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();

        var startupCompletion = new TaskCompletionSource<object?>();
        Process.OutputDataReceived += Process_OutputDataReceived;

        cancellationToken.ThrowIfCancellationRequested();

        await startupCompletion.Task;

        cancellationToken.ThrowIfCancellationRequested();

        Process.OutputDataReceived -= Process_OutputDataReceived;

        void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data?.Contains("Daemon is ready") ?? false)
                startupCompletion.SetResult(null);
        }
    }

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

        var copiedFile = await tempFolder.CreateCopyOfAsync(file, overwrite: true, cancellationToken);

        return (IAddressableFile)copiedFile;
    }
}