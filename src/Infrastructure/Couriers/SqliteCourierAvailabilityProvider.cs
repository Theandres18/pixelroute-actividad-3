using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Couriers;
using PixelRoute.Infrastructure.Persistence;

namespace PixelRoute.Infrastructure.Couriers;

/// <summary>
/// Adaptador SQLite del puerto ICourierAvailabilityProvider.
/// Las entregas activas de un repartidor son las que están Asignadas o En ruta.
/// </summary>
public sealed class SqliteCourierAvailabilityProvider : ICourierAvailabilityProvider
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteCourierAvailabilityProvider(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<CourierAvailability?> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT c.is_online, c.max_active_deliveries, " +
            "       (SELECT COUNT(*) FROM deliveries d " +
            "        WHERE d.courier_id = c.id AND d.status IN ('Assigned', 'InRoute')) AS active_deliveries " +
            "FROM couriers c WHERE c.id = @id";
        command.Parameters.AddWithValue("@id", courierId.Value.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CourierAvailability(
            courierId,
            reader.GetInt32(0) == 1,
            reader.GetInt32(2),
            reader.GetInt32(1));
    }
}
