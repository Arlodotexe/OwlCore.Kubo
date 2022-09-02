using System;
using System.Collections.Generic;

namespace OwlCore.Kubo;

public class KuboDistributionData
{
    public string? Id { get; set; }
    public string? Version { get; set; }
    public string? ReleaseLink { get; set; }
    public string? Name { get; set; }
    public string? Owner { get; set; }
    public string? Description { get; set; }
    public string? Date { get; set; }
    public Dictionary<string, KuboDistributionPlatform>? Platforms { get; set; }
}