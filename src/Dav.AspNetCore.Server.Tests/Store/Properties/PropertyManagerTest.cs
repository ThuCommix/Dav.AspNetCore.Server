using System.Xml.Linq;
using Dav.AspNetCore.Server.Store.Properties;
using Moq;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Store.Properties;

public class PropertyManagerTest
{
    public PropertyManagerTest()
    {
        Property.Registrations.Clear();
    }
    
    [Fact]
    public void GetPropertyMetadata_ExistingProperty_ReturnsPropertyMetadata()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(
                DefaultValue: 9,
                Expensive: true,
                Protected: true,
                Computed: true));
        
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        var propertyManager = new PropertyManager(serviceProvider.Object);

        var storeItem = new TestStoreItem(new Uri("/"));

        // act
        var propertyMetadata = propertyManager.GetPropertyMetadata(storeItem, property.Name);

        // assert
        Assert.NotNull(propertyMetadata);
        Assert.Equal(property.Metadata.DefaultValue, propertyMetadata.DefaultValue);
        Assert.Equal(property.Metadata.Expensive, propertyMetadata.Expensive);
        Assert.Equal(property.Metadata.Protected, propertyMetadata.Protected);
        Assert.Equal(property.Metadata.Computed, propertyMetadata.Computed);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public void GetPropertyMetadata_MissingProperty_ReturnsNull()
    {
        // arrange
        var serviceProviderMock = new Mock<IServiceProvider>(MockBehavior.Strict);
        var propertyManager = new PropertyManager(serviceProviderMock.Object);

        var storeItem = new TestStoreItem(new Uri("/"));

        // act
        var propertyMetadata = propertyManager.GetPropertyMetadata(storeItem, XName.Get("MyProperty"));

        // assert
        Assert.Null(propertyMetadata);
        
        serviceProviderMock.VerifyAll();
    }

    [Fact]
    public async Task GetPropertyNamesAsync_WithoutPropertyStore_ReturnsRegisteredProperties()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(
                DefaultValue: 9,
                Expensive: true,
                Protected: true,
                Computed: true));
        
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        var propertyManager = new PropertyManager(serviceProvider.Object);

        var storeItem = new TestStoreItem(new Uri("/"));
        
        // act
        var results = await propertyManager.GetPropertyNamesAsync(storeItem);

        // assert
        Assert.Single(results);
        Assert.Equal(property.Name, results.First());
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyNamesAsync_WithPropertyStore_ReturnsRegisteredProperties()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(
                DefaultValue: 9,
                Expensive: true,
                Protected: true,
                Computed: true));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var property2 = XName.Get("MyProperty2");
        var propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
        propertyStore.Setup(s => s.GetPropertiesAsync(storeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PropertyData(property2, null) });
        
        var propertyManager = new PropertyManager(serviceProvider.Object, propertyStore.Object);

        // act
        var results = await propertyManager.GetPropertyNamesAsync(storeItem);

        // assert
        Assert.Equal(2, results.Count);
        Assert.Equal(property.Name, results.First());
        Assert.Equal(property2, results.Last());
        
        serviceProvider.VerifyAll();
        propertyStore.VerifyAll();
    }

    [Fact]
    public async Task GetPropertyAsync_MissingProperty_ReturnsNotFound()
    {
        // arrange
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, XName.Get("MyProperty"));

        // assert
        Assert.Equal(DavStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Value);
        
        serviceProvider.VerifyAll();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(9)]
    public async Task GetPropertyAsync_NoRead_ReturnsMetadataDefaultValue(object? defaultValue)
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(DefaultValue: defaultValue));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Equal(defaultValue, result.Value);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_WithRead_ReturnsValue()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            read: (context, _) =>
            {
                context.SetResult("MyValue");
                return ValueTask.CompletedTask;
            });
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Equal("MyValue", result.Value);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_SecondRead_ReturnsCacheValue()
    {
        // arrange
        var readCalls = 0;
        
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            read: (context, _) =>
            {
                context.SetResult("MyValue");
                readCalls++;
                return ValueTask.CompletedTask;
            });
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);
        await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // assert
        Assert.Equal(1, readCalls);
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Equal("MyValue", result.Value);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_NoReadWithPropertyStore_ReturnsValue()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
        propertyStore.Setup(s => s.GetPropertiesAsync(storeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PropertyData(property.Name, "MyValue") });
        
        var propertyManager = new PropertyManager(serviceProvider.Object, propertyStore.Object);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // assert
        Assert.Equal(DavStatusCode.Ok, result.StatusCode);
        Assert.Equal("MyValue", result.Value);
        
        serviceProvider.VerifyAll();
        propertyStore.VerifyAll();
    }
    
    [Fact]
    public async Task GetPropertyAsync_NoReadWithPropertyStoreMissing_ReturnsNotFound()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
        propertyStore.Setup(s => s.GetPropertiesAsync(storeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PropertyData>());
        
        var propertyManager = new PropertyManager(serviceProvider.Object, propertyStore.Object);

        // act
        var result = await propertyManager.GetPropertyAsync(storeItem, property.Name);

        // assert
        Assert.Equal(DavStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Value);
        
        serviceProvider.VerifyAll();
        propertyStore.VerifyAll();
    }

    [Fact]
    public async Task SetPropertyAsync_MetadataProtected_ReturnsForbidden()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(Protected: true));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, property.Name, null);

        // assert
        Assert.Equal(DavStatusCode.Forbidden, result);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_MetadataComputed_ReturnsForbidden()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            metadata: new PropertyMetadata(Computed: true));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, property.Name, null);

        // assert
        Assert.Equal(DavStatusCode.Forbidden, result);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_MissingProperty_ReturnsNotFound()
    {
        // arrange
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, XName.Get("MyProperty"), null);

        // assert
        Assert.Equal(DavStatusCode.NotFound, result);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_NoChange_ReturnsForbidden()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, property.Name, null);

        // assert
        Assert.Equal(DavStatusCode.Forbidden, result);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithChange_ReturnsOk()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"),
            change: (_, _) => ValueTask.CompletedTask);
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        
        var propertyManager = new PropertyManager(serviceProvider.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, property.Name, null);

        // assert
        Assert.Equal(DavStatusCode.Ok, result);
        
        serviceProvider.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithPropertyStoreMissingProperty_ReturnsNotFound()
    {
        // arrange
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
        propertyStore.Setup(s => s.GetPropertiesAsync(storeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PropertyData>());
        
        var propertyManager = new PropertyManager(serviceProvider.Object, propertyStore.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, XName.Get("MyProperty"), null);

        // assert
        Assert.Equal(DavStatusCode.NotFound, result);
        
        serviceProvider.VerifyAll();
        propertyStore.VerifyAll();
    }
    
    [Fact]
    public async Task SetPropertyAsync_WithPropertyStore_ReturnsOk()
    {
        // arrange
        var property = Property.RegisterProperty<TestStoreItem>(
            XName.Get("MyProperty"));
        
        var storeItem = new TestStoreItem(new Uri("/"));
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
        propertyStore.Setup(s => s.GetPropertiesAsync(storeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PropertyData(property.Name, null) });

        propertyStore.Setup(s => s.SetPropertyAsync(
            storeItem,
            property.Name,
            It.IsAny<PropertyMetadata>(),
            null,
            It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        
        var propertyManager = new PropertyManager(serviceProvider.Object, propertyStore.Object);

        // act
        var result = await propertyManager.SetPropertyAsync(storeItem, property.Name, null);

        // assert
        Assert.Equal(DavStatusCode.Ok, result);
        
        serviceProvider.VerifyAll();
        propertyStore.VerifyAll();
    }
}