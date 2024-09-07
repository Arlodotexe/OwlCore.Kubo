namespace OwlCore.Kubo;

/// <summary>
/// Represents an architecture for a Kubo distribution.
/// </summary>
public class KuboDistributionArchitecture
{
    /// <summary>
    /// A relative download link for this architecture binary.
    /// </summary>
    public string? Link { get; set; }
    
    /// <summary>
    /// The Cid for this architecture binary.
    /// </summary>
    public string? Cid { get; set; }
    
    /// <summary>
    /// A hash that can be used to verify the downloaded data.
    /// </summary>
    public string? Sha512 { get; set; }
}