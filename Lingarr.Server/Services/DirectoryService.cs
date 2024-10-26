using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class DirectoryService : IDirectoryService
{
    /// <inheritdoc />
    public DirectoryInfo GetDirectoryInfo(string path)
    {
        return new DirectoryInfo(path);
    }
    
    /// <inheritdoc />
    public List<DirectoryItem> GetDirectoryContents(string path)
    {
        var directory = GetDirectoryInfo(path);
        var items = new List<DirectoryItem>();

        foreach (var dir in directory.GetDirectories())
        {
            items.Add(new DirectoryItem
            {
                Name = dir.Name,
                FullPath = dir.FullName
            });
        }

        return items.OrderBy(i => i.Name).ToList();
    }
}