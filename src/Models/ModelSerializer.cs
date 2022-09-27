using System.Text.Json.Serialization;

namespace OwlCore.Kubo.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(MfsFileData))]
    [JsonSerializable(typeof(MfsFileContentsBody))]
    [JsonSerializable(typeof(MfsFileStatData))]
    [JsonSerializable(typeof(FilesFlushResponse))]
    internal partial class ModelSerializer : JsonSerializerContext
    {
    }
}
