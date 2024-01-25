namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class AesPasswordEncryptedPubSubTests
    {
        [TestMethod]
        public async Task PublishAsync()
        {
            var encryptedPubsub = new AesPasswordEncryptedPubSub(TestFixture.Client.PubSub, password: "testing", salt: null);
            
            await encryptedPubsub.PublishAsync(topic: "owlcore-kubo-test-runner", message: "hello world!");
        }
    }
}
