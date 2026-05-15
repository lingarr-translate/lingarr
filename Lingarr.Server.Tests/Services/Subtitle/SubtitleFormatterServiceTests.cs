using System.Text;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

/// <summary>
/// Tests for SubtitleFormatterService — markup removal and SVG vector drawing skip.
/// </summary>
public class SubtitleFormatterServiceTests
{
    // ===== Normal text handling =====

    [Fact]
    public void RemoveMarkup_PlainText_ReturnsUnchanged()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello world");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_WithAssTags_StripsTags()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello {\\b}world{\\b0}");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_WithHtmlTags_StripsTags()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello <b>world</b>");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_WithLineBreaks_ConvertsToSpaces()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello\\Nworld\\nsecond line");
        Assert.Equal("Hello world second line", result);
    }

    [Fact]
    public void RemoveMarkup_WithHardSpace_ConvertsToSpace()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello\\hworld");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_WithTab_ConvertsToSpace()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello\\tworld");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_WithMultipleWhitespace_CollapsesToSingleSpace()
    {
        var result = SubtitleFormatterService.RemoveMarkup("Hello   world\\n\\n  test");
        Assert.Equal("Hello world test", result);
    }

    [Fact]
    public void RemoveMarkup_EmptyString_ReturnsEmpty()
    {
        var result = SubtitleFormatterService.RemoveMarkup("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_Null_ReturnsEmpty()
    {
        var result = SubtitleFormatterService.RemoveMarkup(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_WhitespaceOnly_ReturnsEmpty()
    {
        var result = SubtitleFormatterService.RemoveMarkup("   ");
        Assert.Equal(string.Empty, result);
    }

    // ===== ASS drawing-mode tag stripping ({\pN}...{\p0}) =====

    /// <summary>
    /// Karaoke/OP subtitles in anime use {\p1} drawing mode to render vector text.
    /// The drawing commands remain after tag stripping if not handled explicitly.
    /// </summary>
    [Fact]
    public void RemoveMarkup_WithDrawingModeTags_StripsTagAndVectorCommands()
    {
        // {\p1} enables drawing mode; {\p0} disables; commands in between are vector data
        var input = "{\\p1}m 0 0 l 100 100{\\p0}";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        // After stripping {\p1}...{\p0} the inner vector path would remain,
        // but our regex strips the entire {\pN}...{\p0} block first
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_DrawingModeTagInMiddle_StripsAndReturnsText()
    {
        var input = "Hello {\\p1}m 0 0{\\p0} world";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void RemoveMarkup_NestedTags_StripsCorrectly()
    {
        var input = "{\\blur5}{\\p1}m 0 0{\\p0}{\\blur0}Text";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal("Text", result);
    }

    // ===== Bare SVG vector path detection =====

    /// <summary>
    /// Some ASS files store vector art as plain text (no {\pN} wrapping).
    /// The text field contains only SVG-like commands: "m 0 0 l 100 100".
    /// These must be detected and skipped — they are not translatable text.
    /// </summary>
    [Fact]
    public void RemoveMarkup_BareVectorPath_mCommand_ReturnsEmpty()
    {
        var input = "m 0 0";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_BareVectorPath_mWithCoordinates_ReturnsEmpty()
    {
        var input = "m 0 0 l 100 200 l -50 75";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_BareVectorPath_MixedCaseCommand_ReturnsEmpty()
    {
        var result1 = SubtitleFormatterService.RemoveMarkup("M 0 0 L 100 200");
        var result2 = SubtitleFormatterService.RemoveMarkup("c 10 20 30 40 50 60");
        var result3 = SubtitleFormatterService.RemoveMarkup("B 1 2 3 4 5 6");
        Assert.Equal(string.Empty, result1);
        Assert.Equal(string.Empty, result2);
        Assert.Equal(string.Empty, result3);
    }

    [Fact]
    public void RemoveMarkup_BareVectorPath_WithDecimalCoords_ReturnsEmpty()
    {
        var input = "m 0.5 -10.25 l 100.0 200.5";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_BareVectorPath_WithScientificNotation_ReturnsEmpty()
    {
        var input = "m 1e2 2E-3 l 3.5e1 -4.2E+2";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveMarkup_BareVectorPath_ChainedCommands_ReturnsEmpty()
    {
        // Bare vector lines often chain multiple commands without {\pN} tags
        var input = "m 9 23 b 8 22 b 4 18 b 6 25 b 8 12";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Text that LOOKS like it starts with a vector command but has readable
    /// characters after the coordinates should NOT be skipped.
    /// </summary>
    [Fact]
    public void RemoveMarkup_TextStartingWithVectorCommand_NotEmpty()
    {
        // "morning" starts with 'm' but has readable text after
        var result = SubtitleFormatterService.RemoveMarkup("m 9:00 morning meeting");
        Assert.Equal("m 9:00 morning meeting", result);
    }

    [Fact]
    public void RemoveMarkup_CleanText_ReturnsText()
    {
        var result = SubtitleFormatterService.RemoveMarkup("This is clean dialogue");
        Assert.Equal("This is clean dialogue", result);
    }

    [Fact]
    public void RemoveMarkup_VectorCommandsWithTextAfter_ReturnsText()
    {
        // A line that contains vector art AND readable text should keep the text
        var input = "{\\p1}m 0 0 l 100 100{\\p0} Meeting";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal("Meeting", result);
    }

    [Fact]
    public void RemoveMarkup_DrawingModeWithMultiplePaths_StripsEntireBlock()
    {
        // Multiple {\pN} blocks in one line — all should be stripped
        var input = "{\\p1}m 0 0 0 100{\\p0} Title {\\p1}m 200 200 200 300{\\p0}";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal("Title", result);
    }

    // ===== Edge cases =====

    [Fact]
    public void RemoveMarkup_CaseInsensitiveDrawingTags_StripsCorrectly()
    {
        var input = "{\\P1}m 0 0{\\P0} text {\\p2}m 10 10{\\p2} more";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal("text more", result);
    }

    [Fact]
    public void RemoveMarkup_UnclosedDrawingTag_StripsVectorPrefix()
    {
        // Unclosed {\p1}: drawing-block stripper needs a paired \p and skips
        // this case. Once braces are removed the residue starts with a vector
        // prefix ("m 0 0 …"), which VectorPrefixPattern catches as drawing
        // data with trailing residue and drops.
        var input = "{\\p1}m 0 0 text without close";
        var result = SubtitleFormatterService.RemoveMarkup(input);
        Assert.Equal(string.Empty, result);
    }
}