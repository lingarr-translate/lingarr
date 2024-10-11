namespace Lingarr.Server.Services;

public class PathConversionService
{
    public PathConversionService()
    {
    }

    /// <summary>
    /// Normalizes the given path by ensuring consistent use of directory separators
    /// depending on the operating system.
    /// </summary>
    /// <param name="path">The input file or directory path.</param>
    /// <returns>The normalized path with consistent directory separators.</returns>
    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Normalize directory separators based on the current operating system
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Converts an external path to a path relative to the application's root folder.
    /// Removes the first directory from the external path and prefixes it with the application's root folder.
    /// </summary>
    /// <param name="externalPath">The external path to be converted.</param>
    /// <param name="rootFolder">The external path to be converted.</param>
    /// <returns>The path relative to the application's root folder.</returns>
    public string ConvertToAppPath(string externalPath, string rootFolder)
    {
        if (string.IsNullOrEmpty(externalPath))
            return externalPath;

        // Normalize the external path
        externalPath = NormalizePath(externalPath);

        // Handle the case where the external path is a root path (e.g., C:\ or /)
        if (Path.IsPathRooted(externalPath) && externalPath.Equals(Path.GetPathRoot(externalPath), StringComparison.OrdinalIgnoreCase))
        {
            // For root paths, add the root folder
            return Path.Combine(rootFolder, externalPath.TrimStart(Path.DirectorySeparatorChar));
        }

        // Split the path into parts (directories and files)
        var parts = externalPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        // If there are enough parts, remove the first directory and combine with the root folder
        if (parts.Length > 1)
        {
            string internalPath = Path.Combine(parts.Skip(1).ToArray());
            return Path.Combine(rootFolder, internalPath);
        }

        // If there's only one part or no first directory can be removed, but added root folder
        return Path.Combine(rootFolder, externalPath);
    }
}
