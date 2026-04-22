using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

/// <summary>
/// Tests for <see cref="SsaWriter"/> — covers the strip-formatting Styles block
/// behaviour (always emit a Default style even when <c>format.Styles</c> is empty)
/// and the empty-translation fallback to the original Lines.
/// </summary>
public class SsaWriterTests
{
    [Fact]
    public async Task WriteStreamAsync_StripFormatting_EmitsDefaultStyleEvenWithEmptyStyles()
    {
        // TranslationJob clears format.Styles before writing when stripping is on.
        // The writer must still emit [V4+ Styles] + "Style: Default" so Dialogue
        // lines referencing "Default" are not dangling.
        var item = new SubtitleItem
        {
            StartTime = 1000,
            EndTime = 4000,
            Lines = new() { "Hello" },
            PlaintextLines = new() { "Hello" },
            TranslatedLines = new() { "مرحبا" },
            SsaFormat = new SsaFormat
            {
                ScriptInfo = new() { "[Script Info]", "Title: Test" },
                Styles = new() /* deliberately empty — mimics post-clear state */
            }
        };

        using var ms = new MemoryStream();
        await new SsaWriter().WriteStreamAsync(ms, new[] { item }, stripSubtitleFormatting: true);
        var output = Encoding.UTF8.GetString(ms.ToArray());

        Assert.Contains("[V4+ Styles]", output);
        Assert.Contains("Style: Default,", output);
    }

    [Fact]
    public async Task WriteStreamAsync_PreservesOriginalStyles_WhenNotStripping()
    {
        var item = new SubtitleItem
        {
            StartTime = 0,
            EndTime = 2000,
            Lines = new() { "Hi" },
            PlaintextLines = new() { "Hi" },
            TranslatedLines = new() { "مرحبا" },
            SsaFormat = new SsaFormat
            {
                ScriptInfo = new() { "[Script Info]" },
                Styles = new() { "[V4+ Styles]", "Style: MyCustom,Arial,20,&H00FFFFFF" }
            }
        };

        using var ms = new MemoryStream();
        await new SsaWriter().WriteStreamAsync(ms, new[] { item }, stripSubtitleFormatting: false);
        var output = Encoding.UTF8.GetString(ms.ToArray());

        Assert.Contains("Style: MyCustom,Arial,20,&H00FFFFFF", output);
    }

    [Fact]
    public async Task WriteStreamAsync_EmptyTranslatedLines_FallsBackToOriginalLines()
    {
        // When TranslatedLines contains only empty strings (karaoke skip,
        // translator guard), the writer must fall back to the original Lines
        // so the renderer still receives the animation data / vector path.
        var item = new SubtitleItem
        {
            StartTime = 0,
            EndTime = 1000,
            Lines = new() { "{\\p1}m 0 0 l 10 10{\\p0}" },
            PlaintextLines = new() { "" },
            TranslatedLines = new() { "" },
            SsaFormat = new SsaFormat { ScriptInfo = new() { "[Script Info]" } }
        };

        using var ms = new MemoryStream();
        await new SsaWriter().WriteStreamAsync(ms, new[] { item }, stripSubtitleFormatting: false);
        var output = Encoding.UTF8.GetString(ms.ToArray());

        Assert.Contains("{\\p1}m 0 0 l 10 10{\\p0}", output);
    }
}
