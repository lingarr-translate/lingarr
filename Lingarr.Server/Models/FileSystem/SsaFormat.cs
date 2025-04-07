namespace Lingarr.Server.Models.FileSystem;

public class SsaFormat
{
    public List<string> ScriptInfo { get; set; } = [];
    public List<string> Styles { get; set; } = [];
    public List<string> EventsFormat { get; set; } = [];
    public SsaWrapStyle WrapStyle { get; set; } = SsaWrapStyle.None;
}