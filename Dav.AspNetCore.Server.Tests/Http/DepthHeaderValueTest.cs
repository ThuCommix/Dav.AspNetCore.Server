using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class DepthHeaderValueTest
{
    [Theory]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("abc", null)]
    [InlineData("0", Depth.None)]
    [InlineData("1", Depth.One)]
    [InlineData("infinity", Depth.Infinity)]
    public void TryParse(string? input, Depth? expectedResult)
    {
        // act
        var result = DepthHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.Equal(expectedResult != null, result);
        if (expectedResult != null)
        {
            Assert.Equal(expectedResult, parsedValue?.Depth);
        }
    }
}