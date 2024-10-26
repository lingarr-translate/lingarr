using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

public interface IDirectoryService
{
    /// <summary>
    /// Gets directory information for the specified path
    /// </summary>
    /// <param name="path">The full path to the directory</param>
    /// <returns>Directory information object</returns>
    DirectoryInfo GetDirectoryInfo(string path);
    
    /// <summary>
    /// Gets a list of directory items contained in the specified path
    /// </summary>
    /// <param name="path">The full path to search</param>
    /// <returns>An ordered list of directory items</returns>
    List<DirectoryItem> GetDirectoryContents(string path);
}