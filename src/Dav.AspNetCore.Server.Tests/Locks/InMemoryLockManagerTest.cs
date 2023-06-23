using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Locks;

public class InMemoryLockManagerTest
{
    [Theory]
    [InlineData(LockType.Exclusive, true, 0)]
    [InlineData(LockType.Exclusive, false, 60)]
    [InlineData(LockType.Shared, true, 0)]
    [InlineData(LockType.Shared, false, 60)]
    public async Task LockAsync(LockType lockType, bool recursive, int timeout)
    {
        // arrange
        var memoryLockManager = new InMemoryLockManager(Array.Empty<ResourceLock>());
        var path = new ResourcePath("/test.txt");

        // act
        var result = await memoryLockManager.LockAsync(
            path, 
            lockType, 
            new XElement("href", "xUnit"), 
            recursive, 
            TimeSpan.FromSeconds(timeout));

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.NotNull(result.ResourceLock);
        Assert.Equal(path, result.ResourceLock.Path);
        Assert.Equal(lockType, result.ResourceLock.LockType);
        Assert.Equal(recursive, result.ResourceLock.Recursive);
        Assert.Equal(TimeSpan.FromSeconds(timeout), result.ResourceLock.Timeout);
    }

    [Fact]
    public async Task LockAsync_ParentUriLocked_Fails()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var result = await memoryLockManager.LockAsync(
            new ResourcePath("/test.txt"), 
            LockType.Exclusive, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Locked, result.StatusCode);
        Assert.Null(result.ResourceLock);
    }

    [Fact]
    public async Task UnlockAsync()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/test.txt"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var statusCode = await memoryLockManager.UnlockAsync(resourceLock.Path, resourceLock.Id);

        // assert
        Assert.Equal(DavStatusCode.NoContent, statusCode);
    }
    
    [Fact]
    public async Task UnlockAsync_UriNotLocked_Fails()
    {
        // arrange
        var memoryLockManager = new InMemoryLockManager(Array.Empty<ResourceLock>());

        // act
        var statusCode = await memoryLockManager.UnlockAsync(new ResourcePath("/test.txt"), new Uri($"urn:uuid:{Guid.NewGuid():D}"));

        // assert
        Assert.Equal(DavStatusCode.Conflict, statusCode);
    }

    [Fact]
    public async Task LockAsync_SameUriLocked_Fails()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/test.txt"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });
        
        // act
        var result = await memoryLockManager.LockAsync(
            resourceLock.Path, 
            LockType.Exclusive, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Locked, result.StatusCode);
        Assert.Null(result.ResourceLock);
    }

    [Fact]
    public async Task RefreshLockAsync()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/test.txt"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);

        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var result = await memoryLockManager.RefreshLockAsync(
            resourceLock.Path, 
            resourceLock.Id,
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.NotNull(result.ResourceLock);
        Assert.Equal(resourceLock.Path, result.ResourceLock.Path);
        Assert.Equal(resourceLock.LockType, result.ResourceLock.LockType);
        Assert.Equal(resourceLock.Recursive, result.ResourceLock.Recursive);
        Assert.Equal(resourceLock.Timeout, result.ResourceLock.Timeout);
    }
    
    [Fact]
    public async Task RefreshLockAsync_LockDoesNotExist_Fails()
    {
        // arrange
        var memoryLockManager = new InMemoryLockManager(Array.Empty<ResourceLock>());

        // act
        var result = await memoryLockManager.RefreshLockAsync(
            new ResourcePath("/test.txt"), 
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.PreconditionFailed, result.StatusCode);
        Assert.Null(result.ResourceLock);
    }

    [Fact]
    public async Task LockAsync_SharedLockOnSharedUri_Works()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/test.txt"),
            LockType.Shared,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);

        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var result = await memoryLockManager.LockAsync(
            resourceLock.Path, 
            LockType.Shared, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.NotNull(result.ResourceLock);
        Assert.Equal(resourceLock.Path, result.ResourceLock.Path);
        Assert.Equal(resourceLock.LockType, result.ResourceLock.LockType);
        Assert.Equal(resourceLock.Recursive, result.ResourceLock.Recursive);
        Assert.Equal(resourceLock.Timeout, result.ResourceLock.Timeout);
    }
    
    [Fact]
    public async Task LockAsync_SharedLockOnSharedParentUri_Works()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/"),
            LockType.Shared,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);

        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });
        var path = new ResourcePath("/test.txt");

        // act
        var result = await memoryLockManager.LockAsync(
            path,
            LockType.Shared, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.NotNull(result.ResourceLock);
        Assert.Equal(path, result.ResourceLock.Path);
        Assert.Equal(resourceLock.LockType, result.ResourceLock.LockType);
        Assert.Equal(resourceLock.Recursive, result.ResourceLock.Recursive);
        Assert.Equal(resourceLock.Timeout, result.ResourceLock.Timeout);
    }
    
    [Fact]
    public async Task LockAsync_SharedLockOnExclusiveUri_Fails()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/test.txt"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);

        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var result = await memoryLockManager.LockAsync(
            resourceLock.Path, 
            LockType.Shared, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Locked, result.StatusCode);
        Assert.Null(result.ResourceLock);
    }
    
    [Fact]
    public async Task LockAsync_SharedLockOnExclusiveParentUri_Fails()
    {
        // arrange
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            new ResourcePath("/"),
            LockType.Exclusive,
            new XElement("href", "xUnit"),
            true,
            TimeSpan.Zero,
            DateTime.Now);

        var memoryLockManager = new InMemoryLockManager(new[] { resourceLock });

        // act
        var result = await memoryLockManager.LockAsync(
            new ResourcePath("/test.txt"), 
            LockType.Shared, 
            new XElement("href", "xUnit"), 
            true, 
            TimeSpan.Zero);

        // assert
        Assert.Equal(DavStatusCode.Locked, result.StatusCode);
        Assert.Null(result.ResourceLock);
    }

    [Fact]
    public async Task GetSupportedLocksAsync()
    {
        // arrange
        var memoryLockManager = new InMemoryLockManager(Array.Empty<ResourceLock>());
        var item = new TestStoreItem(new ResourcePath("/test.txt"));

        // act
        var result = await memoryLockManager.GetSupportedLocksAsync(item);

        // assert
        Assert.Equal(2, result.Count);
        Assert.Contains(LockType.Exclusive, result);
        Assert.Contains(LockType.Shared, result);
    }
}