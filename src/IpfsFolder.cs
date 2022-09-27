﻿using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Storage;
using System.Runtime.CompilerServices;

namespace OwlCore.Kubo
{
    /// <summary>
    /// A folder that resides on IPFS.
    /// </summary>
    public class IpfsFolder : IFolder
    {
        private readonly IpfsClient _client;

        /// <summary>
        /// Creates a new instance of <see cref="IpfsFolder"/>.
        /// </summary>
        /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
        /// <param name="client">The IPFS Client to use for retrieving the content.</param>
        public IpfsFolder(Cid cid, IpfsClient client)
        {
            Id = cid;
            Name = cid;
            _client = client;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IpfsFolder"/>.
        /// </summary>
        /// <param name="cid">The CID of the folder, such as "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V".</param>
        /// <param name="name">The name of the folder.</param>
        /// <param name="client">The IPFS Client to use for retrieving the content.</param>
        public IpfsFolder(Cid cid, string name, IpfsClient client)
        {
            Id = cid;
            Name = !string.IsNullOrWhiteSpace(name) ? name : cid;
            _client = client;
        }

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var itemInfo = await _client.FileSystem.ListFileAsync(Id, cancellationToken);
            Guard.IsTrue(itemInfo.IsDirectory);

            foreach (var link in itemInfo.Links)
            {
                Guard.IsNotNullOrWhiteSpace(link.Id);
                var linkedItemInfo = await _client.FileSystem.ListFileAsync(link.Id, cancellationToken);

                if (linkedItemInfo.IsDirectory)
                {
                    if (type.HasFlag(StorableType.Folder))
                        yield return new AddressableIpfsFolder(linkedItemInfo.Id, link.Name, _client, new IFolder[] { this });
                }
                else
                {
                    if (type.HasFlag(StorableType.File))
                        yield return new AddressableIpfsFile(linkedItemInfo.Id, link.Name, _client, new IFolder[] { this });
                }
            }
        }
    }
}
