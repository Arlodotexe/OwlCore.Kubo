using OwlCore.Storage;
using System.Diagnostics;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpnsFolderTests
    {
        [TestMethod]
        public async Task GetFilesAsync()
        {
            var folder = new IpnsFolder("/ipns/ipfs.tech", TestFixture.Client);
            var files = await folder.GetFilesAsync().ToListAsync();

            foreach (var item in files)
            {
                Debug.WriteLine($"{item.Name}, {item.Id}");
                Assert.IsTrue(item.Id.StartsWith(folder.Id));

                Assert.IsTrue(!RemoveFirstInstanceOfString(item.Id, folder.Id).Contains(folder.Id), "Only one instance of the root folder ID should exist in a child id.");
            }

            Assert.IsNotNull(files);
            Assert.IsTrue(files.Count > 2);
        }

        [TestMethod]
        public async Task GetFoldersAsync()
        {
            var folder = new IpnsFolder("/ipns/ipfs.tech", TestFixture.Client);
            var folders = await folder.GetFoldersAsync().ToListAsync();

            foreach (var item in folders)
                Debug.WriteLine($"{item.Name}, {item.Id}");

            Assert.IsNotNull(folders);
            Assert.IsTrue(folders.Count > 2);
        }

        public static string RemoveFirstInstanceOfString(string value, string removeString)
        {
            int index = value.IndexOf(removeString, StringComparison.Ordinal);
            return index < 0 ? value : value.Remove(index, removeString.Length);
        }
    }
}