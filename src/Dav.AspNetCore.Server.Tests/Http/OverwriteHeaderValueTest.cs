using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class OverwriteHeaderValueTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Z", null)]
    [InlineData("F", false)]
    [InlineData("T", true)]
    public void TryParse(string? input, bool? expectedResult)
    {
        // act
        var result = OverwriteHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.Equal(expectedResult != null, result);
        if(expectedResult != null)
            Assert.Equal(expectedResult, parsedValue?.Overwrite);
    }
}