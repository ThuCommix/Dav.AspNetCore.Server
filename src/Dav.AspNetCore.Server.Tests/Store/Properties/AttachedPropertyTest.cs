using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Dav.AspNetCore.Server.Store.Properties.Converters;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Store.Properties;

public class AttachedPropertyTest
{
    [Fact]
    public void CreateAttached()
    {
        // arrange
        ValueTask<DavStatusCode> SetValueAsync<TOwner>(HttpContext context, TOwner item, object? value, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(DavStatusCode.Ok);
        }

        ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(PropertyResult.Success("Value"));
        }

        var propertyName = XName.Get("MyProperty");
        var converter = new StringConverter();

        // act
        var attachedProperty = AttachedProperty.CreateAttached<IStoreItem, string>(
            propertyName, 
            setter: SetValueAsync,
            getter: GetValueAsync, 
            converter: converter, 
            isExpensive: true, 
            isProtected: true, 
            isComputed: true);

        // assert
        Assert.Equal(propertyName, attachedProperty.Metadata.Name);
        Assert.True(attachedProperty.Metadata.IsExpensive);
        Assert.True(attachedProperty.Metadata.IsProtected);
        Assert.True(attachedProperty.Metadata.IsComputed);
        Assert.Equal(attachedProperty.GetValueAsync, GetValueAsync);
        Assert.Equal(attachedProperty.SetValueAsync, SetValueAsync);
    }

    [Fact]
    public async Task SetValueAsync_ConvertsValue()
    {
        // arrange
        ValueTask<PropertyResult> GetValueAsync<TOwner>(HttpContext context, TOwner item, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(PropertyResult.Success("Value"));
        }

        var item = new Mock<IStoreItem>(MockBehavior.Strict);
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        var converter = new Mock<IConverter<string>>(MockBehavior.Strict);
        converter.Setup(s => s.Convert(It.IsAny<string>())).Returns("Value");
        
        var propertyName = XName.Get("MyProperty");
        
        var attachedProperty = (AttachedProperty)AttachedProperty.CreateAttached<IStoreItem, string>(
            propertyName, 
            getter: GetValueAsync,
            converter: converter.Object);

        // act
        await attachedProperty.GetValueAsync!(httpContext.Object, item.Object, CancellationToken.None);

        // assert
        item.VerifyAll();
        httpContext.VerifyAll();
        converter.VerifyAll();
    }
    
    [Fact]
    public async Task GetValueAsync_ConvertsValue()
    {
        // arrange
        ValueTask<DavStatusCode> SetValueAsync<TOwner>(HttpContext context, TOwner item, object? value, CancellationToken cancellationToken)
            where TOwner : IStoreItem
        {
            return ValueTask.FromResult(DavStatusCode.Ok);
        }

        var item = new Mock<IStoreItem>(MockBehavior.Strict);
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        var converter = new Mock<IConverter<string>>(MockBehavior.Strict);
        converter.Setup(s => s.ConvertBack(It.IsAny<object?>())).Returns("Value");
        
        var propertyName = XName.Get("MyProperty");
        
        var attachedProperty = (AttachedProperty)AttachedProperty.CreateAttached<IStoreItem, string>(
            propertyName, 
            setter: SetValueAsync,
            converter: converter.Object);

        // act
        await attachedProperty.SetValueAsync!(httpContext.Object, item.Object, "Value", CancellationToken.None);

        // assert
        item.VerifyAll();
        httpContext.VerifyAll();
        converter.VerifyAll();
    }
}