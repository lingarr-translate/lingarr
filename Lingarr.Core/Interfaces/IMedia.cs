namespace Lingarr.Core.Interfaces;

public interface IMedia
{
    int Id { get; set; }
    string Title { get; set; }
    string? FileName { get; set; }
    string? Path { get; set; }
    string? MediaHash { get; set; }
}