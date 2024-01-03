using Microsoft.Extensions.Caching.Memory;
using Lingarr.Server.Models;

namespace Lingarr.Server.Services;

public class DirectoryService
{
    private readonly IMemoryCache _memoryCache;
    private ILogger<DirectoryService> _logger;

    public DirectoryService(IMemoryCache memoryCache, ILogger<DirectoryService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger;
    }

    public List<DirectoryItem> GetDirectoryList(string mediaType)
    {
        // Validate directoryType parameter
        if (string.IsNullOrWhiteSpace(mediaType) ||
            !IsValidDirectoryType(mediaType))
        {
            _logger.LogError($"Invalid directoryType: {mediaType}");
            throw new ArgumentException("Invalid directoryType. Supported values: tvshows, movies");
        }

        // Define a unique key for caching based on the directory type
        var cacheKey = $"MediaListCache_{mediaType}";

        // Try to retrieve the directory list from the cache
        if (_memoryCache.TryGetValue<List<DirectoryItem>>(cacheKey, out var directoryList) && directoryList?.Count > 0)
        {
            return directoryList;
        }
        
        // If the cache is not available, read the directory and its contents
        var directoryPath = GetDirectoryPath(mediaType);
        directoryList = GetDirectoryContents(directoryPath, mediaType);

        // Cache the directory list with a specified expiration time (e.g., 1 hour)
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _memoryCache.Set(cacheKey, directoryList, cacheEntryOptions);

        return directoryList;
    }

    private bool IsValidDirectoryType(string directoryType)
    {
        return directoryType.Equals("tvshows", StringComparison.OrdinalIgnoreCase) ||
               directoryType.Equals("movies", StringComparison.OrdinalIgnoreCase);
    }

    private string GetDirectoryPath(string directoryType)
    {
        var baseDirectory = "Media/";

        switch (directoryType.ToLower())
        {
            case "tvshows":
                return Path.Combine(baseDirectory, "tvshows");
            case "movies":
                return Path.Combine(baseDirectory, "movies");
            default:
                throw new ArgumentException("Invalid directoryType");
        }
    }

    private List<DirectoryItem> GetDirectoryContents(string directoryPath, string rootDirectoryName)
    {
        try
        {
            // Recursively get all files and subdirectories in the specified directory
            List<DirectoryItem> directoryContents = new List<DirectoryItem>();
            GetContentsRecursively(directoryPath, directoryContents, rootDirectoryName);

            return directoryContents;
        }
        catch (Exception exception)
        {
            // Exceptions (e.g., directory not found, access denied)
            _logger.LogError(exception, "An error occurred when retrieving the directory: {ErrorMessage}", exception.Message);
            return new List<DirectoryItem>();
        }
    }

    private void GetContentsRecursively(string directoryPath, List<DirectoryItem> directoryContents, string rootDirectoryName)
    {
        // Get all files in the directory with the .srt extension
        var srtFiles = Directory.GetFiles(directoryPath, "*.srt");

        // Extract the folder name from the directory path
        var folderName = Path.GetFileName(directoryPath);

        // Check if the folder is not the root directory
        if (!string.Equals(folderName, rootDirectoryName, StringComparison.OrdinalIgnoreCase) && (srtFiles.Length > 0 || Directory.GetDirectories(directoryPath).Length > 0))
        {
            // Add the folder name and the array of .srt file paths to the list
            directoryContents.Add(new DirectoryItem
            {
                Name = folderName,
                Subtitles = srtFiles
            });
        }

        // Get all subdirectories
        var subdirectories = Directory.GetDirectories(directoryPath);

        // Recursively process subdirectories
        foreach (var subdirectory in subdirectories)
        {
            GetContentsRecursively(subdirectory, directoryContents, rootDirectoryName);
        }
    }
}
