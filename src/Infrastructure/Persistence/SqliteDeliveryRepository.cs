using System.Globalization;
using Microsoft.Data.Sqlite;
using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Errors;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Infrastructure.Persistence;

/// <summary>
/// Adaptador SQLite del puerto IDeliveryRepository.
/// Usa solo consultas parametrizadas y control de concurrencia optimista mediante la versión del agregado.
/// </summary>
public sealed class SqliteDeliveryRepository : IDeliveryRepository
{
    private const int SqliteConstraintError = 19;

    private readonly SqliteConnectionFactory _factory;

    public SqliteDeliveryRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, order_id, address, promised_by, status, courier_id, assigned_at, version " +
            "FROM deliveries WHERE id = @id";
        command.Parameters.AddWithValue("@id", id.Value.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        CourierId? courierId = reader.IsDBNull(5) ? null : new CourierId(Guid.Parse(reader.GetString(5)));
        DateTimeOffset? assignedAt = reader.IsDBNull(6) ? null : ParseDate(reader.GetString(6));

        return Delivery.Restore(
            new DeliveryId(Guid.Parse(reader.GetString(0))),
            new OrderId(Guid.Parse(reader.GetString(1))),
            new DeliveryAddress(reader.GetString(2)),
            new DeliveryPromise(ParseDate(reader.GetString(3))),
            Enum.Parse<DeliveryStatus>(reader.GetString(4)),
            courierId,
            assignedAt,
            reader.GetInt32(7));
    }

    public async Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken);

        if (delivery.Version == 0)
        {
            await InsertAsync(connection, delivery, cancellationToken);
        }
        else
        {
            await UpdateAsync(connection, delivery, cancellationToken);
        }
    }

    private static async Task InsertAsync(SqliteConnection connection, Delivery delivery, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO deliveries (id, order_id, address, promised_by, status, courier_id, assigned_at, version) " +
            "VALUES (@id, @orderId, @address, @promisedBy, @status, @courierId, @assignedAt, 1)";
        command.Parameters.AddWithValue("@id", delivery.Id.Value.ToString());
        command.Parameters.AddWithValue("@orderId", delivery.OrderId.Value.ToString());
        command.Parameters.AddWithValue("@address", delivery.Address.Value);
        command.Parameters.AddWithValue("@promisedBy", FormatDate(delivery.Promise.PromisedBy));
        command.Parameters.AddWithValue("@status", delivery.Status.ToString());
        command.Parameters.AddWithValue("@courierId", CourierValue(delivery));
        command.Parameters.AddWithValue("@assignedAt", AssignedAtValue(delivery));

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintError)
        {
            throw new ConcurrencyConflictException(delivery.Id);
        }

        delivery.MarkPersisted(1);
    }

    private static async Task UpdateAsync(SqliteConnection connection, Delivery delivery, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE deliveries " +
            "SET status = @status, courier_id = @courierId, assigned_at = @assignedAt, version = version + 1 " +
            "WHERE id = @id AND version = @version";
        command.Parameters.AddWithValue("@id", delivery.Id.Value.ToString());
        command.Parameters.AddWithValue("@status", delivery.Status.ToString());
        command.Parameters.AddWithValue("@courierId", CourierValue(delivery));
        command.Parameters.AddWithValue("@assignedAt", AssignedAtValue(delivery));
        command.Parameters.AddWithValue("@version", delivery.Version);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            // Otra operación modificó la entrega después de que la leímos.
            throw new ConcurrencyConflictException(delivery.Id);
        }

        delivery.MarkPersisted(delivery.Version + 1);
    }

    private static object CourierValue(Delivery delivery)
        => delivery.AssignedCourierId is null ? DBNull.Value : delivery.AssignedCourierId.Value.ToString();

    private static object AssignedAtValue(Delivery delivery)
        => delivery.AssignedAt is null ? DBNull.Value : FormatDate(delivery.AssignedAt.Value);

    private static string FormatDate(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDate(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
