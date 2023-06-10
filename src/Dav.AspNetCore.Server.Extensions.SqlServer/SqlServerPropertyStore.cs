using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Dav.AspNetCore.Server.Extensions.SqlServer;

public class SqlServerPropertyStore : SqlPropertyStore
{
    /// <summary>
    /// Initializes a new <see cref="SqlServerPropertyStore"/> class.
    /// </summary>
    /// <param name="options">The sql property store options.</param>
    public SqlServerPropertyStore(SqlPropertyStoreOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Creates a db connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The db connection.</returns>
    protected override DbConnection CreateConnection(string connectionString) 
        => new SqlConnection(connectionString);

    /// <summary>
    /// Gets the insert command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="elementName">The element name.</param>
    /// <param name="elementNamespace">The element namespace.</param>
    /// <param name="elementValue">The element value.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetInsertCommand(
        DbConnection connection, 
        string uri, 
        string elementName, 
        string elementNamespace, 
        string elementValue)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = $"INSERT INTO {GetTableId()} VALUES (@Uri, @ElementName, @ElementNamespace, @ElementValue)";
        insertCommand.Parameters.Add(new SqlParameter("@Uri", uri));
        insertCommand.Parameters.Add(new SqlParameter("@ElementName", elementName));
        insertCommand.Parameters.Add(new SqlParameter("@ElementNamespace", elementNamespace));
        insertCommand.Parameters.Add(new SqlParameter("@ElementValue", elementValue));

        return insertCommand;
    }

    /// <summary>
    /// Gets the delete command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetDeleteCommand(DbConnection connection, string uri)
    {
        var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = $"DELETE FROM {GetTableId()} WHERE Uri = @Uri";
        deleteCommand.Parameters.Add(new SqlParameter("@Uri",uri));

        return deleteCommand;
    }

    /// <summary>
    /// Gets the select command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetSelectCommand(DbConnection connection, string uri)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {GetTableId()} WHERE Uri = @Uri";
        command.Parameters.Add(new SqlParameter("@Uri", uri));

        return command;
    }

    /// <summary>
    /// Gets the copy command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sourceUri">The source uri.</param>
    /// <param name="destinationUri">The destination uri.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetCopyCommand(
        DbConnection connection, 
        string sourceUri,
        string destinationUri)
    {
        var copyCommand = connection.CreateCommand();
        copyCommand.CommandText = @$"INSERT INTO {GetTableId()}
SELECT
@DestinationUri,
ElementName,
ElementNamespace,
ElementValue
FROM {GetTableId()} WHERE Uri = @SourceUri";
        
        copyCommand.Parameters.Add(new SqlParameter("@SourceUri", sourceUri));
        copyCommand.Parameters.Add(new SqlParameter("@DestinationUri", destinationUri));

        return copyCommand;
    }
}