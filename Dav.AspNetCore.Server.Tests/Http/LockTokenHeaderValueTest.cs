using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class LockTokenHeaderValueTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>", "urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2")]
    public void TryParse(string? input, string? expectedResult)
    {
        // act
        var result = LockTokenHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.Equal(expectedResult != null, result);
        if (expectedResult != null)
            Assert.Equal(expectedResult, parsedValue?.Uri.AbsoluteUri);
    }
}