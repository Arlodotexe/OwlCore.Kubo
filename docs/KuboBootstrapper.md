# KuboBootstrapper
A no-hassle bootstrapper for the Kubo binary.

When started, the binary retrieved and copied to the BinaryWorkingFolder where it can be executed on any platform. By default, this is the system temp folder.

> **Warning** If the user has their own local Kubo node, connect to that instead of bootstrapping a new one.

## Getting a Kubo binary
The [KuboDownloader](KuboDownloader.md) automatically downloads and extracts the correct Kubo binary for the running operating system and architecture.

## Basic usage

To fully customize the client and version used by KuboDownloader, see the [KuboDownloader docs](./KuboDownloader.md).

If the binary isn't in the BinaryWorkingFolder, it will be retrieved using the file delegate provided in the constructor. If no delegate is provided, the latest version will be retrieved via Http.

#### Get latest version
```cs
var httpClient = new HttpClient();
var ipfsClient = new IpfsClient { ApiUri = new Uri(...) };

// Setup bootstrapper. 
// Latest binary, downloaded via default HttpClient.
using var bootstrapper = new KuboBootstrapper(repoPath);

// Latest binary, downloaded via custom HttpClient.
using var bootstrapper = new KuboBootstrapper(repoPath, cancel => KuboDownloader.GetLatestBinaryAsync(httpClient, cancel));

// Latest binary, downloaded via custom IpfsClient.
using var bootstrapper = new KuboBootstrapper(repoPath, cancel => KuboDownloader.GetLatestBinaryAsync(ipfsClient, cancel));

// Start the boostrapper. Once this task finishes, the API and Gateway will be ready for use.
await bootstrapper.StartAsync();
```

#### Get specific version
```cs
var httpClient = new HttpClient();
var ipfsClient = new IpfsClient { ApiUri = new Uri(...) };

// Setup bootstrapper.
// v0.15.0 binary, downloaded via default HttpClient
using var bootstrapper = new KuboBootstrapper(repoPath, cancel => KuboDownloader.GetBinaryVersionAsync(Version.Parse("0.15.0"), cancel));

// v0.15.0 binary, downloaded via default HttpClient
using var bootstrapper = new KuboBootstrapper(repoPath, Version.Parse("0.15.0"));

// v0.15.0 binary, downloaded via custom HttpClient
using var bootstrapper = new KuboBootstrapper(repoPath, cancel => KuboDownloader.GetBinaryVersionAsync(httpClient, new Version(0, 15, 0), cancel));

// v0.15.0 binary, downloaded via custom IpfsClient
using var bootstrapper = new KuboBootstrapper(repoPath, cancel => KuboDownloader.GetBinaryVersionAsync(ipfsClient, new Version(0, 15, 0), cancel));
```

## Additional options
To make it easy to give users control over their node, we've exposed several options for the bootstrapper. 
```cs
IFile kuboBinary = await GetKuboBinary();

// Create a new bootstrapper. Remember to dispose when finished.
using var bootstrapper = new KuboBootstrapper(repoPath)
{
    ApiUri = new Uri("http://127.0.0.1:7700"),
    GatewayUri = new Uri("http://127.0.0.1:8081"),
    RoutingMode = DhtRoutingMode.DhtClient,
    StartupProfiles = new() 
    {
        "lowpower"
    },
};

// Start the boostrapper. Once this task finishes, the API and Gateway will be ready for use.
await bootstrapper.StartAsync();
```

## Start the IpfsClient
Now that you have a running node, you can start using Kubo by passing the API url to a new `IpfsClient`.

```cs
var bootstrapper = new KuboBootstrapper(kuboBinary, repoPath);

// Start the binary
await bootstrapper.StartAsync();

// Create a client
var ipfsClient = new IpfsClient { ApiUri = bootstrapper.ApiUri }

// Start using it
var ipnsFile = new IpnsFile("/ipns/ipfs.tech/index.html", ipfsClient);
```
