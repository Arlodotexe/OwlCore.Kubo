using Ipfs.Http;
using OwlCore.Storage;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpfsFolderTests
    {
        [TestMethod]
        public async Task BasicFolderTraversal()
        {
            await KuboAccess.TryInitAsync();

            var folder = new IpfsFolder("QmSnuWmxptJZdLJpKRarxBMS2Ju2oANVrgbr2xWbie9b2D", KuboAccess.Ipfs);
            var items = await folder.GetItemsAsync().ToListAsync();
            
            foreach(var item in items)
                Debug.WriteLine($"{item.Name}, {item.Id}");

            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count > 2);
        }
    }
}