namespace Lingarr.Core.Logging;

public class CustomLogFormatterOptions
{
    public string Green { get; set; } = "\u001b[32m";
    public string Orange { get; set; } = "\u001b[33m";
    public string Red { get; set; } = "\u001b[31m"; 
    public string Reset { get; set; } = "\u001b[0m";
}