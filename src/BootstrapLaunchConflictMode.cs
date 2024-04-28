namespace OwlCore.Kubo;

/// <summary>
/// The behavior when the repo is locked and a node is already running on the requested port.
/// </summary>
public enum BootstrapLaunchConflictMode
{
    /// <summary>
    /// Throw when the requested port is already in use.
    /// </summary>
    Throw,

    /// <summary>
    /// If the repo is locked, load the Api and Gateway port, attach to it and shut it down, then bootstrap a new process on the requested ports.
    /// </summary>
    Relaunch,

    /// <summary>
    /// If the repo is locked, load the configured (currently running) Api and Gateway port and attach to it without shutting it down or starting a new process. May discard the requested ports.
    /// </summary>
    /// <remarks>Note that attaching does not allow you to take control of the running process. See <see cref="Relaunch"/> instead if you need this functionality.</remarks>
    Attach,
}