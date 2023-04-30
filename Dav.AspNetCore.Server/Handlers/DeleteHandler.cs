using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Handlers;

internal class DeleteHandler : RequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = Context.Request.Path.ToUri();
        
        if (Item == null)
        {
            Context.SetResult(DavStatusCode.NoContent);
            return;
        }
        
        var activeLocks = await Collection.LockManager.GetLocksAsync(
            requestUri,
            cancellationToken);
        
        var webDavHeaders = Context.Request.GetTypedWebDavHeaders();
        if (activeLocks.Count > 0 && webDavHeaders.If.Count == 0)
        {
            await Context.SendLockedAsync(requestUri, cancellationToken);
            return;
        }

        var result = await DeleteItemRecursiveAsync(Collection, Item, cancellationToken);
        if (result == null)
        {
            Context.SetResult(DavStatusCode.NoContent);
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

    private async Task<UriError?> DeleteItemRecursiveAsync(
        IStoreCollection collection, 
        IStoreItem item, 
        CancellationToken cancellationToken = default)
    {
        if (item is IStoreCollection collectionToDelete)
        {
            var items = await collectionToDelete.GetItemsAsync(cancellationToken);
            foreach (var subItem in items)
            {
                var result = await DeleteItemRecursiveAsync(collectionToDelete, subItem, cancellationToken);
                if (result != null)
                    return result;
            }
        }

        var isLocked = await CheckLockedAsync(item.Uri, cancellationToken);
        if (isLocked)
        {
            var tokenSubmitted = await ValidateTokenAsync(item.Uri, cancellationToken);
            if (!tokenSubmitted)
                return new UriError(item.Uri, DavStatusCode.Locked, new XElement(XmlNames.LockTokenSubmitted));
        }
        
        var itemName = item.Uri.GetRelativeUri(collection.Uri).LocalPath.TrimStart('/');
        var status = await collection.DeleteItemAsync(itemName, cancellationToken);
        return status != DavStatusCode.NoContent 
            ? new UriError(item.Uri, status, null) 
            : null;
    }
}