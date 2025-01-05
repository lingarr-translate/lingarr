using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services.Subtitle;

public class SsaWriter : ISubtitleWriter
{
    private string JoinLinesByWrapStyle(List<string> lines, SsaWrapStyle wrapStyle)
    {
        // Always join with \N as it's the standard in ASS/SSA
        // The wrap style affects how we handle the lines when reading/displaying
        // but when writing, we always use \N as the canonical format
        return string.Join("\\N", lines);
    }

    public async Task WriteStreamAsync(Stream stream, IEnumerable<SubtitleItem> subtitleItems)
    {
        var items = subtitleItems.ToList();
        if (!items.Any()) return;

        await using var writer = new StreamWriter(stream);
        var format = items[0].SsaFormat;

        // Write original sections
        if (format?.ScriptInfo.Any() == true)
        {
            foreach (var line in format.ScriptInfo)
            {
                await writer.WriteLineAsync(line);
            }
            await writer.WriteLineAsync();
        }

        if (format?.Styles.Any() == true)
        {
            foreach (var line in format.Styles)
            {
                await writer.WriteLineAsync(line);
            }
            await writer.WriteLineAsync();
        }

        // Write events section
        await writer.WriteLineAsync("[Events]");
        await writer.WriteLineAsync("Format: Marked, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var item in items)
        {
            var startTime = TimeSpan.FromMilliseconds(item.StartTime);
            var endTime = TimeSpan.FromMilliseconds(item.EndTime);
            var dialogue = item.SsaDialogue ?? new SsaDialogue();
            
            // Join lines according to wrap style
            var text = JoinLinesByWrapStyle(item.Lines, item.SsaFormat?.WrapStyle ?? SsaWrapStyle.None);
            
            await writer.WriteLineAsync(
                $"Dialogue: {dialogue.Marked}," +
                $"{startTime:h\\:mm\\:ss\\.ff}," +
                $"{endTime:h\\:mm\\:ss\\.ff}," +
                $"{dialogue.Style}," +
                $"{dialogue.Name}," +
                $"{dialogue.MarginL}," +
                $"{dialogue.MarginR}," +
                $"{dialogue.MarginV}," +
                $"{dialogue.Effect}," +
                $"{text}");
        }
    }
}