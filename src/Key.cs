using Ipfs;
using Newtonsoft.Json;

namespace OwlCore.Kubo;

/// <summary>
/// This record is a simple implementation of the <see cref="IKey"/> interface, representing a cryptographic key with an identifier and a name.
/// </summary>
/// <remarks>
/// This is provided primarily for serialization purposes, as the <see cref="IKey"/> interface is an inbox interface and cannot be serialized directly.
/// It is used to create a concrete type that can be serialized and deserialized while still adhering to the <see cref="IKey"/> contract.
/// </remarks>
public record Key : IKey
{
    /// <summary>
    /// Creates a new instance of the <see cref="Key"/> class with the specified id and name.
    /// </summary>
    /// <param name="id">The Cid of the key.</param>
    /// <param name="name">The name of the key.</param>
    [JsonConstructor]
    public Key(Cid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Key"/> class from an existing key.
    /// </summary>
    /// <param name="key">The key to copy the name and id from.</param>
    public Key(IKey key)
    {
        Id = key.Id;
        Name = key.Name;
    }

    /// <inheritdoc />
    public Cid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }
}