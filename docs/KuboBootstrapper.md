# KuboBootstrapper
A no-hassle bootstrapper for the Kubo binary.

On startup, the binary is copied to the users's temp directory, where it can be executed on any platform.

> **Warning** Don't use the bootstrapper unless you need to. If the user already has a running Kubo node, use that instead of spawning another one.

## Get a Kubo binary
Before you can use the bootstrapper, you need an `IFile` that points to the Kubo binary. 

If the user is online, [KuboDownloader](KuboDownloader.md) can automatically download and extract the correct Kubo binary for the running operating system and architecture from the IPFS shipyard.

## Basic usage

```cs
IFile kuboBinary = await GetKuboBinary();

// Create a new boostrapper
var bootstrapper = new KuboBootstrapper(kuboBinary, repoPath);

// Start the boostrapper. Once this task finishes, the API and Gateway will be ready for use.
await bootstrapper.StartAsync();

// Dispose of the bootstrapper to kill the process and clean up.
bootstrapper.Dispose();
```

## Custom API address
```cs
IFile kuboBinary = await GetKuboBinary();

// Create a new boostrapper
var bootstrapper = new KuboBootstrapper(kuboBinary, repoPath)
{
    ApiUri = new Uri("http://127.0.0.1:7700"),
};

// Start the boostrapper. Once this task finishes, the API and Gateway will be ready for use.
await bootstrapper.StartAsync();

// Dispose of the bootstrapper to kill the process and clean up.
bootstrapper.Dispose();
```

## Start the IpfsClient
Now that you have a running node, you can start using Kubo by passing the API url to a new `IpfsClient`.

```cs
// Start the binary
var bootstrapper = new KuboBootstrapper(kuboBinary, repoPath);
await bootstrapper.StartAsync();

// Create a client
var ipfsClient = new IpfsClient(bootstrapper.ApiUri.ToString());

// Start using it
var ipnsFile = new IpnsFile("/ipns/ipfs.tech/index.html", ipfsClient);
```
