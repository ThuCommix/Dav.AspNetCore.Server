using System.Xml.Linq;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server.Handlers;

internal class CopyHandler : RequestHandler
{
    private IPropertyStore? propertyStore;
    
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        if (Item == null)
        {
            Context.SetResult(DavStatusCode.NotFound);
            return;
        }
        
        if (WebDavHeaders.Destination == null)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        if (WebDavHeaders.Depth == Depth.One)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        var depth = WebDavHeaders.Depth ?? Depth.None;

        var destination = WebDavHeaders.Destination;
        if (!string.IsNullOrWhiteSpace(Context.Request.PathBase))
        {
            if (Context.Request.PathBase.HasValue)
                destination = new ResourcePath(destination.ToString().Substring(Context.Request.PathBase.Value.Length));
        }
        
        var overwrite = WebDavHeaders.Overwrite ?? false;
        var destinationParentPath = destination.Parent ?? ResourcePath.Root;

        var destinationCollection = await Store.GetCollectionAsync(destinationParentPath, cancellationToken);
        if (destinationCollection == null)
        {
            Context.SetResult(DavStatusCode.Conflict);
            return;
        }
        
        var destinationItemName = destination.Name;
        var destinationItem = await Store.GetItemAsync(destination, cancellationToken);
        if (destinationItem != null && !overwrite)
        {
            Context.SetResult(DavStatusCode.PreconditionFailed);
            return;
        }

        propertyStore = Context.RequestServices.GetService<IPropertyStore>();

        var errors = new List<WebDavError>();
        var result = await CopyItemRecursiveAsync(
            Item,
            destinationCollection,
            destinationItemName,
            depth == Depth.Infinity,
            errors,
            cancellationToken);

        if (result)
        {
            Context.SetResult(destinationItem != null
                ? DavStatusCode.NoContent
                : DavStatusCode.Ok);
            return;
        }
        
        var responses = new List<XElement>();
        foreach (var davError in errors)
        {
            var href = new XElement(XmlNames.Href, $"{Context.Request.PathBase}{davError.Path}");
            var status = new XElement(XmlNames.Status, $"HTTP/1.1 {(int)davError.StatusCode} {davError.StatusCode.GetDisplayName()}");
            var response = new XElement(XmlNames.Response, href, status);
            
            if (davError.ErrorElement != null)
            {
                var error = new XElement(XmlNames.Error, davError.ErrorElement);
                response.Add(error);
            }

            responses.Add(response);
        }


        var multiStatus = new XElement(XmlNames.MultiStatus, responses);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multiStatus);

        await Context.WriteDocumentAsync(DavStatusCode.MultiStatus, document, cancellationToken);
    }

    private async Task<bool> CopyItemRecursiveAsync(
        IStoreItem item,
        IStoreCollection destination,
        string name,
        bool recursive,
        ICollection<WebDavError> errors,
        CancellationToken cancellationToken = default)
    {
        var destinationPath = ResourcePath.Combine(destination.Path, name);
        var isLocked = await CheckLockedAsync(destinationPath, cancellationToken);
        if (isLocked)
        {
            var tokenSubmitted = await ValidateTokenAsync(destinationPath, cancellationToken);
            if (!tokenSubmitted)
            {
                errors.Add(new WebDavError(destinationPath, DavStatusCode.Locked, new XElement(XmlNames.LockTokenSubmitted)));
                return false;
            }
        }

        var destinationItem = await destination.GetItemAsync(name, cancellationToken);
        if (destinationItem != null)
        {
            var deleteResult = await DeleteItemRecursiveAsync(destination, destinationItem, errors, cancellationToken);
            if (!deleteResult)
                return false;
        }
        
        var result = await item.CopyAsync(destination, name, true, cancellationToken);
        if (result.Item == null)
        {
            errors.Add(new WebDavError(destinationPath, result.StatusCode, null));
            return false;
        }

        if (propertyStore != null)
            await propertyStore.CopyPropertiesAsync(item, result.Item, cancellationToken);

        if (recursive && item is IStoreCollection collection)
        {
            var subItemError = false;
            var items = await collection.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var destinationCopy = result.Item as IStoreCollection;
                if (destinationCopy == null)
                    throw new InvalidOperationException("If the copied item is a collection, the copy result must also be a collection.");
                
                var itemName = subItem.Path.Name;
                var error = await CopyItemRecursiveAsync(subItem, destinationCopy, itemName, recursive, errors, cancellationToken);
                if (!error)
                    subItemError = true;
            }

            if (subItemError)
                return false;
        }

        return true;
    }
}