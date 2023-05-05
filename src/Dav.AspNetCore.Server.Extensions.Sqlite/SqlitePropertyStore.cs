using System.Data;
using System.Xml;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.Data.Sqlite;

namespace Dav.AspNetCore.Server.Extensions.Sqlite;

public class SqlitePropertyStore : IPropertyStore, IDisposable
{
    private static readonly XName Property = XName.Get("Property", "https://github.com/ThuCommix/Dav.AspNetCore.Server");
    
    private readonly SqlitePropertyStoreOptions options;
    private readonly SqliteConnection connection;
    private readonly Dictionary<IStoreItem, Dictionary<XName, PropertyData>> propertyCache = new();
    private readonly Dictionary<IStoreItem, bool> writeLookup = new();

    /// <summary>
    /// Initializes a new <see cref="SqlitePropertyStore"/> class.
    /// </summary>
    /// <param name="options">The sqlite property store options.</param>
    public SqlitePropertyStore(SqlitePropertyStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        
        connection = new SqliteConnection(options.ConnectionString);
        this.options = options;
    }
    
    /// <summary>
    /// Commits the property store changes async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        foreach (var entry in propertyCache)
        {
            if (!writeLookup.ContainsKey(entry.Key))
                continue;

            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM dav_aspnetcore_server_property WHERE Uri = @Uri";
            deleteCommand.Parameters.Add(new SqliteParameter("@Uri", entry.Key.Uri.LocalPath));

            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            foreach (var propertyData in entry.Value)
            {
                if (propertyData.Value.CurrentValue == null)
                    continue;
                
                await using var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO dav_aspnetcore_server_property VALUES (@Uri, @ElementName, @ElementNamespace, @ElementValue)";
                insertCommand.Parameters.Add(new SqliteParameter("@Uri", entry.Key.Uri.LocalPath));
                insertCommand.Parameters.Add(new SqliteParameter("@ElementName", propertyData.Key.LocalName));
                insertCommand.Parameters.Add(new SqliteParameter("@ElementNamespace", propertyData.Key.NamespaceName));

                if (propertyData.Value.CurrentValue is XElement[] elements)
                {
                    insertCommand.Parameters.Add(new SqliteParameter("@ElementValue",
                        new XElement(Property, Array.ConvertAll(elements, input => (object)input)).ToString()));
                }
                else
                {
                    insertCommand.Parameters.Add(new SqliteParameter("@ElementValue", propertyData.Value.CurrentValue.ToString()));    
                }

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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        await using var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM dav_aspnetcore_server_property WHERE Uri = @Uri";
        deleteCommand.Parameters.Add(new SqliteParameter("@Uri", item.Uri.LocalPath));

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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        await using var copyCommand = connection.CreateCommand();
        copyCommand.CommandText = @"INSERT INTO dav_aspnetcore_server_property
SELECT
@DestinationUri,
ElementName,
ElementNamespace,
ElementValue
FROM dav_aspnetcore_server_property WHERE Uri = @SourceUri";
        
        copyCommand.Parameters.Add(new SqliteParameter("@SourceUri", source.Uri.LocalPath));
        copyCommand.Parameters.Add(new SqliteParameter("@DestinationUri", destination.Uri.LocalPath));

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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM dav_aspnetcore_server_property WHERE Uri = @Uri";
        command.Parameters.Add(new SqliteParameter("@Uri", item.Uri.LocalPath));

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
        connection.Dispose();
    }
}