using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class TimeoutHeaderValueTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Minute-60")]
    public void TryParseInvalid(string? input)
    {
        // act
        var result = TimeoutHeaderValue.TryParse(input, out _);

        // assert
        Assert.False(result);
    }
    
    [Fact]
    public void TryParseInfinite()
    {
        // arrange
        var input = "Infinite";

        // act
        var result = TimeoutHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Single(parsedValue.Timeouts);
        Assert.Equal(TimeSpan.Zero, parsedValue.Timeouts[0]);
    }
    
    [Fact]
    public void TryParseSecond()
    {
        // arrange
        var input = "Second-60";

        // act
        var result = TimeoutHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Single(parsedValue.Timeouts);
        Assert.Equal(TimeSpan.FromSeconds(60), parsedValue.Timeouts[0]);
    }
    
    [Fact]
    public void TryParseMultiple()
    {
        // arrange
        var input = "Infinite, Second-60";

        // act
        var result = TimeoutHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(2, parsedValue.Timeouts.Length);
        Assert.Equal(TimeSpan.Zero, parsedValue.Timeouts[0]);
        Assert.Equal(TimeSpan.FromSeconds(60), parsedValue.Timeouts[1]);
    }
}