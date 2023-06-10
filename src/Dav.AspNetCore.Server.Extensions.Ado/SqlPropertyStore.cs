using System.Data;
using System.Data.Common;
using System.Xml;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;

namespace Dav.AspNetCore.Server.Extensions;

public abstract class SqlPropertyStore : IPropertyStore, IDisposable
{
    private static readonly XName Property = XName.Get("Property", "https://github.com/ThuCommix/Dav.AspNetCore.Server");
    
    private readonly SqlPropertyStoreOptions options;
    private readonly Lazy<DbConnection> connection;
    private readonly Dictionary<IStoreItem, Dictionary<XName, PropertyData>> propertyCache = new();
    private readonly Dictionary<IStoreItem, bool> writeLookup = new();

    /// <summary>
    /// Initializes a new <see cref="SqlPropertyStore"/> class.
    /// </summary>
    /// <param name="options">The mssql property store options.</param>
    protected SqlPropertyStore(SqlPropertyStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        
        this.options = options;
        connection = new Lazy<DbConnection>(() => CreateConnection(options.ConnectionString));
    }
    
    /// <summary>
    /// Commits the property store changes async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);
        
        foreach (var entry in propertyCache)
        {
            if (!writeLookup.ContainsKey(entry.Key))
                continue;

            await using var deleteCommand = GetDeleteCommand(connection.Value, entry.Key.Uri.LocalPath);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            foreach (var propertyData in entry.Value)
            {
                if (propertyData.Value.CurrentValue == null)
                    continue;

                string propertyValue;
                if (propertyData.Value.CurrentValue is XElement[] elements)
                {
                    propertyValue = new XElement(
                        Property, 
                        Array.ConvertAll(elements, input => (object)input)).ToString();
                }
                else
                {
                    propertyValue = propertyData.Value.CurrentValue.ToString()!;    
                }

                await using var insertCommand = GetInsertCommand(
                    connection.Value,
                    entry.Key.Uri.LocalPath,
                    propertyData.Key.LocalName,
                    propertyData.Key.NamespaceName,
                    propertyValue);

                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Deletes all properties of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask DeletePropertiesAsync(IStoreItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);

        await using var deleteCommand = GetDeleteCommand(
            connection.Value,
            item.Uri.LocalPath);

        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        
        propertyCache.Remove(item);
    }

    /// <summary>
    /// Copies all properties of the specified item to the destination async.
    /// </summary>
    /// <param name="source">The source store item.</param>
    /// <param name="destination">The destination store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask CopyPropertiesAsync(
        IStoreItem source, 
        IStoreItem destination, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);

        await using var copyCommand = GetCopyCommand(
            connection.Value,
            source.Uri.LocalPath,
            destination.Uri.LocalPath);
        
        await copyCommand.ExecuteNonQueryAsync(cancellationToken);
        
        if (propertyCache.TryGetValue(source, out var propertyMap))
        {
            propertyCache[destination] = propertyMap.ToDictionary(x => x.Key, x => x.Value);
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
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);

        await using var command = GetSelectCommand(connection.Value, item.Uri.LocalPath);

        var propertyDataList = new List<PropertyData>();
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var propertyName = XName.Get(
                reader.GetString("ElementName"),
                reader.GetString("ElementNamespace"));

            object propertyValue = reader.GetString("ElementValue");
            
            try
            {
                var property = XElement.Parse(reader.GetString("ElementValue"));
                propertyValue = property;

                if (property.Name == Property)
                    propertyValue = property.Elements().ToArray();
            }
            catch (XmlException)
            {
            }

            propertyDataList.Add(new PropertyData(propertyName, propertyValue));
        }
        
        propertyCache[item] = new Dictionary<XName, PropertyData>();
        foreach (var propertyData in propertyDataList)
        {
            propertyCache[item][propertyData.Name] = propertyData;
        }

        return propertyDataList;
    }
    
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        connection.Value.Dispose();
    }

    /// <summary>
    /// Creates a db connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The db connection.</returns>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Gets the insert command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="elementName">The element name.</param>
    /// <param name="elementNamespace">The element namespace.</param>
    /// <param name="elementValue">The element value.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetInsertCommand(
        DbConnection connection,
        string uri,
        string elementName,
        string elementNamespace,
        string elementValue);
    
    /// <summary>
    /// Gets the delete command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetDeleteCommand(
        DbConnection connection,
        string uri);
    
    /// <summary>
    /// Gets the select command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetSelectCommand(
        DbConnection connection,
        string uri);

    /// <summary>
    /// Gets the copy command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sourceUri">The source uri.</param>
    /// <param name="destinationUri">The destination uri.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetCopyCommand(
        DbConnection connection,
        string sourceUri,
        string destinationUri);
    
    /// <summary>
    /// Gets the table id.
    /// </summary>
    /// <returns></returns>
    protected string GetTableId()
    {
        return string.IsNullOrWhiteSpace(options.Schema) 
            ? $"[{options.Table}]" 
            : $"[{options.Schema}].[{options.Table}]";
    }
}