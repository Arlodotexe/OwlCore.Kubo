using Ipfs.Http;
using OwlCore.Storage;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpnsFolderTests
    {
        [TestMethod]
        public async Task GetFilesAsync()
        {
            await KuboAccess.TryInitAsync();

            var folder = new IpnsFolder("/ipns/ipfs.tech", KuboAccess.Ipfs);
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

            var folder = new IpnsFolder("/ipns/ipfs.tech", KuboAccess.Ipfs);
            var folders = await folder.GetFoldersAsync().ToListAsync();
            
            foreach(var item in folders)
                Debug.WriteLine($"{item.Name}, {item.Id}");

            Assert.IsNotNull(folders);
            Assert.IsTrue(folders.Count > 2);
        }
    }
}