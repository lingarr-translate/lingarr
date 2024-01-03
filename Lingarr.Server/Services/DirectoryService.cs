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

    public FileStream ReadFileStream(string FilePath)
    {
        if (File.Exists(FilePath))
        {
            return new FileStream(FilePath, FileMode.Open, FileAccess.Read);
        }
        else 
        {
            throw new FileNotFoundException($"File not found: {FilePath}");
        }
    }

    public FileStream WriteFileStream(string FilePath, string subtitleStream)
    {
        return new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write);
    }

    public List<DirectoryItem> GetDirectoryList(string mediaType)
    {
        // Validate directoryType parameter
        if (string.IsNullOrWhiteSpace(mediaType) ||
            !IsValidDirectoryType(mediaType))
        {
            throw new ArgumentException("Invalid directoryType. Supported values: tvshows, movies");
        }

        // Define a unique key for caching based on the directory type
        string cacheKey = $"MediaListCache_{mediaType}";

        // Try to retrieve the directory list from the cache
        if (!_memoryCache.TryGetValue<List<DirectoryItem>>(cacheKey, out var directoryList))
        {
            // If the cache is not available, read the directory and its contents
            string directoryPath = GetDirectoryPath(mediaType);
            directoryList = GetDirectoryContents(directoryPath, mediaType);

            // Cache the directory list with a specified expiration time (e.g., 1 hour)
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            _memoryCache.Set(cacheKey, directoryList, cacheEntryOptions);
        }

        return directoryList ?? new List<DirectoryItem>();
    }

    private bool IsValidDirectoryType(string directoryType)
    {
        return directoryType.Equals("tvshows", StringComparison.OrdinalIgnoreCase) ||
               directoryType.Equals("movies", StringComparison.OrdinalIgnoreCase);
    }

    private string GetDirectoryPath(string directoryType)
    {
        string baseDirectory = "Media/";

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
            _logger.LogError(exception.Message, exception.StackTrace);
            return new List<DirectoryItem>();
        }
    }

    private void GetContentsRecursively(string directoryPath, List<DirectoryItem> directoryContents, string rootDirectoryName)
    {
        // Get all files in the directory with the .srt extension
        string[] srtFiles = Directory.GetFiles(directoryPath, "*.srt");

        // Extract the folder name from the directory path
        string folderName = Path.GetFileName(directoryPath);

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
        string[] subdirectories = Directory.GetDirectories(directoryPath);

        // Recursively process subdirectories
        foreach (string subdirectory in subdirectories)
        {
            GetContentsRecursively(subdirectory, directoryContents, rootDirectoryName);
        }
    }
}
