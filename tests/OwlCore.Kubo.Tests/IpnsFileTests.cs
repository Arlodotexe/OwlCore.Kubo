namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class IpnsFileTests
    {
        [TestMethod]
        public async Task BasicFileReadTest()
        {
            await KuboAccess.TryInitAsync();

            var file = new IpnsFile("/ipns/ipfs.tech/index.html", KuboAccess.Ipfs);
            using var stream = await file.OpenStreamAsync();

            using StreamReader text = new StreamReader(stream);
            var txt = await text.ReadToEndAsync();

            Assert.IsTrue(txt.StartsWith("<!DOCTYPE html>"));
        }
    }
}