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

    [Fact]
    public void ParseStream_ParsesDialogueTimingAndText()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.50,0:00:03.25,Default,Bob,0,0,0,,Hello world\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        var item = Assert.Single(items);
        Assert.Equal(1500, item.StartTime);
        Assert.Equal(3250, item.EndTime);
        Assert.Equal(["Hello world"], item.Lines);
        Assert.Equal("Bob", item.SsaDialogue?.Name);
        Assert.Equal("Default", item.SsaDialogue?.Style);
    }

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

    [Fact]
    public void ParseStream_TextContainingCommas_NotSplit()
    {
        var ssa = MinimalHeader + "\n" +
                  "Dialogue: 0,0:00:01.00,0:00:02.00,Default,,0,0,0,,Hello, world, and more\n";

        var items = _parser.ParseStream(ToStream(ssa), Encoding.UTF8);

        Assert.Equal(["Hello, world, and more"], items[0].Lines);
    }

    [Fact]
    public void ParseStream_NoDialogueLines_Throws()
    {
        var ssa = MinimalHeader + "\n";

        Assert.Throws<ArgumentException>(() =>
            _parser.ParseStream(ToStream(ssa), Encoding.UTF8));
    }

    [Fact]
    public void ParseStream_NonSeekableStream_Throws()
    {
        var nonSeekable = new NonSeekableStream(Encoding.UTF8.GetBytes(MinimalHeader));
        Assert.Throws<ArgumentException>(() =>
            _parser.ParseStream(nonSeekable, Encoding.UTF8));
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
