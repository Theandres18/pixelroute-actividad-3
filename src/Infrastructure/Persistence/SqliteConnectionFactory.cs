using Microsoft.Data.Sqlite;

namespace PixelRoute.Infrastructure.Persistence;

/// <summary>Crea conexiones a SQLite. Es el único punto que conoce la cadena de conexión.</summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
