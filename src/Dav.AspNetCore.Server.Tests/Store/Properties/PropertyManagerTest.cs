using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Dav.AspNetCore.Server.Store.Properties.Converters;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Store.Properties;

public class PropertyManagerTest
{
    [Fact]
    public void Indexer_UnknownProperty_ReturnsNull()
    {
        // arrange
        var item = new Mock<IStoreItem>();
        var propertyManager = new PropertyManager(item.Object);

        // act
        var metadata = propertyManager[XName.Get("MyProperty")];

        // assert
        Assert.Null(metadata);
    }
    
    [Fact]
    public void Indexer_KnownProperty_ReturnsPropertyMetadata()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName);

        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        // act
        var metadata = propertyManager[XName.Get("MyProperty")];

        // assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName);

        // act
        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        // assert
        Assert.Single(propertyManager.Properties);
    }
    
    [Fact]
    public void AttachProperty_AlreadyAttached_Throws()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName);

        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        // act
        Assert.Throws<InvalidOperationException>(() => propertyManager.AttachProperty(attachedProperty));
    }
    
    [Fact]
    public void AttachProperty_NewProperty_UpdatesPropertyMetadata()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName1 = XName.Get("MyProperty1");
        var attachedProperty1 = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName1);
        
        var propertyName2 = XName.Get("MyProperty2");
        var attachedProperty2 = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName2);

        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty1 });

        // act
        propertyManager.AttachProperty(attachedProperty2);

        // assert
        Assert.Equal(2, propertyManager.Properties.Count);
        Assert.NotNull(propertyManager[propertyName2]);
        Assert.Equal(propertyName2, propertyManager[propertyName2]?.Name);
    }

    [Fact]
    public void DetachProperty_ForeignMetadata_ReturnsFalse()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName1 = XName.Get("MyProperty1");
        var attachedProperty1 = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName1);
        
        var propertyName2 = XName.Get("MyProperty2");
        var propertyMetadata = new PropertyMetadata(propertyName2, false, false, false);

        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty1 });

        // act
        var result = propertyManager.DetachProperty(propertyMetadata);

        // assert
        Assert.False(result);
    }
    
    [Fact]
    public void DetachProperty_KnownProperty_ReturnsTrue()
    {
        // arrange
        var item = new Mock<IStoreItem>();

        var propertyName1 = XName.Get("MyProperty1");
        var attachedProperty1 = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName1);
        
        var propertyName2 = XName.Get("MyProperty2");
        var attachedProperty2 = AttachedProperty.CreateAttached<IStoreItem, string>(propertyName2);

        var propertyManager = new PropertyManager(item.Object, new[] { attachedProperty1, attachedProperty2 });

        var propertyMetadata = propertyManager[propertyName2]!;

        // act
        var result = propertyManager.DetachProperty(propertyMetadata);

        // assert
        Assert.True(result);
        Assert.Single(propertyManager.Properties);
        Assert.Null(propertyManager[propertyName2]);
    }

    [Fact]
    public async Task GetPropertyAsync_UnknownProperty_ReturnsNotFound()
    {
        // arrange
        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");

        IPropertyManager propertyManager = new PropertyManager(item.Object);

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DavStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Value);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_NoGetter_ReturnsNull()
    {
        // arrange
        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, getter: null);

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_WithGetter_ReturnsValue()
    {
        // arrange
        ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(PropertyResult.Success("Value"));
        }
        
        var item = new Mock<IStoreItem>();

        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, getter: GetValueAsync, converter: new StringConverter());

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Equal("Value", result.Value);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_WithGetter_ReturnsCacheValue()
    {
        // arrange
        var count = 0;
        
        ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            count++;
            return ValueTask.FromResult(PropertyResult.Success("Value"));
        }
        
        var item = new Mock<IStoreItem>();

        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, getter: GetValueAsync, converter: new StringConverter());

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);
        await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);

        // assert
        Assert.Equal(1, count);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_UnknownProperty_ReturnsNotFound()
    {
        // arrange
        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");

        IPropertyManager propertyManager = new PropertyManager(item.Object);

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.SetPropertyAsync(httpContext.Object, propertyName, null);

        // assert
        Assert.Equal(DavStatusCode.NotFound, result);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_NoSetter_ReturnsForbidden()
    {
        // arrange
        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, setter: null);

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.SetPropertyAsync(httpContext.Object, propertyName, null);

        // assert
        Assert.Equal(DavStatusCode.Forbidden, result);
        
        httpContext.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithSetter_ReturnsOk()
    {
        // arrange
        ValueTask<DavStatusCode> SetValueAsync<TOwner>(HttpContext context, TOwner item, object? value, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(DavStatusCode.Ok);
        }

        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, setter: SetValueAsync, converter: new StringConverter());

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.SetPropertyAsync(httpContext.Object, propertyName, null);

        // assert
        Assert.Equal(DavStatusCode.Ok, result);
        
        httpContext.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetPropertyAsync_SuccessfulSet_UpdatesCache(bool successfulSet)
    {
        // arrange
        ValueTask<DavStatusCode> SetValueAsync<TOwner>(HttpContext context, TOwner item, object? value, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return successfulSet ? ValueTask.FromResult(DavStatusCode.Ok) : ValueTask.FromResult(DavStatusCode.Forbidden);
        }
        
        var count = 0;
        
        ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            count++;
            return ValueTask.FromResult(PropertyResult.Success("Value"));
        }

        var item = new Mock<IStoreItem>();
        
        var propertyName = XName.Get("MyProperty");
        var attachedProperty = AttachedProperty
            .CreateAttached<IStoreItem, string>(propertyName, setter: SetValueAsync, getter: GetValueAsync, converter: new StringConverter());

        IPropertyManager propertyManager = new PropertyManager(item.Object, new[] { attachedProperty });

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

        // act
        var result = await propertyManager.SetPropertyAsync(httpContext.Object, propertyName, null);
        await propertyManager.GetPropertyAsync(httpContext.Object, propertyName);

        // assert
        Assert.Equal(successfulSet ? DavStatusCode.Ok : DavStatusCode.Forbidden, result);
        Assert.Equal(successfulSet ? 0 : 1, count);
        
        httpContext.VerifyAll();
    }
}