using Lingarr.Server.Extensions;
using Xunit;

namespace Lingarr.Server.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void SplitIntoLines_EmptyString_ReturnsSingleEmptyLine()
    {
        var result = "".SplitIntoLines(42);

        Assert.Equal([""], result);
    }

    [Fact]
    public void SplitIntoLines_SingleWordUnderLimit_ReturnsSingleLine()
    {
        var result = "Hello".SplitIntoLines(42);

        Assert.Equal(["Hello"], result);
    }

    [Fact]
    public void SplitIntoLines_FitsInOneLine_ReturnsSingleLine()
    {
        var result = "Hello world".SplitIntoLines(42);

        Assert.Equal(["Hello world"], result);
    }

    [Fact]
    public void SplitIntoLines_ExactlyAtLimit_KeepsOnOneLine()
    {
        // "Hello world" is 11 chars, limit is 11
        var result = "Hello world".SplitIntoLines(11);

        Assert.Equal(["Hello world"], result);
    }

    [Fact]
    public void SplitIntoLines_OneCharOverLimit_WrapsAtWordBoundary()
    {
        // "Hello world" is 11 chars, limit is 10 -> wraps
        var result = "Hello world".SplitIntoLines(10);

        Assert.Equal(["Hello", "world"], result);
    }

    [Fact]
    public void SplitIntoLines_MultipleWraps_KeepsWordsIntact()
    {
        var result = "Hello world this is a longer test".SplitIntoLines(12);

        Assert.Equal(["Hello world", "this is a", "longer test"], result);
    }

    [Fact]
    public void SplitIntoLines_WordLongerThanLimit_PutsItOnItsOwnLine()
    {
        // "supercalifragilistic" is 20 chars, limit 10
        // We don't break words, so it stays as a single token on its own line
        var result = "hi supercalifragilistic bye".SplitIntoLines(10);

        Assert.Equal(["hi", "supercalifragilistic", "bye"], result);
    }
}
