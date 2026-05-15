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
        // Even when no source styles exist, the writer must emit
        // [V4+ Styles] + "Style: Default" so Dialogue lines referencing
        // "Default" are not dangling.
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

    [Fact]
    public async Task WriteStreamAsync_StripFormatting_PreservesSourceDefaultStyle()
    {
        // Fansubs declare their own Default style (e.g. Open Sans 55 at
        // PlayResY=720). Stripping should preserve it verbatim rather than
        // replacing with a generic fallback that renders too small.
        var item = new SubtitleItem
        {
            StartTime = 0,
            EndTime = 1000,
            Lines = new() { "Hi" },
            PlaintextLines = new() { "Hi" },
            TranslatedLines = new() { "مرحبا" },
            SsaFormat = new SsaFormat
            {
                ScriptInfo = new() { "[Script Info]", "PlayResY: 720" },
                Styles = new()
                {
                    "[V4+ Styles]",
                    "Style: Default,Open Sans,55,&H00EEEEEE,&H00FFFFFF,&H002A2A2A,&HB4000000,-1,0,0,0,100,100,0,0,1,3.25,0,2,90,90,30,1",
                    "Style: Alt,Open Sans,40,&H00FFFFFF"
                }
            }
        };

        using var ms = new MemoryStream();
        await new SsaWriter().WriteStreamAsync(ms, new[] { item }, stripSubtitleFormatting: true);
        var output = Encoding.UTF8.GetString(ms.ToArray());

        Assert.Contains("Style: Default,Open Sans,55,", output);
        Assert.DoesNotContain("Roboto Medium", output);
    }

    [Fact]
    public async Task WriteStreamAsync_StripFormatting_ScalesFallbackFontsizeToPlayResY()
    {
        // When the source has no Default style, the generic Roboto fallback
        // must scale fontsize by PlayResY so it reads at a sensible size on
        // high-res canvases (~48 at 720, ~72 at 1080) rather than a fixed 26.
        var item = new SubtitleItem
        {
            StartTime = 0,
            EndTime = 1000,
            Lines = new() { "Hi" },
            PlaintextLines = new() { "Hi" },
            TranslatedLines = new() { "مرحبا" },
            SsaFormat = new SsaFormat
            {
                ScriptInfo = new() { "[Script Info]", "PlayResY: 720" },
                Styles = new() { "[V4+ Styles]", "Style: Alt,Arial,30,&H00FFFFFF" }
            }
        };

        using var ms = new MemoryStream();
        await new SsaWriter().WriteStreamAsync(ms, new[] { item }, stripSubtitleFormatting: true);
        var output = Encoding.UTF8.GetString(ms.ToArray());

        Assert.Contains("Style: Default,Roboto Medium,48,", output);
    }
}
