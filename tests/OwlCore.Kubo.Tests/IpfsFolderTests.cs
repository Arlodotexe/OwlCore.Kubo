using OwlCore.Storage;
using System.Diagnostics;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpfsFolderTests
    {
        [TestMethod]
        public async Task GetFilesAsync()
        {
            await KuboAccess.TryInitAsync();

            var folder = new IpfsFolder("QmSnuWmxptJZdLJpKRarxBMS2Ju2oANVrgbr2xWbie9b2D", KuboAccess.Ipfs);
            var files = await folder.GetFilesAsync().ToListAsync();
            
            foreach(var item in files)
                Debug.WriteLine($"{item.Name}, {item.Id}");

            Assert.IsNotNull(files);
            Assert.IsTrue(files.Count > 2);
        }

        [TestMethod]
        public async Task GetFoldersAsync()
        {
            await KuboAccess.TryInitAsync();

            var folder = new IpfsFolder("QmSnuWmxptJZdLJpKRarxBMS2Ju2oANVrgbr2xWbie9b2D", KuboAccess.Ipfs);
            var folders = await folder.GetFoldersAsync().ToListAsync();
            
            foreach(var item in folders)
                Debug.WriteLine($"{item.Name}, {item.Id}");

            Assert.IsNotNull(folders);
            Assert.AreEqual(2, folders.Count);
        }
    }
}