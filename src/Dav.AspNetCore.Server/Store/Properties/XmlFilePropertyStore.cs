using System.Xml;
using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public class XmlFilePropertyStore : IPropertyStore
{
    private const string Namespace = "https://github.com/ThuCommix/Dav.AspNetCore.Server";
    private static readonly XName PropertyStore = XName.Get("PropertyStore", Namespace);
    private static readonly XName Property = XName.Get("Property", Namespace);
    
    private readonly XmlFilePropertyStoreOptions options;
    private readonly Dictionary<IStoreItem, Dictionary<XName, PropertyData>> propertyCache = new();
    private readonly Dictionary<IStoreItem, bool> writeLookup = new();

    /// <summary>
    /// Initializes a new <see cref="XmlFilePropertyStore"/> class.
    /// </summary>
    /// <param name="options">The xml file property store options.</param>
    public XmlFilePropertyStore(XmlFilePropertyStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        this.options = options;
    }

    /// <summary>
    /// Commits the property store changes async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask CommitChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in propertyCache)
        {
            if (!writeLookup.ContainsKey(entry.Key))
                continue;
            
            var propertyStore = new XElement(PropertyStore);
            var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
                propertyStore);

            foreach (var propertyData in entry.Value)
            {
                propertyStore.Add(new XElement(Property, new XElement(propertyData.Value.Name, propertyData.Value.CurrentValue)));
            }
            
            var xmlFilePath = Path.Combine(options.RootPath, entry.Key.Uri.LocalPath.TrimStart('/') + ".xml");
            var fileInfo = new FileInfo(xmlFilePath);
            if (fileInfo.Directory?.Exists == false)
                fileInfo.Directory.Create();
            
            await using var fileStream = File.OpenWrite(xmlFilePath);
            await document.SaveAsync(fileStream, SaveOptions.None, cancellationToken);
        }
    }

    /// <summary>
    /// Sets a property of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="propertyMetadata">The property metadata.</param>
    /// <param name="isRegistered">A value indicating whether the property is registered.</param>
    /// <param name="propertyValue">The property value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask<bool> SetPropertyAsync(
        IStoreItem item, 
        XName propertyName, 
        PropertyMetadata propertyMetadata,
        bool isRegistered,
        object? propertyValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));
        
        if (!propertyCache.TryGetValue(item, out var propertyMap))
        {
            await GetPropertiesAsync(item, cancellationToken);
            propertyMap = propertyCache[item];
        }

        var propertyExists = propertyMap.ContainsKey(propertyName);
        if (propertyExists || options.AcceptCustomProperties || isRegistered)
        {
            propertyMap[propertyName] = new PropertyData(propertyName, propertyValue);
            writeLookup[item] = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the properties of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of stored properties.</returns>
    public async ValueTask<IReadOnlyCollection<PropertyData>> GetPropertiesAsync(
        IStoreItem item, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        
        if (propertyCache.TryGetValue(item, out var propertyMap))
            return propertyMap.Values;
        
        var xmlFilePath = Path.Combine(options.RootPath, item.Uri.LocalPath.TrimStart('/') + ".xml");
        if (!File.Exists(xmlFilePath))
        {
            propertyCache[item] = new Dictionary<XName, PropertyData>();
            return Array.Empty<PropertyData>();
        }

        var fileStream = File.OpenRead(xmlFilePath);
        var propertyDataList = new List<PropertyData>();
        
        try
        {
            var document = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken);
            var propertyStore = document.Element(PropertyStore);
            var properties = propertyStore?.Elements(Property).Select(x => x.Elements().First());
            if (properties == null)
            {
                propertyCache[item] = new Dictionary<XName, PropertyData>();
                return propertyDataList;
            }

            foreach (var property in properties)
            {
                object? propertyValue = null;
                if (property.FirstNode != null)
                {
                    propertyValue = property.FirstNode.NodeType switch
                    {
                        XmlNodeType.Text => property.Value,
                        XmlNodeType.Element => property.Elements().ToArray(),
                        _ => propertyValue
                    };
                }

                var propertyData = new PropertyData(
                    property.Name,
                    propertyValue);
                
                propertyDataList.Add(propertyData);
            }
        }
        catch
        {
            propertyCache[item] = new Dictionary<XName, PropertyData>();
            return propertyDataList;
        }
        finally
        {
            await fileStream.DisposeAsync();
        }
        
        propertyCache[item] = new Dictionary<XName, PropertyData>();
        foreach (var propertyData in propertyDataList)
        {
            propertyCache[item][propertyData.Name] = propertyData;
        }
        
        return propertyDataList;
    }
}