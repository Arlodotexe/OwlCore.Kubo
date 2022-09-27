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

            var messageCount = 0;
            
            var topic = "test";
            var cs = new CancellationTokenSource();
            try
            {
                await encryptedPubsub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);

                await encryptedPubsub.PublishAsync(topic, "hello world!");

                await Task.Delay(1000);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }
    }
}
