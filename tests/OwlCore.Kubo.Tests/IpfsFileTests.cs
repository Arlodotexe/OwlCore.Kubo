using Ipfs.Http;
using OwlCore.Storage;
using System.IO;
using System.Threading.Channels;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpfsFileTests
    {
        [TestMethod]
        public async Task BasicFileReadTest()
        {
            
            var file = new IpfsFile("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", TestFixture.Client);
            using var stream = await file.OpenStreamAsync();

            using StreamReader text = new StreamReader(stream);
            var txt = await text.ReadToEndAsync();

            Assert.AreEqual("hello world", txt);
        }
    }
}