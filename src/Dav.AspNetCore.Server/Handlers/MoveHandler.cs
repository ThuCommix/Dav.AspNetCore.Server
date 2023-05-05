using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server.Handlers;

internal class MoveHandler : RequestHandler
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

        var destination = WebDavHeaders.Destination;
        if (!string.IsNullOrWhiteSpace(Context.Request.PathBase))
        {
            destination = new Uri(destination.LocalPath.Substring(Context.Request.PathBase.Value.Length));
        }
        
        var overwrite = WebDavHeaders.Overwrite ?? false;
        var destinationParentUri = destination.GetParent();

        var destinationCollection = await Store.GetCollectionAsync(destinationParentUri, cancellationToken);
        if (destinationCollection == null)
        {
            Context.SetResult(DavStatusCode.Conflict);
            return;
        }

        var destinationItemName = destination.GetRelativeUri(destinationParentUri).LocalPath.Trim('/');
        var destinationItem = await Store.GetItemAsync(destination, cancellationToken);
        if (destinationItem != null && !overwrite)
        {
            Context.SetResult(DavStatusCode.PreconditionFailed);
            return;
        }

        propertyStore = Context.RequestServices.GetService<IPropertyStore>();

        var errors = new List<WebDavError>();
        var result = await MoveItemRecursiveAsync(
            Collection,
            Item, 
            destinationCollection, 
            destinationItemName,
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
            var href = new XElement(XmlNames.Href, $"{Context.Request.PathBase}{davError.Uri.AbsolutePath}");
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

    private async Task<bool> MoveItemRecursiveAsync(
        IStoreCollection collection,
        IStoreItem item,
        IStoreCollection destination,
        string name,
        ICollection<WebDavError> errors,
        CancellationToken cancellationToken = default)
    {
        var destinationUri = UriHelper.Combine(destination.Uri, name);
        var isLocked = await CheckLockedAsync(destinationUri, cancellationToken);
        if (isLocked)
        {
            var tokenSubmitted = await ValidateTokenAsync(destinationUri, cancellationToken);
            if (!tokenSubmitted)
            {
                errors.Add(new WebDavError(destinationUri, DavStatusCode.Locked, new XElement(XmlNames.LockTokenSubmitted)));
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
            errors.Add(new WebDavError(destinationUri, result.StatusCode, null));
            return false;
        }

        if (propertyStore != null)
            await propertyStore.CopyPropertiesAsync(item, result.Item, cancellationToken);

        if (item is IStoreCollection collectionToMove)
        {
            var subItemError = false;
            var items = await collectionToMove.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var destinationMove = result.Item as IStoreCollection;
                if (destinationMove == null)
                    throw new InvalidOperationException("If the copied item is a collection, the copy result must also be a collection.");
                
                var subItemName = subItem.Uri.GetRelativeUri(collectionToMove.Uri).LocalPath.Trim('/');
                var error = await MoveItemRecursiveAsync(collectionToMove, subItem, destinationMove, subItemName, errors, cancellationToken);
                if (!error)
                    subItemError = true;
            }
            
            if (subItemError)
                return false;
        }
        
        var itemName = item.Uri.GetRelativeUri(collection.Uri).LocalPath.Trim('/');
        var status = await collection.DeleteItemAsync(itemName, cancellationToken);
        if (status != DavStatusCode.NoContent)
        {
            errors.Add(new WebDavError(UriHelper.Combine(collection.Uri, itemName), status, null));
            return false;
        }

        if (propertyStore != null)
            await propertyStore.DeletePropertiesAsync(item, cancellationToken);

        return true;
    }
}