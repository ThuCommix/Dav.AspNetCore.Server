using Xunit;

namespace Dav.AspNetCore.Server.Tests;

public class UriHelperTest
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/test", "/")]
    [InlineData("/test/", "/")]
    [InlineData("/test/file1.txt", "/test/")]
    [InlineData("/test/file1.txt/", "/test/")]
    public void GetParent(string uriString, string expectedParentUriString)
    {
        // arrange
        var uri = new Uri(uriString);

        // act
        var parentUri = uri.GetParent();

        // assert
        Assert.Equal(new Uri(expectedParentUriString).LocalPath, parentUri.LocalPath);
    }

    [Theory]
    [InlineData("/test/test.txt", "/test", "/test.txt")]
    [InlineData("/test/test.txt", "/abc", "/abc")]
    [InlineData("/test/test.txt", "/test/test/", "/test/test/")]
    public void GetRelativeUri(string relativeToUriString, string uriString, string expectedUriString)
    {
        // arrange
        var relativeTo = new Uri(relativeToUriString);
        var uri = new Uri(uriString);

        // act
        var result = relativeTo.GetRelativeUri(uri);

        // assert
        Assert.Equal(new Uri(expectedUriString).LocalPath, result.LocalPath);
    }
}