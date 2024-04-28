using CommunityToolkit.Diagnostics;
using OwlCore.ComponentModel;
using System.Text.Json;

namespace OwlCore.Kubo.Cache;

/// <summary>
/// An <see cref="IAsyncSerializer{TSerialized}"/> and implementation for serializing and deserializing streams using System.Text.Json.
/// </summary>
public class KuboCacheSerializer : IAsyncSerializer<Stream>, ISerializer<Stream>
{
    /// <summary>
    /// A singleton instance for <see cref="KuboCacheSerializer"/>.
    /// </summary>
    public static KuboCacheSerializer Singleton { get; } = new();

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync<T>(T data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, typeof(T), context: KuboCacheSerializerContext.Default, cancellationToken: cancellationToken ?? CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync(Type inputType, object data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, inputType, context: KuboCacheSerializerContext.Default, cancellationToken: cancellationToken ?? CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public async Task<TResult> DeserializeAsync<TResult>(Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, typeof(TResult), KuboCacheSerializerContext.Default);
        Guard.IsNotNull(result);
        return (TResult)result;
    }

    /// <inheritdoc />
    public async Task<object> DeserializeAsync(Type returnType, Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, returnType, KuboCacheSerializerContext.Default);
        Guard.IsNotNull(result);
        return result;
    }

    /// <inheritdoc />
    public Stream Serialize<T>(T data)
    {
        var stream = new MemoryStream();
        JsonSerializer.SerializeAsync(stream, data, typeof(T), context: KuboCacheSerializerContext.Default, cancellationToken: CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public Stream Serialize(Type type, object data)
    {
        var stream = new MemoryStream();
        JsonSerializer.SerializeAsync(stream, data, type, context: KuboCacheSerializerContext.Default, cancellationToken: CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public TResult Deserialize<TResult>(Stream serialized)
    {
        var result = JsonSerializer.Deserialize(serialized, typeof(TResult), KuboCacheSerializerContext.Default);
        Guard.IsNotNull(result);
        return (TResult)result;
    }

    /// <inheritdoc />
    public object Deserialize(Type type, Stream serialized)
    {
        var result = JsonSerializer.Deserialize(serialized, type, KuboCacheSerializerContext.Default);
        Guard.IsNotNull(result);
        return result;
    }
}
