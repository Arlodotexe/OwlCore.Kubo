using System.Text.Json.Serialization;

namespace OwlCore.Kubo.Cache;

/// <summary>
/// Supplies type information for settings values in <see cref="CachedCoreApi"/>.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<string>))]
public partial class KuboCacheSerializerContext : JsonSerializerContext
{
}