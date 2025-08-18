using OwlCore.Storage;
using System.Diagnostics;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpnsFolderTests
    {
        [TestMethod]
        public void PathHelpers_GetParentPath_IpnsRoot()
        {
            var parent = PathHelpers.GetParentPath("/ipns/ipfs.tech");
            Assert.AreEqual("/ipns", parent);
        }

        [TestMethod]
        public async Task GetParent_FromItemInRoot_ReturnsRootThenNull()
        {
            // Arrange: an IPNS root folder (no trailing child path)
            var start = new IpnsFolder("/ipns/ipfs.tech/api/", TestFixture.Client);

            // Act
            var root = (IChildFolder?)await start.GetParentAsync();
            
            // Assert: parent of "/ipns/<name>/<subfolder>/" should be "/ipns/<name>"
            Assert.IsNotNull(root);

            // Act
            var shouldBeNull = await root.GetParentAsync();

            // Assert: parent of "/ipns/<name>" should be null (no "/ipns" folder)
            Assert.IsNull(shouldBeNull);
        }

        [TestMethod]
        public async Task GetParent_OnIpnsRoot_ReturnsNull()
        {
            // Arrange: an IPNS root folder (no trailing child path)
            var root = new IpnsFolder("/ipns/ipfs.tech/", TestFixture.Client);

            // Act
            var parent = await root.GetParentAsync();

            // Assert: parent of "/ipns/<name>" should be null (no "/ipns" folder)
            Assert.IsNull(parent);
        }

        [TestMethod]
        public async Task GetFilesAsync()
        {
            var folder = new IpnsFolder("/ipns/ipfs.tech/", TestFixture.Client);
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