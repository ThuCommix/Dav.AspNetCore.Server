using System.Xml.Linq;
using Dav.AspNetCore.Server.Http;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server.Handlers;

internal abstract class RequestHandler : IRequestHandler
{
    private readonly Dictionary<Uri, IReadOnlyCollection<ResourceLock>> lockCache = new();
    private IPropertyStore? propertyStore;

    private static readonly List<string> SkipLockValidation = new()
    {
        WebDavMethods.Options,
        WebDavMethods.Head,
        WebDavMethods.Get,
        WebDavMethods.PropFind,
        WebDavMethods.Unlock
    };

    /// <summary>
    /// Gets the http context.
    /// </summary>
    protected HttpContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the store.
    /// </summary>
    protected IStore Store { get; private set; } = null!;

    /// <summary>
    /// Gets the parent collection.
    /// </summary>
    protected IStoreCollection Collection { get; private set; } = null!;

    /// <summary>
    /// Gets the requested item.
    /// </summary>
    protected IStoreItem? Item { get; private set; }

    /// <summary>
    /// Gets the lock manager.
    /// </summary>
    protected ILockManager LockManager { get; private set; } = null!;

    /// <summary>
    /// Gets the property manager.
    /// </summary>
    protected IPropertyManager PropertyManager { get; private set; } = null!;

    /// <summary>
    /// Gets the web dav request headers.
    /// </summary>
    protected WebDavRequestHeaders WebDavHeaders { get; private set; } = null!;

    /// <summary>
    /// Gets the web dav options.
    /// </summary>
    protected WebDavOptions Options { get; private set; } = null!;
    
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="store">The resource store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task HandleRequestAsync(
        HttpContext context, 
        IStore store, 
        CancellationToken cancellationToken = default)
    {
        Context = context;
        Store = store;
        
        var requestUri = context.Request.Path.ToUri();
        var parentUri = requestUri.GetParent();
        
        var collection = await store.GetCollectionAsync(parentUri, cancellationToken);
        if (collection == null)
        {
            context.SetResult(DavStatusCode.NotFound);
            return;
        }

        Collection = collection;
        Options = Context.RequestServices.GetRequiredService<WebDavOptions>();
        PropertyManager = context.RequestServices.GetRequiredService<IPropertyManager>();
        LockManager = Context.RequestServices.GetRequiredService<ILockManager>();
        WebDavHeaders = context.Request.GetTypedWebDavHeaders();

        propertyStore = Context.RequestServices.GetService<IPropertyStore>();

        if (requestUri == parentUri)
        {
            Item = collection;
        }
        else
        {
            var itemName = requestUri.GetRelativeUri(Collection.Uri).LocalPath.Trim('/');
            Item = await collection.GetItemAsync(itemName, cancellationToken);
        }

        var precondition = await ValidatePreConditionsAsync(cancellationToken);
        if (!precondition)
            return;

        if (!SkipLockValidation.Contains(Context.Request.Method))
        {
            var locked = await CheckLockedAsync(requestUri, cancellationToken);
            if (locked)
            {
                var tokenSubmitted = await ValidateTokenAsync(requestUri, cancellationToken);
                if (!tokenSubmitted)
                {
                    await Context.SendLockedAsync(requestUri, cancellationToken);
                    return;
                }
            }   
        }

        await HandleRequestAsync(cancellationToken);
        
        if (propertyStore != null)
            await propertyStore.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if the given uri is locked async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the uri is locked, otherwise false.</returns>
    protected virtual async ValueTask<bool> CheckLockedAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await EnsureLocksAsync(uri, cancellationToken);
        return lockCache[uri].Count > 0;
    }

    /// <summary>
    /// Validates the submitted token against the specified uri async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the token was submitted and was matched against the uri or the uri was not locked, otherwise false.</returns>
    protected async ValueTask<bool> ValidateTokenAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        await EnsureLocksAsync(uri, cancellationToken);
        
