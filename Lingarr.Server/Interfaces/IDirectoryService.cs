namespace Lingarr.Server.Interfaces;

public interface IDirectoryService
{
    List<string> GetDirectoryList(string directoryType);
}