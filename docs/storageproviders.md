# Storage providers
OwlCore.Kubo provides [OwlCore.Storage](https://github.com/Arlodotexe/OwlCore.Storage) implementations for IPFS, IPNS and MFS.

## Prerequisites

All storage implementations require an instance of `IpfsClient` in the constructor.

## Table of contents
- [IPFS](#ipfs)
- [IPNS](#ipns)
- [Mutable FileSystem](#mutable-filesystem-mfs) (MFS)

## IPFS
A standard implementation of `IFile` and `IFolder` that gets content from IPFS using the supplied CID.

### `IpfsFile`
```cs
IFile file = new IpfsFile("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", ipfsClient);

// Open file stream.
using var stream = await file.OpenStreamAsync();
```

### `IpfsFolder`
```cs
IFolder folder = new IpfsFolder("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", ipfsClient);

// Get just files
await foreach (var file in folder.GetFilesAsync())
{
    DoSomething(file);
}

// Get just folders
await foreach (var subFolder in folder.GetFoldersAsync())
{
    DoSomething(subFolder);
}

// Get both files and folders
await foreach (var item in folder.GetItemsAsync())
{
    if (item is IFile file)
    {
        DoSomething(file);
    }

    if (item is IFolder subFolder)
    {
        DoSomething(subFolder);
    }
}
```
## IPNS
A standard implementation of `IFile` and `IFolder` that gets content from IPFS by resolving the supplied IPNS address.

### `IpnsFile`

```cs
IFile file = new IpnsFile("/ipns/ipfs.tech/index.html", ipfsClient);

// Open file stream.
using var stream = await file.OpenStreamAsync();
```

### `IpnsFolder`

```cs
IFolder folder = new IpnsFolder("/ipns/ipfs.tech", ipfsClient);

// Get just files
await foreach (var file in folder.GetFilesAsync())
{
    DoSomething(file);
}

// Get just folders
await foreach (var subFolder in folder.GetFoldersAsync())
{
    DoSomething(subFolder);
}

// Get both files and folders
await foreach (var item in folder.GetItemsAsync())
{
    if (item is IFile file)
    {
        DoSomething(file);
    }

    if (item is IFolder subFolder)
    {
        DoSomething(subFolder);
    }
}
```

## Mutable Filesystem (MFS)
Gets a file or folder from an MFS path. These are fully modifiable, and can replace local storage.

### `MfsStream`
This can be treated as a standard `System.IO.Stream`, for maximum compatability.

```cs
using var stream = new MfsStream("/myApp/myDatabase.bak", ipfsClient);

// Since we're using MFS, we can write to the stream.
await stream.WriteAsync(GenerateRandomData(256), 0, 256);
```

> **Note** Use the async methods wherever possible. The synchronous methods are long-running and may block the current thread.

### `MfsFile`

```cs
var file = new MfsFile("/myApp/myDatabase.bak", ipfsClient);

// Open file stream. This uses an MfsStream.
using var stream = await file.OpenStreamAsync();

// Since we're using MFS, we can write to the stream.
await stream.WriteAsync(GenerateRandomData(256), 0, 256);

// Flush data into IPFS and get the CID of the file content.
var cid = file.FlushAsync();
```

### `MfsFolder`

#### Basic usage

```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

// See IpfsFolder or IpnsFolder for enumeration examples. 
```

#### Create new files & folders
```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

var newFolder = folder.CreateFolderAsync("newFolder");
var newFile = folder.CreateFileAsync("newFile");
```

#### Copy or move items into MFS
```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

// Use any IFile. Can be on local disk, in cloud storage, or even on IPFS.
var file = new SystemFile("/path/to/file.jpg");
var copiedFile = folder.CreateCopyOfAsync(file);

var file = new IpnsFile("/ipns/ipfs.tech/index.html", ipfsClient);
var movedFile = folder.MoveFromAsync(file);
```

####  Delete items
```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

await folder.DeleteAsync(copiedFile);
await folder.DeleteAsync(newFolder);
```

#### Watch a folder for changes
```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

using var folderWatcher = await folder.GetFolderWatcherAsync();

folderWatcher.CollectionChanged += OnFolderContentsChanged;

void OnFolderContentsChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    if (e.NewItems is not null)
    {
        foreach (var item in e.NewItems)
        {
            if (item is IFile file)
            {
                // Handle the new file
            }

            if (item is IFolder folder)
            {
                // Handle the new folder
            }
        }
    }

    // Something similar to above for e.OldItems...
}
```

#### Get folder CID
Flush data into IPFS and get the CID of all content in the folder.
```cs
var folder = new MfsFolder("/myApp/", ipfsClient);

var cid = folder.FlushAsync();
```
