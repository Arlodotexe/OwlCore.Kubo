using Ipfs.CoreApi;
using OwlCore.Storage;
using System;
using System.Threading.Tasks;

namespace OwlCore.Kubo;

/// <summary>
/// A property for accessing and modifying the last modified time of an MFS item.
/// </summary>
public class MfsLastModifiedAtProperty : SimpleModifiableStorageProperty<DateTime?>, IModifiableLastModifiedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="MfsLastModifiedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The owner of the property.</param>
    /// <param name="client">The IPFS client.</param>
    /// <param name="path">The MFS path.</param>
    public MfsLastModifiedAtProperty(IStorable owner, ICoreApi client, string path) : base(
        id: owner.Id + ":Mtime",
        name: nameof(ILastModifiedAt.LastModifiedAt),
        asyncGetter: async (ct) =>
        {
            var stat = await client.Mfs.StatAsync(path, ct);
            if (stat.Mtime == null)
                return null;

            var dt = DateTimeOffset.FromUnixTimeSeconds(stat.Mtime.Value);
            
            if (stat.MtimeNsecs.HasValue)
                dt = dt.AddTicks(stat.MtimeNsecs.Value / 100);

            return dt.UtcDateTime;
        },
        asyncSetter: async (val, ct) =>
        {
            if (val == null) throw new ArgumentNullException(nameof(val), "Cannot set last modified time to null.");
            await client.Mfs.TouchAsync(path, val, ct);
        }
    )
    {
    }
}
