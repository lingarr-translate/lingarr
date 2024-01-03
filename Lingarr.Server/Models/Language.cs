namespace Lingarr.Server.Models;
public interface ILanguage
{
    string name { get; set; }
    string code { get; set; }
}

public class LanguageModel : ILanguage
{
    public string name { get; set; }
    public string code { get; set; }
}

