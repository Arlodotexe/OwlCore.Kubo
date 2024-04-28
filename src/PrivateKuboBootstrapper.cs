using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.Storage;

namespace OwlCore.Kubo;

/// <summary>
/// An easy-to-use bootstrapper for private Kubo swarms.
/// </summary>
public class PrivateKuboBootstrapper : KuboBootstrapper
{
    /// <summary>
    /// Creates a new instance of <see cref="PrivateKuboBootstrapper"/>.
    /// </summary>
    public PrivateKuboBootstrapper(string repoPath, Func<CancellationToken, Task<IFile>> getKuboBinaryFile)
        : base(repoPath, getKuboBinaryFile)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="PrivateKuboBootstrapper"/>.
    /// </summary>
    public PrivateKuboBootstrapper(string repoPath, Version kuboVersion)
        : base(repoPath, kuboVersion)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="PrivateKuboBootstrapper"/>.
    /// </summary>
    public PrivateKuboBootstrapper(string repoPath)
        : base(repoPath)
    {
    }

    /// <summary>
    /// Gets or sets a bool indicating whether Kubo should force the use of private networks, and fail if no swarm key is found. Default is true.
    /// </summary>
    public bool Libp2pForcePnet { get; set; } = true;

    /// <summary>
    /// The list of peers that will be bootstrapped instead of the default ones. These are the *trusted peers* from which to learn about other peers in the network.
    /// </summary>
    public required IEnumerable<MultiAddress> BootstrapPeerMultiAddresses { get; set; }

    /// <summary>
    /// The behavior to use if the swarm key already exists in the repo.
    /// </summary>
    public required ConfigMode SwarmKeyConfigMode { get; set; }

    /// <summary>
    /// The swarm key to use.
    /// </summary>
    /// <remarks>
    /// Generate a swarm key file via <see cref="SwarmKeyGen.CreateAsync(CancellationToken)"/>.
    /// </remarks>
    public required IFile SwarmKeyFile { get; set; }

    /// <inheritdoc/>
    protected override async Task ApplyRoutingSettingsAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(KuboBinaryFile);

        // Copy swarm key to repo
        await RepoFolder.CreateCopyOfAsync(SwarmKeyFile, overwrite: SwarmKeyConfigMode == ConfigMode.OverwriteExisting, cancellationToken);

        if (Libp2pForcePnet)
            EnvironmentVariables["LIBP2P_FORCE_PNET"] = "1";

        // Clear out all existing bootstrap peers
        RunExecutable(KuboBinaryFile, $"bootstrap rm --all --repo-dir \"{RepoFolder.Path}\"", throwOnError: false);

        // Setup bootstrap peers
        foreach (var multiAddress in BootstrapPeerMultiAddresses)
        {
            RunExecutable(KuboBinaryFile, $"bootstrap add {multiAddress} --repo-dir \"{RepoFolder.Path}\"", throwOnError: false);
        }

        // Always call base at the end for these overridden settings methods.
        await base.ApplyRoutingSettingsAsync(cancellationToken);
    }
}