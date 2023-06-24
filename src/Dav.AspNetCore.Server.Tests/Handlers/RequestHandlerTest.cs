using System.Reflection;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Handlers;
using Dav.AspNetCore.Server.Http;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Handlers;

public class RequestHandlerTest
{
    [Fact]
    public async Task HandleRequestAsync_InitializeProperties()
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        var item = new Mock<IStoreItem>();
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.Equal(httpContext.Object, GetProtectedProperty<HttpContext?>(requestHandler, "Context"));
        Assert.Equal(store.Object, GetProtectedProperty<IStore?>(requestHandler, "Store"));
        Assert.Equal(collection.Object, GetProtectedProperty<IStoreCollection?>(requestHandler, "Collection"));
        Assert.Equal(item.Object, GetProtectedProperty<IStoreItem?>(requestHandler, "Item"));
        Assert.Equal(lockManager.Object, GetProtectedProperty<ILockManager?>(requestHandler, "LockManager"));
        Assert.Equal(options, GetProtectedProperty<WebDavOptions?>(requestHandler, "Options"));
        Assert.NotNull(GetProtectedProperty<WebDavRequestHeaders?>(requestHandler, "WebDavHeaders"));
        Assert.True(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        item.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }

    [Fact]
    public async Task HandleRequestAsync_RootCollectionNotFound_SetsStatusCodeNotFound()
    {
        // arrange
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.SetupSet(context => context.Response.StatusCode = StatusCodes.Status404NotFound);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(new ResourcePath("/"), It.IsAny<CancellationToken>())).ReturnsAsync((IStoreCollection?)null);

