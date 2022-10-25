# AesPasswordEncryptedPubSub

Encrypts outgoing and decrypts incoming pubsub messages using AES encryption derived from a pre-shared passkey.

## Basic usage

```csharp
IPubSubApi publicPubSub = _ipfsClient.PubSub;
IPubSubApi encryptedPubSub = new AesPasswordEncryptedPubSub(publicPubSub, password: "testing", salt: null);

// Send / receive unencrypted:
await publicPubSub.PublishAsync(topic: "owlcore-kubo-sample-docs", message: "hello world!");

// Send / receive encrypted:
await encryptedPubSub.PublishAsync(topic: "owlcore-kubo-sample-docs", message: "hello world!");
```

