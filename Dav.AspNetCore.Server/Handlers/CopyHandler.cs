using System.Xml.Linq;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Handlers;

internal class CopyHandler : RequestHandler
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

        if (WebDavHeaders.Depth == Depth.One)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        var depth = WebDavHeaders.Depth ?? Depth.None;

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
        
        var destinationItemName = WebDavHeaders.Destination.GetRelativeUri(destinationParentUri).LocalPath.Trim('/');
        var destinationItem = await Store.GetItemAsync(WebDavHeaders.Destination, cancellationToken);
        if (destinationItem != null && !overwrite)
        {
            Context.SetResult(DavStatusCode.PreconditionFailed);
            return;
        }

        var result = await CopyItemRecursiveAsync(
            Item,
            destinationCollection,
            destinationItemName,
            depth == Depth.Infinity,
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

    private async Task<UriError?> CopyItemRecursiveAsync(
        IStoreItem item,
        IStoreCollection destination,
        string name,
        bool recursive,
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
            return new UriError(destinationUri, result.StatusCode, null);

        if (recursive && item is IStoreCollection collection)
        {
            var items = await collection.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var destinationCopy = result.Item as IStoreCollection;
                if (destinationCopy == null)
                    throw new InvalidOperationException("If the copied item is a collection, the copy result must also be a collection.");
                
                var itemName = subItem.Uri.GetRelativeUri(collection.Uri).LocalPath.TrimStart('/');
                var error = await CopyItemRecursiveAsync(subItem, destinationCopy, itemName, recursive, cancellationToken);
                if (error != null)
                    return error;
            }
        }

        return null;
    }
}