        var requestHandler = new RequestHandlerImpl();
        
        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);

        // assert
        Assert.False(requestHandler.OverrideCalled);
        
        httpContext.VerifyAll();
        store.VerifyAll();
    }

    [Fact]
    public async Task HandleRequestAsync_RequestOnRoot_PreventsAdditionalGetItem()
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.True(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }

    [Theory]
    [InlineData(WebDavMethods.Options, false)]
    [InlineData(WebDavMethods.Head, false)]
    [InlineData(WebDavMethods.Get, false)]
    [InlineData(WebDavMethods.Put, true)]
    [InlineData(WebDavMethods.Delete, true)]
    [InlineData(WebDavMethods.PropFind, false)]
    [InlineData(WebDavMethods.PropPatch, true)]
    [InlineData(WebDavMethods.MkCol, true)]
    [InlineData(WebDavMethods.Copy, true)]
    [InlineData(WebDavMethods.Move, true)]
    [InlineData(WebDavMethods.Lock, true)]
    [InlineData(WebDavMethods.Unlock, false)]
    public async Task HandleRequestAsync_ItemLocked_ReturnsLocked(string method, bool locked)
    {
        // arrange
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();

        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        if (locked)
        {
            lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { resourceLock });
        }

        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var responseStream = new MemoryStream();
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(s => s.Request.Method).Returns(method);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (locked)
        {
            httpContext.Setup(s => s.Response.Body).Returns(responseStream);
            httpContext.Setup(s => s.Request.PathBase).Returns(PathString.Empty);
            httpContext.SetupSet(s => s.Response.ContentType = "application/xml; charset=\"utf-8\"");
            httpContext.SetupSet(s => s.Response.ContentLength = It.IsAny<long>());
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status423Locked);
        }
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.Equal(!locked, requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Fact]
    public async Task HandleRequestAsync_WithUnlockToken_Continues()
    {
        // arrange
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();

        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { resourceLock });
        
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);
        
        var headers = new HeaderDictionary
        {
            ["If"] = $"(<{resourceLock.Id.AbsoluteUri}>)"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Put);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.True(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_IfEtag(bool matchEtag)
    {
        // arrange
        const string itemEtag = "A";
        
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        
        var etag = matchEtag ? itemEtag : "B";
        var headers = new HeaderDictionary
        {
            ["If"] = $"([\"{etag}\"])"
        };
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (matchEtag)
        {
            httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        }
        else
        {
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        }
        
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(collection.Object, XmlNames.GetEtag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemEtag));

        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.Equal(matchEtag, requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
    }
    
    [Fact]
    public async Task HandleRequestAsync_IfNegatedEtag_ReturnsPreConditionFailed()
    {
        // arrange
        const string itemEtag = "A";
        
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        
        var headers = new HeaderDictionary
        {
            ["If"] = $"(Not [\"{itemEtag}\"])"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(collection.Object, XmlNames.GetEtag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemEtag));

        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.False(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_IfToken(bool matchToken)
    {
        // arrange
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();

        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { resourceLock });
        
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var token = matchToken ? resourceLock.Id.AbsoluteUri : "B";
        var headers = new HeaderDictionary
        {
            ["If"] = $"(<{token}>)"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (matchToken)
        {
            httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        }
        else
        {
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        }

        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.Equal(matchToken, requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }

    [Fact]
    public async Task HandleRequestAsync_IfNegatedToken_ReturnsPreConditionFailed()
    {
        // arrange
        var requestServices = new ServiceCollection();
        var options = new WebDavOptions();

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();

        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { resourceLock });
        
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);
        
        var headers = new HeaderDictionary
        {
            ["If"] = $"(Not <{resourceLock.Id.AbsoluteUri}>)"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);

        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.False(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Fact]
    public async Task HandleRequestAsync_IfMatchWildcardNonExistingItem_ReturnsPreConditionFailed()
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var headers = new HeaderDictionary
        {
            ["If-Match"] = "*"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync((IStoreItem?)null);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_IfMatch(bool matchEtag)
    {
        // arrange
        const string itemEtag = "A";
        
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);

        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);

        var etag = matchEtag ? itemEtag : "B";
        var headers = new HeaderDictionary
        {
            ["If-Match"] = $"\"{etag}\""
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (matchEtag)
        {
            httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        }
        else
        {
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        }

        var item = new Mock<IStoreItem>();
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(item.Object, XmlNames.GetEtag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemEtag));

        requestServices.AddSingleton(propertyManager.Object);

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
        item.VerifyAll();
    }
    
    [Theory]
    [InlineData(WebDavMethods.Head, true, 0)]
    [InlineData(WebDavMethods.Get, true, 0)]
    [InlineData(WebDavMethods.Put, true, 0)]
    [InlineData(WebDavMethods.Delete, true, 0)]
    [InlineData(WebDavMethods.PropFind, true, 0)]
    [InlineData(WebDavMethods.PropPatch, true, 0)]
    [InlineData(WebDavMethods.MkCol, true, 0)]
    [InlineData(WebDavMethods.Copy, true, 0)]
    [InlineData(WebDavMethods.Move, true, 0)]
    [InlineData(WebDavMethods.Unlock, true, 0)]
    [InlineData(WebDavMethods.Lock, true, 0)]
    [InlineData(WebDavMethods.Head, false, StatusCodes.Status304NotModified)]
    [InlineData(WebDavMethods.Get, false, StatusCodes.Status304NotModified)]
    [InlineData(WebDavMethods.Put, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.Delete, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.PropFind, false, StatusCodes.Status304NotModified)]
    [InlineData(WebDavMethods.PropPatch, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.MkCol, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.Copy, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.Move, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.Unlock, false, StatusCodes.Status412PreconditionFailed)]
    [InlineData(WebDavMethods.Lock, false, StatusCodes.Status412PreconditionFailed)]
    public async Task HandleRequestAsync_IfNoneMatch(string method, bool noneMatchEtag, int statusCode)
    {
        // arrange
        const string itemEtag = "A";
        
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        lockManager.Setup(s => s.GetLocksAsync(It.IsAny<ResourcePath>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceLock>());

        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);

        var etag = noneMatchEtag ? "B" : itemEtag;
        var headers = new HeaderDictionary
        {
            ["If-None-Match"] = $"\"{etag}\""
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        httpContext.Setup(s => s.Request.Method).Returns(method);

        if (!noneMatchEtag)
        {
            httpContext.SetupSet(s => s.Response.StatusCode = statusCode);
        }

        var item = new Mock<IStoreItem>();
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(item.Object, XmlNames.GetEtag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemEtag));

        requestServices.AddSingleton(propertyManager.Object);
                    
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
        item.VerifyAll();
    }
    
    [Fact]
    public async Task HandleRequestAsync_IfNoneMatchWildcardExistingItem_ReturnsPreConditionFailed()
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var headers = new HeaderDictionary
        {
            ["If-None-Match"] = "*"
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Put);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);

        var item = new Mock<IStoreItem>();
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_IfUnmodifiedSince(bool expectedResult)
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);

        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);

        var itemModified = DateTimeOffset.UtcNow;

        var headers = new HeaderDictionary
        {
            ["If-Unmodified-Since"] = itemModified.ToString("R")
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (expectedResult)
        {
            itemModified += TimeSpan.FromDays(1);
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status412PreconditionFailed);
        }
        else
        {
            httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        }

        var item = new Mock<IStoreItem>();
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(item.Object, XmlNames.GetLastModified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemModified.ToString("R")));

        requestServices.AddSingleton(propertyManager.Object);

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_IfModifiedSince(bool expectedResult)
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);

        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);

        var itemModified = DateTimeOffset.UtcNow;

        var headers = new HeaderDictionary
        {
            ["If-Modified-Since"] = itemModified.ToString("R")
        };

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        if (expectedResult)
        {
            itemModified += TimeSpan.FromDays(1);
        }
        else
        {
            httpContext.SetupSet(s => s.Response.StatusCode = StatusCodes.Status304NotModified);
        }

        var item = new Mock<IStoreItem>();
        var propertyManager = new Mock<IPropertyManager>();
        propertyManager.Setup(s => s.GetPropertyAsync(item.Object, XmlNames.GetLastModified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyResult(DavStatusCode.Ok, itemModified.ToString("R")));

        requestServices.AddSingleton(propertyManager.Object);

        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckLockedAsync(bool isLocked)
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        
        var collectionPath = new ResourcePath("/");
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        if (isLocked)
        {
            lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { resourceLock });
        }
        else
        {
            lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceLock>());
        }

        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        
        var collection = new Mock<IStoreCollection>();
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);

        // act
        var result = await requestHandler.CheckLockedAsync(collectionPath);
        
        // assert
        Assert.Equal(isLocked ,result);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidateTokenAsync(bool tokenMatch)
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        
        var collectionPath = new ResourcePath("/");
        var resourceLock = new ResourceLock(
            new Uri($"urn:uuid:{Guid.NewGuid():D}"),
            collectionPath,
            LockType.Exclusive,
            new XElement(XName.Get("owner")),
            true,
            TimeSpan.Zero,
            DateTime.Now);
        
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        lockManager.Setup(s => s.GetLocksAsync(collectionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { resourceLock });

        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);
        
        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var token = resourceLock.Id.AbsoluteUri;
        if (!tokenMatch)
            token = "abc";
        
        var headers = new HeaderDictionary
        {
            ["If"] = $"(Not <{token}>) (<{token}>)"
        };
        
        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/"));
        httpContext.Setup(s => s.Request.Headers).Returns(headers);
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);
        
        var collection = new Mock<IStoreCollection>();
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);

        // act
        var result = await requestHandler.ValidateTokenAsync(collectionPath);
        
        // assert
        Assert.Equal(tokenMatch,result);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleRequestAsync_PropertyStore_CallCommitChangesAsync(bool withPropertyStore)
    {
        // arrange
        var requestServices = new ServiceCollection();

        var options = new WebDavOptions();
        var lockManager = new Mock<ILockManager>(MockBehavior.Strict);
        var propertyManager = new Mock<IPropertyManager>(MockBehavior.Strict);

        Mock<IPropertyStore>? propertyStore = null;
        if (withPropertyStore)
        {
            propertyStore = new Mock<IPropertyStore>(MockBehavior.Strict);
            propertyStore.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
                
            requestServices.AddSingleton(propertyStore.Object);
        }

        requestServices.AddSingleton(options);
        requestServices.AddSingleton(lockManager.Object);
        requestServices.AddSingleton(propertyManager.Object);

        var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
        httpContext.Setup(s => s.Request.Path).Returns(new PathString("/test.txt"));
        httpContext.Setup(s => s.Request.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(s => s.Request.Method).Returns(WebDavMethods.Get);
        httpContext.Setup(s => s.RequestServices).Returns(requestServices.BuildServiceProvider);

        var item = new Mock<IStoreItem>();
        
        var collectionPath = new ResourcePath("/");
        var collection = new Mock<IStoreCollection>();
        collection.Setup(s => s.GetItemAsync("test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(item.Object);
        
        var store = new Mock<IStore>(MockBehavior.Strict);
        store.Setup(s => s.GetItemAsync(collectionPath, It.IsAny<CancellationToken>())).ReturnsAsync(collection.Object);

        var requestHandler = new RequestHandlerImpl();

        // act
        await requestHandler.HandleRequestAsync(httpContext.Object, store.Object);
        
        // assert
        Assert.True(requestHandler.OverrideCalled);
        
        lockManager.VerifyAll();
        httpContext.VerifyAll();
        collection.VerifyAll();
        item.VerifyAll();
        store.VerifyAll();
        propertyManager.VerifyAll();

        if (propertyStore != null)
            propertyStore.VerifyAll();
    }

    private static T GetProtectedProperty<T>(RequestHandler handler, string propertyName)
    {
        var propertyInfo = handler.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (propertyInfo == null)
            Assert.Fail($"{propertyName} was not found.");

        return (T)propertyInfo.GetValue(handler)!;
    }
    
    private class RequestHandlerImpl : RequestHandler
    {
        public bool OverrideCalled { get; private set; }
        protected override Task HandleRequestAsync(CancellationToken cancellationToken = default)
        {
            OverrideCalled = true;
            return Task.CompletedTask;
        }

        public ValueTask<bool> CheckLockedAsync(ResourcePath path) => base.CheckLockedAsync(path);

        public ValueTask<bool> ValidateTokenAsync(ResourcePath path) => base.ValidateTokenAsync(path);
    }
}