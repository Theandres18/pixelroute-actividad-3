using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using PixelRoute.Infrastructure.Persistence;

namespace PixelRoute.Tests.Infrastructure;

/// <summary>Base de datos SQLite temporal y aislada para cada prueba.</summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"pixelroute-test-{Guid.NewGuid():N}.db");

    public SqliteTestDatabase()
    {
        Factory = new SqliteConnectionFactory($"Data Source={_path};Pooling=False");
    }

    public SqliteConnectionFactory Factory { get; }

    public async Task<IReadOnlyList<string>> MigrateAsync(bool includeSeed = false)
    {
        var runner = new SqlMigrationRunner(Factory, NullLogger<SqlMigrationRunner>.Instance);
        return await runner.RunAsync(includeSeed);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = await Factory.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> CountAsync(string table)
    {
        await using var connection = await Factory.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value);
    }

    public async Task InsertCourierAsync(Guid id, bool online = true, int maxActive = 3)
    {
        await using var connection = await Factory.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO couriers (id, name, is_online, max_active_deliveries) VALUES (@id, 'Repartidor de prueba', @online, @max)";
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@online", online ? 1 : 0);
        command.Parameters.AddWithValue("@max", maxActive);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // Si el archivo temporal no se puede borrar ahora, el sistema operativo lo limpiará después.
        }
    }
}
