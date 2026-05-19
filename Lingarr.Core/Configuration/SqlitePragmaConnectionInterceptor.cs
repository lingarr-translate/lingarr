using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lingarr.Core.Configuration;

public sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    private const int BusyTimeoutMilliseconds = 120000;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        if (connection is not SqliteConnection)
        {
            return;
        }

        using var command = connection.CreateCommand();

        command.CommandText = "PRAGMA journal_mode=WAL";
        command.ExecuteScalar();

        command.CommandText = $"PRAGMA busy_timeout={BusyTimeoutMilliseconds}";
        command.ExecuteNonQuery();

        command.CommandText = "PRAGMA synchronous=NORMAL";
        command.ExecuteNonQuery();
    }
}
