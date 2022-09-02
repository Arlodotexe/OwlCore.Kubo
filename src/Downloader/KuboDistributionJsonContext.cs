using System.Text.Json.Serialization;

namespace OwlCore.Kubo;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(KuboDistributionData))]
public partial class KuboDistributionJsonContext : JsonSerializerContext
{
}