namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class AesPasswordEncryptedPubSubTests
    {
        [TestMethod]
        public async Task PublishAsync()
        {
            await KuboAccess.TryInitAsync();

            var encryptedPubsub = new AesPasswordEncryptedPubSub(KuboAccess.Ipfs.PubSub, password: "testing");
            
            await encryptedPubsub.PublishAsync("owlcore-kubo-test-runner", "hello world!");
        }
    }
}
