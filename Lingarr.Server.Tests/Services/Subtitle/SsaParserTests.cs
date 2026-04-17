using System.IO;
using System.Text;
using Lingarr.Server.Services.Subtitle;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

/// <summary>
/// Tests for SsaParser — SSA/ASS subtitle parsing including vector drawing skip.
/// </summary>
public class SsaParserTests
{
    private readonly SsaParser _parser = new();

    // ===== Basic parsing =====

    [Fact]
    public void ParseStream_ValidAssFile_ReturnsSubtitleItems()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,Hello world
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal("Hello world", items[0].PlaintextLines[0]);
        Assert.Equal(1000, items[0].StartTime);
        Assert.Equal(4000, items[0].EndTime);
    }

    [Fact]
    public void ParseStream_MultipleDialogueLines_ReturnsAll()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,First line
Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Second line
Dialogue: 0,0:00:09.00,0:00:12.00,Default,,0,0,0,,Third line
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Equal(3, items.Count);
        Assert.Equal("First line", items[0].PlaintextLines[0]);
        Assert.Equal("Second line", items[1].PlaintextLines[0]);
        Assert.Equal("Third line", items[2].PlaintextLines[0]);
    }

    // ===== Line break handling =====

    [Fact]
    public void ParseStream_DialogueWithLineBreak_SplitsCorrectly()
    {
        var ass = @"[Script Info]
Title: Test
WrapStyle: 0

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,Line one\NLine two
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal(2, items[0].PlaintextLines.Count);
        Assert.Equal("Line one", items[0].PlaintextLines[0]);
        Assert.Equal("Line two", items[0].PlaintextLines[1]);
    }

    // ===== ASS markup handling =====

    [Fact]
    public void ParseStream_DialogueWithAssTags_StripsTagsFromPlaintext()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,{\b}Bold{\b0} text
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal("Bold text", items[0].PlaintextLines[0]);
    }

    // ===== SVG vector drawing lines — skip behavior =====

    /// <summary>
    /// Lines that contain only SVG vector drawing commands (no readable text)
    /// should be skipped entirely — not passed to translation as garbage.
    /// Example: karaoke sign overlays in anime where the text field is
    /// a Bezier path like "m 0 0 b 8 22 b 4 18".
    /// </summary>
    [Fact]
    public void ParseStream_BareVectorDrawingLine_SkipsLine()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,m 0 0 l 0 -403 l 716 -403 l 716 0
Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Real dialogue text
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        // Pure vector line skipped; only the real dialogue remains
        Assert.Single(items);
        Assert.Equal("Real dialogue text", items[0].PlaintextLines[0]);
    }

    /// <summary>
    /// Lines with {\pN} drawing-mode tags wrapping vector data should be skipped.
    /// These are typically Signs/OP/ED overlays in anime.
    /// </summary>
    [Fact]
    public void ParseStream_DrawingModeVectorLine_SkipsLine()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 6,0:00:00.00,0:00:02.00,Signs,,0,0,0,,{\p1}m 0 0 l 716 0 l 716 403 l 0 403{\p0}
Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,Actual subtitle
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        // Drawing line skipped; only real subtitle remains
        Assert.Single(items);
        Assert.Equal("Actual subtitle", items[0].PlaintextLines[0]);
    }

    /// <summary>
    /// Mixed line: vector commands followed by real text — should keep the text.
    /// </summary>
    [Fact]
    public void ParseStream_MixedVectorAndText_KeepsText()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,{\p1}m 0 0{\p0} Meeting at 9 AM
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal("Meeting at 9 AM", items[0].PlaintextLines[0]);
    }

    /// <summary>
    /// Multiple dialogue lines in a single entry, all pure vectors — skip.
    /// </summary>
    [Fact]
    public void ParseStream_AllLinesPureVector_SkipsEntry()
    {
        var ass = @"[Script Info]
Title: Test
WrapStyle: 0

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,m 0 0 l 10 10\Nm 20 20 l 30 30
";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        // All lines pure vectors → entry skipped
        Assert.Empty(items);
    }

    // ===== Error handling =====

    [Fact]
    public void ParseStream_EmptyFile_ThrowsArgumentException()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));

        var ex = Assert.Throws<ArgumentException>(() => _parser.ParseStream(stream, Encoding.UTF8));
        Assert.Contains("No valid subtitles", ex.Message);
    }

    [Fact]
    public void ParseStream_NonSeekableStream_ThrowsArgumentException()
    {
        var stream = new NonSeekableStream();
        var ex = Assert.Throws<ArgumentException>(() => _parser.ParseStream(stream, Encoding.UTF.UTF8));
        Assert.Contains("seekable", ex.Message);
    }

    // ===== Timing =====

    [Fact]
    public void ParseStream_TimingParsedCorrectly()
    {
        var ass = @"[Script Info]
Title: Test

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:01:30.50,0:01:45.75,Default,,0,0,0,,Timing test
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal(90500, items[0].StartTime);  // 1 min 30 sec 500 ms
        Assert.Equal(105750, items[0].EndTime);   // 1 min 45 sec 750 ms
    }

    // ===== Non-default WrapStyle =====

    [Fact]
    public void ParseStream_WrapStyleNone_RecognizesBackslashN()
    {
        var ass = @"[Script Info]
Title: Test
WrapStyle: 0

[V4+ Styles]
Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0

[Events]
Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,Line one\nLine two
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ass));
        var items = _parser.ParseStream(stream, Encoding.UTF8);

        Assert.Single(items);
        Assert.Equal(2, items[0].PlaintextLines.Count);
    }

    // ===== Helper =====

    private class NonSeekableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}