        var activeLocks = lockCache[uri];
        if (activeLocks.Count > 0)
        {
            if (WebDavHeaders.If.Count == 0)
                return false;
            
            IEnumerable<IfHeaderValueCondition> Matches(Uri? matchUri)
            {
                foreach (var condition in WebDavHeaders.If)
                {
                    if (condition.Uri == matchUri)
                        yield return condition;
                }
            }
            
            // check if we can generally unlock one of the locks
            var conditions = new List<IfHeaderValueCondition>(Matches(uri));
            conditions.AddRange(Matches(null));
            
            var unlock = activeLocks
                .Any(x => conditions
                    .Any(y => y.Tokens
                        .Any(z => z.Value == x.Id.AbsoluteUri)));

            return unlock;
        }

        return true;
    }
    
    protected async Task<bool> DeleteItemRecursiveAsync(
        IStoreCollection collection, 
        IStoreItem item,
        ICollection<WebDavError> errors,
        CancellationToken cancellationToken = default)
    {
        var error = false;
        if (item is IStoreCollection collectionToDelete)
        {
            var items = await collectionToDelete.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var result = await DeleteItemRecursiveAsync(collectionToDelete, subItem, errors, cancellationToken);
                if (!result)
                    error = true;
            }
        }

        var isLocked = await CheckLockedAsync(item.Uri, cancellationToken);
        if (isLocked)
        {
            var tokenSubmitted = await ValidateTokenAsync(item.Uri, cancellationToken);
            if (!tokenSubmitted)
            {
                error = true;
                errors.Add(new WebDavError(item.Uri, DavStatusCode.Locked, new XElement(XmlNames.LockTokenSubmitted)));
            }
        }

        if (error)
            return false;
        
        var itemName = item.Uri.GetRelativeUri(collection.Uri).LocalPath.TrimStart('/');
        var status = await collection.DeleteItemAsync(itemName, cancellationToken);
        if (status != DavStatusCode.NoContent)
        {
            errors.Add(new WebDavError(item.Uri, status, null));
            return false;
        }

        if (propertyStore != null)
            await propertyStore.DeletePropertiesAsync(item, cancellationToken);

        return true;
    }

    private async ValueTask<bool> ValidatePreConditionsAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = Context.Request.Path.ToUri();
        var items = new Dictionary<Uri, IStoreItem?>();

        if (WebDavHeaders.If.Count > 0)
        {
            var groups = WebDavHeaders.If
                .GroupBy(x => x.Uri != null ? x.Uri : requestUri);

            async ValueTask<bool> ValidateConditionAsync(IfHeaderValueCondition condition)
            {
                var resourceUri = condition.Uri ?? requestUri;
                if (!items.ContainsKey(resourceUri))
                {
                    var parentUri = resourceUri.GetParent();
                    var collection = await Store.GetCollectionAsync(parentUri, cancellationToken);
                    if (collection != null)
                    {
                        var itemName = resourceUri.GetRelativeUri(collection.Uri).LocalPath.Trim('/');
                        if (string.IsNullOrWhiteSpace(itemName))
                        {
                            items[resourceUri] = collection;
                        }
                        else
                        {
                            items[resourceUri] = await collection.GetItemAsync(itemName, cancellationToken);
                        }   
                    }
                }

                var item = items[resourceUri];
                if (condition.Tags.Length > 0)
                {
                    string? itemEtag = null;
                    if (item != null)
                    {
                        var etagResult = await PropertyManager.GetPropertyAsync(item, XmlNames.GetEtag, cancellationToken);
                        if (etagResult.IsSuccess)
                            itemEtag = (string?)etagResult.Value;
                    }

                    foreach (var tag in condition.Tags)
                    {
                        var conditionResult = tag.Negate
                            ? itemEtag != tag.Value
                            : itemEtag == tag.Value;

                        if (!conditionResult)
                            return false;
                    }
                }

                if (condition.Tokens.Length > 0)
                {
                    await EnsureLocksAsync(resourceUri, cancellationToken);

                    var activeLocks = lockCache[resourceUri];
                    foreach (var stateToken in condition.Tokens)
                    {
                        var conditionResult = stateToken.Negate
                            ? activeLocks.All(x => x.Id.AbsoluteUri != stateToken.Value)
                            : activeLocks.Any(x => x.Id.AbsoluteUri == stateToken.Value);

                        if (!conditionResult)
                            return false;
                    }
                }

                return true;
            }

            foreach (var conditions in groups)
            {
                var anyMatch = false;
                foreach (var condition in conditions)
                {
                    var result = await ValidateConditionAsync(condition);
                    if (result)
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (!anyMatch)
                {
                    Context.SetResult(DavStatusCode.PreconditionFailed);
                    return false;
                }
            }
        }

        var requestHeaders = Context.Request.GetTypedHeaders();
        if (requestHeaders.IfMatch?.Count > 0)
        {
            if (requestHeaders.IfMatch.Count == 1 && requestHeaders.IfMatch[0].Tag == "*")
            {
                if (Item == null)
                {
                    Context.SetResult(DavStatusCode.PreconditionFailed);
                    return false;
                }
            }
            else
            {
                string? itemEtag = null;
                if (Item != null)
                {
                    var etagResult = await PropertyManager.GetPropertyAsync(Item, XmlNames.GetEtag, cancellationToken);
                    if (etagResult.IsSuccess)
                        itemEtag = (string?)etagResult.Value;
                }

                if (requestHeaders.IfMatch.All(x => x.Tag != $"\"{itemEtag}\""))
                {
                    Context.SetResult(DavStatusCode.PreconditionFailed);
                    return false;
                }
            }
        }

        if (requestHeaders.IfNoneMatch?.Count > 0)
        {
            var statusCode = Context.Request.Method is WebDavMethods.Get or WebDavMethods.Head or WebDavMethods.PropFind
                ? DavStatusCode.NotModified
                : DavStatusCode.PreconditionFailed;
            
            if (requestHeaders.IfNoneMatch.Count == 1 && requestHeaders.IfNoneMatch[0].Tag == "*")
            {
                if (Item != null)
                {
                    Context.SetResult(statusCode);
                    return false;
                }
            }
            else
            {
                string? itemEtag = null;
                if (Item != null)
                {
                    var etagResult = await PropertyManager.GetPropertyAsync(Item, XmlNames.GetEtag, cancellationToken);
                    if (etagResult.IsSuccess)
                        itemEtag = (string?)etagResult.Value;
                }

                if (requestHeaders.IfNoneMatch.Any(x => x.Tag == $"\"{itemEtag}\""))
                {
                    Context.SetResult(statusCode);
                    return false;
                }  
            }
        }
        else if (requestHeaders.IfUnmodifiedSince != null)
        {
            if (Item != null)
            {
                var modifiedResult = await PropertyManager.GetPropertyAsync(Item, XmlNames.GetLastModified, cancellationToken);
                if (modifiedResult.IsSuccess && DateTimeOffset.Parse(modifiedResult.Value!.ToString()!) > requestHeaders.IfUnmodifiedSince)
                {
                    Context.SetResult(DavStatusCode.PreconditionFailed);
                    return false;
                }
            }
        }

        if ((Context.Request.Method == WebDavMethods.Get ||
            Context.Request.Method == WebDavMethods.Head) &&
            requestHeaders.IfModifiedSince != null)
        {
            if (Item != null)
            {
                var modifiedResult = await PropertyManager.GetPropertyAsync(Item, XmlNames.GetLastModified, cancellationToken);
                if (modifiedResult.IsSuccess && DateTimeOffset.Parse(modifiedResult.Value!.ToString()!) <= requestHeaders.IfModifiedSince)
                {
                    Context.SetResult(DavStatusCode.NotModified);
                    return false;
                }
            }
        }

        return true;
    }

    private async ValueTask EnsureLocksAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (!lockCache.ContainsKey(uri))
            lockCache[uri] = await LockManager.GetLocksAsync(uri, cancellationToken);
    }

    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected abstract Task HandleRequestAsync(CancellationToken cancellationToken = default);
}