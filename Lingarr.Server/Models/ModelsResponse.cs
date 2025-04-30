namespace Lingarr.Server.Models;

public class ModelsResponse
{
    public List<LabelValue> Options { get; set; } = new();
    public string? Message { get; set; }
}