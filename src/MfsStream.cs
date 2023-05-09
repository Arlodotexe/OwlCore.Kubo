using CommunityToolkit.Common;
using CommunityToolkit.Diagnostics;
using Ipfs.Http;

namespace OwlCore.Kubo;

/// <summary>
/// A <see cref="Stream"/> that reads and writes to a file in Kubo's Mutable FileSystem.
/// </summary>
public class MfsStream : Stream
{
    private readonly string _path;
    private readonly IpfsClient _client;
    private long _length;

    /// <summary>
    /// Creates a new instance of <see cref="MfsStream"/>.
    /// </summary>
    /// <param name="path">The MFS path of the file.</param>
    /// <param name="length">The known length of the stream.</param>
    /// <param name="client">The client to use for interacting with IPFS.</param>
    public MfsStream(string path, long length, IpfsClient client)
    {
        _path = path;
        _length = length;
        _client = client;
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => InternalCanWrite;

    /// <inheritdoc cref="CanWrite"/>
    internal bool InternalCanWrite { get; set; }

    /// <inheritdoc/>
    public override long Length => _length;

    /// <inheritdoc/>
    public override long Position { get; set; }

    /// <inheritdoc/>
    public override void Flush()
    {
        _ = _client.DoCommandAsync("files/flush", CancellationToken.None, _path).Result;
    }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _client.DoCommandAsync("files/flush", cancellationToken, _path);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
    }

    /// <inheritdoc/>
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Guard.IsLessThanOrEqualTo(offset + count, Length);
        Guard.IsGreaterThanOrEqualTo(offset, 0);

        var result = await _client.PostDownloadAsync("files/read", cancellationToken, _path, $"offset={Position + offset}", $"count={count}");

        using var memStream = new MemoryStream();
        await result.CopyToAsync(memStream);
        var bytes = memStream.ToArray();

        for (var i = 0; i < bytes.Length; i++)
            buffer[i] = bytes[i];

        Position += bytes.Length;

        return bytes.Length;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
        {
            Guard.IsLessThanOrEqualTo(offset, Length);
            Guard.IsGreaterThanOrEqualTo(offset, 0);
            Position = offset;
        }

        if (origin == SeekOrigin.End)
        {
            Guard.IsLessThanOrEqualTo(Length + offset, Length);
            Guard.IsLessThanOrEqualTo(offset, 0);
            Position = Length + offset;
        }

        if (origin == SeekOrigin.Current)
        {
            Guard.IsLessThanOrEqualTo(Position + offset, Length);
            Position = Position + offset;
        }

        return Position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        Guard.IsGreaterThanOrEqualTo(value, 0);
        _length = value;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count, CancellationToken.None).GetResultOrDefault();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            Flush();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Any(x => x.Message.Contains("not exist")))
        {
            // Ignored. Using statements cause disposing to happen when the stream leaves execution scope.
            // However, the containing file can be deleted before scope is left.
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Guard.IsGreaterThanOrEqualTo(offset, 0);

        if (Position + count > Length)
        {
            SetLength(Position + count);
        }

        await _client.Upload2Async("files/write", cancellationToken, new MemoryStream(buffer, offset, count), GetFileName(_path), $"arg={_path}", $"offset={Position}", $"count={count}", $"create=true");
        Position += count;
    }

    static string GetFileName(string path)
    {
        var dirName = Path.GetDirectoryName(path);
        return path.Replace('/', '\\').Replace(dirName ?? string.Empty, string.Empty).Trim('/').Trim('\\');
    }
}