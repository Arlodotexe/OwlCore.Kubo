using Ipfs.Http;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using System.Diagnostics;

namespace OwlCore.Kubo.Tests;

[TestClass]
public class TestFixture
{
    /// <summary>
    /// A client that enables access to ipfs.
    /// </summary>
    public static IpfsClient Client => Bootstrapper?.Client ?? throw new InvalidOperationException("Bootstrapper not found, unable to set up client.");

    /// <summary>
    /// The bootstrapper that was used to create the <see cref="Client"/>.
    /// </summary>
    public static KuboBootstrapper? Bootstrapper { get; private set; }

    /// <summary>
    /// Sets up the test fixture.
    /// </summary>
    [AssemblyInitialize]
    public static async Task Setup(TestContext context)
    {
        OwlCore.Diagnostics.Logger.MessageReceived += (sender, args) => Debug.WriteLine(args.Message);

        Assert.IsNotNull(context.DeploymentDirectory);

        var workingFolder = await SafeCreateWorkingFolder(new SystemFolder(context.DeploymentDirectory), typeof(TestFixture).Namespace ?? throw new ArgumentNullException());

        Bootstrapper = await CreateNodeAsync(workingFolder, "node1", 5034, 8034);
    }

    /// <summary>
    /// Tears down the test fixture.
    /// </summary>
    [AssemblyCleanup]
    public static void Teardown()
    {
        Bootstrapper?.Dispose();
    }

    /// <summary>
    /// Creates a Kubo node with the provided <paramref name="apiPort"/> and <paramref name="gatewayPort"/>, downloading and bootstrapping as needed.
    /// </summary>
    /// <param name="workingDirectory">The directory where Kubo will be downloaded to and executed in.</param>
    /// <param name="nodeRepoName">A unique name for this node's ipfs repo.</param>
    /// <param name="apiPort">The port number to use for the Kubo RPC API.</param>
    /// <param name="gatewayPort">The port number to use for the locally hosted Ipfs Http Gateway.</param>
    /// <returns>An instance of <see cref="KuboBootstrapper"/> that has been started and is ready to use.</returns>
    public static async Task<KuboBootstrapper> CreateNodeAsync(SystemFolder workingDirectory, string nodeRepoName, int apiPort, int gatewayPort)
    {
        var nodeRepo = (SystemFolder)await workingDirectory.CreateFolderAsync(nodeRepoName, overwrite: true);

        var node = new KuboBootstrapper(nodeRepo.Path)
        {
            ApiUri = new Uri($"http://127.0.0.1:{apiPort}"),
            GatewayUri = new Uri($"http://127.0.0.1:{gatewayPort}"),
            RoutingMode = DhtRoutingMode.AutoClient,
            LaunchConflictMode = BootstrapLaunchConflictMode.Relaunch,
        };

        OwlCore.Diagnostics.Logger.LogInformation($"Starting node {nodeRepoName}\n");

        await node.StartAsync();

        Assert.IsNotNull(node.Process);
        return node;
    }

    /// <summary>
    /// Creates a temp folder for the test fixture to work in, safely unlocking and removing existing files if needed.
    /// </summary>
    /// <returns>The folder that was created.</returns>
    public static async Task<SystemFolder> SafeCreateWorkingFolder(SystemFolder rootFolder, string name)
    {
        // When Kubo is stopped unexpectedly, it may leave some files with a ReadOnly attribute.
        // Since this folder is created every time tests are run, we need to clean up any files leftover from prior runs.
        // To do that, we need to remove the ReadOnly file attribute.
        var testTempRoot = (SystemFolder)await rootFolder.CreateFolderAsync(name, overwrite: false);
        await SetAllFileAttributesRecursive(testTempRoot, attributes => attributes & ~FileAttributes.ReadOnly);

        // Delete and recreate the folder.
        return (SystemFolder)await rootFolder.CreateFolderAsync(name, overwrite: true);
    }

    /// <summary>
    /// Changes the file attributes of all files in all subfolders of the provided <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="rootFolder">The folder to set file permissions in.</param>
    /// <param name="transform">This function is provided the current file attributes, and should return the new file attributes.</param>
    public static async Task SetAllFileAttributesRecursive(SystemFolder rootFolder, Func<FileAttributes, FileAttributes> transform)
    {
        await foreach (SystemFile file in rootFolder.GetFilesAsync())
            file.Info.Attributes = transform(file.Info.Attributes);

        await foreach (SystemFolder folder in rootFolder.GetFoldersAsync())
            await SetAllFileAttributesRecursive(folder, transform);
    }
}