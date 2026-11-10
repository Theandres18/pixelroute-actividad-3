using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PixelRoute.Infrastructure.Notifications;
using PixelRoute.Infrastructure.Persistence;

namespace PixelRoute.Tests.Api;

/// <summary>Arranca la API completa (entrada, casos de uso, dominio y SQLite real) con una base de datos temporal.</summary>
public sealed class PixelRouteApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"pixelroute-api-{Guid.NewGuid():N}.db");

    public CapturingLoggerProvider Logs { get; } = new();

    public ControllableNotificationGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PixelRoute"] = $"Data Source={_databasePath};Pooling=False",
                ["Security:DispatcherApiKey"] = ApiKey,
                ["Database:SeedDemoData"] = "false",
                ["Notifications:MaxAttempts"] = "3",
                ["Notifications:BaseDelayMilliseconds"] = "1"
            }));

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<INotificationGateway>();
            services.AddSingleton<INotificationGateway>(Gateway);
        });
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    public async Task InsertCourierAsync(Guid id, bool online = true, int maxActive = 3)
    {
        var connectionFactory = Services.GetRequiredService<SqliteConnectionFactory>();
        await using var connection = await connectionFactory.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO couriers (id, name, is_online, max_active_deliveries) VALUES (@id, 'Repartidor de prueba', @online, @max)";
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@online", online ? 1 : 0);
        command.Parameters.AddWithValue("@max", maxActive);
        await command.ExecuteNonQueryAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();

            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // El archivo temporal se limpiará más tarde si sigue en uso.
            }
        }
    }
}
