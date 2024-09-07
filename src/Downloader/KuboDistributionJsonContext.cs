using System.Text.Json.Serialization;

namespace OwlCore.Kubo;

/// <summary>
/// Json context for serializing and deserializing <see cref="KuboDistributionData"/>.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(KuboDistributionData))]
public partial class KuboDistributionJsonContext : JsonSerializerContext
{
}