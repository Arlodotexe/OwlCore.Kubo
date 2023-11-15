# KuboDownloader

> **Warning**
> KuboDownloader pulls from the IPFS Shipyard over HTTP, and will not work when the user is offline.

Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture

## Supported environments

### Platforms:

Works on Windows, Linux, MacOS and FreeBSD (with .NET 5+)

### Architectures:

Works on x64, x86, ARM and ARM64

## Basic usage
#### Get latest version
```cs
var httpClient = new HttpClient();
var ipfsClient = new IpfsClient { ApiUri = new Uri(...) };

// Latest binary, downloaded via default HttpClient
IFile latestKuboBinary = await KuboDownloader.GetLatestBinaryAsync();

// Latest binary, downloaded via custom HttpClient
IFile latestKuboBinary = await KuboDownloader.GetLatestBinaryAsync(httpClient);

// Latest binary, downloaded via custom IpfsClient
IFile latestKuboBinary = await KuboDownloader.GetLatestBinaryAsync(ipfsClient);
```

#### Get specific version
```cs
var httpClient = new HttpClient();
var ipfsClient = new IpfsClient { ApiUri = new Uri(...) };

// v0.15.0 binary, downloaded via default HttpClient
IFile kuboBinary = await KuboDownloader.GetBinaryVersionAsync(Version.Parse("0.15.0"));

// v0.15.0 binary, downloaded via custom HttpClient
IFile kuboBinary = await KuboDownloader.GetBinaryVersionAsync(httpClient, new Version(0, 15, 0));

// v0.15.0 binary, downloaded via custom IpfsClient
IFile kuboBinary = await KuboDownloader.GetBinaryVersionAsync(ipfsClient, new Version(0, 15, 0));
```

## Using the binary

The `IFile` you're given is created by crawling the contents of the compressed archive directly from Http or Ipfs, and is not saved on disk. The stream supports reading and seeking, with lazy-loading of the underlying stream.

You can treat this `IFile` like any other file in OwlCore.Storage, whether copying it to another location (local, cloud, etc) or providing it to other tooling.

#### Bootstrap Kubo

This helper can be combined with the `KuboBootstrapper` to automatically download the binary, only when needed. It requires no additional copying or setup.

To learn how to use the bootstrapper, [see the docs](./KuboBootstrapper.md).

#### Save or copy the binary file.

The returned object is an `IFile`, which can be saved or copied as needed.

```cs
// The returned file can be copied to any modifiable folder, if needed.
IModifiableFolder destination = new SystemFolder("/some/path");
var copiedFile = await destination.CreateCopyOfAsync(kuboBinary);
```

#### Retrieve the copied binary after restart
The Id of a file or folder is contractually guaranteed to be unique within that filesystem (while Name may not be).

Given this, simply save the `copiedFile.Id` string. After a restart, provide it to `GetItemAsync()` or `GetItemRecursiveAsync` on the destination folder to retrieve it again.
```cs
var kuboBinary = await destination.GetItemAsync(copiedFileId);
```
