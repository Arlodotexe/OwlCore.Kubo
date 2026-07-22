using Ipfs.CoreApi;
using OwlCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Kubo;

/// <summary>
/// A read-only property for accessing the last modified time of an IPNS item.
/// </summary>
/// <remarks>
/// <para>
/// This property resolves the IPNS address to a CID, then retrieves the optional UnixFS 1.5 
/// <c>mtime</c> field from the underlying DAG node via <c>files/stat</c>.
/// Since IPNS points to immutable IPFS content, this property is read-only.
/// </para>
/// <para>
/// Most IPFS content does not have <c>mtime</c> set (it's opt-in per the UnixFS spec),
/// so <see cref="IStorageProperty{T}.GetValueAsync"/> will often return <c>null</c>.
/// </para>
/// </remarks>
public class IpnsLastModifiedAtProperty : SimpleStorageProperty<DateTime?>, ILastModifiedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="IpnsLastModifiedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The storage item that owns this property.</param>
    /// <param name="client">The IPFS client.</param>
    /// <param name="ipnsAddress">The IPNS address to resolve.</param>
    public IpnsLastModifiedAtProperty(IStorable owner, ICoreApi client, string ipnsAddress)
        : base(
            id: owner.Id + ":Mtime",
            name: nameof(ILastModifiedAt.LastModifiedAt),
            asyncGetter: async (ct) =>
            {
                // Resolve IPNS to get the current CID
                var resolvedIpns = await client.Name.ResolveAsync(ipnsAddress, recursive: true, cancel: ct);
                
                // Use files/stat on the resolved path to decode the UnixFS Data blob
                var stat = await client.Mfs.StatAsync(resolvedIpns, ct);
                
                if (stat.Mtime == null)
                    return null;

                var dt = DateTimeOffset.FromUnixTimeSeconds(stat.Mtime.Value);
                if (stat.MtimeNsecs.HasValue)
                    dt = dt.AddTicks(stat.MtimeNsecs.Value / 100);

                return dt.UtcDateTime;
            })
    {
    }
}

