using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class DestinationHeaderValueTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("/", "/")]
    [InlineData("/test1", "/test1")]
    [InlineData("test1", "/test1")]
    [InlineData("http://localhost:500/test1", "/test1")]
    public void TryParse(string? input, string? uriString)
    {
        // act
        var result = DestinationHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.Equal(uriString != null, result);
        if (uriString != null)
        {
            Assert.Equal(new ResourcePath(uriString), parsedValue?.Destination);
        }
    }
    
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("/", "/")]
    [InlineData("/test1", "/test1")]
    [InlineData("test1", "/test1")]
    [InlineData("http://localhost:500/test1", "/test1")]
    public void Parse(string input, string? uriString)
    {
        // act
        if (uriString == null)
        {
            Assert.Throws<FormatException>(() => DestinationHeaderValue.Parse(input));
            return;
        }

        var result = DestinationHeaderValue.Parse(input);

        // assert
        Assert.Equal(new ResourcePath(uriString), result.Destination);
    }
}