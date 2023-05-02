using System.Xml.Linq;

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

        var errors = new List<WebDavError>();
        var result = await DeleteItemRecursiveAsync(Collection, Item, errors, cancellationToken);
        if (result)
        {
            Context.SetResult(DavStatusCode.NoContent);
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
}