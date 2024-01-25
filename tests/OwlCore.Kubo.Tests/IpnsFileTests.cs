namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpnsFileTests
    {
        [TestMethod]
        public async Task BasicFileReadTest()
        {
            var file = new IpnsFile("/ipns/ipfs.tech/index.html", TestFixture.Client);
            using var stream = await file.OpenStreamAsync();

            using StreamReader text = new StreamReader(stream);
            var txt = await text.ReadToEndAsync();

            Assert.IsTrue(txt.StartsWith("<!DOCTYPE html>"));
        }
    }
}