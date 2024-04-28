namespace OwlCore.Kubo;

/// <summary>
/// Different ways of handling config settings.
/// </summary>
public enum ConfigMode
{
    /// <summary>
    /// If the config value is already set, it will be used instead.
    /// </summary>
    UseExisting,

    /// <summary>
    /// If the config value is already set, it will be overwritten.
    /// </summary>
    OverwriteExisting,
}