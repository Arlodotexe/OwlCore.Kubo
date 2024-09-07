namespace OwlCore.Kubo;

/// <summary>
/// Represents data about a Kubo distribution.
/// </summary>
public class KuboDistributionData
{
    /// <summary>
    /// A unique identifier for this distribution.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// The version for this distribution.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// The release link for this distribution.
    /// </summary>
    public string? ReleaseLink { get; set; }
    
    /// <summary>
    /// The name of this distribution.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The owner of this distribution.
    /// </summary>
    public string? Owner { get; set; }
    
    /// <summary>
    /// A description of this distribution.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The published date.
    /// </summary>
    public string? Date { get; set; }
    
    /// <summary>
    /// The platforms for this distribution.
    /// </summary>
    public Dictionary<string, KuboDistributionPlatform>? Platforms { get; set; }
}