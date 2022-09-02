using System.Collections.Generic;

namespace OwlCore.Kubo;

public class KuboDistributionPlatform
{
    public string? Name { get; set; }
    public Dictionary<string, KuboDistributionArchitecture>? Archs { get; set; }
}