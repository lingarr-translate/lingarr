namespace Lingarr.Server.Models.FileSystem;

public class SsaFormat
{
    public List<string> ScriptInfo { get; set; } = new();
    public List<string> Styles { get; set; } = new();
    public List<string> EventsFormat { get; set; } = new();
    public SsaWrapStyle WrapStyle { get; set; } = SsaWrapStyle.None;
}