using System.Data.Common;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Npgsql;

namespace Dav.AspNetCore.Server.Extensions.Npgsql;

public class NpgsqlLockManager : SqlLockManager
{
    /// <summary>
    /// Initializes a new <see cref="NpgsqlLockManager"/> class.
    /// </summary>
    /// <param name="options">The sql lock options.</param>
    public NpgsqlLockManager(SqlLockOptions options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Creates a db connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The db connection.</returns>
    protected override DbConnection CreateConnection(string connectionString) 
        => new NpgsqlConnection(connectionString);

    /// <summary>
    /// Gets the insert command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="lockType">The lock type.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="recursive">A value indicating whether the lock is recursive.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <param name="depth">The depth.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetInsertCommand(
        DbConnection connection, 
        string id, 
        string uri, 
        LockType lockType, 
        XElement owner, 
        bool recursive, 
        long timeout,
        long totalSeconds, 
        int depth)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO {GetTableId()} VALUES (@Id, @Uri, @LockType, @Owner, @Recursive, @Timeout, @Issued, @Depth)";
        command.Parameters.Add(new NpgsqlParameter("@Id", id));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri));
        command.Parameters.Add(new NpgsqlParameter("@LockType", (int)lockType));
        command.Parameters.Add(new NpgsqlParameter("@Owner", owner.ToString(SaveOptions.DisableFormatting)));
        command.Parameters.Add(new NpgsqlParameter("@Recursive", recursive));
        command.Parameters.Add(new NpgsqlParameter("@Timeout", timeout));
        command.Parameters.Add(new NpgsqlParameter("@Issued", totalSeconds));
        command.Parameters.Add(new NpgsqlParameter("@Depth", depth));

        return command;
    }

    /// <summary>
    /// Gets the active locks command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetActiveLocksCommand(
        DbConnection connection, 
        string uri, 
        int depth, 
        long totalSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {GetTableId()} WHERE ((Depth <= @Depth AND Recursive = true) OR Uri = @Uri) AND (Issued + Timeout > @TotalSeconds OR Timeout = 0)";
        command.Parameters.Add(new NpgsqlParameter("@Depth", depth));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri));
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", totalSeconds));

        return command;
    }

    /// <summary>
    /// Gets the active lock by id command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetActiveLockByIdCommand(
        DbConnection connection, 
        string id, 
        string uri, 
        long totalSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id FROM {GetTableId()} WHERE Id = @Id AND Uri = @Uri AND (Issued + Timeout > @TotalSeconds OR Timeout = 0) LIMIT 1";
        command.Parameters.Add(new NpgsqlParameter("@Id", id));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri));
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", totalSeconds));

        return command;
    }

    /// <summary>
    /// Gets the refresh command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetRefreshCommand(
        DbConnection connection, 
        string id, 
        long timeout, 
        long totalSeconds)
    {
        var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE {GetTableId()} SET Timeout = @Timeout, Issued = @TotalSeconds WHERE Id = @Id";
        updateCommand.Parameters.Add(new NpgsqlParameter("@Id", id));
        updateCommand.Parameters.Add(new NpgsqlParameter("@Timeout", timeout));
        updateCommand.Parameters.Add(new NpgsqlParameter("@TotalSeconds", totalSeconds));

        return updateCommand;
    }

    /// <summary>
    /// Gets the delete command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetDeleteCommand(DbConnection connection, string id)
    {
        var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = $"DELETE FROM {GetTableId()} WHERE Id = @Id";
        deleteCommand.Parameters.Add(new NpgsqlParameter("@Id", id));

        return deleteCommand;
    }

    /// <summary>
    /// Gets the delete stale locks command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected override DbCommand GetDeleteStaleCommand(DbConnection connection, long totalSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {GetTableId()} WHERE (Issued + Timeout < @TotalSeconds AND Timeout <> 0)";
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", totalSeconds));

        return command;
    }
}
