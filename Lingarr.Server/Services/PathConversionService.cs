using Lingarr.Core.Data;
using Lingarr.Core.Enum;

namespace Lingarr.Server.Services;

public class PathConversionService
{
    private readonly LingarrDbContext _context;

    public PathConversionService(LingarrDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Converts and maps a source path based on the specified media type, applying configured path mappings
    /// and normalizing directory separators.
    /// </summary>
    /// <param name="sourcePath">The original path to be converted.</param>
    /// <param name="mediaType">The type of media associated with the path.</param>
    public string ConvertAndMapPath(string sourcePath, MediaType mediaType)
    {
        if (string.IsNullOrEmpty(sourcePath))
        {
            return sourcePath;
        }

        var normalizedPath = NormalizePath(sourcePath);
        return PathReplace(normalizedPath, mediaType);
    }

    /// <summary>
    /// Normalizes a file system path by converting Windows-style backslashes to forward slashes
    /// and removing Windows drive letters (e.g., "C:" or "D:").
    /// </summary>
    /// <param name="path">The path to normalize. Can be either Windows or Linux style path.</param>
    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        path = path.Replace('\\', '/').TrimStart();
        
        if (path.Length >= 2 && path[1] == ':')
        {
            path = path.Substring(2);
        }

        return path;
    }

    /// <summary>
    /// Applies configured path mappings from the database to the source path for the specified media type.
    /// </summary>
    /// <param name="sourcePath">The original path to apply mappings to.</param>
    /// <param name="mediaType">The type of media to filter relevant path mappings.</param>
    /// <returns>The path with all applicable mappings applied.</returns>
    private string PathReplace(string sourcePath, MediaType mediaType)
    {
        var mappings = _context.PathMappings.Where(m => m.MediaType == mediaType).ToList();
        foreach (var mapping in mappings)
        {
            if (sourcePath.Contains(mapping.SourcePath))
            {
                sourcePath = sourcePath.Replace(mapping.SourcePath, mapping.DestinationPath);
            }
        }

        return sourcePath;
    }
}