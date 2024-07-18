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
[JsonSerializable(typeof(List<CachedKeyApi.CachedKeyInfo>))]
[JsonSerializable(typeof(List<CachedNameApi.PublishedCidName>))]
[JsonSerializable(typeof(List<CachedNameApi.PublishedPathName>))]
[JsonSerializable(typeof(List<CachedNameApi.ResolvedName>))]
public partial class KuboCacheSerializerContext : JsonSerializerContext
{
}