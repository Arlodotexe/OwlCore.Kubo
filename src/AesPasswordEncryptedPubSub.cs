using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Extensions;
using System.Security.Cryptography;
using System.Text;
using PublishedMessage = OwlCore.Kubo.Models.PublishedMessage;

namespace OwlCore.Kubo;

/// <summary>
/// Encrypts outgoing and decrypts incoming pubsub messages using AES encryption derived from a pre-shared passkey.
/// </summary>
public class AesPasswordEncryptedPubSub : IPubSubApi, IDelegable<IPubSubApi>
{
    private readonly string _password;
    private readonly string? _salt;

    /// <summary>
    /// Creates a new instance of <see cref="AesPasswordEncryptedPubSub"/>.
    /// </summary>
    /// <param name="inner">An existing, functional <see cref="IPubSubApi"/> instance to use for sending and receiving encrypted messages.</param>
    /// <param name="password">A pre-shared passkey used for encryption and decryption.</param>
    /// <param name="salt">An optional password salt.</param>
    public AesPasswordEncryptedPubSub(IPubSubApi inner, string password, string? salt = null)
    {
        Inner = inner;
        _password = password;
        _salt = salt;
    }

    /// <inheritdoc />
    public IPubSubApi Inner { get; }

    /// <inheritdoc />
    public Task<IEnumerable<Peer>> PeersAsync(string? topic = null, CancellationToken cancel = default) => Inner.PeersAsync(topic, cancel);

    /// <inheritdoc />
    public Task PublishAsync(string topic, string message, CancellationToken cancel = default) => PublishAsync(topic, Encoding.UTF8.GetBytes(message), cancel);

    /// <inheritdoc />
    public Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default) => PublishAsync(topic, new MemoryStream(message), cancel);

    /// <inheritdoc />
    public async Task PublishAsync(string topic, Stream message, CancellationToken cancel = default)
    {
        if (message.CanSeek)
            message.Seek(0, SeekOrigin.Begin);

        var aes = Aes.Create();
        var passBytes = new Rfc2898DeriveBytes(password: _password, salt: Encoding.UTF8.GetBytes(_salt ?? string.Empty));

        aes.Key = passBytes.GetBytes(aes.KeySize / 8);
        aes.IV = passBytes.GetBytes(aes.BlockSize / 8);

        using var encryptedOutputStream = new MemoryStream();
        using var streamEncryptor = new CryptoStream(encryptedOutputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        var unencryptedBytes = await message.ToBytesAsync();
        streamEncryptor.Write(unencryptedBytes, 0, unencryptedBytes.Length);
        streamEncryptor.FlushFinalBlock();

        encryptedOutputStream.Position = 0;

        await Inner.PublishAsync(topic, encryptedOutputStream, cancel);
    }

    /// <inheritdoc />
    public Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
    {
        return Inner.SubscribeAsync(topic, msg =>
        {
            if (TryTransformPublishedMessage(msg) is IPublishedMessage transformedMsg)
            {
                handler(transformedMsg);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default)
    {
        return Inner.SubscribedTopicsAsync(cancel);
    }

    internal IPublishedMessage? TryTransformPublishedMessage(IPublishedMessage publishedMessage)
    {
        var aes = Aes.Create();
        var passBytes = new Rfc2898DeriveBytes(password: _password, salt: Encoding.UTF8.GetBytes(_salt ?? string.Empty));

        aes.Key = passBytes.GetBytes(aes.KeySize / 8);
        aes.IV = passBytes.GetBytes(aes.BlockSize / 8);

        try
        {
            using var outputStream = new MemoryStream();
            using var streamDecryptor = new CryptoStream(publishedMessage.DataStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            streamDecryptor.CopyTo(outputStream);

            var outputBytes = outputStream.ToBytes();

            return new PublishedMessage(publishedMessage.Sender, publishedMessage.Topics, publishedMessage.SequenceNumber, outputBytes, outputStream, publishedMessage.Size);
        }
        catch
        {
            // If the message can't be decrypted, swallow and ignore it. Unencrypted messages can be read via the normal, unencrypted pubsub API.
            return null;
        }
    }
}
