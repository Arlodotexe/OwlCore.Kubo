using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

namespace OwlCore.Kubo;

/// <summary>
/// Extends <see cref="IStorable"/> with additional Kubo features.
/// </summary>
public static partial class StorableKuboExtensions
{
    /// <inheritdoc cref="IGetCid.GetCidAsync(CancellationToken)"/>
    public static async Task<Cid> GetCidAsync(this IStorable item, IpfsClient client, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Wrap with known supported IGetCid implementations.
        // Non-inbox wrappers that implement IGetCid should be applied before being passed to this method.
        // ---
        // Consumers can apply their own implementation wrappers for IGetCid before calling this method.
        // You'd typically only do this if you can interact your Kubo node faster than a manually piped stream,
        // or if your implementation of IGetCid has enhanced capabilities (e.g. folder support).
        // ---
        if (item is not IGetCid && item is SystemFile systemFile)
            item = new ContentAddressedSystemFile(systemFile.Path, client);

        if (item is not IGetCid && item is SystemFolder systemFolder)
            item = new ContentAddressedSystemFolder(systemFolder.Path, client);

        // If the implementation can handle content addressing directly, use that.
        if (item is IGetCid contentAddressedStorable)
            return await contentAddressedStorable.GetCidAsync(cancellationToken);

        // Otherwise, a fallback approach that manually connects the streams together.
        // The Kubo API doesn't support this scenario for folders, without assuming that the Id is a local path,
        // a scenario that's already handled where supported via the IGetCid interface.
        if (item is IFile file)
        {
            using var stream = await file.OpenStreamAsync(FileAccess.Read, cancellationToken);

            var res = await client.FileSystem.AddAsync(stream, file.Name, new()
            {
                OnlyHash = true,
                Pin = false
            }, cancellationToken);

            Guard.IsFalse(res.IsDirectory);
            return res.ToLink().Id;
        }

        throw new NotSupportedException($"Provided storable item is not a supported type. Provide an {nameof(IFile)} or an implementation of {nameof(IGetCid)}");
    }
}
