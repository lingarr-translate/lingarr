using System;
using Lingarr.Core.Helpers;
using Xunit;

namespace Lingarr.Server.Tests.Core;

public class UtcDateTimeTests
{
    private static readonly DateTimeOffset Expected = new(2026, 8, 20, 20, 52, 17, TimeSpan.Zero);

    [Theory]
    [InlineData("2026-08-20T20:52:17.0000000Z")]
    [InlineData("2026-08-20T14:52:17.0000000-06:00")]
    [InlineData("2026-08-20T20:52:17.0000000")]
    public void TryParse_ReturnsTheUtcInstant(string stored)
    {
        Assert.True(UtcDateTime.TryParse(stored, out var parsed));

        // Default parse styles resolve against the host clock, so this fails without the styles.
        Assert.Equal(TimeSpan.Zero, parsed.Offset);
        Assert.Equal(Expected, parsed);
    }

    [Theory]
    [InlineData("2026-08-20T20:52:17Z")]
    [InlineData("2026-08-20T14:52:17-06:00")]
    [InlineData("2026-08-20T20:52:17")]
    public void Parse_ReturnsTheUtcInstant(string upstream)
    {
        var parsed = UtcDateTime.Parse(upstream);

        Assert.Equal(TimeSpan.Zero, parsed.Offset);
        Assert.Equal(Expected, parsed);
    }
}
