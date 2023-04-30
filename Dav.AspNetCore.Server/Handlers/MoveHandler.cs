using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Handlers;

internal class MoveHandler : RequestHandler
{
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

        var result = await MoveItemRecursiveAsync(
            Collection,
            Item, 
            destinationCollection, 
            destinationItemName,
            cancellationToken);

        if (result == null)
        {
            Context.SetResult(destinationItem != null
                ? DavStatusCode.NoContent
                : DavStatusCode.Ok);
            return;
        }
        
        var href = new XElement(XmlNames.Href, $"{Context.Request.PathBase}{result.Uri.AbsolutePath}");
        var status = new XElement(XmlNames.Status, $"HTTP/1.1 {(int)result.StatusCode} {result.StatusCode.GetDisplayName()}");
        var error = new XElement(XmlNames.Error, result.ErrorElement);
        var response = new XElement(XmlNames.Response, href, status, error);
        var multiStatus = new XElement(XmlNames.MultiStatus, response);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multiStatus);

        await Context.WriteDocumentAsync(DavStatusCode.MultiStatus, document, cancellationToken);
    }

    private async Task<UriError?> MoveItemRecursiveAsync(
        IStoreCollection collection,
        IStoreItem item,
        IStoreCollection destination,
        string name,
        CancellationToken cancellationToken = default)
    {
        var destinationUri = UriHelper.Combine(destination.Uri, name);
        var destinationItem = await destination.GetItemAsync(name, cancellationToken);
        if (destinationItem != null)
        {
            var isLocked = await CheckLockedAsync(destinationUri, cancellationToken);
            if (isLocked)
            {
                var tokenSubmitted = await ValidateTokenAsync(item.Uri, cancellationToken);
                if (!tokenSubmitted)
                    return new UriError(item.Uri, DavStatusCode.Locked, new XElement(XmlNames.LockTokenSubmitted));
            }
            
            var deleteResult = await destination.DeleteItemAsync(name, cancellationToken);
            if (deleteResult != DavStatusCode.NoContent)
                return new UriError(destinationUri, deleteResult, null);
        }
        
        var result = await item.CopyAsync(destination, name, true, cancellationToken);
        if (result.Item == null)
            return new UriError(UriHelper.Combine(destination.Uri, name), result.StatusCode, null);

        if (item is IStoreCollection collectionToMove)
        {
            var items = await collectionToMove.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var destinationMove = result.Item as IStoreCollection;
                if (destinationMove == null)
                    throw new InvalidOperationException("If the copied item is a collection, the copy result must also be a collection.");
                
                var subItemName = subItem.Uri.GetRelativeUri(collectionToMove.Uri).LocalPath.Trim('/');
                var error = await MoveItemRecursiveAsync(collectionToMove, subItem, destinationMove, subItemName, cancellationToken);
                if (error != null)
                    return error;
            }
        }
        
        var itemName = item.Uri.GetRelativeUri(collection.Uri).LocalPath.Trim('/');
        var status = await collection.DeleteItemAsync(itemName, cancellationToken);
        if (status != DavStatusCode.NoContent)
            return new UriError(UriHelper.Combine(collection.Uri, itemName), status, null);

        return null;
    }
}