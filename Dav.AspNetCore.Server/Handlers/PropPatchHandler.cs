using System.Xml;
using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Handlers;

internal class PropPatchHandler : RequestHandler
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

        var requestDocument = await Context.ReadDocumentAsync(cancellationToken);
        if (requestDocument == null)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        var propertyUpdate = requestDocument.Element(XmlNames.PropertyUpdate);
        if (propertyUpdate == null)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }

        var results = new Dictionary<XName, DavStatusCode>();
        
        var sets = propertyUpdate
            .Element(XmlNames.Set)?.Element(XmlNames.Property)?.Elements();
        
        if (sets != null)
        {
            foreach (var element in sets)
            {
                object? propertyValue = null;
                if (element.FirstNode != null)
                {
                    propertyValue = element.FirstNode.NodeType switch
                    {
                        XmlNodeType.Text => element.Value,
                        XmlNodeType.Element => element.Elements(),
                        _ => propertyValue
                    };
                }
                
                var result = await Item.PropertyManager.SetPropertyAsync(
                    Context,
                    Item,
                    element.Name,
                    propertyValue,
                    cancellationToken);
                
                results.Add(element.Name, result);
            }
        }
        
        var removes = propertyUpdate
            .Element(XmlNames.Remove)?.Element(XmlNames.Property)?.Elements();

        if (removes != null)
        {
            foreach (var element in removes)
            {
                var result = await Item.PropertyManager.SetPropertyAsync(
                    Context,
                    Item,
                    element.Name,
                    null,
                    cancellationToken);
                
                results.Add(element.Name, result);
            }
        }

        var href = new XElement(XmlNames.Href, $"{Context.Request.PathBase}{Item.Uri.AbsolutePath}");
        var response = new XElement(XmlNames.Response, href);
        var multiStatus = new XElement(XmlNames.MultiStatus, response);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multiStatus);

        foreach (var statusGroup in results.GroupBy(x => x.Value))
        {
            var status = new XElement(XmlNames.Status, $"HTTP/1.1 {(int)statusGroup.Key} {statusGroup.Key.GetDisplayName()}");
            var prop = new XElement(XmlNames.Property);
            var propStat = new XElement(XmlNames.PropertyStatus, prop, status);

            foreach (var propertyName in statusGroup)
            {
                prop.Add(new XElement(propertyName.Key));
            }
            
            response.Add(propStat);
        }
        
        await Context.WriteDocumentAsync(DavStatusCode.MultiStatus, document, cancellationToken);
    }
}