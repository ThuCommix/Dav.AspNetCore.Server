using System.Xml.Linq;
using Dav.AspNetCore.Server.Http.Headers;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Handlers;

internal class PropFindHandler : RequestHandler
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

        var items = new List<IStoreItem>();
        if (Item is IStoreCollection collection)
        {
            var headers = Context.Request.GetTypedWebDavHeaders();
            if (headers.Depth == Depth.Infinity && Options.DisallowInfinityDepth)
            {
                Context.SetResult(DavStatusCode.Forbidden);
                return;
            }

            var depth = headers.Depth ?? (Options.DisallowInfinityDepth ? Depth.One : Depth.Infinity);
            await AddItemsRecursive(collection, depth, 0, items, cancellationToken);
        }
        else
        {
            items.Add(Item);
        }

        var requestedProperties = await GetRequestedPropertiesAsync(Context, cancellationToken);
        
        var multiStatus = new XElement(XmlNames.MultiStatus);
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multiStatus);
        
        foreach (var item in items)
        {
            var response = new XElement(XmlNames.Response);
            response.Add(new XElement(XmlNames.Href, $"{Context.Request.PathBase}{item.Path}"));

            var propertyValues = await GetPropertiesAsync(
                item,
                requestedProperties,
                cancellationToken);

            foreach (var statusGrouping in propertyValues
                         .GroupBy(x => x.Value.StatusCode))
            {
                var propStat = new XElement(XmlNames.PropertyStatus);
                var prop = new XElement(XmlNames.Property);   
                
                foreach (var property in statusGrouping)
                {
                    prop.Add(new XElement(property.Key, property.Value.Value));
                }

                propStat.Add(prop);
                propStat.Add(new XElement(XmlNames.Status, $"HTTP/1.1 {(int)statusGrouping.Key} {statusGrouping.Key.GetDisplayName()}"));
                
                response.Add(propStat);
            }
            
            multiStatus.Add(response);
        }

        await Context.WriteDocumentAsync(DavStatusCode.MultiStatus, document, cancellationToken);
    }

    private static async Task AddItemsRecursive(
        IStoreCollection collection, 
        Depth depth,
        int iteration,
        ICollection<IStoreItem> results,
        CancellationToken cancellationToken = default)
    {
        results.Add(collection);
        
        if (iteration >= (int)depth && depth != Depth.Infinity)
            return;
        
        var items = await collection.GetItemsAsync(cancellationToken);
        var collections = items.OfType<IStoreCollection>().ToList();
        foreach (var subCollection in collections)
        {
            await AddItemsRecursive(subCollection, depth, iteration + 1, results, cancellationToken);
        }
        
        foreach (var item in items.Except(collections))
        {
            results.Add(item);
        }
    }

    private async Task<Dictionary<XName, PropertyResult>> GetPropertiesAsync(
        IStoreItem item,
        PropFindRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OnlyPropertyNames)
        {
            var propertyNames = await PropertyManager.GetPropertyNamesAsync(item, cancellationToken);
            return propertyNames.ToDictionary(x => x, _ => new PropertyResult(DavStatusCode.Ok));
        }

        var propertyValues = new Dictionary<XName, PropertyResult>();
        var properties = new List<XName>();

        // add all non-expensive properties
        if (request.AllProperties) 
        {
            var propertyNames = await PropertyManager.GetPropertyNamesAsync(item, cancellationToken);
            foreach (var propertyName in propertyNames)
            {
                var propertyMetadata = PropertyManager.GetPropertyMetadata(item, propertyName);
                if (propertyMetadata == null || !propertyMetadata.Expensive)
                    properties.Add(propertyName);
            }
        }

        // this will also contain properties which are included explicitly
        foreach (var propertyName in request.Properties)
        {
            if (properties.All(x => x != propertyName))
                properties.Add(propertyName);
        }
        
        foreach (var propertyName in properties)
        {
            try
            {
                var propertyValue = await PropertyManager.GetPropertyAsync(item, propertyName, cancellationToken);
                propertyValues.Add(propertyName, propertyValue);
            }
            catch
            {
                propertyValues.Add(propertyName, new PropertyResult(DavStatusCode.InternalServerError));
            }
        }

        return propertyValues;
    }

    private async Task<PropFindRequest> GetRequestedPropertiesAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var document = await context.ReadDocumentAsync(cancellationToken);
        if (document == null)
        {
            return new PropFindRequest(
                Array.Empty<XName>(),
                false,
                true);
        }

        var propfind = document.Element(XmlNames.PropertyFind);
        if (propfind == null)
        {
            return new PropFindRequest(
                Array.Empty<XName>(),
                false,
                true);
        }
        
        var properties = propfind.Element(XmlNames.Property)?.Elements().ToList() ?? new List<XElement>();
        var allProp = propfind.Element(XmlNames.AllProperties);
        var propNames = propfind.Element(XmlNames.PropertyName);

        var include = propfind.Element(XmlNames.Include);
        if (include != null)
        {
            properties.AddRange(include.Elements());
        }

        return new PropFindRequest(
            properties.Select(x => x.Name).ToList(),
            propNames != null,
            allProp != null);
    }

    private record PropFindRequest(
        IEnumerable<XName> Properties,
        bool OnlyPropertyNames,
        bool AllProperties);
}