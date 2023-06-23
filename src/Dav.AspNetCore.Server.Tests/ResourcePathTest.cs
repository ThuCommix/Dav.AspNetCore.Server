using System.Reflection;
using Xunit;

namespace Dav.AspNetCore.Server.Tests;

public class ResourcePathTest
{
    [Theory]
    [InlineData("/", null)]
    [InlineData("/test", "/")]
    [InlineData("/test/", "/")]
    [InlineData("/test/file1.txt", "/test")]
    [InlineData("/test/file1.txt/", "/test")]
    public void GetParent(string pathString, string? expectedParentPathString)
    {
        // arrange
        var inputPath = new ResourcePath(pathString);

        // act
        var parentPath = inputPath.Parent;

        // assert
        if (expectedParentPathString == null)
        {
            Assert.Null(parentPath);
            return;
        }
        
        Assert.NotNull(parentPath);
        Assert.Equal(expectedParentPathString, parentPath);
    }

    [Theory]
    [InlineData("/test", "item.txt", "/test/item.txt")]
    [InlineData("/test", "/abc", "/test/abc")]
    public void Combine(string path1String, string path2String, string expectedPathString)
    {
        // arrange
        var pathString1 = new ResourcePath(path1String);

        // act
        var combinedPath = ResourcePath.Combine(pathString1, path2String);

        // assert
        Assert.Equal(expectedPathString, combinedPath);
    }
    
    [Theory]
    [InlineData("/test/test.txt", "/test", "/test.txt")]
    [InlineData("/test/test.txt", "/abc", "/abc")]
    [InlineData("/test/test.txt", "/test/test/", "/test/test")]
    public void GetRelativePath(string relativeToPathString, string pathString, string expectedPathString)
    {
        // arrange
        var relativeTo = new ResourcePath(relativeToPathString);
        var path = new ResourcePath(pathString);

        // act
        var result = ResourcePath.GetRelativePath(relativeTo, path);

        // assert
        Assert.Equal(expectedPathString, result);
    }

    [Fact]
    public void Unescape()
    {
        // act
        var path = new ResourcePath("/test/hello%20test");

        // assert
        var value = path.GetType()
            .GetField("value",  BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(path);
        
        Assert.Equal("/test/hello test", value);
    }
    
    [Fact]
    public void Escape()
    {
        // act
        var path = new ResourcePath("/test/hello test");

        // assert
        Assert.Equal("/test/hello%20test", path);
    }
    
    [Fact]
    public void Name()
    {
        // act
        var path = new ResourcePath("/test/hello test");

        // assert
        Assert.Equal("hello test", path.Name);
    }
}