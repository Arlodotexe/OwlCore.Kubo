namespace OwlCore.Kubo;

/// <summary>
/// Represents a distribution platform for Kubo.
/// </summary>
public class KuboDistributionPlatform
{
    /// <summary>
    /// The name of the distribution platform.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The architectures available for this distribution.
    /// </summary>
    public Dictionary<string, KuboDistributionArchitecture>? Archs { get; set; }
}