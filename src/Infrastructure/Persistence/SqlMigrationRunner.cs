using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PixelRoute.Infrastructure.Persistence;

/// <summary>
/// Ejecuta, en orden y una sola vez, los scripts SQL versionados que van embebidos en el ensamblado
/// (carpeta Persistence/Migrations). Es un enfoque al estilo Flyway: cada script se registra en
/// la tabla schema_migrations, así que ejecutar el runner dos veces no repite nada.
/// </summary>
public sealed class SqlMigrationRunner
{
    private readonly SqliteConnectionFactory _factory;
    private readonly ILogger<SqlMigrationRunner> _logger;

    public SqlMigrationRunner(SqliteConnectionFactory factory, ILogger<SqlMigrationRunner> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>Devuelve los scripts aplicados en esta ejecución.</summary>
    public async Task<IReadOnlyList<string>> RunAsync(bool includeSeed, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken);

        await ExecuteAsync(
            connection,
            null,
            "CREATE TABLE IF NOT EXISTS schema_migrations (version TEXT NOT NULL PRIMARY KEY, applied_at TEXT NOT NULL);",
            cancellationToken);

        var alreadyApplied = await GetAppliedAsync(connection, cancellationToken);
        var assembly = typeof(SqlMigrationRunner).Assembly;

        var scripts = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Where(name => includeSeed || !name.Contains(".Seed.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var appliedNow = new List<string>();

        foreach (var resourceName in scripts)
        {
            var version = resourceName.Substring(resourceName.IndexOf("Persistence.", StringComparison.Ordinal) + "Persistence.".Length);

            if (alreadyApplied.Contains(version))
            {
                continue;
            }

            string sql;
            using (var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"No se encontró el recurso {resourceName}."))
            using (var reader = new StreamReader(stream))
            {
                sql = await reader.ReadToEndAsync(cancellationToken);
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                await ExecuteAsync(connection, transaction, sql, cancellationToken);
                await RegisterAsync(connection, transaction, version, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                _logger.LogError("Falló la migración {Version}; se deshicieron sus cambios.", version);
                throw;
            }

            appliedNow.Add(version);
            _logger.LogInformation("Migración aplicada: {Version}", version);
        }

        if (appliedNow.Count == 0)
        {
            _logger.LogInformation("La base de datos ya estaba al día.");
        }

        return appliedNow;
    }

    private static async Task<HashSet<string>> GetAppliedAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_migrations";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private static async Task RegisterAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string version,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO schema_migrations (version, applied_at) VALUES (@version, @appliedAt)";
        command.Parameters.AddWithValue("@version", version);
        command.Parameters.AddWithValue("@appliedAt", DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
