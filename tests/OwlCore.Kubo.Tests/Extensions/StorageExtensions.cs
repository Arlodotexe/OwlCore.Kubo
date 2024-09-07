using System.Runtime.CompilerServices;
using OwlCore.Storage;

namespace OwlCore.Kubo.Tests;

public static class StorageExtensions
{
    public static async IAsyncEnumerable<IChildFile> CreateFilesAsync(this IModifiableFolder folder, int fileCount, Func<int, string> getFileName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < fileCount; i++)
            yield return await folder.CreateFileAsync(getFileName(i), overwrite: true, cancellationToken: cancellationToken);
    }

    public static async Task WriteRandomBytes(this IFile file, long numberOfBytes, int bufferSize, CancellationToken cancellationToken)
    {
        var rnd = new Random();

        await using var fileStream = await file.OpenWriteAsync(cancellationToken);

        var bytes = new byte[bufferSize];
        var bytesWritten = 0L;
        while (bytesWritten < numberOfBytes)
        {
            var remaining = numberOfBytes - bytesWritten;
            
            // Always runs if there are bytes left, even if there's fewer bytes left than the buffer.
            // Truncate the buffer size to remaining length if smaller than buffer.
            if (bufferSize > remaining)
                bufferSize = (int)remaining;

            if (bytes.Length != bufferSize)
                bytes = new byte[bufferSize];
            
            rnd.NextBytes(bytes);

            await fileStream.WriteAsync(bytes, cancellationToken);
            bytesWritten += bufferSize;
        }
    }

    public static async Task AssertStreamEqualAsync(this Stream srcFileStream, Stream destFileStream, int bufferSize, CancellationToken cancellationToken)
    {
        Assert.AreEqual(srcFileStream.Length, destFileStream.Length);

        var totalBytes = srcFileStream.Length;
        var bytesChecked = 0L;

        var srcBuffer = new byte[bufferSize];
        var destBuffer = new byte[bufferSize];

        // Fill each buffer until bufferSize is reached.
        // Each stream must fill the buffer until it is full,
        // except if no bytes are left.
        while (bytesChecked < totalBytes)
        {
            var srcBytesRead = 0;
            while (srcBytesRead < srcBuffer.Length)
            {
                var srcBytesReadInternal = await srcFileStream.ReadAsync(srcBuffer, offset: srcBytesRead, count: srcBuffer.Length - srcBytesRead, cancellationToken);
                if (srcBytesReadInternal == 0)
                    break;

                srcBytesRead += srcBytesReadInternal;
            }


            var destBytesRead = 0;
            while (destBytesRead < destBuffer.Length)
            {
                var destBytesReadInternal = await destFileStream.ReadAsync(destBuffer, offset: destBytesRead, count: destBuffer.Length - destBytesRead, cancellationToken);
                if (destBytesReadInternal == 0)
                    break;

                destBytesRead += destBytesReadInternal;
            }

            if (srcBytesRead != destBytesRead)
            {
                throw new InvalidOperationException($"Mismatch in bytes read between source and destination streams: {destBytesRead} and {srcBytesRead}.");
            }

            // When buffers are full, compare and continue.
            CollectionAssert.AreEqual(destBuffer, srcBuffer);
            bytesChecked += srcBytesRead;
        }
    }
}