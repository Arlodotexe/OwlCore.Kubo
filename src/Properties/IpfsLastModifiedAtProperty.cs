using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Kubo;

/// <summary>
/// A read-only property for accessing the last modified time of an IPFS item.
/// </summary>
/// <remarks>
/// <para>
/// This property retrieves the optional UnixFS 1.5 <c>mtime</c> field from the DAG node
/// via <c>files/stat</c>. Since IPFS content is immutable, this property is read-only.
/// </para>
/// <para>
/// Most IPFS content does not have <c>mtime</c> set (it's opt-in per the UnixFS spec),
/// so <see cref="IStorageProperty{T}.GetValueAsync"/> will often return <c>null</c>.
/// </para>
/// </remarks>
public class IpfsLastModifiedAtProperty : SimpleStorageProperty<DateTime?>, ILastModifiedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="IpfsLastModifiedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The storage item that owns this property.</param>
    /// <param name="client">The IPFS client.</param>
    /// <param name="cid">The CID of the IPFS content.</param>
    public IpfsLastModifiedAtProperty(IStorable owner, ICoreApi client, Cid cid)
        : base(
            id: owner.Id + ":Mtime",
            name: nameof(ILastModifiedAt.LastModifiedAt),
            asyncGetter: async (ct) =>
            {
                // Use files/stat on the /ipfs/ path to decode the UnixFS Data blob
                var stat = await client.Mfs.StatAsync($"/ipfs/{cid}", ct);
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

