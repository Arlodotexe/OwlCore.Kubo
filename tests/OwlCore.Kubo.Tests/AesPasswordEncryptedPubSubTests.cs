﻿namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class AesPasswordEncryptedPubSubTests
    {
        [TestMethod]
        public async Task PublishAsync()
        {
            await KuboAccess.TryInitAsync();

            var encryptedPubsub = new AesPasswordEncryptedPubSub(KuboAccess.Ipfs.PubSub, password: "testing", salt: null);
            
            await encryptedPubsub.PublishAsync(topic: "owlcore-kubo-test-runner", message: "hello world!");
        }
    }
}
