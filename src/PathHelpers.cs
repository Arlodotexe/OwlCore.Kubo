namespace OwlCore.Kubo;

/// <summary>
/// A collection of helpers for working with paths.
/// </summary>
public static class PathHelpers
{
    internal static string GetFolderItemName(string path)
    {
        var parts = path.Trim('/').Split('/').ToArray();
        return parts[^1];
    }

    internal static string GetParentPath(string relativePath)
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

    internal static string GetParentDirectoryName(string relativePath)
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