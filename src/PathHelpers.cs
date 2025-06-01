using Ipfs;

namespace OwlCore.Kubo;

/// <summary>
/// A collection of helpers for working with paths.
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// The IPFS protocol path values, used to identify paths that are part of the IPFS network.
    /// These paths typically start with "ipfs://" or "/ipfs/".
    /// </summary>
    public static string[] IpfsProtocolPathValues { get; } = ["ipfs://", "/ipfs/"];

    /// <summary>
    /// The IPNS protocol path values, used to identify paths that are part of the IPNS (InterPlanetary Name System).
    /// These paths typically start with "ipns://" or "/ipns/".
    /// </summary>
    public static string[] IpnsProtocolPathValues { get; } = ["ipns://", "/ipns/"];

    /// <summary>
    /// The MFS protocol path values, used to identify paths that are part of the MFS (Mutable File System).
    /// These paths typically start with "mfs://" or "/mfs/".
    /// </summary>
    public static string[] MfsProtocolPathValues { get; } = ["mfs://", "/mfs/"];

    /// <summary>
    /// Tries to extract the file name from a path query, such as "ipfs://bafkreiemhhtwjmqpc735bpoa4rtahoowrmtrcn3jcy33jhdp7hvpqe2kx4?filename=MyFile".
    /// </summary>
    /// <param name="path">The path to extract the file name from.</param>
    /// <returns>The extracted file name, or null if not found.</returns>
    public static string? TryGetFileNameFromPathQuery(string path)
    {
        // Manually parse out the path query to get the file name
        // Handle with or without trailing slash before the query
        // Example: ipfs://bafkreiemhhtwjmqpc735bpoa4rtahoowrmtrcn3jcy33jhdp7hvpqe2kx4?filename=ManagedKeys
#if NET5_0_OR_GREATER
        var pathSplit = path.Split("filename=");
#else
        var pathSplit = path.Split(new[] { "filename=" }, StringSplitOptions.None);
#endif

        if (pathSplit.Length > 1)
        {
            var pathSplitFileNameStart = pathSplit[1];
            var fileNameEndSplit = pathSplitFileNameStart.Split('&');

            // If there are additional query parameters, take the first part as the file name
            if (fileNameEndSplit.Length > 0)
                return fileNameEndSplit[0];
            // No additional query parameters, return the file name directly
            else
                return pathSplitFileNameStart;
        }

        return null;
    }

    /// <summary>
    /// Removes the given protocols from a path, returning only the path itself.
    /// </summary>
    /// <param name="path">The path with a prepended protocol.</param>
    /// <param name="protocolValues">The protocol values to remove.</param>
    /// <returns>The provided path without a prepended protocol.</returns>
    /// <exception cref="ArgumentException">The provided path has no ipns protocol.</exception>
    public static string RemoveProtocols(string path, string[] protocolValues)
    {
        foreach (var protocol in protocolValues)
        {
            if (path.StartsWith(protocol))
            {
                // Remove the protocol prefix and return the path
                return path[protocol.Length..];
            }
        }

        throw new ArgumentException($"No given protocols '{string.Join(", ", protocolValues)}' were found in {path}");
    }

    /// <summary>
    /// Removes all query parameters from a path, returning only the path itself.
    /// </summary>
    public static string RemoveQueries(string path, char querySeparator = '?')
    {
        // Remove all query parameters if they exist
        var pathQuerySplit = path.Split(querySeparator);
        if (pathQuerySplit.Length > 1)
            return pathQuerySplit[0];

        return path;
    }

    /// <summary>
    /// Gets the name of the last item in a folder path, regardless of whether it's a file or a folder.
    /// This is useful for extracting the name of a file or folder from a path.
    /// </summary>
    /// <param name="path">The path to extract the item name from.</param>
    /// <returns>The name of the last item in the path.</returns>
    public static string GetFolderItemName(string path)
    {
        var parts = path.Trim('/').Split('/').ToArray();
        return parts[^1];
    }

    /// <summary>
    /// Gets the parent path of a given relative path.
    /// If the provided path is the root ("/"), it returns "/".
    /// </summary>
    /// <param name="relativePath">The relative path to get the parent of.</param>
    /// <returns>The parent path of the given relative path.</returns>
    public static string GetParentPath(string relativePath)
    {
        // If the provided path is the root.
        if (relativePath.Trim('/').Split('/').Count() == 1)
            return "/";

        var directorySeparatorChar = '/';

        // Path.GetDirectoryName() treats strings that end with a directory separator as a directory. If there's no trailing slash, it's treated as a file.
        var isFolder = relativePath.EndsWith(directorySeparatorChar.ToString());

        // Run it twice for folders. The first time only shaves off the trailing directory separator.
        var parentDirectoryName = isFolder ? Path.GetDirectoryName(Path.GetDirectoryName(relativePath)) : Path.GetDirectoryName(relativePath);

        // It also doesn't return a string that has a path separator at the end.
        return parentDirectoryName?.Replace('\\', '/') + (isFolder ? directorySeparatorChar : string.Empty) ?? string.Empty;
    }

    /// <summary>
    /// Gets the name of the parent directory of a given relative path.
    /// If the provided path is the root ("/"), it returns "/".
    /// <param name="relativePath">The relative path to get the parent directory name from.</param>
    /// <returns>The name of the parent directory.</returns>
    public static string GetParentDirectoryName(string relativePath)
    {
        // If the provided path is the root.
        if (Path.GetPathRoot(relativePath)?.Replace('\\', '/') == relativePath)
            return relativePath;

        var directorySeparatorChar = Path.DirectorySeparatorChar;

        var parentPath = GetParentPath(relativePath);
        var parentParentPath = GetParentPath(parentPath);

        return parentPath.Replace(parentParentPath, "").TrimEnd(directorySeparatorChar);
    }
}
