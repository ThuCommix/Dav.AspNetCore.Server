using System.Xml.Linq;
using Dav.AspNetCore.Server.Store.Properties;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Store.Properties;

public class PropertyTest
{
    public PropertyTest()
    {
        Property.Registrations.Clear();
    }
    
    [Fact]
    public void RegisterProperty_MissingMetadata_CreatesDefaultMetadata()
    {
        // act
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"));

        // assert
        Assert.Null(property.Metadata.DefaultValue);
        Assert.False(property.Metadata.Expensive);
        Assert.False(property.Metadata.Computed);
        Assert.False(property.Metadata.Protected);
    }
    
    [Fact]
    public void RegisterProperty()
    {
        // act
        ValueTask ReadCallback(PropertyReadContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        ValueTask ChangeCallback(PropertyChangeContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            read: ReadCallback,
            change: ChangeCallback,
            metadata: new PropertyMetadata("Test", true, true, true));

        // assert
        Assert.Equal("MyProperty", property.Name);
        Assert.Equal(ReadCallback, property.ReadCallback);
        Assert.Equal(ChangeCallback, property.ChangeCallback);
        Assert.Equal("Test", property.Metadata.DefaultValue);
        Assert.True(property.Metadata.Expensive);
        Assert.True(property.Metadata.Computed);
        Assert.True(property.Metadata.Protected);
    }
}