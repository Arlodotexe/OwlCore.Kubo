# KuboDownloader

> **Warning**
> KuboDownloader pulls from the IPFS Shipyard over HTTP, and will not work when the user is offline.

Automatically downloads and extracts the correct Kubo binary for the running operating system and architecture

## Supported environments

### Platforms:

Works on Windows, Linux, MacOS and FreeBSD (with .NET 5+)

### Architectures:

Works on x64, x86, ARM and ARM64

## Download latest version
```cs
var downloader = new KuboDownloader();

IFile latestKuboBinary = await downloader.DownloadLatestBinaryAsync();
```
 
## Download a specific version
```cs
var downloader = new KuboDownloader();

IFile kuboBinary = await downloader.DownloadBinaryAsync(Version.Parse("0.15.0"));
```

## Save the downloaded binary
Once you have the binary downloaded, save it somewhere so you don't need to redownload every time.

```cs
// Copy the binary to a known location.
// Any modifiable folder can be used, even if it's not on local disk!
IModifiableFolder destination = new SystemFolder("/some/path");

var copiedFile = await destination.CreateCopyOfAsync(kuboBinary);
```

## Retrieve the saved binary
The recommended approach is to save the file ID, and provide it to `GetItemAsync()` in the folder that you copied the binary to:

```cs
var kuboBinary = await destination.GetItemAsync(copiedFileId);
```
This will work for all storage implementations, with maximum performance when `OwlCore.Storage.IFolderCanFastGetItem` is supported by the storage implementor.


## Use the binary

To start the downloaded binary in your app, see [KuboBootstrapper](KuboBootstrapper.md).
