using System;
using System.IO;
using System.Text;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

public class SsaParserTests
{
    private readonly SsaParser _parser = new();

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private const string MinimalHeader = """
        [Script Info]
        Title: Test
        ScriptType: v4.00+

        [V4+ Styles]
        Format: Name, Fontname, Fontsize
        Style: Default,Arial,20

        [Events]
        Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
        """;

    // ===== Basic parsing =====

    [Fact]
    public void ParseStream_ParsesDialogueTimingTextAndDialogueFields()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.50,0:00:03.25,Default,Bob,0,0,0,,Hello world\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        var item = Assert.Single(items);
        Assert.Equal(1500, item.StartTime);
        Assert.Equal(3250, item.EndTime);
        Assert.Equal(["Hello world"], item.Lines);
        Assert.Equal("Hello world", item.PlaintextLines[0]);
        Assert.Equal("Bob", item.SsaDialogue?.Name);
        Assert.Equal("Default", item.SsaDialogue?.Style);
    }

    [Fact]
    public void ParseStream_MultipleDialogueLines_ReturnsAll()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,First\n" +
                  "Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Second\n" +
                  "Dialogue: 0,0:00:09.00,0:00:12.00,Default,,0,0,0,,Third\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(3, items.Count);
        Assert.Equal("First", items[0].PlaintextLines[0]);
        Assert.Equal("Second", items[1].PlaintextLines[0]);
        Assert.Equal("Third", items[2].PlaintextLines[0]);
    }

    [Fact]
    public void ParseStream_DialogueWithAssTags_StripsTagsFromPlaintext()
    {
        var ssa = MinimalHeader + "\n" +
                  @"Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,{\b}Bold{\b0} text" + "\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal("Bold text", Assert.Single(items).PlaintextLines[0]);
    }

    [Fact]
    public void ParseStream_TextContainingCommas_NotSplit()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Hello, world, and more\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(["Hello, world, and more"], items[0].Lines);
    }

    // ===== Format / styles capture =====

    [Fact]
    public void ParseStream_CapturesV4PlusStylesSectionAndHeader()
    {
        var ssa = MinimalHeader + "\nDialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Hi\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        var styles = items[0].SsaFormat!.Styles;
        Assert.Contains("[V4+ Styles]", styles);
        Assert.Contains("Format: Name, Fontname, Fontsize", styles);
        Assert.Contains("Style: Default,Arial,20", styles);
    }

    [Fact]
    public void ParseStream_CapturesV4StylesSectionAndHeader()
    {
        var ssa = """
            [Script Info]
            Title: Legacy

            [V4 Styles]
            Format: Name, Fontname
            Style: Default,Arial

            [Events]
            Format: Marked, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: Marked=0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Hi
            """;

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        var styles = items[0].SsaFormat!.Styles;
        Assert.Contains("[V4 Styles]", styles);
        Assert.Contains("Style: Default,Arial", styles);
    }

    [Fact]
    public void ParseStream_CapturesEventsSectionHeader()
    {
        var ssa = MinimalHeader + "\nDialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Hi\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Contains("[Events]", items[0].SsaFormat!.EventsFormat);
        Assert.Contains(items[0].SsaFormat!.EventsFormat, l => l.StartsWith("Format:"));
    }

    // ===== Wrap style =====

    [Fact]
    public void ParseStream_ParsesWrapStyleFromScriptInfo()
    {
        var ssa = """
            [Script Info]
            Title: Test
            WrapStyle: 1

            [V4+ Styles]
            Format: Name
            Style: Default

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Line one\\NLine two
            """;

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(SsaWrapStyle.EndOfLine, items[0].SsaFormat!.WrapStyle);
    }

    [Theory]
    [InlineData(0)] // Smart
    [InlineData(1)] // EndOfLine
    [InlineData(3)] // SmartWideLowerLine
    public void ParseStream_NonNoneWrapStyles_SplitOnBackslashN_Only(int wrapStyleValue)
    {
        var ssa = $$"""
            [Script Info]
            WrapStyle: {{wrapStyleValue}}

            [V4+ Styles]
            Format: Name
            Style: Default

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,A\Nb\nc
            """;

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(["A", "b\\nc"], items[0].Lines);
    }

    [Fact]
    public void ParseStream_NoneWrapStyle_SplitsOnBothBackslashNAndBackslashLowerN()
    {
        var ssa = """
            [Script Info]
            WrapStyle: 2

            [V4+ Styles]
            Format: Name
            Style: Default

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,A\Nb\nc
            """;

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(["A", "b", "c"], items[0].Lines);
    }

    // ===== SVG vector drawing lines =====
    // Pure vector lines are kept in the parsed output with empty PlaintextLines:
    // the downstream translation guard skips empty plaintext while the original
    // Lines (carrying the vector path the renderer needs) survive to the writer.

    [Fact]
    public void ParseStream_BareVectorDrawingLine_KeepsEntryWithEmptyPlaintext()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,m 0 0 l 0 -403 l 716 -403 l 716 0\n" +
                  "Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Real dialogue text\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(2, items.Count);
        Assert.Equal(string.Empty, items[0].PlaintextLines[0]);
        Assert.Equal("Real dialogue text", items[1].PlaintextLines[0]);
    }

    [Fact]
    public void ParseStream_DrawingModeVectorLine_KeepsEntryWithEmptyPlaintext()
    {
        var ssa = MinimalHeader + "\n" +
                  @"Dialogue: 6,0:00:00.00,0:00:02.00,Signs,,0,0,0,,{\p1}m 0 0 l 716 0 l 716 403 l 0 403{\p0}" + "\n" +
                  "Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Actual subtitle\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(2, items.Count);
        Assert.Equal(string.Empty, items[0].PlaintextLines[0]);
        Assert.Equal("Actual subtitle", items[1].PlaintextLines[0]);
    }

    [Fact]
    public void ParseStream_MixedVectorAndText_KeepsText()
    {
        var ssa = MinimalHeader + "\n" +
                  @"Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,{\p1}m 0 0{\p0} Meeting at 9 AM" + "\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal("Meeting at 9 AM", Assert.Single(items).PlaintextLines[0]);
    }

    [Fact]
    public void ParseStream_AllLinesPureVector_KeepsEntryWithEmptyPlaintext()
    {
        var ssa = MinimalHeader + "\n" +
                  @"Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,m 0 0 l 10 10\Nm 20 20 l 30 30" + "\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        var item = Assert.Single(items);
        Assert.Equal(2, item.PlaintextLines.Count);
        Assert.All(item.PlaintextLines, p => Assert.Equal(string.Empty, p));
    }

    // ===== Layer-omitted fallback (older Aegisub output) =====

    [Fact]
    public void ParseStream_LayerOmittedDialogue_Parses()
    {
        // Older Aegisub/FFmpeg outputs write Dialogue lines without the Layer
        // column. Parser must treat "Text at column N-1" as a fallback so these
        // files don't error with "No valid subtitles".
        var ssa = """
            [Script Info]
            Title: Test

            [V4+ Styles]
            Format: Name, Fontname, Fontsize
            Style: Default,Arial,20

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0:00:01.00,0:00:04.00,Default,,0,0,0,,Hello from legacy Aegisub
            """;

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal("Hello from legacy Aegisub", Assert.Single(items).PlaintextLines[0]);
    }

    // ===== Error handling =====

    [Fact]
    public void ParseStream_NoDialogueLines_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _parser.ParseStream(ToStream(MinimalHeader + "\n"), Encoding.UTF8));
        Assert.Contains("No valid subtitles", ex.Message);
    }

    [Fact]
    public void ParseStream_NonSeekableStream_Throws()
    {
        var nonSeekable = new NonSeekableStream(Encoding.UTF8.GetBytes(MinimalHeader));
        var ex = Assert.Throws<ArgumentException>(() =>
            _parser.ParseStream(nonSeekable, Encoding.UTF8));
        Assert.Contains("seekable", ex.Message);
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
